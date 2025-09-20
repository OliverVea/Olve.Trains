using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Layout;
using Olve.Logging;
using Olve.Results;
using Olve.Results.TUnit;
using Olve.Utilities.Ids;
using Silk.NET.Maths;
using TUnit.Core.Helpers;

namespace Olve.Engine3D.Tests.Layout;

public static class GuiElementLayoutServiceTestData
{
    public static IEnumerable<Func<(string TestName,
        GuiElementBox Parent,
        Vector2D<Px> ExpectedParentSize,
        IReadOnlyCollection<GuiElementBox> Children,
        IReadOnlyCollection<Vector2D<Px>> ExpectedChildrenSizes)>> AdditionTestData()
    {
        yield return () => (
            "Single box",
            Box(prefW: 150, prefH: 100),
            new Vector2D<Px>(150, 100),
            [],
            []);
        yield return () => (
            "Single child box",
            Box(),
            new Vector2D<Px>(150, 100),
            [Box(prefW: 150, prefH: 100)],
            [new Vector2D<Px>(150, 100)]);
        yield return () => (
            "Single child box with larger parent",
            Box(prefW: 400, prefH: 350),
            new Vector2D<Px>(400, 350),
            [Box(prefW: 150, prefH: 100)],
            [new Vector2D<Px>(150, 100)]);
        yield return () => (
            "Two child boxes (horizontal)",
            Box(),
            new Vector2D<Px>(300, 100),
            [Box(prefW: 150, prefH: 100), Box(prefW: 150, prefH: 100)],
            [new Vector2D<Px>(150, 100), new Vector2D<Px>(150, 100)]);
        yield return () => (
            "Two child boxes (vertical)",
            Box(axis: UIAxis.Y),
            new Vector2D<Px>(150, 200),
            [Box(prefW: 150, prefH: 100), Box(prefW: 150, prefH: 100)],
            [new Vector2D<Px>(150, 100), new Vector2D<Px>(150, 100)]);
        yield return () => (
            "Child box grows",
            Box(prefW: 150, prefH: 100),
            new Vector2D<Px>(150, 100),
            [Box(resizingWeight: 1f)],
            [new Vector2D<Px>(150, 100)]);
        yield return () => (
            "Two child boxes grow with weight",
            Box(prefW: 600, prefH: 200),
            new Vector2D<Px>(600, 200),
            [Box(resizingWeight: 2f), Box(resizingWeight: 1f)],
            [new Vector2D<Px>(400, 200), new Vector2D<Px>(200, 200)]);
        yield return () => (
            "Two child boxes grow vertically with weight",
            Box(axis: UIAxis.Y, prefW: 600, prefH: 300),
            new Vector2D<Px>(600, 300),
            [Box(resizingWeight: 2f), Box(resizingWeight: 1f)],
            [new Vector2D<Px>(600, 200), new Vector2D<Px>(600, 100)]);
        yield return () => (
            "Mixed preferred + weighted (horizontal)",
            Box(prefW: 500, prefH: 100),
            new Vector2D<Px>(500, 100),
            [
                Box(prefW: 150, prefH: 100),
                Box(resizingWeight: 1f),
                Box(resizingWeight: 2f)
            ],
            // leftover = 350; weights 1:2 => 116/234 if rounding, pick a policy (e.g., 117/233)
            [
                new Vector2D<Px>(150, 100),
                new Vector2D<Px>(117, 100),
                new Vector2D<Px>(233, 100)
            ]);
        yield return () => (
            "Zero-weight child stays preferred",
            Box(prefW: 300, prefH: 100),
            new Vector2D<Px>(300, 100),
            [
                Box(prefW: 100, prefH: 100),
                Box(resizingWeight: 1f),
                Box(resizingWeight: 0f)
            ],
            [
                new Vector2D<Px>(100, 100),
                new Vector2D<Px>(200, 100),
                new Vector2D<Px>(0, 0)
            ]
        );
    }

    private static GuiElementBox Box(UIAxis axis = UIAxis.X, Dp? prefW = null, Dp? prefH = null,
        float resizingWeight = 0f)
        => new()
        {
            LayoutAxis = axis,
            Size = new SizeSpec
            {
                PreferredWidth = prefW,
                PreferredHeight = prefH,
                ResizingWeight = resizingWeight,
            },
        };
}

public class GuiElementLayoutServiceTests
{
    private static readonly LayoutContext DefaultContext = new()
    {
        DesignSize = new Vector2D<int>(1920, 1080),
        ViewportSize = new Vector2D<int>(1920, 1080),
        UiScale = 1,
        DevicePixelRatio = 1
    };

    private static readonly Id<GuiAnchor> DefaultAnchorId = Id.New<GuiAnchor>();

    private static readonly InMemoryLoggingManager LoggingManager = new();
    private static readonly GuiElementService GuiElementService = new(LoggingManager);

    private static GuiElementLayoutService BuildSut(LayoutContext? layoutContext = null) =>
        new(LoggingManager, GuiElementService, layoutContext ?? DefaultContext);

    [Test, NotInParallel]
    public async Task ComputeLayout_EmptyConfiguration_Succeeds()
    {
        var sut = BuildSut();
        var result = sut.ComputeLayout();
        await Assert.That(result).Succeeded();
    }

    [Test, NotInParallel]
    [MethodDataSource(typeof(GuiElementLayoutServiceTestData),
        nameof(GuiElementLayoutServiceTestData.AdditionTestData))]
    public async Task NestedWidthTest(string testName,
        GuiElementBox parent,
        Vector2D<Px> expectedParentSize,
        IReadOnlyCollection<GuiElementBox> children,
        IReadOnlyCollection<Vector2D<Px>> expectedChildrenSizes)
    {
        // Arrange
        var sut = BuildSut();

        var parentResult = GuiElementService.AddGuiElement("Parent", DefaultAnchorId);
        await Assert.That(parentResult).Succeeded();
        var parentId = parentResult.Value;
        sut.CreateOrSetElementBox(parentId, parent);

        var childIds = new Id<GuiElement>[children.Count];

        var i = 0;
        foreach (var child in children)
        {
            var childResult = GuiElementService.AddGuiElement("Child_" + (i + 1), parentId);
            await Assert.That(childResult).Succeeded();
            sut.CreateOrSetElementBox(childResult.Value, child);

            childIds[i] = childResult.Value;
            i++;
        }

        // Act
        sut.ComputeLayout();

        // Assert
        using var assertScope = Assert.Multiple();

        var gotParent = sut.TryGetBoxPosition(parentId, out var parentPosition);
        await Assert.That(gotParent).IsTrue();
        await Assert.That(parentPosition.Size.X).IsEqualTo(expectedParentSize.X);
        await Assert.That(parentPosition.Size.Y).IsEqualTo(expectedParentSize.Y);

        foreach (var (childId, expectedChildSize) in childIds.Zip(expectedChildrenSizes))
        {
            var gotChild = sut.TryGetBoxPosition(childId, out var childPosition);
            await Assert.That(gotChild).IsTrue();
            await Assert.That(childPosition.Size.X).IsEqualTo(expectedChildSize.X);
            await Assert.That(childPosition.Size.Y).IsEqualTo(expectedChildSize.Y);
        }
    }

    [Test, NotInParallel]
public async Task TripleNested_Layout_Sizes_And_Positions()
{
    // Fresh instances per test for isolation
    var logging = new InMemoryLoggingManager();
    var ge = new GuiElementService(logging);
    var sut = new GuiElementLayoutService(logging, ge, DefaultContext);

    // Helper to reduce noise
    GuiElementBox Box(UIAxis axis = UIAxis.X, Dp? prefW = null, Dp? prefH = null, float weight = 0f) => new()
    {
        LayoutAxis = axis,
        Size = new SizeSpec
        {
            PreferredWidth = prefW,
            PreferredHeight = prefH,
            ResizingWeight = weight, // ensure this matches your actual property name
        },
    };

    // Create the tree
    var anchorId = DefaultAnchorId; // Parent anchored to root anchor

    var parentId = await ge.AddGuiElement("Parent", anchorId).AssertSuccessAndGetAsync();
    sut.CreateOrSetElementBox(parentId, Box(UIAxis.X, prefW: 600, prefH: 300)); // 600x300 parent

    // Left panel (fixed W=200, fills height=300 via parent)
    var leftPanelId = await ge.AddGuiElement("LeftPanel", parentId).AssertSuccessAndGetAsync();
    sut.CreateOrSetElementBox(leftPanelId, Box(UIAxis.Y, prefW: 200)); // width 200, vertical stack

    // Top row inside left panel (200x100)
    var topRowId = await ge.AddGuiElement("TopRow", leftPanelId).AssertSuccessAndGetAsync();
    sut.CreateOrSetElementBox(topRowId, Box(UIAxis.X, prefW: 200, prefH: 100));

    var leaf1Id = await ge.AddGuiElement("Leaf1", topRowId).AssertSuccessAndGetAsync();
    sut.CreateOrSetElementBox(leaf1Id, Box(prefW: 100, prefH: 100));

    var leaf2Id = await ge.AddGuiElement("Leaf2", topRowId).AssertSuccessAndGetAsync();
    sut.CreateOrSetElementBox(leaf2Id, Box(prefW: 100, prefH: 100));

    // Bottom box inside left panel (200x200)
    var bottomBoxId = await ge.AddGuiElement("BottomBox", leftPanelId).AssertSuccessAndGetAsync();
    sut.CreateOrSetElementBox(bottomBoxId, Box(prefW: 200, prefH: 200));

    // Right panel (takes remaining width 400, fills height 300)
    var rightPanelId = await ge.AddGuiElement("RightPanel", parentId).AssertSuccessAndGetAsync();
    sut.CreateOrSetElementBox(rightPanelId, Box(UIAxis.X, weight: 1f)); // width determined by leftover

    var grow1Id = await ge.AddGuiElement("Grow1", rightPanelId).AssertSuccessAndGetAsync();
    sut.CreateOrSetElementBox(grow1Id, Box(weight: 1f));

    var grow2Id = await ge.AddGuiElement("Grow2", rightPanelId).AssertSuccessAndGetAsync();
    sut.CreateOrSetElementBox(grow2Id, Box(weight: 1f));

    // Act
    var result = sut.ComputeLayout();
    await Assert.That(result).Succeeded();

    // Assert sizes (and a couple positions)
    using var scope = Assert.Multiple();

    // Parent
    await Assert.That(sut.TryGetBoxPosition(parentId, out var parentPos)).IsTrue();
    await Assert.That(parentPos.Size.X.Value).IsEqualTo(600);
    await Assert.That(parentPos.Size.Y.Value).IsEqualTo(300);

    // Left panel: 200x300 at X=0
    await Assert.That(sut.TryGetBoxPosition(leftPanelId, out var leftPos)).IsTrue();
    await Assert.That(leftPos.Size.X.Value).IsEqualTo(200);
    await Assert.That(leftPos.Size.Y.Value).IsEqualTo(300);
    //await Assert.That(leftPos.Position.X.Value).IsEqualTo(0); // depends on absolute positioning
    //await Assert.That(leftPos.Position.Y.Value).IsEqualTo(0);

    // Top row: 200x100 at Y=0 inside left panel
    await Assert.That(sut.TryGetBoxPosition(topRowId, out var topRowPos)).IsTrue();
    await Assert.That(topRowPos.Size.X.Value).IsEqualTo(200);
    await Assert.That(topRowPos.Size.Y.Value).IsEqualTo(100);
    //await Assert.That(topRowPos.Position.X.Value).IsEqualTo(0);
    //await Assert.That(topRowPos.Position.Y.Value).IsEqualTo(0);

    // Leaves: 100x100 each; Leaf2 sits to the right of Leaf1
    await Assert.That(sut.TryGetBoxPosition(leaf1Id, out var leaf1Pos)).IsTrue();
    await Assert.That(leaf1Pos.Size.X.Value).IsEqualTo(100);
    await Assert.That(leaf1Pos.Size.Y.Value).IsEqualTo(100);
    //await Assert.That(leaf1Pos.Position.X.Value).IsEqualTo(0);
    //await Assert.That(leaf1Pos.Position.Y.Value).IsEqualTo(0);

    await Assert.That(sut.TryGetBoxPosition(leaf2Id, out var leaf2Pos)).IsTrue();
    await Assert.That(leaf2Pos.Size.X.Value).IsEqualTo(100);
    await Assert.That(leaf2Pos.Size.Y.Value).IsEqualTo(100);
    //await Assert.That(leaf2Pos.Position.X.Value).IsEqualTo(100);
    //await Assert.That(leaf2Pos.Position.Y.Value).IsEqualTo(0);

    // Bottom box: 200x200 stacked under top row
    await Assert.That(sut.TryGetBoxPosition(bottomBoxId, out var bottomPos)).IsTrue();
    await Assert.That(bottomPos.Size.X.Value).IsEqualTo(200);
    await Assert.That(bottomPos.Size.Y.Value).IsEqualTo(200);
    //await Assert.That(bottomPos.Position.X.Value).IsEqualTo(0);
    //await Assert.That(bottomPos.Position.Y.Value).IsEqualTo(100);

    // Right panel: 400x300 placed to the right of left panel at X=200
    await Assert.That(sut.TryGetBoxPosition(rightPanelId, out var rightPos)).IsTrue();
    await Assert.That(rightPos.Size.X.Value).IsEqualTo(400);
    await Assert.That(rightPos.Size.Y.Value).IsEqualTo(300);
    //await Assert.That(rightPos.Position.X.Value).IsEqualTo(200);
    //await Assert.That(rightPos.Position.Y.Value).IsEqualTo(0);

    // Grow children split the 400 width evenly (weights 1:1)
    await Assert.That(sut.TryGetBoxPosition(grow1Id, out var g1)).IsTrue();
    await Assert.That(g1.Size.X.Value).IsEqualTo(200);
    await Assert.That(g1.Size.Y.Value).IsEqualTo(300);
    //await Assert.That(g1.Position.X.Value).IsEqualTo(200);
    //await Assert.That(g1.Position.Y.Value).IsEqualTo(0);

    await Assert.That(sut.TryGetBoxPosition(grow2Id, out var g2)).IsTrue();
    await Assert.That(g2.Size.X.Value).IsEqualTo(200);
    await Assert.That(g2.Size.Y.Value).IsEqualTo(300);
    //await Assert.That(g2.Position.X.Value).IsEqualTo(400);
    //await Assert.That(g2.Position.Y.Value).IsEqualTo(0);
}
}

public static class TUnitResultExtensions
{
    public static async Task<Result<T>> AssertSuccess<T>(this Task<Result<T>> task)
    {
        var r = await task;
        await Assert.That(r).Succeeded();
        return r;
    }
}


internal static class ResultAssertExtensions
{
    // --- Generic Result<T> ---

    // For sync Result<T>
    public static async Task<T> AssertSuccessAndGetAsync<T>(this Result<T> result)
    {
        await Assert.That(result).Succeeded();
        return result.Value!;
    }

    // For Task<Result<T>>
    public static async Task<T> AssertSuccessAndGetAsync<T>(this Task<Result<T>> resultTask)
    {
        var result = await resultTask;
        await Assert.That(result).Succeeded();
        return result.Value!;
    }

    // --- Non-generic Result ---

    // For sync Result
    public static async Task AssertSuccessAsync(this Result result)
        => await Assert.That(result).Succeeded();

    // For Task<Result>
    public static async Task AssertSuccessAsync(this Task<Result> resultTask)
    {
        var result = await resultTask;
        await Assert.That(result).Succeeded();
    }
}