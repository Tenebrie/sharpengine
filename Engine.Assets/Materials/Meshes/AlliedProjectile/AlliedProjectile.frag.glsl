$input v_color0, v_uv0

#include <bgfx_shader.sh>

SAMPLER2D(s_diffuse, 0);

void main()
{
    gl_FragColor = vec4(0.0, 1.0, 1.0, 1.0);
}