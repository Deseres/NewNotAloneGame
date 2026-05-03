using NotAlone.Models;
using NotAlone.Services;
using NotAlone.Tests.Helpers;
using Xunit;

namespace NotAlone.Tests;

/// <summary>
/// Simulates 10 complete games to observe creature AI behavior and unpredictability.
/// </summary>
public class GameSimulator
{
    [Fact]
    public void Simulate10Games()
    {
        var sb = new System.Text.StringBuilder();
        var engine = new GameEngine();
        var creatureLogic = new CreatureLogic();
        var random = Random.Shared;

        var results = new List<(int gameNum, string outcome, int playerProgress, int creatureProgress, int rounds, List<int> creatureChoices)>();

        for (int gameNum = 1; gameNum <= 10; gameNum++)
        {
            creatureLogic.ResetHistory();
            var session = TestSessionFactory.Create();
            session.CurrentPhase = GamePhase.Selection;

            var creatureChoices = new List<int>();
            int rounds = 0;

            while (!session.IsGameOver && rounds < 50)  // Safety limit
            {
                rounds++;

                // === SELECTION PHASE ===
                // Player picks a somewhat smart location (bias toward available ones, avoid extremes)
                int playerChoice;
                if (session.AvailableLocations.Count > 0)
                {
                    // 70% chance to pick a "safe" location (2, 6, 7), 30% chance random
                    var safeChoices = session.AvailableLocations.Where(l => l is 2 or 6 or 7).ToList();
                    if (safeChoices.Count > 0 && random.NextDouble() < 0.70)
                        playerChoice = safeChoices[random.Next(safeChoices.Count)];
                    else
                        playerChoice = session.AvailableLocations[random.Next(session.AvailableLocations.Count)];
                }
                else
                {
                    playerChoice = random.Next(1, 11);
                }

                engine.PlayRound(session, playerChoice);
                session.CurrentPhase = GamePhase.CreatureTurn;

                // === CREATURE TURN PHASE ===
                creatureLogic.SelectCreatureLocation(session);
                creatureChoices.Add(session.CreatureChosenLocation ?? 0);
                session.CurrentPhase = GamePhase.Result;

                // === RESOLVE ROUND ===
                engine.ResolveRound(session);

                // Record for creature learning
                if (session.CurrentPlayerChoice.HasValue && session.CreatureChosenLocation.HasValue)
                {
                    if (session.CurrentPlayerChoice.Value == session.CreatureChosenLocation.Value)
                    {
                        creatureLogic.RecordCatch(session.CreatureChosenLocation.Value);
                    }
                    else
                    {
                        creatureLogic.RecordEscape(session.CreatureChosenLocation.Value);
                    }
                }

                // Update previous choices for next round
                session.PreviousPlayerChoice = session.CurrentPlayerChoice;
                session.CurrentPlayerChoice = null;

                // Check win conditions
                if (session.PlayerProgress >= GameSession.MaxPlayerProgress)
                {
                    session.IsGameOver = true;
                }
                else if (session.CreatureProgress >= GameSession.MaxCreatureProgress)
                {
                    session.IsGameOver = true;
                }
                else if (session.PlayerWillpower <= 0)
                {
                    session.IsGameOver = true;
                }

                session.CurrentPhase = GamePhase.Selection;
            }

            // Determine outcome
            string outcome;
            if (session.PlayerProgress >= GameSession.MaxPlayerProgress)
                outcome = "PLAYER WIN";
            else if (session.CreatureProgress >= GameSession.MaxCreatureProgress || session.PlayerWillpower <= 0)
                outcome = "CREATURE WIN";
            else
                outcome = "TIMEOUT";

            results.Add((gameNum, outcome, session.PlayerProgress, session.CreatureProgress, rounds, creatureChoices));
        }

        // Print results
        sb.AppendLine("\n" + "=".PadRight(100, '='));
        sb.AppendLine("GAME SIMULATION RESULTS - 10 GAMES");
        sb.AppendLine("=".PadRight(100, '=') + "\n");

        int playerWins = 0, creatureWins = 0;
        var creatureLocationFrequency = new Dictionary<int, int>();
        var creatureLocationChoiceOrder = new List<string>();

        foreach (var (gameNum, outcome, playerProgress, creatureProgress, rounds, creatureChoices) in results)
        {
            if (outcome == "PLAYER WIN") playerWins++;
            else if (outcome == "CREATURE WIN") creatureWins++;

            sb.AppendLine($"Game {gameNum}: {outcome,-15} | Rounds: {rounds,2} | P:{playerProgress}/7 C:{creatureProgress}/5");
            sb.AppendLine($"  Creature choices: {string.Join(", ", creatureChoices)}");

            // Track frequency
            foreach (var choice in creatureChoices)
            {
                if (!creatureLocationFrequency.ContainsKey(choice))
                    creatureLocationFrequency[choice] = 0;
                creatureLocationFrequency[choice]++;
            }

            sb.AppendLine();
        }

        // Summary stats
        sb.AppendLine("=".PadRight(100, '='));
        sb.AppendLine("SUMMARY STATISTICS");
        sb.AppendLine("=".PadRight(100, '='));
        sb.AppendLine($"Player Wins: {playerWins}/10");
        sb.AppendLine($"Creature Wins: {creatureWins}/10");
        sb.AppendLine($"Win Rate: {(creatureWins / 10.0 * 100):F1}% creature, {(playerWins / 10.0 * 100):F1}% player\n");

        sb.AppendLine("Creature Location Selection Frequency:");
        var sorted = creatureLocationFrequency.OrderByDescending(kv => kv.Value);
        foreach (var (loc, count) in sorted)
        {
            double pct = (count / (double)creatureLocationFrequency.Values.Sum()) * 100;
            sb.AppendLine($"  Location {loc,2}: {count,3} times ({pct:F1}%)");
        }

        sb.AppendLine("\nPrimary Locations (1,3,5) vs Secondary (2,4,6,7,8,9,10):");
        int primaryTotal = creatureLocationFrequency.Where(kv => kv.Key is 1 or 3 or 5).Sum(kv => kv.Value);
        int secondaryTotal = creatureLocationFrequency.Where(kv => kv.Key is 2 or 4 or 6 or 7 or 8 or 9 or 10).Sum(kv => kv.Value);
        sb.AppendLine($"  Primary (1,3,5):     {primaryTotal} attacks ({(primaryTotal / (double)(primaryTotal + secondaryTotal) * 100):F1}%)");
        sb.AppendLine($"  Secondary (others):  {secondaryTotal} attacks ({(secondaryTotal / (double)(primaryTotal + secondaryTotal) * 100):F1}%)");

        sb.AppendLine("\n" + "=".PadRight(100, '=') + "\n");

        string output = sb.ToString();
        try
        {
            string testDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
            string filePath = System.IO.Path.Combine(testDir, "simulation_results.txt");
            System.IO.File.WriteAllText(filePath, output);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error writing file: {ex.Message}");
        }
        System.Console.WriteLine(output);  // Also print to console so xUnit captures it
        Xunit.Assert.True(true);  // Pass the test
    }
}
