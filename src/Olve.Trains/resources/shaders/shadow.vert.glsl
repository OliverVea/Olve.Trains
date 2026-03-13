#version 330 core

// @implements(IWithPosition3D.Position)
layout (location = 0) in vec3 position;
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

void main()
{
    gl_Position = projection * view * iWorld * vec4(position, 1.0);
}
