---
description: 'Guidelines for NotAlone Game backend development'
applyTo: '**/*.cs'
---

# NotAlone Game - AI Development Guide

## Project Overview

**NotAlone** is a .NET 10 strategic survival game. Players (progress target: 7) choose locations to survive while a creature (progress target: 5) with strategic AI tries to catch them. Stack: ASP.NET Core Web API + EF Core (SQLite/SQL Server) + React/TypeScript frontend served as SPA from `wwwroot/`.

Win/loss: `PlayerProgress >= MaxPlayerProgress (7)` = player wins; `CreatureProgress >= MaxCreatureProgress (5)` OR `PlayerWillpower <= 0` = creature wins.

## Three-Phase Turn State Machine

**Files**: [Services/GameEngine.cs](../Services/GameEngine.cs), [Models/GameSession.cs](../Models/GameSession.cs)

`Selection → CreatureTurn → Result → (next) Selection`

| Phase | API endpoint | What happens |
|---|---|---|
| Selection | `POST /api/game/{id}/play` | Player picks location from `AvailableLocations`; location moved to `UsedLocations`; `CurrentPlayerChoice` set |
| CreatureTurn | `POST /api/game/{id}/creature-turn` | `CreatureLogic` sets `CreatureChosenLocation` (and `CreatureBlockingLocation` when `PlayerProgress >= 4`) |
| Result | `POST /api/game/{id}/next-round` | `ResolveRound()` runs; effects applied; `PreviousPlayerChoice` and `PreviousCreatureChoice` updated for next round's creature learning; history saved if game over |

**Always guard with phase checks**: `if (session.CurrentPhase != GamePhase.Selection) return BadRequest(...)`.

## Location Map

`AvailableLocations` starts as `[1,2,3,4,5]` (NOT all 10). Locations 6-10 unlock progressively.

| ID | Name | Effect on escape (not blocked) |
|---|---|---|
| 1 | Lair | Copies creature's chosen location effect |
| 2 | Jungle | Restores 1 random used location; preserves card in hand |
| 3 | River | Activates `IsRiverVisionActive` — creature pre-move shown next round |
| 4 | Beach | Lights beacon / grants player progress (`BeachAndWreckBlock` disables) |
| 8 | Wreck | Saves location from being lost on catch (`BeachAndWreckBlock` disables) |
| 9 | Source | Restores 1 willpower |
| 10 | Artefact | Sets `IsArtefactActive = true` → disables `CurrentModifier` AND `CreatureBlockingLocation` next round |

## Second-Phase Blocking (PlayerProgress ≥ 4)

When `PlayerProgress >= 4`, creature selects **two** locations:
1. `CreatureBlockingLocation` — chosen FIRST from `AvailableLocations + CurrentPlayerChoice`; negates that location's effect
2. `CreatureChosenLocation` — chosen SECOND from remaining candidates; determines if caught

**Blocking only activates when `CurrentModifier != CreatureModifier.None`**. `IsArtefactActive` sets modifier to `None`, disabling both modifier AND blocking. Always check:
```csharp
var isLocationBlocked = playerChoice == session.CreatureBlockingLocation
    && session.CurrentModifier != CreatureModifier.None;
```

## Player Choice Naming Convention (CRITICAL FOR FAIRNESS)

- **`CurrentPlayerChoice`**: Player's choice **THIS round** (set during Selection phase). Used for catch comparison.
- **`PreviousPlayerChoice`**: Player's choice **LAST round** (set at end of Result phase). **CreatureLogic uses this for pattern prediction** to avoid cheating.
- **`_playerLocationHistory`**: Only contains `PreviousPlayerChoice` values (past rounds), never current round. Ensures creature can't abuse current-round information.

**This prevents unfair prediction**: Creature sees history of past plays, predicts next move, but cannot see what player chose this round until Result phase resolves.

## Creature Modifier System

Modifiers apply to `CreatureChosenLocation` (attack) only, never to `CreatureBlockingLocation`:

- `DoubleDamage` — extra -1 willpower on catch
- `BlockPlayerProgress` — blocks ALL escape progress
- `LoseRandomLocation` — player loses a random available location on catch
- `BeachAndWreckBlock` — disables Beach(4) and Wreck(8) effects
- `ExtraCreatureProgress` — creature gains +2 progress on catch
- `None` — special: disables Artefact protection AND blocking negation

## Creature AI (`CreatureLogic.cs`)

Three mixed strategies tracked via success rates:
1. **Trap** — targets beach/wreck locations (1, 3, 8) with `BeachAndWreckBlock`
2. **Interception** — predicts player's next location from `_playerLocationHistory`
3. **Exploitation** — if same location used 3+ times in last 5 rounds (`_singleLocationObsessionThreshold`), prioritizes it

`DetermineOptimalCreatureLocation(session, modifier, candidateLocations)` scores only the **passed `candidateLocations`**, never `session.AvailableLocations` directly.

## Data Persistence

`GameStore` wraps EF Core (`AppDbContext`). Sessions are persisted to the database — **not in-memory**. Migrations live in `Migrations/`. Run `dotnet ef migrations add <Name>` for schema changes.

## Build & Test

```bash
dotnet build                  # Debug build
dotnet run                    # API on http://localhost:5000
dotnet test                   # Runs NotAlone.Tests/
dotnet ef migrations add Name # Add EF migration
```

Tests in [NotAlone.Tests/GameEngineModifierTests.cs](../NotAlone.Tests/GameEngineModifierTests.cs): use `CreateTestSession()` helper, configure `CurrentModifier`, call `engine.ApplyCreatureModifier()` or `engine.ResolveRound()`. No Arrange/Act/Assert comments — match existing style.

## Key Conventions

- **Status messages**: Append to `session.StatusMessage` with phase prefix: `[Selection] ✓ ...`, `[CreatureTurn] ...`, `[Result] ⚠️ ...`
- **Location list mutation**: Use `.Remove()`/`.Add()` in place; never reassign `AvailableLocations`; use `.ToList()` copies in loops
- **Nullable creature locations**: Always `.HasValue` guard before `.Value` on `int?` session fields
- **Deferred comparison**: Never compare creature vs player location in `CreatureTurn` — only inside `ResolveRound()`
- **C# style**: C# 14, file-scoped namespaces, `is null` checks, XML doc on public methods, PascalCase public / camelCase private