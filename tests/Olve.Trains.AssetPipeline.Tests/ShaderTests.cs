using Olve.Trains.AssetPipeline.Shaders;

namespace Olve.Trains.AssetPipeline.Tests;

public class ShaderTests
{
    
    
    [Test]
    public async Task GetUniforms_FragmentSource()
    {
        // Arrange
        
        // Act
        var result = ShaderHelper.GetUniforms(FragmentSource);
        
        // Assert
        await Assert.That(result.Succeeded).IsTrue();

        var uniforms = result.Value ?? throw new Exception();
        
        await Assert.That(uniforms).HasCount().EqualTo(5);
    }
    
    [Test]
    public async Task GetUniforms_VertexSource()
    {
        // Arrange
        
        // Act
        var result = ShaderHelper.GetUniforms(VertexSource);
        
        // Assert
        await Assert.That(result.Succeeded).IsTrue();

        var uniforms = result.Value ?? throw new Exception();
        
        await Assert.That(uniforms).HasCount().EqualTo(5);
    }
    
    


    private const string FragmentSource =
        """
        #version 330 core
        
        in vec3 FragPos;
        in vec3 FragNormal;
        in float DistanceToMouseRay;
        
        out vec4 FragColor;
        
        uniform vec3 ambientLightColor;
        uniform float ambientIntensity;
        
        uniform vec3 directionalLightColor;
        uniform vec3 directionalLightDir;
        uniform float directionalIntensity;
        
        void main()
        {
            // Normalize the normal and light direction
            vec3 norm = normalize(FragNormal);
            vec3 lightDir = normalize(-directionalLightDir); // Ensure it's pointing towards the fragment
        
            // Ambient lighting
            vec3 ambient = ambientLightColor * ambientIntensity;
        
            // Diffuse lighting
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * directionalLightColor * directionalIntensity;
        
            // Specular lighting (Blinn-Phong)
            // Assume the camera is at (0,0,0)
            vec3 viewDir = normalize(-FragPos);
            vec3 halfDir = normalize(lightDir + viewDir);
            // Hard-coded shininess factor of 32.0 gives a moderate highlight
            float spec = pow(max(dot(norm, halfDir), 0.0), 32.0);
            // Modulate the specular highlight by DistanceToMouseRay so that fragments near the mouse ray glow more.
            spec *= (1.0 - clamp(DistanceToMouseRay, 0.0, 1.0));
            vec3 specular = spec * directionalLightColor * directionalIntensity;
        
            // Combine all lighting components
            vec3 lighting = ambient + diffuse + specular;
        
            FragColor = vec4(lighting, 1.0);
        }
        """;

    private const string VertexSource =
        """
        #version 330 core
        
        layout (location = 0) in vec3 position;
        layout (location = 1) in vec3 normal;
        
        uniform mat4 world;
        uniform mat4 view;
        uniform mat4 projection;
        
        uniform vec3 mouseRayOrigin;
        uniform vec3 mouseRayDirection;
        
        out vec3 FragPos;
        out vec3 FragNormal;
        out float DistanceToMouseRay;
        
        void main()
        {
            // Compute the world-space position
            vec4 worldPos = world * vec4(position, 1.0);
            FragPos = worldPos.xyz;
        
            // Transform the normal correctly (for non-uniform scaling, use inverse transpose)
            FragNormal = normalize((transpose(inverse(world)) * vec4(normal, 0.0)).xyz);
        
            // Calculate the distance from the vertex to the mouse ray in world space.
            vec3 rayToVertex = FragPos - mouseRayOrigin;
            // Optionally adjust the divisor (e.g., / 10.0) to suit your scene scale.
            DistanceToMouseRay = length(cross(mouseRayDirection, rayToVertex)) / 10.0;
        
            // Compute final position on screen
            gl_Position = projection * view * worldPos;
        }
        """;
}