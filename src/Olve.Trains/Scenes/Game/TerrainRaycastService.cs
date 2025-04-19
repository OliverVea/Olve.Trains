using Olve.Engine3D.Camera;
using Olve.Engine3D.Input;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public class TerrainRaycastService(MouseManager mouseManager, CameraSceneService cameraSceneService, TerrainService terrainService) : SceneService
{
    private HeightmapRaycaster? _heightmapRaycaster;
    
    public Ray3D<float>? MouseRay { get; set; }
    public Vector3D<float>? TerrainIntersection { get; set; }
    public Vector3D<float>? TerrainIntersectionTileCenter { get; set; }
    public Vector2D<int>? TerrainIntersectionIndex { get; set; }
    
    public override int Priority => GetPriorityFromDependencies([cameraSceneService, terrainService]);

    public override Result Load()
    {
        if (terrainService.Terrain is not { } terrain)
        {
            return new ResultProblem("TerrainService.Terrain is null");
        }

        _heightmapRaycaster = new HeightmapRaycaster(terrain.Heightmap);

        return Result.Success();
    }

    public override Result<Pass> Input(TimeSpan deltaTime)
    {
        var mouseCoordinates = mouseManager.State.NormalizedPosition;

        if (mouseCoordinates.X is >= -1 and <= 1
            && mouseCoordinates.Y is >= -1 and <= 1
            && cameraSceneService.Camera.GetRay(mouseCoordinates).TryPickValue(out var mouseRay))
        {
            MouseRay = mouseRay;
        }
        else
        {
            MouseRay = null;
        }

        return Pass.Pass;
    }
    
    public override Result Update(TimeSpan deltaTime)
    {
        TerrainIntersection = null;
        
        if (_heightmapRaycaster is null)
        {
            return new ResultProblem("HeightmapRaycaster is null");
        }
        
        if (MouseRay is null)
        {
            return Result.Success();
        }
        
        if (_heightmapRaycaster.TryRaycast(MouseRay.Value, out var intersection))
        {
            TerrainIntersection = intersection;
            
            // Convert to 2D index
            var x = (int)intersection.Value.X;
            var z = (int)intersection.Value.Z;
            
            TerrainIntersectionIndex = new Vector2D<int>(x, z);
            TerrainIntersectionTileCenter = new Vector3D<float>(x + 0.5f, intersection.Value.Y, z + 0.5f);
        }

        return Result.Success();
    }
}