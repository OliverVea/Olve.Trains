using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Utilities;
using Olve.Results.TUnit;
using Olve.Utilities.Ids;
using Silk.NET.Maths;

namespace Olve.Engine3D.Tests.Layout;

public class GuiLayoutServiceTests
{
    private static readonly LayoutContext DefaultContext = new()
    {
        DesignSize = new Vector2D<Dp>(1920, 1080),
        AspectRatio = 16f / 9,
        DpPxRatio = new DpPxRatio(1),
        UiScale = 1,
    };

    private static readonly Id<GuiAnchor> DefaultAnchorId = Id.New<GuiAnchor>();

    private static (GuiNodeService, Provider<LayoutContext>, GuiLayoutService) BuildSut(LayoutContext? layoutContext = null)
    {
        GuiNodeService guiNodeService = new(NullLogger<GuiNodeService>.Instance);
        GuiAnchorService guiAnchorService = new(NullLogger<GuiAnchorService>.Instance);
        Provider<LayoutContext> layoutContextProvider = new(layoutContext ?? DefaultContext);
        GuiLayoutService guiLayoutService = new(NullLogger<GuiLayoutService>.Instance, guiNodeService, guiAnchorService, layoutContextProvider);

        // Register the default anchor
        guiAnchorService.RegisterAnchor(AnchorPosition.TopLeft, GrowthDirection.DownRight);

        return (guiNodeService, layoutContextProvider, guiLayoutService);
    }

    [Test, NotInParallel]
    public async Task ComputeLayout_EmptyConfiguration_Succeeds()
    {
        var (_, _, sut) = BuildSut();
        var result = sut.ComputeLayout();
        await Assert.That(result).Succeeded();
    }

    [Test, NotInParallel]
    [MethodDataSource(typeof(GuiLayoutServiceTestData),
        nameof(GuiLayoutServiceTestData.AdditionTestData))]
    public async Task NestedWidthTest(string testName,
        LayoutBox parent,
        Vector2D<Px> expectedParentSize,
        IReadOnlyCollection<LayoutBox> children,
        IReadOnlyCollection<Vector2D<Px>> expectedChildrenSizes)
    {
        // Arrange
        var (guiNodeService, _, sut) = BuildSut();

        var parentResult = guiNodeService.AddNode("Parent", DefaultAnchorId);
        await Assert.That(parentResult).Succeeded();
        var parentId = parentResult.Value;
        sut.SetNodeBox(parentId, parent);

        var childIds = new Id<GuiNode>[children.Count];

        var i = 0;
        foreach (var child in children)
        {
            var childResult = guiNodeService.AddNode("Child_" + (i + 1), parentId);
            await Assert.That(childResult).Succeeded();
            sut.SetNodeBox(childResult.Value, child);

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
        var (ge, _, sut) = BuildSut(DefaultContext);

        // Create the tree
        var anchorId = DefaultAnchorId; // Parent anchored to root anchor

        var parentId = await ge.AddNode("Parent", anchorId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(parentId, Box(UIAxis.X, prefW: 600, prefH: 300)); // 600x300 parent

        // Left panel (fixed W=200, fills height=300 via parent)
        var leftPanelId = await ge.AddNode("LeftPanel", parentId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(leftPanelId, Box(UIAxis.Y, prefW: 200)); // width 200, vertical stack

        // Top row inside left panel (200x100)
        var topRowId = await ge.AddNode("TopRow", leftPanelId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(topRowId, Box(UIAxis.X, prefW: 200, prefH: 100));

        var leaf1Id = await ge.AddNode("Leaf1", topRowId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(leaf1Id, Box(prefW: 100, prefH: 100));

        var leaf2Id = await ge.AddNode("Leaf2", topRowId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(leaf2Id, Box(prefW: 100, prefH: 100));

        // Bottom box inside left panel (200x200)
        var bottomBoxId = await ge.AddNode("BottomBox", leftPanelId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(bottomBoxId, Box(prefW: 200, prefH: 200));

        // Right panel (takes remaining width 400, fills height 300)
        var rightPanelId = await ge.AddNode("RightPanel", parentId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(rightPanelId, Box(UIAxis.X, weight: 1f)); // width determined by leftover

        var grow1Id = await ge.AddNode("Grow1", rightPanelId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(grow1Id, Box(weight: 1f));

        var grow2Id = await ge.AddNode("Grow2", rightPanelId).AssertSuccessAndGetAsync();
        sut.SetNodeBox(grow2Id, Box(weight: 1f));

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
        await Assert.That(leftPos.Position.X.Value).IsEqualTo(0); // depends on absolute positioning
        await Assert.That(leftPos.Position.Y.Value).IsEqualTo(0);

        // Top row: 200x100 at Y=0 inside left panel
        await Assert.That(sut.TryGetBoxPosition(topRowId, out var topRowPos)).IsTrue();
        await Assert.That(topRowPos.Size.X.Value).IsEqualTo(200);
        await Assert.That(topRowPos.Size.Y.Value).IsEqualTo(100);
        await Assert.That(topRowPos.Position.X.Value).IsEqualTo(0);
        await Assert.That(topRowPos.Position.Y.Value).IsEqualTo(0);

        // Leaves: 100x100 each; Leaf2 sits to the right of Leaf1
        await Assert.That(sut.TryGetBoxPosition(leaf1Id, out var leaf1Pos)).IsTrue();
        await Assert.That(leaf1Pos.Size.X.Value).IsEqualTo(100);
        await Assert.That(leaf1Pos.Size.Y.Value).IsEqualTo(100);
        await Assert.That(leaf1Pos.Position.X.Value).IsEqualTo(0);
        await Assert.That(leaf1Pos.Position.Y.Value).IsEqualTo(0);

        await Assert.That(sut.TryGetBoxPosition(leaf2Id, out var leaf2Pos)).IsTrue();
        await Assert.That(leaf2Pos.Size.X.Value).IsEqualTo(100);
        await Assert.That(leaf2Pos.Size.Y.Value).IsEqualTo(100);
        await Assert.That(leaf2Pos.Position.X.Value).IsEqualTo(100);
        await Assert.That(leaf2Pos.Position.Y.Value).IsEqualTo(0);

        // Bottom box: 200x200 stacked under top row
        await Assert.That(sut.TryGetBoxPosition(bottomBoxId, out var bottomPos)).IsTrue();
        await Assert.That(bottomPos.Size.X.Value).IsEqualTo(200);
        await Assert.That(bottomPos.Size.Y.Value).IsEqualTo(200);
        await Assert.That(bottomPos.Position.X.Value).IsEqualTo(0);
        await Assert.That(bottomPos.Position.Y.Value).IsEqualTo(100);

        // Right panel: 400x300 placed to the right of left panel at X=200
        await Assert.That(sut.TryGetBoxPosition(rightPanelId, out var rightPos)).IsTrue();
        await Assert.That(rightPos.Size.X.Value).IsEqualTo(400);
        await Assert.That(rightPos.Size.Y.Value).IsEqualTo(300);
        await Assert.That(rightPos.Position.X.Value).IsEqualTo(200);
        await Assert.That(rightPos.Position.Y.Value).IsEqualTo(0);

        // Grow children split the 400 width evenly (weights 1:1)
        await Assert.That(sut.TryGetBoxPosition(grow1Id, out var g1)).IsTrue();
        await Assert.That(g1.Size.X.Value).IsEqualTo(200);
        await Assert.That(g1.Size.Y.Value).IsEqualTo(300);
        await Assert.That(g1.Position.X.Value).IsEqualTo(200);
        await Assert.That(g1.Position.Y.Value).IsEqualTo(0);

        await Assert.That(sut.TryGetBoxPosition(grow2Id, out var g2)).IsTrue();
        await Assert.That(g2.Size.X.Value).IsEqualTo(200);
        await Assert.That(g2.Size.Y.Value).IsEqualTo(300);
        await Assert.That(g2.Position.X.Value).IsEqualTo(400);
        await Assert.That(g2.Position.Y.Value).IsEqualTo(0);

        return;
    }

    // Helper to reduce noise
    private static LayoutBox Box(UIAxis axis = UIAxis.X, Dp? prefW = null, Dp? prefH = null, float weight = 0f) => new()
    {
        LayoutAxis = axis,
        Size = new SizeSpec
        {
            PreferredWidth = prefW,
            PreferredHeight = prefH,
            ResizingWeight = weight, // ensure this matches your actual property name
        },
    };
}
