using System.Diagnostics.CodeAnalysis;
using Olve.Results;

namespace Olve.Trains.AssetPipeline.Shaders;

public static class ShaderHelper
{
    public static Result<IReadOnlyList<Uniform>> GetUniforms(string shaderSource)
    {
        var offset = 0;
        var i = shaderSource.IndexOf("\nuniform ", offset, StringComparison.OrdinalIgnoreCase);
        
        List<Uniform> uniforms = [];

        while (i != -1)
        {
            var lineNumber = shaderSource[..i].Count(c => c == '\n');
            
            var uniformTypeStart = i + "\nuniform ".Length;
            var uniformTypeEnd = shaderSource.IndexOf(' ', uniformTypeStart);

            if (uniformTypeEnd == -1)
            {
                return new ResultProblem("Could not find end of uniform at line number '{0}'", lineNumber);
            }

            if (uniformTypeEnd - uniformTypeStart < 1)
            {
                return new ResultProblem("Could not find uniform type at line number '{0}'", lineNumber);
            }
            
            var uniformTypeString = shaderSource[uniformTypeStart..uniformTypeEnd];
            if (!TryParseUniformType(uniformTypeString, out var uniformType))
            {
                return new ResultProblem("Could not parse uniform type '{0}' at line number '{1}'", uniformTypeString, lineNumber);
            }

            var uniformNameStart = uniformTypeEnd + 1;
            var uniformNameEnd = shaderSource.IndexOfAny([' ', ';', '='], uniformNameStart);

            if (uniformNameEnd == -1)
            {
                return new ResultProblem("Could not find end of uniform at line number '{0}'", lineNumber);
            }
            
            if (uniformNameEnd - uniformNameStart < 1)
            {
                return new ResultProblem("Could not find uniform name at line number '{0}'", lineNumber);
            }
            
            var uniformName = shaderSource[uniformNameStart..uniformNameEnd];

            Uniform uniform = new()
            {
                Name = uniformName,
                Type = uniformType.Value,
            };
            
            uniforms.Add(uniform);
            
            offset = uniformNameEnd + 1;
            i = shaderSource.IndexOf("\nuniform ", offset, StringComparison.OrdinalIgnoreCase);
        }

        return uniforms;
    }

    public static bool TryParseUniformType(string uniformTypeString, [NotNullWhen(true)] out UniformType? uniformType)
    {
        uniformType = uniformTypeString switch
        {
            "bool" => UniformType.Bool,
            "vec2" => UniformType.Vector2,
            "vec3" => UniformType.Vector3,
            "vec4" => UniformType.Vector4,
            "float" => UniformType.Float,
            "mat3" => UniformType.Matrix3,
            "mat4" => UniformType.Matrix4,
            "sampler2D" => UniformType.Sampler2D,
            _ => null
        };

        return uniformType.HasValue;
    }
}