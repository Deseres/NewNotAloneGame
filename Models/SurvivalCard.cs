namespace NotAlone.Models;

public class SurvivalCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SurvivalCardType Type { get; set; }
    public GamePhase PlayablePhase { get; set; }
}
