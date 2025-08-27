#version 330 core

in vec3 GS_FragPos;
flat in vec3 FragNormal;
flat in int isNormalLine; // 1 if normal line, 0 if edge line
in float gsDistanceToMouse;

uniform float mouseRadius;

out vec4 FragColor;

void main()
{
    if (gsDistanceToMouse > mouseRadius)
    {
        discard;
    }

    float t = clamp(1.0 - gsDistanceToMouse / mouseRadius, 0.0, 1.0);
    float aMouse = smoothstep(0.0, 1.0, t);

    if (isNormalLine == 1)
    {
        FragColor = vec4(0.0, 0.0, 1.0, 1.0);  // blue
    }
    else
    {
        FragColor = vec4(1.0, 1.0, 1.0, aMouse);  // white
    }
}
