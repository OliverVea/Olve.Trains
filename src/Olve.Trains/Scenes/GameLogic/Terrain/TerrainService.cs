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

    public Result Load()
    {
        const int length = 50;
        const int width = 50;

        var heights = new int[length * width];
        Array.Fill(heights, 1);

        for (var z = 3; z <= 6; z++)
        {
            for (var x = 12; x <= 15; x++)
            {
                heights[z * width + x] = 2;
            }
        }

        HeightmapData heightmap = new()
        {
            Heights = heights,
            Width = width,
            Length = length,
            Step = 0.125f
        };

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
        var random = new Random(42);

        for (var z = 0; z < heightmap.Length; z++)
        {
            for (var x = 0; x < heightmap.Width; x++)
            {
                if (random.NextDouble() > 0.08) continue;

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
