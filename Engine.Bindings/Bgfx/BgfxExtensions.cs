using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Engine.Bindings.Bgfx;

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
		UserInterface = 1,
	}

	internal static byte PackDebugColor(DebugColor bgColor, DebugColor fgColor)
	{
		return (byte)(((byte)bgColor << 4) | (byte)fgColor);
	}
	
	// /// <summary>
	// /// Start VertexLayout.
	// /// </summary>
	// ///
	// /// <param name="_rendererType">Renderer backend type. See: `bgfx::RendererType`</param>
	// /// <see cref="Bgfx" srcline="2370" />
	// [DllImport(DllName, EntryPoint="bgfx_vertex_layout_begin", CallingConvention = CallingConvention.Cdecl)]
	// public static extern unsafe VertexLayout* vertex_layout_begin(VertexLayout* _this, RendererType _rendererType);
	//
	// /// <summary>
	// /// Add attribute to VertexLayout.
	// /// @remarks Must be called between begin/end.
	// /// </summary>
	// ///
	// /// <param name="_attrib">Attribute semantics. See: `bgfx::Attrib`</param>
	// /// <param name="_num">Number of elements 1, 2, 3 or 4.</param>
	// /// <param name="_type">Element type.</param>
	// /// <param name="_normalized">When using fixed point AttribType (f.e. Uint8) value will be normalized for vertex shader usage. When normalized is set to true, AttribType::Uint8 value in range 0-255 will be in range 0.0-1.0 in vertex shader.</param>
	// /// <param name="_asInt">Packaging rule for vertexPack, vertexUnpack, and vertexConvert for AttribType::Uint8 and AttribType::Int16. Unpacking code must be implemented inside vertex shader.</param>
	// /// <see cref="Bgfx" srcline="2384" />
	// [DllImport(DllName, EntryPoint="bgfx_vertex_layout_add", CallingConvention = CallingConvention.Cdecl)]
	// public static extern unsafe VertexLayout* vertex_layout_add(VertexLayout* _this, Attrib _attrib, byte _num, AttribType _type, bool _normalized, bool _asInt);
	//
	// /// <summary>
	// /// End VertexLayout.
	// /// </summary>
	// /// <see cref="Bgfx" srcline="2413" />
	// public static unsafe void vertex_layout_end(VertexLayout* _this)
	// {
	// 	
	// }

	public struct VertexLayoutAttribute(Attrib attribute, int num, AttribType type, bool normalized, bool asInt)
	{
		public readonly Attrib Attribute = attribute;
		public readonly int Num = num;
		public readonly AttribType Type = type;
		public readonly bool Normalized = normalized;
		public readonly bool AsInt = asInt;
	}

	public static unsafe VertexLayout CreateVertexLayout(VertexLayoutAttribute[] attribute)
	{
		var layout = new VertexLayout();
		vertex_layout_begin(&layout, get_renderer_type());
		foreach (var attr in attribute)
		{
			vertex_layout_add(&layout, attr.Attribute, (byte)attr.Num, attr.Type, attr.Normalized, attr.AsInt);
		}
		vertex_layout_end(&layout);

		return layout;
	}
	
    /// <summary>
    /// Reset graphic settings and back-buffer size.
    /// @attention This call doesn’t change the window size, it just resizes
    ///   the back-buffer. Your windowing code controls the window size.
    /// </summary>
    ///
    /// <param name="width">Back-buffer width.</param>
    /// <param name="height">Back-buffer height.</param>
    /// <param name="flags">See <see cref="ResetFlags" />.</param>
    /// <param name="format">Texture format. See: `TextureFormat::Enum`.</param>
    /// <see cref="Bindings.Bgfx.Bgfx" srcline="2588" />
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
    /// <see cref="Bindings.Bgfx.Bgfx" srcline="2657" />
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
	/// Create static index buffer.
	/// </summary>
	///
	/// <param name="indices">Array of indices to create an index buffer out of.</param>
	/// <param name="flags">Buffer creation flags. See <see cref="BufferFlags" />.</param>
	/// <see cref="Bindings.Bgfx.Bgfx" srcline="2716" />
	public static unsafe IndexBuffer CreateIndexBuffer(ref ushort[] indices, BufferFlags flags = BufferFlags.None)
	{
		fixed (ushort* ptr = indices)
		{
			var byteSize = (uint)indices.Length * sizeof(ushort);
			var handle = create_index_buffer(copy(ptr, byteSize), 0);
			return new IndexBuffer
			{
				Handle = handle,
				Count = indices.Length
			};
		}
	}
	
	public struct IndexBuffer
	{
		public IndexBufferHandle Handle;
		public int Count;
	}

	/// <summary>
	/// Create static vertex buffer.
	/// </summary>
	///
	/// <param name="verts">Array of vertices to create a vertex buffer out of.</param>
	/// <param name="layout">Vertex layout reference.</param>
	/// <param name="flags">Buffer creation flags. See <see cref="BufferFlags" />.</param>
	/// <see cref="Bindings.Bgfx.Bgfx" srcline="2765" />
	public static unsafe VertexBuffer CreateVertexBuffer<TVertex>(ref TVertex[] verts, ref VertexLayout layout, BufferFlags flags = BufferFlags.None)
		where TVertex : unmanaged
	{
		fixed (TVertex* vPtr = verts)
		fixed (VertexLayout* layoutPtr = &layout)
		{
			var byteSize = (uint)(verts.Length * sizeof(TVertex));
			var handle = create_vertex_buffer(copy(vPtr, byteSize), layoutPtr, (ushort)flags);
			return new VertexBuffer
			{
				Handle = handle,
				Count = verts.Length
			};
		}
	}

	public struct VertexBuffer
	{
		public VertexBufferHandle Handle;
		public int Count;
	}

	/// <summary>
	/// Allocate transient index buffer.
	/// </summary>
	///
	/// <param name="_tib">TransientIndexBuffer structure will be filled, and will be valid for the duration of frame, and can be reused for multiple draw calls.</param>
	/// <param name="_num">Number of indices to allocate.</param>
	/// <param name="_index32">Set to `true` if input indices will be 32-bit.</param>
	/// <see cref="Bindings.Bgfx.Bgfx" srcline="2908" />
	public static unsafe TransientIndexBuffer CreateTransientIndexBuffer(ushort[] indices)
	{
		TransientIndexBuffer ivb;
		alloc_transient_index_buffer(&ivb, (uint)indices.Length, false);
		var ibData = (ushort*)ivb.data;
		for (var index = 0; index < indices.Length; index++)
		{
			ibData[index] = indices[index];
		}

		return ivb;
	}
	
	/// <summary>
	/// Allocate transient vertex buffer.
	/// </summary>
	///
	/// <param name="_tvb">TransientVertexBuffer structure will be filled, and will be valid for the duration of frame, and can be reused for multiple draw calls.</param>
	/// <param name="_num">Number of vertices to allocate.</param>
	/// <param name="layout">Vertex layout.</param>
	/// <see cref="Bindings.Bgfx.Bgfx" srcline="2919" />
	public static unsafe TransientVertexBuffer CreateTransientVertexBuffer<TVertex>(ref TVertex[] verts, ref VertexLayout layout) where TVertex : unmanaged
	{
		TransientVertexBuffer tvb;
		fixed (VertexLayout* layoutPtr = &layout)
		{
			alloc_transient_vertex_buffer(&tvb, (uint)verts.Length, layoutPtr);
		}
		var vbData = (TVertex*)tvb.data;
		for (var i = 0; i < verts.Length; i++)
		{
			vbData[i] = verts[i];
		}

		return tvb;
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
    /// <see cref="Bindings.Bgfx.Bgfx" srcline="3486" />
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
    /// <see cref="Bindings.Bgfx.Bgfx" srcline="3525" />
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
    /// <see cref="Bindings.Bgfx.Bgfx" srcline="4219" />
    public static void SetState(StateFlags flags, uint rgba = 0)
    {
	    set_state((ulong)flags, rgba);
    }
    public static unsafe void SetState(Encoder* encoder, StateFlags flags, uint rgba = 0)
    {
	    encoder_set_state(encoder, (ulong)flags, rgba);
    }

    /// <summary>
    /// Set index buffer for draw primitive.
    /// </summary>
    ///
    /// <param name="handle">Index buffer.</param>
    /// <param name="firstIndex">First index to render.</param>
    /// <param name="numIndices">Number of indices to render.</param>
    /// <see cref="Bindings.Bgfx.Bgfx" srcline="4318" />
    public static void SetIndexBuffer(IndexBufferHandle handle, int firstIndex, int numIndices)
    {
	    set_index_buffer(handle, (uint)firstIndex, (uint)numIndices);
    }
    
    public static void SetIndexBuffer(IndexBuffer buffer)
	{
	    set_index_buffer(buffer.Handle, 0, (uint)buffer.Count);
	}
	public static unsafe void SetIndexBuffer(TransientIndexBuffer buffer)
	{
		set_transient_index_buffer(&buffer, 0, buffer.size);
	}
	public static unsafe void SetIndexBuffer(Encoder* encoder, IndexBuffer buffer)
	{
		encoder_set_index_buffer(encoder, buffer.Handle, 0, (uint)buffer.Count);
	}

    /// <summary>
    /// Set vertex buffer for draw primitive.
    /// </summary>
    ///
    /// <param name="handle">Vertex buffer.</param>
    /// <param name="startVertex">First vertex to render.</param>
    /// <param name="numVertices">Number of vertices to render.</param>
    ///
    /// <see cref="Bindings.Bgfx.Bgfx" srcline="4352" />
    public static void SetVertexBuffer(VertexBufferHandle handle, int startVertex, int numVertices)
    {
	    set_vertex_buffer(0, handle, (uint)startVertex, (uint)numVertices);
    }

    public static void SetVertexBuffer(VertexBuffer buffer)
    {
	    set_vertex_buffer(0, buffer.Handle, 0, (uint)buffer.Count);
    }
    public static unsafe void SetVertexBuffer(TransientVertexBuffer buffer)
    {
		set_transient_vertex_buffer(0, &buffer, 0, buffer.size);
    }
    public static unsafe void SetVertexBuffer(Encoder* encoder, VertexBuffer buffer)
    {
	    encoder_set_vertex_buffer(encoder, 0, buffer.Handle, 0, (uint)buffer.Count);
    }

    /// <summary>
    /// Set instance data buffer for draw primitive.
    /// </summary>
    ///
    /// <param name="idb">Transient instance data buffer.</param>
    /// <param name="start">First instance data.</param>
    /// <param name="num">Number of data instances.</param>
    /// 
    /// <see cref="Bindings.Bgfx.Bgfx" srcline="4437" />
    public static void SetInstanceDataBuffer(DynamicVertexBufferHandle idb, uint start, uint num)
    {
	    set_instance_data_from_dynamic_vertex_buffer(idb, start, num);
    }
    public static unsafe void SetInstanceDataBuffer(Encoder* encoder, DynamicVertexBufferHandle idb, uint start, uint num)
    {
	    encoder_set_instance_data_from_dynamic_vertex_buffer(encoder, idb, start, num);
    }
    
    /// <summary>
    /// Marks a view as "touched", ensuring that its background is cleared even if nothing is rendered.
    /// </summary>
    /// <param name="viewId">The index of the view to touch.</param>
    /// <returns>The number of draw calls.</returns>
    /// <see cref="Bindings.Bgfx.Bgfx" srcline="4492" />
    public static void Touch(ViewId viewId)
    {
	    touch((ushort)viewId);
    }

    /// <summary>
    /// Submit primitive for rendering.
    /// </summary>
    ///
    /// <param name="_id">View id.</param>
    /// <param name="_program">Program.</param>
    /// <param name="_depth">Depth for sorting.</param>
    /// <param name="_flags">Which states to discard for next draw. See `BGFX_DISCARD_*`.</param>
    ///
    public static void Submit(ushort viewId, ProgramHandle _program, uint _depth, byte _flags)
    {
	    submit(viewId, _program, _depth, _flags);
    }
    public static void Submit(ViewId viewId, ProgramHandle _program, uint _depth, byte _flags)
    {
	    submit((ushort)viewId, _program, _depth, _flags);
    }
    public static unsafe void Submit(Encoder* encoder, ushort viewId, ProgramHandle _program, uint _depth, byte _flags)
    {
	    encoder_submit(encoder, viewId, _program, _depth, _flags);
    }
}