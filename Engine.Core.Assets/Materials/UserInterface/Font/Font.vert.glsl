$input a_position, a_color0, i_data0, i_data1, i_data2, i_data3, a_texcoord0
$output v_color0, v_uv0

#include <bgfx_shader.sh>

void main()
{
    gl_Position = vec4(a_position, 1.0);

    v_color0 = a_color0;
    v_uv0 = a_texcoord0;
}
 