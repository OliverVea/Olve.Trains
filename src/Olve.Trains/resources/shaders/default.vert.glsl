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

// @implements(ICameraPositionShader.View)
uniform mat4 view;
// @implements(ICameraPositionShader.Projection)
uniform mat4 projection;

out vec3 FragPos;
out vec3 FragNormal;
out vec2 TexCoords;

void main()
{
    FragPos = vec3(iWorld * vec4(position, 1.0));
    FragNormal = mat3(transpose(inverse(iWorld))) * normal;
    TexCoords = texCoords;

    gl_Position = projection * view * iWorld * vec4(position, 1.0);
}
