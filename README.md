# NotAlone - Full Stack Survival Game

A strategic location-based survival game with creature AI mechanics. Backend is built with .NET 10 Web API.

---

## 🎮 Quick Start for Frontend Developers

### Prerequisites
- Backend running on `http://localhost:5000`
- Any frontend framework (React, Vue, Angular, Svelte, vanilla JS, etc.)

### Development Requirements
Your frontend framework/technology of choice should support:
- HTTP requests (to communicate with REST API)
- Local storage (for JWT token persistence)
- CORS handling for `http://localhost:5000`

---

## 📋 Backend API Overview

The backend runs on `http://localhost:5000` and provides the following endpoints:

### Authentication
All game endpoints require JWT authentication (except `/api/auth/register` and `/api/auth/login`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login and get JWT token |
| GET | `/api/auth/profile` | Get user profile |
| PUT | `/api/auth/profile` | Update user profile |

**Example Login:**
```bash
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "player1@example.com",
  "password": "Password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "player1@example.com"
  }
}
```

### Game Management
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/game/start` | Start a new game session | ✓ |
| GET | `/api/game/{id}` | Get game session details | ✓ |
| POST | `/api/game/{id}/play` | Play a round (choose location) | ✓ |
| POST | `/api/game/{id}/next-round` | Move to next round | ✓ |
| POST | `/api/game/{id}/end` | End game session | ✓ |

### Survival Cards
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/survival/cards` | Get all available cards | ✓ |
| POST | `/api/survival/cards/play/{cardId}` | Play a card | ✓ |
| GET | `/api/survival/status/{sessionId}` | Get survival status | ✓ |

**Play Card with Parameters:**
```bash
POST http://localhost:5000/api/survival/cards/play/3
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "targetLocationIds": [2, 4]
}
```

### Will Power & Trading
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/game/{id}/resist` | Spend willpower to restore locations | ✓ |
| POST | `/api/game/{id}/giveup` | Give up (boost creature progress) | ✓ |

---

## 🎴 Card System

### Available Cards

| ID | Name | Type | Phase | Effect |
|----|------|------|-------|--------|
| 1 | Heal | Heal | Selection | Restore +1 willpower (max 3) |
| 2 | Beacon | Beacon | Selection | Light the beacon |
| 3 | Regenerate 2 Locations | LocationsRegen | Selection | Restore 2 used locations |
| 4 | Move Target | MoveTarget | Result | Change creature location |
| 5 | Fog | Fog | Selection | Hide location from creature |

### Card Phase System

- **Selection Phase**: Player chooses a location and plays cards while creature location is unknown
- **Result Phase**: Locations are revealed, cards trigger effects, game logic resolves
- **GameOver**: Game ends with victory or defeat

---

## 🛠️ Frontend Integration Guide

### API Client Setup

Your frontend needs to:

1. **Make HTTP requests** to `http://localhost:5000/api`
2. **Store JWT token** in localStorage after login
3. **Include token** in `Authorization: Bearer {token}` header for protected endpoints
4. **Handle 401 responses** by clearing token and redirecting to login

Example with vanilla JavaScript fetch:
```javascript
const API_BASE_URL = 'http://localhost:5000/api';

const apiRequest = async (endpoint, method = 'GET', body = null) => {
  const token = localStorage.getItem('authToken');
  const headers = {
    'Content-Type': 'application/json',
  };
  
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : null,
  });
  
  if (response.status === 401) {
    localStorage.removeItem('authToken');
    window.location.href = '/login';
  }
  
  return response.json();
};
```

### Service Organization

Organize API calls by feature:
- **Auth** - Register, login, profile
- **Game** - Game sessions, rounds, game lifecycle
- **Survival** - Cards, card playing, survival status
- **Trade** - Trading (future implementation)

### Views/Pages to Implement

1. **Login & Register** - User authentication
2. **Game Board** - Main gameplay interface
3. **Survival Cards** - Card display and playing
4. **Game History** - Completed games list
5. **User Profile** - User management

### Core Features Checklist

- [ ] User registration and login
- [ ] JWT token storage and management
- [ ] Protected routes/pages
- [ ] Game session creation
- [ ] Location selection (5-6 locations)
- [ ] Creature location reveal (Result phase)
- [ ] Survival card display and playing
- [ ] Will power/locations status display
- [ ] Game over screen with results
- [ ] Game history view

---

## 🔄 Authentication Flow

```
1. User fills login form
2. POST /api/auth/login with email and password
3. Backend returns JWT token
4. Store token in localStorage
5. Include "Authorization: Bearer TOKEN" header in all future requests
6. If 401 response → clear token and redirect to login
```

---

## 🎮 Game Flow

```
1. User starts game (POST /api/game/start)
2. Selection Phase:
   - Player chooses location
   - Can play cards
   - POST /api/game/{id}/play with location
3. Result Phase:
   - Creature location revealed
   - Cards resolve
   - POST /api/game/{id}/next-round
4. Repeat until game over
5. POST /api/game/{id}/end to finish
```

---

## 📊 Data Models

### Game Session
```json
{
  "id": "uuid",
  "userId": "uuid",
  "roundNumber": 1,
  "playerWillpower": 3,
  "creatureProgress": 0,
  "phase": "Selection|Result|GameOver",
  "selectedLocationId": 1,
  "creatureLocationId": 2,
  "usedLocations": [1, 3],
  "availableLocations": [2, 4, 5, 6],
  "createdAt": "2026-04-25T..."
}
```

### User
```json
{
  "id": "uuid",
  "email": "player@example.com",
  "passwordHash": "hashed_password",
  "createdAt": "2026-04-25T..."
}
```

### Survival Card
```json
{
  "id": 1,
  "name": "Heal",
  "type": "Heal|Beacon|LocationsRegen|MoveTarget|Fog",
  "phase": "Selection|Result",
  "description": "Restore +1 willpower (max 3)"
}
```

### Game History
```json
{
  "id": "uuid",
  "userId": "uuid",
  "gameSessionId": "uuid",
  "outcome": "Victory|Defeat",
  "finalRound": 10,
  "finalWillpower": 2,
  "completedAt": "2026-04-25T..."
}
```

---

## 🏗️ Backend Architecture

### Controllers

#### AuthController
- **POST /api/auth/register** - User registration
- **POST /api/auth/login** - User authentication
- **GET /api/auth/profile** - Get user profile
- **PUT /api/auth/profile** - Update user profile

#### GameController  
- **POST /api/game/start** - Initialize new game session
- **GET /api/game/{id}** - Retrieve game state
- **POST /api/game/{id}/play** - Process location selection
- **POST /api/game/{id}/next-round** - Advance to next round
- **POST /api/game/{id}/end** - End game session
- **POST /api/game/{id}/resist** - Spend willpower
- **POST /api/game/{id}/giveup** - Surrender and reset

#### SurvivalController
- **GET /api/survival/cards** - Get card library
- **POST /api/survival/cards/play/{cardId}** - Play specific card
- **GET /api/survival/status/{sessionId}** - Get survival status

#### TradeController
- *Future implementation for trading system*

### Services

#### GameEngine
- Core game logic and round processing
- Phase transitions (Selection → Result)
- Win/lose condition checking
- Location availability tracking

#### GameStore
- In-memory storage for active game sessions (Singleton pattern)
- Session retrieval and updates
- Real-time game state management

#### CreatureLogic
- Creature AI decision making
- Location selection algorithm
- Progress tracking

#### SurvivalService
- Card effect resolution
- Willpower management
- Location regeneration logic

#### AppDbContext
- Entity Framework Core database context
- User, GameSession, GameHistory, SurvivalCard entities
- Database migrations
- Connection to SQL Server

### Models (Domain Entities)

- **User** - User accounts and authentication
- **GameSession** - Active game state
- **GameHistory** - Completed game records
- **SurvivalCard** - Card definitions and metadata
- **SurvivalCardType** - Enum for card types

---

## 🗄️ Database Schema

The application uses 4 main tables:

1. **Users** - User credentials and profiles
2. **GameSessions** - Active games with current state
3. **GameHistories** - Historical game records and outcomes
4. **SurvivalCards** - Card library and definitions

Database is automatically initialized with migrations on backend startup.

---

## � JWT Configuration

The backend uses JWT (JSON Web Tokens) for stateless authentication:

**Token Claims:**
- `sub` - Subject (User ID)
- `email` - User email
- `exp` - Expiration time (default: 60 minutes)
- `iss` - Issuer: `NotAloneAPI`
- `aud` - Audience: `NotAloneClient`

**Secret Key:** Configured in `appsettings.json`
```json
{
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256!",
    "Issuer": "NotAloneAPI",
    "Audience": "NotAloneClient",
    "ExpirationMinutes": 60
  }
}
```

**Usage:**
1. Frontend stores token in localStorage after login
2. Include in all requests: `Authorization: Bearer {token}`
3. Backend validates token signature and expiration
4. Token refresh requires re-login (no refresh tokens currently)

---

## 🎯 Dependency Injection (DI)

Services registered in Program.cs:

```csharp
builder.Services.AddScoped<GameStore>();
builder.Services.AddScoped<GameEngine>();
builder.Services.AddScoped<CreatureLogic>();
builder.Services.AddScoped<SurvivalService>();
builder.Services.AddScoped<TradeService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
```

**Scoped Services:** New instance per HTTP request
**DbContext:** Manages database connections and transactions

---

## 📂 Project Structure

```
NotAlone.csproj
├── Controllers/
│   ├── AuthController.cs
│   ├── GameController.cs
│   ├── SurvivalController.cs
│   └── TradeController.cs
├── Services/
│   ├── AppDbContext.cs
│   ├── GameEngine.cs
│   ├── GameStore.cs
│   ├── SurvivalService.cs
│   ├── CreatureLogic.cs
│   └── TradeService.cs
├── Models/
│   ├── User.cs
│   ├── GameSession.cs
│   ├── GameHistory.cs
│   ├── SurvivalCard.cs
│   ├── SurvivalCardType.cs
│   ├── PlayCardRequest.cs
│   └── ResistRequest.cs
├── Migrations/
│   └── [EF Core migration files]
├── Program.cs
├── README.md
├── ARCHITECTURE.md
└── NotAlone.csproj
```

---

## �🗄️ Database

Uses **SQL Server** with Entity Framework Core.

**Connection String:** `Server=localhost\SQLEXPRESS;Initial Catalog=NotAloneDb;`

Database is automatically initialized with migrations on backend startup.

---

## 🧪 Testing

### Unit Tests
Located in `NotAlone.Tests/`:
- `GameEngineModifierTests.cs` - Game logic unit tests

### Manual Testing
1. **Swagger UI** - Test endpoints interactively
   ```
   http://localhost:5000/swagger
   ```

2. **HTTP Client** - Use NotAlone.http or Postman
   - Pre-configured requests for all endpoints
   - JWT token injection

3. **Frontend Integration**
   - Test authentication flow
   - Verify game round progression
   - Check card playing mechanics

---

## 🔧 Build & Run

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB or Express Edition)
- Visual Studio 2022 or VS Code

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
# or use the VS Code task
# or run with: dotnet run --urls=http://localhost:5000
```

### Database
```bash
# Migrations run automatically on startup
# Manual migration (if needed):
# dotnet ef database update
```

---

## 📋 Configuration Files

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Initial Catalog=NotAloneDb;Integrated Security=true;Encrypt=false;"
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "NotAloneAPI",
    "Audience": "NotAloneClient",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### appsettings.Development.json
- Local development overrides
- Debug logging configuration

---

## 🚀 Deployment Checklist

- [ ] Update API_BASE_URL for production environment
- [ ] Generate new JWT secret key (not the default)
- [ ] Configure production SQL Server connection
- [ ] Set up CORS for frontend domain
- [ ] Enable HTTPS/SSL
- [ ] Configure logging and monitoring
- [ ] Implement proper error handling and user feedback
- [ ] Add loading states for async operations
- [ ] Test all authentication flows
- [ ] Implement game state caching if needed
- [ ] Add analytics/logging
- [ ] Set up CI/CD pipeline
- [ ] Database backups and disaster recovery

---

## 📝 Development Tips

1. **Testing API Endpoints**: Use Swagger UI at `http://localhost:5000/swagger`
2. **Check Token**: Decode JWT at `jwt.io` to verify claims
3. **Database Errors**: Check SQL Server is running and connection string is correct
4. **CORS Issues**: Verify frontend is making requests to `http://localhost:5000`
5. **Game Logic**: Review `GameEngine.cs` for round processing logic
6. **Database Queries**: Use SQL Server Management Studio to inspect data
7. **Performance**: Monitor GameStore for memory issues with many concurrent games
8. **Card Effects**: Check `SurvivalService.cs` for card resolution logic

---

## 📚 Useful Resources

- [JWT Explained](https://jwt.io/introduction)
- [REST API Best Practices](https://restfulapi.net/)
- [CORS Explained](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [HTTP Status Codes](https://httpwg.org/specs/rfc9110.html#status.codes)
- [ASP.NET Core API](https://learn.microsoft.com/aspnet/core)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [JWT Bearer Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/jwt-authn)

---

## ⚙️ Backend Reference

Backend requires:
- .NET 10 SDK
- SQL Server (LocalDB or Express)
- Run: `dotnet run` from project root
- Listens on: `http://localhost:5000`
- Swagger available at: `http://localhost:5000/swagger`

**Backend folder structure:**
```
Controllers/      - API endpoints (Auth, Game, Survival, Trade)
Services/         - Business logic (GameEngine, SurvivalService, etc.)
Models/           - Domain entities (User, GameSession, etc.)
Migrations/       - EF Core database schema migrations
Program.cs        - Startup configuration and DI setup
```

---

## � JWT Configuration

The backend uses JWT (JSON Web Tokens) for stateless authentication:

**Token Claims:**
- `sub` - Subject (User ID)
- `email` - User email
- `exp` - Expiration time (default: 60 minutes)
- `iss` - Issuer: `NotAloneAPI`
- `aud` - Audience: `NotAloneClient`

**Secret Key:** Configured in `appsettings.json`
```json
{
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256!",
    "Issuer": "NotAloneAPI",
    "Audience": "NotAloneClient",
    "ExpirationMinutes": 60
  }
}
```

**Usage:**
1. Frontend stores token in localStorage after login
2. Include in all requests: `Authorization: Bearer {token}`
3. Backend validates token signature and expiration
4. Token refresh requires re-login (no refresh tokens currently)

---

## 🎯 Dependency Injection (DI)

Services registered in Program.cs:

```csharp
builder.Services.AddScoped<GameStore>();
builder.Services.AddScoped<GameEngine>();
builder.Services.AddScoped<CreatureLogic>();
builder.Services.AddScoped<SurvivalService>();
builder.Services.AddScoped<TradeService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
```

**Scoped Services:** New instance per HTTP request
**DbContext:** Manages database connections and transactions

---

## 📂 Project Structure

```
NotAlone.csproj
├── Controllers/
│   ├── AuthController.cs
│   ├── GameController.cs
│   ├── SurvivalController.cs
│   └── TradeController.cs
├── Services/
│   ├── AppDbContext.cs
│   ├── GameEngine.cs
│   ├── GameStore.cs
│   ├── SurvivalService.cs
│   ├── CreatureLogic.cs
│   └── TradeService.cs
├── Models/
│   ├── User.cs
│   ├── GameSession.cs
│   ├── GameHistory.cs
│   ├── SurvivalCard.cs
│   ├── SurvivalCardType.cs
│   ├── PlayCardRequest.cs
│   └── ResistRequest.cs
├── Migrations/
│   └── [EF Core migration files]
├── Program.cs
├── README.md
├── ARCHITECTURE.md
└── NotAlone.csproj
```

---

## 🗄️ Database
