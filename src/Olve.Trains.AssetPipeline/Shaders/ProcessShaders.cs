using Microsoft.Extensions.Logging;
using Olve.Paths.Glob;
using Olve.Trains.AssetPipeline.Assets;
using Scriban.Runtime;

namespace Olve.Trains.AssetPipeline.Shaders;

/// <summary>
///     Compiles shader slang shaders to GLSL
/// </summary>
/// <param name="logger"></param>
public class ProcessShaders(
    ILogger<ProcessShaders> logger,
    NamespaceProvider namespaceProvider,
    TemplateWriter templateWriter,
    PathProvider pathProvider)
{
    private static readonly string TemplateFileName = "ShaderClass.scriban";

    public record Request;
    public record Response(IReadOnlyList<ShaderProgram> Shaders);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing shader files");

        var shaderFilesPaths = pathProvider.ShadersSourceFolder.TryGlob("**/*.glsl", out var hits) ? hits : [];
        var shaders = new List<Shader>();

        pathProvider.ShadersOutputFolder.EnsurePathExists();

        foreach (var absoluteShaderPath in shaderFilesPaths)
        {
            var shaderFile = System.IO.Path.GetRelativePath(pathProvider.ShadersSourceFolder.Path, absoluteShaderPath.Path);
            logger.LogDebug("Reading shader: {ShaderFile}", shaderFile);

            // Load shader as string
            var shaderSource = await File.ReadAllTextAsync(absoluteShaderPath.Path, ct);

            // Read uniforms
            if (ShaderHelper.GetUniforms(shaderSource, shaderFile).TryPickProblems(out var problems, out var uniforms))
            {
                return problems.Prepend("Failed to read uniforms in shader file '{0}'", shaderFile);
            }

            var uniformNames = string.Join(", ", uniforms.Select(u => u.Name));
            logger.LogDebug("Got uniforms: {UniformNames}", uniformNames);

            var fileName = System.IO.Path.GetFileNameWithoutExtension(shaderFile);
            var fileNameSegments = fileName.Split('.');

            if (fileNameSegments.Length != 2)
            {
                return new ResultProblem("Invalid shader file name '{0}' shader name should be in the format: '<shader name>.<frag/vert>.glsl'.", shaderFile);
            }

            var shaderName = char.ToUpper(fileNameSegments[0][0]) + fileNameSegments[0][1..];
            var shaderType = fileNameSegments[1].ToLowerInvariant() switch
            {
                "frag" => ShaderType.Fragment,
                "vert" => ShaderType.Vertex,
                "geom" => ShaderType.Geometry,
                _ => ShaderType.Unknown
            };

            if (shaderType == ShaderType.Unknown)
            {
                return new ResultProblem("Invalid shader type '{0}' in shader file '{1}'. Shader type should be either 'frag' or 'vert'.", fileNameSegments[1], shaderFile);
            }

            // Read vertex attributes from vertex shaders
            IReadOnlyList<VertexAttribute> vertexAttributes = [];
            if (shaderType == ShaderType.Vertex)
            {
                if (ShaderHelper.GetVertexAttributes(shaderSource).TryPickProblems(out var attrProblems, out var attrs))
                {
                    return attrProblems.Prepend("Failed to read vertex attributes in shader file '{0}'", shaderFile);
                }

                vertexAttributes = attrs;
                var attrNames = string.Join(", ", vertexAttributes.Select(a => $"{a.Name}({a.Type})"));
                logger.LogDebug("Got vertex attributes: {AttributeNames}", attrNames);
            }

            // Populate shader class
            var shader = new Shader
            {
                Name = shaderName,
                Type = shaderType,
                SourcePath = shaderFile,
                SourceCode = shaderSource,
                Uniforms = uniforms,
                VertexAttributes = vertexAttributes,
            };

            shaders.Add(shader);

            logger.LogDebug("Finished reading shader: {ShaderFile}", shaderFile);
        }

        List<ShaderProgram> shaderPrograms = [];

        foreach (var shaderGroup in shaders.GroupBy(x => x.Name))
        {
            var programName = shaderGroup.Key;

            var fragmentShaders = shaderGroup.Where(x => x.Type == ShaderType.Fragment).ToList();
            if (fragmentShaders.Count != 1)
            {
                return new ResultProblem("Expected exactly one fragment shader for program '{0}', but found {1}.", programName, fragmentShaders.Count);
            }
            var fragmentShader = fragmentShaders.First();

            var vertexShaders = shaderGroup.Where(x => x.Type == ShaderType.Vertex).ToList();
            if (vertexShaders.Count != 1)
            {
                return new ResultProblem("Expected exactly one vertex shader for program '{0}', but found {1}.", programName, vertexShaders.Count);
            }
            var vertexShader = vertexShaders.First();

            var geometryShaders = shaderGroup.Where(x => x.Type == ShaderType.Geometry).ToList();
            if (geometryShaders.Count > 1)
            {
                return new ResultProblem("Expected at most one geometry shader for program '{0}', but found {1}.", programName, geometryShaders.Count);
            }

            var geometryShader = geometryShaders.FirstOrDefault();

            Dictionary<string, Uniform> uniforms = [];

            if (AddUniforms(uniforms, fragmentShader.Uniforms).TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to add uniforms for fragment shader '{0}'", fragmentShader.SourcePath);
            }

            if (AddUniforms(uniforms, vertexShader.Uniforms).TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to add uniforms for vertex shader '{0}'", vertexShader.SourcePath);
            }

            if (geometryShader != null)
            {
                if (AddUniforms(uniforms, geometryShader.Uniforms).TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to add uniforms for geometry shader '{0}'", geometryShader.SourcePath);
                }
            }

            var destinationPath = pathProvider.ShadersOutputFolder / ("Shaders." + programName + ".cs");

            var shaderProgram = new ShaderProgram
            {
                Name = programName,
                Namespace = namespaceProvider.ShaderNamespace,
                Destination = destinationPath,
                FragmentShader = fragmentShader,
                VertexShader = vertexShader,
                GeometryShader = geometryShader,
                Uniforms = uniforms.Values.ToArray(),
                VertexAttributes = vertexShader.VertexAttributes,
            };

            shaderPrograms.Add(shaderProgram);
        }

        if (!Paths.Path.TryGetAssemblyExecutable(out var assemblyPath))
        {
            return new ResultProblem("Could not find assembly executable.");
        }

        var templatePath = assemblyPath.Parent / "templates" / TemplateFileName;

        foreach (var shaderProgram in shaderPrograms)
        {
            var shaderProgramResult = await WriteShaderSourceFileAsync(shaderProgram, templatePath, ct);
            if (shaderProgramResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to write shader source file for shader program '{0}'", shaderProgram.Name);
            }
        }

        logger.LogInformation("Compiled {ShaderCount} shader programs successfully!", shaderPrograms.Count);

        return new Response(shaderPrograms);
    }

    private async Task<Result> WriteShaderSourceFileAsync(ShaderProgram shaderProgram, IPath templatePath, CancellationToken ct)
    {
        logger.LogDebug("Rendering shader program: {ShaderProgramName}", shaderProgram.Name);

        var scriptObject = MapToScriptObject(shaderProgram);

        return await templateWriter.WriteTemplateAsync(templatePath, scriptObject, shaderProgram.Destination , ct);
    }

    private static ScriptObject MapToScriptObject(ShaderProgram shaderProgram)
    {
        ScriptObject programObject = new() {
            { "Name", shaderProgram.Name },
            { "Namespace", shaderProgram.Namespace} };

        List<ScriptObject> uniformObjects = [];

        foreach (var uniform in shaderProgram.Uniforms)
        {
            ScriptObject uniformObject = new()
            {
                { "Name", uniform.Name },
                { "VariableName", uniform.Name },
                { "PropertyName", char.ToUpper(uniform.Name[0]) + uniform.Name[1..] },
                { "UniformType", uniform.Type.ToString() },
                { "DataType", uniform.GetDataType() },
                { "RenderParameterType", uniform.Type.GetRenderParameterType() },
                { "LayoutLocation", uniform.LayoutLocation },
                { "Initializer", uniform.Type.GetInitializer() },
                { "PixelType", uniform.PixelType }
            };

            uniformObjects.Add(uniformObject);
        }

        programObject.Add("Uniforms", uniformObjects);

        // Vertex attributes
        List<ScriptObject> vertexAttributeObjects = [];
        var perVertexAttributes = shaderProgram.VertexAttributes.Where(a => !a.IsInstanced).ToList();
        var instancedAttributes = shaderProgram.VertexAttributes.Where(a => a.IsInstanced).ToList();
        var hasInstancedAttributes = instancedAttributes.Count > 0;

        var vertexStride = perVertexAttributes.Sum(a => a.Type.GetComponentCount());
        var instanceStride = instancedAttributes.Sum(a => a.Type.GetComponentCount());

        var perVertexByteOffset = 0;
        var instanceByteOffset = 0;

        foreach (var attr in shaderProgram.VertexAttributes)
        {
            var componentCount = attr.Type.GetComponentCount();
            var byteOffset = attr.IsInstanced ? instanceByteOffset : perVertexByteOffset;

            var locationSlotCount = attr.Type.GetLocationSlotCount();

            ScriptObject attrObject = new()
            {
                { "Location", attr.Location },
                { "Name", attr.Name },
                { "PropertyName", char.ToUpper(attr.Name[0]) + attr.Name[1..] },
                { "ComponentCount", componentCount },
                { "LocationSlotCount", locationSlotCount },
                { "IsInstanced", attr.IsInstanced },
                { "ByteOffset", byteOffset },
                { "DataType", attr.Type.GetVertexDataType() },
                { "WriteCode", GetWriteCode(attr.Name, componentCount) },
            };

            vertexAttributeObjects.Add(attrObject);

            if (attr.IsInstanced)
                instanceByteOffset += componentCount * sizeof(float);
            else
                perVertexByteOffset += componentCount * sizeof(float);
        }

        programObject.Add("VertexAttributes", vertexAttributeObjects);
        programObject.Add("HasInstancedAttributes", hasInstancedAttributes);
        programObject.Add("VertexStride", vertexStride);
        programObject.Add("VertexStrideBytes", vertexStride * sizeof(float));
        programObject.Add("InstanceStride", instanceStride);
        programObject.Add("InstanceStrideBytes", instanceStride * sizeof(float));

        // Separate lists for struct generation (avoids Scriban filtering issues)
        var instancedAttrObjects = vertexAttributeObjects.Where(a => (bool)a["IsInstanced"]).ToList();
        var perVertexAttrObjects = vertexAttributeObjects.Where(a => !(bool)a["IsInstanced"]).ToList();
        programObject.Add("InstancedAttributes", instancedAttrObjects);
        programObject.Add("PerVertexAttributes", perVertexAttrObjects);

        ScriptObject fragmentShaderObject = new()
        {
            { "Name", shaderProgram.FragmentShader },
            { "SourcePath", shaderProgram.FragmentShader.SourcePath },
            { "SourceCode", shaderProgram.FragmentShader.SourceCode }
        };

        programObject.Add("FragmentShader", fragmentShaderObject);

        ScriptObject vertexShaderObject = new()
        {
            { "Name", shaderProgram.VertexShader },
            { "SourcePath", shaderProgram.VertexShader.SourcePath },
            { "SourceCode", shaderProgram.VertexShader.SourceCode }
        };

        programObject.Add("VertexShader", vertexShaderObject);

        if (shaderProgram.GeometryShader != null)
        {
            ScriptObject geometryShaderObject = new()
            {
                { "Name", shaderProgram.GeometryShader },
                { "SourcePath", shaderProgram.GeometryShader.SourcePath },
                { "SourceCode", shaderProgram.GeometryShader.SourceCode }
            };

            programObject.Add("GeometryShader", geometryShaderObject);
        }

        return programObject;
    }

    private static string GetWriteCode(string name, int componentCount)
    {
        return componentCount switch
        {
            1 => $"buffer[i++] = {name};",
            2 => $"buffer[i++] = {name}.X; buffer[i++] = {name}.Y;",
            3 => $"buffer[i++] = {name}.X; buffer[i++] = {name}.Y; buffer[i++] = {name}.Z;",
            4 => $"buffer[i++] = {name}.X; buffer[i++] = {name}.Y; buffer[i++] = {name}.Z; buffer[i++] = {name}.W;",
            16 => // mat4: row-major order matching Matrix4X4<T>.CopyTo / UniformMatrix4(transpose:false)
                $"buffer[i++] = {name}.M11; buffer[i++] = {name}.M12; buffer[i++] = {name}.M13; buffer[i++] = {name}.M14; " +
                $"buffer[i++] = {name}.M21; buffer[i++] = {name}.M22; buffer[i++] = {name}.M23; buffer[i++] = {name}.M24; " +
                $"buffer[i++] = {name}.M31; buffer[i++] = {name}.M32; buffer[i++] = {name}.M33; buffer[i++] = {name}.M34; " +
                $"buffer[i++] = {name}.M41; buffer[i++] = {name}.M42; buffer[i++] = {name}.M43; buffer[i++] = {name}.M44;",
            _ => throw new ArgumentException($"Unsupported component count: {componentCount}")
        };
    }

    private static Result AddUniforms(Dictionary<string, Uniform> uniforms, IReadOnlyList<Uniform> newUniforms)
    {
        foreach (var uniform in newUniforms)
        {
            if (uniforms.TryGetValue(uniform.Name, out var existingUniform))
            {
                if (existingUniform.Type != uniform.Type)
                {
                    return new ResultProblem("Uniform '{0}' already exists with type '{1}', but found type '{2}' in shader.", uniform.Name, existingUniform.Type, uniform.Type);
                }
            }

            uniforms[uniform.Name] = uniform;
        }

        return Result.Success();
    }
}
