namespace NotAlone.Models;

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int PlayerWillpower { get; set; } = 3;
    public int PlayerProgress { get; set; } = 0;
    public int CreatureProgress { get; set; } = 0;
    public int? LastCreatureChoice { get; set; }
    public bool IsGameOver { get; set; } = false;
    public string StatusMessage { get; set; } = "Game Started. Survival is unlikely.";


    // All possible locations
    public int[] Locations { get; } = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    // Player's available and used locations
    public List<int> AvailableLocations { get; set; } = new List<int> { 1, 2, 3, 4, 5 };
    public List<int> UsedLocations { get; set; } = new List<int>();


    
    // Winning thresholds
    public const int MaxProgress = 7;
}