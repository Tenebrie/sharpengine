using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Task = Microsoft.Build.Utilities.Task;

namespace Engine.Tooling.Weaver;

public sealed class WeaverTask : Task
{
    [Required] public string AssemblyPath { get; set; } = "";

    public override bool Execute()
    {
        var asmPath = AssemblyPath;
        var pdbPath = Path.ChangeExtension(asmPath, ".pdb");
        Log.LogMessage(MessageImportance.High, $"[Weaver] Rewriting: {asmPath}");

        using var fs = File.Open(asmPath, FileMode.Open, FileAccess.Read,
                                 FileShare.ReadWrite | FileShare.Delete);

        var read = new ReaderParameters { ReadSymbols = File.Exists(pdbPath), InMemory = true };
        var asm  = AssemblyDefinition.ReadAssembly(fs, read);
        var module = asm.MainModule;

        const string atomFullName = "Engine.Core.EntitySystem.Entities.Atom";

        var patched = 0;

        foreach (var type in module.Types.SelectMany(AllTypes))
        foreach (var m in type.Methods)
        {
            if (!m.HasBody) continue;

            var il = m.Body.GetILProcessor();
            var ins = m.Body.Instructions;

            foreach (var op in ins)
            {
                if ((op.OpCode != OpCodes.Call && op.OpCode != OpCodes.Callvirt) ||
                    op.Operand is not MethodReference mr)
                    continue;

                // We only care about instance Pause()/Unpause() declared on Atom
                var isPauseLike   = mr is { Name: "Pause", HasParameters: false };
                var isUnpauseLike = mr is { Name: "Unpause", HasParameters: false };
                if (!isPauseLike && !isUnpauseLike)
                    continue;

                // IMPORTANT: match on the DECLARING TYPE FULL NAME from the callsite
                if (mr.DeclaringType.FullName != atomFullName) continue;

                if (m.IsStatic)
                {
                    Log.LogWarning($"[Weaver] Skipping static caller: {m.FullName}");
                    continue;
                }

                // Load caller 'this' (box if the caller is a struct)
                if (m.DeclaringType.IsValueType)
                {
                    il.InsertBefore(op, il.Create(OpCodes.Ldarg_0));
                    il.InsertBefore(op, il.Create(OpCodes.Box, module.ImportReference(m.DeclaringType)));
                }
                else
                {
                    il.InsertBefore(op, il.Create(OpCodes.Ldarg_0));
                }

                // Build a reference to PauseButProperly/UnpauseButProperly on THE SAME declaring type as the original call
                // This keeps the assembly/scope correct without resolving anything.
                var newName = isPauseLike ? "PauseBy" : "UnpauseBy";

                // parameter type is Atom (same declaring type as the original Pause), not object
                var atomTypeRef = mr.DeclaringType; // this is the Engine's Atom in the correct scope

                var newRef = new MethodReference(newName, module.TypeSystem.Void, atomTypeRef)
                {
                    HasThis = true
                };
                newRef.Parameters.Add(new ParameterDefinition(atomTypeRef));

                // Swap the call
                op.Operand = module.ImportReference(newRef); // import into this module
                op.OpCode  = OpCodes.Callvirt;

                patched++;
            }
        }

        // Write atomically
        var tmp = asmPath + ".weave.tmp";
        var write = new WriterParameters { WriteSymbols = File.Exists(pdbPath) };
        asm.Write(tmp, write);

        var bak = asmPath + ".bak";
        if (!File.Exists(bak)) File.Copy(asmPath, bak, overwrite: false);
        File.Replace(tmp, asmPath, bak);

        Log.LogMessage(MessageImportance.High, $"[Weaver] Patched {patched} callsite(s).");
        return true;
    }

    private static IEnumerable<TypeDefinition> AllTypes(TypeDefinition t)
    {
        yield return t;
        foreach (var n in t.NestedTypes.SelectMany(AllTypes)) yield return n;
    }
}