#version 330 core

in vec3 FragPos;
in vec3 FragNormal;
in vec2 TexCoords;

out vec4 FragColor;

uniform vec3 ambientLightColor;
uniform float ambientLightIntensity;

uniform vec3 directionalLightColor;
uniform vec3 directionalLightDir;
uniform float directionalIntensity;

uniform sampler2D textureSampler;

void main()
{
    vec3 norm = normalize(FragNormal);
    vec3 lightDir = normalize(-directionalLightDir);

    // Compute screen-space derivatives for edge detection
    vec3 dx = dFdx(norm);
    vec3 dy = dFdy(norm);
    float edgeFactor = length(dx) + length(dy);
    float smoothing = smoothstep(0.2, 0.5, edgeFactor);

    // Calculate diffuse component with edge smoothing applied
    float diff = max(dot(norm, lightDir), 0.0) * (1.0 - smoothing);
    vec3 diffuse = diff * directionalLightColor * directionalIntensity;

    // Ambient component
    vec3 ambient = ambientLightColor * ambientLightIntensity;

    // Sample the texture color using texture coordinates
    vec4 textureColor = texture(textureSampler, TexCoords);
    //vec4 textureColor = vec4(TexCoords.xy, 0, 1.0);

    // Combine texture with the lighting calculations
    vec3 finalColor = (ambient + diffuse) * textureColor.rgb;
    FragColor = vec4(finalColor, textureColor.a);
}
