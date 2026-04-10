using System.Linq;
using NotAlone.Models;

namespace NotAlone.Services;

public class GameEngine
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
        session.PlayerWillpower = 3;
        session.UsedLocations.Clear();
        session.AvailableLocations = new List<int> { 1, 2, 3, 4, 5 };
        session.CreatureProgress ++;
        session.StatusMessage = "You have given up. Regain all your cards.";
    }
	public void PlayRound(GameSession session, int playerLocation)
	{
		// Only allow player to pick from available locations
		if (!session.AvailableLocations.Contains(playerLocation))
		{
			session.StatusMessage = $"Invalid move. Location {playerLocation} is not available.";
			return;
		}

		// Creature chooses from all possible locations
		var creatureIdx = Random.Shared.Next(session.AvailableLocations.Count);
		var creatureChoice = session.AvailableLocations[creatureIdx];
		session.LastCreatureChoice = creatureChoice;

		// 2. Resolution Logic
		if (playerLocation == creatureChoice)
		{
			session.PlayerWillpower--;
			session.CreatureProgress++;
			session.StatusMessage = $"Caught! The Creature was at {creatureChoice}.";

			// If willpower drops to zero or below, immediately give up
			if (session.PlayerWillpower <= 0)
			{
				GiveUp(session);
				return;
			}
		}
		else
		{
			session.PlayerProgress++;
			session.StatusMessage = $"Safe. You visited {playerLocation}, Creature was at {creatureChoice}.";
		}

		// Move played location from available to used for player only
		session.AvailableLocations.Remove(playerLocation);
		session.UsedLocations.Add(playerLocation);

		// 3. Victory Check
		if (session.CreatureProgress >= GameSession.MaxProgress)
		{
			session.IsGameOver = true;
			session.StatusMessage = "The Creature has assimilated you.";
		}
		else if (session.PlayerProgress >= GameSession.MaxProgress)
		{
			session.IsGameOver = true;
			session.StatusMessage = "Rescue has arrived! You escaped Artemia.";
		}
	}
    public void ResolveRound(GameSession session)
    {
        // This method can be expanded for more complex logic, such as special events or power-ups.
    }
}
