using NotAlone.Models;

namespace NotAlone.Services;

public class SurvivalService
{
    private readonly List<SurvivalCard> _cards = InitializeCards();

    private static List<SurvivalCard> InitializeCards()
    {
        return new List<SurvivalCard>
        {
            new SurvivalCard 
            { 
                Id = 1, 
                Name = "Heal", 
                Type = SurvivalCardType.Heal, 
                PlayablePhase = GamePhase.Selection 
            },
            new SurvivalCard 
            { 
                Id = 2, 
                Name = "Beacon", 
                Type = SurvivalCardType.Beacon, 
                PlayablePhase = GamePhase.Selection 
            },
            new SurvivalCard 
            { 
                Id = 3, 
                Name = "Regenerate 2 Locations", 
                Type = SurvivalCardType.LocationsRegen, 
                PlayablePhase = GamePhase.Selection 
            },
            new SurvivalCard 
            { 
                Id = 4, 
                Name = "Move Target", 
                Type = SurvivalCardType.MoveTarget, 
                PlayablePhase = GamePhase.Result 
            },
            new SurvivalCard 
            {
                Id = 5, 
                Name = "Fog", 
                Type = SurvivalCardType.Fog, 
                PlayablePhase = GamePhase.Selection 
            }
        };
    }

    public SurvivalCard? GetCardById(int id)
    {
        return _cards.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>
    /// Validates if target locations can be used for this card type.
    /// </summary>
    public (bool isValid, string message) ValidateTargetLocations(SurvivalCard card, GameSession session, List<int>? targetLocationIds)
    {
        if (card.Type == SurvivalCardType.LocationsRegen)
        {
            // If no target locations specified, that's fine — will restore whatever is available
            if (targetLocationIds == null || targetLocationIds.Count == 0)
                return (true, string.Empty);

            // If target locations are specified, validate them
            if (targetLocationIds.Count > 2)
                return (false, "❌ LocationsRegen can restore at most 2 locations.");

            // Check if all target locations exist in UsedLocations
            foreach (var locId in targetLocationIds)
            {
                if (!session.UsedLocations.Contains(locId))
                    return (false, $"❌ Location {locId} is not in used locations and cannot be restored.");
            }

            // Check for duplicates
            if (targetLocationIds.Distinct().Count() != targetLocationIds.Count)
                return (false, "❌ Target locations must be unique.");

            return (true, string.Empty);
        }

        // Other card types don't require target locations
        return (true, string.Empty);
    }

    public void ApplyEffect(GameSession session, SurvivalCard card, List<int>? targetLocationIds = null, CardDirection? direction = null)
    {
        if (session == null) return;

        switch (card.Type)
        {
            case SurvivalCardType.Heal:
                session.PlayerWillpower = Math.Min(session.PlayerWillpower + 1, 3);
                session.StatusMessage = $"[Card: Heal] 💪 You restored 1 willpower. Current: {session.PlayerWillpower}/3.";
                break;

            case SurvivalCardType.Beacon:
                session.IsBeaconLit = true;
                session.StatusMessage = $"[Card: Beacon] 🔥 Beacon at location 4 is lit! Will it attract rescue or the Creature?";
                break;

            case SurvivalCardType.LocationsRegen:
                ApplyLocationsRegenEffect(session, targetLocationIds);
                break;

            case SurvivalCardType.MoveTarget:
                ApplyMoveTargetEffect(session, direction);
                break;

            case SurvivalCardType.Fog:
                session.IsFogActive = true;
                session.StatusMessage = $"[Card: Fog] 🌫️ Fog activated! The Creature will choose from all locations.";
                break;
        }
    }

    private void ApplyMoveTargetEffect(GameSession session, CardDirection? direction)
    {
        if (session == null || session.CreatureChosenLocation == null || direction == null)
        {
            if (session != null)
                session.StatusMessage = "[Card: MoveTarget] ❌ Error: direction not specified or the Creature has not yet chosen a location.";
            return;
        }

        int currentLocation = session.CreatureChosenLocation.Value;
        int newLocation = currentLocation + (int)direction;

        // Validate bounds [1, 10]
        if (newLocation < 1 || newLocation > 10)
        {
            session.StatusMessage = $"[Card: MoveTarget] ❌ Location {newLocation} is out of bounds [1-10]. " +
                $"The Creature stays at {currentLocation}.";
            return;
        }

        session.CreatureChosenLocation = newLocation;
        string directionName = direction == CardDirection.Left ? "LEFT" : "RIGHT";
        session.StatusMessage = $"[Card: MoveTarget] 🎯 The Creature moved {directionName}: " +
            $"{currentLocation} → {newLocation}. This may save you in the Result phase!";
    }

    private void ApplyLocationsRegenEffect(GameSession session, List<int>? targetLocationIds)
    {
        var locationsToRestore = new List<int>();

        if (targetLocationIds != null && targetLocationIds.Count > 0)
        {
            // Restore specified locations (only those that exist in UsedLocations)
            locationsToRestore = [..targetLocationIds.Where(session.UsedLocations.Contains)];
        }
        else
        {
            // Restore up to 2 locations automatically from UsedLocations
            locationsToRestore = [..session.UsedLocations.Take(2)];
        }

        // Move to available
        var restoredLocations = new List<int>();
        foreach (var locId in locationsToRestore)
        {
            if (session.UsedLocations.Contains(locId))
            {
                session.UsedLocations.Remove(locId);
                if (!session.AvailableLocations.Contains(locId))
                    session.AvailableLocations.Add(locId);
                restoredLocations.Add(locId);
            }
        }

        if (restoredLocations.Count > 0)
        {
            var locationsStr = string.Join(", ", restoredLocations);
            session.StatusMessage = $"[Card: LocationsRegen] 🌱 Restored locations: {locationsStr}. " +
                $"Available ({session.AvailableLocations.Count}): {string.Join(", ", session.AvailableLocations)}";
        }
        else
        {
            session.StatusMessage = "[Card: LocationsRegen] ℹ️ No used locations available to restore.";
        }
    }
}
