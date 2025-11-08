using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Engine.Tooling.Weaver;

internal static class WeaverTask
{
    public static void Execute(string assemblyPath)
    {
        Console.WriteLine($"[Weaver] Rewriting: {assemblyPath}");

        using var fs = File.Open(assemblyPath, FileMode.Open, FileAccess.Read,
                                 FileShare.ReadWrite | FileShare.Delete);

        var read = new ReaderParameters { ReadSymbols = true, InMemory = true };
        var asm  = AssemblyDefinition.ReadAssembly(fs, read);
        var module = asm.MainModule;

        const string atomFullName = "Engine.Core.EntitySystem.Entities.Atom";

        var patched = 0;

        foreach (var type in module.Types.SelectMany(AllTypes))
        foreach (var m in type.Methods.ToList())
        {
            if (!m.HasBody) continue;

            var il  = m.Body.GetILProcessor();
            var ins = m.Body.Instructions;

            foreach (var op in ins.ToArray())
            {
                if ((op.OpCode != OpCodes.Call && op.OpCode != OpCodes.Callvirt) ||
                    op.Operand is not MethodReference mr)
                    continue;

                var isPauseLike   = mr is { Name: "Pause", HasParameters: false };
                var isUnpauseLike = mr is { Name: "Unpause", HasParameters: false };
                if (!isPauseLike && !isUnpauseLike)
                    continue;

                if (mr.DeclaringType.FullName != atomFullName) continue;

                if (m.IsStatic)
                {
                    Console.WriteLine($"[Weaver] Skipping static caller: {m.FullName}");
                    continue;
                }

                // load 'this'
                if (m.DeclaringType.IsValueType)
                {
                    il.InsertBefore(op, il.Create(OpCodes.Ldarg_0));
                    il.InsertBefore(op, il.Create(OpCodes.Box, module.ImportReference(m.DeclaringType)));
                }
                else
                {
                    il.InsertBefore(op, il.Create(OpCodes.Ldarg_0));
                }

                var newName = isPauseLike ? "PauseBy" : "UnpauseBy";
                var atomTypeRef = mr.DeclaringType;

                var newRef = new MethodReference(newName, module.TypeSystem.Void, atomTypeRef)
                {
                    HasThis = true
                };
                newRef.Parameters.Add(new ParameterDefinition(atomTypeRef));

                op.Operand = module.ImportReference(newRef);
                op.OpCode  = OpCodes.Callvirt;

                patched++;
            }
        }

        // Write atomically
        var tmp = assemblyPath + ".weave.tmp";
        var write = new WriterParameters { WriteSymbols = module.HasSymbols, SymbolWriterProvider = new EmbeddedPortablePdbWriterProvider()};
        asm.Write(tmp, write);

        var bak = assemblyPath + ".bak";
        if (!File.Exists(bak)) File.Copy(assemblyPath, bak, overwrite: false);
        File.Replace(tmp, assemblyPath, bak);

        var dllName = Path.GetFileNameWithoutExtension(assemblyPath);
        Console.WriteLine($"[Weaver] Patched {patched} callsite(s) in {dllName}");
    }

    private static IEnumerable<TypeDefinition> AllTypes(TypeDefinition t)
    {
        yield return t;
        foreach (var n in t.NestedTypes.SelectMany(AllTypes)) yield return n;
    }
}