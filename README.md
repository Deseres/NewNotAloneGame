# NotAlone - Web API

A .NET 10 Web API for the "Not Alone" survival game featuring location-based gameplay, creature AI, and survival card mechanics.

## Features

- **Game Sessions**: Manage individual game states with Singleton pattern
- **Location System**: Players and creatures choose locations strategically
- **Survival Cards**: Play special cards with unique effects
- **Phases**: Selection (player choice) and Result (effects resolution)
- **Will Power Trading**: Resist or give up through willpower mechanics

## Building & Running

```bash
# Build
dotnet build

# Run
dotnet run --urls=http://localhost:5000
```

## API Endpoints

### Game Management

- `POST /api/game/start` - Start a new game session
- `POST /api/game/{id}/play` - Play a round (choose location)
- `POST /api/game/{id}/next-round` - Move to next round

### Survival Cards

- `POST /api/game/{id}/cards/play/{cardId}` - Play a survival card
  
  **Request Body (optional):**
  ```json
  {
    "targetLocationIds": [2, 3]
  }
  ```
  
  Required for `LocationsRegen` card type

### Will Power Trading

- `POST /api/game/{id}/resist` - Resist (spend willpower to restore locations)
- `POST /api/game/{id}/giveup` - Give up (reset and boost creature progress)

## Card Types

| ID | Name | Type | Phase | Effect |
|---|---|---|---|---|
| 1 | Heal | Heal | Selection | Restore +1 willpower (max 3) |
| 2 | Beacon | Beacon | Selection | Light the beacon |
| 3 | Regenerate 2 Locations | LocationsRegen | Selection | Restore 2 used locations to available |
| 4 | Move Target | MoveTarget | Result | TBD |
| 5 | Fog | Fog | Selection | Activate fog effect |

## Game Phases

- **Selection**: Player chooses a location while creature is hidden
- **Result**: Effects are resolved, special locations trigger
- **GameOver**: Game has ended (victory/defeat)

## Project Structure

```
Controllers/
  - GameController.cs
  - SurvivalController.cs
  - TradeController.cs
Models/
  - GameSession.cs
  - SurvivalCard.cs
  - SurvivalCardType.cs
  - PlayCardRequest.cs
Services/
  - GameEngine.cs
  - SurvivalService.cs
  - TradeService.cs
  - GameStore.cs (Singleton)
```

## Requirements

- .NET 10 SDK

## Example: Playing a LocationsRegen Card

```bash
POST /api/game/550e8400-e29b-41d4-a716-446655440000/cards/play/3
Content-Type: application/json

{
  "targetLocationIds": [2, 4]
}
```

Response: Locations 2 and 4 are restored to available locations.
