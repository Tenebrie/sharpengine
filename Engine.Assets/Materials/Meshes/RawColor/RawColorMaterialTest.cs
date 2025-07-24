using JetBrains.Annotations;

namespace Engine.Assets.Materials.Meshes.RawColor;

public abstract class InlineMaterial() : Material("virtual://*")
{
    public virtual string ComputeShader() => "";
    public virtual string FragmentShader() => throw new Exception("Fragment shader is not implemented");
    public virtual string VertexShader() => throw new Exception("Vertex shader is not implemented");
}


public class RawColorMaterialTest() : InlineMaterial
{
    public override string FragmentShader()
    {
        /*language=glsl*/
        return """
           $input v_color0, v_uv0

           #include <bgfx_shader.sh>

           SAMPLER2D(s_diffuse, 0);

           void main()
           {
               gl_FragColor = v_color0;
               // gl_FragColor = texture2D(s_diffuse, v_uv0);
           }
        """;
    }

    public override string VertexShader()
    {
        return """
           $input a_position, a_color0, i_data0, i_data1, i_data2, i_data3, a_texcoord0
           $output v_color0, v_uv0
           
           #include <bgfx_shader.sh>
           #include <common.sh>   // for mtxFromCols()
           
           void main()
           {
               // Reconstruct 4×4 model matrix from four vec4 instance‐attributes.
               mat4 model = mtxFromCols(i_data0, i_data1, i_data2, i_data3);
           
               // Transform vertex
               vec4 worldPos   = mul(model, vec4(a_position, 1.0) );
               gl_Position     = mul(u_modelViewProj, worldPos);
           
               // Pass through vertex color
               v_color0 = a_color0;
               v_uv0 = a_texcoord0;
           }
        """;
    }
}