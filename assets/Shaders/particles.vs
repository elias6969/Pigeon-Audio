#version 410 core
layout(location=0) in vec2 a_pos;   // already NDC
layout(location=1) in float a_size; // in "pixels"
layout(location=2) in float a_alpha;

uniform float u_flash; // 0..1

out float v_alpha;

void main() {
    gl_Position = vec4(a_pos, 0.0, 1.0);
    gl_PointSize = a_size * (1.0 + 0.4 * u_flash); // per-particle size
    v_alpha = a_alpha;
}
