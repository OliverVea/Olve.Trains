using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;
using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Trains.Scenes.GameLogic.Collision;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class StationInfoPanelService(
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService,
    MouseRaycastService mouseRaycastService,
    MouseManager mouseManager,
    BuildingCollisionService buildingCollisionService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    StationService stationService,
    StationBlueprintService stationBlueprintService,
    IndustryService industryService,
    IndustryRecipeService industryRecipeService,
    CargoTypeService cargoTypeService,
    CargoInventoryService cargoInventoryService,
    CargoTransferPolicyService cargoTransferPolicyService) : ISceneService
{
    private readonly record struct MountedRow(
        Layouts.InventoryRow Row,
        Id<GuiElementRegistrations> RegistrationId,
        Id<CargoInventory> InventoryId,
        Id<CargoType> CargoTypeId);

    private Layouts.StationInfoPanel? _panel;
    private Id<GuiElementRegistrations> _panelRegistrationId;
    private Id<GuiAnchor> _anchorId;
    private bool _isOpen;
    private bool _clickedThisFrame;
    private Id<Building> _buildingId;
    private readonly List<MountedRow> _mountedRows = [];

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

    public Result<Pass> Input(TimeSpan deltaTime)
    {
        _clickedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        return Pass.Pass;
    }

    public Result Update(TimeSpan deltaTime)
    {
        if (_clickedThisFrame)
        {
            foreach (var hit in mouseRaycastService.Hits)
            {
                if (hit.Group != ColliderGroups.Building) continue;

                if (buildingCollisionService.TryGetBuildingId(hit.ColliderId, out var buildingId)
                    && stationService.TryGetStation(buildingId, out _))
                {
                    OpenPanel(buildingId);
                }

                break;
            }
        }

        if (_isOpen)
        {
            UpdateRowContent();
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
        _panel = Layouts.BuildStationInfoPanel();

        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopRight, GrowthDirection.DownLeft, depth: 10)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        UpdateStationName();

        if (guiElementService.RegisterElementAndChildren(_anchorId, _panel.Overlay)
            .TryPickProblems(out problems, out _panelRegistrationId))
        {
            guiAnchorService.UnregisterAnchor(_anchorId);
            return problems;
        }

        MountInventoryRows();

        _isOpen = true;
        return Result.Success();
    }

    private Result ClosePanel()
    {
        UnmountRows();
        guiElementService.UnregisterElementAndChildren(_panelRegistrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);
        _isOpen = false;
        _panel = null;
        return Result.Success();
    }

    private void UpdateStationName()
    {
        if (_panel is null) return;

        if (buildingService.TryGetBuilding(_buildingId, out var building)
            && buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            _panel.StationName.Content = blueprint.Description;
        }
    }

    private void MountInventoryRows()
    {
        if (_panel is null) return;

        if (!guiElementService.TryGetGuiNodeId(_panel.InventoryContainer.Id, _panelRegistrationId,
                out var containerNodeId))
        {
            return;
        }

        if (!buildingService.TryGetBuilding(_buildingId, out var stationBuilding)) return;
        if (!stationBlueprintService.TryGetProperties(stationBuilding.BlueprintId, out var stationProps)) return;

        foreach (var building in buildingService.Buildings)
        {
            if (!industryService.TryGetByBuilding(building.Id, out var industry)) continue;

            var distance = TileDistance(stationBuilding.Position.BottomLeft, building.Position.BottomLeft);
            if (distance > stationProps.Range) continue;

            if (!industryRecipeService.TryGetRecipe(industry.RecipeId, out var recipe)) continue;

            foreach (var (cargoTypeId, direction) in cargoTransferPolicyService.GetPolicies(industry.InventoryId))
            {
                var row = Layouts.BuildInventoryRow();

                // Direction from the train/station perspective:
                // Industry.In = train delivers -> industry, Industry.Out = train picks up <- industry
                var directionText = direction switch
                {
                    TransferDirection.In => "->",
                    TransferDirection.Out => "<-",
                    TransferDirection.Both => "<>",
                    _ => "--",
                };

                var cargoName = cargoTypeService.TryGetCargoType(cargoTypeId, out var cargoType)
                    ? cargoType.Name
                    : "Unknown";

                row.DirectionIndicator.Content = directionText;
                row.CargoName.Content = $"{recipe.Name}: {cargoName}";

                var amount = cargoInventoryService.GetAmount(industry.InventoryId, cargoTypeId);
                var capacity = cargoInventoryService.GetRemainingCapacityForType(industry.InventoryId, cargoTypeId) + amount;
                row.AmountText.Content = $"{amount}/{capacity}";

                if (guiElementService.RegisterElementAndChildren(containerNodeId, row.Row)
                    .TryPickProblems(out _, out var rowRegistrationId))
                {
                    continue;
                }

                _mountedRows.Add(new MountedRow(row, rowRegistrationId, industry.InventoryId, cargoTypeId));
            }
        }

        if (_mountedRows.Count == 0)
        {
            _panel.IndustriesHeader.Content = "No nearby industries";
        }
    }

    private void UpdateRowContent()
    {
        foreach (var mountedRow in _mountedRows)
        {
            var amount = cargoInventoryService.GetAmount(mountedRow.InventoryId, mountedRow.CargoTypeId);
            var capacity = cargoInventoryService.GetRemainingCapacityForType(mountedRow.InventoryId, mountedRow.CargoTypeId) + amount;
            mountedRow.Row.AmountText.Content = $"{amount}/{capacity}";
        }
    }

    private void UnmountRows()
    {
        foreach (var mountedRow in _mountedRows)
        {
            guiElementService.UnregisterElementAndChildren(mountedRow.RegistrationId);
        }

        _mountedRows.Clear();
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (!_isOpen || _panel is null) return;

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

    private static int TileDistance(TilePosition a, TilePosition b)
    {
        var dx = Math.Abs(a.X - b.X);
        var dz = Math.Abs(a.Z - b.Z);
        return Math.Max(dx, dz);
    }
}
