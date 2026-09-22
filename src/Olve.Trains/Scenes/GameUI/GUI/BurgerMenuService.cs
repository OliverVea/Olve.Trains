using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Commands;
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
    GuiAnchorService guiAnchorService,
    ILogger<BurgerMenuService> logger) : ISceneService
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
        if (guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight, depth: 10)
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
        else if (NodeIdMatches(BurgerMenu.SaveGameButton, message.NodeId))
        {
            // Snapshot the running game into the quicksave slot, then return to gameplay.
            RunGameCommand("save-game kind=quicksave");
            CloseMenu();
        }
        else if (NodeIdMatches(BurgerMenu.LoadGameButton, message.NodeId))
        {
            // Tears down the running game and re-enters through the loading scene. On success this unloads
            // the GameUIScene (and this menu); on failure (e.g. no quicksave yet) the session stays intact.
            RunGameCommand("load-game kind=quicksave");
        }
        // OptionsButton — noop for now
    }

    private void RunGameCommand(string command)
    {
        // Run through the command system so the save/load handlers execute in the GameLogicScene scope and
        // the action is logged like any other command.
        var commandRunner = serviceProvider.GetRequiredService<CommandRunner>();
        if (commandRunner.Run(new RunCommandRequest(command)).TryPickProblems(out var problems))
        {
            logger.LogWarning("Command '{Command}' failed: {Problems}", command, problems);
        }
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

        if (sceneManager.UnloadScene(SceneIds.GameLogicScene.Id)
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return sceneManager.LoadAndActivateScene(SceneIds.MainMenuScene);
    }
}
