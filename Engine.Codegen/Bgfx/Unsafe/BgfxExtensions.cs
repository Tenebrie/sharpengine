using System.Runtime.InteropServices;

namespace Engine.Codegen.Bgfx.Unsafe;

public static partial class Bgfx
{
    /// <summary>
    /// Set view clear flags.
    /// </summary>
    ///
    /// <param name="viewId">View id.</param>
    /// <param name="flags">Clear flags. Use `BGFX_CLEAR_NONE` to remove any clear operation. See: `BGFX_CLEAR_*`.</param>
    /// <param name="rgba">Color clear value.</param>
    /// <param name="depth">Depth clear value.</param>
    /// <param name="stencil">Stencil clear value.</param>
    ///
    [DllImport(DllName, EntryPoint="bgfx_set_view_clear", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetViewClear(ushort viewId, ClearFlags flags, uint rgba, float depth, byte stencil);
}