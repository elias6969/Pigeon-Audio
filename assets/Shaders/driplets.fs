#version 420 core

in vec2 v_uv;
out vec4 FragColor;

uniform float u_time;
uniform float u_bass;
uniform float u_mid;
uniform float u_treble;

uniform sampler2D u_sceneTex;
uniform vec2 u_blobs[64];
uniform int u_blobCount;

uniform vec2 u_resolution;
uniform int u_imagefitmode;

// color customization
uniform vec3 u_barColor;     // base goo color
uniform bool u_animateHue;   // animate hue toggle

#define DEBUG_BLOBS 0

//////////////////////////////////////////////////////
// Utility: hue shifting
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

//////////////////////////////////////////////////////
// Adjust image UV based on fit mode
vec2 getAdjustedUv(vec2 uv, sampler2D tex, vec2 res) {
    vec2 texSize = vec2(textureSize(tex, 0));
    float aspectTex = texSize.x / texSize.y;
    float aspectScreen = res.x / res.y;
    vec2 scale = vec2(1.0);

    if (u_imagefitmode == 0) {
        if (aspectTex > aspectScreen)
            scale.y = aspectScreen / aspectTex;
        else
            scale.x = aspectTex / aspectScreen;

        uv = (uv - 0.5) / scale + 0.5;

        if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
            discard;
    }
    return uv;
}

//////////////////////////////////////////////////////
// Blob rendering with wobble & fusion
float renderBlobs(vec2 uv, float time, out float id) {
    float combined = 0.0;
    id = -1.0;

    for (int i = 0; i < u_blobCount; ++i) {
        vec2 pos = u_blobs[i];
        float wobble = 0.01 * sin(uv.x * 40.0 + time * 4.0 + float(i) * 13.1) * u_treble;
        vec2 wobbledUV = uv + vec2(wobble, 0.0);

        float d = length(wobbledUV - pos);
        float r = 0.03 + 0.03 * u_bass;
        float blob = smoothstep(r, r - 0.01, d);

        // Fused blob blending
        combined += blob - combined * blob;

        if (blob > 0.01) id = float(i);
    }
    return combined;
}

//////////////////////////////////////////////////////
// Fresnel glow effect
float fresnel(vec2 uv, vec2 center, float edge) {
    float d = distance(uv, center);
    return pow(1.0 - clamp(d / edge, 0.0, 1.0), 3.0);
}

//////////////////////////////////////////////////////
// Main
void main() {
    vec2 uv = v_uv;
    vec2 center = vec2(0.5);
    float time = u_time;

    float blobID;
    float shape = renderBlobs(uv, time, blobID);
    if (shape <= 0.01) discard;

#if DEBUG_BLOBS
    float hue = mod(blobID / 64.0, 1.0);
    vec3 dbgColor = vec3(hue, 0.5 + 0.5 * sin(u_time), 1.0 - hue);
    FragColor = vec4(dbgColor, shape);
    return;
#endif

    // Refraction (goo lens distortion)
    vec2 direction = normalize(uv - center);
    vec2 refractUV = uv + direction * 0.02 * shape;
    vec2 adjUv = getAdjustedUv(v_uv, u_sceneTex, u_resolution);
    vec3 sceneColor = texture(u_sceneTex, adjUv).rgb;

    // Dynamic goo color
    vec3 baseColor = u_barColor;
    if (u_animateHue)
        baseColor = hueShift(baseColor, u_time * 0.5 + u_bass * 2.0);

    // Brighten when bass/mid are strong
    vec3 gooColor = mix(
        baseColor * 0.6,
        baseColor * (1.0 + u_mid * 0.8 + u_bass * 0.5),
        clamp(u_bass + u_mid, 0.0, 1.0)
    );

    // Edge glow (with treble intensity)
    float edge = fresnel(uv, center, 0.45) * u_treble * 0.9;
    vec3 glow = hueShift(vec3(0.6, 0.8, 1.0), u_time * 0.7) * edge;

    // Combine everything
    vec3 finalColor = mix(sceneColor, gooColor, 0.4);
    finalColor += glow;
    finalColor *= 0.9 + 0.1 * shape;

    FragColor = vec4(finalColor, shape);
}
