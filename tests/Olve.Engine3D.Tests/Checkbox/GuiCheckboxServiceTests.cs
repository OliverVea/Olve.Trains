using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Collision;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Input;
using Olve.Engine3D.Utilities;
using Olve.Utilities.Ids;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Olve.Engine3D.Tests.Checkbox;

public class GuiCheckboxServiceTests
{
    private static readonly LayoutContext DefaultContext = new()
    {
        DesignSize = new Vector2D<Dp>(1920, 1080),
        AspectRatio = 16f / 9,
        DpPxRatio = new DpPxRatio(1),
        UiScale = 1,
    };

    private record TestHarness(
        GuiNodeService NodeService,
        GuiAnchorService AnchorService,
        GuiElementService ElementService,
        GuiLayoutService LayoutService,
        GuiCollisionService CollisionService,
        GuiDepthService DepthService,
        GuiFocusService FocusService,
        GuiActivationService ActivationService,
        GuiNodeStateService StateService,
        GuiMouseInputService MouseInputService,
        GuiCheckboxService CheckboxService,
        MouseManager MouseManager);

    private static TestHarness BuildHarness()
    {
        var nodeService = new GuiNodeService(NullLogger<GuiNodeService>.Instance);
        var anchorService = new GuiAnchorService(NullLogger<GuiAnchorService>.Instance);
        var layoutContextProvider = new Provider<LayoutContext>(DefaultContext);
        var elementService = new GuiElementService(nodeService);
        var stateService = new GuiNodeStateService(
            NullLogger<GuiNodeStateService>.Instance, nodeService, elementService);
        var layoutService = new GuiLayoutService(
            NullLogger<GuiLayoutService>.Instance, nodeService, stateService, anchorService, layoutContextProvider);
        var collisionService = new GuiCollisionService(layoutService);
        var depthService = new GuiDepthService(NullLogger<GuiDepthService>.Instance, nodeService, anchorService);
        var focusService = new GuiFocusService();
        var activationService = new GuiActivationService();

        var mouseManager = new MouseManager(new Provider<IInputContext>(), new Provider<IWindow>());

        var mouseInputService = new GuiMouseInputService(
            mouseManager, collisionService, activationService,
            elementService, depthService, focusService, stateService);

        var checkboxService = new GuiCheckboxService(
            NullLogger<GuiCheckboxService>.Instance,
            elementService, activationService, stateService);

        return new TestHarness(
            nodeService, anchorService, elementService, layoutService,
            collisionService, depthService, focusService, activationService,
            stateService, mouseInputService, checkboxService, mouseManager);
    }

    private static (Engine3D.GUI.Elements.Checkbox Checkbox, Id<GuiElementRegistrations> RegistrationId) RegisterCheckbox(
        TestHarness h, bool initialValue = false)
    {
        var checkbox = new Engine3D.GUI.Elements.Checkbox
        {
            Id = Id.New<GuiElement>(),
            Name = "TestCheckbox",
            IsChecked = initialValue,
        };

        var anchorId = h.AnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight).Value;

        h.LayoutService.Load();
        h.DepthService.Load();
        h.CheckboxService.Load();

        var registrationId = h.ElementService.RegisterElementAndChildren(anchorId, checkbox).Value;

        SetLayoutBoxesForElement(h, checkbox, registrationId);
        h.LayoutService.ComputeLayout();

        // Enable the background so mouse input recognises it
        if (h.ElementService.TryGetGuiNodeId(checkbox.Background.Id, registrationId, out var bgNodeId))
        {
            h.StateService.SetState(bgNodeId, GuiNodeState.Show | GuiNodeState.Enabled);
        }

        // Set initial indicator visibility
        if (h.ElementService.TryGetGuiNodeId(checkbox.Indicator.Id, registrationId, out var indicatorNodeId))
        {
            var indicatorState = initialValue
                ? GuiNodeState.Show | GuiNodeState.Enabled
                : GuiNodeState.Enabled;
            h.StateService.SetState(indicatorNodeId, indicatorState);
        }

        return (checkbox, registrationId);
    }

    private static void SetLayoutBoxesForElement(
        TestHarness h, GuiElement element, Id<GuiElementRegistrations> registrationId)
    {
        if (element.LayoutBox is { } layoutBox
            && h.ElementService.TryGetGuiNodeId(element.Id, registrationId, out var nodeId))
        {
            h.LayoutService.SetNodeBox(nodeId, layoutBox);
        }

        foreach (var child in element.Children)
        {
            SetLayoutBoxesForElement(h, child, registrationId);
        }
    }

    private static void SimulateClick(TestHarness h, float x, float y)
    {
        // Press
        h.MouseManager.State.Position = new Vector2D<float>(x, y);
        h.MouseManager.State.Set(
            new HashSet<MouseButton> { MouseButton.Left },
            new HashSet<MouseButton>());
        h.MouseInputService.Input();

        // Release on same position (triggers activation)
        h.MouseManager.State.Set(
            new HashSet<MouseButton>(),
            new HashSet<MouseButton> { MouseButton.Left });
        h.MouseInputService.Input();

        h.CheckboxService.Update();
    }

    [Test, NotInParallel]
    public async Task Checkbox_ClickTogglesValue()
    {
        var h = BuildHarness();
        var (checkbox, _) = RegisterCheckbox(h, initialValue: false);

        GuiCheckboxService.CheckboxValueChangedMessage? captured = null;
        h.CheckboxService.OnValueChanged.Subscribe(msg => captured = msg);

        // Click center of checkbox (20x20 at top-left)
        SimulateClick(h, 10f, 10f);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Value.IsChecked).IsTrue();
        await Assert.That(checkbox.IsChecked).IsTrue();
    }

    [Test, NotInParallel]
    public async Task Checkbox_DoubleClickReturnsToOriginal()
    {
        var h = BuildHarness();
        var (checkbox, _) = RegisterCheckbox(h, initialValue: false);

        SimulateClick(h, 10f, 10f);
        await Assert.That(checkbox.IsChecked).IsTrue();

        SimulateClick(h, 10f, 10f);
        await Assert.That(checkbox.IsChecked).IsFalse();
    }

    [Test, NotInParallel]
    public async Task Checkbox_InitiallyChecked_ClickUnchecks()
    {
        var h = BuildHarness();
        var (checkbox, _) = RegisterCheckbox(h, initialValue: true);

        SimulateClick(h, 10f, 10f);

        await Assert.That(checkbox.IsChecked).IsFalse();
    }

    [Test, NotInParallel]
    public async Task Checkbox_ClickOutside_DoesNotToggle()
    {
        var h = BuildHarness();
        var (checkbox, _) = RegisterCheckbox(h, initialValue: false);

        // Click outside the checkbox bounds
        SimulateClick(h, 100f, 100f);

        await Assert.That(checkbox.IsChecked).IsFalse();
    }

    [Test, NotInParallel]
    public async Task Checkbox_EventContainsCorrectId()
    {
        var h = BuildHarness();
        var (checkbox, _) = RegisterCheckbox(h, initialValue: false);

        GuiCheckboxService.CheckboxValueChangedMessage? captured = null;
        h.CheckboxService.OnValueChanged.Subscribe(msg => captured = msg);

        SimulateClick(h, 10f, 10f);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Value.CheckboxId).IsEqualTo(checkbox.Id);
    }

}
