---
description: 'Guidelines for NotAlone Game backend development'
applyTo: '**/*.cs'
---

# NotAlone Game - AI Development Guide

## MCP Server Tools
- **Context7** (Docs): Search live docs for third-party libraries before relying on internal knowledge.
- **Sequential Thinking** (Reasoning): Use for complex architectural tasks or multi-step debugging.
- **Playwright** (Web): Inspect live UI at `http://localhost:5000`. Confirm server is running first.
- If a tool needs a specific input (Library ID, URL) that isn't clear, ask rather than guess.

## Project Overview

**NotAlone** is a .NET 10 strategic survival game. Player (target: 7 progress) picks locations while a creature AI (target: 5 progress) tries to catch them. Stack: ASP.NET Core Web API + EF Core (SQL Server) + React/TypeScript SPA served from `wwwroot/`.

Win/loss: `PlayerProgress >= 7` = player wins; `CreatureProgress >= 5` OR `PlayerWillpower <= 0` = creature wins.

## Three-Phase Turn State Machine

**Key files**: [Services/GameEngine.cs](../Services/GameEngine.cs), [Models/GameSession.cs](../Models/GameSession.cs), [Controllers/GameController.cs](../Controllers/GameController.cs)

`Selection → CreatureTurn → Result → (next) Selection`

| Phase | Endpoint | What happens |
|---|---|---|
| Selection | `POST /api/game/{id}/play` | Player picks from `AvailableLocations`; moved to `UsedLocations`; `CurrentPlayerChoice` set; phase → `CreatureTurn` |
| CreatureTurn | `POST /api/game/{id}/creature-turn` | `CreatureLogic.SelectCreatureLocation()` sets `CreatureChosenLocation` + `CreatureBlockingLocation` + `CurrentModifier`; phase → `Result` |
| Result | `POST /api/game/{id}/next-round` | `GameEngine.ResolveRound()` runs all effects; `PreviousPlayerChoice` updated; phase → `Selection` |

Phase transitions happen in the **controller**, not in `GameEngine`. Always guard: `if (session.CurrentPhase != GamePhase.X) return BadRequest(...)`.

## Architecture: Service Responsibilities

| Service | Responsibility |
|---|---|
| `GameEngine` | Pure game logic — `PlayRound()`, `ResolveRound()`, location effects, modifiers |
| `CreatureLogic` | AI decision-making — priority scoring, pattern detection, modifier assignment |
| `GameStore` | EF Core persistence via `AppDbContext`; also holds `Sessions` in-memory dict for optional caching |
| `TradeService` | `Resist()` (spend willpower to restore locations) and `GiveUp()` |
| `SurvivalService` | Survival card management |

All services are registered as **Scoped** in `Program.cs`. `CreatureLogic` is stateful (holds `_playerLocationHistory`, catch/escape dictionaries) — call `ResetHistory()` on game start.

## Location Map

`AvailableLocations` starts as `[1,2,3,4,5]`. Locations 6–10 unlock progressively.

| ID | Name | Escape effect |
|---|---|---|
| 1 | Lair | Copies creature's chosen location effect |
| 2 | Jungle | Restores 1 random used location; card returns to hand |
| 3 | River | Sets `IsRiverVisionActive` — creature move pre-revealed next round |
| 4 | Beach | Lights beacon first visit; +1 player progress on subsequent visits (`BeachAndWreckBlock` disables) |
| 5 | Rover | Unlocks one blocked (not yet available) location |
| 6 | Swamp | Restores up to 2 used locations; card returns to hand |
| 7 | Shelter | Grants a random survival card (ID 1–5) |
| 8 | Wreck | +1 player progress (`BeachAndWreckBlock` disables) |
| 9 | Source | +1 willpower (up to `MaxWillpower = 3`) |
| 10 | Artefact | Sets `IsArtefactActive = true` → disables `CurrentModifier` AND `CreatureBlockingLocation` next round |

Creature picks Beach (4) → extinguishes beacon regardless of player's location.

## Blocking & Modifier System

When `CurrentModifier != None`, creature selects **two** locations per turn:
1. `CreatureBlockingLocation` — from `AvailableLocations + CurrentPlayerChoice`; negates that location's effect
2. `CreatureChosenLocation` — from remaining candidates; determines catch

`IsArtefactActive` forces `CurrentModifier = None`, disabling both. Check:
```csharp
var isLocationBlocked = playerChoice == session.CreatureBlockingLocation
    && session.CurrentModifier != CreatureModifier.None;
```

Modifiers (applied to attack only, never blocking): `DoubleDamage`, `BlockPlayerProgress`, `LoseRandomLocation`, `BeachAndWreckBlock`, `ExtraCreatureProgress`. `None` disables artefact protection.

## Player Choice Naming (CRITICAL)

- `CurrentPlayerChoice`: set in **Selection phase** — used for catch comparison in `ResolveRound()`
- `PreviousPlayerChoice`: set at **end of Result phase** — what `CreatureLogic` reads for pattern prediction
- `_playerLocationHistory` in `CreatureLogic`: only `PreviousPlayerChoice` values — creature never sees current round's choice

This separation prevents the AI from unfairly knowing the current round's player move.

## Data Persistence

`GameStore` wraps `AppDbContext` (EF Core, SQL Server). Sessions are DB-persisted. Migrations live in `Migrations/`. After any `GameSession` model change, run:
```bash
dotnet ef migrations add <Name>
```

## Build & Test

```bash
dotnet build                  # Debug build
dotnet run                    # API on http://localhost:5000
dotnet test                   # Runs NotAlone.Tests/
dotnet ef migrations add Name # Schema change
```

Tests use `TestSessionFactory` ([NotAlone.Tests/Helpers/SessionFactory.cs](../NotAlone.Tests/Helpers/SessionFactory.cs)):
- `TestSessionFactory.Create(willpower, playerProgress, ...)` — clean `GamePhase.Selection` session
- `TestSessionFactory.RunFullRound(engine, session, playerLoc, creatureLoc, modifier, blockingLoc)` — drives full state machine deterministically without `CreatureLogic`

No Arrange/Act/Assert comments. Match existing test style in [GameEngineModifierTests.cs](../NotAlone.Tests/GameEngineModifierTests.cs).

## Key Conventions

- **Status messages**: Phase-prefixed — `[Selection] ✓ ...`, `[CreatureTurn] ...`, `[Result] ⚠️ ...`; append with `+=`
- **Location lists**: `.Remove()`/`.Add()` in-place; never reassign `AvailableLocations`; use `.ToList()` copies in loops
- **Nullable guards**: `.HasValue` check before `.Value` on all `int?` session fields (`CreatureChosenLocation`, `CreatureBlockingLocation`, `CurrentPlayerChoice`)
- **Deferred comparison**: Never compare creature vs player location in `CreatureTurn` phase — only inside `ResolveRound()`
- **C# style**: C# 14, file-scoped namespaces, `is null` checks, XML doc on public methods, PascalCase public / camelCase private