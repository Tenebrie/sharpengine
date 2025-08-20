using System.Reflection;
using Diligent;
using Engine.Core.Assets.Rendering;
using Engine.Core.Communication.Tasks;
using Engine.Core.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using TextureFormat = Diligent.TextureFormat;

namespace Engine.Core.Assets.Materials;

public sealed class Texture : IDisposable
{
    public static RenderContext Context { get; set; }
    
    public bool IsValid { get; private set; } = false;
    // public NativeTexture Handle { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    private readonly Image<Rgba32> _baseImage;
    private readonly IResampler _sampler = KnownResamplers.Lanczos3;
    private readonly ITexture _textureHandle;

    private unsafe Texture(byte[] data, ushort width, ushort height, bool generateMips = false)
    {
        Width = width;
        Height = height;
        
        _baseImage = Image.LoadPixelData<Rgba32>(data, width, height);
        _textureHandle = Context.RenderDevice.CreateTexture(new TextureDesc
        {
            Type = ResourceDimension.Tex2d,
            Width = width,
            Height = height,
            Format = TextureFormat.RGBA8_UNorm_sRGB,
            BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
            MipLevels = generateMips ? 0u : 1u,
            MiscFlags = MiscTextureFlags.GenerateMips
        });

        fixed (byte* pixelDataPtr = data)
        {
            Context.DeviceContext.UpdateTexture(
                _textureHandle,
                mipLevel: 0,
                slice: 0,
                dstBox: new Box { MaxX = width, MaxY = height },
                new TextureSubResData { Data = (IntPtr)pixelDataPtr, Stride = (ulong)(width * 4) },
                ResourceStateTransitionMode.Transition,
                ResourceStateTransitionMode.Transition
            );
        }

        if (generateMips)
            Task.Run(() => GenerateMips(width, height));
            // GenerateMips(width, height);
    }

    public ITextureView GetDefaultView()
    {
        return _textureHandle.GetDefaultView(TextureViewType.ShaderResource);
    }

    public override int GetHashCode()
    {
        return _baseImage.GetHashCode();
    }

    public unsafe void Update(byte[] data, int minX, int minY, int maxX, int maxY)
    {
        fixed (byte* pixelDataPtr = data)
        {
            Context.DeviceContext.UpdateTexture(
                _textureHandle,
                mipLevel: 0,
                slice: 0,
                dstBox: new Box
                {
                    MinX = (uint)minX, MaxX = (uint)maxX,
                    MinY = (uint)minY, MaxY = (uint)maxY
                },
                new TextureSubResData { Data = (IntPtr)pixelDataPtr, Stride = (ulong)((maxX - minX) * 4) },
                ResourceStateTransitionMode.Transition,
                ResourceStateTransitionMode.Transition
            );
        }
    }
    
    private void GenerateMips(ushort width, ushort height)
    {
        var level = 1;
        var mipWidth = width;
        var mipHeight = height;
        
        while (mipWidth > 1 && mipHeight > 1)
        {
            var currentLevel = level;
            var currentMipWidth = mipWidth;
            var currentMipHeight = mipHeight;
            Task.Run(() =>
            {
                try
                {
                    GenerateMipLevel(currentLevel, currentMipWidth, currentMipHeight);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to generate mip level {currentLevel}: {ex.Message}");
                    Console.Error.WriteLine(ex.StackTrace);
                }
            });

            level += 1;
            mipWidth /= 2;
            mipHeight /= 2;
        }
    }

    private unsafe void GenerateMipLevel(int level, ushort parentWidth, ushort parentHeight)
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
        
        MainThreadTask.Run(() =>
        {
            fixed (byte* pixelDataPtr = data)
            {
                Context.DeviceContext.UpdateTexture(
                    _textureHandle,
                    mipLevel: (uint)level,
                    slice: 0,
                    dstBox: new Box { MaxX = (uint)width, MaxY = (uint)height },
                    new TextureSubResData { Data = (IntPtr)pixelDataPtr, Stride = (ulong)(width * 4) },
                    ResourceStateTransitionMode.Transition,
                    ResourceStateTransitionMode.Transition
                );
            }
        });
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _textureHandle.Dispose();
    }
    
    ~Texture() => Dispose();
    
    public static Texture CreateFromDisk(string path)
    {
        var filepath = Path.Combine("Assets", path);
        if (AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Textures.TryGet(filepath, out var texture))
            return texture;
        
        texture = CreateFromDisktWithoutCache(filepath);
        AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Textures.Put(filepath, texture);
        return texture;
    }

    private static Texture CreateFromDisktWithoutCache(string filepath)
    {
        using var image = Image.Load<Rgba32>(filepath);
                
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
        var textureData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(textureData);
        var tex = new Texture(textureData, (ushort)image.Width, (ushort)image.Height, generateMips);
        // AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Textures.Put(Guid.NewGuid().ToString(), tex);

        return tex;
    }
}