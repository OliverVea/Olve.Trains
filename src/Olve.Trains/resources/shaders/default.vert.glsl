#version 330 core

// @implements(IWithPosition3D.Position)
layout (location = 0) in vec3 position;
// @implements(IWithNormal3D.Normal)
layout (location = 1) in vec3 normal;
// @implements(IWithTexCoords2D.TexCoords)
layout (location = 2) in vec2 texCoords;

// @implements(IWithWorldMatrix.WorldMatrix)
// @instanced
layout (location = 3) in mat4 iWorld;

// @instanced
layout (location = 7) in vec3 iColorOverride;
// @instanced
layout (location = 8) in float iColorMix;

// @implements(ICameraPositionShader.View)
uniform mat4 view;
// @implements(ICameraPositionShader.Projection)
uniform mat4 projection;

// @implements(IShadowShader.LightSpaceMatrix)
uniform mat4 lightSpaceMatrix;

out vec3 FragPos;
out vec3 FragNormal;
out vec2 TexCoords;
out vec4 FragPosLightSpace;
out vec3 InstanceColorOverride;
out float InstanceColorMix;

void main()
{
    vec4 worldPos = iWorld * vec4(position, 1.0);
    FragPos = worldPos.xyz;
    FragNormal = mat3(transpose(inverse(iWorld))) * normal;
    TexCoords = texCoords;
    FragPosLightSpace = lightSpaceMatrix * worldPos;
    InstanceColorOverride = iColorOverride;
    InstanceColorMix = iColorMix;

    gl_Position = projection * view * worldPos;
}
