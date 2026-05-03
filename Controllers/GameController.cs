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
            return NotFound(new { error = "❌ Game session not found." });
        
        return Ok(new { session = session });
    }

    [HttpPost("start")]
    public async Task<ActionResult<GameSession>> StartGame()
    {
        var session = new GameSession();
        int randomCard = Random.Shared.Next(1, 6);
        session.AvailableSurvivalCards = new List<int> { randomCard };
        session.StatusMessage = "🎮 Game started! Survival is unlikely. Choose a location to begin.";

        _creatureLogic.ResetHistory();  

        await _store.CreateSessionAsync(session);
        _store.Sessions[session.Id] = session;
        return Ok(new { message = session.StatusMessage, session = session });
    }

    [HttpPost("{id}/play")]
    public async Task<ActionResult<GameSession>> PlayRound(Guid id, [FromBody] int playerLocation)
    {
        var session = await _store.GetSessionAsync(id);
        if (session == null)
            return NotFound(new { error = "❌ Game session not found." });

        if (session.IsGameOver)
            return BadRequest(new { error = "❌ The game is already over." });

        if (session.CurrentPhase != GamePhase.Selection)
            return BadRequest(new { error = $"❌ Location selection is only allowed during the Selection phase. Current phase: {session.CurrentPhase}." });

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
            return NotFound(new { error = "❌ Game session not found." });

        if (session.IsGameOver)
            return BadRequest(new { error = "❌ The game is already over." });

        if (session.CurrentPhase != GamePhase.CreatureTurn)
            return BadRequest(new { error = $"❌ Creature turn is only allowed during the CreatureTurn phase. Current phase: {session.CurrentPhase}." });

        // If River Vision already pre-committed the creature's choice, skip re-selection
        if (!session.IsRiverVisionRevealed)
        {
            _creatureLogic.SelectCreatureLocation(session);
        }
        else
        {
            var rvBlockInfo = session.CreatureBlockingLocation.HasValue
                ? $" Blocking location: {session.CreatureBlockingLocation}."
                : string.Empty;
            session.StatusMessage = $"[CreatureTurn] 👁️ The Creature already made its move (revealed by River Vision). Attacking location: {session.CreatureChosenLocation}.{rvBlockInfo} Modifier: {ReadableModifier(session.CurrentModifier)}.";
        }

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
            return NotFound(new { error = "❌ Game session not found." });

        if (session.IsGameOver)
            return BadRequest(new { error = "❌ The game is already over." });

        if (session.CurrentPhase != GamePhase.Result)
            return BadRequest(new { error = $"❌ Advancing to the next round is only allowed during the Result phase. Current phase: {session.CurrentPhase}." });

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
        session.RoundNumber++;
        session.CurrentPhase = GamePhase.Selection;

        // Save current choices to previous for next round's creature learning
        session.PreviousPlayerChoice = session.CurrentPlayerChoice;
        session.PreviousCreatureChoice = session.CreatureChosenLocation;

        // If river vision is active, pre-commit the Creature's move BEFORE the player picks
        if (session.IsRiverVisionActive && !session.IsRiverVisionRevealed)
        {
            // Creature picks blind — it doesn't know where the player will go next round
            session.CurrentPlayerChoice = null;

            // Use the real creature AI to commit — this is the actual move the creature will make
            _creatureLogic.SelectCreatureLocation(session);
            session.IsRiverVisionRevealed = true;

            session.StatusMessage = $"[NextRound] 👁️ River Vision is active: the Creature will attack location {session.CreatureChosenLocation}. Now choose your location.";
            await _store.UpdateSessionAsync(session);
            return Ok(new { message = session.StatusMessage, session = session });
        }

        session.StatusMessage = "[NextRound] ▶️ New round started. Choose your location.";
        await _store.UpdateSessionAsync(session);
        return Ok(new { message = session.StatusMessage, session = session });
    }

    private static string ReadableModifier(CreatureModifier modifier) => modifier switch
    {
        CreatureModifier.None                  => "None",
        CreatureModifier.DoubleDamage          => "Double Damage",
        CreatureModifier.BlockPlayerProgress   => "Block Progress",
        CreatureModifier.LoseRandomLocation    => "Lose Location",
        CreatureModifier.BeachAndWreckBlock    => "Beach & Wreck Block",
        CreatureModifier.ExtraCreatureProgress => "Extra Creature Progress",
        _                                      => modifier.ToString()
    };
}