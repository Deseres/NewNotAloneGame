---
description: 'Guidelines for NotAlone Game backend development'
applyTo: '**/*.cs'
---

# NotAlone Game - AI Development Guide

## Project Overview

**NotAlone** is a .NET 10 strategic survival game with creature AI mechanics. Players choose locations to survive while a creature with strategic AI tries to catch them. Key differentiators:
- **Creature Second Phase**: When creature progress ≥ 4, creature blocks locations (negates effects) FIRST, then attacks SECOND from remaining locations
- **Creature Modifiers**: Six special modifiers that activate randomly (DoubleDamage, BlockPlayerProgress, LoseRandomLocation, BeachAndWreckBlock, ExtraCreatureProgress)
- **Card Effects**: Survival cards with location restoration, location duplication, and transformation effects

## Critical Architecture Patterns

### 1. Game State Machine & Three-Phase Turn Structure
**Files**: Services/GameEngine.cs, Models/GameSession.cs

Each round flows: **Selection** → **CreatureTurn** → **Result** → *next Selection*

- **Selection**: Player chooses location (1-10) from `AvailableLocations`
- **CreatureTurn**: Creature AI selects location. If `PlayerProgress >= 4`, creature now selects TWO locations:
  1. `CreatureBlockingLocation` - chosen FIRST from `AvailableLocations + LastPlayerChoice`, negates player location effects if modifier active
  2. `CreatureChosenLocation` - chosen SECOND from `AvailableLocations + LastPlayerChoice - CreatureBlockingLocation`, determines if player is caught
- **Result**: Apply location effects, modifiers, catch logic. Call `ResolveRound()` before advancing

**Critical**: Always check `session.CurrentPhase` before allowing player actions. Blocking only applies when `CurrentModifier != None`.

### 2. Creature AI Decision Logic with Multi-Strategy
**File**: Services/CreatureLogic.cs

Creature uses **strategic mixing** of 3 approaches:
1. **Trap Strategy**: Blocks beaches/wrecks (1,3,8) with `BeachAndWreckBlock` modifier to steal escape benefits
2. **Interception Strategy**: Predicts player location choice and picks same location to catch
3. **Exploitation Detection**: Monitors player location history - if player exploits same location 3+ times in 5 rounds, makes it primary target

Key method: `DetermineOptimalCreatureLocation(session, currentModifier, candidateLocations)` - scores all locations in `candidateLocations` list, NOT all 10 locations.

**Important**: When adding location evaluation logic, always use the `candidateLocations` parameter passed in, not `session.AvailableLocations` directly.

### 3. Creature Modifier System - Location-Specific Application
**File**: Services/GameEngine.cs (lines ~165-200)

Modifiers ONLY apply to `CreatureChosenLocation` (the attack location), NOT to `CreatureBlockingLocation`:
- **DoubleDamage**: Player loses 2 willpower instead of 1 when caught
- **BlockPlayerProgress**: Escape doesn't increase player progress (if not caught)
- **LoseRandomLocation**: Player loses random available location when caught
- **BeachAndWreckBlock**: Blocks Beach(4) and Wreck(8) location effects for player (both catch and escape)
- **ExtraCreatureProgress**: Creature gains +2 progress instead of +1 when catching player
- **None**: Creature gets 0 progress bonus (creature still catches, but minimal advancement)

**Critical Design Rule**: `CurrentModifier == None` has special meaning - it disables Artefact protection AND blocks the blocking location negation effect.

### 4. Location Effects & Card Interaction
**File**: Services/GameEngine.cs (lines ~250-420)

Location special effects only trigger if:
1. Player chooses that location (playerChoice == location ID)
2. Location is NOT blocked: `!isLocationBlocked` (blocking negates special effects)
3. Player survives (not caught by creature)

Example locations:
- **Forest(2)**: Restore one random used location to available
- **Beach(4)**: Gain progress bonus on escape (disabled by BeachAndWreckBlock modifier)
- **Wreck(8)**: Lose location penalty on catch (disabled by BeachAndWreckBlock modifier)

## Critical Implementation Details

### Session State Properties
- `AvailableLocations`: Pool player can choose from each turn (1-10)
- `UsedLocations`: Locations already used, must be "restored" to available before use again
- `CreatureChosenLocation`: Creature's attack location (deferred comparison happens in ResolveRound)
- `CreatureBlockingLocation`: Creature's blocking location (only when progress >= 4 AND modifier active)
- `LastPlayerChoice` / `LastCreatureChoice`: Previous turn choices, used for strategy tracking
- `CurrentModifier`: Active modifier this round, affects damage/effects calculations
- `CurrentPhase`: Enforced state machine - prevents invalid action sequences

### When Adding Game Logic

1. **Check phase first**: `if (session.CurrentPhase != GamePhase.ExpectedPhase) return BadRequest(...)`
2. **Preserve existing location data**: Don't mutate `AvailableLocations` list directly in loops; use `.ToList()` or `.Where()`
3. **Deferred creature choice**: Never compare creature vs player location in `SelectCreatureLocation()` - only in `ResolveRound()`
4. **Blocking logic**: Always verify `session.CreatureBlockingLocation.HasValue && session.CurrentModifier != CreatureModifier.None` before applying block effects
5. **Progress threshold**: Use `if (session.PlayerProgress >= 4)` to enable second-phase mechanics

## Build & Testing Workflow

### Build Commands
```bash
dotnet build                    # Debug build
dotnet build -c Release         # Release build
dotnet run                      # Run API on localhost:5000
dotnet test                     # Run all tests in NotAlone.Tests/
```

### Key Test Files
- **NotAlone.Tests/GameEngineModifierTests.cs**: Tests each modifier's behavior with catch/escape scenarios
- Test pattern: Create `GameSession` with `CreateTestSession()` helper, configure modifier, call `engine.ApplyCreatureModifier()` or `engine.ResolveRound()`
- Tests verify damage, progress, willpower, and status messages

### API Endpoints
- `POST /api/game/start` - Create new session
- `POST /api/game/{id}/play` - Player chooses location (Selection phase)
- `POST /api/game/{id}/creature-turn` - Creature AI selects (CreatureTurn phase)
- `POST /api/game/{id}/next-round` - Resolve effects & advance (Result → Selection)

## Code Patterns Specific to NotAlone

### Mutation Patterns
- Always `.ToList()` when creating modified location lists to avoid list mutation during iteration
- Use `session.UsedLocations.Remove(loc)` and `session.AvailableLocations.Add(loc)` atomically
- Never reassign `AvailableLocations` directly; modify in place

### Status Message Convention
- All game events logged to `session.StatusMessage` with prefix: `[MethodName]` or `[PhaseName]`
- Examples: `"[CreatureTurn] ✓ Существо выбрало локацию 5 (High Value). Модификатор: DoubleDamage."`
- Used by frontend to display game progression to player

### Nullable Reference Types
- Creature locations: `int?` (nullable - creature might not select if no locations available)
- `session.LastPlayerChoice.HasValue` before accessing `.Value`
- `session.CreatureBlockingLocation.HasValue` before comparing locations

## C# Code Standards (Inherited)

- C# 14 features, file-scoped namespaces, `is null` checks
- XML doc comments on public methods explaining creature behavior impact
- No "Arrange/Act/Assert" comments in tests; copy style from existing `GameEngineModifierTests.cs`
- PascalCase for public, camelCase for private fields