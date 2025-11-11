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
// Glow function (balanced intensity)
// ---------------------------
vec3 balancedGlow(vec3 base, float distance, float intensity, float radius) {
    float glow = exp(-pow(distance / radius, 2.0)) * intensity;
    float brightness = dot(base, vec3(0.299, 0.587, 0.114));
    float dimFactor = mix(1.5, 0.4, brightness); // brighter base = weaker glow
    return base * glow * dimFactor;
}

// ---------------------------
// Main bar rendering
// ---------------------------
vec3 renderBars(vec2 uv, float time) {
    float barCount = float(NUM_BARS);
    float barWidth = 1.0 / barCount;
    int idx = int(uv.x / barWidth);
    if (idx < 0 || idx >= NUM_BARS) discard;

    float value = clamp(u_fft[idx] * 8.0, 0.0, 1.0);
    float barHeight = value * 0.85 + 0.05;
    float offsetY = uv.y - sin(time * 1.5 + float(idx) * 0.15) * 0.02;

    // color handling
    vec3 base = u_barColor;
    if (u_animateHue)
        base = hueShift(base, time * 0.6 + float(idx) * 0.2);

    float gradient = smoothstep(barHeight, barHeight - 0.015, offsetY);
    float pulse = 0.8 + 0.2 * sin(time * 4.0 + float(idx) * 0.4);
    vec3 color = base * pulse * gradient;

    // glow layer (non-blinding)
    float glowDist = abs(barHeight - uv.y);
    vec3 glow = balancedGlow(base, glowDist, 1.5, 0.08);

    // reflection effect (under bars)
    float refl = smoothstep(0.0, -0.3, uv.y - (barHeight - 0.4));
    vec3 reflection = base * pow(refl, 2.0) * 0.3;

    return color + glow + reflection;
}

// ---------------------------
// Tone mapping for exposure control
// ---------------------------
vec3 toneMap(vec3 col) {
    // Reinhard tone mapping (prevents white blowout)
    col = col / (1.0 + col);
    // Slight gamma lift for soft glow look
    return pow(col, vec3(0.9));
}

// ---------------------------
// Background
// ---------------------------
vec3 background(vec2 uv, float time) {
    vec3 tex = texture(u_texture, uv).rgb;
    tex *= 0.5 + 0.5 * sin(time * 0.5 + uv.x * 4.0);
    tex = pow(tex, vec3(1.2)); // slightly darken for bloom contrast
    return tex;
}

// ---------------------------
// Main
// ---------------------------
void main() {
    vec3 bg = background(uv, u_time);
    vec3 bars = renderBars(uv, u_time);

    // Combine layers
    vec3 combined = bg * 0.7 + bars;

    // Apply tone mapping
    vec3 finalColor = toneMap(combined);

    FragColor = vec4(finalColor, 1.0);
}
