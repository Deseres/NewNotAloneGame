using NotAlone.Models;
using NotAlone.Services;
using NotAlone.Tests.Helpers;
using Xunit;

namespace NotAlone.Tests;

/// <summary>
/// Tests for key location special effects.
/// Each test escapes the player (creature goes elsewhere) and verifies the location's effect fires.
/// A second test per location verifies the effect is suppressed when the location is blocked.
/// </summary>
public class GameEngineLocationTests
{
    private readonly GameEngine _engine = new();

    // ── Jungle (2) ────────────────────────────────────────────────────────────

    [Fact]
    public void Jungle_WhenEscaped_RestoresOneUsedLocation()
    {
        var session = TestSessionFactory.Create();
        session.AvailableLocations = new List<int> { 2, 5, 6, 7 };
        session.UsedLocations = new List<int> { 3 }; // something to restore

        TestSessionFactory.RunFullRound(_engine, session, 2, 9);

        Assert.Contains(3, session.AvailableLocations);
    }

    [Fact]
    public void Jungle_WhenEscaped_CardReturnedToHand()
    {
        var session = TestSessionFactory.Create();
        session.AvailableLocations = new List<int> { 2, 5, 6, 7 };
        session.UsedLocations = new List<int> { 3 };

        TestSessionFactory.RunFullRound(_engine, session, 2, 9);

        // Jungle returns itself to available
        Assert.Contains(2, session.AvailableLocations);
    }

    [Fact]
    public void Jungle_WhenBlocked_NoRestoreAndNotReturnedToHand()
    {
        var session = TestSessionFactory.Create(playerProgress: 4);
        session.AvailableLocations = new List<int> { 2, 5, 6, 7 };
        session.UsedLocations = new List<int> { 3 };

        // Blocking location = 2, modifier must not be None
        TestSessionFactory.RunFullRound(_engine, session, 2, 9, CreatureModifier.DoubleDamage, creatureBlockingLocation: 2);

        Assert.DoesNotContain(3, session.AvailableLocations);
        Assert.DoesNotContain(2, session.AvailableLocations);
    }

    // ── River (3) ─────────────────────────────────────────────────────────────

    [Fact]
    public void River_WhenEscaped_ActivatesRiverVision()
    {
        var session = TestSessionFactory.Create();

        TestSessionFactory.RunFullRound(_engine, session, 3, 9);

        Assert.True(session.IsRiverVisionActive);
    }

    [Fact]
    public void River_WhenBlocked_RiverVisionNotActivated()
    {
        var session = TestSessionFactory.Create(playerProgress: 4);
        session.AvailableLocations = new List<int> { 3, 5, 6, 7 };

        TestSessionFactory.RunFullRound(_engine, session, 3, 9, CreatureModifier.DoubleDamage, creatureBlockingLocation: 3);

        Assert.False(session.IsRiverVisionActive);
    }

    // ── Beach (4) ─────────────────────────────────────────────────────────────

    [Fact]
    public void Beach_FirstVisit_LightsBeacon()
    {
        var session = TestSessionFactory.Create(beaconLit: false);

        TestSessionFactory.RunFullRound(_engine, session, 4, 9);

        Assert.True(session.IsBeaconLit);
    }

    [Fact]
    public void Beach_SecondVisit_GrantsProgress()
    {
        var session = TestSessionFactory.Create(playerProgress: 0, beaconLit: true);

        TestSessionFactory.RunFullRound(_engine, session, 4, 9);

        // Base escape +1, beach bonus +1 = 2
        Assert.Equal(2, session.PlayerProgress);
    }

    // ── Wreck (8) ─────────────────────────────────────────────────────────────

    [Fact]
    public void Wreck_WhenEscaped_GrantsExtraProgress()
    {
        var session = TestSessionFactory.Create(playerProgress: 0);

        TestSessionFactory.RunFullRound(_engine, session, 8, 9);

        // Base escape +1, Wreck +1 = 2
        Assert.Equal(2, session.PlayerProgress);
    }

    [Fact]
    public void Wreck_WhenBlocked_NoExtraProgress()
    {
        var session = TestSessionFactory.Create(playerProgress: 0, availableLocations: new List<int> { 8, 5, 6, 7, 9 });

        TestSessionFactory.RunFullRound(_engine, session, 8, 9, CreatureModifier.BeachAndWreckBlock);

        Assert.Equal(1, session.PlayerProgress); // base escape only
    }

    // ── Source (9) ────────────────────────────────────────────────────────────

    [Fact]
    public void Source_WhenEscaped_RestoresWillpower()
    {
        var session = TestSessionFactory.Create(willpower: 2);

        TestSessionFactory.RunFullRound(_engine, session, 9, 5);

        Assert.Equal(3, session.PlayerWillpower);
    }

    [Fact]
    public void Source_AtMaxWillpower_DoesNotExceedMax()
    {
        var session = TestSessionFactory.Create(willpower: GameSession.MaxWillpower);

        TestSessionFactory.RunFullRound(_engine, session, 9, 5);

        Assert.Equal(GameSession.MaxWillpower, session.PlayerWillpower);
    }

    // ── Artefact (10) ─────────────────────────────────────────────────────────

    [Fact]
    public void Artefact_WhenEscaped_SetsArtefactActiveFlag()
    {
        var session = TestSessionFactory.Create();

        TestSessionFactory.RunFullRound(_engine, session, 10, 5);

        // Artefact activates and is then consumed at end of round — IsArtefactActive resets to false
        // The flag was set and cleared within the same round; check the status message instead
        Assert.Contains("Артефакт", session.StatusMessage);
    }

    // ── Lair (1) ──────────────────────────────────────────────────────────────

    [Fact]
    public void Lair_WhenEscaped_CopiesCreatureLocationEffect_Source()
    {
        // Creature went to Source (9), player escaped via Lair (1) → should restore willpower
        var session = TestSessionFactory.Create(willpower: 2);

        TestSessionFactory.RunFullRound(_engine, session, 1, 9);

        Assert.Equal(3, session.PlayerWillpower);
    }

    [Fact]
    public void Lair_WhenCaught_DealsDoubleWillpowerDamage()
    {
        var session = TestSessionFactory.Create(willpower: 5);

        TestSessionFactory.RunFullRound(_engine, session, 1, 1);

        // Base catch: -1, Lair special: -1 extra = 3 total
        Assert.Equal(3, session.PlayerWillpower);
    }

    // ── Second-phase blocking (generic) ───────────────────────────────────────

    [Fact]
    public void SecondPhaseBlocking_WithModifierNone_BlockingIsDisabled()
    {
        // When modifier is None, blocking should not suppress location effects
        var session = TestSessionFactory.Create(playerProgress: 4, willpower: 2);
        session.AvailableLocations = new List<int> { 9, 5, 6, 7 };

        // blockingLocation = 9, but modifier = None → blocking inactive
        TestSessionFactory.RunFullRound(_engine, session, 9, 5, CreatureModifier.None, creatureBlockingLocation: 9);

        // Source should still fire
        Assert.Equal(3, session.PlayerWillpower);
    }
}
