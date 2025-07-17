using FontStashSharp;

namespace Engine.Rendering.Fonts;

public class FontRenderer
{
    public FontRenderer()
    {
        var fstash   = new FontSystem();                     // FontStashSharp
        var ttf = File.ReadAllBytes("Assets/Fonts/Roboto-Regular.ttf");
        fstash.AddFont(ttf);
        // fstash.GetFont(32).DrawText
            
            
            
            
        // var text = font.LayoutText("Hello, Maya!", 32f);
        // ITexture2D tx = new BgfxTexture2D(fstash.Texture);   // bind atlas as bgfx texture
    }
}