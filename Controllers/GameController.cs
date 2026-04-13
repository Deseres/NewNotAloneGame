using Microsoft.AspNetCore.Mvc;
using NotAlone.Models;
using NotAlone.Services;

namespace NotAlone.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("start")]
    public ActionResult<GameSession> StartGame()
    {
        var session = new GameSession();
        // Give player one random survival card (IDs 1-5)
        int randomCard = Random.Shared.Next(1, 6);
        session.AvailableSurvivalCards = new List<int> { randomCard };
        session.StatusMessage = "🎮 Игра началась! Выживание маловероятно. Выберите локацию для начала.";
        _store.Sessions[session.Id] = session;
        return Ok(new { message = session.StatusMessage, session = session });
    }

    [HttpPost("{id}/play")]
    public ActionResult<GameSession> PlayRound(Guid id, [FromBody] int playerLocation)
    {
        if (!_store.Sessions.TryGetValue(id, out var session))
            return NotFound(new { error = "❌ Игровая сессия не найдена." });

        if (session.IsGameOver)
            return BadRequest(new { error = "❌ Игра уже завершена." });

        if (session.CurrentPhase != GamePhase.Selection)
            return BadRequest(new { error = $"❌ Выбор локации допускается только в фазе Selection. Текущая фаза: {session.CurrentPhase}." });

        _engine.PlayRound(session, playerLocation);

        // move to CreatureTurn phase
        session.CurrentPhase = GamePhase.CreatureTurn;

        return Ok(new { message = session.StatusMessage, session = session });
    }

    [HttpPost("{id}/creature-turn")]
    public ActionResult<GameSession> CreatureTurn(Guid id)
    {
        if (!_store.Sessions.TryGetValue(id, out var session))
            return NotFound(new { error = "❌ Игровая сессия не найдена." });

        if (session.IsGameOver)
            return BadRequest(new { error = "❌ Игра уже завершена." });

        if (session.CurrentPhase != GamePhase.CreatureTurn)
            return BadRequest(new { error = $"❌ Выбор Существа допускается только в фазе CreatureTurn. Текущая фаза: {session.CurrentPhase}." });

        _creatureLogic.SelectCreatureLocation(session);

        // move to Result phase
        session.CurrentPhase = GamePhase.Result;

        return Ok(new { message = session.StatusMessage, session = session });
    }

    [HttpPost("{id}/next-round")]
    public ActionResult<GameSession> NextRound(Guid id)
    {
        if (!_store.Sessions.TryGetValue(id, out var session))
            return NotFound(new { error = "❌ Игровая сессия не найдена." });

        if (session.IsGameOver)
            return BadRequest(new { error = "❌ Игра уже завершена." });

        if (session.CurrentPhase != GamePhase.Result)
            return BadRequest(new { error = $"❌ Переход к следующему раунду допускается только в фазе Result. Текущая фаза: {session.CurrentPhase}." });

        // First, run result-phase effects (special locations, card effects)
        _engine.ResolveRound(session);

        if (session.IsGameOver)
            return Ok(new { message = session.StatusMessage, session = session });

        // If game continues, move to next Selection phase
        session.CurrentPhase = GamePhase.Selection;

        // If river vision is active, pre-generate the Creature's move for next round
        if (session.IsRiverVisionActive && !session.IsRiverVisionRevealed)
        {
            if (session.AvailableLocations.Count > 0)
            {
                var idx = Random.Shared.Next(session.AvailableLocations.Count);
                var preChoice = session.AvailableLocations[idx];
                session.LastCreatureChoice = preChoice;
                session.IsRiverVisionRevealed = true;
                session.StatusMessage = $"[NextRound] 👁️ Видение реки активно: Существо пойдёт на локацию {preChoice}. Выберите вашу локацию.";
                return Ok(new { message = session.StatusMessage, session = session });
            }
        }

        session.StatusMessage = "[NextRound] ▶️ Новый раунд начался. Выберите вашу локацию.";
        return Ok(new { message = session.StatusMessage, session = session });
    }
}