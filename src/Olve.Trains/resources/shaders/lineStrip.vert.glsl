#version 330 core

// @implements(IWithPosition3D.Position)
layout(location = 0) in vec3 aPosition;
// @implements(IWithColor3D.Color)
layout(location = 1) in vec3 aColor;

// @implements(IWithWorldMatrix.WorldMatrix)
// @instanced
layout(location = 2) in mat4 iWorld;

out vec3 vColor;

// @implements(ICameraPositionShader.View)
uniform mat4 view;
// @implements(ICameraPositionShader.Projection)
uniform mat4 projection;

void main()
{
    vColor = aColor;
    vec4 worldPos = iWorld * vec4(aPosition, 1.0);
    gl_Position = projection * view * worldPos;
}
