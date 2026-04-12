using Microsoft.AspNetCore.Mvc;
using NotAlone.Models;
using NotAlone.Services;

namespace NotAlone.Controllers;

[ApiController]
[Route("api/game")]
public class SurvivalController : ControllerBase
{
    private readonly GameStore _gameStore;
    private readonly SurvivalService _survivalService;

    public SurvivalController(GameStore gameStore, SurvivalService survivalService)
    {
        _gameStore = gameStore;
        _survivalService = survivalService;
    }

    [HttpPost("{id}/cards/play/{cardId}")]
    public IActionResult PlayCard(Guid id, int cardId, [FromBody] PlayCardRequest? request)
    {
        if (!_gameStore.Sessions.TryGetValue(id, out var session))
            return NotFound(new { error = "❌ Игровая сессия не найдена." });

        if (!session.PlayerHand.Contains(cardId))
            return BadRequest(new { error = $"❌ Карта {cardId} не в вашей руке." });

        var card = _survivalService.GetCardById(cardId);
        if (card == null)
            return NotFound(new { error = $"❌ Карта {cardId} не существует." });

        if (card.PlayablePhase != session.CurrentPhase)
            return BadRequest(new { error = $"❌ Карта '{card.Name}' может быть сыграна только в фазе {card.PlayablePhase}. Текущая фаза: {session.CurrentPhase}." });

        var targetLocations = request?.TargetLocationIds;
        var direction = request?.Direction;

        // Reject requests with target locations for non-LocationsRegen cards
        if (targetLocations != null && targetLocations.Count > 0 && card.Type != SurvivalCardType.LocationsRegen)
            return BadRequest(new { error = "❌ Эта карта не поддерживает целевые локации." });

        // Reject requests with direction for non-MoveTarget cards
        if (direction != null && card.Type != SurvivalCardType.MoveTarget)
            return BadRequest(new { error = "❌ Эта карта не поддерживает параметр направления." });

        // Validate direction for MoveTarget card
        if (card.Type == SurvivalCardType.MoveTarget && direction == null)
            return BadRequest(new { error = "❌ Карта 'Move Target' требует параметр направления (Left или Right)." });

        // Validate LocationsRegen card requirements
        if (card.Type == SurvivalCardType.LocationsRegen)
        {
            var (isValid, message) = _survivalService.ValidateTargetLocations(card, session, targetLocations);
            if (!isValid)
                return BadRequest(new { error = message });
        }

        // Apply card effect
        _survivalService.ApplyEffect(session, card, targetLocations, direction);

        // Remove from hand and move to used
        session.PlayerHand.Remove(cardId);
        session.UsedSurvivalCards.Add(cardId);

        return Ok(new 
        { 
            message = $"✓ Карта '{card.Name}' успешно сыграна!", 
            card = card, 
            effect = session.StatusMessage,
            session = session 
        });
    }
}
