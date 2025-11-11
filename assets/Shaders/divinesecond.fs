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

// ======================================================
// Divine Foundation — energy + spectrum base
// ======================================================

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

// Hash + noise for ethereal motion
float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Spectrum analysis mapping
float getFFT(float i) {
    int idx = int(clamp(i, 0.0, float(NUM_BARS - 1)));
    return clamp(u_fft[idx] * 8.0, 0.0, 1.0);
}

// ======================================================
// Geometry foundation — “pulse field” and bar core
// ======================================================
float polarBars(vec2 p, float time) {
    float angle = atan(p.y, p.x);
    float radius = length(p);
    float idx = mod(angle / (2.0 * PI) * float(NUM_BARS), float(NUM_BARS));
    float val = getFFT(idx);
    float height = 0.25 + val * 0.6;
    float band = smoothstep(height, height - 0.03, radius);
    float pulse = 0.8 + 0.2 * sin(time * 4.0 + idx * 0.2);
    return band * pulse * (0.4 + 0.6 * val);
}

// ======================================================
// Divine color gradients
// ======================================================
vec3 baseRadiance(float a, float r, float time) {
    vec3 color = vec3(0.0);
    color.r = 0.6 + 0.4 * sin(a * 4.0 + time * 0.9);
    color.g = 0.6 + 0.4 * cos(a * 3.0 - time * 1.3);
    color.b = 0.5 + 0.5 * sin(a * 6.0 + r * 2.5 + time * 0.6);
    return color;
}

// ======================================================
// Part 2 – Ethereal Fields and Divine Motion Distortion
// ======================================================

// --- radial plasma ---
float plasma(vec2 p, float t) {
    return sin(p.x * 8.0 + sin(p.y * 4.0 + t * 1.2)) *
           cos(p.y * 7.0 + cos(p.x * 5.0 - t * 1.1));
}

// --- fractal light warp ---
float fractalLayer(vec2 p, float time) {
    float acc = 0.0;
    float scale = 1.0;
    for (int i = 0; i < 4; ++i) {
        acc += plasma(p * scale, time * scale) / scale;
        scale *= 2.1;
    }
    return acc / 4.0;
}

// --- dynamic halos based on frequency bands ---
vec3 halo(vec2 p, float time) {
    float radius = length(p);
    float bass = getFFT(10.0);
    float mid = getFFT(60.0);
    float treble = getFFT(160.0);

    float pulse = sin(time * 2.5 + bass * 5.0) * 0.5 + 0.5;
    float glow = exp(-pow(radius - (0.4 + bass * 0.4), 2.0) * 20.0);

    vec3 color = vec3(0.2 + bass * 0.8, 0.3 + mid * 0.6, 0.9 + treble * 0.3);
    color *= glow * (1.2 + pulse);
    return color;
}

// --- spectral fog (slow–moving cosmic dust) ---
vec3 spectralFog(vec2 uv, float time) {
    vec2 q = uv * 2.0 - 1.0;
    q.x *= u_resolution.x / u_resolution.y;
    float n = fractalLayer(q * 0.8 + time * 0.1, time * 0.3);
    vec3 baseFog = vec3(0.05, 0.1, 0.12) + n * 0.4;
    baseFog *= 0.5 + 0.5 * sin(time * 0.7 + n * 3.14);
    return baseFog;
}

// --- motion distortion, gives “liquid” effect ---
vec2 motionDistort(vec2 p, float time) {
    float n1 = noise(p * 3.0 + time * 0.5);
    float n2 = noise(p * 5.0 - time * 0.7);
    return p + vec2(n1, n2) * 0.03;
}

// ======================================================
// Divine extension of main()
// ======================================================
void main() {
    vec2 p = (uv - 0.5) * 2.0;
    p.x *= u_resolution.x / u_resolution.y;
    float time = u_time * 0.6;

    // base distortion for dream-motion
    vec2 dp = motionDistort(p, time);
    float radius = length(dp);
    float angle = atan(dp.y, dp.x);

    // primary energy
    float energy = polarBars(dp, time);
    vec3 radiance = baseRadiance(angle, radius, time);
    vec3 coreColor = mix(vec3(0.05), u_barColor, energy);

    if (u_animateHue)
        coreColor = hueShift(coreColor, time + radius * 2.0 + angle * 0.5);

    // combine with halos, fog, and fractal interference
    vec3 divineHalo = halo(dp, time);
    vec3 fog = spectralFog(uv, time);
    //vec3 field = fractalLayer(dp * 1.2 + fog.xy, time);

    float field = fractalLayer(dp * 1.2 + fog.xy, time);
    vec3 plasmaColor = vec3(0.4, 0.7, 1.0) * field * (0.6 + energy * 0.8);
    //vec3 plasmaColor = vec3(0.4, 0.7, 1.0) * field * (0.6 + energy * 0.8);
    vec3 merged = coreColor * (1.0 + energy) + divineHalo + plasmaColor + fog * 0.4;

    // texture overlay for grounding
    vec3 tex = texture(u_texture, uv).rgb * 0.3;
    vec3 final = mix(tex, merged, 0.7);

    // tone-map & soft bloom
    final = final / (1.0 + final);
    final = pow(final, vec3(0.92));

    FragColor = vec4(final, 1.0);
}
