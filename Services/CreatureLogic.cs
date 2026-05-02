using System.Linq;
using NotAlone.Models;

namespace NotAlone.Services;

/// <summary>
/// Creature behavior profiles: each has a preferred location that acts as a "home base"
/// </summary>
public enum CreatureBehavior
{
    LairHunter = 1,      // Prefers location 1 (Lair) - aggressive, high-risk hunting
    RiverWatcher = 3,    // Prefers location 3 (River) - strategic, vision control
    RoverPatroller = 5   // Prefers location 5 (Rover) - methodical, exploration
}

/// <summary>
/// Strategic AI logic for the creature with seven-tier decision hierarchy:
/// Tier 0: Abuse detection (40%+ frequency > up to 85% attack probability)
/// Tier 1: Win condition (creature progress ≥ 4, aggressive hunting)
/// Tier 2: Player threat (player progress ≥ 5, defensive blocking)
/// Tier 3: Strategy mixing (60% trap + 40% interception, soft 10% transitions)
/// Tier 4: Pattern prediction (exponential decay 0.3, cycle detection)
/// Tier 5: Location value scoring (catch rate + strategic value + modifier bonus)
/// Tier 6: Fallback (random from available candidates)
/// 
/// Creature picks a behavior profile (LairHunter/RiverWatcher/RoverPatroller) at game start
/// and slightly favors its preferred location. After 2 consecutive choices of the same location,
/// it shifts to a different behavior to stay unpredictable.
/// </summary>
public class CreatureLogic
{
    private readonly Random _random = Random.Shared;

    // ===== INTERNAL STATE =====
    private List<int> _playerLocationHistory = new();
    private Dictionary<int, int> _locationCatchHistory = new();
    private Dictionary<int, int> _locationTotalAttempts = new();
    private Dictionary<int, float> _locationFrequency = new();
    private int? _detectedExploitLocation;
    private int? _lastTargetedLocation;
    private int _consecutiveTargets = 0;
    
    private CreatureBehavior _currentBehavior = CreatureBehavior.RiverWatcher;
    private int _behaviorChoicesSinceSwitch = 0;

    private bool _isUsingTrapStrategy = true;
    private int _trapSuccesses;
    private int _trapAttempts;
    private int _interceptSuccesses;
    private int _interceptAttempts;
    private int _consecutiveRoundsWithoutCatch;
    private int _roundsSinceLastPatternUpdate;
    private float _trapStrategyWeight = 0.60f;

    // ===== CONSTANTS =====
    private const float AbuseFrequencyThreshold = 0.40f;
    private const int AbuseConfidenceMax = 85;
    private const float PatternDecayFactor = 0.3f;
    private const float AbusePenaltyDecay = 0.30f;
    private const int PatternUpdateFrequency = 2;
    private const float SoftTransitionRate = 0.10f;
    private const int StrategyUpdateThreshold = 3;
    private const int MaxHistorySize = 5;

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Main decision entry point. Assigns modifier, optionally selects blocking location (phase 2),
    /// builds attack candidates, and determines optimal attack location via the 7-tier hierarchy.
    /// </summary>
    public void SelectCreatureLocation(GameSession session)
    {
        if (session is null) return;

        // Assign modifier for this round
        if (session.IsArtefactActive)
        {
            session.CurrentModifier = CreatureModifier.None;
        }
        else
        {
            var modifiers = new[]
            {
                CreatureModifier.DoubleDamage,
                CreatureModifier.BlockPlayerProgress,
                CreatureModifier.LoseRandomLocation,
                CreatureModifier.BeachAndWreckBlock,
                CreatureModifier.ExtraCreatureProgress
            };
            session.CurrentModifier = modifiers[_random.Next(modifiers.Length)];
        }

        // Update strategy weights every PatternUpdateFrequency rounds
        _roundsSinceLastPatternUpdate++;
        if (_roundsSinceLastPatternUpdate >= PatternUpdateFrequency)
        {
            UpdateStrategyWeights();
            _roundsSinceLastPatternUpdate = 0;
        }

        // PHASE 2: Select blocking location from round 0 onwards
        int? blockingLocation = null;
        if (session.CurrentModifier != CreatureModifier.None)
        {
            blockingLocation = DetermineBlockingLocation(session);
            session.CreatureBlockingLocation = blockingLocation;
        }
        else
        {
            session.CreatureBlockingLocation = null;
        }

        // Build attack candidates: AvailableLocations + CurrentPlayerChoice - BlockingLocation
        // When Fog is active, creature can also target used locations (sees the full map)
        var attackCandidates = new List<int>();
        if (session.IsFogActive)
        {
            // Fog active: creature can target available + used locations (full vision of active board)
            attackCandidates = new List<int>(session.AvailableLocations);
            foreach (var used in session.UsedLocations)
                if (!attackCandidates.Contains(used))
                    attackCandidates.Add(used);
        }
        else
        {
            // Normal mode: creature only knows about available locations
            attackCandidates = new List<int>(session.AvailableLocations);
        }
        
        if (session.CurrentPlayerChoice.HasValue && !attackCandidates.Contains(session.CurrentPlayerChoice.Value))
            attackCandidates.Add(session.CurrentPlayerChoice.Value);
        if (blockingLocation.HasValue)
            attackCandidates.Remove(blockingLocation.Value);

        if (attackCandidates.Count == 0)
            attackCandidates = new List<int>(session.AvailableLocations);

        // River Vision: reuse pre-revealed choice
        int creatureChoice;
        string selectionLogic;
        if (session.IsRiverVisionActive && session.IsRiverVisionRevealed && session.PreviousCreatureChoice.HasValue)
        {
            creatureChoice = session.PreviousCreatureChoice.Value;
            selectionLogic = "(Видение реки - предсказано)";
        }
        else
        {
            creatureChoice = DetermineOptimalCreatureLocation(session, session.CurrentModifier, attackCandidates, out selectionLogic);
        }

        session.CreatureChosenLocation = creatureChoice;
        session.PreviousCreatureChoice = creatureChoice;

        // Track location fatigue: update consecutive targeting counter
        if (_lastTargetedLocation == creatureChoice)
        {
            _consecutiveTargets++;
        }
        else
        {
            _lastTargetedLocation = creatureChoice;
            _consecutiveTargets = 1;
        }

        // Update behavior preference based on choices
        UpdateBehaviorPreference(creatureChoice);

        var blockMsg = blockingLocation.HasValue
            ? $" Блокировка: {blockingLocation} (скрыто)."
            : string.Empty;

        session.StatusMessage =
            $"[CreatureTurn] ✓ Существо выбрало локацию {creatureChoice} {selectionLogic}. " +
            $"Модификатор: {session.CurrentModifier}.{blockMsg}";

        if (blockingLocation.HasValue)
            session.StatusMessage += $"\n[Phase 2] Существо выбрало блокирующую локацию: {blockingLocation} (скрыто от игрока)."; 
    }

    /// <summary>
    /// Records a successful catch at the given location for adaptive learning.
    /// Resets consecutive escape counter and increments strategy success counts.
    /// </summary>
    public void RecordCatch(int location)
    {
        if (!_locationCatchHistory.ContainsKey(location))
            _locationCatchHistory[location] = 0;
        _locationCatchHistory[location]++;

        if (!_locationTotalAttempts.ContainsKey(location))
            _locationTotalAttempts[location] = 0;
        _locationTotalAttempts[location]++;

        if (_isUsingTrapStrategy)
            _trapSuccesses++;
        else
            _interceptSuccesses++;

        _consecutiveRoundsWithoutCatch = 0;
    }

    /// <summary>
    /// Records a successful escape from the given location.
    /// Decays abuse frequency by 30% to reduce over-targeting the same location.
    /// </summary>
    public void RecordEscape(int location)
    {
        if (!_locationTotalAttempts.ContainsKey(location))
            _locationTotalAttempts[location] = 0;
        _locationTotalAttempts[location]++;

        // Decay abuse frequency on escape
        if (_locationFrequency.ContainsKey(location))
        {
            _locationFrequency[location] *= (1f - AbusePenaltyDecay);
            if (_locationFrequency[location] < AbuseFrequencyThreshold)
                _detectedExploitLocation = null;
        }

        _consecutiveRoundsWithoutCatch++;
    }

    /// <summary>
    /// Resets all internal AI state for a new game session.
    /// </summary>
    public void ResetHistory()
    {
        _playerLocationHistory.Clear();
        _locationCatchHistory.Clear();
        _locationTotalAttempts.Clear();
        _locationFrequency.Clear();
        _detectedExploitLocation = null;
        _lastTargetedLocation = null;
        _consecutiveTargets = 0;
        
        // Pick a random behavior profile for this game
        var behaviors = new[] { CreatureBehavior.LairHunter, CreatureBehavior.RiverWatcher, CreatureBehavior.RoverPatroller };
        _currentBehavior = behaviors[_random.Next(behaviors.Length)];
        _behaviorChoicesSinceSwitch = 0;
        
        _isUsingTrapStrategy = true;
        _trapSuccesses = 0;
        _trapAttempts = 0;
        _interceptSuccesses = 0;
        _interceptAttempts = 0;
        _consecutiveRoundsWithoutCatch = 0;
        _roundsSinceLastPatternUpdate = 0;
        _trapStrategyWeight = 0.60f;
    }

    // ===== PRIVATE DECISION ENGINES =====

    /// <summary>
    /// TIER 0: Detects player exploiting a specific location at >=40% weighted frequency.
    /// Sets _detectedExploitLocation when abuse detected, clears it when frequency drops.
    /// Uses PREVIOUS player choices only (added after previous round resolved), not current round's choice.
    /// </summary>
    private void DetectPlayerLocationAbuse(GameSession session)
    {
        if (!session.PreviousPlayerChoice.HasValue) return;

        int previousChoice = session.PreviousPlayerChoice.Value;

        // Add to history (keep last 5)
        _playerLocationHistory.Add(previousChoice);
        if (_playerLocationHistory.Count > MaxHistorySize)
            _playerLocationHistory.RemoveAt(0);

        if (_playerLocationHistory.Count < 2) return;

        // Calculate weighted frequency using exponential decay
        float totalWeight = 0f;
        var weightedCounts = new Dictionary<int, float>();

        for (int i = 0; i < _playerLocationHistory.Count; i++)
        {
            int roundsAgo = _playerLocationHistory.Count - 1 - i;
            float weight = MathF.Exp(-PatternDecayFactor * roundsAgo);
            int loc = _playerLocationHistory[i];

            if (!weightedCounts.ContainsKey(loc)) weightedCounts[loc] = 0f;
            weightedCounts[loc] += weight;
            totalWeight += weight;
        }

        // Update frequency map
        foreach (var kvp in weightedCounts)
            _locationFrequency[kvp.Key] = totalWeight > 0 ? kvp.Value / totalWeight : 0f;

        // Check threshold
        if (_locationFrequency.TryGetValue(previousChoice, out float freq) && freq >= AbuseFrequencyThreshold)
            _detectedExploitLocation = previousChoice;
        else if (_detectedExploitLocation == previousChoice)
            _detectedExploitLocation = null;
    }

    /// <summary>
    /// TIER 3: Trap strategy - selects locations with modifier synergy (Lair, Wreck, River).
    /// Returns null if no synergistic trap location is available in candidates.
    /// IMPORTANT: When modifier is BeachAndWreckBlock or BlockPlayerProgress, avoids Beach(4) and Wreck(8)
    /// because those modifiers are negated if the creature targets those locations.
    /// </summary>
    private int? SelectTrapLocationWithSynergy(GameSession session, CreatureModifier modifier, List<int> candidates)
    {
        var synergyLocations = modifier switch
        {
            CreatureModifier.DoubleDamage          => new[] { 3, 8 },
            CreatureModifier.BeachAndWreckBlock    => new[] { 3, 7 },      // Avoid Beach(4) & Wreck(8)
            CreatureModifier.BlockPlayerProgress   => new[] { 3, 7 },      // Avoid Beach(4) & Wreck(8)
            CreatureModifier.ExtraCreatureProgress => new[] { 8, 3 },
            CreatureModifier.LoseRandomLocation    => new[] { 3, 8 },
            _                                      => new[] { 3, 8 }
        };

        var available = synergyLocations.Where(l => candidates.Contains(l)).ToList();
        return available.Count > 0 ? available[_random.Next(available.Count)] : (int?)null;
    }

    /// <summary>
    /// TIER 5: Scores all candidate locations using catch rate history, strategic value, and modifier bonuses.
    /// Returns a dictionary of location to integer score (0-100 scale).
    /// Only scores locations present in candidates.
    /// IMPORTANT: When modifier is BeachAndWreckBlock or BlockPlayerProgress, skips Beach(4) and Wreck(8)
    /// because the creature's attack is negated if it targets those locations.
    /// </summary>
    private Dictionary<int, int> EvaluateAllLocations(GameSession session, CreatureModifier modifier, List<int> candidates)
    {
        var scores = new Dictionary<int, int>();
        float urgency = CalculateGameUrgency(session);
        bool playerNearVictory = session.PlayerProgress >= GameSession.MaxPlayerProgress - 2;
        bool creatureNearVictory = session.CreatureProgress >= GameSession.MaxCreatureProgress - 2;
        bool shouldAvoidBeachWreck = modifier == CreatureModifier.BeachAndWreckBlock || modifier == CreatureModifier.BlockPlayerProgress;

        foreach (var loc in candidates)
        {
            // Skip Beach(4) and Wreck(8) entirely if modifier requires avoiding them
            if (shouldAvoidBeachWreck && (loc == 4 || loc == 8))
                continue;
            float catchRate = CalculateCatchRateForLocation(loc);
            float strategicValue = CalculateLocationStrategicValue(loc, session, modifier);

            int score = (int)(catchRate * 40f + strategicValue * 0.6f);

            // LOCATION FATIGUE: Penalize if creature targeted this location consecutively
            if (_lastTargetedLocation == loc && _consecutiveTargets > 0)
            {
                // Exponential fatigue penalty: -20% per consecutive target
                float fatiguePenalty = 1f - (_consecutiveTargets * 0.20f);
                fatiguePenalty = Math.Max(0.2f, fatiguePenalty); // Floor at 20% of original score
                score = (int)(score * fatiguePenalty);
            }

            // BEHAVIOR PREFERENCE: Add small bonus if this location matches creature's personality
            score += GetBehaviorPreferenceBonus(loc);

            // DIMINISHING RETURNS: Penalize over-attacking the same location
            if (_detectedExploitLocation == loc && _locationTotalAttempts.TryGetValue(loc, out int attempts) && attempts >= 3)
            {
                score = (int)(score * 0.5f); // Cut score in half if creature is abusing the same location too much
            }

            score += loc switch
            {
                1 =>
                    (session.PlayerWillpower <= 1 ? 5 : 0) +
                    (creatureNearVictory ? (int)(5 * urgency) : 0) +
                    (modifier == CreatureModifier.DoubleDamage ? 5 : 0),
                4 =>
                    (session.IsBeaconLit ? 15 : 0) +
                    (playerNearVictory ? (int)(20 * urgency) : 0) +
                    (modifier == CreatureModifier.BeachAndWreckBlock ? 20 : 0),
                7 =>
                    (playerNearVictory ? (int)(15 * urgency) : 0) +
                    (modifier == CreatureModifier.BlockPlayerProgress ? 15 : 0),
                8 =>
                    (playerNearVictory ? (int)(25 * urgency) : 0) +
                    (modifier == CreatureModifier.BeachAndWreckBlock ? 20 : 0),
                9 =>
                    (session.PlayerWillpower == 2 ? 10 : 0) +
                    (session.PlayerWillpower <= 1 ? 20 : 0),
                10 => modifier != CreatureModifier.None ? -40 : -20,
                _  => 0
            };

            if (modifier == CreatureModifier.DoubleDamage)
            {
                if (_locationCatchHistory.TryGetValue(loc, out int catches) && catches > 0)
                    score += catches * 8;
            }
            else if (modifier == CreatureModifier.ExtraCreatureProgress)
            {
                if (_locationCatchHistory.TryGetValue(loc, out int catches) && catches > 0)
                    score += catches * 10;
            }

            if (_locationFrequency.TryGetValue(loc, out float freq) && freq > 0.25f)
                score += (int)(freq * 20f);

            if (session.CreatureProgress > session.PlayerProgress)
                score += 10;
            else if (session.CreatureProgress < session.PlayerProgress - 3)
                score += (int)(20 * urgency);

            scores[loc] = Math.Max(0, score);
        }

        return scores;
    }

    /// <summary>
    /// TIER 4: Predicts the player's next location using weighted exponential decay (0.3) and cycle detection.
    /// Returns null if no reliable prediction can be made.
    /// Uses PREVIOUS player choice history, not current choice.
    /// </summary>
    private int? PredictPlayerLocationAndCatch(GameSession session)
    {
        if (!session.PreviousPlayerChoice.HasValue)
        {
            var safeFirst = session.AvailableLocations
                .Where(l => l != 1 && l != 10)
                .OrderByDescending(GetLocationSafetyScore)
                .Take(3).ToList();
            return safeFirst.Count > 0 ? safeFirst[_random.Next(safeFirst.Count)] : (int?)null;
        }

        if (_playerLocationHistory.Count < 2) return session.PreviousPlayerChoice.Value;

        int last = _playerLocationHistory[^1];
        int secondLast = _playerLocationHistory.Count >= 2 ? _playerLocationHistory[^2] : -1;
        bool playerAlternates = last != secondLast;
        int repeatConfidence = playerAlternates ? 30 : 55;
        if (session.AvailableLocations.Contains(last) && _random.Next(100) < repeatConfidence)
            return last;

        // Cycle detection (e.g. 1->5->1->5)
        if (_playerLocationHistory.Count >= 4)
        {
            var recent = _playerLocationHistory.TakeLast(4).ToList();
            if (recent[0] == recent[2] && recent[1] == recent[3] && recent[0] != recent[1])
            {
                int predicted = (recent[^1] == recent[0]) ? recent[1] : recent[0];
                if (session.AvailableLocations.Contains(predicted))
                    return predicted;
            }
        }

        // Weighted frequency
        if (_playerLocationHistory.Count >= 3)
        {
            var wCounts = new Dictionary<int, float>();
            for (int i = 0; i < _playerLocationHistory.Count; i++)
            {
                int roundsAgo = _playerLocationHistory.Count - 1 - i;
                float w = MathF.Exp(-PatternDecayFactor * roundsAgo);
                int l = _playerLocationHistory[i];
                if (!wCounts.ContainsKey(l)) wCounts[l] = 0f;
                wCounts[l] += w;
            }
            var topLoc = wCounts
                .Where(kv => session.AvailableLocations.Contains(kv.Key))
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault();
            if (topLoc.Key != 0) return topLoc.Key;
        }

        var safePredictions = session.AvailableLocations
            .Where(l => l != 10)
            .OrderByDescending(GetLocationSafetyScore)
            .Take(3).ToList();
        return safePredictions.Count > 0 ? safePredictions[_random.Next(safePredictions.Count)] : (int?)null;
    }

    /// <summary>
    /// TIER 2: When player is near victory (progress >= MaxPlayerProgress - 2), selects a high-escape-reward
    /// location to attack. Prioritises Beach(4), Desert(7), River(3) to deny player progress.
    /// IMPORTANT: When modifier is BeachAndWreckBlock or BlockPlayerProgress, avoids Beach(4) and Wreck(8)
    /// because targeting them negates the modifier's benefits.
    /// </summary>
    private int DetermineProgressBlockingLocation(GameSession session, CreatureModifier modifier)
    {
        var blockTargets = modifier == CreatureModifier.BeachAndWreckBlock || modifier == CreatureModifier.BlockPlayerProgress
            ? new[] { 7, 3, 1, 5 }  // Avoid Beach(4) & Wreck(8) for these modifiers
            : new[] { 4, 7, 3, 8 };

        var available = blockTargets.Where(l => session.AvailableLocations.Contains(l)).ToList();
        if (available.Count > 0) return available[0];

        if (session.CurrentPlayerChoice.HasValue && session.AvailableLocations.Contains(session.CurrentPlayerChoice.Value))
            return session.CurrentPlayerChoice.Value;

        return session.AvailableLocations.Count > 0
            ? session.AvailableLocations[_random.Next(session.AvailableLocations.Count)]
            : 1;
    }

    /// <summary>
    /// PHASE 2: Scores blocking candidates using (catch_rate * 0.4) + (location_value * 0.3) + (confidence * 0.3).
    /// Excellent blocks: Beach(4), Desert(7), River(3). Avoids Wreck(8).
    /// IMPORTANT: When modifier is BeachAndWreckBlock or BlockPlayerProgress, also avoids Beach(4) and Wreck(8)
    /// because the blocking location gains no strategic advantage if targeting those.
    /// </summary>
    private int? DetermineBlockingLocation(GameSession session)
    {
        var candidates = new List<int>(session.AvailableLocations);
        if (session.CurrentPlayerChoice.HasValue && !candidates.Contains(session.CurrentPlayerChoice.Value))
            candidates.Add(session.CurrentPlayerChoice.Value);

        // Filter out Beach(4) and Wreck(8) if current modifier requires avoiding them
        bool shouldAvoidBeachWreck = session.CurrentModifier == CreatureModifier.BeachAndWreckBlock || 
                                     session.CurrentModifier == CreatureModifier.BlockPlayerProgress;
        if (shouldAvoidBeachWreck)
            candidates = candidates.Where(l => l != 4 && l != 8).ToList();

        if (candidates.Count <= 1) return null;

        float bestScore = -1f;
        int bestLoc = candidates[0];

        foreach (var loc in candidates)
        {
            float catchRate = CalculateCatchRateForLocation(loc);
            float locValue = CalculateLocationStrategicValue(loc, session, session.CurrentModifier) / 100f;
            float confidence = _locationFrequency.TryGetValue(loc, out float f) ? Math.Min(f * 1.5f, 1f) : 0.3f;

            float penalty = loc == 8 ? 0.5f : 1f; // Never block Wreck � creature gets no benefit
            float bonus = loc is 4 or 7 or 3 ? 0.2f : 0f;

            float score = (catchRate * 0.4f + locValue * 0.3f + confidence * 0.3f + bonus) * penalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestLoc = loc;
            }
        }

        return bestLoc;
    }

    /// <summary>
    /// Main 7-tier decision hierarchy. Returns the best attack location and sets selectionLogic description.
    /// Called by SelectCreatureLocation after candidates are built.
    /// </summary>
    private int DetermineOptimalCreatureLocation(
        GameSession session, CreatureModifier currentModifier,
        List<int> candidateLocations, out string selectionLogic)
    {
        selectionLogic = "(❓ СЛУЧАЙНЫЙ ВЫБОР)";

        if (candidateLocations.Count == 0)
            return session.AvailableLocations.Count > 0 ? session.AvailableLocations[0] : 1;

        if (candidateLocations.Count == 1)
        {
            selectionLogic = "(единственная локация)";
            return candidateLocations[0];
        }

        // TIER 0: ABUSE DETECTION
        DetectPlayerLocationAbuse(session);
        if (_detectedExploitLocation.HasValue && candidateLocations.Contains(_detectedExploitLocation.Value))
        {
            float freq = _locationFrequency.TryGetValue(_detectedExploitLocation.Value, out float f) ? f : AbuseFrequencyThreshold;
            int attackProb = Math.Min((int)(freq * 100f), AbuseConfidenceMax);
            if (_random.Next(100) < attackProb)
            {
                selectionLogic = $"(⚠️ ЭКСПЛУАТАЦИЯ ОБНАРУЖЕНА: локация {_detectedExploitLocation} на {attackProb}% частоте)";
                return _detectedExploitLocation.Value;
            }
        }

        // TIER 1: WIN CONDITION
        if (session.CreatureProgress >= GameSession.MaxCreatureProgress - 1)
        {
            var catchLoc = PredictPlayerLocationAndCatch(session);
            if (catchLoc.HasValue && candidateLocations.Contains(catchLoc.Value))
            {
                selectionLogic = "(🎯 ПОБЕДНАЯ ПОЗИЦИЯ: агрессивный перехват)";
                return catchLoc.Value;
            }
        }

        // TIER 2: PLAYER THREAT
        if (session.PlayerProgress >= GameSession.MaxPlayerProgress - 2)
        {
            var blockLoc = DetermineProgressBlockingLocation(session, currentModifier);
            if (candidateLocations.Contains(blockLoc))
            {
                selectionLogic = "(🛡 ОБОРОННАЯ ПОЗИЦИЯ: блокировка прогресса)";
                return blockLoc;
            }
        }

        // TIER 3: STRATEGY MIXING
        double trapWeight = CalculateTrapStrategyWeight();
        double roll = _random.NextDouble();

        if (roll < trapWeight)
        {
            var trapLoc = SelectTrapLocationWithSynergy(session, currentModifier, candidateLocations);
            if (trapLoc.HasValue)
            {
                _trapAttempts++;
                _isUsingTrapStrategy = true;
                selectionLogic = $"(🪤 ЛОВУШКА - синергия модификатора {currentModifier})";
                return trapLoc.Value;
            }
        }

        // TIER 4: PATTERN PREDICTION
        {
            var predicted = PredictPlayerLocationAndCatch(session);
            if (predicted.HasValue && candidateLocations.Contains(predicted.Value))
            {
                _interceptAttempts++;
                _isUsingTrapStrategy = false;
                selectionLogic = $"(🔮 ПЕРЕХВАТ: предсказана локация {predicted.Value})";
                return predicted.Value;
            }
        }

        // TIER 5: LOCATION VALUE SCORING
        var scores = EvaluateAllLocations(session, currentModifier, candidateLocations);
        if (scores.Count > 0)
        {
            var best = scores.OrderByDescending(x => x.Value).First();
            selectionLogic = best.Value > 70
                ? "(\u2705 \u041e\u041f\u0422\u0418\u041c\u0410\u041b\u042c\u041d\u0410\u042f: \u0432\u044b\u0441\u043e\u043a\u0438\u0439 \u043f\u043e\u0442\u0435\u043d\u0446\u0438\u0430\u043b)"
                : best.Value > 40
                    ? "(\u2696\ufe0f \u0421\u0411\u0410\u041b\u0410\u041d\u0421\u0418\u0420\u041e\u0412\u0410\u041d\u041d\u0410\u042f)"
                    : "(\u2753 \u0421\u041b\u0423\u0427\u0410\u0419\u041d\u042b\u0419 \u0412\u042b\u0411\u041e\u0420)";
            return best.Key;
        }

        // TIER 6: FALLBACK
        selectionLogic = "(❓ СЛУЧАЙНЫЙ ВЫБОР)";
        return candidateLocations[_random.Next(candidateLocations.Count)];
    }

    // ===== HELPER CALCULATIONS =====

    /// <summary>
    /// Returns the historical catch rate for a location (0-1). Defaults to 0.4 with no history.
    /// Caps abused locations at AbuseConfidenceMax percentage.
    /// </summary>
    private float CalculateCatchRateForLocation(int location)
    {
        if (!_locationTotalAttempts.TryGetValue(location, out int attempts) || attempts == 0)
            return 0.4f;

        int catches = _locationCatchHistory.TryGetValue(location, out int c) ? c : 0;
        float rate = (float)catches / attempts;

        if (_detectedExploitLocation == location)
            rate = Math.Min(rate, AbuseConfidenceMax / 100f);

        return Math.Clamp(rate, 0f, 1f);
    }

    /// <summary>
    /// Returns the strategic value of a location (0-100 scale) for the creature given the current modifier.
    /// Higher = more desirable for the creature to target.
    /// </summary>
    private float CalculateLocationStrategicValue(int location, GameSession session, CreatureModifier modifier)
    {
        float baseValue = location switch
        {
            1  => 25f,  // Lair is a trap for the creature—shouldn't be default choice
            2  => 30f,
            3  => 45f,
            4  => 55f,
            5  => 20f,
            6  => 30f,
            7  => 50f,
            8  => 60f,
            9  => 35f,
            10 => 10f,
            _  => 25f
        };

        float modBonus = (location, modifier) switch
        {
            (1, CreatureModifier.DoubleDamage)          => 5f,
            (4, CreatureModifier.BeachAndWreckBlock)    => 15f,
            (8, CreatureModifier.BeachAndWreckBlock)    => 15f,
            (4, CreatureModifier.BlockPlayerProgress)   => 15f,
            (7, CreatureModifier.BlockPlayerProgress)   => 15f,
            (3, CreatureModifier.BlockPlayerProgress)   => 10f,
            (10, _) when modifier != CreatureModifier.None => -30f,
            _ => 0f
        };

        return Math.Clamp(baseValue + modBonus, 0f, 100f);
    }

    /// <summary>
    /// Returns a safety score for a location from the player's perspective.
    /// Higher = safer for the player, so creature may expect the player to go there.
    /// </summary>
    private int GetLocationSafetyScore(int location) => location switch
    {
        1  => 0,
        2  => 40,
        3  => 45,
        4  => 35,
        5  => 50,
        6  => 48,
        7  => 52,
        8  => 38,
        9  => 55,
        10 => 60,
        _  => 30
    };

    /// <summary>
    /// Returns game urgency (0-1): 1 = must act immediately, 0 = plenty of time.
    /// Driven by how close either side is to their respective win threshold.
    /// </summary>
    private float CalculateGameUrgency(GameSession session)
    {
        int playerSteps   = GameSession.MaxPlayerProgress  - session.PlayerProgress;
        int creatureSteps = GameSession.MaxCreatureProgress - session.CreatureProgress;
        float playerUrgency   = playerSteps  <= 2 ? 0.9f : playerSteps  <= 3 ? 0.7f : 0.3f;
        float creatureUrgency = creatureSteps <= 2 ? 0.9f : creatureSteps <= 3 ? 0.7f : 0.3f;
        return Math.Max(playerUrgency, creatureUrgency);
    }

    /// <summary>
    /// Returns the current trap strategy weight clamped to [0.30, 0.90].
    /// </summary>
    private double CalculateTrapStrategyWeight() =>
        Math.Clamp(_trapStrategyWeight, 0.30f, 0.90f);

    /// <summary>
    /// Updates trap/interception strategy weights every PatternUpdateFrequency rounds.
    /// Shifts 10% toward interception if trap success rate falls below 20% for 3+ attempts,
    /// or toward trap if interception under-performs. Otherwise drifts back to 60/40 baseline.
    /// </summary>
    private void UpdateStrategyWeights()
    {
        double trapRate      = _trapAttempts      > 0 ? (double)_trapSuccesses      / _trapAttempts      : 0.3;
        double interceptRate = _interceptAttempts > 0 ? (double)_interceptSuccesses / _interceptAttempts : 0.2;

        if (_trapAttempts >= StrategyUpdateThreshold && trapRate < 0.20)
            _trapStrategyWeight -= SoftTransitionRate;
        else if (_interceptAttempts >= StrategyUpdateThreshold && interceptRate < 0.20)
            _trapStrategyWeight += SoftTransitionRate;
        else
        {
            if (_trapStrategyWeight < 0.60f)
                _trapStrategyWeight += SoftTransitionRate * 0.5f;
            else if (_trapStrategyWeight > 0.60f)
                _trapStrategyWeight -= SoftTransitionRate * 0.5f;
        }

        _trapStrategyWeight = Math.Clamp(_trapStrategyWeight, 0.30f, 0.90f);
    }

    /// <summary>
    /// Returns a small bonus (0-15) based on the creature's current behavior preference.
    /// LairHunter loves location 1, RiverWatcher loves location 3, RoverPatroller loves location 5.
    /// This bonus keeps the creature personality consistent without making it predictable.
    /// </summary>
    private int GetBehaviorPreferenceBonus(int location)
    {
        int preferredLocation = _currentBehavior switch
        {
            CreatureBehavior.LairHunter => 1,
            CreatureBehavior.RiverWatcher => 3,
            CreatureBehavior.RoverPatroller => 5,
            _ => 0
        };

        return location == preferredLocation ? 12 : 0;
    }

    /// <summary>
    /// Switches creature behavior after 2 consecutive choices of the same location.
    /// Keeps the creature unpredictable and prevents location obsession from a behavior level.
    /// </summary>
    private void UpdateBehaviorPreference(int chosenLocation)
    {
        if (_lastTargetedLocation == chosenLocation)
        {
            _behaviorChoicesSinceSwitch++;
            if (_behaviorChoicesSinceSwitch >= 2)
            {
                // Switch to a different behavior
                var behaviors = new[] { CreatureBehavior.LairHunter, CreatureBehavior.RiverWatcher, CreatureBehavior.RoverPatroller };
                var otherBehaviors = behaviors.Where(b => b != _currentBehavior).ToArray();
                _currentBehavior = otherBehaviors[_random.Next(otherBehaviors.Length)];
                _behaviorChoicesSinceSwitch = 0;
            }
        }
        else
        {
            _behaviorChoicesSinceSwitch = 0;
        }
    }}