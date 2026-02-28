#version 330 core

in vec3 FragPos;
in vec3 FragNormal;
in vec2 TexCoords;

out vec4 FragColor;

// @implements(IDaylightShader.AmbientLightColor)
uniform vec3 ambientLightColor;
// @implements(IDaylightShader.AmbientLightIntensity)
uniform float ambientLightIntensity;

// @implements(IDaylightShader.DirectionalLight0Color)
uniform vec3 directionalLight0Color;
// @implements(IDaylightShader.DirectionalLight0Dir)
uniform vec3 directionalLight0Dir;
// @implements(IDaylightShader.DirectionalLight0Intensity)
uniform float directionalLight0Intensity;

// @implements(IDaylightShader.DirectionalLight1Color)
uniform vec3 directionalLight1Color;
// @implements(IDaylightShader.DirectionalLight1Dir)
uniform vec3 directionalLight1Dir;
// @implements(IDaylightShader.DirectionalLight1Intensity)
uniform float directionalLight1Intensity;

// @implements(ICameraDirectionShader.CameraDirection)
uniform vec3 cameraDirection;

// @pixelType(RGBA)
uniform sampler2D textureSampler;

uniform vec3 uColor;
uniform vec3 uColorOverride;
uniform float uColorMix;
uniform float uOpacity;

void main()
{
    vec3 norm = normalize(FragNormal);

    // Compute screen-space derivatives for edge detection
    vec3 dx = dFdx(norm);
    vec3 dy = dFdy(norm);
    float edgeFactor = length(dx) + length(dy);
    float smoothing = smoothstep(0.2, 0.5, edgeFactor);

    // Directional light 0
    vec3 lightDir0 = normalize(-directionalLight0Dir);
    float diff0 = max(dot(norm, lightDir0), 0.0) * (1.0 - smoothing);
    vec3 diffuse0 = diff0 * directionalLight0Color * directionalLight0Intensity;

    // Directional light 1
    vec3 lightDir1 = normalize(-directionalLight1Dir);
    float diff1 = max(dot(norm, lightDir1), 0.0) * (1.0 - smoothing);
    vec3 diffuse1 = diff1 * directionalLight1Color * directionalLight1Intensity;

    // Ambient component
    vec3 ambient = ambientLightColor * ambientLightIntensity;

    // Sample the texture color using texture coordinates
    vec4 textureColor = texture(textureSampler, TexCoords);

    // Color tinting: mix base color with override, then multiply by texture
    vec3 baseColor = mix(uColor, uColorOverride, uColorMix) * textureColor.rgb;

    float rimFactor = 1.0 - max(dot(cameraDirection, norm), 0.0);
    rimFactor = smoothstep(0.3, 0.8, rimFactor);
    vec3 rimLight = rimFactor * vec3(1.0) * 0.5;

    // Combine lighting components
    vec3 finalColor = (ambient + diffuse0 + diffuse1 + rimLight) * baseColor;
    FragColor = vec4(finalColor, uOpacity);
}
