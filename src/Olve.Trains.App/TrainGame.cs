using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Olve.Engine3D.Camera.Cameras;
using Olve.Engine3D.Camera.Controllers;
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;
using Olve.Engine3D.IO.Images;
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

        perspectiveCamera.View.Position = new Vector3(0, 0, 0);

        perspectiveCamera.Projection.AspectRatio = GraphicsDevice.DisplayMode.AspectRatio;
        perspectiveCamera.Projection.NearPlane = 0.1f;
        perspectiveCamera.Projection.FarPlane = 10000f;

        // Isometric Camera
        var target = perspectiveCamera.View.Position;
        var viewingDirection = Vector3.Left + Vector3.Down + Vector3.Backward;
        var aspectRatio = GraphicsDevice.DisplayMode.AspectRatio;
        const float orthographicSize = 50f;
        const float nearPlane = -10000f;
        const float farPlane =   10000f;

        var isometricOrthographicCameraController = IsometricOrthographicCameraController.Create(target, viewingDirection, orthographicSize,
            aspectRatio, nearPlane, farPlane);

        const float viewingDistance = 10f;
        const float fieldOfView = MathHelper.PiOver4;

        var isometricPerspectiveCameraController = IsometricPerspectiveCameraController.Create(target,
            viewingDirection, viewingDistance, fieldOfView, aspectRatio, 0.1f, 10_000f);

        _cameraControllers.Add(("isometric orthographic", isometricOrthographicCameraController));
        _cameraControllers.Add(("isometric perspective", isometricPerspectiveCameraController));
        _cameraControllers.Add(("perspective", new PerspectiveCameraController(perspectiveCamera)));


        base.Initialize();
    }

    protected override void LoadContent()
    {
        var basicEffect = new BasicEffect(GraphicsDevice)
        {
            TextureEnabled = false,
            LightingEnabled = false,
            VertexColorEnabled = false,
            DiffuseColor = new Vector3(0, 0.5f, 0),
        };
        
        var terrainResult = TerrainLoader.LoadTerrain(new MapFilePath("./Content/maps/map_01.ora"));
        if (terrainResult.TryPickProblems(out var problems, out var terrainMesh))
        {
            foreach (var problem in problems)
            {
                Console.WriteLine(problem.ToDebugString());
            }
            
            throw new InvalidOperationException("Could not load terrain.");
        }
        
        var drawableMesh = new TriMeshDrawable(GraphicsDevice, terrainMesh, [basicEffect]);
        _drawables.Add(drawableMesh);
        
        foreach (var drawable in _drawables)
        {
            drawable.Initialize();
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

        GraphicsDevice.RasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
        };

        if (CameraController is {} cameraController)
        {
            var view = cameraController.Camera.GetViewMatrix();
            var projection = cameraController.Camera.GetProjectionMatrix();

            foreach (var drawable in _drawables)
            {
                drawable.Draw(view, projection);
            }
        }

        base.Draw(gameTime);
    }
}