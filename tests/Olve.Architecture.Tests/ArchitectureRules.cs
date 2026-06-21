using System.Reflection;
using NetArchTest.Rules;

namespace Olve.Architecture.Tests;

// Architecture fitness functions — enforce the layering/cohesion rules documented in
// CLAUDE.md so changes can't silently cross boundaries. Run via `scripts/quality.sh`.
public class ArchitectureRules
{
    static readonly Assembly Engine = typeof(Olve.Engine3D.Rendering.RenderingManager).Assembly;
    static readonly Assembly Game = typeof(Olve.Trains.GameServiceRegistration).Assembly;

    static void AssertNoDependency(Assembly asm, string fromNamespace, params string[] forbidden)
    {
        var result = Types.InAssembly(asm)
            .That().ResideInNamespace(fromNamespace)
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();
        if (!result.IsSuccessful)
        {
            var offenders = result.FailingTypes is null ? "" : string.Join("\n    ", result.FailingTypes.Select(x => x.ToString()));
            throw new Exception(
                $"Architecture rule violated: '{fromNamespace}' must not depend on [{string.Join(", ", forbidden)}].\n  Offending types:\n    {offenders}");
        }
    }

    // The engine library must never know about the game built on top of it.
    [Test]
    public void Engine_does_not_depend_on_the_game()
        => AssertNoDependency(Engine, "Olve.Engine3D", "Olve.Trains");

    // Simulation/domain code must not depend on how it is drawn or its UI.
    [Test]
    public void GameLogic_does_not_depend_on_presentation()
        => AssertNoDependency(Game, "Olve.Trains.Scenes.GameLogic",
            "Olve.Trains.Scenes.GameRendering", "Olve.Trains.Scenes.GameUI");

}
