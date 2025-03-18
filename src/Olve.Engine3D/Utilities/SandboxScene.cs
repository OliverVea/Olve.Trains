using System.Drawing;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Camera.Cameras;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Shaders;
using Olve.Engine3D.Input;
using Olve.Engine3D.Input.InputSchemes;
using Olve.Engine3D.Scenes;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Shader = Olve.Engine3D.Graphics.Shader;

namespace Olve.Engine3D.Utilities;

public class SandboxScene : Scene
{
    public ShaderSource<VertexShaderAsset> VertexShader { get; set; } = new(new("direct"), VertexShaderSource);
    public ShaderSource<FragmentShaderAsset> FragmentShader { get; set; } = new(new("direct"), FragmentShaderSource);

    public ShaderSource<VertexShaderAsset> RayVertexShader { get; set; } = new(new("direct"), RayVertexShaderSource);
    public ShaderSource<FragmentShaderAsset> RayFragmentShader { get; set; } = new(new("direct"), RayFragmentShaderSource);

    private Shader _rayShader = null!;

    public static readonly SceneId SceneId = new("SandboxScene");
    public override SceneId Id => SceneId;

    private GameObjectRenderer _renderer = null!;
    private PerspectiveCameraController _perspectiveCameraController = null!;
    private readonly List<ICameraScheme> _cameraSchemes = [];

    private readonly Queue<Ray3D<float>> _rays = new();

    private CameraMovementInput _cameraMovementInput = new();

    public override Result Load()
    {
        if (EntityHelper.CreateQuad(VertexShader, FragmentShader).TryPickProblems(out var problems, out var quad))
        {
            return problems.Prepend("Failed creating quad");
        }

        _renderer = new GameObjectRenderer();
        _renderer.Register(quad);

        FirstPersonView view = new() { Position = new Vector3D<float>(0, 0, -5) };
        PerspectiveProjection projection = new() { AspectRatio = 16f / 9f, NearPlane = 0.1f, FarPlane = 1000 };

        PerspectiveCamera camera = new(view, projection);

        _perspectiveCameraController = new PerspectiveCameraController(camera);

        _cameraSchemes.Add(new WasdMovement());
        _cameraSchemes.Add(new MouseLook());

        if (ShaderLoader.Create(RayVertexShader, RayFragmentShader).TryPickProblems(out var rayProblems, out _rayShader!))
        {
            return rayProblems.Prepend("Failed creating ray shader");
        }

        _rayShader.ViewMatrixUniformName = "view";
        _rayShader.ProjectionMatrixUniformName = "proj";

        return Result.Success();
    }

    public override Result<PassInput> Input()
    {
        _cameraMovementInput = new CameraMovementInput();

        foreach (var scheme in _cameraSchemes)
        {
            _cameraMovementInput += scheme.GetMovementInput();
        }

        if (GameManager.MouseManager.State.IsButtonPressed(MouseButton.Left))
        {
            var rayResult = _perspectiveCameraController.Camera.GetRay(new Vector2D<float>(0f, 0f));
            if (rayResult.TryPickProblems(out var problems, out var ray))
            {
                return problems.Prepend("Failed creating ray");
            }

            _rays.Enqueue(ray);
            if (_rays.Count > 10)
            {
                _rays.Dequeue();
            }
        }

        if (GameManager.KeyboardManager.State.IsKeyPressed(Key.C))
        {
            _rays.Clear();
        }

        return PassInput.Pass;
    }

    public override Result Update(TimeSpan deltaTime)
    {
        _perspectiveCameraController.Move(_cameraMovementInput.Direction, deltaTime);
        _perspectiveCameraController.Rotate(_cameraMovementInput.Rotation, deltaTime);
        _perspectiveCameraController.Zoom(_cameraMovementInput.Zoom, deltaTime);

        return Result.Success();
    }

    public override Result Render(TimeSpan deltaTime)
    {
        GameManager.Gl.ClearColor(Color.CornflowerBlue);
        GameManager.Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var viewMatrix = _perspectiveCameraController.Camera.GetViewMatrix();
        var projectionMatrix = _perspectiveCameraController.Camera.GetProjectionMatrix();

        _renderer.Render(viewMatrix, projectionMatrix);

        foreach (var ray in _rays)
        {
            ray.Render(_rayShader, viewMatrix, projectionMatrix);
        }

        return Result.Success();
    }


    private const string VertexShaderSource =
        """
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        uniform mat4 world;
        uniform mat4 view;
        uniform mat4 projection;

        void main()
        {
          gl_Position = projection * view * world * vec4(aPosition, 1.0);
        }
        """;

    private const string FragmentShaderSource =
        """
        #version 330 core

        out vec4 out_color;

        void main()
        {
            out_color = vec4(1.0, 0.5, 0.2, 1.0);
        }
        """;


    private const string RayVertexShaderSource =
        """
        #version 330 core
        
        layout(location = 0) in vec3 aPos; // Input vertex position
        
        uniform mat4 view;
        uniform mat4 projection;
        
        void main()
        {
            gl_Position = projection * view * vec4(aPos, 1.0);
        }
        """;

    private const string RayFragmentShaderSource =
        """
        #version 330 core
        
        out vec4 FragColor;
        
        uniform vec4 uLineColor = vec4(1.0, 0.0, 0.0, 1.0); // Line color
        
        void main()
        {
            FragColor = uLineColor;
        }
        """;
}