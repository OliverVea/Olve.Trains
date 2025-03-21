
// shaders.slang

// Uniform data to be passed from application -> shader.
cbuffer Uniforms
{
    float4x4 world;
    float4x4 view;
    float4x4 projection;
    float3 mouseRayOrigin;
    float3 mouseRayDirection;
    float3 ambientLightColor;
    float ambientIntensity;
    float3 directionalLightColor;
    float3 directionalLightDir;
    float directionalIntensity;
}

// Per-vertex attributes to be assembled from bound vertex buffers.
struct AssembledVertex
{
    float3 position : POSITION;
    float3 normal   : NORMAL;
};

// Output of the vertex shader, and input to the fragment shader.
struct CoarseVertex
{
    float3 fragPos;
    float3 fragNormal;
    float distanceToMouseRay;
};

// Output of the fragment shader
struct Fragment
{
    float4 color;
};

struct VertexStageOutput
{
    CoarseVertex    coarseVertex    : CoarseVertex;
    float4          sv_position     : SV_Position;
};

[shader("vertex")]
VertexStageOutput vertexMain(AssembledVertex assembledVertex)
{
    VertexStageOutput output;

    float4 worldPos = mul(world, float4(assembledVertex.position, 1.0));
    output.coarseVertex.fragPos = worldPos.xyz;

    output.coarseVertex.fragNormal = normalize(mul(world, float4(assembledVertex.normal, 0.0)).xyz);

    float3 rayToVertex = output.coarseVertex.fragPos - mouseRayOrigin;
    output.coarseVertex.distanceToMouseRay = length(cross(mouseRayDirection, rayToVertex)) / 10.0;

    output.sv_position = mul(projection, mul(view, worldPos));

    return output;
}

[shader("fragment")]
Fragment fragmentMain(CoarseVertex coarseVertex : CoarseVertex) : SV_Target
{
    Fragment output;

    float3 norm = normalize(coarseVertex.fragNormal);
    float3 lightDir = normalize(-directionalLightDir);

    float3 ambient = ambientLightColor * ambientIntensity;

    float diff = max(dot(norm, lightDir), 0.0);
    float3 diffuse = diff * directionalLightColor * directionalIntensity;

    float3 viewDir = normalize(-coarseVertex.fragPos);
    float3 halfDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(norm, halfDir), 0.0), 32.0);
    spec *= (1.0 - saturate(coarseVertex.distanceToMouseRay));
    float3 specular = spec * directionalLightColor * directionalIntensity;

    float3 lighting = ambient + diffuse + specular;

    output.color = float4(lighting, 1.0);
    return output;
}