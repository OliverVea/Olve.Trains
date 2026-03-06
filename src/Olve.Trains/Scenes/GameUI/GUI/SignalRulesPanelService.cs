using System.Text;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Utilities.Types;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class SignalRulesPanelService(
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService,
    JunctionService junctionService,
    JunctionSignalRuleService junctionSignalRuleService,
    MouseRaycastService mouseRaycastService,
    MouseManager mouseManager,
    JunctionSignalCollisionService junctionSignalCollisionService) : ISceneService
{
    private static readonly Layouts.SignalRulesPanel Panel = Layouts.BuildSignalRulesPanel();

    private Id<GuiElementRegistrations> _registrationId;
    private Id<GuiAnchor> _anchorId;
    private bool _isOpen;
    private bool _clickedThisFrame;
    private Id<Junction> _junctionId;

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

    public Result OpenPanel(Id<Junction> junctionId)
    {
        if (_isOpen)
        {
            if (_junctionId == junctionId)
            {
                return ClosePanel();
            }

            ClosePanel();
        }

        _junctionId = junctionId;

        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopRight, GrowthDirection.DownLeft, depth: 10)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        UpdatePanelContent();

        if (guiElementService.RegisterElementAndChildren(_anchorId, Panel.Overlay)
            .TryPickProblems(out problems, out _registrationId))
        {
            guiAnchorService.UnregisterAnchor(_anchorId);
            return problems;
        }

        _isOpen = true;
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        if (_clickedThisFrame)
        {
            foreach (var hit in mouseRaycastService.Hits)
            {
                if (hit.Group != ColliderGroups.Signal) continue;

                if (junctionSignalCollisionService.TryGetJunctionId(hit.ColliderId, out var junctionId))
                {
                    OpenPanel(junctionId);
                }

                break;
            }
        }

        if (_isOpen)
        {
            UpdatePanelContent();
        }

        return Result.Success();
    }

    private Result ClosePanel()
    {
        guiElementService.UnregisterElementAndChildren(_registrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);
        _isOpen = false;
        return Result.Success();
    }

    private void UpdatePanelContent()
    {
        var connections = junctionService.GetConnections(_junctionId);

        if (junctionService.TryGetJunction(_junctionId, out var junction))
        {
            Panel.JunctionInfo.Content = $"Position: ({junction.Position.X}, {junction.Position.Z}) | Connections: {connections.Count}";
        }

        var rules = junctionSignalRuleService.GetRulesForJunction(_junctionId);
        if (rules.Count == 0)
        {
            Panel.RulesText.Content = "No rules configured.";
        }
        else
        {
            var sb = new StringBuilder();
            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                var vehicles = string.Join(",", rule.Vehicles);
                var sources = string.Join(",", rule.Sources);
                var destinations = string.Join(",", rule.Destinations);
                var distribution = FormatDistribution(rule.Distribution);
                sb.AppendLine($"{i + 1}. {vehicles} > {sources} > {destinations} ({distribution})");
            }

            Panel.RulesText.Content = sb.ToString().TrimEnd();
        }
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (!_isOpen) return;

        if (NodeIdMatches(Panel.Overlay, message.NodeId))
        {
            ClosePanel();
        }
        else if (NodeIdMatches(Panel.AddRuleButton, message.NodeId))
        {
            junctionSignalRuleService.AddRuleForJunction(
                _junctionId,
                [new Any()],
                [new Any()],
                [new Any()],
                new RoundRobin());
        }
        else if (NodeIdMatches(Panel.ClearRulesButton, message.NodeId))
        {
            junctionSignalRuleService.ClearRulesForJunction(_junctionId);
        }
    }

    private static string FormatDistribution(SignalRuleDistribution distribution)
    {
        return distribution.Match(
            _ => "round-robin");
    }

    private bool NodeIdMatches(GuiElement guiElement, Id<GuiNode> nodeId)
    {
        if (!guiElementService.TryGetGuiNodeId(guiElement.Id, _registrationId, out var guiElementNodeId))
        {
            return false;
        }

        return nodeId == guiElementNodeId;
    }
}
