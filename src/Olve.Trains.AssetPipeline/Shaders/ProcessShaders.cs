using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;
using Olve.Trains.AssetPipeline.Assets;
using Scriban.Runtime;

namespace Olve.Trains.AssetPipeline.Shaders;

/// <summary>
///     Compiles shader slang shaders to GLSL
/// </summary>
/// <param name="logger"></param>
public class ProcessShaders(ILogger<ProcessShaders> logger, TemplateWriter templateWriter) : IAsyncOperation<ProcessShaders.Request, ProcessShaders.Response>
{
    private static readonly string TemplateFilePath = Path.Combine(Paths.TemplatesSourceFolder, "ShaderClass.scriban");
    
    public record Request;
    public record Response(IReadOnlyList<ShaderProgram> Shaders);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Processing shader files");

        var shaderFiles = Directory.GetFiles(Paths.ShaderSourceFolder, "*.glsl", SearchOption.AllDirectories);
        var shaders = new List<Shader>(shaderFiles.Length);

        Directory.CreateDirectory(Paths.ShaderOutputFolder);
        
        foreach (var absoluteShaderFile in shaderFiles)
        {
            var shaderFile = Path.GetRelativePath(Paths.ShaderSourceFolder, absoluteShaderFile);
            logger.LogDebug("Reading shader: {ShaderFile}", shaderFile);
            
            // Load shader as string
            var shaderSource = await File.ReadAllTextAsync(absoluteShaderFile, ct);
            
            // Read uniforms
            if (ShaderHelper.GetUniforms(shaderSource).TryPickProblems(out var problems, out var uniforms))
            {
                return problems.Prepend("Failed to read uniforms in shader file '{0}'", shaderFile);
            }
            
            var uniformNames = string.Join(", ", uniforms.Select(u => u.Name));
            logger.LogDebug("Got uniforms: {UniformNames}", uniformNames);
            
            var fileName = Path.GetFileNameWithoutExtension(shaderFile);
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

            // Populate shader class
            var shader = new Shader
            {
                Name = shaderName,
                Type = shaderType,
                SourcePath = shaderFile,
                SourceCode = shaderSource,
                Uniforms = uniforms
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

            var destinationPath = Path.Combine(Paths.ShaderOutputFolder, programName + ".cs");
            
            var shaderProgram = new ShaderProgram
            {
                Name = programName,
                Destination = destinationPath,
                FragmentShader = fragmentShader,
                VertexShader = vertexShader,
                GeometryShader = geometryShader,
                Uniforms = uniforms.Values.ToArray()
            };
            
            shaderPrograms.Add(shaderProgram);
        }
        
        if (!File.Exists(TemplateFilePath))
        {
            return new ResultProblem("Template file '{0}' not found", TemplateFilePath);
        }

        foreach (var shaderProgram in shaderPrograms)
        {
            var shaderProgramResult = await WriteShaderSourceFileAsync(shaderProgram, ct);
            if (shaderProgramResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to write shader source file for shader program '{0}'", shaderProgram.Name);
            }
        }
        
        logger.LogInformation("Compiled {ShaderCount} shader programs successfully!", shaderPrograms.Count);

        return new Response(shaderPrograms);
    }

    private async Task<Result> WriteShaderSourceFileAsync(ShaderProgram shaderProgram, CancellationToken ct)
    {
        logger.LogDebug("Rendering shader program: {ShaderProgramName}", shaderProgram.Name);

        var scriptObject = MapToScriptObject(shaderProgram);

        return await templateWriter.WriteTemplateAsync(TemplateFilePath, scriptObject, shaderProgram.Destination , ct);
    }

    private static ScriptObject MapToScriptObject(ShaderProgram shaderProgram)
    {
        ScriptObject programObject = new() { { "Name", shaderProgram.Name } };

        List<ScriptObject> uniformObjects = [];
        
        foreach (var uniform in shaderProgram.Uniforms)
        {
            ScriptObject uniformObject = new()
            {
                { "Name", uniform.Name },
                { "VariableName", uniform.Name },
                { "PropertyName", char.ToUpper(uniform.Name[0]) + uniform.Name[1..] },
                { "UniformType", uniform.Type },
                { "DataType", uniform.Type.GetDataType() },
                { "RenderParameterType", uniform.Type.GetRenderParameterType() },
                { "LayoutLocation", uniform.LayoutLocation },
                { "Initializer", uniform.Type.GetInitializer() }
            };

            uniformObjects.Add(uniformObject);
        }
        
        programObject.Add("Uniforms", uniformObjects);

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
