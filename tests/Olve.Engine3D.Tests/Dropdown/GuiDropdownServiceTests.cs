using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Tests.Dropdown;

public class GuiDropdownServiceTests
{
    private record TestHarness(
        GuiNodeService NodeService,
        GuiAnchorService AnchorService,
        GuiElementService ElementService,
        GuiLayoutService LayoutService,
        GuiDepthService DepthService,
        GuiActivationService ActivationService,
        GuiNodeStateService StateService,
        GuiDropdownService DropdownService);

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

        var dropdownService = new GuiDropdownService(
            NullLogger<GuiDropdownService>.Instance,
            elementService, activationService, stateService);

        return new TestHarness(
            nodeService, anchorService, elementService, layoutService,
            depthService, activationService, stateService, dropdownService);
    }

    private static (Engine3D.GUI.Elements.Dropdown Dropdown, Id<GuiElementRegistrations> RegistrationId)
        RegisterDropdown(TestHarness h, string[] options, int? initialSelected = null)
    {
        var dropdown = new Engine3D.GUI.Elements.Dropdown
        {
            Id = Id.New<GuiElement>(),
            Name = "TestDropdown",
            Options = options,
            SelectedIndex = initialSelected ?? -1,
        };

        var container = new Box
        {
            Id = Id.New<GuiElement>(),
            Name = "DropdownContainer",
            Children = [dropdown],
        };

        var anchorId = h.AnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight).Value;

        h.LayoutService.Load();
        h.DepthService.Load();
        h.DropdownService.Load();

        var registrationId = h.ElementService.RegisterElementAndChildren(anchorId, container).Value;

        // Enable button and options container
        if (h.ElementService.TryGetGuiNodeId(dropdown.Button.Id, registrationId, out var buttonNodeId))
        {
            h.StateService.SetState(buttonNodeId, GuiNodeState.Show | GuiNodeState.Enabled);
        }

        if (h.ElementService.TryGetGuiNodeId(dropdown.OptionsContainer.Id, registrationId, out var containerNodeId))
        {
            h.StateService.SetState(containerNodeId, GuiNodeState.Show | GuiNodeState.Enabled);
        }

        // Apply initial dirty state
        h.DropdownService.Update();

        return (dropdown, registrationId);
    }

    private static void ActivateButton(TestHarness h, Engine3D.GUI.Elements.Dropdown dropdown, Id<GuiElementRegistrations> registrationId)
    {
        if (h.ElementService.TryGetGuiNodeId(dropdown.Button.Id, registrationId, out var buttonNodeId))
        {
            h.ActivationService.Activate(buttonNodeId);
            h.DropdownService.Update();
        }
    }

    private static void ActivateOption(TestHarness h, Engine3D.GUI.Elements.Dropdown dropdown, Id<GuiElementRegistrations> registrationId, int optionIndex)
    {
        var optionBox = dropdown.OptionsContainer.Children[optionIndex];
        if (h.ElementService.TryGetGuiNodeId(optionBox.Id, registrationId, out var optionNodeId))
        {
            h.ActivationService.Activate(optionNodeId);
            h.DropdownService.Update();
        }
    }

    [Test, NotInParallel]
    public async Task Dropdown_ClickButton_Expands()
    {
        var h = BuildHarness();
        var (dropdown, regId) = RegisterDropdown(h, ["Option 1", "Option 2", "Option 3"]);

        ActivateButton(h, dropdown, regId);

        await Assert.That(dropdown.IsExpanded).IsTrue();
    }

    [Test, NotInParallel]
    public async Task Dropdown_ClickButtonTwice_TogglesExpanded()
    {
        var h = BuildHarness();
        var (dropdown, regId) = RegisterDropdown(h, ["Option 1", "Option 2", "Option 3"]);

        ActivateButton(h, dropdown, regId);
        await Assert.That(dropdown.IsExpanded).IsTrue();

        ActivateButton(h, dropdown, regId);
        await Assert.That(dropdown.IsExpanded).IsFalse();
    }

    [Test, NotInParallel]
    public async Task Dropdown_ClickOption_SelectsAndCollapses()
    {
        var h = BuildHarness();
        var (dropdown, regId) = RegisterDropdown(h, ["Option 1", "Option 2", "Option 3"]);

        ActivateButton(h, dropdown, regId); // Expand
        ActivateOption(h, dropdown, regId, 1);

        await Assert.That(dropdown.SelectedIndex).IsEqualTo(1);
        await Assert.That(dropdown.IsExpanded).IsFalse();
    }

    [Test, NotInParallel]
    public async Task Dropdown_ButtonLabelUpdates_WhenOptionSelected()
    {
        var h = BuildHarness();
        var (dropdown, regId) = RegisterDropdown(h, ["Alpha", "Beta", "Gamma"]);

        ActivateButton(h, dropdown, regId); // Expand
        ActivateOption(h, dropdown, regId, 2);

        await Assert.That(dropdown.ButtonLabel.Content).IsEqualTo("Gamma");
    }

    [Test, NotInParallel]
    public async Task Dropdown_ButtonLabelShowsPlaceholder_WhenNoSelection()
    {
        var h = BuildHarness();
        var (dropdown, _) = RegisterDropdown(h, ["Option 1", "Option 2"]);

        await Assert.That(dropdown.ButtonLabel.Content).IsEqualTo("Select...");
    }

    [Test, NotInParallel]
    public async Task Dropdown_EventFires_WhenOptionSelected()
    {
        var h = BuildHarness();
        var (dropdown, regId) = RegisterDropdown(h, ["A", "B", "C"]);

        GuiDropdownService.DropdownValueChangedMessage? captured = null;
        h.DropdownService.OnValueChanged.Subscribe(msg => captured = msg);

        ActivateButton(h, dropdown, regId);
        ActivateOption(h, dropdown, regId, 1);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Value.DropdownId).IsEqualTo(dropdown.Id);
        await Assert.That(captured.Value.OldSelectedIndex).IsEqualTo(-1);
        await Assert.That(captured.Value.NewSelectedIndex).IsEqualTo(1);
        await Assert.That(captured.Value.OldSelectedText).IsNull();
        await Assert.That(captured.Value.NewSelectedText).IsEqualTo("B");
    }

    [Test, NotInParallel]
    public async Task Dropdown_InitialSelection_ButtonLabelSet()
    {
        var h = BuildHarness();
        var (dropdown, _) = RegisterDropdown(h, ["Red", "Green", "Blue"], initialSelected: 2);

        await Assert.That(dropdown.ButtonLabel.Content).IsEqualTo("Blue");
    }

    [Test, NotInParallel]
    public async Task Dropdown_OptionsContainerHidden_WhenCollapsed()
    {
        var h = BuildHarness();
        var (dropdown, registrationId) = RegisterDropdown(h, ["X", "Y", "Z"]);

        h.ElementService.TryGetGuiNodeId(dropdown.OptionsContainer.Id, registrationId, out var containerNodeId);
        h.StateService.TryGetState(containerNodeId, out var state);

        await Assert.That(state.HasFlag(GuiNodeState.Show)).IsFalse();
    }

    [Test, NotInParallel]
    public async Task Dropdown_OptionsContainerVisible_WhenExpanded()
    {
        var h = BuildHarness();
        var (dropdown, registrationId) = RegisterDropdown(h, ["X", "Y", "Z"]);

        ActivateButton(h, dropdown, registrationId);

        h.ElementService.TryGetGuiNodeId(dropdown.OptionsContainer.Id, registrationId, out var containerNodeId);
        h.StateService.TryGetState(containerNodeId, out var state);

        await Assert.That(state.HasFlag(GuiNodeState.Show)).IsTrue();
    }
}
