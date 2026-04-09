using NotAlone.Models;

namespace NotAlone.Services;

public class GameStore
{
    // A simple dictionary to hold games in memory
    public Dictionary<Guid, GameSession> Sessions { get; } = new();
}