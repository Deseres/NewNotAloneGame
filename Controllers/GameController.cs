using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotAlone.Models;
using NotAlone.Services;
using System.Security.Claims;

namespace NotAlone.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly GameStore _store;
    private readonly GameEngine _engine;
    private readonly CreatureLogic _creatureLogic;

    public GameController(GameStore store, GameEngine engine, CreatureLogic creatureLogic)
    {
        _store = store;
        _engine = engine;
        _creatureLogic = creatureLogic;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GameSession>> GetSession(Guid id)
    {
        var session = await _store.GetSessionAsync(id);
        if (session is null)
            return NotFound(new { error = "❌ Игровая сессия не найдена." });
        
        return Ok(new { session = session });
    }

    [HttpPost("start")]
    public async Task<ActionResult<GameSession>> StartGame()
    {
        var session = new GameSession();
        // Give player one random survival card (IDs 1-5)
        int randomCard = Random.Shared.Next(1, 6);
        session.AvailableSurvivalCards = new List<int> { randomCard };
        session.StatusMessage = "🎮 Игра началась! Выживание маловероятно. Выберите локацию для начала.";
        await _store.CreateSessionAsync(session);
        _store.Sessions[session.Id] = session;
        return Ok(new { message = session.StatusMessage, session = session });
    }

    [HttpPost("{id}/play")]
    public async Task<ActionResult<GameSession>> PlayRound(Guid id, [FromBody] int playerLocation)
    {
        var session = await _store.GetSessionAsync(id);
        if (session == null)
            return NotFound(new { error = "❌ Игровая сессия не найдена." });

        if (session.IsGameOver)
            return BadRequest(new { error = "❌ Игра уже завершена." });

        if (session.CurrentPhase != GamePhase.Selection)
            return BadRequest(new { error = $"❌ Выбор локации допускается только в фазе Selection. Текущая фаза: {session.CurrentPhase}." });

        _engine.PlayRound(session, playerLocation);

        // move to CreatureTurn phase
        session.CurrentPhase = GamePhase.CreatureTurn;

        await _store.UpdateSessionAsync(session);
        return Ok(new { message = session.StatusMessage, session = session });
    }

    [HttpPost("{id}/creature-turn")]
    public async Task<ActionResult<GameSession>> CreatureTurn(Guid id)
    {
        var session = await _store.GetSessionAsync(id);
        if (session == null)
            return NotFound(new { error = "❌ Игровая сессия не найдена." });

        if (session.IsGameOver)
            return BadRequest(new { error = "❌ Игра уже завершена." });

        if (session.CurrentPhase != GamePhase.CreatureTurn)
            return BadRequest(new { error = $"❌ Выбор Существа допускается только в фазе CreatureTurn. Текущая фаза: {session.CurrentPhase}." });

        _creatureLogic.SelectCreatureLocation(session);

        // move to Result phase
        session.CurrentPhase = GamePhase.Result;

        await _store.UpdateSessionAsync(session);
        return Ok(new { message = session.StatusMessage, session = session });
    }

    [HttpPost("{id}/next-round")]
    public async Task<ActionResult<GameSession>> NextRound(Guid id)
    {
        var session = await _store.GetSessionAsync(id);
        if (session == null)
            return NotFound(new { error = "❌ Игровая сессия не найдена." });

        if (session.IsGameOver)
            return BadRequest(new { error = "❌ Игра уже завершена." });

        if (session.CurrentPhase != GamePhase.Result)
            return BadRequest(new { error = $"❌ Переход к следующему раунду допускается только в фазе Result. Текущая фаза: {session.CurrentPhase}." });

        // First, run result-phase effects (special locations, card effects)
        _engine.ResolveRound(session);

        if (session.IsGameOver)
        {
            await _store.UpdateSessionAsync(session);
            
            // Get user ID from claims and save game history
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                await _engine.SaveGameHistoryAsync(session, userId);
            }
            
            return Ok(new { message = session.StatusMessage, session = session });
        }

        // If game continues, move to next Selection phase
        session.CurrentPhase = GamePhase.Selection;

        // If river vision is active, pre-generate the Creature's move for next round
        if (session.IsRiverVisionActive && !session.IsRiverVisionRevealed)
        {
            if (session.AvailableLocations.Count > 0)
            {
                // Build candidates for River Vision pre-choice: only AvailableLocations (no LastPlayerChoice)
                // LastPlayerChoice was already processed in current round and moved to UsedLocations
                var riverVisionCandidates = new List<int>(session.AvailableLocations);

                // If second phase active, predict and exclude the blocking location
                int? predictedBlockingLocation = null;
                if (session.PlayerProgress >= 4 && riverVisionCandidates.Count > 1)
                {
                    // Simulate blocking location selection (random from candidates)
                    // In real game creature AI will pick strategically, but for River Vision we use random
                    predictedBlockingLocation = riverVisionCandidates[Random.Shared.Next(riverVisionCandidates.Count)];
                    riverVisionCandidates.Remove(predictedBlockingLocation.Value);
                }

                // Now pick River Vision choice from remaining candidates
                if (riverVisionCandidates.Count > 0)
                {
                    var idx = Random.Shared.Next(riverVisionCandidates.Count);
                    var preChoice = riverVisionCandidates[idx];
                    session.LastCreatureChoice = preChoice;
                    session.IsRiverVisionRevealed = true;
                    session.StatusMessage = $"[NextRound] 👁️ Видение реки активно: Существо пойдёт на локацию {preChoice}. Выберите вашу локацию.";
                    await _store.UpdateSessionAsync(session);
                    return Ok(new { message = session.StatusMessage, session = session });
                }
            }
        }

        session.StatusMessage = "[NextRound] ▶️ Новый раунд начался. Выберите вашу локацию.";
        await _store.UpdateSessionAsync(session);
        return Ok(new { message = session.StatusMessage, session = session });
    }
}