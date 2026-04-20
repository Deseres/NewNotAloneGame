using NotAlone.Models;
using Microsoft.EntityFrameworkCore;

namespace NotAlone.Services;

public class GameStore
{
    private readonly AppDbContext _context;

    public GameStore(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all active game sessions
    /// </summary>
    public async Task<IEnumerable<GameSession>> GetAllSessionsAsync()
    {
        return await _context.GameSessions.ToListAsync();
    }

    /// <summary>
    /// Get a specific game session by ID
    /// </summary>
    public async Task<GameSession?> GetSessionAsync(Guid sessionId)
    {
        return await _context.GameSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    /// <summary>
    /// Create and save a new game session
    /// </summary>
    public async Task<GameSession> CreateSessionAsync(GameSession session)
    {
        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    /// <summary>
    /// Update and save an existing game session
    /// </summary>
    public async Task UpdateSessionAsync(GameSession session)
    {
        _context.GameSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Delete a game session
    /// </summary>
    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session != null)
        {
            _context.GameSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// In-memory cache for performance (optional, can be removed if not needed)
    /// </summary>
    public Dictionary<Guid, GameSession> Sessions { get; } = new();
}