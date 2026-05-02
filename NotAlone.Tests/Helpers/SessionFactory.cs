using NotAlone.Models;
using NotAlone.Services;

namespace NotAlone.Tests.Helpers;

/// <summary>
/// Shared helpers for constructing test sessions and running full phase-state-machine rounds.
/// </summary>
public static class TestSessionFactory
{
    /// <summary>
    /// Creates a clean GameSession ready for the Selection phase.
    /// </summary>
    public static GameSession Create(
        int willpower = 3,
        int playerProgress = 0,
        int creatureProgress = 0,
        bool beaconLit = false,
        List<int>? availableLocations = null)
    {
        return new GameSession
        {
            PlayerWillpower    = willpower,
            PlayerProgress     = playerProgress,
            CreatureProgress   = creatureProgress,
            IsBeaconLit        = beaconLit,
            AvailableLocations = availableLocations ?? new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
            UsedLocations      = new List<int>(),
            CurrentPhase       = GamePhase.Selection,
        };
    }

    /// <summary>
    /// Runs one complete round through the full state machine:
    /// Selection → CreatureTurn → Result.
    /// Creature location and modifier are set manually to keep tests deterministic.
    /// </summary>
    public static void RunFullRound(
        GameEngine engine,
        GameSession session,
        int playerLocation,
        int creatureLocation,
        CreatureModifier modifier = CreatureModifier.None,
        int? creatureBlockingLocation = null)
    {
        // Phase 1 — Selection
        engine.PlayRound(session, playerLocation);
        session.CurrentPhase = GamePhase.CreatureTurn;

        // Phase 2 — CreatureTurn (set manually; avoids randomness from CreatureLogic)
        session.CreatureChosenLocation    = creatureLocation;
        session.CreatureBlockingLocation  = creatureBlockingLocation;
        session.CurrentModifier           = modifier;
        session.CurrentPhase              = GamePhase.Result;

        // Phase 3 — Result
        engine.ResolveRound(session);

        if (!session.IsGameOver)
        {
            session.CurrentPhase = GamePhase.Selection;
            // Save current choices to previous for next potential round
            session.PreviousPlayerChoice = session.CurrentPlayerChoice;
            session.PreviousCreatureChoice = session.CreatureChosenLocation;
        }
    }
}
