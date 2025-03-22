using Microsoft.Extensions.Logging;
using Olve.Operations;
using Olve.Results;
using Scriban;
using Scriban.Runtime;

namespace Olve.Engine3D.AssetPipeline.Operations;

/// <summary>
///     Compiles shader slang shaders to GLSL
/// </summary>
/// <param name="logger"></param>
public class ProcessShaders(ILogger<ProcessShaders> logger) : IAsyncOperation<ProcessShaders.Request, ProcessShaders.Response>
{
    private const string ShaderSourceDirectory = "/app/shaders";
    private const string ShaderOutputDirectory = "/app/output/shaders";
    private const string ApplicationRoot = "/app";
    
    private static string TemplateFilePath { get; } = Path.Combine(ApplicationRoot, "Templates/ShaderClass.scriban");
    
    public record Request;
    public record Response(IReadOnlyList<ShaderProgram> Shaders);

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken ct = default)
    {
        logger.LogInformation("Processing shader files");

        var shaderFiles = Directory.GetFiles(ShaderSourceDirectory, "*.glsl", SearchOption.AllDirectories);
        var shaders = new List<Shader>(shaderFiles.Length);
        
        foreach (var absoluteShaderFile in shaderFiles)
        {
            var shaderFile = Path.GetRelativePath(ShaderSourceDirectory, absoluteShaderFile);
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
            if (fragmentShaders.Count > 1)
            {
                return new ResultProblem("Expected at most one fragment shader for program '{0}', but found {1}.", programName, fragmentShaders.Count);
            }
            var fragmentShader = fragmentShaders.FirstOrDefault();
            
            var vertexShaders = shaderGroup.Where(x => x.Type == ShaderType.Vertex).ToList();
            if (vertexShaders.Count > 1)
            {
                return new ResultProblem("Expected at most one vertex shader for program '{0}', but found {1}.", programName, vertexShaders.Count);
            }
            var vertexShader = vertexShaders.FirstOrDefault();

            Dictionary<string, Uniform> uniforms = [];
            
            if (fragmentShader != null)
            {
                if (AddUniforms(uniforms, fragmentShader.Uniforms).TryPickProblems(out var problems))
                {
                    return problems.Prepend("Failed to add uniforms for fragment shader '{0}'", fragmentShader.SourcePath);
                }
            }
            
            if (vertexShader != null)
            {
                if (AddUniforms(uniforms, vertexShader.Uniforms).TryPickProblems(out var problems))
                {
                    return problems.Prepend("Failed to add uniforms for vertex shader '{0}'", vertexShader.SourcePath);
                }
            }
            
            var shaderProgram = new ShaderProgram
            {
                Name = programName,
                FragmentShader = fragmentShader,
                VertexShader = vertexShader,
                Uniforms = uniforms.Values.ToArray()
            };
            
            shaderPrograms.Add(shaderProgram);
        }
        
        if (!File.Exists(TemplateFilePath))
        {
            return new ResultProblem("Template file '{0}' not found", TemplateFilePath);
        }
        
        var templateFile = await File.ReadAllTextAsync(TemplateFilePath, ct);

        foreach (var shaderProgram in shaderPrograms)
        {
            var shaderProgramResult = await WriteShaderSourceFileAsync(shaderProgram, templateFile);
            if (shaderProgramResult.TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to write shader source file for shader program '{0}'", shaderProgram.FragmentShader?.Name ?? shaderProgram.VertexShader?.Name ?? "Unknown");
            }
        }
        
        logger.LogInformation("Shaders compiled successfully!");

        return new Response(shaderPrograms);
    }

    private async Task<Result> WriteShaderSourceFileAsync(ShaderProgram shaderProgram, string templateFile)
    {
        logger.LogDebug("Rendering shader program: {ShaderProgramName}", shaderProgram.Name);
        
        var template = Template.Parse(templateFile, TemplateFilePath);
        var scriptObject = MapToScriptObject(shaderProgram);
        
        try
        {
            var sourceCode = await template.RenderAsync(scriptObject);
            
            var outputPath = Path.Combine(ShaderOutputDirectory, shaderProgram.Name + ".cs");
            
            await File.WriteAllTextAsync(outputPath, sourceCode);
            
            logger.LogDebug("Shader program '{ShaderProgramName}' rendered successfully to '{OutputPath}'.", shaderProgram.Name, outputPath);

            return Result.Success();
        }
        catch (Exception e)
        {
            return new ResultProblem(e, "Failed to render C# shader source template '{0}'", TemplateFilePath);
        }
    }

    private static ScriptObject MapToScriptObject(ShaderProgram shaderProgram)
    {
        ScriptObject programObject = new();
        programObject.Add("Name", shaderProgram.Name);
        
        List<ScriptObject> uniformObjects = [];
        
        foreach (var uniform in shaderProgram.Uniforms)
        {
            ScriptObject uniformObject = new();
            uniformObject.Add("Name", uniform.Name);
            uniformObject.Add("VariableName", uniform.Name);
            uniformObject.Add("PropertyName", char.ToUpper(uniform.Name[0]) + uniform.Name[1..]);
            uniformObject.Add("UniformType", uniform.Type);
            uniformObject.Add("DataType", uniform.Type.GetDataType());
            uniformObject.Add("RenderParameterType", uniform.Type.GetRenderParameterType());
            uniformObject.Add("LayoutLocation", uniform.LayoutLocation);
            
            uniformObjects.Add(uniformObject);
        }
        
        programObject.Add("Uniforms", uniformObjects);

        if (shaderProgram.FragmentShader != null)
        {
            ScriptObject fragmentShaderObject = new();
            fragmentShaderObject.Add("Name", shaderProgram.FragmentShader);
            fragmentShaderObject.Add("SourcePath", shaderProgram.FragmentShader.SourcePath);
            fragmentShaderObject.Add("SourceCode", shaderProgram.FragmentShader.SourceCode);
            
            programObject.Add("FragmentShader", fragmentShaderObject);
        }
        
        if (shaderProgram.VertexShader != null)
        {
            ScriptObject vertexShaderObject = new();
            vertexShaderObject.Add("Name", shaderProgram.VertexShader);
            vertexShaderObject.Add("SourcePath", shaderProgram.VertexShader.SourcePath);
            vertexShaderObject.Add("SourceCode", shaderProgram.VertexShader.SourceCode);
            
            programObject.Add("VertexShader", vertexShaderObject);
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

public class ShaderProgram
{
    public required string Name { get; set; }
    public Shader? FragmentShader { get; set; }
    public Shader? VertexShader { get; set; }
    
    public IReadOnlyList<Uniform> Uniforms { get; set; }
}

public enum ShaderType
{
    Unknown,
    Fragment,
    Vertex,
}

public class Shader
{
    public required string Name { get; set; }
    public required ShaderType Type { get; set; }
    public required string SourcePath { get; set; }
    public required string SourceCode { get; set; }
    public required IReadOnlyList<Uniform> Uniforms { get; set; }
}

public class Uniform
{
    public required string Name { get; set; }
    public required UniformType Type { get; set; }
    public int? LayoutLocation { get; set; }

    public override string ToString()
    {
        return $"{Type} {Name}";
    }
}

public enum UniformType
{
    Unknown,
    Float,
    Vector3,
    Matrix4,
}

public static class UniformTypeExtensions
{
    public static string GetDataType(this UniformType uniformType)
    {
        return uniformType switch
        {
            UniformType.Float => "float",
            UniformType.Vector3 => "Vector3D<float>",
            UniformType.Matrix4 => "Matrix4X4<float>",
            _ => "Unknown"
        };
    }

    public static string GetRenderParameterType(this UniformType uniformType)
    {
        return uniformType switch
        {
            UniformType.Float => "RenderingParameter.Float",
            UniformType.Vector3 => "RenderingParameter.Vector3D",
            UniformType.Matrix4 => "RenderingParameter.Matrix4X4",
            _ => "Unknown"
        };
    }
}