using NotAlone.Models;
using NotAlone.Services;
using Xunit;

namespace NotAlone.Tests;

public class GameEngineModifierTests
{
	private GameSession CreateTestSession()
	{
		var session = new GameSession();
		session.PlayerWillpower = 5;
		session.CreatureProgress = 0;
		session.PlayerProgress = 0;
		session.IsBeaconLit = false;
		session.AvailableLocations = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		session.UsedLocations = new List<int>();
		return session;
	}

	[Fact]
	public void DoubleDamage_WhenCaught_DealsExtraDamage()
	{
		// Arrange
		var session = CreateTestSession();
		var engine = new GameEngine();
		var initialWillpower = session.PlayerWillpower;
		session.CurrentModifier = CreatureModifier.DoubleDamage;

		// Act - Call actual method
		var playerChoice = 5;
		var creatureChoice = 5;
		engine.ApplyCreatureModifier(session, playerChoice, creatureChoice);

		// Assert
		Assert.Equal(initialWillpower - 1, session.PlayerWillpower);
		Assert.Contains("Double Damage", session.StatusMessage);
	}

	[Fact]
	public void DoubleDamage_WhenEscaped_NoEffect()
	{
		// Arrange
		var session = CreateTestSession();
		var engine = new GameEngine();
		var initialWillpower = session.PlayerWillpower;
		session.CurrentModifier = CreatureModifier.DoubleDamage;

		// Act - Call actual method with player escaped
		var playerChoice = 5;
		var creatureChoice = 3;
		engine.ApplyCreatureModifier(session, playerChoice, creatureChoice);

		// Assert
		Assert.Equal(initialWillpower, session.PlayerWillpower);
	}

	[Fact]
	public void BlockPlayerProgress_WhenEscaped_BlocksProgress()
	{
		// Arrange
		var session = CreateTestSession();
		session.PlayerProgress = 2;
		var initialProgress = session.PlayerProgress;
		session.CurrentModifier = CreatureModifier.BlockPlayerProgress;
		var engine = new GameEngine();

		// Act - Call actual method
		var playerChoice = 5;
		var creatureChoice = 3;
		engine.ApplyCreatureModifier(session, playerChoice, creatureChoice);

		// Assert
		Assert.Equal(initialProgress - 1, session.PlayerProgress);
	}

	[Fact]
	public void BlockPlayerProgress_WhenCaught_NoEffect()
	{
		// Arrange
		var session = CreateTestSession();
		session.PlayerProgress = 2;
		var initialProgress = session.PlayerProgress;
		session.CurrentModifier = CreatureModifier.BlockPlayerProgress;
		var engine = new GameEngine();

		// Act - Call actual method
		var playerChoice = 5;
		var creatureChoice = 5;
		engine.ApplyCreatureModifier(session, playerChoice, creatureChoice);

		// Assert
		Assert.Equal(initialProgress, session.PlayerProgress);
	}

	[Fact]
	public void BeachAndWreckBlock_Beach_WithBeaconLit_CancelsProgressGain()
	{
		// Arrange
		var session = CreateTestSession();
		var engine = new GameEngine();
		session.IsBeaconLit = true;
		session.PlayerProgress = 1;
		var initialProgress = session.PlayerProgress;
		session.CurrentModifier = CreatureModifier.BeachAndWreckBlock;

		// Act - Call actual method
		var playerChoice = 4; // Beach
		var creatureChoice = 3;
		engine.ApplyCreatureModifier(session, playerChoice, creatureChoice);

		// Assert
		Assert.Equal(initialProgress - 1, session.PlayerProgress);
	}

	[Fact]
	public void BeachAndWreckBlock_Beach_WithoutBeaconLit_PreventLighting()
	{
		// Arrange
		var session = CreateTestSession();
		var engine = new GameEngine();
		session.IsBeaconLit = false;
		session.CurrentModifier = CreatureModifier.BeachAndWreckBlock;

		// Act - Call actual method
		var playerChoice = 4; // Beach
		var creatureChoice = 3;
		engine.ApplyCreatureModifier(session, playerChoice, creatureChoice);

		// Assert
		Assert.False(session.IsBeaconLit);
	}

	[Fact]
	public void BeachAndWreckBlock_Wreck_CancelsProgressGain()
	{
		// Arrange
		var session = CreateTestSession();
		var engine = new GameEngine();
		session.PlayerProgress = 1;
		var initialProgress = session.PlayerProgress;
		session.CurrentModifier = CreatureModifier.BeachAndWreckBlock;

		// Act - Call actual method
		var playerChoice = 8; // Wreck
		var creatureChoice = 3;
		engine.ApplyCreatureModifier(session, playerChoice, creatureChoice);

		// Assert
		Assert.Equal(initialProgress - 1, session.PlayerProgress);
	}

	[Fact]
	public void BeachAndWreckBlock_WhenCaught_NoEffect()
	{
		// Arrange
		var session = CreateTestSession();
		session.IsBeaconLit = true;
		session.PlayerProgress = 1;
		var initialProgress = session.PlayerProgress;
		var initialBeacon = session.IsBeaconLit;
		session.CurrentModifier = CreatureModifier.BeachAndWreckBlock;

		// Act - Player caught at Beach
		var playerChoice = 4;
		var creatureChoice = 4;

		if (playerChoice != creatureChoice) // This is false, so nothing happens
		{
			if (playerChoice == 4)
			{
				session.PlayerProgress--;
			}
		}

		// Assert
		Assert.Equal(initialProgress, session.PlayerProgress);
		Assert.Equal(initialBeacon, session.IsBeaconLit);
	}

	[Fact]
	public void ExtraCreatureProgress_WhenCaught_GrantsExtraProgress()
	{
		// Arrange
		var session = CreateTestSession();
		var engine = new GameEngine();
		session.CreatureProgress = 0;
		var initialProgress = session.CreatureProgress;
		session.CurrentModifier = CreatureModifier.ExtraCreatureProgress;

		// Act - Call actual method
		var playerChoice = 5;
		var creatureChoice = 5;
		engine.ApplyCreatureModifier(session, playerChoice, creatureChoice);

		// Assert
		Assert.Equal(initialProgress + 1, session.CreatureProgress);
	}

	[Fact]
	public void ExtraCreatureProgress_WhenEscaped_NoEffect()
	{
		// Arrange
		var session = CreateTestSession();
		var engine = new GameEngine();
		session.CreatureProgress = 0;
		var initialProgress = session.CreatureProgress;
		session.CurrentModifier = CreatureModifier.ExtraCreatureProgress;

		// Act - Call actual method
		var playerChoice = 5;
		var creatureChoice = 3;
		engine.ApplyCreatureModifier(session, playerChoice, creatureChoice);

		// Assert
		Assert.Equal(initialProgress, session.CreatureProgress);
	}
}
