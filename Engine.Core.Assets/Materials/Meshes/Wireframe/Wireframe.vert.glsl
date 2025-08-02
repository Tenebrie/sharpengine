$input a_position, a_color0, i_data0, i_data1, i_data2, i_data3
$output v_color0

#include <bgfx_shader.sh>

void main()
{
    // Reconstruct 4×4 model matrix from four vec4 instance‐attributes.
    mat4 model = mtxFromCols(i_data0, i_data1, i_data2, i_data3);

    // Transform vertex
    vec4 worldPos = mul(model, vec4(a_position, 1.0) );
    gl_Position = mul(u_modelViewProj, worldPos);

    // Pass through vertex color
    v_color0 = a_color0;
}
