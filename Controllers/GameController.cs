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

        _engine.PlayRound(session, playerLocation);
        return Ok(session);
    }
}