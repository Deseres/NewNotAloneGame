using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotAlone.Models;
using NotAlone.Services;

namespace NotAlone.Controllers;

[ApiController]
[Route("api/game")]
[Authorize]
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
    public async Task<IActionResult> PlayCard(Guid id, int cardId, [FromBody] PlayCardRequest? request)
    {
        var session = await _gameStore.GetSessionAsync(id);
        if (session == null)
            return NotFound(new { error = "❌ Игровая сессия не найдена." });

        if (!session.AvailableSurvivalCards.Contains(cardId))
            return BadRequest(new { error = $"❌ Карта {cardId} не в вашей руке." });

        var card = _survivalService.GetCardById(cardId);
        if (card == null)
            return NotFound(new { error = $"❌ Карта {cardId} не существует." });

        if (card.PlayablePhase != session.CurrentPhase)
            return BadRequest(new { error = $"❌ Карта '{card.Name}' может быть сыграна только в фазе {card.PlayablePhase}. Текущая фаза: {session.CurrentPhase}." });

        var targetLocations = request?.TargetLocationIds;
        var direction = request?.Direction;

        // Reject if body was provided but contains unexpected fields for this card type
        if (request != null)
        {
            bool hasTargetLocations = targetLocations != null && targetLocations.Count > 0;
            bool hasDirection = direction != null;
            
            // If body is provided but contains fields not needed for this card, reject it
            if (!hasTargetLocations && !hasDirection)
                return BadRequest(new { error = "❌ Это карта не требует параметров. Отправьте запрос без тела (body)." });

            if (hasTargetLocations && card.Type != SurvivalCardType.LocationsRegen)
                return BadRequest(new { error = "❌ Эта карта не поддерживает целевые локации. Отправьте запрос без тела." });

            if (hasDirection && card.Type != SurvivalCardType.MoveTarget)
                return BadRequest(new { error = "❌ Эта карта не поддерживает параметр направления. Отправьте запрос без тела." });
        }

        // Validate that target locations don't contain invalid location 0
        if (targetLocations != null && targetLocations.Any(id => id <= 0))
            return BadRequest(new { error = "❌ Номер локации должен быть больше 0." });

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
        session.AvailableSurvivalCards.Remove(cardId);
        session.UsedSurvivalCards.Add(cardId);

        await _gameStore.UpdateSessionAsync(session);

        return Ok(new 
        { 
            message = $"✓ Карта '{card.Name}' успешно сыграна!", 
            card = card, 
            effect = session.StatusMessage,
            session = session 
        });
    }
}
