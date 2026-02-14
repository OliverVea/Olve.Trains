using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Utilities;
using Olve.Results.TUnit;
using Olve.Utilities.Ids;
using Silk.NET.Maths;

namespace Olve.Engine3D.Tests.Layout;

public class GuiLayoutServicePositioningTests
{
    private static readonly LayoutContext DefaultContext = new()
    {
        DesignSize = new Vector2D<Dp>(1920, 1080),
        AspectRatio = 16f / 9,
        DpPxRatio = new DpPxRatio(1),
        UiScale = 1,
    };

    private static readonly Id<GuiAnchor> DefaultAnchorId = Id.New<GuiAnchor>();

    private static GuiLayoutService BuildSut(
        GuiNodeService? ge = null,
        LayoutContext? ctx = null)
    {
        var svc = ge ?? new GuiNodeService(NullLogger<GuiNodeService>.Instance);
        GuiAnchorService guiAnchorService = new(NullLogger<GuiAnchorService>.Instance);
        Provider<LayoutContext> lcp = new(ctx ?? DefaultContext);

        // Register the default anchor
        guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight);

        return new GuiLayoutService(NullLogger<GuiLayoutService>.Instance, svc, guiAnchorService, lcp);
    }

    // Shorthand for creating boxes
    private static LayoutBox Box(
        UIAxis axis = UIAxis.X,
        Dp? prefW = null,
        Dp? prefH = null,
        float weight = 0f,
        Dp? horizontalChrome = null,
        Dp? verticalChrome = null)
        => new()
        {
            LayoutAxis = axis,
            Size = new SizeSpec
            {
                PreferredWidth = prefW,
                PreferredHeight = prefH,
                ResizingWeight = weight
            },
            Margin = Thickness.Sym((horizontalChrome ?? Dp.Zero) / 2f, (verticalChrome ?? Dp.Zero) / 2f),
        };

    [Test, NotInParallel]
    public async Task Positions_SingleChild_Horizontal_Defaults_To_Origin()
    {
        // Arrange
        var ge = new GuiNodeService(NullLogger<GuiNodeService>.Instance);
        var sut = BuildSut(ge);

        var parentId = await ge.AddNode("Parent", DefaultAnchorId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(parentId, Box(prefW: 300, prefH: 100)); // parent 300x100

        var childId = await ge.AddNode("Child", parentId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(childId, Box(prefW: 100, prefH: 100));  // child 100x100

        // Act
        var result = sut.ComputeLayout();
        await Assert.That(result).Succeeded();

        // Assert
        await Assert.That(sut.TryGetBoxPosition(parentId, out var parentPos)).IsTrue();
        await Assert.That(parentPos.Position.X.Value).IsEqualTo(0);
        await Assert.That(parentPos.Position.Y.Value).IsEqualTo(0);
        await Assert.That(parentPos.Size.X.Value).IsEqualTo(300);
        await Assert.That(parentPos.Size.Y.Value).IsEqualTo(100);

        await Assert.That(sut.TryGetBoxPosition(childId, out var childPos)).IsTrue();
        await Assert.That(childPos.Position.X.Value).IsEqualTo(0);
        await Assert.That(childPos.Position.Y.Value).IsEqualTo(0);
        await Assert.That(childPos.Size.X.Value).IsEqualTo(100);
        await Assert.That(childPos.Size.Y.Value).IsEqualTo(100);
    }

    [Test, NotInParallel]
    public async Task Positions_TwoChildren_Horizontal_Stack()
    {
        // Arrange
        var ge = new GuiNodeService(NullLogger<GuiNodeService>.Instance);
        var sut = BuildSut(ge);

        var parentId = await ge.AddNode("Parent", DefaultAnchorId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(parentId, Box(prefW: 300, prefH: 100)); // 300x100

        var c1 = await ge.AddNode("C1", parentId).AssertSuccessAndGetAsync();
        var c2 = await ge.AddNode("C2", parentId).AssertSuccessAndGetAsync();

        sut.SetNodeBox(c1, Box(prefW: 120, prefH: 100));
        sut.SetNodeBox(c2, Box(prefW: 180, prefH: 100));

        // Act
        var r = sut.ComputeLayout();
        await Assert.That(r).Succeeded();

        // Assert
        await Assert.That(sut.TryGetBoxPosition(c1, out var p1)).IsTrue();
        await Assert.That(p1.Position.X.Value).IsEqualTo(0);
        await Assert.That(p1.Position.Y.Value).IsEqualTo(0);

        await Assert.That(sut.TryGetBoxPosition(c2, out var p2)).IsTrue();
        await Assert.That(p2.Position.X.Value).IsEqualTo(120); // placed right after C1 (no gap/chrome)
        await Assert.That(p2.Position.Y.Value).IsEqualTo(0);
    }

    [Test, NotInParallel]
    public async Task Positions_TwoChildren_Vertical_Stack()
    {
        // Arrange
        var ge = new GuiNodeService(NullLogger<GuiNodeService>.Instance);
        var sut = BuildSut(ge);

        var parentId = await ge.AddNode("Parent", DefaultAnchorId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(parentId, Box(axis: UIAxis.Y, prefW: 100, prefH: 300)); // vertical parent

        var c1 = await ge.AddNode("C1", parentId).AssertSuccessAndGetAsync();
        var c2 = await ge.AddNode("C2", parentId).AssertSuccessAndGetAsync();

        sut.SetNodeBox(c1, Box(prefW: 100, prefH: 120));
        sut.SetNodeBox(c2, Box(prefW: 100, prefH: 180));

        // Act
        var r = sut.ComputeLayout();
        await Assert.That(r).Succeeded();

        // Assert
        await Assert.That(sut.TryGetBoxPosition(c1, out var p1)).IsTrue();
        await Assert.That(p1.Position.X.Value).IsEqualTo(0);
        await Assert.That(p1.Position.Y.Value).IsEqualTo(0);

        await Assert.That(sut.TryGetBoxPosition(c2, out var p2)).IsTrue();
        await Assert.That(p2.Position.X.Value).IsEqualTo(0);
        await Assert.That(p2.Position.Y.Value).IsEqualTo(120); // stacked below C1
    }

    [Test, NotInParallel]
    public async Task Positions_Nested_LeftRight_With_Fill_Weights()
    {
        // Arrange
        var ge = new GuiNodeService(NullLogger<GuiNodeService>.Instance);
        var sut = BuildSut(ge);

        var parentId = await ge.AddNode("Parent", DefaultAnchorId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(parentId, Box(prefW: 600, prefH: 300));

        var leftId  = await ge.AddNode("Left",  parentId).AssertSuccessAndGetAsync();
        var rightId = await ge.AddNode("Right", parentId).AssertSuccessAndGetAsync();

        // left fixed width, full height (due to sizing pass cross-axis fill)
        sut.SetNodeBox(leftId, Box(axis: UIAxis.Y, prefW: 200, prefH: 180));

        // right gets remaining width by weight=1
        sut.SetNodeBox(rightId, Box(weight: 1f));

        // Two children inside right; split space evenly (weights 1:1)
        var r1 = await ge.AddNode("R1", rightId).AssertSuccessAndGetAsync();
        var r2 = await ge.AddNode("R2", rightId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(r1, Box(weight: 1f));
        sut.SetNodeBox(r2, Box(weight: 1f));

        // Act
        var res = sut.ComputeLayout();
        await Assert.That(res).Succeeded();

        // Assert parent
        await Assert.That(sut.TryGetBoxPosition(parentId, out var p)).IsTrue();
        await Assert.That(p.Position.X.Value).IsEqualTo(0);
        await Assert.That(p.Position.Y.Value).IsEqualTo(0);
        await Assert.That(p.Size.X.Value).IsEqualTo(600);
        await Assert.That(p.Size.Y.Value).IsEqualTo(300);

        // Left: 200x180 at (0,0)
        await Assert.That(sut.TryGetBoxPosition(leftId, out var left)).IsTrue();
        await Assert.That(left.Size.X.Value).IsEqualTo(200);
        await Assert.That(left.Size.Y.Value).IsEqualTo(180);
        await Assert.That(left.Position.X.Value).IsEqualTo(0);
        await Assert.That(left.Position.Y.Value).IsEqualTo(0);

        // Right: 400x300 at (200,0)
        await Assert.That(sut.TryGetBoxPosition(rightId, out var right)).IsTrue();
        await Assert.That(right.Size.X.Value).IsEqualTo(400);
        await Assert.That(right.Size.Y.Value).IsEqualTo(300);
        await Assert.That(right.Position.X.Value).IsEqualTo(200);
        await Assert.That(right.Position.Y.Value).IsEqualTo(0);

        // R1: 200x300 at (200,0)
        await Assert.That(sut.TryGetBoxPosition(r1, out var rp1)).IsTrue();
        await Assert.That(rp1.Size.X.Value).IsEqualTo(200);
        await Assert.That(rp1.Size.Y.Value).IsEqualTo(300);
        await Assert.That(rp1.Position.X.Value).IsEqualTo(200);
        await Assert.That(rp1.Position.Y.Value).IsEqualTo(0);

        // R2: 200x300 at (400,0)
        await Assert.That(sut.TryGetBoxPosition(r2, out var rp2)).IsTrue();
        await Assert.That(rp2.Size.X.Value).IsEqualTo(200);
        await Assert.That(rp2.Size.Y.Value).IsEqualTo(300);
        await Assert.That(rp2.Position.X.Value).IsEqualTo(400);
        await Assert.That(rp2.Position.Y.Value).IsEqualTo(0);
    }

    [Test, NotInParallel]
    public async Task Positions_Respect_Chrome_As_Inner_Content_Offset()
    {
        // Arrange: parent has chrome (total) that reduces content area by 40x20 and shifts origin by 20x10
        var ge = new GuiNodeService(NullLogger<GuiNodeService>.Instance);
        var sut = BuildSut(ge);

        var parentId = await ge.AddNode("Parent", DefaultAnchorId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(parentId, Box(prefW: 300, prefH: 200, horizontalChrome: new Dp(40), verticalChrome: new Dp(20)));

        var c1 = await ge.AddNode("C1", parentId).AssertSuccessAndGetAsync();
        var c2 = await ge.AddNode("C2", parentId).AssertSuccessAndGetAsync();

        sut.SetNodeBox(c1, Box(prefW: 100, prefH: 80));
        sut.SetNodeBox(c2, Box(prefW: 60,  prefH: 80));

        // Act
        var res = sut.ComputeLayout();
        await Assert.That(res).Succeeded();

        // Assert:
        // Parent outer top-left is (0,0); content origin = (20,10)
        await Assert.That(sut.TryGetBoxPosition(c1, out var p1)).IsTrue();
        await Assert.That(p1.Position.X.Value).IsEqualTo(20);
        await Assert.That(p1.Position.Y.Value).IsEqualTo(10);

        await Assert.That(sut.TryGetBoxPosition(c2, out var p2)).IsTrue();
        await Assert.That(p2.Position.X.Value).IsEqualTo(20 + 100); // next to c1 in horizontal flow
        await Assert.That(p2.Position.Y.Value).IsEqualTo(10);
    }

    [Test, NotInParallel]
    public async Task TryGetBoxPosition_Fails_Before_ComputeLayout()
    {
        var ge = new GuiNodeService(NullLogger<GuiNodeService>.Instance);
        var sut = BuildSut(ge);

        var parentId = await ge.AddNode("Parent", DefaultAnchorId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(parentId, Box(prefW: 100, prefH: 50));

        var ok = sut.TryGetBoxPosition(parentId, out _);
        await Assert.That(ok).IsFalse(); // no sizes/position yet
    }
}
