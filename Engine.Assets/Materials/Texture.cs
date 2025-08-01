using Engine.Core.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using static Engine.Bindings.Bgfx.Bgfx;

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

    public unsafe void Update(byte[] data, int offsetX, int offsetY, int width, int height)
    {
        fixed (byte* ptr = data)
        {
            var mem = copy(ptr, (uint)data.Length);
            update_texture_2d(Handle, 0, 0, (ushort)offsetX, (ushort)offsetY, (ushort)width, (ushort)height, mem, ushort.MaxValue);
        }
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
        
        var mipmap = _mipLevels[0].Clone(ctx => ctx.Resize(
            size: new Size(width, height),
            sampler: _sampler,
            compand: true
        ));

        _mipLevels.Add(mipmap);
        mipmap.CopyPixelDataTo(data);

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
    
    public static Texture CreateFromDisk(string path)
    {
        using var image = Image.Load<Rgba32>(path);
                
        var textureData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(textureData);
                
        return new Texture(textureData, (ushort)image.Width, (ushort)image.Height, generateMips: true);
    }
    
    public static Texture CreateFromBytes(byte[] data, ushort width, ushort height, bool generateMips = false)
    {
        return new Texture(data, width, height, generateMips);
    }
    
    public static Texture CreateFromImage(Image<Rgba32> image, bool generateMips = false)
    {
        var textureData = new byte[image.Width * image.Width * 4];
        image.CopyPixelDataTo(textureData);
        
        return new Texture(textureData, (ushort)image.Width, (ushort)image.Height, generateMips);
    }
}