using System.Linq;
using NotAlone.Models;

namespace NotAlone.Services;

public class GameEngine
{
	private readonly TradeService _tradeService;
	private readonly CreatureLogic? _creatureLogic;
	private readonly AppDbContext? _dbContext;

	public GameEngine(TradeService? tradeService = null, CreatureLogic? creatureLogic = null, AppDbContext? dbContext = null)
	{
		_tradeService = tradeService ?? new TradeService();
		_creatureLogic = creatureLogic;
		_dbContext = dbContext;
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
			return (true, $"Jungle restored location {toRestore}.");
		}

		// preserve flag still true (player's card preserved), but no card restored
		return (true, "Jungle found no used locations to restore.");
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
			? $"Swamp restored {restored} location(s)."
			: "Swamp found no used locations to restore.";
		
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
		
		return (true, $"Shelter provided you a survival card: {randomCardType}.");
	}

	// Helper: apply River effect (enable river vision for next round)
	private string ApplyRiverEffect(GameSession session)
	{
		if (session == null) return string.Empty;
		session.IsRiverVisionActive = true;
		session.IsRiverVisionRevealed = false;
		return "River Vision active: in the next round the Creature's move will be revealed.";
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
			return $"Rover explored the territory and unlocked location {unlocked}.";
		}
		return "Rover returned valuable data, but all locations are already unlocked.";
	}

	// Helper: apply Beach effect for a player (light beacon or grant progress)
	private string ApplyBeachForPlayer(GameSession session, bool blockProgress = false)
	{
		if (session == null) return string.Empty;

		if (!session.IsBeaconLit)
		{
			session.IsBeaconLit = true;
			return "You lit the beacon at location 4.";
		}
		else
		{
			// Only grant progress if not blocked by modifier
			if (!blockProgress)
			{
				session.PlayerProgress++;
				return "The beacon at location 4 aided your rescue (+1 progress).";
			}
			else
			{
				return "The beacon lights your way, but the Creature is blocking all your progress.";
			}
		}
	}

	// Helper: apply Wreck effect (grants player progress)
	private string ApplyWreck(GameSession session)
	{
		if (session == null) return string.Empty;
		session.PlayerProgress++;
		return "The Wreck aided your rescue (+1 progress).";
	}

	// Helper: apply Source effect (restores willpower)
	private string ApplySource(GameSession session)
	{
		if (session == null) return string.Empty;
		if (session.PlayerWillpower < GameSession.MaxWillpower)
		{
			session.PlayerWillpower++;
			return "The Source restored your willpower (+1 willpower).";
		}
		return "The Source found nothing to offer — willpower is already at maximum.";
	}

	// Helper: apply Artefact effect (disables creature power)
	private string ApplyArtefactEffect(GameSession session)
	{
		if (session == null) return string.Empty;
		session.IsArtefactActive = true;
		return "Artefact activated: the Creature's power will be neutralised next round.";
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
					session.StatusMessage += $"\n⚠️ [Modifier: Double Damage] The Creature deals extra damage!";
				}
				break;

			case CreatureModifier.BlockPlayerProgress:
				// Blocks ALL progress on escape (handled in ResolveRound)
				session.StatusMessage += $"\n⚠️ [Modifier: Block Progress] All your rescue progress was blocked!";
				break;

			case CreatureModifier.LoseRandomLocation:
				if (session.AvailableLocations.Count > 0)
				{
					var idx = Random.Shared.Next(session.AvailableLocations.Count);
					var lost = session.AvailableLocations[idx];
					session.AvailableLocations.RemoveAt(idx);
					session.UsedLocations.Add(lost);
					session.StatusMessage += $"\n⚠️ [Modifier: Lose Location] You lost location {lost}!";
				}
				break;
			case CreatureModifier.BeachAndWreckBlock:
				if (playerChoice != creatureChoice) // Only if player escaped
				{
					if (playerChoice == 4) // Beach
					{
						session.StatusMessage += $"\n⚠️ [Modifier: Beach Block] The Beach bonus effect was blocked!";
					}
					else if (playerChoice == 8) // Wreck
					{
						session.StatusMessage += $"\n⚠️ [Modifier: Wreck Block] The Wreck bonus progress was blocked!";
					}
				}
				break;
			case CreatureModifier.ExtraCreatureProgress:
					if (playerChoice == creatureChoice)
					{
						session.CreatureProgress = Math.Min(GameSession.MaxCreatureProgress, session.CreatureProgress + 1);
						session.StatusMessage += $"\n⚠️ [Modifier: Extra Creature Progress] The Creature advances faster!";
					}
					break;
		}
		// Don't reset modifier yet - it's needed for location effects!
	}

	public void PlayRound(GameSession session, int playerLocation)
	{
		// Only allow player to pick from available locations
		if (!session.AvailableLocations.Contains(playerLocation))
		{
			session.StatusMessage = $"[Selection] ❌ Location {playerLocation} is unavailable. Available: {string.Join(", ", session.AvailableLocations)}";
			return;
		}

		// Remember player's selection for THIS round (CurrentPlayerChoice)
		// PreviousPlayerChoice will be set after round resolves
		session.CurrentPlayerChoice = playerLocation;

		// Move played location from available to used for player only
		session.AvailableLocations.Remove(playerLocation);
		session.UsedLocations.Add(playerLocation);

		session.StatusMessage = $"[Selection] ✓ You chose location {playerLocation}. Waiting for the Creature...";
	}



	public void ResolveRound(GameSession session)
	{
		// Only run result-phase effects when we're in Result phase
		if (session == null)
			return;

		if (session.CurrentPhase != GamePhase.Result)
			return;

		// Prepare choices - use CurrentPlayerChoice (set this round in Selection phase)
		var playerChoice = session.CurrentPlayerChoice;
		var creatureChoice = session.CreatureChosenLocation; // Use deferred creature choice

		// Apply creature modifier FIRST (before progress changes)
		if (playerChoice.HasValue && creatureChoice.HasValue)
		{
			ApplyCreatureModifier(session, playerChoice.Value, creatureChoice.Value);
		}

		var wasCaught = playerChoice.HasValue && creatureChoice.HasValue && playerChoice.Value == creatureChoice.Value;

		// Check if location is blocked by creature's second phase blocking location
		// Blocking is only active when modifier is not disabled
		var isLocationBlocked = playerChoice.HasValue && session.CreatureBlockingLocation.HasValue && 
		                        playerChoice.Value == session.CreatureBlockingLocation.Value &&
		                        session.CurrentModifier != CreatureModifier.None; // Artefact disables blocking too

		// Perform the actual comparison (creature vs player)
		if (playerChoice.HasValue && creatureChoice.HasValue)
		{
			if (playerChoice.Value == creatureChoice.Value)
			{
				// Record the catch for creature learning
				_creatureLogic?.RecordCatch(creatureChoice.Value);

				session.PlayerWillpower--;
				session.CreatureProgress++;
				session.StatusMessage = $"[Result] ⚠️ CAUGHT! Both you and the Creature chose location {creatureChoice}. " +
					$"Lost 1 willpower (remaining: {session.PlayerWillpower}). " +
					$"Creature: {session.CreatureProgress}/{GameSession.MaxCreatureProgress} to assimilation.";

				// If willpower drops to zero or below, immediately give up
				if (session.PlayerWillpower <= 0)
				{
					_tradeService.GiveUp(session);
					return;
				}

				// CHECK CREATURE WIN IMMEDIATELY AFTER CATCH
				if (session.CreatureProgress >= GameSession.MaxCreatureProgress)
				{
					session.IsGameOver = true;
					session.StatusMessage = "💀 GAME OVER: The Creature has assimilated you. Defeat.";
					return;
				}
			}
			else
			{
				// ESCAPED: Only grant progress on successful escape
				// Progress blocked by BlockPlayerProgress modifier OR if location is blocked by creature
				bool progressBlocked = (session.CurrentModifier == CreatureModifier.BlockPlayerProgress) 
									|| isLocationBlocked;
				
				if (!progressBlocked)
				{
					session.PlayerProgress++;
				}

				// Record escape so creature AI can decay abuse detection and update attempt history
				if (creatureChoice.HasValue)
					_creatureLogic?.RecordEscape(creatureChoice.Value);
				
				session.StatusMessage = $"[Result] ✓ ESCAPED! You are at location {playerChoice}, the Creature at {creatureChoice}. " +
					$"Rescue progress: {session.PlayerProgress}/{GameSession.MaxPlayerProgress}.";
				
				if (isLocationBlocked)
					session.StatusMessage += " But the Creature blocked your location, stopping all progress!";
				else if (session.CurrentModifier == CreatureModifier.BlockPlayerProgress)
					session.StatusMessage += " But all your progress was blocked by the modifier!";
				else if (session.CurrentModifier == CreatureModifier.BeachAndWreckBlock && (playerChoice == 4 || playerChoice == 8))
					session.StatusMessage += " But the location's bonus effect was blocked!";
			}

		}

		// Flag to indicate whether the player's played card should return to hand (available)
		var shouldReturnToHand = false;

		// Creature picks Beach (4) -> extinguish beacon regardless of player's presence
		if (creatureChoice.HasValue && creatureChoice.Value == 4)
		{
			session.IsBeaconLit = false;
			session.StatusMessage += " The Creature extinguished the beacon at location 4.";
		}

		// Handle caught cases first
		if (wasCaught)
		{
			if (playerChoice.HasValue && playerChoice.Value == 1)
			{
				// Lair caught: extra willpower loss
				session.PlayerWillpower--;
				session.StatusMessage += " You walked straight into the Creature's Lair! An extra willpower lost.";
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
			if (playerChoice.HasValue && !isLocationBlocked)
			{
				// Check if location effects should be blocked by BeachAndWreckBlock modifier
				bool blockLocationBonus = (session.CurrentModifier == CreatureModifier.BeachAndWreckBlock && (playerChoice == 4 || playerChoice == 8));
				
				// Also block all location effects if BlockPlayerProgress is active
				bool blockAllProgress = (session.CurrentModifier == CreatureModifier.BlockPlayerProgress);

				if (playerChoice.Value == 2)
				{
					var (preserve, msg) = ApplyJungleEffect(session, playerChoice.Value);
					if (preserve) shouldReturnToHand = true;
					session.StatusMessage += " " + msg;
				}
				else if (playerChoice.Value == 4)
				{
					if (!blockLocationBonus && !blockAllProgress)
					{
						var msg = ApplyBeachForPlayer(session, false);
						session.StatusMessage += " " + msg;
					}
					else if (!blockLocationBonus) // BeachAndWreckBlock doesn't apply, but BlockPlayerProgress does
					{
						var msg = ApplyBeachForPlayer(session, blockAllProgress);
						session.StatusMessage += " " + msg;
					}
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
					if (!blockLocationBonus && !blockAllProgress)
					{
						var msg = ApplyWreck(session);
						session.StatusMessage += " " + msg;
					}
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
						{
							if (!blockAllProgress && !blockLocationBonus)
								effectMsg = ApplyBeachForPlayer(session);							else if (!blockLocationBonus)
								effectMsg = ApplyBeachForPlayer(session, blockAllProgress);							else
								effectMsg = "Beach effect blocked by modifier";
						}
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
							if (!blockAllProgress && !blockLocationBonus)
								effectMsg = ApplyWreck(session);
							else
								effectMsg = "Wreck effect blocked by modifier";
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
							effectMsg = $"The effect of location {cc} is not implemented for copying.";
						
						session.StatusMessage = $"While the Creature hunted elsewhere, you searched its Lair and used the powers of location {cc}. {effectMsg}";
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
				session.StatusMessage += $" Location {pl} returned to your hand via its effect.";
			}
		}

		// Reset modifier AFTER all effects are applied
		session.CurrentModifier = CreatureModifier.None;

		if (session.IsRiverVisionRevealed)
		{
			session.IsRiverVisionActive = false;
			session.IsRiverVisionRevealed = false;
			session.StatusMessage += " River Vision has been used and is no longer active.";
		}
		if (session.IsFogActive)
		{
			session.IsFogActive = false;
			session.StatusMessage += " The fog has lifted and is no longer active.";
		}
		
		// Deactivate artefact ONLY if it was NOT just activated this round (i.e., it's from a previous round)
		// If player just used location 10, keep IsArtefactActive = true for next round's creature to see it
		bool justUsedArtefact = playerChoice.HasValue && playerChoice.Value == 10;
		if (session.IsArtefactActive && !justUsedArtefact)
		{
			session.IsArtefactActive = false;
			session.StatusMessage += " Artefact has been used and deactivated.";
		}

		// Check player win AFTER all effects (location effects, modifiers, etc.) are applied
		// Creature win is already checked immediately after catch, so this only triggers on escape
		if (session.PlayerProgress >= GameSession.MaxPlayerProgress)
		{
			session.IsGameOver = true;
			session.StatusMessage = "🚀 GAME OVER: Rescue has arrived! You escaped from Artemia!";
		}
	}

	/// <summary>
	/// Save game history to database when game ends
	/// </summary>
	public async Task SaveGameHistoryAsync(GameSession session, Guid userId)
	{
		if (_dbContext == null || !session.IsGameOver)
			return;

		var isPlayerWon = session.PlayerProgress >= GameSession.MaxPlayerProgress;
		var result = isPlayerWon ? "Win" : "Loss";

		var gameHistory = new GameHistory
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			CompletedAt = DateTime.UtcNow,
			RoundsPlayed = session.RoundNumber,
			PlayerProgress = session.PlayerProgress,
			CreatureProgress = session.CreatureProgress,
			Result = result,
			DurationSeconds = 0 // You can track this if you store StartTime in GameSession
		};

		_dbContext.GameHistories.Add(gameHistory);
		await _dbContext.SaveChangesAsync();
	}
}
