using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotAlone.Models;
using NotAlone.Services;
using System.Security.Claims;

namespace NotAlone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HistoryController(AppDbContext dbContext) => _dbContext = dbContext;

    /// <summary>Returns the authenticated user's game history, newest first.</summary>
    [HttpGet]
    [Authorize]
    public IActionResult GetMyHistory()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "❌ Invalid token." });

        var history = BuildQuery(gh => gh.UserId == userId);
        return Ok(new { total = history.Count, history });
    }

    /// <summary>Returns game history for all users (public leaderboard feed).</summary>
    [HttpGet("all")]
    public IActionResult GetAllHistory()
    {
        var history = BuildQuery();
        return Ok(new { total = history.Count, history });
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private List<GameHistoryDto> BuildQuery(Func<GameHistory, bool>? filter = null)
    {
        var query = _dbContext.GameHistories.AsEnumerable();
        if (filter is not null) query = query.Where(filter);

        return query
            .OrderByDescending(gh => gh.CompletedAt)
            .Select(gh => new GameHistoryDto
            {
                Id               = gh.Id,
                UserId           = gh.UserId,
                CompletedAt      = gh.CompletedAt,
                RoundsPlayed     = gh.RoundsPlayed,
                PlayerProgress   = gh.PlayerProgress,
                CreatureProgress = gh.CreatureProgress,
                Result           = gh.Result,
                DurationSeconds  = gh.DurationSeconds
            })
            .ToList();
    }
}

public class GameHistoryDto
{
    public Guid     Id               { get; set; }
    public Guid     UserId           { get; set; }
    public DateTime CompletedAt      { get; set; }
    public int      RoundsPlayed     { get; set; }
    public int      PlayerProgress   { get; set; }
    public int      CreatureProgress { get; set; }
    public string   Result           { get; set; } = "Loss";
    public int      DurationSeconds  { get; set; }
}
