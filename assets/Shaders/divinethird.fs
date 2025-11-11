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

// ===================================================
// Utility: hue shift / YIQ
// ===================================================
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

// ===================================================
// Hash / noise / turbulence helpers
// ===================================================
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

// FFT helper
float getFFT(float i) {
    int idx = int(clamp(i, 0.0, float(NUM_BARS - 1)));
    return clamp(u_fft[idx] * 8.0, 0.0, 1.0);
}

// ===================================================
// Divine base geometry
// ===================================================
float petalPattern(vec2 p, float n) {
    float a = atan(p.y, p.x);
    float r = length(p);
    return abs(cos(a * n)) * exp(-r * 2.0);
}

float spiralField(vec2 p, float t) {
    float r = length(p);
    float a = atan(p.y, p.x);
    float spiral = sin(a * 8.0 + r * 8.0 - t * 4.0);
    return spiral * exp(-r * 1.5);
}

// ===================================================
// Core plasma — breathing of divine heart
// ===================================================
float plasma(vec2 p, float t) {
    float r = length(p);
    float n = noise(p * 4.0 + vec2(t * 0.3, -t * 0.5));
    float swirl = sin(r * 6.0 - t * 2.0 + n * 3.14);
    float waves = cos(p.x * 10.0 + sin(p.y * 6.0 + t * 1.5));
    return (swirl + waves) * 0.5 + 0.5;
}

// Layered turbulence for recursive feedback fields
float turbulence(vec2 p, float t) {
    float acc = 0.0;
    float scale = 1.0;
    for (int i = 0; i < 5; i++) {
        acc += abs(noise(p * scale + t * 0.3)) / scale;
        scale *= 2.0;
    }
    return acc;
}

// Temporal feedback wave
float timeRipple(vec2 p, float t, float amp) {
    float r = length(p);
    float wave = sin(r * 10.0 - t * 6.0 + amp * 3.0);
    return wave * exp(-r * 2.0);
}

// God fractal spiral — recursive divine geometry
float divineFractal(vec2 p, float t) {
    float sum = 0.0;
    float scale = 1.0;
    for (int i = 0; i < 4; i++) {
        sum += abs(sin(p.x * 6.0 * scale + t * 0.5)) *
               abs(cos(p.y * 7.0 * scale - t * 0.4)) / scale;
        p = p.yx * mat2(0.6, 0.8, -0.8, 0.6);
        scale *= 1.5;
    }
    return sum / 4.0;
}

// Polar interference field — holy distortion zone
float interference(vec2 p, float t) {
    float r = length(p);
    float a = atan(p.y, p.x);
    float wave = sin(a * 20.0 + r * 10.0 - t * 4.0);
    float inner = cos(a * 6.0 - r * 8.0 + t * 2.0);
    return (wave + inner) * 0.5;
}

// FFT-based dynamic distortion
vec2 divineDistort(vec2 p, float t) {
    float bass = getFFT(10.0);
    float mid = getFFT(70.0);
    float tre = getFFT(150.0);
    float freqPulse = (bass * 0.6 + mid * 0.3 + tre * 0.1);

    float d1 = turbulence(p * 1.5, t) * 0.02 * (1.0 + bass * 3.0);
    float d2 = sin(p.y * 10.0 + t * 2.0) * 0.02 * (1.0 + mid * 2.0);
    float d3 = cos(p.x * 12.0 - t * 2.5) * 0.02 * (1.0 + tre * 1.5);

    return p + vec2(d1 + d2, d3 - d2) * freqPulse;
}

// Fractured color modulation
vec3 spectralRadiance(vec2 p, float t) {
    float bass = getFFT(8.0);
    float mid = getFFT(60.0);
    float tre = getFFT(130.0);
    vec3 c1 = vec3(0.9, 0.4, 0.2) * bass;
    vec3 c2 = vec3(0.3, 0.9, 0.6) * mid;
    vec3 c3 = vec3(0.5, 0.7, 1.0) * tre;
    return (c1 + c2 + c3) * (0.6 + 0.4 * sin(t * 2.0));
}

// Cosmic bloom feedback — recursive mirror layers
vec3 bloomFeedback(vec2 p, float t) {
    vec3 col = vec3(0.0);
    float scale = 1.0;
    for (int i = 0; i < 5; i++) {
        float bass = getFFT(float(i * 30 + 10));
        float pulse = sin(t * 1.5 + bass * 5.0 + float(i)) * 0.5 + 0.5;
        vec2 q = p * scale + vec2(p.y, -p.x) * 0.3;
        float intensity = plasma(q + pulse * 0.2, t) * 0.8;
        col += hueShift(u_barColor, t * 0.3 + float(i) * 1.2) * intensity * (1.2 - bass);
        scale *= 1.4 + bass * 0.5;
        p = divineDistort(p * 0.8, t + bass * 2.0);
    }
    return col / 3.5;
}

// Subatomic grid — logic behind god’s mind
float divineCircuit(vec2 p, float t) {
    p *= 5.0;
    vec2 grid = abs(fract(p) - 0.5);
    float cell = min(grid.x, grid.y);
    float glow = exp(-cell * 25.0);
    float flicker = sin(t * 60.0 + p.x * 10.0) * 0.5 + 0.5;
    return glow * flicker;
}

// Astral noise interference — hyperdimensional randomness
vec3 astralField(vec2 p, float t) {
    float field = 0.0;
    float amp = 1.0;
    for (int i = 0; i < 3; i++) {
        field += plasma(p * amp * 1.5 + t * 0.2, t * 0.3) / amp;
        amp *= 2.1;
    }
    float flicker = 0.6 + 0.4 * sin(t * 3.0 + field * 2.0);
    return vec3(field * 0.4, field * 0.6, field) * flicker;
}

// Divine lightning arcs — chaotic revelation bursts
vec3 divineArcs(vec2 p, float t) {
    float r = length(p);
    float a = atan(p.y, p.x);
    float pulse = sin(a * 30.0 - r * 20.0 + t * 6.0) * exp(-r * 2.0);
    float energy = getFFT(30.0 + mod(a * 100.0, 160.0));
    vec3 arc = vec3(0.8, 0.9, 1.0) * pow(abs(pulse), 3.0) * (1.5 + energy * 2.0);
    return arc;
}

void main() {
    vec2 p = (uv - 0.5) * 2.0;
    p.x *= u_resolution.x / u_resolution.y;
    float t = u_time * 0.7;

    // Distort geometry and warp space
    vec2 dp = divineDistort(p, t);
    float r = length(dp);
    float a = atan(dp.y, dp.x);

    // Foundational fields
    float field = divineFractal(dp * 0.8, t);
    float inter = interference(dp, t);
    float rip = timeRipple(dp, t, field);
    float bass = getFFT(10.0);
    float mid = getFFT(70.0);
    float tre = getFFT(140.0);

    // Bloom of god consciousness
    vec3 feedback = bloomFeedback(dp, t);
    vec3 circuits = vec3(divineCircuit(dp, t)) * vec3(0.4, 0.7, 1.0);
    vec3 arcs = divineArcs(dp, t);
    vec3 astral = astralField(dp * 0.7 + feedback.xy, t);

    // Merge cosmic elements
    vec3 merge = feedback * 0.9 + astral * 0.8 + circuits * 0.5 + arcs;
    vec3 spectral = spectralRadiance(dp, t);
    vec3 base = hueShift(u_barColor, t + field * 2.0 + bass * 4.0);

    // Core divine form
    vec3 core = base * (field + rip * 0.6 + inter * 0.4);
    vec3 god = merge + core + feedback * (1.0 + bass * 0.5);

    // Depth warping with FFT-driven reality folds
    float fold = sin(t + r * 8.0 + mid * 5.0);
    god += hueShift(vec3(0.4, 0.8, 1.0), fold * 3.0) * pow(abs(fold), 2.0);

    // Spectral aura expansion
    float aura = exp(-r * 4.0) * (0.6 + 0.4 * sin(t * 3.0 + inter * 4.0));
    vec3 halo = vec3(0.5, 0.8, 1.0) * aura * (1.0 + tre * 2.0);
    god += halo;

    // Blend with texture as material memory
    vec3 tex = texture(u_texture, uv).rgb * 0.25;
    vec3 final = mix(tex, god, 0.85);

    // God’s final bloom
    final = pow(final, vec3(1.2));
    final += vec3(0.05, 0.1, 0.15) * log(1.0 + length(final));
    final = final / (1.0 + final);
    final = pow(final, vec3(0.95));

    FragColor = vec4(final, 1.0);
}
