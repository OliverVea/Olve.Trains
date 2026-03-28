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
using Olve.Trains.Scenes.GameLogic.Trains.Wagons;
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
    TrainPositionService trainPositionService,
    TrainWagonService trainWagonService,
    WagonBlueprintService wagonBlueprintService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([mouseRaycastService]);

    private readonly record struct MountedTrainSection(
        Id<Train> TrainId,
        Layouts.DepotTrainSection Section,
        Id<GuiElementRegistrations> RegistrationId,
        List<MountedWagonRow> WagonRows);

    private readonly record struct MountedWagonRow(
        int WagonIndex,
        Layouts.DepotTrainRow Row,
        Id<GuiElementRegistrations> RegistrationId);

    private Layouts.DepotPanel? _panel;
    private Id<GuiElementRegistrations> _panelRegistrationId;
    private Id<GuiAnchor> _anchorId;
    private bool _isOpen;
    private bool _clickedThisFrame;
    private Id<Building> _buildingId;
    private Depot _depot;
    private readonly List<MountedTrainSection> _mountedSections = [];

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

        MountTrainSections();

        _isOpen = true;
        return Result.Success();
    }

    private Result ClosePanel()
    {
        UnmountTrainSections();
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
        TrainTrackPosition position = new(trackPoint, TrainDirection.Forward);

        if (trainPositionService.SetTrackPosition(trainId, position).TryPickProblems(out problems))
        {
            return problems;
        }

        if (trainPositionService.SetMotion(trainId, new TrainMotion(0f, 0f)).TryPickProblems(out problems))
        {
            return problems;
        }

        RefreshTrainSections();
        return Result.Success();
    }

    private Result DeleteTrain(Id<Train> trainId)
    {
        trainService.DeleteTrain(trainId);
        RefreshTrainSections();
        return Result.Success();
    }

    private Result AddWagon(Id<Train> trainId)
    {
        if (trainWagonService.AddWagon(trainId, WagonBlueprintCatalog.GoodsWagon)
            .TryPickProblems(out var problems, out _))
        {
            return problems;
        }

        RefreshTrainSections();
        return Result.Success();
    }

    private Result RemoveWagon(Id<Train> trainId, int wagonIndex)
    {
        if (trainWagonService.RemoveWagon(trainId, wagonIndex).TryPickProblems(out var problems))
        {
            return problems;
        }

        RefreshTrainSections();
        return Result.Success();
    }

    private void RefreshTrainSections()
    {
        UnmountTrainSections();
        MountTrainSections();
    }

    private void MountTrainSections()
    {
        if (_panel is null) return;

        if (!guiElementService.TryGetGuiNodeId(_panel.TrainListContainer.Id, _panelRegistrationId,
                out var containerNodeId))
        {
            return;
        }

        foreach (var (trainId, trackPosition) in trainPositionService.TrackPositions)
        {
            if (trackPosition.TrackId != _depot.TrackId) continue;

            var section = Layouts.BuildDepotTrainSection();
            section.TrainName.Content = $"Train {trainId}";

            // Try to get the actual train name
            foreach (var id in trainService.TrainIds)
            {
                if (id != trainId) continue;
                // TrainService doesn't expose TryGetTrain, use the ID display
                break;
            }

            if (guiElementService.RegisterElementAndChildren(containerNodeId, section.Section)
                .TryPickProblems(out _, out var sectionRegistrationId))
            {
                continue;
            }

            var wagonRows = new List<MountedWagonRow>();
            MountWagonRows(trainId, section, sectionRegistrationId, wagonRows);

            _mountedSections.Add(new MountedTrainSection(trainId, section, sectionRegistrationId, wagonRows));
        }
    }

    private void MountWagonRows(
        Id<Train> trainId,
        Layouts.DepotTrainSection section,
        Id<GuiElementRegistrations> sectionRegistrationId,
        List<MountedWagonRow> wagonRows)
    {
        if (!guiElementService.TryGetGuiNodeId(section.WagonContainer.Id, sectionRegistrationId,
                out var wagonContainerNodeId))
        {
            return;
        }

        var wagons = trainWagonService.GetWagons(trainId);
        for (var i = 0; i < wagons.Count; i++)
        {
            var wagon = wagons[i];
            var row = Layouts.BuildDepotTrainRow();

            var wagonName = wagonBlueprintService.TryGet(wagon.BlueprintId, out var blueprint)
                ? blueprint.Name
                : "Unknown Wagon";
            row.WagonName.Content = wagonName;

            if (guiElementService.RegisterElementAndChildren(wagonContainerNodeId, row.Row)
                .TryPickProblems(out _, out var rowRegistrationId))
            {
                continue;
            }

            wagonRows.Add(new MountedWagonRow(i, row, rowRegistrationId));
        }
    }

    private void UnmountTrainSections()
    {
        foreach (var section in _mountedSections)
        {
            foreach (var wagonRow in section.WagonRows)
            {
                guiElementService.UnregisterElementAndChildren(wagonRow.RegistrationId);
            }

            guiElementService.UnregisterElementAndChildren(section.RegistrationId);
        }

        _mountedSections.Clear();
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (!_isOpen || _panel is null) return;

        if (NodeIdMatches(_panel.CreateTrainButton, message.NodeId))
        {
            CreateTrain();
            return;
        }

        foreach (var section in _mountedSections)
        {
            if (NodeIdMatches(section.Section.AddWagonButton, message.NodeId, section.RegistrationId))
            {
                AddWagon(section.TrainId);
                return;
            }

            if (NodeIdMatches(section.Section.DeleteTrainButton, message.NodeId, section.RegistrationId))
            {
                DeleteTrain(section.TrainId);
                return;
            }

            foreach (var wagonRow in section.WagonRows)
            {
                if (NodeIdMatches(wagonRow.Row.RemoveButton, message.NodeId, wagonRow.RegistrationId))
                {
                    RemoveWagon(section.TrainId, wagonRow.WagonIndex);
                    return;
                }
            }
        }

        if (NodeIdMatches(_panel.Overlay, message.NodeId))
        {
            ClosePanel();
        }
    }

    private bool NodeIdMatches(GuiElement guiElement, Id<GuiNode> nodeId)
    {
        return NodeIdMatches(guiElement, nodeId, _panelRegistrationId);
    }

    private bool NodeIdMatches(GuiElement guiElement, Id<GuiNode> nodeId, Id<GuiElementRegistrations> registrationId)
    {
        if (!guiElementService.TryGetGuiNodeId(guiElement.Id, registrationId, out var guiElementNodeId))
        {
            return false;
        }

        return nodeId == guiElementNodeId;
    }
}
