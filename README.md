# NotAlone - Full Stack Survival Game

A strategic location-based survival game with creature AI mechanics. Backend is built with .NET 10 Web API.

---

## Quick Start

### Prerequisites
- Frontend framework/library of choice
- API is live at: https://notalone-api-bjbza7e9gafjfrfv.swedencentral-01.azurewebsites.net

### Development
```bash
# API Base URL
https://notalone-api-bjbza7e9gafjfrfv.swedencentral-01.azurewebsites.net/api

# Swagger UI
https://notalone-api-bjbza7e9gafjfrfv.swedencentral-01.azurewebsites.net/swagger

# CORS: Enabled for all origins
```

---

## Backend API Overview

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login and get JWT token |

**Example Login:**
```bash
POST https://notalone-api-bjbza7e9gafjfrfv.swedencentral-01.azurewebsites.net/api/auth/login
Content-Type: application/json

{
  "email": "player1@example.com",
  "password": "Password123!"
}
```

**Password Requirements:**
- Minimum 8 characters
- Must contain at least one special character (!@#$%^&*)

**Response:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "player1@example.com",
  "username": "player1_abc1",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Game Management
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/game/start` | Start a new game session | Required |
| POST | `/api/game/{id}/play` | Play a round (choose location) | Required |
| POST | `/api/game/{id}/creature-turn` | Creature turn (chooses location) | Required |
| POST | `/api/game/{id}/next-round` | Move to next round | Required |

### Survival Cards
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/game/{id}/cards/play/{cardId}` | Play a card | Required |

### Willpower & Surrender
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/game/{id}/resist` | Spend willpower to restore locations | Required |
| POST | `/api/game/{id}/giveup` | Give up (boost creature progress) | Required |

---

## Locations (1-10)

The game features 10 strategic locations with unique mechanics:

| # | Location | Effect | Safety | Strategic Use |
|---|----------|--------|--------|----------------|
| 1 | **Lair** | If you escape: copies creature's location effect. If caught: lose extra willpower (2 total) | 0/100 | High risk, high reward - study creature strategy |
| 2 | **Jungle** | If you escape: restore one random used location | 40/100 | Mid-game recovery for locations |
| 3 | **River** | If you escape: enables River Vision (see creature's next move) | 45/100 | Powerful intel for next round prediction |
| 4 | **Beach** | If you escape with beacon lit: gain +1 progress. Can extinguish creature's beacon | 35/100 | Beacon manipulation - key tactical location |
| 5 | **Rover** | If you escape: unlock one randomly blocked location | 50/100 | Expand options when trapped |
| 6 | **Swamp** | If you escape: preserved in hand (can reuse next round) | 48/100 | Strategic recycling for survival cards |
| 7 | **Shelter** | If you escape: preserved in hand (can reuse next round) | 52/100 | Safe card regeneration |
| 8 | **Wreck** | If you escape: gain +1 progress toward victory | 38/100 | High-value escape location |
| 9 | **Source** | If you escape: restore +1 willpower (max 3) | 55/100 | Healing location - creature hunts here |
| 10 | **Artefact** | If you escape: disable creature's next modifier | 60/100 | Neutralizes creature power - safest location |

**Location Mechanics:**
- **Available Locations**: Start with 5 of the first 5 locations
- **Used Locations**: Locations you've chosen become "used" temporarily
- **Blocked Locations**: Locations 6-10 start blocked (must unlock via Rover)
- **Recycled Cards**: Swamp & Shelter preserve cards for next round
- **Escape vs Caught**: Effects only trigger if you escape; being caught prevents location effects

**Creature Modifiers:**
Each round the creature applies a random modifier affecting the outcome:
- **DoubleDamage**: Creature gains +2 progress instead of +1 when catching
- **BlockPlayerProgress**: Your escape doesn't grant progress
- **LoseRandomLocation**: You lose one random available location
- **BeachAndWreckBlock**: Beach & Wreck effects disabled
- **ExtraCreatureProgress**: Creature gains +2 progress (but you can still gain progress on escape)

---

## Authentication Flow

```
1. User submits login form with email and password
2. POST /api/auth/login
3. Backend returns JWT token (valid 60 minutes)
4. Store token in localStorage or secure storage
5. Include "Authorization: Bearer TOKEN" header in all future requests
6. If 401 response -> clear token and redirect to login
```

---

## Game Flow

```
1. User starts game (POST /api/game/start)
2. Selection Phase:
   - Player chooses location
   - Can play cards
   - POST /api/game/{id}/play with location
3. Result Phase:
   - Creature location revealed
   - Cards resolve effects
   - POST /api/game/{id}/next-round
4. Repeat until game over
```

---

## Architecture

### Controllers
- **AuthController** - User registration and login
- **GameController** - Game session management (start, play, end)
- **SurvivalController** - Card library and card playing
- **TradeController** 

### Core Services
- **GameEngine** - Round processing and phase transitions
- **GameStore** - In-memory game session storage
- **CreatureLogic** - Creature AI decision making
- **SurvivalService** - Card effects and willpower management
- **AppDbContext** - Entity Framework Core database access

### Models
- **User** - User accounts
- **GameSession** - Active game state
- **GameHistory** - Completed game records
- **SurvivalCard** - Card definitions

---

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Initial Catalog=NotAloneDb;Integrated Security=true;Encrypt=false;"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256!",
    "Issuer": "NotAloneAPI",
    "Audience": "NotAloneClient",
    "ExpirationMinutes": 60
  }
}
```

### JWT Details
- **Expiration:** 60 minutes (configurable)
- **Algorithm:** HMAC SHA-256
- **Token Claims:** User ID, Email, Username
- **Secret Key:** Update before production deployment

---

## Database

Uses **SQL Server** with Entity Framework Core. Migrations run automatically on startup.

**Tables:**
- **Users** - User credentials
- **GameSessions** - Active game state
- **GameHistories** - Completed game records
- **SurvivalCards** - Card library

---

## Project Structure

```
NotAlone/
├── Controllers/          - API endpoints
├── Services/             - Business logic & database
├── Models/               - Domain entities
├── Migrations/           - EF Core migrations
├── Program.cs            - Startup & DI configuration
├── NotAlone.csproj       - Project file
└── README.md
```

---

## Testing

**Swagger UI:** https://notalone-api-bjbza7e9gafjfrfv.swedencentral-01.azurewebsites.net/swagger

**HTTP Client File:** `NotAlone.http` contains pre-configured requests

**Unit Tests:** `NotAlone.Tests/GameEngineModifierTests.cs`

---

## Deployment

### Local Monolith Deployment (Manual)

This project uses **manual local deployment** - no automated CI/CD pipelines. Deploy from VS Code:

#### 1. Build Locally
```bash
# Build the backend
dotnet build -c Release

# Build the frontend
cd Frontend
npm run build
cd ..
```

#### 2. Publish as Monolith
```bash
dotnet publish -c Release
```
This creates `/bin/Release/net10.0/publish/` containing:
- Backend API executable
- Frontend assets in `wwwroot/`
- All dependencies

#### 3. Deploy Package
Copy the entire `/bin/Release/net10.0/publish/` folder to your hosting server.

#### 4. Run
```bash
# On the server
cd publish
dotnet NotAlone.dll --urls=http://localhost:5000
```

The API and frontend are served together from the same application.

### Pre-Deployment Checklist

Before pushing to production:
- [ ] Generate new JWT secret key (update `appsettings.json`)
- [ ] Configure SQL Server connection string for production
- [ ] Enable HTTPS/SSL
- [ ] Update CORS for your frontend domain
- [ ] Set up logging and monitoring
- [ ] Test authentication flow
- [ ] Verify all game mechanics work end-to-end
