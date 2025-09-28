#version 330 core

in VS_OUT {
    vec2 local01;
    vec4 color;
} fs_in;

out vec4 fragColor;

void main()
{
    fragColor = fs_in.color;
}
