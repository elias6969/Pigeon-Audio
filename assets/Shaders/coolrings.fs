#version 420 core
in vec2 uv;
out vec4 FragColor;

#define NUM_BARS 200
layout(std140, binding = 0) uniform FFTBlock {
    float u_fft[NUM_BARS];
};

uniform float u_time;
uniform sampler2D u_texture;
uniform vec2 u_resolution;
uniform vec3 u_barColor;
uniform bool u_animateHue;

const float PI = 3.14159265359;

// ---------------------------
// Hue shift
// ---------------------------
vec3 hueShift(vec3 color, float shift) {
    const mat3 toYIQ = mat3(
        0.299, 0.587, 0.114,
        0.596, -0.274, -0.321,
        0.211, -0.523, 0.312
    );
    const mat3 toRGB = mat3(
        1.0, 0.956, 0.621,
        1.0, -0.272, -0.647,
        1.0, -1.107, 1.705
    );
    vec3 yiq = toYIQ * color;
    float hue = atan(yiq.z, yiq.y) + shift;
    float chroma = length(yiq.yz);
    yiq.y = chroma * cos(hue);
    yiq.z = chroma * sin(hue);
    return toRGB * yiq;
}

// ---------------------------
// Fractal noise field
// ---------------------------
float hash(vec2 p) { return fract(sin(dot(p, vec2(41.23, 289.97))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash(i);
    float b = hash(i + vec2(1, 0));
    float c = hash(i + vec2(0, 1));
    float d = hash(i + vec2(1, 1));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}
float fbm(vec2 p) {
    float f = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 5; i++) {
        f += amp * noise(p);
        p *= 2.0;
        amp *= 0.5;
    }
    return f;
}

// ---------------------------
// Main
// ---------------------------
void main() {
    vec2 st = uv;
    st -= 0.5;
    st.x *= u_resolution.x / u_resolution.y;

    float radius = length(st);
    float angle = atan(st.y, st.x);

    // Audio aggregation
    float low = 0.0, mid = 0.0, high = 0.0;
    for (int i = 0; i < NUM_BARS; i++) {
        float v = clamp(u_fft[i] * 6.0, 0.0, 1.0);
        if (i < NUM_BARS / 3) low += v;
        else if (i < 2 * NUM_BARS / 3) mid += v;
        else high += v;
    }
    low /= float(NUM_BARS / 3);
    mid /= float(NUM_BARS / 3);
    high /= float(NUM_BARS / 3);

    // --- Radiation pulse field ---
    float pulse = sin(u_time * 3.0 + radius * 40.0) * 0.5 + 0.5;
    float rings = sin(radius * 50.0 - u_time * 4.0) * 0.5 + 0.5;
    float decay = exp(-radius * 3.5);
    float radiation = (pulse + rings) * decay * (0.5 + 1.5 * low);

    // --- Electrical interference ---
    float n = fbm(st * 4.0 + u_time * 0.6);
    float flicker = sin(u_time * 20.0 + n * 10.0) * 0.5 + 0.5;
    float interference = smoothstep(0.45, 1.0, n * (0.6 + high * 2.0)) * flicker;

    // --- Color handling ---
    vec3 base = mix(vec3(0.1, 0.8, 0.1), vec3(1.0, 0.9, 0.3), mid);
    if (u_animateHue)
        base = hueShift(base, u_time * 0.7 + high * 2.0);

    // --- Radiation glow field ---
    float glow = exp(-pow(radius * 2.5, 1.5)) * (0.5 + 1.5 * low);
    vec3 glowColor = base * glow * (1.0 + flicker * 0.4);

    // --- Central radiation burn ---
    float core = exp(-pow(radius * 5.0, 2.0)) * (1.5 + mid * 2.0);
    vec3 coreColor = vec3(1.0, 1.0, 0.8) * core;

    // --- Outer radiation rings (hazard look) ---
    float band = smoothstep(0.01, 0.0, abs(fract(radius * 10.0 - u_time * 0.7) - 0.5));
    vec3 hazard = vec3(0.9, 1.0, 0.3) * band * high * 0.7;

    // --- Texture blend (distorted background) ---
    vec2 warp = st + vec2(fbm(st * 3.0 + u_time * 0.5)) * 0.04;
    vec3 tex = texture(u_texture, warp + 0.5).rgb * 0.5;

    // Combine
    vec3 color = tex + glowColor + radiation * base + interference * 0.6 + coreColor + hazard;

    // Slight overexposure rolloff
    color = color / (1.0 + color);
    color = pow(color, vec3(0.85));

    FragColor = vec4(color, 1.0);
}
