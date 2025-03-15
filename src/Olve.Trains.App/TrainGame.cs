using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Olve.Engine3D;
using Olve.Engine3D.Camera.Cameras;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.Objects;
using Olve.Trains.Terrain;
using IDrawable = Olve.Engine3D.Graphics.IDrawable;
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

        perspectiveCamera.View.Position = new Vector3(500, 0, 500);

        perspectiveCamera.Projection.AspectRatio = GraphicsDevice.DisplayMode.AspectRatio;
        perspectiveCamera.Projection.NearPlane = 0.1f;
        perspectiveCamera.Projection.FarPlane = 10000f;


        // Isometric Camera
        var target = perspectiveCamera.View.Position;
        var viewingDirection = Vector3.Left + Vector3.Down + Vector3.Backward;
        var aspectRatio = GraphicsDevice.DisplayMode.AspectRatio;
        const float orthographicSize = 50f;
        const float nearPlane = -1000f;
        const float farPlane = 100000f;

        var isometricOrthographicCameraController = IsometricOrthographicCameraController.Create(target, viewingDirection, orthographicSize,
            aspectRatio, nearPlane, farPlane);

        const float viewingDistance = 10f;
        const float fieldOfView = MathHelper.PiOver4;

        var isometricPerspectiveCameraController = IsometricPerspectiveCameraController.Create(target,
            viewingDirection, viewingDistance, fieldOfView, aspectRatio, 0.1f, 10_000f);

        _cameraControllers.Add(("isometric orthographic", isometricOrthographicCameraController));
        _cameraControllers.Add(("isometric perspective", isometricPerspectiveCameraController));
        _cameraControllers.Add(("perspective", new PerspectiveCameraController(perspectiveCamera)));

        foreach (var drawable in _drawables)
        {
            drawable.Initialize();
        }

        base.Initialize();
    }

    protected override void LoadContent()
    {
        /*
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
        */
        
        var terrainResult = TerrainLoader.LoadTerrain(new MapFilePath("./Content/maps/map-01.ora"));
        if (terrainResult.TryPickProblems(out var problems, out var terrain))
        {
            foreach (var problem in problems)
            {
                Console.WriteLine(problem.ToDebugString());
            }
        }
        else
        {
            TerrainDrawable terrainDrawable = new(GraphicsDevice, terrain);
            _drawables.Add(terrainDrawable);
        }
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
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GraphicsDevice.RasterizerState = new RasterizerState()
        {
            CullMode = CullMode.None
        };

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