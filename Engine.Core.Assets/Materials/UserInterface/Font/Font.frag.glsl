// fragment shader
$input v_color0, v_uv0

#include <bgfx_shader.sh>

SAMPLER2D(s_diffuse, 0);

void main()
{
    // sample the atlas (gives you white glyph + alpha) 
    vec4 sampled = texture2D(s_diffuse, v_uv0);

    // multiply by the color you passed in
    gl_FragColor = sampled * v_color0;
}