using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Tests.RadioButton;

public class GuiRadioButtonServiceTests
{
    private static readonly Id<RadioButtonGroup> TestGroup = Id.FromName<RadioButtonGroup>("TestGroup");

    private record TestHarness(
        GuiNodeService NodeService,
        GuiAnchorService AnchorService,
        GuiElementService ElementService,
        GuiLayoutService LayoutService,
        GuiDepthService DepthService,
        GuiActivationService ActivationService,
        GuiNodeStateService StateService,
        GuiRadioButtonService RadioButtonService);

    private static TestHarness BuildHarness()
    {
        var nodeService = new GuiNodeService(NullLogger<GuiNodeService>.Instance);
        var anchorService = new GuiAnchorService(NullLogger<GuiAnchorService>.Instance);
        var layoutContextProvider = new Provider<LayoutContext>(new LayoutContext
        {
            DesignSize = new Silk.NET.Maths.Vector2D<Dp>(1920, 1080),
            AspectRatio = 16f / 9,
            DpPxRatio = new DpPxRatio(1),
            UiScale = 1,
        });
        var layoutService = new GuiLayoutService(
            NullLogger<GuiLayoutService>.Instance, nodeService, anchorService, layoutContextProvider);
        var elementService = new GuiElementService(nodeService);
        var depthService = new GuiDepthService(NullLogger<GuiDepthService>.Instance, nodeService, anchorService);
        var activationService = new GuiActivationService();
        var stateService = new GuiNodeStateService(
            NullLogger<GuiNodeStateService>.Instance, nodeService, elementService);

        var radioButtonService = new GuiRadioButtonService(
            NullLogger<GuiRadioButtonService>.Instance,
            elementService, activationService, stateService);

        return new TestHarness(
            nodeService, anchorService, elementService, layoutService,
            depthService, activationService, stateService, radioButtonService);
    }

    private static (Engine3D.GUI.Elements.RadioButton[] Buttons, Id<GuiElementRegistrations> RegistrationId)
        RegisterRadioButtons(TestHarness h, int count = 3, int? initialSelected = null)
    {
        var buttons = new Engine3D.GUI.Elements.RadioButton[count];
        for (var i = 0; i < count; i++)
        {
            buttons[i] = new Engine3D.GUI.Elements.RadioButton
            {
                Id = Id.New<GuiElement>(),
                Name = $"Radio{i}",
                Group = TestGroup,
                IsSelected = i == initialSelected,
            };
        }

        var container = new Box
        {
            Id = Id.New<GuiElement>(),
            Name = "RadioContainer",
            Vertical = true,
            Children = buttons,
        };

        var anchorId = h.AnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight).Value;

        h.LayoutService.Load();
        h.DepthService.Load();
        h.RadioButtonService.Load();

        var registrationId = h.ElementService.RegisterElementAndChildren(anchorId, container).Value;

        // Enable backgrounds and indicators
        foreach (var button in buttons)
        {
            if (h.ElementService.TryGetGuiNodeId(button.Background.Id, registrationId, out var bgNodeId))
            {
                h.StateService.SetState(bgNodeId, GuiNodeState.Show | GuiNodeState.Enabled);
            }

            if (h.ElementService.TryGetGuiNodeId(button.Indicator.Id, registrationId, out var indNodeId))
            {
                h.StateService.SetState(indNodeId, GuiNodeState.Show | GuiNodeState.Enabled);
            }
        }

        // Apply initial dirty state
        h.RadioButtonService.Update();

        return (buttons, registrationId);
    }

    private static void ActivateButton(TestHarness h, Engine3D.GUI.Elements.RadioButton button, Id<GuiElementRegistrations> registrationId)
    {
        if (h.ElementService.TryGetGuiNodeId(button.Background.Id, registrationId, out var bgNodeId))
        {
            h.ActivationService.Activate(bgNodeId);
            h.RadioButtonService.Update();
        }
    }

    [Test, NotInParallel]
    public async Task RadioButton_ClickSelectsOption()
    {
        var h = BuildHarness();
        var (buttons, regId) = RegisterRadioButtons(h);

        ActivateButton(h, buttons[0], regId);

        await Assert.That(buttons[0].IsSelected).IsTrue();
    }

    [Test, NotInParallel]
    public async Task RadioButton_ClickDifferentOption_DeselectsPrevious()
    {
        var h = BuildHarness();
        var (buttons, regId) = RegisterRadioButtons(h, initialSelected: 0);

        ActivateButton(h, buttons[1], regId);

        await Assert.That(buttons[0].IsSelected).IsFalse();
        await Assert.That(buttons[1].IsSelected).IsTrue();
    }

    [Test, NotInParallel]
    public async Task RadioButton_ClickSameOption_NoChange()
    {
        var h = BuildHarness();
        var (buttons, regId) = RegisterRadioButtons(h, initialSelected: 0);

        GuiRadioButtonService.RadioButtonValueChangedMessage? captured = null;
        h.RadioButtonService.OnValueChanged.Subscribe(msg => captured = msg);

        ActivateButton(h, buttons[0], regId);

        await Assert.That(buttons[0].IsSelected).IsTrue();
        await Assert.That(captured).IsNull();
    }

    [Test, NotInParallel]
    public async Task RadioButton_ClickOutside_NoChange()
    {
        var h = BuildHarness();
        var (buttons, _) = RegisterRadioButtons(h, initialSelected: 0);

        // Activate a non-radio-button node — should be ignored
        h.ActivationService.Activate(Id.New<GuiNode>());
        h.RadioButtonService.Update();

        await Assert.That(buttons[0].IsSelected).IsTrue();
    }

    [Test, NotInParallel]
    public async Task RadioButton_EventContainsCorrectIds()
    {
        var h = BuildHarness();
        var (buttons, regId) = RegisterRadioButtons(h, initialSelected: 0);

        GuiRadioButtonService.RadioButtonValueChangedMessage? captured = null;
        h.RadioButtonService.OnValueChanged.Subscribe(msg => captured = msg);

        ActivateButton(h, buttons[2], regId);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Value.GroupId).IsEqualTo(TestGroup);
        await Assert.That(captured.Value.OldSelectedId).IsEqualTo(buttons[0].Id);
        await Assert.That(captured.Value.NewSelectedId).IsEqualTo(buttons[2].Id);
    }

    [Test, NotInParallel]
    public async Task RadioButton_InitialSelection_IndicatorVisible()
    {
        var h = BuildHarness();
        var (buttons, registrationId) = RegisterRadioButtons(h, initialSelected: 1);

        h.ElementService.TryGetGuiNodeId(buttons[0].Indicator.Id, registrationId, out var ind0);
        h.ElementService.TryGetGuiNodeId(buttons[1].Indicator.Id, registrationId, out var ind1);

        h.StateService.TryGetState(ind0, out var state0);
        h.StateService.TryGetState(ind1, out var state1);

        await Assert.That(state0.HasFlag(GuiNodeState.Show)).IsFalse();
        await Assert.That(state1.HasFlag(GuiNodeState.Show)).IsTrue();
    }
}
