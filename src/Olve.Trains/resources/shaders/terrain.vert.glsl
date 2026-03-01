#version 330 core

// @pixelType(float)
uniform sampler2D heightMap;
uniform ivec2 gridSize;                 // (width, height) in texels

// @implements(IWithWorldMatrix.WorldMatrix)
// @instanced
layout(location = 0) in mat4 iWorld;

// @implements(ICameraPositionShader.View)
uniform mat4 view;
// @implements(ICameraPositionShader.Projection)
uniform mat4 projection;

out vec3 FragPos;

void main()
{
    int quadsX = gridSize.x - 1;

    // Each quad emits 6 vertices (2 triangles).
    // Determine which quad and which corner this vertex belongs to.
    int quadIndex = gl_VertexID / 6;
    int corner    = gl_VertexID % 6;

    int qx = quadIndex % quadsX;
    int qz = quadIndex / quadsX;

    // Quad corners: a=(qx,qz) b=(qx+1,qz) c=(qx,qz+1) d=(qx+1,qz+1)
    // Sample heights at all four corners to choose diagonal adaptively
    float hA = texelFetch(heightMap, ivec2(qx,     qz),     0).r;
    float hB = texelFetch(heightMap, ivec2(qx + 1, qz),     0).r;
    float hC = texelFetch(heightMap, ivec2(qx,     qz + 1), 0).r;
    float hD = texelFetch(heightMap, ivec2(qx + 1, qz + 1), 0).r;

    bool diagAD = (hA + hD) <= (hB + hC);

    // Map corner index to grid position
    // diagAD: tri1={a,b,d} tri2={d,c,a}  => corners 0,1,2,3,4,5
    // !diagAD: tri1={c,a,b} tri2={b,d,c}
    ivec2 offsets[6];
    if (diagAD) {
        offsets[0] = ivec2(0, 0); // a
        offsets[1] = ivec2(1, 0); // b
        offsets[2] = ivec2(1, 1); // d
        offsets[3] = ivec2(1, 1); // d
        offsets[4] = ivec2(0, 1); // c
        offsets[5] = ivec2(0, 0); // a
    } else {
        offsets[0] = ivec2(0, 1); // c
        offsets[1] = ivec2(0, 0); // a
        offsets[2] = ivec2(1, 0); // b
        offsets[3] = ivec2(1, 0); // b
        offsets[4] = ivec2(1, 1); // d
        offsets[5] = ivec2(0, 1); // c
    }

    ivec2 gridPos = ivec2(qx, qz) + offsets[corner];

    float h = texelFetch(heightMap, gridPos, 0).r;

    vec3 pos = vec3(float(gridPos.x), h, float(gridPos.y));

    vec4 worldPos = iWorld * vec4(pos, 1.0);
    FragPos = worldPos.xyz;

    gl_Position = projection * view * worldPos;
}