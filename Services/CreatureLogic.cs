using System.Linq;
using NotAlone.Models;

namespace NotAlone.Services;

public class CreatureLogic
{


	public void SelectCreatureLocation(GameSession session)
	{
		if (session == null) return;

		// Creature chooses from all possible locations unless river vision has pre-generated the choice
		int creatureChoice;
		string selectionLogic = "";
		
		if (session.IsRiverVisionActive && session.IsRiverVisionRevealed && session.LastCreatureChoice.HasValue)
		{
			creatureChoice = session.LastCreatureChoice.Value;
			selectionLogic = "(Видение реки - предсказано)";
		}
		else
		{
			if (session.IsFogActive == true)
			{
				var combined = session.AvailableLocations.Concat(session.UsedLocations).ToList();
				var creatureIdx = Random.Shared.Next(combined.Count);
				creatureChoice = combined[creatureIdx];
				session.LastCreatureChoice = creatureChoice;
				selectionLogic = $"(Туман - выбор из {combined.Count} мест)";
			}
			else
			{
				// Creature chooses from available locations + player's choice
				var options = session.AvailableLocations.ToList();
				if (session.LastPlayerChoice.HasValue && !options.Contains(session.LastPlayerChoice.Value))
					options.Add(session.LastPlayerChoice.Value);
				
				var creatureIdx = Random.Shared.Next(options.Count);
				creatureChoice = options[creatureIdx];
				session.LastCreatureChoice = creatureChoice;
				selectionLogic = $"(Выбор из {options.Count} доступных + выбор игрока)";
			}
		}
		
		// Select random modifier for this round
		var modifiers = new[] { CreatureModifier.DoubleDamage, CreatureModifier.BlockPlayerProgress, CreatureModifier.LoseRandomLocation };
		session.CurrentModifier = modifiers[Random.Shared.Next(modifiers.Length)];
		
        // Apply deferred Artefact effect
		if (session.IsArtefactActive)
		{
			session.CurrentModifier = CreatureModifier.None;
		}

        // Store creature's chosen location for deferred comparison in ResolveRound
		session.CreatureChosenLocation = creatureChoice;

		session.StatusMessage = $"[CreatureTurn] ✓ Существо выбрало локацию {creatureChoice} {selectionLogic}. Модификатор: {session.CurrentModifier}. Переход в Result фазу...";
	}
}
