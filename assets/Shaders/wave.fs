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
// Hue rotation utility
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
// Noise function (for wobble and texture)
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

// ---------------------------
// Main
// ---------------------------
void main() {
    vec2 st = uv;
    st.x *= u_resolution.x / u_resolution.y;

    // Centered coordinate system
    vec2 p = st - 0.5;

    // --- Audio-driven parameters ---
    int idxA = int(mod(floor(abs(p.y) * float(NUM_BARS)), NUM_BARS));
    int idxB = int(mod(idxA + 50, NUM_BARS)); // second wave offset
    float ampA = clamp(u_fft[idxA] * 10.0, 0.0, 1.0);
    float ampB = clamp(u_fft[idxB] * 10.0, 0.0, 1.0);

    // --- Double helix paths ---
    float waveA = sin(p.y * 8.0 + u_time * 2.0) * (0.25 + ampA * 0.25);
    float waveB = -sin(p.y * 8.0 + u_time * 2.0 + PI) * (0.25 + ampB * 0.25);

    // --- Ladder connection (middle glow) ---
    float connect = exp(-pow(p.x, 2.0) * 30.0) * 0.3;

    // --- DNA strand thickness ---
    float distA = abs(p.x - waveA);
    float distB = abs(p.x - waveB);
    float strandA = exp(-pow(distA * 18.0, 2.0));
    float strandB = exp(-pow(distB * 18.0, 2.0));

    // --- Trippy color mapping ---
    float phase = sin(u_time + p.y * 3.0) * 0.5 + 0.5;
    vec3 base = u_barColor;
    if (u_animateHue)
        base = hueShift(base, u_time * 0.6 + p.y * 3.0);

    vec3 colA = base * (0.6 + 0.4 * ampA) * vec3(1.0, 0.5 + phase * 0.5, 1.0);
    vec3 colB = hueShift(base, PI / 3.0) * (0.6 + 0.4 * ampB) * vec3(1.0, 1.0, 0.7);

    // Combine DNA strands and ladder
    vec3 dna = colA * strandA + colB * strandB;
    dna += connect * (colA + colB) * 0.5;

    // --- Subtle flowing distortion ---
    float n = noise(st * 3.0 + u_time * 0.4) * 0.04;
    float wobble = sin(p.y * 10.0 + u_time * 1.5 + n * 20.0) * 0.02;
    dna += wobble * base * 0.4;

    // --- Texture background blend ---
    vec3 tex = texture(u_texture, uv + n * 0.1).rgb * 0.5;
    tex = pow(tex, vec3(1.3));
    vec3 combined = tex + dna * 1.4;

    // --- Glow + tone map ---
    float glow = exp(-pow(length(p) * 1.8, 2.0)) * (ampA + ampB);
    combined += glow * base * 0.8;

    combined = combined / (1.0 + combined);
    combined = pow(combined, vec3(0.9));

    FragColor = vec4(combined, 1.0);
}
