using System.Diagnostics.CodeAnalysis;

namespace Olve.Trains.AssetPipeline.Shaders;

public static class ShaderHelper
{
    public static Result<IReadOnlyList<Uniform>> GetUniforms(string shaderSource, string? fileName = null)
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

            // Parse @pixelType(...) annotation from the previous line
            string? pixelType = null;
            if (uniformType == UniformType.Sampler2D)
            {
                // Check previous line (i points to the \n before "uniform")
                var prevLineEnd = i;
                var prevLineStart = shaderSource.LastIndexOf('\n', prevLineEnd - 1);
                if (prevLineStart == -1) prevLineStart = 0;
                var prevLine = shaderSource[prevLineStart..prevLineEnd];

                if (TryParsePixelTypeAnnotation(prevLine, out var parsed))
                {
                    pixelType = parsed;
                }
                else
                {
                    var fileInfo = fileName != null ? $" in '{fileName}'" : "";
                    return new ResultProblem(
                        "sampler2D uniform '{0}' at line {1}{2} is missing a // @pixelType(...) annotation on the previous line",
                        uniformName, lineNumber, fileInfo);
                }
            }

            // Parse @implements(...) annotations from preceding comment lines
            // Walk backwards through consecutive annotation lines before the uniform
            var implementsList = new List<ImplementsAnnotation>();
            {
                var searchEnd = i; // \n before "uniform"
                while (searchEnd > 0)
                {
                    var searchStart = shaderSource.LastIndexOf('\n', searchEnd - 1);
                    if (searchStart == -1) searchStart = 0;
                    var prevLine = shaderSource[searchStart..searchEnd].Trim();

                    if (TryParseImplementsAnnotation(prevLine, out var iface, out var prop))
                    {
                        implementsList.Add(new ImplementsAnnotation(iface, prop));
                        searchEnd = searchStart;
                    }
                    else if (prevLine.Contains("@pixelType(", StringComparison.Ordinal))
                    {
                        // Skip @pixelType lines, keep walking
                        searchEnd = searchStart;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            Uniform uniform = new()
            {
                Name = uniformName,
                Type = uniformType.Value,
                PixelType = pixelType,
                Implements = implementsList,
            };

            uniforms.Add(uniform);

            offset = uniformNameEnd + 1;
            i = shaderSource.IndexOf("\nuniform ", offset, StringComparison.OrdinalIgnoreCase);
        }

        return uniforms;
    }

    public static Result<IReadOnlyList<VertexAttribute>> GetVertexAttributes(string shaderSource)
    {
        List<VertexAttribute> attributes = [];
        var lines = shaderSource.Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex].Trim();

            // Match: layout(location = N) in <type> <name>;
            // Also handles layout (location=N) with varying whitespace
            var layoutIndex = line.IndexOf("layout", StringComparison.Ordinal);
            if (layoutIndex == -1) continue;

            var inIndex = line.IndexOf(" in ", StringComparison.Ordinal);
            if (inIndex == -1) continue;

            // Extract location number
            var locationIndex = line.IndexOf("location", StringComparison.Ordinal);
            if (locationIndex == -1) continue;

            var equalsIndex = line.IndexOf('=', locationIndex);
            if (equalsIndex == -1) continue;

            var closeParenIndex = line.IndexOf(')', equalsIndex);
            if (closeParenIndex == -1) continue;

            var locationString = line[(equalsIndex + 1)..closeParenIndex].Trim();
            if (!int.TryParse(locationString, out var location))
            {
                return new ResultProblem(
                    "Could not parse location number '{0}' at line {1}",
                    locationString, lineIndex + 1);
            }

            // Extract type and name after " in "
            var afterIn = line[(inIndex + 4)..].TrimStart();
            var spaceIndex = afterIn.IndexOf(' ');
            if (spaceIndex == -1)
            {
                return new ResultProblem(
                    "Could not find type and name after 'in' at line {0}",
                    lineIndex + 1);
            }

            var typeString = afterIn[..spaceIndex];
            if (!TryParseUniformType(typeString, out var type))
            {
                return new ResultProblem(
                    "Could not parse vertex attribute type '{0}' at line {1}",
                    typeString, lineIndex + 1);
            }

            var nameStart = afterIn[(spaceIndex + 1)..];
            var nameEnd = nameStart.IndexOfAny([';', ' ']);
            var name = nameEnd == -1 ? nameStart : nameStart[..nameEnd];

            // Check for annotations on preceding lines and same line
            var isInstanced = false;
            var attrImplements = new List<ImplementsAnnotation>();

            // Check same line for @instanced
            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex != -1 && line[commentIndex..].Contains("@instanced", StringComparison.OrdinalIgnoreCase))
            {
                isInstanced = true;
            }

            // Walk backwards through preceding annotation lines
            for (var prevIdx = lineIndex - 1; prevIdx >= 0; prevIdx--)
            {
                var prevLine = lines[prevIdx].Trim();

                if (prevLine.Contains("@instanced", StringComparison.OrdinalIgnoreCase))
                {
                    isInstanced = true;
                }
                else if (TryParseImplementsAnnotation(prevLine, out var iface, out var prop))
                {
                    attrImplements.Add(new ImplementsAnnotation(iface, prop));
                }
                else
                {
                    break;
                }
            }

            attributes.Add(new VertexAttribute
            {
                Location = location,
                Name = name,
                Type = type.Value,
                IsInstanced = isInstanced,
                Implements = attrImplements,
            });
        }

        // Sort by location to ensure consistent ordering
        attributes.Sort((a, b) => a.Location.CompareTo(b.Location));

        return attributes;
    }

    private static bool TryParseImplementsAnnotation(
        string text,
        [NotNullWhen(true)] out string? interfaceName,
        [NotNullWhen(true)] out string? propertyName)
    {
        interfaceName = null;
        propertyName = null;

        var markerIndex = text.IndexOf("@implements(", StringComparison.Ordinal);
        if (markerIndex == -1) return false;

        var start = markerIndex + "@implements(".Length;
        var end = text.IndexOf(')', start);
        if (end == -1) return false;

        var content = text[start..end].Trim();
        var dotIndex = content.IndexOf('.');
        if (dotIndex == -1 || dotIndex == 0 || dotIndex == content.Length - 1) return false;

        interfaceName = content[..dotIndex];
        propertyName = content[(dotIndex + 1)..];
        return true;
    }

    private static bool TryParsePixelTypeAnnotation(string text, [NotNullWhen(true)] out string? pixelType)
    {
        pixelType = null;

        var markerIndex = text.IndexOf("@pixelType(", StringComparison.Ordinal);
        if (markerIndex == -1) return false;

        var start = markerIndex + "@pixelType(".Length;
        var end = text.IndexOf(')', start);
        if (end == -1) return false;

        pixelType = text[start..end].Trim();
        return pixelType.Length > 0;
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
            "ivec2" => UniformType.IntVector2,
            _ => null
        };

        return uniformType.HasValue;
    }
}