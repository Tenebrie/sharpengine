using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Engine.Codegen.Bgfx.Unsafe;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static partial class Bgfx
{
	public enum DebugColor : byte
	{
		Black         = 0,
		Blue          = 1,
		Green         = 2,
		Cyan          = 3,
		Red           = 4,
		Magenta       = 5,
		Brown         = 6,
		LightGray     = 7,
		DarkGray      = 8,
		LightBlue     = 9,
		LightGreen    = 10,
		LightCyan     = 11,
		LightRed      = 12,
		LightMagenta  = 13,
		Yellow        = 14,
		White         = 15
	}
	
	public enum ViewId : ushort
	{
		World = 0,
		DebugText = 1,
	}

	internal static byte PackDebugColor(DebugColor bgColor, DebugColor fgColor)
	{
		return (byte)(((byte)bgColor << 4) | (byte)fgColor);
	}
	
    /// <summary>
    /// Reset graphic settings and back-buffer size.
    /// @attention This call doesn’t change the window size, it just resizes
    ///   the back-buffer. Your windowing code controls the window size.
    /// </summary>
    ///
    /// <param name="width">Back-buffer width.</param>
    /// <param name="height">Back-buffer height.</param>
    /// <param name="flags">See: `BGFX_RESET_*` for more info.   - `BGFX_RESET_NONE` - No reset flags.   - `BGFX_RESET_FULLSCREEN` - Not supported yet.   - `BGFX_RESET_MSAA_X[2/4/8/16]` - Enable 2, 4, 8 or 16 x MSAA.   - `BGFX_RESET_VSYNC` - Enable V-Sync.   - `BGFX_RESET_MAXANISOTROPY` - Turn on/off max anisotropy.   - `BGFX_RESET_CAPTURE` - Begin screen capture.   - `BGFX_RESET_FLUSH_AFTER_RENDER` - Flush rendering after submitting to GPU.   - `BGFX_RESET_FLIP_AFTER_RENDER` - This flag  specifies where flip     occurs. Default behaviour is that flip occurs before rendering new     frame. This flag only has effect when `BGFX_CONFIG_MULTITHREADED=0`.   - `BGFX_RESET_SRGB_BACKBUFFER` - Enable sRGB back-buffer.</param>
    /// <param name="format">Texture format. See: `TextureFormat::Enum`.</param>
    /// <see cref="Bgfx" srcline="2588" />
    ///
    public static void Reset(int width, int height, ResetFlags flags, TextureFormat format)
    {
        reset((uint)width, (uint)height, (uint)flags, format);
    }

    /// <summary>
    /// Advance to next frame. When using multithreaded renderer, this call
    /// just swaps internal buffers, kicks render thread, and returns. In
    /// single threaded renderer this call does frame rendering.
    /// </summary>
    ///
    /// <param name="capture">Capture frame with graphics debugger.</param>
    ///
    public static uint Frame(bool capture)
    {
	    return frame(capture);
    }
    
    /// <summary>
    /// Set debug flags.
    /// </summary>
    ///
    /// <param name="debug">Available flags:   - `BGFX_DEBUG_IFH` - Infinitely fast hardware. When this flag is set     all rendering calls will be skipped. This is useful when profiling     to quickly assess potential bottlenecks between CPU and GPU.   - `BGFX_DEBUG_PROFILER` - Enable profiler.   - `BGFX_DEBUG_STATS` - Display internal statistics.   - `BGFX_DEBUG_TEXT` - Display debug text.   - `BGFX_DEBUG_WIREFRAME` - Wireframe rendering. All rendering     primitives will be rendered as lines.</param>
    /// <see cref="Bgfx" srcline="2657" />
    [DllImport(DllName, EntryPoint="bgfx_set_debug", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetDebug(DebugFlags debug);

    /// <inheritdoc cref="DebugTextClear(DebugColor, bool)"/>
    public static void DebugTextClear()
    {
	    dbg_text_clear(PackDebugColor(DebugColor.Black, DebugColor.Black), false);
    }
    
    /// <summary>
    /// Clear internal debug text buffer.
    /// </summary>
    ///
    /// <param name="bgColor">Background color.</param>
    /// <param name="smallFont">Default 8x16 or 8x8 font.</param>
    ///
    public static void DebugTextClear(DebugColor bgColor, bool smallFont)
    {
	    dbg_text_clear(PackDebugColor(bgColor, DebugColor.Black), smallFont);
    }

	/// <summary>
	/// Print formatted data to internal debug text character-buffer (VGA-compatible text mode).
	/// </summary>
	/// 
	/// <param name="x">Position x from the left corner of the window.</param>
	/// <param name="y">Position y from the top corner of the window.</param>
	/// <param name="fgColor">The foreground color of the text.</param>
	/// <param name="bgColor">The background color of the text.</param>
	/// <param name="format">`printf` style format.</param>
	/// <param name="args">Printf arguments.</param>
	/// 
	public static void DebugTextPrintf(int x, int y, DebugColor bgColor, DebugColor fgColor, string format, string args)
	{
		dbg_text_printf((ushort)x, (ushort)y, PackDebugColor(bgColor, fgColor), string.Format(format, args));
	}
	
	public static void DebugTextWrite(int x, int y, string message) {
		dbg_text_printf((ushort)x, (ushort)y, PackDebugColor(DebugColor.Black, DebugColor.White), message);
	}
	
	/// <summary>
	/// Writes debug text to the screen.
	/// </summary>
	/// <param name="x">Position x from the left corner of the window.</param>
	/// <param name="y">Position y from the top corner of the window.</param>
	/// <param name="fgColor">The foreground color of the text.</param>
	/// <param name="bgColor">The background color of the text.</param>
	/// <param name="message">The message to write.</param>
	public static void DebugTextWrite(int x, int y, DebugColor bgColor, DebugColor fgColor, string message) {
		dbg_text_printf((ushort)x, (ushort)y, PackDebugColor(bgColor, fgColor), message);
	}

	/// <summary>
	/// Print formatted data from variable argument list to internal debug text character-buffer (VGA-compatible text mode).
	/// </summary>
	///
	/// <param name="x">Position x from the left corner of the window.</param>
	/// <param name="y">Position y from the top corner of the window.</param>
	/// <param name="bgColor">Background color.</param>
	/// <param name="fgColor">Foreground color.</param>
	/// <param name="format">`printf` style format.</param>
	/// <param name="argList">Variable arguments list for format string.</param>
	///
	public static void DebugTextVprintf(int x, int y, DebugColor bgColor, DebugColor fgColor, [MarshalAs(UnmanagedType.LPStr)] string format, IntPtr argList)
	{
		dbg_text_vprintf((ushort)x, (ushort)y, PackDebugColor(bgColor, fgColor), format, argList);
	}

	/// <summary>
	/// Draws data directly into the debug text buffer.
	/// </summary>
	/// <param name="x">The X position, in cells.</param>
	/// <param name="y">The Y position, in cells.</param>
	/// <param name="width">The width of the image to draw.</param>
	/// <param name="height">The height of the image to draw.</param>
	/// <param name="data">The image data bytes.</param>
	/// <param name="pitch">The pitch of each line in the image data.</param>
	public static unsafe void DebugTextImage (int x, int y, int width, int height, byte[] data, int pitch) {
		fixed (byte* ptr = data)
			dbg_text_image((ushort)x, (ushort)y, (ushort)width, (ushort)height, (void*)new IntPtr(ptr), (ushort)pitch);
	}
    
    /// <summary>
    /// Set view rectangle. Draw primitive outside view will be clipped.
    /// </summary>
    ///
    /// <param name="viewId">View id.</param>
    /// <param name="x">Position x from the left corner of the window.</param>
    /// <param name="y">Position y from the top corner of the window.</param>
    /// <param name="width">Width of view port region.</param>
    /// <param name="height">Height of view port region.</param>
    /// <see cref="Bgfx" srcline="3486" />
    ///
    public static void SetViewRect(ViewId viewId, int x, int y, int width, int height)
    {
        set_view_rect((ushort)viewId, (ushort)x, (ushort)y, (ushort)width, (ushort)height);
    }

    /// <summary>
    /// Set view clear flags.
    /// </summary>
    ///
    /// <param name="viewId">View id.</param>
    /// <param name="flags">Clear flags. Use `BGFX_CLEAR_NONE` to remove any clear operation. See: `BGFX_CLEAR_*`.</param>
    /// <param name="rgba">Color clear value.</param>
    /// <param name="depth">Depth clear value.</param>
    /// <param name="stencil">Stencil clear value.</param>
    /// <see cref="Bgfx" srcline="3525" />
    ///
    public static void SetViewClear(ViewId viewId, ClearFlags flags, uint rgba, float depth, byte stencil)
    {
	    set_view_clear((ushort)viewId, (uint)flags, rgba, depth, stencil);
    }
    
    /// <summary>
    /// Set render states for draw primitive.
    /// @remarks
    ///   1. To set up more complex states use:
    ///      `BGFX_STATE_ALPHA_REF(_ref)`,
    ///      `BGFX_STATE_POINT_SIZE(_size)`,
    ///      `BGFX_STATE_BLEND_FUNC(_src, _dst)`,
    ///      `BGFX_STATE_BLEND_FUNC_SEPARATE(_srcRGB, _dstRGB, _srcA, _dstA)`,
    ///      `BGFX_STATE_BLEND_EQUATION(_equation)`,
    ///      `BGFX_STATE_BLEND_EQUATION_SEPARATE(_equationRGB, _equationA)`
    ///   2. `BGFX_STATE_BLEND_EQUATION_ADD` is set when no other blend
    ///      equation is specified.
    /// </summary>
    ///
    /// <param name="flags">State flags. Default state for primitive type is   triangles. See: `BGFX_STATE_DEFAULT`.   - `BGFX_STATE_DEPTH_TEST_*` - Depth test function.   - `BGFX_STATE_BLEND_*` - See remark 1 about BGFX_STATE_BLEND_FUNC.   - `BGFX_STATE_BLEND_EQUATION_*` - See remark 2.   - `BGFX_STATE_CULL_*` - Backface culling mode.   - `BGFX_STATE_WRITE_*` - Enable R, G, B, A or Z write.   - `BGFX_STATE_MSAA` - Enable hardware multisample antialiasing.   - `BGFX_STATE_PT_[TRISTRIP/LINES/POINTS]` - Primitive type.</param>
    /// <param name="rgba">Sets blend factor used by `BGFX_STATE_BLEND_FACTOR` and   `BGFX_STATE_BLEND_INV_FACTOR` blend modes.</param>
    /// <see cref="Bgfx" srcline="4219" />
    ///
    [DllImport(DllName, EntryPoint="bgfx_set_state", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetState(StateFlags flags, uint rgba = 0);
    
    /// <summary>
    /// Marks a view as "touched", ensuring that its background is cleared even if nothing is rendered.
    /// </summary>
    /// <param name="viewId">The index of the view to touch.</param>
    /// <returns>The number of draw calls.</returns>
    /// <see cref="Bgfx" srcline="4492" />
    public static void Touch(ViewId viewId) {
	    touch((ushort)viewId);
    }
}