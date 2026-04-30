namespace NotAlone.Models;

public enum GamePhase { Selection, CreatureTurn, Result, GameOver }

public enum CreatureModifier
{
	None = 0,
	DoubleDamage = 1,           // CreatureProgress increases by 2 instead of 1
	BlockPlayerProgress = 2,    // Player progress won't increase on escape
	LoseRandomLocation = 3,      // Player loses one random available location

    BeachAndWreckBlock = 4, //Blocks effect of Beach and Wreck locations

    ExtraCreatureProgress = 5 // Creature progress increases by 2 instead of 1, but player can still progress normally


}

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// When this game session was started
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int PlayerWillpower { get; set; } = 3;
    public int PlayerProgress { get; set; } = 0;
    public int CreatureProgress { get; set; } = 0;
    public int? LastCreatureChoice { get; set; }
    public int? LastPlayerChoice { get; set; }
    public bool IsGameOver { get; set; } = false;
    public bool IsBeaconLit { get; set; } = false;
    // River vision: when true, next round the Creature's move will be visible to player
    public bool IsRiverVisionActive { get; set; } = false;
    // Internal: indicates that the next-round pre-generation (reveal) has been done
    public bool IsRiverVisionRevealed { get; set; } = false;

    public bool IsFogActive { get; set; } = false;

    public string StatusMessage { get; set; } = "Game Started. Survival is unlikely.";

    // Current game phase
    public GamePhase CurrentPhase { get; set; } = GamePhase.Selection;

    // Survival Cards
    public List<int> AvailableSurvivalCards { get; set; } = new();

    public List<int> SurvivalCards { get; set; } = new List<int> { 1, 2, 3, 4, 5 };
    public List<int> UsedSurvivalCards { get; set; } = new();
    public List<int> ActiveCardEffects { get; set; } = new();

    // All possible locations
    public int[] Locations { get; } = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    // Track number of rounds played
    public int RoundNumber { get; set; } = 0;

    // Player's available and used locations
    public List<int> AvailableLocations { get; set; } = new List<int> { 1, 2, 3, 4, 5 };
    public List<int> UsedLocations { get; set; } = new List<int>();

    // Creature's chosen location (for deferred comparison in ResolveRound)
    public int? CreatureChosenLocation { get; set; }

    // Creature's blocking location in second phase (player progress >= 4)
    // When creature chooses this location, it negates the location's special effect
    public int? CreatureBlockingLocation { get; set; }

    public bool IsArtefactActive { get; set; } = false;

    public CreatureModifier CurrentModifier { get; set; } = CreatureModifier.None;

    // Winning thresholds
    public const int MaxPlayerProgress = 7;
    public const int MaxCreatureProgress = 5;

    public const int MaxWillpower = 3;
}