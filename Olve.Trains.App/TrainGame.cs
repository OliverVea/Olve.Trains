using Engine.Camera.Cameras;
using Engine.Camera.Controllers;
using Engine.Camera.Projections;
using Engine.Camera.Views;
using Engine.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Olve.Trains.App;

public class TrainGame : Game
{
    private GraphicsDeviceManager _graphicsDeviceManager;

    private ICameraController _cameraController;
    private GameObject _gameObject;

    private BasicEffect _basicEffect;

    private VertexPositionColor[] _vertexPositionColors;
    private VertexBuffer _vertexBuffer;


    public TrainGame()
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Perspective Camera
        var perspectiveCamera = new PerspectiveCamera(
            new FirstPersonView(),
            new PerspectiveProjection());

        perspectiveCamera.View.Position = new Vector3(0, 0, 100f);

        perspectiveCamera.Projection.AspectRatio = GraphicsDevice.DisplayMode.AspectRatio;
        perspectiveCamera.Projection.NearPlane = 1f;
        perspectiveCamera.Projection.FarPlane = 1000f;

        _cameraController = new PerspectiveCameraController(perspectiveCamera);

        // Isometric Camera
        /*
        var isometricCamera = new IsometricCamera(
            new IsometricView(),
            new OrthographicProjection());

        var direction = Vector3.Normalize(Vector3.Forward + Vector3.Down + Vector3.Left);
        var target = Vector3.Zero;

        isometricCamera.View.Position = target + direction * 100f;
        isometricCamera.View.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(Vector3.Zero, direction, Vector3.Up));

        isometricCamera.Projection.AspectRatio = GraphicsDevice.DisplayMode.AspectRatio;
        isometricCamera.Projection.NearPlane = 1f;
        isometricCamera.Projection.FarPlane = 1000f;

        _cameraController = new IsometricCameraController(isometricCamera);
        */

        // Basic Effect
        _basicEffect = new BasicEffect(GraphicsDevice);
        _basicEffect.Alpha = 1f;
        _basicEffect.VertexColorEnabled = true;
        _basicEffect.LightingEnabled = false;

        // Vertices
        _vertexPositionColors =
        [
            new VertexPositionColor(new (0, 20, 0), Color.Red),
            new VertexPositionColor(new (-20, -20, 0), Color.Green),
            new VertexPositionColor(new (20, -20, 0), Color.Blue),
        ];

        _vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), 3, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(_vertexPositionColors);

        _gameObject = new GameObject();

        base.Initialize();
    }

    protected override void LoadContent()
    {

    }


    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        _cameraController.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _basicEffect.Projection = _cameraController.Camera.GetProjectionMatrix();
        _basicEffect.View = _cameraController.Camera.GetViewMatrix();
        _basicEffect.World = _gameObject.Transform.GetWorldMatrix();

        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.SetVertexBuffer(_vertexBuffer);

        RasterizerState rasterizerState = new()
        {
            CullMode = CullMode.None
        };

        GraphicsDevice.RasterizerState = rasterizerState;

        foreach (var effectPass in _basicEffect.CurrentTechnique.Passes)
        {
            effectPass.Apply();

            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 3);
        }

        base.Draw(gameTime);
    }
}



