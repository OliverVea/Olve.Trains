using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.GUI.Styling.Animation;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Tests.Animation;

public class GuiAnimationServiceTests
{
    private static readonly StyleKey TestStyleKey = new("test-style");

    private static (IServiceProvider Scope, IDisposable Root) BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<EventQueueFactory>();
        services.AddGuiServices();

        var root = services.BuildServiceProvider();
        var scope = root.CreateScope();
        return (scope.ServiceProvider, scope);
    }

    private static (Id<GuiNode> NodeId, Id<GuiElementRegistrations> RegistrationId) RegisterElement(
        IServiceProvider sp,
        StyleKey styleKey)
    {
        var anchorService = sp.GetRequiredService<GuiAnchorService>();
        var elementService = sp.GetRequiredService<GuiElementService>();

        var anchorId = anchorService.RegisterAnchor(
            AnchorPosition.TopLeft, GrowthDirection.DownRight).Value;

        var element = new Box
        {
            Id = Id.New<GuiElement>(),
            Name = "TestBox",
            StyleKey = styleKey,
            Interactive = true,
        };

        var registrationId = elementService.RegisterElementAndChildren(anchorId, element).Value;
        elementService.TryGetGuiNodeId(element.Id, registrationId, out var nodeId);

        return (nodeId, registrationId);
    }

    [Test, NotInParallel]
    public async Task FocusGainThenLoss_OutAnimationDecreases()
    {
        // Arrange
        var (sp, root) = BuildServiceProvider();
        using var _ = root;
        var sut = sp.GetRequiredService<GuiAnimationService>();
        var stateService = sp.GetRequiredService<GuiNodeStateService>();
        var styleRegistry = sp.GetRequiredService<GuiStyleRegistry>();

        styleRegistry.Register(new GuiElementStyling<Box>
        {
            StyleKey = TestStyleKey,
            StateTransitions = new Dictionary<GuiNodeState, StateTransition>
            {
                [GuiNodeState.Focused] = new(
                    In: new GuiTransition(new Ms(100), Easing.Linear),
                    Out: new GuiTransition(new Ms(100), Easing.Linear)),
            },
        });

        var (nodeId, _) = RegisterElement(sp, TestStyleKey);

        sut.Load();

        // Gain focus, advance 20ms (In animation partially completes)
        stateService.SetState(nodeId, GuiNodeState.Focused);
        sut.Update(TimeSpan.FromMilliseconds(20));

        // Lose focus before In completes — this should cancel In and start Out
        stateService.SetState(nodeId, GuiNodeState.None);

        // Capture weight over two successive updates after focus loss
        float? weightFirstUpdate = null;
        float? weightSecondUpdate = null;
        void Capture(GuiAnimationService.GuiStateWeightsChangedMessage msg)
        {
            if (msg.NodeId == nodeId)
            {
                if (weightFirstUpdate is null)
                    weightFirstUpdate = msg.Weights[GuiNodeState.Focused];
                else
                    weightSecondUpdate = msg.Weights[GuiNodeState.Focused];
            }
        }

        sut.GuiStateWeightsChanged.Subscribe(Capture);
        sut.Update(TimeSpan.FromMilliseconds(20));
        sut.Update(TimeSpan.FromMilliseconds(20));
        sut.GuiStateWeightsChanged.Unsubscribe(Capture);

        // Assert: after focus loss, weight should be decreasing toward 0 (Out animation),
        // not increasing toward 1 (which would happen if the old In animation wasn't cancelled)
        await Assert.That(weightFirstUpdate).IsNotNull();
        await Assert.That(weightSecondUpdate).IsNotNull();
        await Assert.That(weightSecondUpdate!.Value).IsLessThan(weightFirstUpdate!.Value);
    }
}
