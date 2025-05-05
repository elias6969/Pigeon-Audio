#version 330 core
in vec2 uv;
out vec4 FragColor;

uniform float u_amplitude;
uniform sampler2D u_texture;

void main() {
    vec2 center = vec2(0.5);
    float dist = distance(uv, center);

    // — PARAMETERS you can tweak —
    float radius    = 0.3 + u_amplitude * 0.5;    // circle radius
    float edgeSoft  = 0.05;                      // softness of the circle edge
    float glowWidth = 0.03 + u_amplitude * 0.05; // how far the glow reaches

    // — INTERIOR MASK — 1.0 inside, 0.0 outside, soft edge
    float interior = smoothstep(radius, radius - edgeSoft, dist);

    // — OUTER GLOW MASK — 1.0 at the circle edge, 0.0 at radius+glowWidth
    float glow = smoothstep(radius + glowWidth, radius, dist);

    // — COMBINED ALPHA — if both are zero, we throw it away
    float alpha = max(interior, glow);
    if (alpha <= 0.0) discard;

    // — SAMPLE YOUR IMAGE (only inside) —
    vec3 tex = texture(u_texture, uv).rgb;

    // — PULSATING SATURATION —
    float sat = 0.5 + u_amplitude * 0.5;
    vec3 gray = vec3(dot(tex, vec3(0.299, 0.587, 0.114)));
    vec3 colorInt = mix(gray, tex, sat) * interior;

    // — BLUE GLOW OUTSIDE THE CIRCLE —
    vec3 glowCol = vec3(0.0, 0.5, 1.0) * glow * u_amplitude;

    // — FINAL COMPOSITE —
    vec3 finalColor = colorInt + glowCol;
    FragColor = vec4(finalColor, alpha);
}
