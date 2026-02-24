#version 330 core

in vec3 FragPos;
in vec3 FragNormal;

out vec4 FragColor;

uniform vec3 ambientLightColor;
uniform float ambientLightIntensity;

uniform vec3 directionalLight0Color;
uniform vec3 directionalLight0Dir;
uniform float directionalLight0Intensity;

uniform vec3 directionalLight1Color;
uniform vec3 directionalLight1Dir;
uniform float directionalLight1Intensity;

uniform vec3 uColor;

void main()
{
    vec3 norm = normalize(FragNormal);

    // Directional light 0 (sun)
    vec3 lightDir0 = normalize(-directionalLight0Dir);
    float diff0 = max(dot(norm, lightDir0), 0.0);
    vec3 diffuse0 = diff0 * directionalLight0Color * directionalLight0Intensity;

    // Directional light 1 (moon)
    vec3 lightDir1 = normalize(-directionalLight1Dir);
    float diff1 = max(dot(norm, lightDir1), 0.0);
    vec3 diffuse1 = diff1 * directionalLight1Color * directionalLight1Intensity;

    // Ambient component
    vec3 ambient = ambientLightColor * ambientLightIntensity;

    // Combine lighting with rail color
    vec3 finalColor = (ambient + diffuse0 + diffuse1) * uColor;
    FragColor = vec4(finalColor, 1.0);
}
