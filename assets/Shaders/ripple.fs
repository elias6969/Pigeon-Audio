#version 330 core
in vec2 uv;
out vec4 FragColor;

uniform sampler2D u_texture;   //my image
uniform float u_time;          //our time from glfwgettime or something
uniform float u_amplitude;    //our amplitude

void main(){
    //Basic statements
    vec2 center = vec2(0.5); 
    vec2 coord = uv - center;            
    float dist = length(coord);          

    float speed    = 0.5; //the speed of the wave?                       
    float frequency= 20.0;                       //the frequency of it happening
    float magnitude= 0.01 + u_amplitude * 0.05;  //the magnitude? idk why

    float wave = sin((dist * frequency) - (u_time * speed)); //our wave

    float offset = wave * magnitude; //our offset? but why do we need an offset? what even is an offset
    vec2 rippleUV = uv + normalize(coord) * offset; 

    vec4 col = texture(u_texture, rippleUV); //our final of what we want to output

    float radius = 0.4;
    if (dist > radius) discard;

    FragColor = col;// the final output
}
