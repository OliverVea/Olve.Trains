#version 330 core

// @pixelType(float)
uniform sampler2D heightMap;
uniform ivec2 gridSize;

// @implements(IWithWorldMatrix.WorldMatrix)
// @instanced
layout(location = 0) in mat4 iWorld;

// @implements(ICameraPositionShader.View)
uniform mat4 view;
// @implements(ICameraPositionShader.Projection)
uniform mat4 projection;

void main()
{
    int quadsX = gridSize.x - 1;

    int quadIndex = gl_VertexID / 6;
    int corner    = gl_VertexID % 6;

    int qx = quadIndex % quadsX;
    int qz = quadIndex / quadsX;

    float hA = texelFetch(heightMap, ivec2(qx,     qz),     0).r;
    float hB = texelFetch(heightMap, ivec2(qx + 1, qz),     0).r;
    float hC = texelFetch(heightMap, ivec2(qx,     qz + 1), 0).r;
    float hD = texelFetch(heightMap, ivec2(qx + 1, qz + 1), 0).r;

    bool diagAD = (hA + hD) <= (hB + hC);

    ivec2 offsets[6];
    if (diagAD) {
        offsets[0] = ivec2(0, 0);
        offsets[1] = ivec2(1, 0);
        offsets[2] = ivec2(1, 1);
        offsets[3] = ivec2(1, 1);
        offsets[4] = ivec2(0, 1);
        offsets[5] = ivec2(0, 0);
    } else {
        offsets[0] = ivec2(0, 1);
        offsets[1] = ivec2(0, 0);
        offsets[2] = ivec2(1, 0);
        offsets[3] = ivec2(1, 0);
        offsets[4] = ivec2(1, 1);
        offsets[5] = ivec2(0, 1);
    }

    ivec2 gridPos = ivec2(qx, qz) + offsets[corner];

    float h = texelFetch(heightMap, gridPos, 0).r;

    vec3 pos = vec3(float(gridPos.x), h, float(gridPos.y));

    vec4 worldPos = iWorld * vec4(pos, 1.0);

    gl_Position = projection * view * worldPos;
}
