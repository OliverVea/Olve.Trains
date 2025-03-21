#version 330 core

in vec3 FragPos;
out vec4 FragColor;

void main()
{
    float height = FragPos.y;
    FragColor = vec4(1.0, 0.5, 0.2, 1.0) * (height + 0.5);
}
