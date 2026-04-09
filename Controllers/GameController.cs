using Microsoft.AspNetCore.Mvc;
using NotAlone.Models;
using NotAlone.Services;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly GameStore _store;

    public GameController(GameStore store)
    {
        _store = store;
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

        // 1. AI Choice (The Creature)
        var creatureChoice = Random.Shared.Next(1, 6);
        session.LastCreatureChoice = creatureChoice;

        // 2. Resolution Logic
        if (playerLocation == creatureChoice)
        {
            session.PlayerWillpower--;
            session.CreatureProgress++;
            session.StatusMessage = $"Caught! The Creature was at {creatureChoice}.";
        }
        else
        {
            session.PlayerProgress++;
            session.StatusMessage = $"Safe. You visited {playerLocation}, Creature was at {creatureChoice}.";
        }

        // 3. Victory Check
        if (session.PlayerWillpower <= 0 || session.CreatureProgress >= GameSession.MaxProgress)
        {
            session.IsGameOver = true;
            session.StatusMessage = "The Creature has assimilated you.";
        }
        else if (session.PlayerProgress >= GameSession.MaxProgress)
        {
            session.IsGameOver = true;
            session.StatusMessage = "Rescue has arrived! You escaped Artemia.";
        }

        return Ok(session);
    }
}