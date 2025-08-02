using System.Runtime.InteropServices;
using Engine.Core.Assets;
using Engine.Core.Assets.Materials;
using Engine.Core.Common;
using FontStashSharp;
using FontStashSharp.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Engine.Native.Bgfx.Bgfx;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Engine.Module.Rendering.Fonts;

public class FontRenderer : IFontStashRenderer2, IDisposable
{
    private FontSystem _fontSystem;
    private DynamicSpriteFont _font;
    private UniformHandle _diffuseTextureHandle;

    private VertexLayout _vertexLayout;
    private MaterialInstance _material = null!;
    
    public FontRenderer()
    {
        _fontSystem = new FontSystem(new FontSystemSettings
        {
            TextureWidth = 1024,
            TextureHeight = 1024
        });
        _fontSystem.AddFont(File.ReadAllBytes("Assets/Fonts/Roboto-Regular.ttf"));
        _font = _fontSystem.GetFont(128);
    }

    public void Initialize()
    {
        _material = AssetManager.LoadMaterial("UserInterface/Font/Font");
        _diffuseTextureHandle = create_uniform("s_diffuse", UniformType.Sampler, 1);
        _vertexLayout = CreateVertexLayout([
            new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.TexCoord0, 2, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true)
        ]);
    }
    
    public void DrawQuad(
        object texture,
        ref VertexPositionColorTexture topLeft,
        ref VertexPositionColorTexture topRight,
        ref VertexPositionColorTexture bottomLeft,
        ref VertexPositionColorTexture bottomRight)
    {
        var tex = (Texture)texture;
        var size = new System.Numerics.Vector3(1920, -1080, 1);
        var vertices = new[]
        {
            new RenderingVertex(topLeft.Position / size, topLeft.TextureCoordinate, topLeft.Color),
            new RenderingVertex(topRight.Position / size, topRight.TextureCoordinate, topRight.Color),
            new RenderingVertex(bottomLeft.Position / size, bottomLeft.TextureCoordinate, bottomLeft.Color),
            new RenderingVertex(bottomRight.Position / size, bottomRight.TextureCoordinate, bottomRight.Color)
        };

        var indices = new ushort[]
        {
            0, 1, 2,
            1, 3, 2
        };

        var vertexBuffer = CreateTransientVertexBuffer(ref vertices, ref _vertexLayout);
        var indexBuffer = CreateTransientIndexBuffer(indices);
        SetVertexBuffer(vertexBuffer);
        SetIndexBuffer(indexBuffer);
        SetState(StateFlags.WriteRgb | StateFlags.WriteA | StateFlags.BlendAlphaToCoverage);
        set_texture(0, _diffuseTextureHandle, tex.Handle, 0);
        Submit(ViewId.UserInterface, _material.Program, 1, 0);
    }

    public ITexture2DManager TextureManager { get; } = new MyTextureManager();

    public void RenderText(string text, float x, float y, Color color)
    {
        _font.DrawText(this, text, new Vector2(x, y), new FSColor(color.R, color.G, color.B, color.A));
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _fontSystem.Dispose();
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Vector2 uv, FSColor color)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly Vector2Float Uv = uv.Downgrade();
        public readonly uint Color = color.ToAbgr();
    }

}

public class MyTextureManager : ITexture2DManager
{
    public object CreateTexture(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        return Texture.CreateFromImage(image);
    }

    public Point GetTextureSize(object texture)
    {
        var tex = (Texture)texture;
        return new Point(tex.Width, tex.Height);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data)
    {
        var tex = (Texture)texture;
        tex.Update(data, bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }
}

public static class ColorExtensions
{
    public static uint ToAbgr(this FSColor color)
    {
        var a = (uint)color.A & 0xFF;
        var r = (uint)color.R & 0xFF;
        var g = (uint)color.G & 0xFF;
        var b = (uint)color.B & 0xFF;

        return (a << 24) | (b << 16) | (g <<  8) | r;
    }
}
