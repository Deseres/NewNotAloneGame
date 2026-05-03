using NotAlone.Models;

namespace NotAlone.Services;

public class TradeService
{
    public void Resist(GameSession session, int givenWillpower, int[] chosenLocations)
    {
        if (session == null)
            return;

        var maxAllowed = givenWillpower == 2 ? 4 : 2;
        if (chosenLocations == null || chosenLocations.Length < 1 || chosenLocations.Length > maxAllowed)
        {
            session.StatusMessage = $"You must choose between 1 and {maxAllowed} locations to restore.";
            return;
        }

        if (givenWillpower < 1 || givenWillpower > 2)
        {
            session.StatusMessage = "Invalid willpower choice. Please choose between 1 and 3.";
            return;
        }

        // apply willpower cost
        if (givenWillpower == 1)
        {
            session.PlayerWillpower--;
            session.StatusMessage = $"[Selection] You restored {chosenLocations.Length} location(s) by giving 1 willpower (remaining: {session.PlayerWillpower}/{GameSession.MaxWillpower}).";        }
        else // givenWillpower == 2
        {
            session.PlayerWillpower -= 2;
            session.StatusMessage = $"[Selection] You restored {chosenLocations.Length} location(s) by giving 2 willpower (remaining: {session.PlayerWillpower}/{GameSession.MaxWillpower}).";        }

        // If willpower drops to zero or below, immediately give up
        if (session.PlayerWillpower <= 0)
        {
            GiveUp(session);
            return;
        }

        // restore the chosen locations
        foreach (var loc in chosenLocations)
        {
            session.UsedLocations.Remove(loc);
            if (!session.AvailableLocations.Contains(loc))
                session.AvailableLocations.Add(loc);
        }
    }

    public void GiveUp(GameSession session)
    {
        if (session == null)
            return;

        session.PlayerWillpower = 3;
        
        // Restore all used locations to available
        foreach (var location in session.UsedLocations)
        {
            if (!session.AvailableLocations.Contains(location))
            {
                session.AvailableLocations.Add(location);
            }
        }
        
        session.UsedLocations.Clear();
        session.CreatureProgress++;
        
        // Check if creature reached victory immediately
        if (session.CreatureProgress >= GameSession.MaxCreatureProgress)
        {
            session.IsGameOver = true;
            session.StatusMessage = "💀 GAME OVER: You surrendered. The Creature has assimilated you. Defeat.";
        }
        else
        {
            session.StatusMessage = "[Selection] You surrendered. All your used locations have been restored. The Creature advances.";
        }
    }
}
