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

namespace Olve.Trains.App;

public class TrainGame : Game
{
    private GraphicsDeviceManager _graphicsDeviceManager;

    private readonly List<(string Label, ICameraController CameraController)> _cameraControllers = [];
    private int _currentCameraControllerIndex = 0;

    private ICameraController? CameraController => _currentCameraControllerIndex < _cameraControllers.Count
        ? _cameraControllers[_currentCameraControllerIndex].CameraController
        : null;

    private List<GameObject> _gameObjects = [];

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
        var isometricCamera = new IsometricOrthographicCamera(
            new IsometricView(),
            new OrthographicProjection());

        var target = Vector3.Zero;
        var viewingDirection = Vector3.Normalize(new Vector3(-1, -1, 1));
        const float distance = 1;

        var cameraPosition = target - viewingDirection * distance;

        isometricCamera.View.Position = cameraPosition;

        var cameraRotation = Matrix.CreateLookAt(cameraPosition, target, Vector3.Up);
        isometricCamera.View.Rotation = Quaternion.CreateFromRotationMatrix(cameraRotation);

        isometricCamera.Projection.AspectRatio = GraphicsDevice.DisplayMode.AspectRatio;
        isometricCamera.Projection.NearPlane = -1000f;
        isometricCamera.Projection.FarPlane = 1000f;

        _cameraControllers.Add(("isometric", new IsometricCameraController(isometricCamera)));
        _cameraControllers.Add(("perspective", new PerspectiveCameraController(perspectiveCamera)));

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

    private bool _isCameraSwitched = false;


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

        // Draw axes
        var axes = new[]
        {
            new VertexPositionColor(new Vector3(0, 0, 0), Color.Red),
            new VertexPositionColor(new Vector3(10, 0, 0), Color.Red),
            new VertexPositionColor(new Vector3(0, 0, 0), Color.Green),
            new VertexPositionColor(new Vector3(0, 10, 0), Color.Green),
            new VertexPositionColor(new Vector3(0, 0, 0), Color.Blue),
            new VertexPositionColor(new Vector3(0, 0, 10), Color.Blue),
        };

        var basicEffect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            World = Matrix.Identity,
            View = CameraController?.Camera.GetViewMatrix() ?? Matrix.Identity,
            Projection = CameraController?.Camera.GetProjectionMatrix() ?? Matrix.Identity
        };

        foreach (var pass in basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, axes, 0, axes.Length / 2);
        }

        base.Draw(gameTime);
    }
}




