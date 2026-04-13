using System.Linq;
using NotAlone.Models;

namespace NotAlone.Services;

public class GameEngine
{
	private readonly TradeService _tradeService;

	public GameEngine(TradeService? tradeService = null)
	{
		_tradeService = tradeService ?? new TradeService();
	}

	// Helper: apply Jungle effect (find random used location excluding currentCardId,
	// restore it to available, and indicate that the player's card should be preserved)
	private (bool shouldPreserve, string message) ApplyJungleEffect(GameSession session, int currentCardId)
	{
		if (session == null)
			return (false, string.Empty);

		// find candidates in UsedLocations excluding the current card id
		var candidates = session.UsedLocations.Where(x => x != currentCardId).ToList();
		if (candidates.Count > 0)
		{
			var idx = Random.Shared.Next(candidates.Count);
			var toRestore = candidates[idx];
			// remove from used and add to available
			session.UsedLocations.Remove(toRestore);
			if (!session.AvailableLocations.Contains(toRestore))
				session.AvailableLocations.Add(toRestore);
			return (true, $"Джунгли восстановили {toRestore}.");
		}

		// preserve flag still true (player's card preserved), but no card restored
		return (true, "Джунгли не нашли использованных карт для восстановления.");
	}

	private (bool shouldPreserve, string message) ApplySwampEffect(GameSession session, int currentCardId)
	{
		if (session == null)
			return (false, string.Empty);

		var candidates = session.UsedLocations.Where(x => x != currentCardId).ToList();
		int restored = 0;
		
		for (int i = 0; i < 2 && candidates.Count > 0; i++)
		{
			var idx = Random.Shared.Next(candidates.Count);
			var toRestore = candidates[idx];
			candidates.RemoveAt(idx);
			session.UsedLocations.Remove(toRestore);
			if (!session.AvailableLocations.Contains(toRestore))
				session.AvailableLocations.Add(toRestore);
			restored++;
		}

		var msg = restored > 0 
			? $"Болото восстановило {restored} карт."
			: "Болото не нашли использованных карт для восстановления.";
		
		return (true, msg);
	}

	// Helper: apply Shelter effect (generate random survival card)
	private (bool shouldPreserve, string message) ApplyShelterEffect(GameSession session)
	{
		if (session == null)
			return (false, string.Empty);

		// Generate random card ID 1-5 (matching valid card IDs)
		int cardId = Random.Shared.Next(1, 6);
		var randomCardType = (SurvivalCardType)(cardId - 1);
		
		session.AvailableSurvivalCards.Add(cardId);
		
		return (true, $"Убежище предоставило вам карту выживания: {randomCardType}.");
	}

	// Helper: apply River effect (enable river vision for next round)
	private string ApplyRiverEffect(GameSession session)
	{
		if (session == null) return string.Empty;
		session.IsRiverVisionActive = true;
		session.IsRiverVisionRevealed = false;
		return "Видение реки активно: в следующем раунде ход Существо будет виден.";
	}

	// Helper: apply Rover effect (unlock one blocked location)
	private string ApplyRoverEffect(GameSession session)
	{
		if (session == null) return string.Empty;
		var blocked = session.Locations.Where(l => !session.AvailableLocations.Contains(l) && !session.UsedLocations.Contains(l)).ToList();
		if (blocked.Count > 0)
		{
			var idx3 = Random.Shared.Next(blocked.Count);
			var unlocked = blocked[idx3];
			if (!session.AvailableLocations.Contains(unlocked))
				session.AvailableLocations.Add(unlocked);
			return $"Вездеход исследовал территорию и открыл локацию {unlocked}.";
		}
		return "Вездеход вернул ценные данные, но все локации уже разблокированы.";
	}

	// Helper: apply Beach effect for a player (light beacon or grant progress)
	private string ApplyBeachForPlayer(GameSession session)
	{
		if (session == null) return string.Empty;

		if (!session.IsBeaconLit)
		{
			session.IsBeaconLit = true;
			return "Вы зажгли маяк на 4.";
		}
		else
		{
			session.PlayerProgress++;
			return "Маяк на 4 помог вашему спасению (+1 прогресс).";
		}
	}

	// Helper: apply Wreck effect (grants player progress)
	private string ApplyWreck(GameSession session)
	{
		if (session == null) return string.Empty;
		session.PlayerProgress++;
		return "Обломки помогли вашему спасению (+1 прогресс).";
	}

	// Helper: apply Source effect (restores willpower)
	private string ApplySource(GameSession session)
	{
		if (session == null) return string.Empty;
		if (session.PlayerWillpower < GameSession.MaxWillpower)
		{
			session.PlayerWillpower++;
			return "Источник восстановил вашу волю (+1 воля).";
		}
		return "Источник не нашёл нужной вам помощи (воля уже максимальна).";
	}

	// Helper: apply Artefact effect (disables creature power)
	private string ApplyArtefactEffect(GameSession session)
	{
		if (session == null) return string.Empty;
		session.IsArtefactActive = true;
		return "Артефакт активирован: сила Существа будет нейтрализована в следующем раунде.";
	}

	// Helper: apply creature modifier effects
	internal void ApplyCreatureModifier(GameSession session, int playerChoice, int creatureChoice)
	{
		if (session == null || session.CurrentModifier == CreatureModifier.None)
			return;

		switch (session.CurrentModifier)
		{
			case CreatureModifier.DoubleDamage:
				if (playerChoice == creatureChoice)
				{
					session.PlayerWillpower = Math.Max(0, session.PlayerWillpower - 1);
					session.StatusMessage += $"\n⚠️ [Modifier: Double Damage] Существо наносит дополнительный урон!";
				}
				break;

			case CreatureModifier.BlockPlayerProgress:
				if (playerChoice != creatureChoice)
				{
					session.PlayerProgress--;
					session.StatusMessage += $"\n⚠️ [Modifier: Block Progress] Ваш прогресс спасения заблокирован!";
				}
				break;

			case CreatureModifier.LoseRandomLocation:
				if (session.AvailableLocations.Count > 0)
				{
					var idx = Random.Shared.Next(session.AvailableLocations.Count);
					var lost = session.AvailableLocations[idx];
					session.AvailableLocations.RemoveAt(idx);
					session.UsedLocations.Add(lost);
					session.StatusMessage += $"\n⚠️ [Modifier: Lose Location] Вы потеряли локацию {lost}!";
				}
				break;
			case CreatureModifier.BeachAndWreckBlock:
				if (playerChoice != creatureChoice) // Only if player escaped
				{
					if (playerChoice == 4) // Beach
					{
						if (session.IsBeaconLit)
						{
							// Beacon was lit, so we cancel the progress gain
							session.PlayerProgress = Math.Max(0, session.PlayerProgress - 1);
							session.StatusMessage += $"\n⚠️ [Modifier: Beach Block] Маяк не помог. Прогресс заблокирован!";
						}
						else
						{
							// Beacon was not lit, so we cancel the lighting effect
							session.IsBeaconLit = false;
							session.StatusMessage += $"\n⚠️ [Modifier: Beach Block] Маяк не удалось зажечь!";
						}
					}
					else if (playerChoice == 8) // Wreck
					{
						// Wreck gives +1 progress, so we cancel it
						session.PlayerProgress = Math.Max(0, session.PlayerProgress - 1);
						session.StatusMessage += $"\n⚠️ [Modifier: Wreck Block] Обломки не помогли. Прогресс заблокирован!";
					}
				}
				break;
			case CreatureModifier.ExtraCreatureProgress:
					if (playerChoice == creatureChoice)
					{
						session.CreatureProgress = Math.Min(GameSession.MaxCreatureProgress, session.CreatureProgress + 1);
						session.StatusMessage += $"\n⚠️ [Modifier: Extra Creature Progress] Существо продвигается быстрее!";
					}
					break;
		}
		// Reset modifier for next round
		session.CurrentModifier = CreatureModifier.None;
	}

	public void PlayRound(GameSession session, int playerLocation)
	{
		// Remember player's selection for later (Result phase)
		session.LastPlayerChoice = playerLocation;

		// Only allow player to pick from available locations
		if (!session.AvailableLocations.Contains(playerLocation))
		{
			session.StatusMessage = $"[Selection] ❌ Локация {playerLocation} недоступна. Доступные: {string.Join(", ", session.AvailableLocations)}";
			return;
		}

		// Move played location from available to used for player only
		session.AvailableLocations.Remove(playerLocation);
		session.UsedLocations.Add(playerLocation);

		session.StatusMessage = $"[Selection] ✓ Вы выбрали локацию {playerLocation}. Ожидание выбора Существа...";
	}



	public void ResolveRound(GameSession session)
	{
		// Only run result-phase effects when we're in Result phase
		if (session == null)
			return;

		if (session.CurrentPhase != GamePhase.Result)
			return;

		// Prepare choices
		var playerChoice = session.LastPlayerChoice;
		var creatureChoice = session.CreatureChosenLocation; // Use deferred creature choice

		// Perform the actual comparison (creature vs player)
		if (playerChoice.HasValue && creatureChoice.HasValue)
		{
			if (playerChoice.Value == creatureChoice.Value)
			{
				session.PlayerWillpower--;
				session.CreatureProgress++;
				session.StatusMessage = $"[Result] ⚠️ ПОЙМАЛИ! И Вы, и Существо выбрали локацию {creatureChoice}. " +
					$"Потеряна 1 воля (осталось {session.PlayerWillpower}). " +
					$"Существо: {session.CreatureProgress}/{GameSession.MaxCreatureProgress} к ассимиляции.";

				// If willpower drops to zero or below, immediately give up
				if (session.PlayerWillpower <= 0)
				{
					_tradeService.GiveUp(session);
					return;
				}
			}
			else
			{
				session.PlayerProgress++;
				session.StatusMessage = $"[Result] ✓ СПАСЛИСЬ! Вы в локации {playerChoice}, Существо в {creatureChoice}. " +
					$"Прогресс спасения: {session.PlayerProgress}/{GameSession.MaxPlayerProgress}.";
			}

			// Victory Check after comparison
			if (session.CreatureProgress >= GameSession.MaxCreatureProgress)
			{
				session.IsGameOver = true;
				session.StatusMessage = "💀 КОНЕЦ ИГРЫ: Существо вас ассимилировало. Вы поражены.";
				return;
			}
			else if (session.PlayerProgress >= GameSession.MaxPlayerProgress)
			{
				session.IsGameOver = true;
				session.StatusMessage = "🚀 КОНЕЦ ИГРЫ: Спасение прибыло! Вы сбежали из Артемии!";
				return;
			}

			// Apply creature modifier
			ApplyCreatureModifier(session, playerChoice.Value, creatureChoice.Value);
		}

		var wasCaught = playerChoice.HasValue && creatureChoice.HasValue && playerChoice.Value == creatureChoice.Value;

		// Flag to indicate whether the player's played card should return to hand (available)
		var shouldReturnToHand = false;

		// Creature picks Beach (4) -> extinguish beacon regardless of player's presence
		if (creatureChoice.HasValue && creatureChoice.Value == 4)
		{
			session.IsBeaconLit = false;
			session.StatusMessage += " Существо потушило маяк на 4.";
		}

		// Handle caught cases first
		if (wasCaught)
		{
			if (playerChoice.HasValue && playerChoice.Value == 1)
			{
				// Lair caught: extra willpower loss
				session.PlayerWillpower--;
				session.StatusMessage = "Вы зашли в самое Логово Существа! Это была ошибка. Потеряно 2 воли";
				if (session.PlayerWillpower <= 0)
				{
					_tradeService.GiveUp(session);
					return;
				}
			}
			// if caught elsewhere, no extra result-phase effects
		}
		else
		{
			// Not caught: handle special locations for player's choice
			if (playerChoice.HasValue)
			{
				if (playerChoice.Value == 2)
				{
					var (preserve, msg) = ApplyJungleEffect(session, playerChoice.Value);
					if (preserve) shouldReturnToHand = true;
					session.StatusMessage += " " + msg;
				}
				else if (playerChoice.Value == 4)
				{
					var msg = ApplyBeachForPlayer(session);
					session.StatusMessage += " " + msg;
				}
				else if (playerChoice.Value == 3)
				{
					var msg = ApplyRiverEffect(session);
					session.StatusMessage += " " + msg;
				}
				else if (playerChoice.Value == 5)
				{
					var rmsg = ApplyRoverEffect(session);
					session.StatusMessage += " " + rmsg;
				}
				else if (playerChoice.Value == 6)
				{
					var (preserve, msg) = ApplySwampEffect(session, playerChoice.Value);
					if (preserve) shouldReturnToHand = true;
					session.StatusMessage += " " + msg;
				}
				else if (playerChoice.Value == 7)
				{
					var (preserve, msg) = ApplyShelterEffect(session);
					session.StatusMessage += " " + msg;
				}
				else if (playerChoice.Value == 8)
				{
					var msg = ApplyWreck(session);
					session.StatusMessage += " " + msg;
				}
				else if (playerChoice.Value == 9)
				{
					var msg = ApplySource(session);
					session.StatusMessage += " " + msg;
				}
				else if (playerChoice.Value == 10)
				{
					var msg = ApplyArtefactEffect(session);
					session.StatusMessage += " " + msg;
				}
				else if (playerChoice.Value == 1)
				{
					// Lair copying: copy creature's location effect
					if (creatureChoice.HasValue)
					{
						var cc = creatureChoice.Value;
						string effectMsg = string.Empty;
						if (cc == 2)
						{
							var (preserve, jmsg) = ApplyJungleEffect(session, 1); // currentCardId = 1 so Lair doesn't restore itself
							if (preserve) shouldReturnToHand = true;
							effectMsg = jmsg;
						}
						else if (cc == 3)
						{
							effectMsg = ApplyRiverEffect(session);
						}
						else if (cc == 4)
							effectMsg = ApplyBeachForPlayer(session);
						else if (cc == 5)
						{
							effectMsg = ApplyRoverEffect(session);
						}
						else if (cc == 6)
						{
							var (preserve, smsg) = ApplySwampEffect(session, 1);
							if (preserve) shouldReturnToHand = true;
							effectMsg = smsg;
						}
						else if (cc == 7)
						{
							var (preserve, sheltermsg) = ApplyShelterEffect(session);
							effectMsg = sheltermsg;
						}
						else if (cc == 8)
						{
							effectMsg = ApplyWreck(session);
						}
						else if (cc == 9)
						{
							effectMsg = ApplySource(session);
						}
						else if (cc == 10)
						{
							effectMsg = ApplyArtefactEffect(session);
						}
						else
							effectMsg = $"Эффект локации {cc} не реализован для копирования.";
						
						session.StatusMessage = $"Пока Существо охотилось в другом месте, вы обыскали его Логово и использовали возможности локации {cc}. {effectMsg}";
					}
				}
			}
		}

		// If river vision was revealed for this round, it has now been used — clear flags
		// At the end: if the special effect requires the player's played card to return to hand, restore it
		if (shouldReturnToHand && playerChoice.HasValue)
		{
			var pl = playerChoice.Value;
			if (session.UsedLocations.Contains(pl))
			{
				session.UsedLocations.Remove(pl);
				if (!session.AvailableLocations.Contains(pl))
					session.AvailableLocations.Add(pl);
				session.StatusMessage += $" Карта {pl} возвращена в руку благодаря эффекту.";
			}
		}

		if (session.IsRiverVisionRevealed)
		{
			session.IsRiverVisionActive = false;
			session.IsRiverVisionRevealed = false;
			session.StatusMessage += " Видение реки использовано и больше не активно.";
		}
		if (session.IsFogActive)
		{
			session.IsFogActive = false;
			session.StatusMessage += " Туман рассеялся и больше не активен.";
		}
		if (session.IsArtefactActive)
		{
			session.IsArtefactActive = false;
			session.StatusMessage += " Артефакт использован и деактивирован.";
		}
	}
}
