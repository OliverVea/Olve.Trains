#version 330 core

in vec3 GS_FragPos;
flat in vec3 FragNormal;
flat in int isNormalLine; // 1 if normal line, 0 if edge line

uniform vec3 ambientLightColor;
uniform float ambientLightIntensity;

uniform vec3 directionalLightColor;
uniform vec3 directionalLightDir;
uniform float directionalIntensity;

out vec4 FragColor;

void main()
{
    // For normal lines, use a fixed color.
    if(isNormalLine == 1)
    {
        FragColor = vec4(1.0, 0.0, 0.0, 1.0);  // red
    }
    else
    {
        FragColor = vec4(0.0, 0.0, 0.0, 1.0);  // black
    }
}
