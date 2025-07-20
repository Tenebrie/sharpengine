using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Assets.Materials;

public sealed class Texture : IDisposable
{
    public bool IsValid = false;
    public TextureHandle Handle { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Texture(byte[] data, ushort width, ushort height, bool hasMips = false)
    {
        Width = width;
        Height = height;

        unsafe
        {
            fixed (byte* ptr = data)
            {
                var mem = copy(ptr, (uint)data.Length);
                Handle = create_texture_2d(width, height, hasMips, 1, TextureFormat.RGBA8, (ulong)TextureFlags.None, mem);
                IsValid = Handle.Valid;
            }
        }
    }

    public void Dispose()
    {
        if (!IsValid || !Handle.Valid)
            return;
        
        destroy_texture(Handle);
    }
    
    public static Texture Load(string path)
    {
        using var image = Image.Load<Rgba32>(path);
        // Flip Y to match OpenGL expectations
        // image.Mutate(x => x.Flip(FlipMode.Vertical));
                
        var textureData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(textureData);
                
        return new Texture(textureData, (ushort)image.Width, (ushort)image.Height);
    }
}