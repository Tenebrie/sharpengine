using System.Reflection;
using Engine.Core.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using static Engine.Native.Bgfx.Bgfx;

namespace Engine.Core.Assets.Materials;

public sealed class Texture : IDisposable
{
    public bool IsValid { get; private set; } = false;
    public NativeTexture Handle { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    private Image<Rgba32> _baseImage = null!;
    private readonly IResampler _sampler = KnownResamplers.Lanczos3;

    private Texture(byte[] data, ushort width, ushort height, bool generateMips = false)
    {
        Width = width;
        Height = height;

        Task.Run(() =>
        {
            try
            {
                InitializeAsync(data, width, height, generateMips);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to initialize texture: {e.Message}");
                Console.Error.WriteLine(e);
            }
        });
    }

    private void InitializeAsync(byte[] data, ushort width, ushort height, bool generateMips = false)
    {
        _baseImage = Image.LoadPixelData<Rgba32>(data, width, height);

        Handle = CreateMutableTexture2D(width, height, generateMips, 1, TextureFormat.RGBA8, (ulong)TextureFlags.None);
        UpdateTexture2D(Handle, 0, 0, data);
        IsValid = Handle.Valid;

        if (generateMips)
            Task.Run(() => GenerateMips(width, height));
    }
    
    public void Update(byte[] data, int offsetX, int offsetY, int width, int height)
    {
        UpdateTexture2D(Handle, 0, 0, offsetX, offsetY, width, height, data);
    }
    
    private void GenerateMips(ushort width, ushort height)
    {
        var level = 1;
        var mipWidth = width;
        var mipHeight = height;
        
        var tasks = new List<Task>();
        
        while (mipWidth > 1 && mipHeight > 1)
        {
            var currentLevel = level;
            var currentMipWidth = mipWidth;
            var currentMipHeight = mipHeight;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    GenerateMipLevel(currentLevel, currentMipWidth, currentMipHeight);
                } catch (Exception ex)
                {
                    Logger.Error($"Failed to generate mip level {currentLevel}: {ex.Message}");
                    Console.Error.WriteLine(ex.StackTrace);
                }
            }));

            level += 1;
            mipWidth /= 2;
            mipHeight /= 2;
        }
        Task.WaitAll(tasks.ToArray());
    }

    private void GenerateMipLevel(int level, ushort parentWidth, ushort parentHeight)
    {
        using var taskHandle = BackgroundTaskManager.Start();
        var width = Math.Max(1, parentWidth / 2);
        var height = Math.Max(1, parentHeight / 2);
        var data = new byte[width * height * 4];

        var mipmap = _baseImage.Clone(ctx => ctx.Resize(
            size: new Size(width, height),
            sampler: _sampler,
            compand: true
        ));

        mipmap.CopyPixelDataTo(data);
        UpdateTexture2D(Handle, 0, level, 0, 0, width, height, data);
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (!IsValid || !Handle.Valid)
            return;
        
        DestroyTexture(Handle);
    }
    
    ~Texture() => Dispose();
    
    public static Texture CreateFromDisk(string path)
    {
        var filepath = Path.Combine("Assets", path);
        if (AssetManager.Shared(Assembly.GetCallingAssembly()).Textures.TryGet(filepath, out var texture))
            return texture;
        
        using var image = Image.Load<Rgba32>(filepath);
                
        var textureData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(textureData);
               
        var tex = new Texture(textureData, (ushort)image.Width, (ushort)image.Height, generateMips: true);
        AssetManager.Shared(Assembly.GetCallingAssembly()).Textures.Put(filepath, tex);
        return tex;
    }
    
    public static Texture CreateFromBytes(byte[] data, ushort width, ushort height, bool generateMips = false)
    {
        return new Texture(data, width, height, generateMips);
    }
    
    public static Texture CreateFromImage(Image<Rgba32> image, bool generateMips = false)
    {
        var textureData = new byte[image.Width * image.Width * 4];
        image.CopyPixelDataTo(textureData);
        var tex = new Texture(textureData, (ushort)image.Width, (ushort)image.Height, generateMips);
        AssetManager.Shared(Assembly.GetCallingAssembly()).Textures.Put(Guid.NewGuid().ToString(), tex);

        return tex;
    }
}