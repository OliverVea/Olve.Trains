using System.Collections.Generic;
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
    private List<GameObject> _gameObjects = [];

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


        base.Initialize();
    }

    protected override void LoadContent()
    {
        var model = Content.Load<Model>("Models/Train");

        var gameObject = new GameObject
        {
            Model = model
        };

        _gameObjects.Add(gameObject);
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
        GraphicsDevice.Clear(Color.Black);

        foreach (var gameObject in _gameObjects)
        {
            var world = gameObject.Transform.GetWorldMatrix();
            var view = _cameraController.Camera.GetViewMatrix();
            var projection = _cameraController.Camera.GetProjectionMatrix();

            gameObject.Model.Draw(world, view, projection);
        }

        base.Draw(gameTime);
    }
}



