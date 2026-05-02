using NotAlone.Models;
using NotAlone.Services;
using NotAlone.Tests.Helpers;
using Xunit;

namespace NotAlone.Tests;

/// <summary>
/// Tests for the core round resolution: caught, escaped, and all three game-over conditions.
/// Each test runs through the full Selection → CreatureTurn → Result state machine.
/// </summary>
public class GameEngineRoundTests
{
    private readonly GameEngine _engine = new();

    [Fact]
    public void Caught_DecreasesPlayerWillpower()
    {
        var session = TestSessionFactory.Create(willpower: 3);

        TestSessionFactory.RunFullRound(_engine, session, playerLocation: 5, creatureLocation: 5);

        Assert.Equal(2, session.PlayerWillpower);
    }

    [Fact]
    public void Caught_IncreasesCreatureProgress()
    {
        var session = TestSessionFactory.Create();

        TestSessionFactory.RunFullRound(_engine, session, playerLocation: 5, creatureLocation: 5);

        Assert.Equal(1, session.CreatureProgress);
    }

    [Fact]
    public void Caught_DoesNotIncreasePlayerProgress()
    {
        var session = TestSessionFactory.Create();

        TestSessionFactory.RunFullRound(_engine, session, playerLocation: 5, creatureLocation: 5);

        Assert.Equal(0, session.PlayerProgress);
    }

    [Fact]
    public void Escaped_IncreasesPlayerProgress()
    {
        var session = TestSessionFactory.Create();

        TestSessionFactory.RunFullRound(_engine, session, playerLocation: 5, creatureLocation: 3);

        Assert.Equal(1, session.PlayerProgress);
    }

    [Fact]
    public void Escaped_DoesNotChangeCreatureProgress()
    {
        var session = TestSessionFactory.Create();

        TestSessionFactory.RunFullRound(_engine, session, playerLocation: 5, creatureLocation: 3);

        Assert.Equal(0, session.CreatureProgress);
    }

    [Fact]
    public void WillpowerReachesZero_CallsGiveUp_RestoresLocations()
    {
        var session = TestSessionFactory.Create(willpower: 1);
        session.AvailableLocations = new List<int> { 5 };
        session.UsedLocations = new List<int> { 1, 2, 3 };

        TestSessionFactory.RunFullRound(_engine, session, playerLocation: 5, creatureLocation: 5);

        // GiveUp restores all used locations and resets willpower to 3
        Assert.Equal(3, session.PlayerWillpower);
        Assert.Contains(1, session.AvailableLocations);
        Assert.Contains(2, session.AvailableLocations);
        Assert.Contains(3, session.AvailableLocations);
    }
    [Fact]
    public void CreatureReachesMaxProgress_GameOver_CreatureWins()
    {
        var session = TestSessionFactory.Create(creatureProgress: GameSession.MaxCreatureProgress - 1);

        TestSessionFactory.RunFullRound(_engine, session, playerLocation: 5, creatureLocation: 5);

        Assert.True(session.IsGameOver);
        Assert.Contains("ассимилировало", session.StatusMessage);
    }

    [Fact]
    public void PlayerReachesMaxProgress_GameOver_PlayerWins()
    {
        var session = TestSessionFactory.Create(playerProgress: GameSession.MaxPlayerProgress - 1);

        TestSessionFactory.RunFullRound(_engine, session, playerLocation: 5, creatureLocation: 3);

        Assert.True(session.IsGameOver);
        Assert.Contains("Спасение прибыло", session.StatusMessage);
    }

    [Fact]
    public void ResolveRound_WrongPhase_DoesNothing()
    {
        var session = TestSessionFactory.Create();
        session.CurrentPhase = GamePhase.Selection;

        _engine.ResolveRound(session);

        Assert.Equal(0, session.PlayerProgress);
        Assert.Equal(0, session.CreatureProgress);
    }

    [Fact]
    public void PlayRound_UnavailableLocation_RejectsChoice()
    {
        var session = TestSessionFactory.Create(availableLocations: new List<int> { 1, 2, 3 });

        _engine.PlayRound(session, 9);

        Assert.DoesNotContain(9, session.UsedLocations);
        Assert.Contains("недоступна", session.StatusMessage);
    }

    [Fact]
    public void PlayRound_MovesLocationFromAvailableToUsed()
    {
        var session = TestSessionFactory.Create();

        _engine.PlayRound(session, 5);

        Assert.DoesNotContain(5, session.AvailableLocations);
        Assert.Contains(5, session.UsedLocations);
    }
}
