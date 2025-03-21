#version 330 core

in vec3 FragPos;
out vec4 FragColor;

uniform float3 ambientLightColor;
uniform float ambientLightStrength;

uniform float3 directionalLightColor;
uniform float3 directionalLightDirection;

uniform float lowestHeight;
uniform float3 lowestGrassColor;
uniform float highestHeight;
uniform float3 highestGrassColor;

void main()
{
    float height = FragPos.y;
    float3 grassColor = mix(lowestGrassColor, highestGrassColor, (height - lowestHeight) / (highestHeight - lowestHeight));
    
    float ambientStrength = ambientLightStrength;
    float3 ambient = ambientStrength * ambientLightColor;
    
    float diff = max(dot(normalize(FragPos), directionalLightDirection), 0.0);
    float3 diffuse = diff * directionalLightColor;
    
    FragColor = vec4((ambient + diffuse) * grassColor, 1.0);
}
