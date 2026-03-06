using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public readonly record struct BuildingMeshProperties(
    AssetPath<MeshData>? MeshPath,
    Matrix4X4<float> ModelTransform);

public sealed class BuildingMeshBlueprintService
    : AbstractBlueprintPropertiesService<BuildingMeshProperties>;
