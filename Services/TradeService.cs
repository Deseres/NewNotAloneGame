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
            session.StatusMessage = "You resisted with 1 willpower, but the Creature is getting stronger.";
        }
        else // givenWillpower == 2
        {
            session.PlayerWillpower -= 2;
            session.StatusMessage = "You resisted with 2 willpower, but the Creature is rapidly assimilating you.";
        }

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

        session.StatusMessage += " Chosen locations have been restored.";
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
            session.StatusMessage = "💀 КОНЕЦ ИГРЫ: Вы сдались. Существо вас ассимилировало. Вы поражены.";
        }
        else
        {
            session.StatusMessage = "You have given up. Regain all your cards.";
        }
    }
}
