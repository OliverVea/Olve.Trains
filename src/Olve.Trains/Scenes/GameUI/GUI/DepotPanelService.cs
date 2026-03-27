using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Depots;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class DepotPanelService(
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService,
    MouseRaycastService mouseRaycastService,
    MouseManager mouseManager,
    BuildingCollisionService buildingCollisionService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    DepotService depotService,
    TrainService trainService,
    TrainPositionService trainPositionService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([mouseRaycastService]);

    private Layouts.DepotPanel? _panel;
    private Id<GuiElementRegistrations> _panelRegistrationId;
    private Id<GuiAnchor> _anchorId;
    private bool _isOpen;
    private bool _clickedThisFrame;
    private Id<Building> _buildingId;
    private Depot _depot;

    public Result Load()
    {
        guiActivationService.GuiElementActivated.Subscribe(OnGuiElementActivated);
        return Result.Success();
    }

    public Result Unload()
    {
        guiActivationService.GuiElementActivated.Unsubscribe(OnGuiElementActivated);

        if (_isOpen)
        {
            ClosePanel();
        }

        return Result.Success();
    }

    public Result<Pass> Input()
    {
        _clickedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        return Pass.Pass;
    }

    public Result Update()
    {
        if (_clickedThisFrame)
        {
            foreach (var hit in mouseRaycastService.Hits)
            {
                if (hit.Group != ColliderGroups.Building) continue;

                if (buildingCollisionService.TryGetBuildingId(hit.ColliderId, out var buildingId)
                    && depotService.TryGetDepot(buildingId, out _))
                {
                    OpenPanel(buildingId);
                }

                break;
            }
        }

        return Result.Success();
    }

    private Result OpenPanel(Id<Building> buildingId)
    {
        if (_isOpen)
        {
            if (_buildingId == buildingId)
            {
                return ClosePanel();
            }

            ClosePanel();
        }

        _buildingId = buildingId;

        if (!depotService.TryGetDepot(buildingId, out _depot))
        {
            return new ResultProblem("Depot not found for building '{0}'", buildingId);
        }

        _panel = Layouts.BuildDepotPanel();

        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopRight, GrowthDirection.DownLeft, depth: 10)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        UpdateDepotName();

        if (guiElementService.RegisterElementAndChildren(_anchorId, _panel.Overlay)
            .TryPickProblems(out problems, out _panelRegistrationId))
        {
            guiAnchorService.UnregisterAnchor(_anchorId);
            return problems;
        }

        _isOpen = true;
        return Result.Success();
    }

    private Result ClosePanel()
    {
        guiElementService.UnregisterElementAndChildren(_panelRegistrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);
        _isOpen = false;
        _panel = null;
        return Result.Success();
    }

    private void UpdateDepotName()
    {
        if (_panel is null) return;

        if (buildingService.TryGetBuilding(_buildingId, out var building)
            && buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            _panel.DepotName.Content = blueprint.Description;
        }
    }

    private Result CreateTrain()
    {
        var name = $"Train {trainService.Count + 1}";

        if (trainService.AddTrain(name).TryPickProblems(out var problems, out var trainId))
        {
            return problems;
        }

        TrackPoint trackPoint = new(_depot.TrackId, 0f);
        TrainTrackPosition position = new(trackPoint, 0f);
        return trainPositionService.SetTrackPosition(trainId, position);
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (!_isOpen || _panel is null) return;

        if (NodeIdMatches(_panel.CreateTrainButton, message.NodeId))
        {
            CreateTrain();
            return;
        }

        if (NodeIdMatches(_panel.Overlay, message.NodeId))
        {
            ClosePanel();
        }
    }

    private bool NodeIdMatches(GuiElement guiElement, Id<GuiNode> nodeId)
    {
        if (!guiElementService.TryGetGuiNodeId(guiElement.Id, _panelRegistrationId, out var guiElementNodeId))
        {
            return false;
        }

        return nodeId == guiElementNodeId;
    }
}
