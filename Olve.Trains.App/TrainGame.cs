using System;
using System.Collections.Generic;
using Engine;
using Engine.Camera.Cameras;
using Engine.Camera.Controllers;
using Engine.Camera.Projections;
using Engine.Camera.Views;
using Engine.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PrimitiveType = Microsoft.Xna.Framework.Graphics.PrimitiveType;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Olve.Trains.App;

public class TrainGame : Game
{
    private readonly GraphicsDeviceManager _graphicsDeviceManager;

    private readonly List<(string Label, ICameraController CameraController)> _cameraControllers = [];
    private int _currentCameraControllerIndex;

    private ICameraController? CameraController => _currentCameraControllerIndex < _cameraControllers.Count
        ? _cameraControllers[_currentCameraControllerIndex].CameraController
        : null;

    private readonly List<GameObject> _gameObjects = [];
    private readonly List<IDrawable> _drawables = [];

    public TrainGame()
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphicsDeviceManager.PreferredBackBufferWidth = 1920;
        _graphicsDeviceManager.PreferredBackBufferHeight = 1080;
        _graphicsDeviceManager.ApplyChanges();

        // Perspective Camera
        var perspectiveCamera = new PerspectiveCamera(
            new FirstPersonView(),
            new PerspectiveProjection());

        perspectiveCamera.View.Position = new Vector3(0, 0, 100f);

        perspectiveCamera.Projection.AspectRatio = GraphicsDevice.DisplayMode.AspectRatio;
        perspectiveCamera.Projection.NearPlane = 0.1f;
        perspectiveCamera.Projection.FarPlane = 10000f;


        // Isometric Camera
        var target = Vector3.Zero;
        var viewingDirection = Vector3.Left + Vector3.Down + Vector3.Backward;
        var aspectRatio = GraphicsDevice.DisplayMode.AspectRatio;
        const float orthographicSize = 50f;
        const float nearPlane = -1000f;
        const float farPlane = 1000f;

        var isometricCameraController = IsometricCameraController.Create(target, viewingDirection, orthographicSize,
            aspectRatio, nearPlane, farPlane);

        _cameraControllers.Add(("isometric", isometricCameraController));
        _cameraControllers.Add(("perspective", new PerspectiveCameraController(perspectiveCamera)));

        _drawables.Add(new WorldAxes(GraphicsDevice));

        base.Initialize();
    }

    protected override void LoadContent()
    {
        var model = Content.Load<Model>("models/SM_Veh_Bullet_Carriage_01");

        var texture = Content.Load<Texture2D>("models/SimpleTrains_Texture_01");

        var effect = model.Meshes[0].MeshParts[0].Effect;

        if (effect is not BasicEffect basicEffect)
        {
            throw new Exception("Effect is not BasicEffect");
        }

        basicEffect.TextureEnabled = true;
        basicEffect.Texture = texture;
        basicEffect.LightingEnabled = false;

        var gameObject = new GameObject
        {
            Model = model,
            Transform = new Transform
            {
                Position = new Vector3(0, -4, 0)
            }
        };

        _gameObjects.Add(gameObject);
    }

    private bool _isCameraSwitched;

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (keyboardState.IsKeyDown(Keys.P))
        {
            _isCameraSwitched = true;
        }
        else if (_isCameraSwitched)
        {
            _currentCameraControllerIndex = (_currentCameraControllerIndex + 1) % _cameraControllers.Count;
            _isCameraSwitched = false;
        }

        CameraController?.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (CameraController is {} cameraController)
        {
            foreach (var gameObject in _gameObjects)
            {
                var world = gameObject.Transform.GetWorldMatrix();
                var view = cameraController.Camera.GetViewMatrix();
                var projection = cameraController.Camera.GetProjectionMatrix();

                gameObject.Model.Draw(world, view, projection);
            }
        }

        foreach (var drawable in _drawables)
        {
            drawable.Draw(Matrix.Identity, CameraController?.Camera.GetViewMatrix() ?? Matrix.Identity,
                CameraController?.Camera.GetProjectionMatrix() ?? Matrix.Identity);
        }

        base.Draw(gameTime);
    }
}


public interface IDrawable
{
    void Draw(Matrix world, Matrix view, Matrix projection);
}

public class WorldAxes(GraphicsDevice graphicsDevice, int axisLength = 10) : IDrawable
{
    private readonly VertexPositionColor[] _vertices =
    [
        new(new Vector3(0, 0, 0), Color.Red),
        new(Vector3.Right * axisLength, Color.Red),
        new(new Vector3(0, 0, 0), Color.Green),
        new(Vector3.Up * axisLength, Color.Green),
        new(new Vector3(0, 0, 0), Color.Blue),
        new(Vector3.Forward * axisLength, Color.Blue)
    ];

    private readonly BasicEffect _basicEffect = new(graphicsDevice)
    {
        VertexColorEnabled = true
    };

    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        _basicEffect.World = world;
        _basicEffect.View = view;
        _basicEffect.Projection = projection;

        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _vertices, 0, _vertices.Length / 2);
        }
    }
}

