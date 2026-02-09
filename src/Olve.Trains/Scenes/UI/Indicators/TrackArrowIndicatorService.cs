using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Math;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Generated.Meshes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Logging;
using Olve.Trains.Scenes.Rendering;

namespace Olve.Trains.Scenes.UI.Indicators;

public class TrackArrowIndicatorService(
    ILoggingManager loggingManager,
    AssetLoader assetLoader,
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    RenderingServiceHelper renderingServiceHelper,
    TextureLoadingManager textureLoadingManager,
    TextureEntityManager textureEntityManager) : SceneService(loggingManager)
{
    private float _scale = 1f;

    private readonly HashSet<Id<ArrowIndicator>> _trackArrows = new();

    private readonly Shaders.Default _shader = new()
    {
        AmbientLightColor = new Vector3D<float>(1f, 1f, 1f),
        AmbientLightIntensity = 1f,
    };

    private GeometryId _geometryId;
    private RenderingInstanceId InstanceId { get; set; }

    private Result LoadShader(IShader shader) => renderingServiceHelper.LoadShader(shader);

    protected override Result OnLoad()
    {
        if (textureLoadingManager.LoadTexture(Textures.PolygonPrototype_Texture_01)
            .TryPickProblems(out var problems, out var textureId))
        {
            return problems.Prepend("Failed to load texture");
        }

        if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(textureId, new TextureUploadOptions())
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to register texture with OpenGL");
        }

        _shader.TextureSampler = textureId;

        if (LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader");
        }

        if (assetLoader.LoadAsset(Meshes.SM_Icon_Arrow_Small_01).TryPickProblems(out problems, out var meshData))
        {
            return problems.Prepend("Failed to load mesh");
        }

        // Convert MeshData to Shaders.Default.Vertex[] and register geometry
        // TODO: investigate this
        var vertices = new Shaders.Default.Vertex[meshData.VertexCount];
        for (var i = 0; i < meshData.VertexCount; i++)
            vertices[i] = new(meshData.Positions[i], meshData.Normals[i], meshData.TextureCoordinates[i]);

        var indices = new uint[meshData.Indices.Length * 3];
        for (var i = 0; i < meshData.Indices.Length; i++)
        {
            indices[i * 3] = meshData.Indices[i].A;
            indices[i * 3 + 1] = meshData.Indices[i].B;
            indices[i * 3 + 2] = meshData.Indices[i].C;
        }

        if (renderingManager3D.RegisterGeometry<Shaders.Default.Vertex>(vertices, indices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register geometry");
        }

        _geometryId = geometryId;

        AABB aabbTarget = new(Vector3D<float>.Zero, Vector3D<float>.One);
        var scaleResult = AABBHelper.GetUniformScaleToFitInside(meshData, aabbTarget);
        if (scaleResult.TryPickProblems(out problems, out _scale))
        {
            return problems.Prepend("Failed to compute scale");
        }

        if (renderingManager3D.RegisterInstance(_geometryId, _shader.RenderingId, new Matrix4X4<float>())
            .TryPickProblems(out problems, out var instanceId))
        {
            return problems.Prepend("Failed to register mesh");
        }

        InstanceId = instanceId;

        return Result.Success();
    }

    protected override Result OnRender(TimeSpan deltaTime)
    {
        if (_trackArrows.Count == 0)
        {
            return Result.Success();
        }

        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);

        return renderingManager3D.Render(_shader);
    }

    public Result<Id<ArrowIndicator>> AddArrowIndicator()
    {
        return Id.New<ArrowIndicator>();
    }

    public DeletionResult RemoveArrowIndicator(Id<ArrowIndicator> trackArrowIndicatorId)
    {
        return trackArrowIndicatorId.Value != default
            ? DeletionResult.Success()
            : DeletionResult.NotFound();
    }

    public Result Show(Id<ArrowIndicator> trackArrowIndicatorId)
    {
        _trackArrows.Add(trackArrowIndicatorId);
        return Result.Success();
    }

    public Result Hide(Id<ArrowIndicator> trackArrowIndicatorId)
    {
        _trackArrows.Remove(trackArrowIndicatorId);
        return Result.Success();
    }

    public Result SetPosition(Id<ArrowIndicator> trackArrowIndicatorId, Vector3D<float> position, Vector3D<float> direction)
    {
        var yOffset = Vector3D<float>.UnitY * 0.15f;

        Vector2D<float> currentTangent2d = new(direction.X, direction.Z);

        var yRotation = -float.Atan2(currentTangent2d.Y, currentTangent2d.X) + float.Pi / 2f;

        var world = Matrix4X4.CreateScale(new Vector3D<float>(0.7f, 0.7f, 0.2f) * _scale) *
               Matrix4X4.CreateRotationX(float.Pi / 2f) *
               Matrix4X4.CreateRotationY(yRotation) *
               Matrix4X4.CreateTranslation(position + yOffset);

        return renderingManager3D.SetInstanceWorld(InstanceId, world);
    }
}
