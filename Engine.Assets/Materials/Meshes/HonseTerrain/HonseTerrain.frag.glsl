$input v_color0, v_uv0

#include <bgfx_shader.sh>

SAMPLER2D(s_diffuse, 0);

void main()
{
    // gl_FragColor = v_color0;
    gl_FragColor = texture2D(s_diffuse, v_uv0) * vec4(0.5, 0.5, 0.5, 1.0);
}
