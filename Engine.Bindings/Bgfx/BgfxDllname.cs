// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeTypeMemberModifiers
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
namespace Engine.Bindings.Bgfx
{
    public static partial class Bgfx
    {
#if DEBUG
       const string DllName = "bgfx_debug";
#else
       const string DllName = "bgfx";
#endif
    }
}