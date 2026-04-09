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
    
    // Winning thresholds
    public const int MaxProgress = 10;
}