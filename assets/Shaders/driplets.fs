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

// Enable this to see debug color per blob ID
#define DEBUG_BLOBS 0

//////////////////////////////////////////////////////
// Renders all blobs with fusion and wobble

float renderBlobs(vec2 uv, float time, out float id) {
    float combined = 0.0;
    id = -1.0;

    for (int i = 0; i < u_blobCount; ++i) {
        vec2 pos = u_blobs[i];

        // Optional wobble
        float wobble = 0.01 * sin(uv.x * 40.0 + time * 4.0 + float(i) * 13.1) * u_treble;
        vec2 wobbledUV = uv + vec2(wobble, 0.0);

        float d = length(wobbledUV - pos);
        float r = 0.07 + 0.03 * u_bass;
        float blob = smoothstep(r, r - 0.01, d);

        // FUSION time!
        combined += blob - combined * blob;

        // Still record one for debug (if needed)
        if (blob > 0.01) id = float(i);
    }

    return combined;
}


//////////////////////////////////////////////////////
// Fresnel-style edge glow
float fresnel(vec2 uv, vec2 center, float edge) {
    float d = distance(uv, center);
    return pow(1.0 - clamp(d / edge, 0.0, 1.0), 3.0);
}

void main() {
    vec2 uv = v_uv;
    vec2 center = vec2(0.5);
    float time = u_time;

    float blobID;
    float shape = renderBlobs(uv, time, blobID);
    if (shape <= 0.01) discard;

#if DEBUG_BLOBS
    // Each blob has a unique hue – good for debugging
    float hue = mod(blobID / 64.0, 1.0);
    vec3 dbgColor = vec3(hue, 0.5 + 0.5 * sin(u_time), 1.0 - hue);
    FragColor = vec4(dbgColor, shape);
    return;
#endif

    // Refraction effect (pulls pixels inward)
    vec2 direction = normalize(uv - center);
    vec2 refractUV = uv + direction * 0.02 * shape;
    vec3 sceneColor = texture(u_sceneTex, refractUV).rgb;

    // Goo base color (reactive)
    vec3 gooColor = mix(
        vec3(0.2, 0.3, 0.8), // idle
        vec3(1.0, 0.4 + u_mid, 0.6), // energized
        clamp(u_bass + u_mid, 0.0, 1.0)
    );

    // Edge glow (more prominent with treble)
    float edge = fresnel(uv, center, 0.45) * u_treble * 0.9;
    vec3 glow = vec3(0.6, 0.8, 1.0) * edge;

    // Composite
    vec3 finalColor = mix(sceneColor, gooColor, 0.4);
    finalColor += glow;
    finalColor *= 0.9 + 0.1 * shape;

    FragColor = vec4(finalColor, shape);
}
