#version 420 core
in vec2 v_uv;
out vec4 FragColor;

uniform sampler2D u_texture;
uniform float u_amplitude;
uniform float u_time;

void main()
{
    vec2 center = vec2(0.5);
    vec2 uv = v_uv - center;
    float r = length(uv);
    float angle = atan(uv.y, uv.x);

    float pulse = sin(r * 10.0 - u_time * 4.0) * 0.5 + 0.5;
    float brightness = smoothstep(0.4, 0.0, abs(pulse - u_amplitude));

    vec4 tex = texture(u_texture, v_uv);
    FragColor = vec4(tex.rgb * brightness, 1.0);
}
