#version 330 core

uniform sampler2D heightMap;            // single-channel heightmap texture
uniform vec2 texelSize;                 // (1/textureWidth, 1/textureHeight)

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

uniform vec3 mousePosition;

out vec3 vWorldPos;
// Mark as flat to ensure no interpolation if desired.
flat out float distanceToMouse;

void main()
{
    // Derive grid dimensions from texelSize
    int gridW = int(round(1.0 / texelSize.x));
    int quadsX = gridW - 1;

    // Each quad emits 6 vertices (2 triangles).
    int quadIndex = gl_VertexID / 6;
    int corner    = gl_VertexID % 6;

    int qx = quadIndex % quadsX;
    int qz = quadIndex / quadsX;

    // Sample heights at all four corners to choose diagonal adaptively
    float hA = texture(heightMap, vec2(qx,     qz)     * texelSize).r;
    float hB = texture(heightMap, vec2(qx + 1, qz)     * texelSize).r;
    float hC = texture(heightMap, vec2(qx,     qz + 1) * texelSize).r;
    float hD = texture(heightMap, vec2(qx + 1, qz + 1) * texelSize).r;

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

    vec2 position = vec2(float(gridPos.x), float(gridPos.y));
    vec2 texCoord = position * texelSize;
    float h = texture(heightMap, texCoord).r + 0.01;

    vec3 pos = vec3(position.x, h, position.y);
    vec4 worldPos = world * vec4(pos, 1.0);
    vWorldPos = worldPos.xyz;
    distanceToMouse = length(vWorldPos.xz - mousePosition.xz);
    gl_Position = projection * view * worldPos;
}
