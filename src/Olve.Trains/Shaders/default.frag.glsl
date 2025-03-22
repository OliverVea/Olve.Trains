#version 330 core

in vec3 FragPos;
in vec3 FragNormal;
in float DistanceToMouseRay;

out vec4 FragColor;

uniform vec3 ambientLightColor;
uniform float ambientIntensity;

uniform vec3 directionalLightColor;
uniform vec3 directionalLightDir;
uniform float directionalIntensity;

void main()
{
    // Normalize the normal and light direction
    vec3 norm = normalize(FragNormal);
    vec3 lightDir = normalize(-directionalLightDir); // Ensure it's pointing towards the fragment

    // Ambient lighting
    vec3 ambient = ambientLightColor * ambientIntensity;

    // Diffuse lighting
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * directionalLightColor * directionalIntensity;

    // Specular lighting (Blinn-Phong)
    // Assume the camera is at (0,0,0)
    vec3 viewDir = normalize(-FragPos);
    vec3 halfDir = normalize(lightDir + viewDir);
    // Hard-coded shininess factor of 32.0 gives a moderate highlight
    float spec = pow(max(dot(norm, halfDir), 0.0), 32.0);
    // Modulate the specular highlight by DistanceToMouseRay so that fragments near the mouse ray glow more.
    spec *= (1.0 - clamp(DistanceToMouseRay, 0.0, 1.0));
    vec3 specular = spec * directionalLightColor * directionalIntensity;

    // Combine all lighting components
    vec3 lighting = ambient + diffuse + specular;

    FragColor = vec4(lighting, 1.0);
}