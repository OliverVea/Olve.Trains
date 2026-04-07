using Olve.Engine3D;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Math;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Generated.Meshes;
using Olve.Generated.Textures;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Environment;

namespace Olve.Trains.Scenes.GameLogic.Terrain;

public class TerrainService(
    CollisionSystem collisionSystem,
    EnvironmentalObjectService environmentalObjectService) : ISceneService
{
    private TerrainData? _terrain;
    public TerrainData Terrain => _terrain ?? throw new NotInitializedException<TerrainData>();
    public float TileStepHeight => _terrain?.Heightmap.Step ?? 1f;
    public int TilesPerMeterHeight => (int)float.Round(1 /  TileStepHeight);

    internal HeightmapData? HeightmapOverride { get; set; }
    internal int TreeSeed { get; set; } = 42;
    internal double TreeSpawnProbability { get; set; } = 0.08;

    public Result Load()
    {
        var heightmap = HeightmapOverride ?? GameSceneArguments.DefaultHeightmap();

        _terrain = new TerrainData
        {
            Heightmap = heightmap
        };

        collisionSystem.RegisterHeightmapCollider(heightmap, ColliderGroups.Terrain);

        PlaceEnvironmentalObjects(heightmap);

        return Result.Success();
    }

    private static readonly AssetPath<MeshData>[] TreeMeshes =
    [
        Meshes.SM_Env_Tree_01,
        Meshes.SM_Env_Tree_02,
        Meshes.SM_Env_Tree_03,
        Meshes.SM_Env_Tree_04,
    ];

    private static readonly AssetPath<TextureData<RGBA>>[] TreeTextures =
    [
        Textures.SimpleTrains_Texture_01,
        Textures.SimpleTrains_Texture_02,
    ];

    private void PlaceEnvironmentalObjects(HeightmapData heightmap)
    {
        var random = new Random(TreeSeed);

        for (var z = 0; z < heightmap.Length; z++)
        {
            for (var x = 0; x < heightmap.Width; x++)
            {
                if (random.NextDouble() > TreeSpawnProbability) continue;

                var height = heightmap.Heights[z * heightmap.Width + x];
                var y = height * heightmap.Step;
                var offsetX = x + (float)random.NextDouble();
                var offsetZ = z + (float)random.NextDouble();
                var yaw = (float)(random.NextDouble() * 2.0 * double.Pi);
                var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, yaw);
                var meshPath = TreeMeshes[random.Next(TreeMeshes.Length)];
                var texturePath = TreeTextures[random.Next(TreeTextures.Length)];

                var position = new Position3D(
                    new Vector3D<float>(offsetX, y, offsetZ),
                    rotation);

                environmentalObjectService.AddObject(EnvironmentalObjectBlueprintCatalog.Tree, position, meshPath, texturePath);
            }
        }
    }
}
