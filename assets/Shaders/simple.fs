#version 420 core

in vec2 uv;             // Normalized screen UV (0 to 1)
out vec4 FragColor;

uniform float u_time;
uniform sampler2D u_texture;

const float PI = 3.14159;

#define NUM_BARS 64     // Number of bars
layout(std140, binding = 0) uniform FFTBlock {
    float u_fft[NUM_BARS];
};

vec3 getBarColor(vec2 uv, float time, sampler2D tex) {
    float barWidth = 1.0 / float(NUM_BARS);
    int index = int(uv.x / barWidth);
    if (index >= NUM_BARS) discard;

    float value = clamp(u_fft[index] * 10.0, 0.0, 1.0);
    float barHeight = value;

    float edgeFade = smoothstep(barHeight, barHeight - 0.08, uv.y);
    float pulse = sin(time + float(index) * 0.5) * 0.1 + 0.9;

    vec3 baseColor = mix(vec3(0.0, 0.2, 0.5), vec3(0.0, 0.7, 1.0), uv.y / barHeight);
    baseColor *= pulse * edgeFade;

    vec3 bg = texture(tex, uv).rgb * 0.3;
    return (uv.y < barHeight) ? baseColor + bg : bg;
}


void main() {
  vec3 color = getBarColor(uv, u_time, u_texture);
  FragColor = vec4(color, 1.0);
}
