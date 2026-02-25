using Olve.Engine3D;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.Primitives;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Scenes;
using Olve.Generated.Shaders;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Light;

namespace Olve.Trains.Scenes.GameRendering;

public class BuildingRenderingService(
    CameraSceneService cameraSceneService,
    GeometryManager geometryManager,
    RenderingGroupManager renderingGroupManager,
    RenderingInstanceManager renderingInstanceManager,
    RenderingServiceHelper renderingServiceHelper,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    GridService gridService,
    SceneLightService sceneLightService,
    TextureManager textureManager,
    TextureEntityManager textureEntityManager,
    TerrainRenderingService terrainRenderingService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRenderingService]);

    private readonly TextureId<RGBA> _whitePixel = textureManager.RegisterTexture(TextureData<RGBA>.Single(RGBA.White));

    private readonly Shaders.Default _shader = new()
    {
        BlendState = RenderState.Opaque,
        UColor = new Vector3D<float>(0.7f, 0.7f, 0.7f),
        UOpacity = 1.0f,
        UColorOverride = new Vector3D<float>(0, 0, 0),
        UColorMix = 0.0f,
    };

    private readonly Dictionary<Id<Building>, Id<Shaders.Default.Instance>> _instanceIds = new();

    private readonly Shaders.Default _ghostShader = new()
    {
        BlendState = RenderState.AlphaBlend,
        UColor = new Vector3D<float>(0.7f, 0.7f, 0.7f),
        UOpacity = 0.5f,
        UColorOverride = new Vector3D<float>(0, 0, 0),
        UColorMix = 0.0f,
    };

    private readonly Dictionary<Id<Building>, Id<Shaders.Default.Instance>> _ghostInstanceIds = new();

    private GroupId<Shaders.Default.Instance> _groupId = null!;
    private GroupId<Shaders.Default.Instance> _ghostGroupId = null!;

    public Result Load()
    {
        if (textureEntityManager.Register<RGBA, RGBAPixelFormat>(_whitePixel, new TextureUploadOptions())
            .TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to register white pixel texture with OpenGL");
        }

        _shader.TextureSampler = _whitePixel;
        _ghostShader.TextureSampler = _whitePixel;

        if (renderingServiceHelper.LoadShader(_shader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load building shader");
        }

        if (renderingServiceHelper.LoadShader(_ghostShader).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to load ghost building shader");
        }

        var vertices = new Shaders.Default.Vertex[UnitCube.VertexCount];
        UnitCube.Populate(vertices);

        var indices = new uint[UnitCube.IndexCount];
        UnitCube.GetIndices(indices);

        if (geometryManager.Register(vertices, indices)
            .TryPickProblems(out problems, out var geometryId))
        {
            return problems.Prepend("Failed to register building geometry");
        }

        if (renderingGroupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance>(
                geometryId, _shader, RenderState.Opaque)
            .TryPickProblems(out problems, out var groupId))
        {
            return problems.Prepend("Failed to register building group");
        }

        _groupId = groupId;

        if (renderingGroupManager.Register<Shaders.Default.Vertex, Shaders.Default.Instance>(
                geometryId, _ghostShader, RenderState.AlphaBlend)
            .TryPickProblems(out problems, out var ghostGroupId))
        {
            return problems.Prepend("Failed to register ghost building group");
        }

        _ghostGroupId = ghostGroupId;

        return Result.Success();
    }

    public Result Unload()
    {
        textureEntityManager.Unregister(_whitePixel);
        return Result.Success();
    }

    public Result Register(Id<Building> buildingId)
    {
        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found: '{0}'", building.BlueprintId);
        }

        var worldMatrix = ComputeWorldMatrix(blueprint.Footprint, building.Position);

        if (renderingInstanceManager.Add(_groupId, new Shaders.Default.Instance(worldMatrix))
            .TryPickProblems(out var problems, out var instanceId))
        {
            return problems.Prepend("Failed to add building instance for '{0}'", buildingId);
        }

        _instanceIds[buildingId] = instanceId;

        return Result.Success();
    }

    public Result RegisterGhost(
        Id<Building> ghostId,
        BuildingPosition position,
        TileFootprint footprint)
    {
        var worldMatrix = ComputeWorldMatrix(footprint, position);

        if (renderingInstanceManager.Add(_ghostGroupId, new Shaders.Default.Instance(worldMatrix))
            .TryPickProblems(out var problems, out var instanceId))
        {
            return problems.Prepend("Failed to add ghost building instance for '{0}'", ghostId);
        }

        _ghostInstanceIds[ghostId] = instanceId;

        return Result.Success();
    }

    public Result UpdateGhost(
        Id<Building> ghostId,
        BuildingPosition position,
        TileFootprint footprint)
    {
        if (!_ghostInstanceIds.TryGetValue(ghostId, out var instanceId))
        {
            return new ResultProblem("Could not find ghost instance for building '{0}'", ghostId);
        }

        var worldMatrix = ComputeWorldMatrix(footprint, position);

        if (renderingInstanceManager.Update(_ghostGroupId, instanceId, new Shaders.Default.Instance(worldMatrix))
            .TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to update ghost building instance for '{0}'", ghostId);
        }

        return Result.Success();
    }

    public void SetGhostAppearance(float opacity, Vector3D<float>? colorOverride = null, float colorMix = 0f)
    {
        _ghostShader.UOpacity = opacity;
        _ghostShader.UColorOverride = colorOverride ?? new Vector3D<float>(0, 0, 0);
        _ghostShader.UColorMix = colorMix;
    }

    public Result Unregister(Id<Building> buildingId)
    {
        if (_instanceIds.Remove(buildingId, out var instanceId))
        {
            return renderingInstanceManager.Remove(_groupId, instanceId);
        }

        if (_ghostInstanceIds.Remove(buildingId, out var ghostInstanceId))
        {
            return renderingInstanceManager.Remove(_ghostGroupId, ghostInstanceId);
        }

        return new ResultProblem("Could not find rendering instance for building '{0}'", buildingId);
    }

    public Result Update(TimeSpan deltaTime)
    {
        cameraSceneService.ApplyCameraPositionParameters(_shader);
        cameraSceneService.ApplyCameraDirectionParameters(_shader);
        sceneLightService.ApplyShaderParameters(_shader);

        cameraSceneService.ApplyCameraPositionParameters(_ghostShader);
        cameraSceneService.ApplyCameraDirectionParameters(_ghostShader);
        sceneLightService.ApplyShaderParameters(_ghostShader);

        return Result.Success();
    }

    private Matrix4X4<float> ComputeWorldMatrix(TileFootprint footprint, BuildingPosition position)
    {
        const float inset = 0f;
        var w = (float)footprint.Width;
        var h = (float)footprint.Height;
        var d = (float)footprint.Depth;
        var sw = w - inset * 2;
        var sd = d - inset * 2;
        var sh = h - inset;

        var y = gridService.ToTileOrigin(position.BottomLeft).Y;

        var rotation = position.CardinalDirection.ToYRotation();

        return Matrix4X4.CreateScale(sw, sh, sd)
               * Matrix4X4.CreateRotationY(rotation)
               * Matrix4X4.CreateTranslation(
                   position.BottomLeft.X + inset,
                   y,
                   position.BottomLeft.Z + inset)
            ;
    }
}
