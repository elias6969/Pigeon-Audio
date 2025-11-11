#version 420 core

in vec2 uv;
out vec4 FragColor;

uniform float u_time;
uniform sampler2D u_texture;
uniform int u_imagefitmode;   // 0 = center, 1 = stretch
uniform vec2 u_resolution;    // window size
uniform vec3 u_barColor;      // base bar color (from ImGui)
uniform bool u_animateHue;    // animate hue toggle

const float PI = 3.14159;
#define NUM_BARS 64

layout(std140, binding = 0) uniform FFTBlock {
    float u_fft[NUM_BARS];
};

// ----------------------------
// --- Helper Functions ---
// ----------------------------

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

vec2 getAdjustedUv(vec2 uv, sampler2D tex, vec2 res) {
    vec2 texSize = vec2(textureSize(tex, 0));
    float aspectTex = texSize.x / texSize.y;
    float aspectScreen = res.x / res.y;

    vec2 scale = vec2(1.0);

    if (u_imagefitmode == 0) {
        if (aspectTex > aspectScreen) {
            // image is wider → show full width, trim height
            scale.y = aspectScreen / aspectTex;
        } else {
            // image is taller → show full height, trim width
            scale.x = aspectTex / aspectScreen;
        }

        uv = (uv - 0.5) / scale + 0.5;

        // discard anything outside the valid region to avoid edge smear
        if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
            discard;
    }

    return uv;
}

// ----------------------------
// --- Bar Visualizer ---
// ----------------------------
vec3 getBarColor(vec2 uv, float time, sampler2D tex, vec2 res) {
    vec2 adjUv = getAdjustedUv(uv, tex, res);

    float barWidth = 1.0 / float(NUM_BARS);
    int index = int(adjUv.x / barWidth);
    if (index >= NUM_BARS) discard;

    float value = clamp(u_fft[index] * 10.0, 0.0, 1.0);
    float barHeight = value;

    float edgeFade = smoothstep(barHeight, barHeight - 0.08, uv.y);
    float pulse = sin(time + float(index) * 0.5) * 0.1 + 0.9;

    // --- Color handling ---
    vec3 baseColor = u_barColor;
    if (u_animateHue)
        baseColor = hueShift(baseColor, time * 0.6 + float(index) * 0.15);

    vec3 barColor = baseColor * (0.5 + 0.5 * uv.y / max(barHeight, 0.001));
    barColor *= pulse * edgeFade;

    vec3 bg = texture(tex, adjUv).rgb * 0.3;
    return (uv.y < barHeight) ? barColor + bg : bg;
}

// ----------------------------
// --- Main ---
// ----------------------------
void main() {
    vec3 color = getBarColor(uv, u_time, u_texture, u_resolution);
    FragColor = vec4(color, 1.0);
}
