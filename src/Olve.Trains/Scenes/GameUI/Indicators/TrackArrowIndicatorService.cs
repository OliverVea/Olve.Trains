using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Math;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Shared.Rendering;

namespace Olve.Trains.Scenes.GameUI.Indicators;

public class TrackArrowIndicatorService(
    AssetLoader assetLoader,
    CameraSceneService cameraSceneService,
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    RenderingServiceHelper renderingServiceHelper,
    TextureLoadingManager textureLoadingManager,
    TextureEntityManager textureEntityManager,
    SharedRenderingService sharedRenderingService) : ISceneService
{
    private float _scale = 1f;

    private readonly HashSet<Id<ArrowIndicator>> _trackArrows = new();

    private readonly Shaders.Default _shader = new()
    {
        AmbientLightColor = new Vector3D<float>(1f, 1f, 1f),
        AmbientLightIntensity = 1f,
        UColor = new Vector3D<float>(1f, 1f, 1f),
        UOpacity = 1.0f,
        UColorMix = 0f,
    };

    private GroupId<Shaders.Default.Instance> _groupId = null!;
    private Id<Shaders.Default.Instance>? _instanceId;

    public Result Load()
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

        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader");
        }

        if (assetLoader.LoadAsset(Meshes.SM_Icon_Arrow_Small_01).TryPickProblems(out problems, out var meshData))
        {
            return problems.Prepend("Failed to load mesh");
        }

        // Populate typed vertices from mesh data
        var vertices = new Shaders.Default.Vertex[meshData.VertexCount];
        meshData.Populate(vertices);

        // Extract indices
        var indices = new uint[meshData.Indices.Length * 3];
        for (var i = 0; i < meshData.Indices.Length; i++)
        {
            indices[i * 3] = meshData.Indices[i].A;
            indices[i * 3 + 1] = meshData.Indices[i].B;
            indices[i * 3 + 2] = meshData.Indices[i].C;
        }

        // Register geometry
        if (geometryManager.Register<Shaders.Default.Vertex>(vertices, indices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register arrow geometry");
        }

        // Register group
        if (renderingGroupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance, IDefaultFrameFormat>(
                geometryId, _shader, sharedRenderingService.MainPass, RenderState.Opaque)
            .TryPickProblems(out problems, out var groupId))
        {
            return problems.Prepend("Failed to register arrow group");
        }

        _groupId = groupId;

        AABB aabbTarget = new(Vector3D<float>.Zero, Vector3D<float>.One);
        var scaleResult = AABBHelper.GetUniformScaleToFitInside(meshData, aabbTarget);
        if (scaleResult.TryPickProblems(out problems, out _scale))
        {
            return problems.Prepend("Failed to compute scale");
        }

        return Result.Success();
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

        if (_instanceId is null)
        {
            if (renderingInstanceManager.Add(_groupId, new Shaders.Default.Instance(new Matrix4X4<float>()))
                .TryPickProblems(out var problems, out var instanceId))
            {
                return problems.Prepend("Failed to add arrow instance");
            }

            _instanceId = instanceId;
        }

        return Result.Success();
    }

    public Result Hide(Id<ArrowIndicator> trackArrowIndicatorId)
    {
        _trackArrows.Remove(trackArrowIndicatorId);

        if (_trackArrows.Count == 0 && _instanceId is { } instanceId)
        {
            if (renderingInstanceManager.Remove(_groupId, instanceId).TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to remove arrow instance");
            }

            _instanceId = null;
        }

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

        if (_instanceId is { } instanceId)
        {
            if (renderingInstanceManager.Update(_groupId, instanceId, new Shaders.Default.Instance(world))
                .TryPickProblems(out var problems))
            {
                return problems.Prepend("Failed to update arrow instance");
            }
        }

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);

        return Result.Success();
    }
}
