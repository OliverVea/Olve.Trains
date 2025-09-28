#version 330 core

layout (location = 0) in vec2 position; // model-space (x, z) position

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
    vec2 texCoord = position * texelSize;
    float h = texture(heightMap, texCoord).r + 0.01;
    vec3 pos = vec3(position.x, h, position.y);
    vec4 worldPos = world * vec4(pos, 1.0);
    vWorldPos = worldPos.xyz;
    distanceToMouse = length(vWorldPos.xz - mousePosition.xz);
    gl_Position = projection * view * worldPos;
}
