using NotAlone.Models;
using NotAlone.Services;
using NotAlone.Tests.Helpers;
using Xunit;

namespace NotAlone.Tests;

/// <summary>
/// Placeholder tests for CreatureLogic.
/// Add tests here as CreatureLogic behaviour needs to be verified.
/// </summary>
public class CreatureLogicTests
{
    private readonly CreatureLogic _logic = new();

    [Fact]
    public void SelectCreatureLocation_SetsCreatureChosenLocation()
    {
        var session = TestSessionFactory.Create();
        session.CurrentPhase = GamePhase.CreatureTurn;
        session.CurrentPlayerChoice = 5;
        session.PreviousPlayerChoice = 5;

        _logic.SelectCreatureLocation(session);

        Assert.True(session.CreatureChosenLocation.HasValue);
        Assert.InRange(session.CreatureChosenLocation!.Value, 1, 10);
    }

    [Fact]
    public void SelectCreatureLocation_WhenArtefactActive_SetsModifierNone()
    {
        var session = TestSessionFactory.Create();
        session.CurrentPhase = GamePhase.CreatureTurn;
        session.IsArtefactActive = true;
        session.CurrentPlayerChoice = 5;
        session.PreviousPlayerChoice = 5;

        _logic.SelectCreatureLocation(session);

        Assert.Equal(CreatureModifier.None, session.CurrentModifier);
    }

    [Fact]
    public void SelectCreatureLocation_WhenArtefactNotActive_SetsNonNoneModifier()
    {
        // Without artefact the modifier should be one of the 5 active modifiers
        // (there is a small chance it lands on None only if artefact is active, which it isn't here)
        var session = TestSessionFactory.Create();
        session.CurrentPhase = GamePhase.CreatureTurn;
        session.IsArtefactActive = false;
        session.CurrentPlayerChoice = 5;
        session.PreviousPlayerChoice = 5;

        _logic.SelectCreatureLocation(session);

        Assert.NotEqual(CreatureModifier.None, session.CurrentModifier);
    }
}
