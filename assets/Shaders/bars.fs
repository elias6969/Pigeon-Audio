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

// new uniforms
uniform vec3 u_barColor;
uniform bool u_animateHue;

const float PI = 3.14159265359;

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

void main() {
    vec2 center = vec2(0.5);
    vec2 coord = uv - center;

    // maintain circular proportions
    coord.x *= u_resolution.x / u_resolution.y;
    float dist = length(coord);

    float baseRadius = 0.3;

    // inside circle → show image
    if (dist <= baseRadius) {
        FragColor = texture(u_texture, uv);
        return;
    }

    // bar setup
    float sector = 2.0 * PI / float(NUM_BARS);
    float angle = atan(coord.y, coord.x);
    if (angle < 0.0) angle += 2.0 * PI;

    float idx = floor(angle / sector);
    float barCenter = (idx + 0.5) * sector;
    float angOffset = abs(angle - barCenter);

    float barWidth = sector * 0.7;
    float aaAng = fwidth(angOffset);
    float angularMask = smoothstep(barWidth * 0.5 + aaAng,
                                   barWidth * 0.5 - aaAng,
                                   angOffset);

    int band = int(idx);
    float value = clamp(u_fft[band] * 10.0, 0.0, 1.0);
    float maxLen = 0.05 + value * 0.35;

    float aaRad = 0.005;
    float innerMask = smoothstep(baseRadius, baseRadius + aaRad, dist);
    float outerMask = smoothstep(baseRadius + maxLen,
                                 baseRadius + maxLen - aaRad,
                                 dist);

    float mask = angularMask * innerMask * outerMask;
    if (mask < 0.01) discard;

    // dynamic color
    vec3 baseColor = u_barColor;
    if (u_animateHue)
        baseColor = hueShift(baseColor, u_time * 0.6 + float(band) * 0.05);

    vec3 glow = baseColor * (0.4 + 0.6 * value);

    float fade = smoothstep(1.0, 0.2, dist);
    vec3 texColor = texture(u_texture, uv).rgb;
    vec3 finalColor = mix(glow, texColor, 0.07) * fade;

    float glowMask = smoothstep(baseRadius + maxLen + 0.01,
                                baseRadius + maxLen - 0.03,
                                dist);
    finalColor += baseColor * glowMask * 0.35;

    FragColor = vec4(finalColor, mask);
}
