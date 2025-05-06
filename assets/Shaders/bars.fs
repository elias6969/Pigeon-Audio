#version 420 core
in vec2 uv;
out vec4 FragColor;
#define NUM_BARS 200
layout(std140, binding = 0) uniform FFTBlock {
  float u_fft[200];
};
uniform float u_time;
uniform sampler2D u_texture;

const float PI = 3.14159265359;

void main(){
    vec2 center = vec2(0.5);
    vec2 coord  = uv - center;
    float dist  = length(coord);

    float baseRadius = 0.3;
    // inside circle: sample image
    if(dist <= baseRadius) {
        FragColor = texture(u_texture, uv);
        return;
    }

    // radial bar setup
    float sector = 2.0*PI/float(NUM_BARS);
    float angle  = atan(coord.y, coord.x);
    if(angle<0.0) angle += 2.0*PI;
    float idx    = floor(angle/sector);
    float barCenter = (idx+0.5)*sector;
    float angOffset = abs(angle - barCenter);

    // angular anti-alias
    float barWidth = sector * 0.7;
    float aaAng    = fwidth(angOffset);
    float angularMask = smoothstep(barWidth*0.5 + aaAng,
                                   barWidth*0.5 - aaAng,
                                   angOffset);

    // radial height from smoothed FFT
    int band = int(idx);
    float value = clamp(u_fft[band]*10.0, 0.0, 1.0);
    float maxLen = 0.05 + value * 0.3;

    // radial anti-alias
    float aaRad = 0.005;
    float innerMask = smoothstep(baseRadius, baseRadius + aaRad, dist);
    float outerMask = smoothstep(baseRadius + maxLen,
                                 baseRadius + maxLen - aaRad,
                                 dist);

    // combine masks
    float mask = angularMask * innerMask * outerMask;
    if(mask < 0.01) discard;

    // color & output
    vec3 color = mix(vec3(0.2,0.6,1.0)*value,
                     texture(u_texture, uv).rgb,
                     0.09);
    FragColor = vec4(color, mask);
}
