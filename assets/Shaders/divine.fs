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

// =====================
// Utility & Color Magic
// =====================

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

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float n = mix(
        mix(hash(i + vec2(0.0, 0.0)), hash(i + vec2(1.0, 0.0)), u.x),
        mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x),
        u.y
    );
    return n;
}

float fbm(vec2 p) {
    float f = 0.0;
    float a = 0.5;
    for (int i = 0; i < 6; i++) {
        f += a * noise(p);
        p *= 2.1;
        a *= 0.55;
    }
    return f;
}

// =====================
// Core Divine Structure
// =====================

vec3 divineField(vec2 p, float t) {
    float n = fbm(p * 2.5 + vec2(t * 0.1, t * 0.07));
    float pulse = sin(t * 0.8) * 0.5 + 0.5;
    float warp = fbm(p * 3.0 + n * 4.0);
    float field = smoothstep(0.4, 1.0, warp * (0.6 + pulse * 0.6));
    vec3 c = mix(vec3(0.0, 0.2, 0.3), vec3(0.8, 0.9, 1.0), field);
    return c * (0.4 + pulse * 0.8);
}

vec3 radiance(vec2 uv, float intensity) {
    float r = length(uv - 0.5);
    float glow = exp(-r * 6.0) * intensity;
    return vec3(glow) * vec3(1.0, 0.95, 0.8);
}

// =====================
// Audio Influence
// =====================

float getBandEnergy(int idx) {
    return clamp(u_fft[idx] * 8.0, 0.0, 1.0);
}

float globalEnergy() {
    float e = 0.0;
    for (int i = 0; i < NUM_BARS; ++i)
        e += u_fft[i];
    return e / float(NUM_BARS);
}

// =====================
// The Vision
// =====================

void main() {
    vec2 st = uv;
    vec2 aspect = vec2(u_resolution.x / u_resolution.y, 1.0);
    vec2 pos = (st - 0.5) * aspect;
    float time = u_time * 0.6;
    float global = globalEnergy();

    // Central Divine Core
    float corePulse = sin(time * 2.0) * 0.5 + 0.5;
    float core = exp(-dot(pos, pos) * 8.0) * (0.8 + corePulse * 0.6 + global * 2.0);

    // Radiating Energy Waves
    float wave = sin(20.0 * length(pos) - time * 4.0 + corePulse * 3.14);
    float halo = smoothstep(0.0, 1.0, 1.0 - abs(wave) * 1.5) * global * 2.0;

    // Bar-Like Frequency Threads (Divine DNA)
    float barWidth = 1.0 / float(NUM_BARS);
    int idx = int(st.x / barWidth);
    float energy = getBandEnergy(idx);
    float dna = sin(st.y * 40.0 + sin(float(idx) * 0.5 + time * 2.0)) * energy;
    dna *= exp(-abs(st.y - 0.5) * 8.0);

    // Divine Fractal Field
    vec3 field = divineField(pos * (2.0 + global * 2.5), time);
    vec3 baseColor = hueShift(u_barColor, time * 0.4 + global * 2.0);

    // Interference Pattern of Thought
    float interference = sin((pos.x * 8.0 + fbm(pos * 3.0 + time * 0.2)) * 3.0);
    interference *= sin(pos.y * 5.0 + fbm(pos * 4.0 - time * 0.3) * 2.0);
    float mindwave = smoothstep(-0.3, 0.8, interference + global * 0.5);

    // Color Weaving
    vec3 godLight = baseColor * (core + halo + dna);
    vec3 neuro = field * mindwave * 1.3;
    vec3 textureInfluence = texture(u_texture, uv * (1.0 + global * 0.1)).rgb * 0.5;
    vec3 vision = godLight + neuro + textureInfluence;

    // Divine Overexposure Control
    vision = pow(vision / (1.0 + vision), vec3(0.95));

    // Add Radiant Aura
    vision += radiance(uv, global * 1.5);

    // Quantum Sparkle
    float spark = fract(sin(dot(uv * u_resolution, vec2(91.7, 73.3))) * 543.1 + time * 30.0);
    spark = smoothstep(0.95, 1.0, spark) * (0.2 + global * 0.8);
    vision += vec3(1.0, 0.9, 0.8) * spark;

    FragColor = vec4(vision, 1.0);
}
