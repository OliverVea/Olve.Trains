using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Scenes;
using Olve.Generated.Layouts;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class BurgerMenuService(
    IServiceProvider serviceProvider,
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService) : ISceneService
{
    private static readonly Layouts.BurgerMenu BurgerMenu = Layouts.BuildBurgerMenu();

    private Id<GuiElementRegistrations> _registrationId;
    private Id<GuiAnchor> _anchorId;
    private bool _isOpen;

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
            CloseMenu();
        }

        return Result.Success();
    }

    public Result ToggleMenu()
    {
        return _isOpen ? CloseMenu() : OpenMenu();
    }

    private Result OpenMenu()
    {
        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        if (guiElementService.RegisterElementAndChildren(_anchorId, BurgerMenu.Overlay)
            .TryPickProblems(out problems, out _registrationId))
        {
            guiAnchorService.UnregisterAnchor(_anchorId);
            return problems;
        }

        _isOpen = true;
        return Result.Success();
    }

    private Result CloseMenu()
    {
        guiElementService.UnregisterElementAndChildren(_registrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);
        _isOpen = false;
        return Result.Success();
    }

    private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
    {
        if (!_isOpen) return;

        if (NodeIdMatches(BurgerMenu.Overlay, message.NodeId)
            || NodeIdMatches(BurgerMenu.ResumeButton, message.NodeId))
        {
            CloseMenu();
        }
        else if (NodeIdMatches(BurgerMenu.MainMenuButton, message.NodeId))
        {
            CloseMenu();
            TransitionToMainMenu();
        }
        // SaveGameButton, LoadGameButton, OptionsButton — noop for now
    }

    private bool NodeIdMatches(GuiElement guiElement, Id<GuiNode> nodeId)
    {
        if (!guiElementService.TryGetGuiNodeId(guiElement.Id, _registrationId, out var guiElementNodeId))
        {
            return false;
        }

        return nodeId == guiElementNodeId;
    }

    private Result TransitionToMainMenu()
    {
        var sceneManager = serviceProvider.GetRequiredService<SceneManager>();

        if (sceneManager.DeactivateAndUnloadScene(SceneIds.GameLogicScene)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(SceneIds.MainMenuScene);
    }
}
