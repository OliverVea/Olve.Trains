using Olve.Engine3D.GUI.Layout;
using Silk.NET.Maths;

namespace Olve.Engine3D.Tests.Layout;

public static class GuiLayoutServiceTestData
{
    public static IEnumerable<Func<(string TestName,
        LayoutBox Parent,
        Vector2D<Px> ExpectedParentSize,
        IReadOnlyCollection<LayoutBox> Children,
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

    private static LayoutBox Box(UIAxis axis = UIAxis.X, Dp? prefW = null, Dp? prefH = null,
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