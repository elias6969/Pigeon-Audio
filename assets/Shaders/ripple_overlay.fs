#version 330 core
in vec2 uv;
out vec4 FragColor;

uniform sampler2D u_texture;
#define NUM_BARS 200
uniform float u_fft[NUM_BARS];
uniform float     u_time;

const float PI = 3.14159265359;

void main(){
    vec2 center = vec2(0.5);
    vec2 coord  = uv - center;
    float dist  = length(coord);

    // — Main circle (image) mask & sample —
    float baseRadius = 0.3;
    if(dist <= baseRadius) {
        vec3 col = texture(u_texture, uv).rgb;
        FragColor = vec4(col, 1.0);
        return;
    }

    // — Radial bars outside the circle —
    // Number of bars around the ring
    float sector = 2.0 * PI / float(NUM_BARS);

    // Compute angle in [0,2PI)
    float angle = atan(coord.y, coord.x);
    if(angle < 0.0) angle += 2.0 * PI;

    // Which bar are we in?
    float idx        = floor(angle / sector);
    float barCenter  = (idx + 0.5) * sector;
    float angOffset  = abs(angle - barCenter);

    // Bar angular width (tweak for thinner/thicker bars)
    float barWidth = sector * 0.6;

    // Only draw within that angular slice
    if(angOffset > barWidth * 0.5) {
        discard;
    }

    // Compute bar length based on amplitude
    int band = int(mod(idx, NUM_BARS)); // wrap if NUM_BARS < actual bar count
    float value = clamp(u_fft[band] * 15.0, 0.0, 1.0); // scale FFT energy
    float maxLen = 0.01 + value * 0.3;

    
    // Position along radial line
    if(dist < baseRadius || dist > baseRadius + maxLen) {
        discard;
    }

    // Smooth step on radial edges
    float innerEdge = smoothstep(baseRadius,       baseRadius + 0.01, dist);
    float outerEdge = smoothstep(baseRadius+maxLen, baseRadius+maxLen-0.01, dist);
    float mask      = min(innerEdge, outerEdge);

    // Bar color: blue fading outwards
    float t = float(band) / float(NUM_BARS);
    vec3 baseCol = vec3(sin(t * 3.1415), sin(t * 6.28), cos(t * 3.1415));
    vec3 barCol = baseCol * mask * value;

    FragColor = vec4(barCol, mask);
}
