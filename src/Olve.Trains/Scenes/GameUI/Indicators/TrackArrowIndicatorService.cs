using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Math;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;
using Olve.Generated.Shaders;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameRendering;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.GameUI.Indicators;

public class TrackArrowIndicatorService(
    AssetLoader assetLoader,
    CameraSceneService cameraSceneService,
    RenderingManager3D renderingManager3D,
    OpenGLInstancedBufferManager instancedBufferManager,
    RenderingServiceHelper renderingServiceHelper,
    TextureLoadingManager textureLoadingManager,
    TextureEntityManager textureEntityManager) : ISceneService
{
    private float _scale = 1f;

    private readonly HashSet<Id<ArrowIndicator>> _trackArrows = new();

    private readonly Shaders.Default _shader = new()
    {
        AmbientLightColor = new Vector3D<float>(1f, 1f, 1f),
        AmbientLightIntensity = 1f,
    };

    private TextureId<RGBA>? _textureId;
    private OpenGLInstancedBufferManager.MeshInstancedRegistration _registration;
    private Shaders.Default.Instance? _currentInstance;
    private bool _dirty;

    public Result Load()
    {
        if (textureLoadingManager.LoadTexture(Textures.PolygonPrototype_Texture_01)
            .TryPickProblems(out var problems, out _textureId))
        {
            return problems.Prepend("Failed to load texture");
        }

        if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(_textureId, new TextureUploadOptions())
            .TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to register texture with OpenGL");
        }

        _shader.TextureSampler = _textureId;

        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load shader");
        }

        if (assetLoader.LoadAsset(Meshes.SM_Icon_Arrow_Small_01).TryPickProblems(out problems, out var meshData))
        {
            return problems.Prepend("Failed to load mesh");
        }

        var (vertexFloats, indices) = MeshDataMarshalHelper.Marshal<Shaders.Default.Vertex>(meshData);

        if (instancedBufferManager.CreateMeshInstanceBuffer(
                vertexFloats,
                (uint)meshData.VertexCount,
                indices,
                Shaders.Default.Vertex.ConfigureAttributes,
                ReadOnlySpan<float>.Empty,
                0,
                Shaders.Default.Instance.ConfigureAttributes,
                BufferUsageARB.DynamicDraw)
            .TryPickProblems(out problems, out var registration))
        {
            return problems.Prepend("Failed to create arrow instanced buffers");
        }

        _registration = registration;

        AABB aabbTarget = new(Vector3D<float>.Zero, Vector3D<float>.One);
        var scaleResult = AABBHelper.GetUniformScaleToFitInside(meshData, aabbTarget);
        if (scaleResult.TryPickProblems(out problems, out _scale))
        {
            return problems.Prepend("Failed to compute scale");
        }

        return Result.Success();
    }

    public Result Unload()
    {
        instancedBufferManager.DeleteMeshInstanceBuffers(_registration);
        renderingServiceHelper.UnloadShader(_shader);
        if (_textureId is { } textureId)
        {
            textureEntityManager.Unregister(textureId);
        }

        return Result.Success();
    }

    public Result Render(TimeSpan deltaTime)
    {
        if (_trackArrows.Count == 0 || _currentInstance is null)
        {
            return Result.Success();
        }

        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);

        return renderingManager3D.RenderInstanced(_shader, _registration, 1);
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

        _currentInstance = new Shaders.Default.Instance(world);
        _dirty = true;

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        if (_dirty && _currentInstance is { } instance)
        {
            var floats = new float[Shaders.Default.Instance.FloatCount];
            instance.WriteTo(floats);

            instancedBufferManager.UpdateMeshInstanceBuffer(
                _registration,
                floats,
                1,
                BufferUsageARB.DynamicDraw);

            _dirty = false;
        }

        return Result.Success();
    }
}
