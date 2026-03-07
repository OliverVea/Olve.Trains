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
layout (location = 7) in vec2 iSize;
// @instanced
layout (location = 8) in vec4 iTint;
// @instanced
layout (location = 9) in vec4 iBorderWidth;  // left, top, right, bottom
// @instanced
layout (location = 10) in vec4 iBorderColor;
// @instanced
layout (location = 11) in vec4 iBorderRadius; // topLeft, topRight, bottomRight, bottomLeft

out VS_OUT {
    vec2 texCoord;
    vec4 tint;
    vec2 fragPosPx;
    vec2 boxSizePx;
    vec4 borderWidthPx;
    vec4 borderColor;
    vec4 borderRadiusPx;
} vs_out;

// @implements(ICameraPositionShader.View)
uniform mat4 view;
// @implements(ICameraPositionShader.Projection)
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * iWorld * vec4(position, 1.0);

    vs_out.texCoord = texCoords;
    vs_out.tint = iTint;
    vs_out.fragPosPx = texCoords * iSize;
    vs_out.boxSizePx = iSize;
    vs_out.borderWidthPx = iBorderWidth;
    vs_out.borderColor = iBorderColor;
    vs_out.borderRadiusPx = iBorderRadius;
}