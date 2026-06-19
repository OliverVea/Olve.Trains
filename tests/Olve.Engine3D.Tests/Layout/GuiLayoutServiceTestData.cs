using Olve.Engine3D.GUI.Layout;

namespace Olve.Engine3D.Tests.Layout;

public static class GuiLayoutServiceTestData
{
    // Expected sizes are plain (Width, Height) int pairs rather than Vector2D<Px>: Silk's
    // Vector2D<T> routes equality/hashing through Scalar<T>, which only supports built-in
    // numerics and throws for Px. TUnit hashes test parameters, so a Vector2D<Px> parameter
    // would throw during test registration.
    public static IEnumerable<Func<(string TestName,
        LayoutBox Parent,
        (int Width, int Height) ExpectedParentSize,
        IReadOnlyCollection<LayoutBox> Children,
        IReadOnlyCollection<(int Width, int Height)> ExpectedChildrenSizes)>> AdditionTestData()
    {
        yield return () => (
            "Single box",
            Box(prefW: 150, prefH: 100),
            (150, 100),
            [],
            []);
        yield return () => (
            "Single child box",
            Box(),
            (150, 100),
            [Box(prefW: 150, prefH: 100)],
            [(150, 100)]);
        yield return () => (
            "Single child box with larger parent",
            Box(prefW: 400, prefH: 350),
            (400, 350),
            [Box(prefW: 150, prefH: 100)],
            [(150, 100)]);
        yield return () => (
            "Two child boxes (horizontal)",
            Box(),
            (300, 100),
            [Box(prefW: 150, prefH: 100), Box(prefW: 150, prefH: 100)],
            [(150, 100), (150, 100)]);
        yield return () => (
            "Two child boxes (vertical)",
            Box(axis: UIAxis.Y),
            (150, 200),
            [Box(prefW: 150, prefH: 100), Box(prefW: 150, prefH: 100)],
            [(150, 100), (150, 100)]);
        yield return () => (
            "Child box grows",
            Box(prefW: 150, prefH: 100),
            (150, 100),
            [Box(resizingWeight: 1f)],
            [(150, 100)]);
        yield return () => (
            "Two child boxes grow with weight",
            Box(prefW: 600, prefH: 200),
            (600, 200),
            [Box(resizingWeight: 2f), Box(resizingWeight: 1f)],
            [(400, 200), (200, 200)]);
        yield return () => (
            "Two child boxes grow vertically with weight",
            Box(axis: UIAxis.Y, prefW: 600, prefH: 300),
            (600, 300),
            [Box(resizingWeight: 2f), Box(resizingWeight: 1f)],
            [(600, 200), (600, 100)]);
        yield return () => (
            "Mixed preferred + weighted (horizontal)",
            Box(prefW: 500, prefH: 100),
            (500, 100),
            [
                Box(prefW: 150, prefH: 100),
                Box(resizingWeight: 1f),
                Box(resizingWeight: 2f)
            ],
            // leftover = 350; weights 1:2 => 116/234 if rounding, pick a policy (e.g., 117/233)
            [
                (150, 100),
                (117, 100),
                (233, 100)
            ]);
        yield return () => (
            "Zero-weight child stays preferred",
            Box(prefW: 300, prefH: 100),
            (300, 100),
            [
                Box(prefW: 100, prefH: 100),
                Box(resizingWeight: 1f),
                Box(resizingWeight: 0f)
            ],
            [
                (100, 100),
                (200, 100),
                (0, 0)
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
