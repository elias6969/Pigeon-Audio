#version 420
layout(location = 0) in vec2 aPos;
out vec2 uv;
uniform mat4 u_projection;

void main()
{
  uv = aPos * 0.5 + 0.5;
  gl_Position = u_projection * vec4(aPos, 0.0, 1.0);
}
