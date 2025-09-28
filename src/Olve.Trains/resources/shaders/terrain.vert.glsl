#version 330 core

layout (location = 0) in vec2 position; // model-space (x, z) position

uniform sampler2D heightMap;            // single-channel heightmap texture
uniform vec2 texelSize;                 // (1/textureWidth, 1/textureHeight)

uniform mat4 world;
uniform mat4 view;
uniform mat4 projection;

out vec3 vWorldPos;  // pass world-space position to geometry shader

void main()
{
    // Calculate texture coordinate.
    // (Assumes model positions are in texture coordinate range; adjust if needed)
    vec2 texCoord = position * texelSize;

    // Sample the height value.
    float h = texture(heightMap, texCoord).r;

    // Construct the model-space position (x, h, z).
    vec3 pos = vec3(position.x, h, position.y);

    // Transform to world space.
    vec4 worldPos = world * vec4(pos, 1.0);
    vWorldPos = worldPos.xyz;

    // Transform to clip space.
    gl_Position = projection * view * worldPos;
}
