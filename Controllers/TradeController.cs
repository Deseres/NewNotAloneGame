using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotAlone.Models;
using NotAlone.Services;
using System.Linq;

namespace NotAlone.Controllers;

[ApiController]
[Route("api/game")]
[Authorize]
public class TradeController : ControllerBase
{
    private readonly GameStore _store;
    private readonly TradeService _tradeService;

    public TradeController(GameStore store, TradeService tradeService)
    {
        _store = store;
        _tradeService = tradeService;
    }

    [HttpPost("{id}/resist")]
    public async Task<ActionResult<GameSession>> Resist(Guid id, [FromBody] ResistRequest request)
    {
        var session = await _store.GetSessionAsync(id);
        if (session == null)
            return NotFound("Game session not found.");

        if (session.IsGameOver)
            return BadRequest("The game is already over.");

        if (session.CurrentPhase != GamePhase.Selection)
            return BadRequest("This action is only allowed in Selection phase.");

        if (request == null)
            return BadRequest("Request body is required.");

        if (request.GivenWillpower < 1 || request.GivenWillpower > 3)
            return BadRequest("GivenWillpower must be between 1 and 3.");

        // 3 WP = GiveUp, chosen locations are ignored
        if (request.GivenWillpower == 3)
        {
            _tradeService.GiveUp(session);
            await _store.UpdateSessionAsync(session);
            return Ok(session);
        }

        var maxAllowed = request.GivenWillpower == 2 ? 4 : 2;
        if (request.ChosenLocations == null || request.ChosenLocations.Length < 1 || request.ChosenLocations.Length > maxAllowed)
            return BadRequest($"You must choose between 1 and {maxAllowed} used locations for the given willpower.");

        if (request.ChosenLocations.Distinct().Count() != request.ChosenLocations.Length)
            return BadRequest("Chosen locations must be distinct.");

        foreach (var loc in request.ChosenLocations)
        {
            if (!session.UsedLocations.Contains(loc))
                return BadRequest("Chosen locations must be from the session's used locations.");
        }

        _tradeService.Resist(session, request.GivenWillpower, request.ChosenLocations);
        await _store.UpdateSessionAsync(session);
        return Ok(session);
    }

    [HttpPost("{id}/giveup")]
    public async Task<ActionResult<GameSession>> GiveUp(Guid id)
    {
        var session = await _store.GetSessionAsync(id);
        if (session == null)
            return NotFound("Game session not found.");

        if (session.IsGameOver)
            return BadRequest("The game is already over.");

        if (session.CurrentPhase != GamePhase.Selection)
            return BadRequest("This action is only allowed in Selection phase.");

        _tradeService.GiveUp(session);
        await _store.UpdateSessionAsync(session);
        return Ok(session);
    }
}
