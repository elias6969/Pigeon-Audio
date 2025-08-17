#version 410 core
in float v_alpha;
out vec4 fragColor;

uniform float u_flash; // 0..1
uniform vec3 u_debugTint;


void main() {
    // soft round sprite
    vec2 uv = gl_PointCoord * 2.0 - 1.0;   // -1..1
    float r2 = dot(uv, uv);
    float soft = smoothstep(1.0, 0.0, r2);

    float glow = 1.0 + 0.7 * u_flash;      // brighten on beat
    //vec3  col  = vec3(1.0, 0.85, 0.45) * glow;
vec3 col = (vec3(0,0.22,1.0) + u_debugTint) * (1.0 + 0.7*u_flash);
    fragColor = vec4(col, soft * v_alpha);
}
