using System.Linq;
using NotAlone.Models;

namespace NotAlone.Services;

/// <summary>
/// Priority-based creature AI with 6 pillars:
/// 1) Tier-1 locations (Rover, River, Lair, Wreck, Beach-if-beacon) get base priority
/// 2) Recently restored locations (+2 priority)
/// 3) Source gets +2 at 1 HP, +1 at 2 HP (willpower restore)
/// 4) Swamp/Jungle get +2 when player has <3 cards
/// 5) Blocking layer gets +2 if River Vision was used last round
/// 6) Rover/River/Wreck get +1 base for being extremely powerful
/// 
/// Additionally: Every 3 rounds, creature makes a completely random choice (chaos round).
/// Modifier awareness: avoids Beach(4)/Wreck(8) if BlockPlayerProgress or BeachAndWreckBlock are active.
/// Catch/escape history still matters via abuse detection (up to 40%+ frequency).
/// </summary>
public class CreatureLogic
{
    private readonly Random _random = Random.Shared;

    // ===== INTERNAL STATE =====
    private List<int> _playerLocationHistory = new();
    private List<int> _creatureLocationHistory = new();  // Tracks creature's own choices to prevent overuse
    private Dictionary<int, int> _locationCatchHistory = new();
    private Dictionary<int, int> _locationTotalAttempts = new();
    private Dictionary<int, float> _locationFrequency = new();
    private int? _detectedExploitLocation;
    private int _roundsSinceLastChaos = 0;

    // ===== CONSTANTS =====
    private const float AbuseFrequencyThreshold = 0.15f;  // Trigger exploitation sooner
    private const int AbuseConfidenceMax = 97;             // Attack exploit location very aggressively
    private const float PatternDecayFactor = 0.08f;        // Slow decay = long memory of player habits
    private const float AbusePenaltyDecay = 0.30f;
    private const int ChaosRoundFrequency = 8;             // Fewer random chaos rounds
    private const int MaxHistorySize = 8;                  // Longer player history window
    private const int CreatureHistorySize = 5;  // Track last 5 creature choices

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Main decision entry point. Assigns modifier, selects blocking location, and determines attack location.
    /// Uses priority-based system with 6 pillars. Every 3 rounds, ignores priorities and chooses randomly.
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
            // Weighted: DoubleDamage=5x, ExtraCreatureProgress=3x, BlockPlayerProgress=2x, others=1x
            var modifiers = new[]
            {
                CreatureModifier.DoubleDamage,          CreatureModifier.DoubleDamage,
                CreatureModifier.DoubleDamage,          CreatureModifier.DoubleDamage,
                CreatureModifier.DoubleDamage,
                CreatureModifier.ExtraCreatureProgress, CreatureModifier.ExtraCreatureProgress,
                CreatureModifier.ExtraCreatureProgress,
                CreatureModifier.BlockPlayerProgress,   CreatureModifier.BlockPlayerProgress,
                CreatureModifier.LoseRandomLocation
            };
            session.CurrentModifier = modifiers[_random.Next(modifiers.Length)];
        }

        // PHASE 2: Select blocking location (when modifier is active)
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
        var attackCandidates = new List<int>(session.AvailableLocations);
        if (session.CurrentPlayerChoice.HasValue && !attackCandidates.Contains(session.CurrentPlayerChoice.Value))
            attackCandidates.Add(session.CurrentPlayerChoice.Value);
        if (blockingLocation.HasValue)
            attackCandidates.Remove(blockingLocation.Value);

        if (attackCandidates.Count == 0)
            attackCandidates = new List<int>(session.AvailableLocations);
        
        // Safety check: if still empty, add all locations
        if (attackCandidates.Count == 0)
            attackCandidates = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Decide attack location
        int creatureChoice;
        string behaviourHint;

        // CHAOS ROUND: Every 3 rounds, ignore priorities and pick randomly
        _roundsSinceLastChaos++;
        if (_roundsSinceLastChaos >= ChaosRoundFrequency)
        {
            creatureChoice = attackCandidates[_random.Next(attackCandidates.Count)];
            behaviourHint = "The Creature moves unpredictably, defying all logic.";
            _roundsSinceLastChaos = 0;
        }
        else
        {
            // Normal decision: use priority system
            creatureChoice = DetermineLocationByPriority(session, attackCandidates, out behaviourHint);
        }

        session.CreatureChosenLocation = creatureChoice;
        session.PreviousCreatureChoice = creatureChoice;

        // Track creature's choice to prevent overuse patterns
        _creatureLocationHistory.Insert(0, creatureChoice);
        if (_creatureLocationHistory.Count > CreatureHistorySize)
            _creatureLocationHistory.RemoveAt(_creatureLocationHistory.Count - 1);

        var blockInfo = blockingLocation.HasValue
            ? $" Blocking location: {blockingLocation}."
            : string.Empty;

        session.StatusMessage =
            $"[CreatureTurn] ✓ The Creature has made its move. {behaviourHint} " +
            $"Attacking location: {creatureChoice}.{blockInfo} Modifier: {ModifierName(session.CurrentModifier)}.";
    }

    /// <summary>
    /// Records a successful catch at the given location for adaptive learning.
    /// Increments catch count for history tracking.
    /// </summary>
    public void RecordCatch(int location)
    {
        if (!_locationCatchHistory.ContainsKey(location))
            _locationCatchHistory[location] = 0;
        _locationCatchHistory[location]++;

        if (!_locationTotalAttempts.ContainsKey(location))
            _locationTotalAttempts[location] = 0;
        _locationTotalAttempts[location]++;
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
    }

    /// <summary>
    /// Resets all internal AI state for a new game session.
    /// </summary>
    public void ResetHistory()
    {
        _playerLocationHistory.Clear();
        _creatureLocationHistory.Clear();
        _locationCatchHistory.Clear();
        _locationTotalAttempts.Clear();
        _locationFrequency.Clear();
        _detectedExploitLocation = null;
        _roundsSinceLastChaos = 0;
    }

    // ===== PRIVATE DECISION ENGINES =====

    /// <summary>
    /// Detects player location abuse (40%+ frequency) using exponential decay weighting.
    /// Uses PREVIOUS player choices only, not current round's choice.
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
    /// Main priority-based location selector. Calculates priorities for each candidate location
    /// and picks the one with highest priority. On ties, picks randomly.
    /// </summary>
    private int DetermineLocationByPriority(GameSession session, List<int> candidates, out string behaviourHint)
    {
        if (candidates.Count == 0)
        {
            behaviourHint = "There is nowhere left to hide.";
            return session.AvailableLocations.Count > 0 ? session.AvailableLocations[0] : 1;
        }

        if (candidates.Count == 1)
        {
            behaviourHint = "The Creature is closing in on you.";
            return candidates[0];
        }

        // Detect player abuse patterns first
        DetectPlayerLocationAbuse(session);

        // Calculate priority for each candidate
        var priorities = new Dictionary<int, float>();
        foreach (var location in candidates)
        {
            float priority = CalculateLocationPriority(location, session);
            priorities[location] = priority;
        }

        // Check for abuse-detected location
        if (_detectedExploitLocation.HasValue && candidates.Contains(_detectedExploitLocation.Value))
        {
            float freq = _locationFrequency.TryGetValue(_detectedExploitLocation.Value, out float f) ? f : AbuseFrequencyThreshold;
            int attackProb = Math.Min((int)(freq * 100f), AbuseConfidenceMax);
            if (_random.Next(100) < attackProb)
            {
                behaviourHint = "The Creature is hunting a pattern — it has learned your habits.";
                return _detectedExploitLocation.Value;
            }
        }

        // Find best priority location(s)
        float bestPriority = priorities.Values.Max();
        var bestLocations = priorities.Where(kv => kv.Value == bestPriority).Select(kv => kv.Key).ToList();
        
        int chosen = bestLocations.Count > 1
            ? bestLocations[_random.Next(bestLocations.Count)]
            : bestLocations[0];

        behaviourHint = bestPriority > 15
            ? "The Creature stalks its most dangerous target."
            : bestPriority > 8
                ? "The Creature weighs its options carefully."
                : "The Creature wanders, uncertain of your trail.";

        return chosen;
    }

    /// <summary>
    /// Calculates priority for a single location based on 6 pillars:
    /// 1) Tier-1 locations (Rover, River, Lair, Wreck, Beach-if-beacon)
    /// 2) Recently restored locations (+2)
    /// 3) Source at low HP (+2 at 1 HP, +1 at 2 HP)
    /// 4) Swamp/Jungle when player has <3 cards (+2)
    /// 5) Blocking layer gets +2 if River Vision used
    /// 6) Rover/River/Wreck extremely powerful (+1 base)
    /// 
    /// Also considers catch history and avoids wasting modifiers.
    /// </summary>
    private float CalculateLocationPriority(int location, GameSession session)
    {
        float priority = 0f;

        // PILLAR 0: PLAYER HUNTING — fully adaptive, follows ACTUAL player behaviour
        // No hardcoded location bias — creature hunts wherever the real player goes.
        //
        // Early game (rounds 1-3, < 3 history entries): no frequency data yet — use
        // a light spread across all available locations so the creature probes broadly
        // instead of predictably camping the same spots.
        bool hasHistory = _playerLocationHistory.Count >= 3;

        if (_playerLocationHistory.Count >= 1 && _playerLocationHistory[^1] == location)
            priority += 35;  // Player was here last round — chase hard
        else if (_playerLocationHistory.Count >= 2 && _playerLocationHistory[^2] == location)
            priority += 20;  // Player was here 2 rounds ago
        else if (hasHistory && _playerLocationHistory[^3] == location)
            priority += 10;  // Player was here 3 rounds ago

        // Frequency signal — builds up over the game and captures player habits
        if (_locationFrequency.TryGetValue(location, out float playerFreq))
        {
            if (playerFreq >= 0.25f) priority += 40;       // Player loves this location
            else if (playerFreq >= 0.15f) priority += 22;  // Player visits often
            else if (playerFreq >= 0.08f) priority += 8;   // Player visits sometimes
            else priority -= 8;                            // Player almost never comes here
        }
        else if (_locationFrequency.Count > 3)
        {
            // History has built up but player has never visited this location
            priority -= 12;
        }

        // PILLAR 1: Tier-1 locations
        switch (location)
        {
            case 5:  // Rover - reduced base so player-hunting dominates
                priority += 3;
                break;
            case 3:  // River - reduced base
                priority += 3;
                break;
            case 1:  // Lair - reduced base
                priority += 3;
                break;
            case 8:  // Wreck - reduced base
                priority += 3;
                break;
            case 4:  // Beach - only if beacon lit
                if (session.IsBeaconLit)
                    priority += 5;
                else
                    priority += 1;
                break;
            default:
                priority += 0;
                break;
        }

        // PILLAR 2: Removed — player-hunting frequency signals supersede this

        // PILLAR 3: Source priority based on player willpower
        if (location == 9)
        {
            if (session.PlayerWillpower == 1)
                priority += 2;
            else if (session.PlayerWillpower == 2)
                priority += 1;
        }

        // PILLAR 4: Swamp/Jungle when player has few cards
        if (location is 2 or 6)
        {
            if (session.AvailableLocations.Count < 3)
                priority += 2;
        }

        // PILLAR 5: Blocking layer gets +2 if River Vision was used last round
        // (This applies to blocking location selection, handled separately)
        if (session.IsRiverVisionActive && session.IsRiverVisionRevealed)
        {
            // Blocking layer: +2 for high-value targets when player knows creature's move
            if (location is 3 or 8 or 4)
                priority += 2;
        }

        // EXPLOITATION FATIGUE: If creature has picked primary locations (1,3,5) too much recently,
        // boost secondary locations (2,4,6,7,8,9,10) to force strategic rotation.
        // This makes unpredictability emerge naturally from the system, not from randomness.
        int primaryLocationPicks = _creatureLocationHistory.Count(loc => loc is 1 or 3 or 5);
        if (primaryLocationPicks >= 5 && !(location is 1 or 3 or 5))
        {
            // Secondary location: boost priority to make it suddenly attractive
            priority += 4;
        }

        // ABUSE PREVENTION: Penalise locations creature used recently to prevent predictable
        // single-location camping. Applies to ALL locations so the creature rotates through
        // the player's favourite spots rather than sitting on just one.
        for (int i = 0; i < _creatureLocationHistory.Count; i++)
        {
            if (_creatureLocationHistory[i] == location)
            {
                int roundsAgo = i + 1;
                float penalty = roundsAgo switch
                {
                    1 => -18f,   // Last round: heavy penalty — rotate to player's next favourite
                    2 => -7f,    // 2 rounds ago: meaningful penalty
                    3 => -2f,    // 3 rounds ago: light penalty
                    _ => 0f
                };
                priority += penalty;
                break;
            }
        }

        // Catch history: boost locations where creature has caught player before
        if (_locationCatchHistory.TryGetValue(location, out int catches) && catches > 0)
            priority += catches * 2;

        // Avoid locations where creature has escaped too often (player is good at defending)
        if (_locationTotalAttempts.TryGetValue(location, out int totalAttempts))
        {
            int catchCount = _locationCatchHistory.TryGetValue(location, out int c) ? c : 0;
            float successRate = totalAttempts > 0 ? (float)catchCount / totalAttempts : 0f;
            if (successRate < 0.2f && totalAttempts >= 3)
                priority -= 3;  // Penalize locations where player always escapes
        }

        // MODIFIER AWARENESS: Avoid Beach/Wreck if modifier would be wasted
        if ((session.CurrentModifier == CreatureModifier.BeachAndWreckBlock ||
             session.CurrentModifier == CreatureModifier.BlockPlayerProgress) &&
            (location == 4 || location == 8))
        {
            priority = 0;  // Don't target these locations when modifier doesn't help
        }

        // Artifact disables modifier: avoid relying on it
        if (session.IsArtefactActive && location is 4 or 8)
            priority -= 2;

        return Math.Max(0f, priority);
    }

    /// <summary>
    /// Selects the best blocking location based on player-value denial.
    /// Uses a SEPARATE formula from attack priority to avoid removing the creature's
    /// top attack targets from the candidate pool.
    /// Targets locations that give the player progress/resources — NOT the creature's hunt targets.
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

        // Identify creature's top attack target (to avoid blocking it)
        var topAttackLocation = _playerLocationHistory.Count >= 1 ? _playerLocationHistory[^1] : (int?)null;

        float bestBlockValue = -1f;
        int? bestLoc = null;

        foreach (var loc in candidates)
        {
            // Skip the creature's top attack target so it stays in the attack pool
            if (loc == topAttackLocation) continue;

            // Player-value denial scoring (completely separate from attack priority)
            float blockValue = loc switch
            {
                4 => session.IsBeaconLit ? 10f : 3f,  // Beach: deny progress if beacon lit
                8 => 7f,                                // Wreck: deny progress
                3 => 6f,                                // River: deny vision
                5 => 5f,                                // Rover: deny unlock
                7 => 5f,                                // Shelter: deny survival card
                9 => 4f,                                // Source: deny willpower restore
                10 => 3f,                               // Artefact: deny artefact
                2 => 2f,                                // Jungle: deny restore
                6 => 2f,                                // Swamp: deny restore
                _ => 1f
            };

            if (blockValue > bestBlockValue)
            {
                bestBlockValue = blockValue;
                bestLoc = loc;
            }
        }

        // Fallback: if all candidates are the top attack target, don't block
        return bestLoc;
    }

    // ===== HELPER CALCULATIONS =====

    /// <summary>
    /// (Placeholder for future enhancement)
    /// Could track recently restored locations explicitly for Pillar 2.
    /// For now, priority system handles this through catch history.
    /// </summary>
    private bool WasRecentlyRestored(int location, GameSession session)
    {
        // TODO: Could track restoration events if needed
        return false;
    }

    /// <summary>
    /// Returns a human-readable display name for a creature modifier.
    /// </summary>
    private static string ModifierName(CreatureModifier modifier) => modifier switch
    {
        CreatureModifier.None                => "None",
        CreatureModifier.DoubleDamage        => "Double Damage",
        CreatureModifier.BlockPlayerProgress => "Block Progress",
        CreatureModifier.LoseRandomLocation  => "Lose Location",
        CreatureModifier.BeachAndWreckBlock  => "Beach & Wreck Block",
        CreatureModifier.ExtraCreatureProgress => "Extra Creature Progress",
        _                                    => modifier.ToString()
    };
}
