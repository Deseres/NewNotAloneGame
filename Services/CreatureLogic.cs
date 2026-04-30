using System.Linq;
using NotAlone.Models;

namespace NotAlone.Services;

/// <summary>
/// Strategic AI logic for the creature that makes intelligent decisions based on:
/// - Game state (progress, willpower, available locations)
/// - Risk assessment (high-value locations vs safe choices)
/// - Player behavior patterns
/// - Trap setup (beach/wreck blocking, progress denial)
/// - Win condition calculations
/// </summary>
public class CreatureLogic
{
	private readonly Random _random = Random.Shared;
	private Dictionary<int, int> _locationCatchHistory = new(); // Track catches by location within session
	private List<int> _playerLocationHistory = new(); // Track player's recent location choices
	private int _trapFailureCount = 0; // How many consecutive rounds trap strategy failed
	private int _consecutiveEscapes = 0; // Consecutive escapes from attempted catches
	private bool _isUsingTrapStrategy = true; // Switch between trap vs interception strategies
	
	// Strategy success tracking for multi-strategy mixing
	private int _trapSuccesses = 0; // Catches using trap locations (1,3,8)
	private int _trapAttempts = 0; // Total trap strategy attempts
	private int _interceptSuccesses = 0; // Catches using predicted interception
	private int _interceptAttempts = 0; // Total interception strategy attempts

	// Pattern detection for player exploitation
	private Dictionary<int, int> _locationFrequency = new(); // Track how often each location is used
	private int _singleLocationObsessionThreshold = 3; // If same location used 3+ times in last 5 rounds, it's suspicious
	private int? _detectedExploitLocation = null; // The location player is exploiting (if any)

	/// <summary>
	/// Detects if player is exploiting a single location through repeated use.
	/// This catches strategies like always choosing location 2 for infinite recycling.
	/// </summary>
	private void DetectPlayerLocationExploitation(GameSession session)
	{
		if (!session.LastPlayerChoice.HasValue)
			return;

		int currentChoice = session.LastPlayerChoice.Value;

		// Update frequency counter
		if (!_locationFrequency.ContainsKey(currentChoice))
			_locationFrequency[currentChoice] = 0;
		_locationFrequency[currentChoice]++;

		// Track player choice for history
		_playerLocationHistory.Add(currentChoice);
		if (_playerLocationHistory.Count > 5)
			_playerLocationHistory.RemoveAt(0); // Keep only last 5 rounds

		// Check for single-location obsession in recent rounds
		var recentLocations = _playerLocationHistory.TakeLast(5).ToList();
		int currentLocationInRecent = recentLocations.Count(l => l == currentChoice);

		// EXPLOITATION PATTERN 1: Same location 3+ times in last 5 rounds = HIGH PRIORITY TARGET
		if (currentLocationInRecent >= _singleLocationObsessionThreshold)
		{
			_detectedExploitLocation = currentChoice;
			return;
		}

		// EXPLOITATION PATTERN 2: Location used >50% of the time (high frequency abuse)
		if (_playerLocationHistory.Count >= 4)
		{
			double frequencyRate = (double)_locationFrequency[currentChoice] / _playerLocationHistory.Count;
			if (frequencyRate > 0.5)
			{
				_detectedExploitLocation = currentChoice;
				return;
			}
		}

		// EXPLOITATION PATTERN 3: Consecutive same-location choices (immediate obsession)
		if (_playerLocationHistory.Count >= 2)
		{
			int consecutiveCount = 1;
			for (int i = _playerLocationHistory.Count - 2; i >= 0; i--)
			{
				if (_playerLocationHistory[i] == currentChoice)
					consecutiveCount++;
				else
					break;
			}

			if (consecutiveCount >= 2)
			{
				_detectedExploitLocation = currentChoice;
				return;
			}
		}

		// No exploitation detected
		_detectedExploitLocation = null;
	}

	/// <summary>
	/// Detects if specific locations (2 and 6) are being abused through consecutive use.
	/// Returns the abused location if detected, otherwise null.
	/// </summary>
	private int? DetectLocationAbusePattern()
	{
		if (_playerLocationHistory.Count < 2)
			return null;

		// Check last two choices for locations 2 or 6 used consecutively
		int lastLocation = _playerLocationHistory[_playerLocationHistory.Count - 1];
		int secondLastLocation = _playerLocationHistory[_playerLocationHistory.Count - 2];

		// If either location 2 or 6 is used 2 times in a row, it's being abused
		if ((lastLocation == 2 || lastLocation == 6) && lastLocation == secondLastLocation)
		{
			return lastLocation;
		}

		return null;
	}

	public void SelectCreatureLocation(GameSession session)
	{
		if (session == null) return;

		// Apply deferred Artefact effect (disables modifier)
		if (session.IsArtefactActive)
		{
			session.CurrentModifier = CreatureModifier.None;
		}
		else
		{
			// Select random modifier for this round
			var modifiers = new[] { 
				CreatureModifier.DoubleDamage, 
				CreatureModifier.BlockPlayerProgress, 
				CreatureModifier.LoseRandomLocation, 
				CreatureModifier.BeachAndWreckBlock, 
				CreatureModifier.ExtraCreatureProgress 
			};
			session.CurrentModifier = modifiers[_random.Next(modifiers.Length)];
		}

		// SECOND PHASE: Creature blocks a location when player progress > 1
		// Blocking location is chosen FIRST from: AvailableLocations + LastPlayerChoice
		int? blockingLocation = null;
		if (session.PlayerProgress >= 4)
		{
			// Build candidate pool: AvailableLocations + LastPlayerChoice
			var blockingCandidates = new List<int>(session.AvailableLocations);
			if (session.LastPlayerChoice.HasValue && !blockingCandidates.Contains(session.LastPlayerChoice.Value))
			{
				blockingCandidates.Add(session.LastPlayerChoice.Value);
			}

			// Only pick blocking location if it would leave at least 1 attack candidate
			if (blockingCandidates.Count > 1)
			{
				blockingLocation = blockingCandidates[_random.Next(blockingCandidates.Count)];
				session.CreatureBlockingLocation = blockingLocation;
			}
		}

		// Creature attacks SECOND from: AvailableLocations + LastPlayerChoice - CreatureBlockingLocation
		int creatureChoice;
		string selectionLogic = "";

		if (session.IsRiverVisionActive && session.IsRiverVisionRevealed && session.LastCreatureChoice.HasValue)
		{
			creatureChoice = session.LastCreatureChoice.Value;
			selectionLogic = "(Видение реки - предсказано)";
		}
		else
		{
			// Build candidate pool: AvailableLocations + LastPlayerChoice, excluding blocking location
			var attackCandidates = new List<int>(session.AvailableLocations);
			if (session.LastPlayerChoice.HasValue && !attackCandidates.Contains(session.LastPlayerChoice.Value))
			{
				attackCandidates.Add(session.LastPlayerChoice.Value);
			}
			
			// Remove blocking location from attack candidates if it exists
			if (blockingLocation.HasValue)
			{
				attackCandidates.Remove(blockingLocation.Value);
			}

			// Make strategic location decision based on the randomly chosen modifier
			creatureChoice = DetermineOptimalCreatureLocation(session, session.CurrentModifier, attackCandidates, out selectionLogic);
		}

		// Store creature's chosen location for deferred comparison in ResolveRound
		session.CreatureChosenLocation = creatureChoice;
		session.StatusMessage = $"[CreatureTurn] ✓ Существо выбрало локацию {creatureChoice} {selectionLogic}. Модификатор: {session.CurrentModifier}. Переход в Result фазу...";

		if (blockingLocation.HasValue)
		{
			session.StatusMessage += $"\n[Phase 2] Существо выбрало блокирующую локацию: {blockingLocation} (скрыто от игрока).";
		}
	}

	/// <summary>
	/// AI decision engine to determine the optimal location choice based on the assigned modifier.
	/// Makes strategic decisions to maximize the effectiveness of the current modifier.
	/// Optionally accepts a custom candidate pool for location selection.
	/// </summary>
	private int DetermineOptimalCreatureLocation(GameSession session, CreatureModifier currentModifier, List<int> candidateLocations, out string selectionLogic)
	{
		selectionLogic = "";

		// ===== PHASE 0: DETECT PLAYER EXPLOITATION PATTERNS =====
		DetectPlayerLocationExploitation(session);
		
		// If we detect a player exploiting a location, prioritize catching them there
		if (_detectedExploitLocation.HasValue && candidateLocations.Contains(_detectedExploitLocation.Value))
		{
			selectionLogic = $"(⚠️ ОБНАРУЖЕНА ЭКСПЛУАТАЦИЯ - игрок повторяет локацию {_detectedExploitLocation}!)";
			return _detectedExploitLocation.Value;
		}

		// ===== PHASE 0.5: DETECT ABUSE OF LOCATIONS 2 & 6 =====
		// If player is abusing locations 2 or 6 through consecutive use, PRIORITIZE catching them there
		int? abusedLocationForCatch = DetectLocationAbusePattern();
		if (abusedLocationForCatch.HasValue && candidateLocations.Contains(abusedLocationForCatch.Value))
		{
			selectionLogic = $"(🎯 ПЕРЕХВАТ АБУСИВА - локация {abusedLocationForCatch} использована дважды подряд!)";
			return abusedLocationForCatch.Value;
		}

		// ===== PHASE 1: CRITICAL WIN CONDITIONS =====
		// If creature is close to winning, be aggressive and try to catch
		if (session.CreatureProgress >= GameSession.MaxCreatureProgress - 1)
		{
			var catchLocation = PredictPlayerLocationAndCatch(session);
			if (catchLocation.HasValue)
			{
				selectionLogic = $"(Победная стратегия - попытка поймать игрока на {catchLocation})";
				return catchLocation.Value;
			}
		}

		// If creature is far behind, take calculated risks
		if (session.CreatureProgress < 2 && session.PlayerProgress > GameSession.MaxPlayerProgress - 3)
		{
			var riskLocation = GetHighRiskHighRewardLocation(session);
			selectionLogic = "(Рискованная стратегия - необходима победа)";
			return riskLocation;
		}

		// ===== PHASE 2: PLAYER THREAT ASSESSMENT =====
		// If player is about to win, block their progress
		if (session.PlayerProgress >= GameSession.MaxPlayerProgress - 3)
		{
			var blockingLocation = DetermineProgressBlockingLocation(session, currentModifier);
			if (blockingLocation > 0)
			{
				selectionLogic = "(Оборонная стратегия - блокировка прогресса)";
				return blockingLocation;
			}
		}

		// ===== STRATEGY MIXING: Use best strategy based on success rates =====
		// Only apply mixing if we have some history and trap failures
		if (_playerLocationHistory.Count >= 2 && (_trapAttempts > 0 || _interceptAttempts > 0))
		{
			// Calculate success rates for each strategy
			double trapSuccessRate = _trapAttempts > 0 ? (double)_trapSuccesses / _trapAttempts : 0.3;
			double interceptSuccessRate = _interceptAttempts > 0 ? (double)_interceptSuccesses / _interceptAttempts : 0.2;
			
			// If trap strategy is failing badly (>2 failures), shift weight toward interception
			if (_trapFailureCount >= 2)
			{
				interceptSuccessRate += 0.3; // Boost interception weight
			}
			
			// Use weighted random selection
			double totalWeight = trapSuccessRate + interceptSuccessRate;
			double trapWeight = trapSuccessRate / totalWeight;
			double roll = _random.NextDouble();
			
			if (roll < trapWeight)
			{
				// Use trap strategy
				var trapLocation = SetupTrapForConfidentPlayer(session);
				_trapAttempts++;
				selectionLogic = "(Ловушка - смешанная стратегия)";
				_isUsingTrapStrategy = true;
				return trapLocation;
			}
			else
			{
				// Try interception strategy
				var patternInterception = AttemptDirectInterception(session, out var interceptLogic);
				if (patternInterception.HasValue)
				{
					_interceptAttempts++;
					_isUsingTrapStrategy = false;
					selectionLogic = $"(Перехватывание - смешанная стратегия) {interceptLogic}";
					return patternInterception.Value;
				}
			}
		}

		// ===== PHASE 3: FALLBACK TO LOCATION VALUE ANALYSIS =====
		// Evaluate all available locations considering the current modifier's effectiveness
		var locationScores = EvaluateAllLocations(session, currentModifier, candidateLocations);
		var bestLocation = locationScores.OrderByDescending(x => x.Value).First().Key;

		var topScore = locationScores[bestLocation];
		if (topScore > 80)
			selectionLogic = "(Оптимальная локация - высокий потенциал для текущего модификатора)";
		else if (topScore > 50)
			selectionLogic = "(Сбалансированная локация)";
		else
			selectionLogic = "(Безопасная локация)";

		return bestLocation;
	}

	/// <summary>
	/// Evaluates each location (1-10) based on strategic value with the given modifier.
	/// Higher scores = more valuable for creature given the current modifier.
	/// Incorporates dynamic context-aware adjustments based on game state.
	/// Only evaluates locations from the provided candidateLocations list.
	/// </summary>
	private Dictionary<int, int> EvaluateAllLocations(GameSession session, CreatureModifier currentModifier, List<int> candidateLocations)
	{
		var scores = new Dictionary<int, int>();

		// Calculate game urgency (0-1, where 1 = must win now)
		float gameUrgency = CalculateGameUrgency(session);
		bool playerNearVictory = session.PlayerProgress >= GameSession.MaxPlayerProgress - 2;
		bool creatureNearVictory = session.CreatureProgress >= GameSession.MaxCreatureProgress - 2;

		foreach (var loc in session.Locations)
		{
			int score = 0;

			// Only score candidate locations
			bool isCandidate = candidateLocations.Contains(loc);
			
			if (!isCandidate)
				continue;

			// Location-specific strategic value (DYNAMIC ADJUSTMENTS)
			switch (loc)
			{
				case 1: // Lair - Extra willpower damage if caught
					score += 30;
					if (session.PlayerWillpower <= 2)
						score += 20; // Dangerous for player
					// Adjust Lair value based on urgency: more valuable when catching is critical
					if (creatureNearVictory)
						score += (int)(30 * gameUrgency); // High value for catch when winning
					// DoubleDamage modifier makes Lair even better
					if (currentModifier == CreatureModifier.DoubleDamage)
						score += 25;
				// Additional boost when player is low on willpower - can kill in one shot
				if (session.PlayerWillpower <= 2)
					score += 10;
					// Less valuable when already winning or when player is about to escape
					if (playerNearVictory)
						score -= 15;
					break;

				case 2: // Jungle - Resist cards
					score += 20;
					break;

				case 4: // Beach - Beacon interaction
					score += 20;
					if (session.IsBeaconLit)
						score += 15; // Can extinguish beacon
					// Increase value if player is close to victory (escape blocker)
					if (playerNearVictory)
						score += (int)(20 * gameUrgency);
					// BeachAndWreckBlock modifier makes Beach more valuable
					if (currentModifier == CreatureModifier.BeachAndWreckBlock)
						score += 20;
					break;

				case 5: // Rover - Unlocks blocked location
					score += 10;
					int blockedCount = 10 - session.AvailableLocations.Count - session.UsedLocations.Count;
					if (blockedCount > 0)
						score += 15; // Relevant if locations are blocked
					break;

				case 6: // Swamp - Restores up to 2 cards
					score += 18;
					break;

				case 7: // Shelter - Generates survival card
					score += 22;
					break;

				case 8: // Wreck - Player progress
					score += 25;
					// Increase value if player is close to victory (escape blocker)
					if (playerNearVictory)
						score += (int)(25 * gameUrgency);
					// BeachAndWreckBlock modifier makes Wreck more valuable
					if (currentModifier == CreatureModifier.BeachAndWreckBlock)
						score += 20;
					break;

				case 9: // Source - Restores willpower
					score += 12;
				// Desperate player with low willpower is likely to seek Source for healing
				// This makes it a high-probability catch location
				// Graduated response based on urgency of player's health
				if (session.PlayerWillpower == 2)
					score += 10;  // Boost Source - creature uses it as a trap
				if (session.PlayerWillpower <= 1)
					score += 20;  // Boost Source - creature uses it as a trap
				break;

			case 10: // Artefact - Disables creature power
				score -= 40; // Avoid at all costs
					// Exception: if creature is losing badly, consider sacrificing modifier
					if (session.CreatureProgress < 1 && session.PlayerProgress > GameSession.MaxPlayerProgress - 2)
						score = -20; // Less negative, more viable as desperation move
					break;
			}

			// Modifier-specific strategic bonuses
			switch (currentModifier)
			{
				case CreatureModifier.DoubleDamage:
					// Prioritize catching opportunities
					if (session.LastPlayerChoice.HasValue && session.LastPlayerChoice.Value == loc)
						score += 40; // Catch is worth double damage
					// Bonus for locations that historically caught player
					if (_locationCatchHistory.ContainsKey(loc) && _locationCatchHistory[loc] > 0)
						score += _locationCatchHistory[loc] * 8;
					break;

				case CreatureModifier.BlockPlayerProgress:
					// Beach/Wreck are already blocked; no bonus needed
					// Focus on other strategic objectives instead
					break;

				case CreatureModifier.LoseRandomLocation:
					// Always useful, especially when many locations available
					if (session.AvailableLocations.Count > 6)
						score += 20;
					break;

				case CreatureModifier.ExtraCreatureProgress:
					// Prioritize catching (triggers on catch)
					// Bonus for historically successful catches
					if (_locationCatchHistory.ContainsKey(loc) && _locationCatchHistory[loc] > 0)
						score += _locationCatchHistory[loc] * 10;
					break;

				case CreatureModifier.BeachAndWreckBlock:
					// Beach and Wreck are blocked; penalize them
					// Creature should focus on other strategic locations
					if (loc == 4 || loc == 8)
						score -= 30; // Avoid blocked locations
					break;
			}

			// Always consider player's last location choice (but never as priority)
			if (session.LastPlayerChoice.HasValue && session.LastPlayerChoice.Value == loc && currentModifier != CreatureModifier.DoubleDamage)
				score += 15; // Modest bonus for potential re-visit or catching opportunity

			// Strategic positioning
			if (session.CreatureProgress > session.PlayerProgress)
				score += 10; // Maintain momentum
			else if (session.CreatureProgress < session.PlayerProgress - 3)
				score += (int)(20 * gameUrgency); // Need aggressive moves when far behind

			scores[loc] = Math.Max(0, score);
		}

		return scores;
	}

	/// <summary>
	/// Calculates urgency level based on game state (0-1 scale).
	/// 1 = must act now (near victory or defeat), 0 = plenty of time
	/// </summary>
	private float CalculateGameUrgency(GameSession session)
	{
		int playerSteps = GameSession.MaxPlayerProgress - session.PlayerProgress;
		int creatureSteps = GameSession.MaxCreatureProgress - session.CreatureProgress;

		float playerUrgency = playerSteps <= 2 ? 0.9f : playerSteps <= 3 ? 0.7f : 0.3f;
		float creatureUrgency = creatureSteps <= 2 ? 0.9f : creatureSteps <= 3 ? 0.7f : 0.3f;

		return Math.Max(playerUrgency, creatureUrgency);
	}

	/// <summary>
	/// Predicts where player might go and tries to catch them.
	/// Uses enhanced pattern recognition, multi-round history analysis, and edge case handling.
	/// </summary>
	private int? PredictPlayerLocationAndCatch(GameSession session)
	{
		if (!session.LastPlayerChoice.HasValue)
		{
			// EDGE CASE: First round with no player history
			// Player typically goes for safe/valuable locations
			var safeLocations = session.AvailableLocations
				.Where(l => l != 1 && l != 10) // Avoid Lair and Artefact extremes
				.OrderByDescending(l => GetLocationSafetyScore(l))
				.Take(3)
				.ToList();

			if (safeLocations.Count > 0)
				return safeLocations[_random.Next(safeLocations.Count)];
			return null;
		}

		// Track player choice for history
		_playerLocationHistory.Add(session.LastPlayerChoice.Value);
		if (_playerLocationHistory.Count > 5)
			_playerLocationHistory.RemoveAt(0); // Keep only last 5 rounds

		// PATTERN 1: Player rarely repeats consecutive locations
		// But may return to favorite location after skipping it
		if (session.AvailableLocations.Contains(session.LastPlayerChoice.Value))
		{
			bool playerAlternates = _playerLocationHistory.Count >= 2 &&
				_playerLocationHistory[_playerLocationHistory.Count - 1] != _playerLocationHistory[_playerLocationHistory.Count - 2];

			int repeatConfidence = playerAlternates ? 35 : 60; // Lower if alternating, higher if stationary
			if (_random.Next(100) < repeatConfidence)
				return session.LastPlayerChoice.Value;
		}

		// PATTERN 2: Multi-round pattern analysis
		// Check if player cycles between 2-3 locations
		if (_playerLocationHistory.Count >= 3)
		{
			var recentLocations = _playerLocationHistory.TakeLast(3).ToList();
			var uniqueRecentLocations = recentLocations.Distinct().Count();

			if (uniqueRecentLocations <= 2)
			{
				// Player cycles between 1-2 locations; predict next in cycle
				var predictedCycle = recentLocations.GroupBy(l => l)
					.OrderByDescending(g => g.Count())
					.First()
					.Key;

				if (session.AvailableLocations.Contains(predictedCycle))
					return predictedCycle;
			}
		}

		// PATTERN 3: Player prefers high-value or safe locations
		var predictedChoices = session.AvailableLocations
			.Where(l => l != 10) // Avoid Artefact
			.OrderByDescending(l => GetLocationSafetyScore(l))
			.Take(3)
			.ToList();

		if (predictedChoices.Count > 0)
			return predictedChoices[_random.Next(predictedChoices.Count)];

		return null;
	}

	/// <summary>
	/// Gets "safety score" of a location from player's perspective.
	/// Higher = safer location.
	/// </summary>
	private int GetLocationSafetyScore(int location)
	{
		return location switch
		{
			1 => 0,  // Lair - Dangerous
			2 => 40, // Jungle - Moderate
			3 => 45, // River - Moderate
			4 => 35, // Beach - Depends on beacon
			5 => 50, // Rover - Safe
			6 => 48, // Swamp - Safe
			7 => 52, // Shelter - Safe
			8 => 38, // Wreck - Dangerous (progress loss)
			9 => 55, // Source - Safe
			10 => 60, // Artefact - Very safe for player
			_ => 30
		};
	}

	/// <summary>
	/// When player appears confident (high willpower), set up a trap.
	/// </summary>
	private int SetupTrapForConfidentPlayer(GameSession session)
	{
		// If player just resisted, they might go for another high-value location
		// Try high-reward but risky locations
		var trapLocations = new[] { 1, 8, 3 }; // Lair, Wreck, River
		var validTraps = trapLocations.Where(l => session.AvailableLocations.Contains(l)).ToList();

		if (validTraps.Count > 0)
			return validTraps[_random.Next(validTraps.Count)];

		return session.AvailableLocations.Count > 0 
			? session.AvailableLocations[_random.Next(session.AvailableLocations.Count)]
			: _random.Next(1, 11);
	}

	/// <summary>
	/// Direct interception strategy: Move directly to predicted player location.
	/// Used when trap strategy repeatedly fails.
	/// </summary>
	private int? AttemptDirectInterception(GameSession session, out string logic)
	{
		logic = "";

		// Get predicted location using pattern analysis
		var predictedLocation = PredictPlayerLocationAndCatch(session);
		if (!predictedLocation.HasValue)
		{
			// Fallback to frequency analysis
			if (_playerLocationHistory.Count > 0)
			{
				var mostFrequentLocation = _playerLocationHistory
					.GroupBy(x => x)
					.OrderByDescending(g => g.Count())
					.FirstOrDefault()?.Key;

				if (mostFrequentLocation.HasValue && session.AvailableLocations.Contains(mostFrequentLocation.Value))
				{
					logic = $"(Частая локация: {mostFrequentLocation})";
					return mostFrequentLocation.Value;
				}
			}
			return null;
		}

		// Verify the predicted location is available
		if (session.AvailableLocations.Contains(predictedLocation.Value))
		{
			logic = $"(Предсказанная локация: {predictedLocation})";
			return predictedLocation.Value;
		}

		return null;
	}

	/// <summary>
	/// Determines locations that block player progress effectively.
	/// Prioritizes Beach and Wreck when using BeachAndWreckBlock modifier.
	/// </summary>
	private int DetermineProgressBlockingLocation(GameSession session, CreatureModifier currentModifier)
	{
		// Beach and Wreck block progress if BeachAndWreckBlock modifier is active
		if (currentModifier == CreatureModifier.BeachAndWreckBlock)
		{
			var blockingLocations = new[] { 4, 8 }; // Beach, Wreck
			var available = blockingLocations.Where(l => session.AvailableLocations.Contains(l)).ToList();

			if (available.Count > 0)
				return available[_random.Next(available.Count)];
		}

		// Fallback: Try to catch player directly
		if (session.LastPlayerChoice.HasValue && session.AvailableLocations.Contains(session.LastPlayerChoice.Value))
			return session.LastPlayerChoice.Value;

		return session.AvailableLocations.Count > 0 
			? session.AvailableLocations[_random.Next(session.AvailableLocations.Count)]
			: _random.Next(1, 11);
	}

	/// <summary>
	/// High risk, high reward strategy when creature is losing badly.
	/// </summary>
	private int GetHighRiskHighRewardLocation(GameSession session)
	{
		// Prioritize locations that give creature advantage
		var aggressiveLocations = new[] { 1, 3, 8 }; // Lair, River, Wreck
		var available = aggressiveLocations.Where(l => session.AvailableLocations.Contains(l)).ToList();

		if (available.Count > 0)
			return available[_random.Next(available.Count)];

		// If no aggressive locations available, try to predict and catch
		var prediction = PredictPlayerLocationAndCatch(session);
		return prediction ?? (session.AvailableLocations.Count > 0 
			? session.AvailableLocations[_random.Next(session.AvailableLocations.Count)]
			: _random.Next(1, 11));
	}

	/// <summary>
	/// Records a successful catch at a location for decision history tracking.
	/// This data is used to refine future location scoring without making it predictable.
	/// </summary>
	public void RecordCatch(int location)
	{
		if (_locationCatchHistory.ContainsKey(location))
			_locationCatchHistory[location]++;
		else
			_locationCatchHistory[location] = 1;

		// Update strategy success rates
		if (_isUsingTrapStrategy)
		{
			_trapSuccesses++;
		}
		else
		{
			_interceptSuccesses++;
		}
		
		// Successful catch resets trap failure counter
		_trapFailureCount = 0;
		_consecutiveEscapes = 0;
	}

	/// <summary>
	/// Records when a trap strategy fails (creature picks trap location but player escapes).
	/// </summary>
	public void RecordTrapFailure()
	{
		if (_isUsingTrapStrategy)
		{
			_consecutiveEscapes++;
			// Only increment trap counter if we had a strong escape (they expected trap and still got out)
			if (_consecutiveEscapes >= 1)
			{
				_trapFailureCount++;
			}
		}
	}

	/// <summary>
	/// Resets decision history (call when starting new game or session).
	/// </summary>
	public void ResetHistory()
	{
		_locationCatchHistory.Clear();
		_playerLocationHistory.Clear();
		_trapFailureCount = 0;
		_consecutiveEscapes = 0;
		_isUsingTrapStrategy = true; // Reset to trap strategy at game start
		
		// Reset strategy tracking
		_trapSuccesses = 0;
		_trapAttempts = 0;
		_interceptSuccesses = 0;
		_interceptAttempts = 0;

		// Reset exploitation detection
		_locationFrequency.Clear();
		_detectedExploitLocation = null;
	}

}
