using NotAlone.Models;
using NotAlone.Services;
using NotAlone.Tests.Helpers;
using Xunit;

namespace NotAlone.Tests;

/// <summary>
/// One test per modifier (caught + escaped) going through the full state machine.
/// Creature location and modifier are set manually so results are deterministic.
/// </summary>
public class GameEngineModifierTests
{
    private readonly GameEngine _engine = new();

    // ── DoubleDamage ──────────────────────────────────────────────────────────

    [Fact]
    public void DoubleDamage_WhenCaught_DealsExtraWillpowerDamage()
    {
        var session = TestSessionFactory.Create(willpower: 5);

        TestSessionFactory.RunFullRound(_engine, session, 5, 5, CreatureModifier.DoubleDamage);

        // Base catch: -1, DoubleDamage extra: -1 = 3 total
        Assert.Equal(3, session.PlayerWillpower);
    }

    [Fact]
    public void DoubleDamage_WhenEscaped_NoExtraDamage()
    {
        var session = TestSessionFactory.Create(willpower: 3);

        TestSessionFactory.RunFullRound(_engine, session, 5, 3, CreatureModifier.DoubleDamage);

        Assert.Equal(3, session.PlayerWillpower);
    }

    // ── BlockPlayerProgress ───────────────────────────────────────────────────

    [Fact]
    public void BlockPlayerProgress_WhenEscaped_ProgressNotIncreased()
    {
        var session = TestSessionFactory.Create(playerProgress: 0);

        TestSessionFactory.RunFullRound(_engine, session, 5, 3, CreatureModifier.BlockPlayerProgress);

        Assert.Equal(0, session.PlayerProgress);
        Assert.Contains("заблокирован", session.StatusMessage);
    }

    [Fact]
    public void BlockPlayerProgress_WhenCaught_WillpowerStillDecreases()
    {
        var session = TestSessionFactory.Create(willpower: 3);

        TestSessionFactory.RunFullRound(_engine, session, 5, 5, CreatureModifier.BlockPlayerProgress);

        Assert.Equal(2, session.PlayerWillpower);
    }

    // ── LoseRandomLocation ────────────────────────────────────────────────────

    [Fact]
    public void LoseRandomLocation_WhenCaught_PlayerLosesOneLocation()
    {
        var session = TestSessionFactory.Create();
        var countBeforeRound = session.AvailableLocations.Count - 1; // -1 because PlayRound moves played location

        TestSessionFactory.RunFullRound(_engine, session, 5, 5, CreatureModifier.LoseRandomLocation);

        // Modifier removes one more on top of the played location already moved
        Assert.Equal(countBeforeRound - 1, session.AvailableLocations.Count);
    }

    [Fact]
    public void LoseRandomLocation_WhenEscaped_StillLosesLocation()
    {
        var session = TestSessionFactory.Create();
        var countBeforeRound = session.AvailableLocations.Count - 1; // -1 for played location

        TestSessionFactory.RunFullRound(_engine, session, 5, 3, CreatureModifier.LoseRandomLocation);

        // Modifier removes one location even on escape
        Assert.Equal(countBeforeRound - 1, session.AvailableLocations.Count);
    }
    // ── BeachAndWreckBlock ────────────────────────────────────────────────────

    [Fact]
    public void BeachAndWreckBlock_Beach_SecondVisit_BlocksBeachProgressBonus()
    {
        // Beacon already lit so second visit would normally grant +1 progress
        var session = TestSessionFactory.Create(playerProgress: 0, beaconLit: true);

        TestSessionFactory.RunFullRound(_engine, session, 4, 3, CreatureModifier.BeachAndWreckBlock);

        // Base escape: +1 progress. Beach bonus blocked → no extra +1
        Assert.Equal(1, session.PlayerProgress);
    }

    [Fact]
    public void BeachAndWreckBlock_Wreck_BlocksWreckProgressBonus()
    {
        var session = TestSessionFactory.Create(playerProgress: 0);

        TestSessionFactory.RunFullRound(_engine, session, 8, 3, CreatureModifier.BeachAndWreckBlock);

        // Base escape: +1. Wreck bonus blocked → no extra +1
        Assert.Equal(1, session.PlayerProgress);
    }

    [Fact]
    public void BeachAndWreckBlock_WhenCaught_WillpowerDecreases()
    {
        var session = TestSessionFactory.Create(willpower: 3, beaconLit: true);

        TestSessionFactory.RunFullRound(_engine, session, 4, 4, CreatureModifier.BeachAndWreckBlock);

        Assert.Equal(2, session.PlayerWillpower);
    }

    // ── ExtraCreatureProgress ─────────────────────────────────────────────────

    [Fact]
    public void ExtraCreatureProgress_WhenCaught_GrantsExtraCreatureProgress()
    {
        var session = TestSessionFactory.Create();

        TestSessionFactory.RunFullRound(_engine, session, 5, 5, CreatureModifier.ExtraCreatureProgress);

        // Base catch: +1, ExtraCreatureProgress: +1 extra = 2 total
        Assert.Equal(2, session.CreatureProgress);
    }

    [Fact]
    public void ExtraCreatureProgress_WhenEscaped_NoExtraProgress()
    {
        var session = TestSessionFactory.Create();

        TestSessionFactory.RunFullRound(_engine, session, 5, 3, CreatureModifier.ExtraCreatureProgress);

        Assert.Equal(0, session.CreatureProgress);
    }

    // ── None (Artefact disables modifier) ────────────────────────────────────

    [Fact]
    public void ModifierNone_WhenCaught_OnlyBaseWillpowerLoss()
    {
        var session = TestSessionFactory.Create(willpower: 5);

        TestSessionFactory.RunFullRound(_engine, session, 5, 5, CreatureModifier.None);

        Assert.Equal(4, session.PlayerWillpower);
    }
}
