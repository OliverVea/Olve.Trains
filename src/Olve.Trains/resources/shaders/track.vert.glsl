#version 330 core

// Per-vertex attributes (from template mesh, divisor=0)
layout(location = 0) in vec3 aPosition;  // x,y = profile offset; z = spline parameter t (0..1)
layout(location = 1) in vec3 aNormal;    // local-space normal of the profile

// Per-instance attributes (Hermite control points, divisor=1)
// @instanced
layout(location = 2) in vec3 iP0;       // start position
// @instanced
layout(location = 3) in vec3 iP1;       // end position
// @instanced
layout(location = 4) in vec3 iT0;       // start tangent
// @instanced
layout(location = 5) in vec3 iT1;       // end tangent

uniform mat4 view;
uniform mat4 projection;

out vec3 FragPos;
out vec3 FragNormal;

void main()
{
    float t = aPosition.z;
    float t2 = t * t;
    float t3 = t2 * t;

    // Hermite basis: position
    vec3 pos = (2.0*t3 - 3.0*t2 + 1.0) * iP0
             + (t3 - 2.0*t2 + t)        * iT0
             + (-2.0*t3 + 3.0*t2)       * iP1
             + (t3 - t2)                 * iT1;

    // Hermite basis: tangent (derivative)
    vec3 tangent = normalize(
        (6.0*t2 - 6.0*t)       * iP0
      + (3.0*t2 - 4.0*t + 1.0) * iT0
      + (-6.0*t2 + 6.0*t)      * iP1
      + (3.0*t2 - 2.0*t)       * iT1
    );

    // Build local frame from tangent
    vec3 up = vec3(0.0, 1.0, 0.0);
    vec3 binormal = normalize(cross(tangent, up));
    vec3 normal = cross(binormal, tangent);

    // Offset template profile vertex into world space
    vec3 worldPos = pos + aPosition.x * binormal + aPosition.y * normal;

    // Rotate normal into world frame
    vec3 worldNormal = normalize(aNormal.x * binormal + aNormal.y * normal + aNormal.z * tangent);

    FragPos = worldPos;
    FragNormal = worldNormal;

    gl_Position = projection * view * vec4(worldPos, 1.0);
}
