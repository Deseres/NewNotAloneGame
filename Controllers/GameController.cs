using Microsoft.AspNetCore.Mvc;
using NotAlone.Models;
using NotAlone.Services;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly GameStore _store;
    private readonly GameEngine _engine;

    public GameController(GameStore store)
    {
        _store = store;
        _engine = new GameEngine();
    }

    [HttpPost("start")]
    public ActionResult<GameSession> StartGame()
    {
        var session = new GameSession();
        _store.Sessions[session.Id] = session;
        return Ok(session);
    }

    [HttpPost("{id}/play")]
    public ActionResult<GameSession> PlayRound(Guid id, [FromBody] int playerLocation)
    {
        if (!_store.Sessions.TryGetValue(id, out var session))
            return NotFound("Game session not found.");

        if (session.IsGameOver)
            return BadRequest("The game is already over.");

        if (session.CurrentPhase != GamePhase.Selection)
            return BadRequest("PlayRound is only allowed in Selection phase.");

        _engine.PlayRound(session, playerLocation);

        // move to Result phase after resolving the play
        session.CurrentPhase = GamePhase.Result;

        // run result-phase effects (special locations, events)
        _engine.ResolveRound(session);

        return Ok(session);
    }

    [HttpPost("{id}/next-round")]
    public ActionResult<GameSession> NextRound(Guid id)
    {
        if (!_store.Sessions.TryGetValue(id, out var session))
            return NotFound("Game session not found.");

        if (session.IsGameOver)
            return BadRequest("The game is already over.");

        if (session.CurrentPhase != GamePhase.Result)
            return BadRequest("NextRound is only allowed in Result phase.");

        session.CurrentPhase = GamePhase.Selection;

        // If river vision is active, pre-generate the Creature's move so player can see it before choosing
        if (session.IsRiverVisionActive && !session.IsRiverVisionRevealed)
        {
            if (session.AvailableLocations.Count > 0)
            {
                var idx = Random.Shared.Next(session.AvailableLocations.Count);
                var preChoice = session.AvailableLocations[idx];
                session.LastCreatureChoice = preChoice;
                session.IsRiverVisionRevealed = true;
                session.StatusMessage = $"Видение реки активно: Существо пойдёт на {preChoice}. Выберите вашу локацию.";
                return Ok(session);
            }
        }

        session.StatusMessage = "New round. Make your selection.";
        return Ok(session);
    }
}