using Engine.Camera.Views;
using Microsoft.Xna.Framework;

namespace Engine.Tests;

public class OrbitalViewTests
{
    [TestCase(MathF.PI / 6, MathF.PI / 4)]
    [TestCase(MathF.PI / 2, MathF.PI / 3)]
    [TestCase(-MathF.PI / 4, MathF.PI / 4)]
    [TestCase(0f, 0f)]
    [TestCase(MathF.PI, -MathF.PI / 2)]
    public void QuaternionToYawPitchRoll_And_Back_ReturnsSameValues(float yaw, float pitch)
    {
        // Arrange
        var target = new Vector3(0, 0, 0);
        var orbitalView = new OrbitalView
        {
            Target = target,
            Yaw = yaw,
            Pitch = pitch
        };

        // Act
        var rotation = orbitalView.Rotation;
        orbitalView.Rotation = rotation;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(orbitalView.Yaw, Is.EqualTo(yaw).Within(0.01f), $"Failed for yaw: {yaw}");
            Assert.That(orbitalView.Pitch, Is.EqualTo(pitch).Within(0.01f), $"Failed for pitch: {pitch}");
        });
    }
}
