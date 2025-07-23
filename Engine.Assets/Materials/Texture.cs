using Engine.Core.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Assets.Materials;

public sealed class Texture : IDisposable
{
    private readonly bool _isValid = false;
    public TextureHandle Handle { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    private readonly List<Image<Rgba32>> _mipLevels;
    private readonly IResampler _sampler = KnownResamplers.Lanczos3;

    private Texture(byte[] data, ushort width, ushort height, bool generateMips = false)
    {
        Width = width;
        Height = height;
        
        CalculateSize(width, height, generateMips, out var totalLevels);
        _mipLevels = new List<Image<Rgba32>>(totalLevels) { Image.LoadPixelData<Rgba32>(data, width, height) };

        unsafe
        {
            fixed (byte* ptr = data)
            {
                var mem = copy(ptr, (uint)data.Length);
                Handle = create_texture_2d(width, height, generateMips, 1, TextureFormat.RGBA8, (ulong)TextureFlags.None, null);
                update_texture_2d(Handle, 0, 0, 0,0, width, height, mem, ushort.MaxValue);
                _isValid = Handle.Valid;
            }
        }

        if (generateMips)
            Task.Run(() => GenerateMips(width, height));
    }
    
    private static void CalculateSize(ushort width, ushort height, bool generateMips, out int totalLevels)
    {
        totalLevels = 1;

        if (!generateMips)
            return;
        
        var mipWidth = width / 2;
        var mipHeight = height / 2;
        
        while (mipWidth > 0 && mipHeight > 0)
        {
            totalLevels += 1;
            mipWidth /= 2;
            mipHeight /= 2;
        }
    }

    private void GenerateMips(ushort width, ushort height)
    {
        var level = 1;
        var mipWidth = width;
        var mipHeight = height;
        
        while (mipWidth > 1 && mipHeight > 1)
        {
            Logger.InfoF("Generating mip level for texture: {0}x{1}", mipWidth / 2, mipHeight / 2);
            GenerateMipLevel(level, mipWidth, mipHeight);
            
            level += 1;
            mipWidth /= 2;
            mipHeight /= 2;
        }
    }

    private unsafe void GenerateMipLevel(int level, ushort parentWidth, ushort parentHeight)
    {
        var width = Math.Max(1, parentWidth / 2);
        var height = Math.Max(1, parentHeight / 2);
        var data = new byte[width * height * 4];
        var length = width * height * 4;
        
        Console.WriteLine(level);
        var mipmap = _mipLevels[0].Clone(ctx => ctx.Resize(
            size: new Size(width, height),
            sampler: _sampler,
            compand: true
        ));

        _mipLevels.Add(mipmap);
        mipmap.CopyPixelDataTo(data);
        mipmap.Save($"proper-mip-{level}.png");

        fixed (byte* ptr = data)
        {
            var mem = copy(ptr, (uint)length);
            update_texture_2d(Handle, 0, (byte)level, 0, 0, (ushort)width, (ushort)height, mem, (ushort)(width * 4));
        }
    }
    
    public void Dispose()
    {
        if (!_isValid || !Handle.Valid)
            return;
        
        destroy_texture(Handle);
    }
    
    public static Texture Load(string path)
    {
        using var image = Image.Load<Rgba32>(path);
                
        var textureData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(textureData);
                
        return new Texture(textureData, (ushort)image.Width, (ushort)image.Height, generateMips: true);
    }
    
    // sRGB 0..255 -> linear 0..1
    float SrgbToLinear(byte v)
    {
        float c = v / 255f;
        return c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    // linear 0..1 -> sRGB 0..255
    byte LinearToSrgb(float l)
    {
        float c = l <= 0.0031308f ? l * 12.92f : 1.055f * MathF.Pow(l, 1f / 2.4f) - 0.055f;
        return (byte)Math.Clamp(MathF.Round(c * 255f), 0, 255);
    }
}