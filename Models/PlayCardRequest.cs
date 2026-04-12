namespace NotAlone.Models;

public enum CardDirection { Left = -1, Right = 1 }

public class PlayCardRequest
{
    public List<int>? TargetLocationIds { get; set; }
    public CardDirection? Direction { get; set; }
}
