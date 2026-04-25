# NotAlone - Full Stack Architecture

## Overview

The project consists of two parts:
- **Backend**: .NET 10 Web API with JWT authentication
- **Frontend**: Any JavaScript framework (React, Vue, Angular, Svelte, vanilla JS, etc.)

## Project Structure

```
c:\NewNotAloneGame/
├── Backend (.NET 10)
│   ├── Controllers/
│   │   ├── AuthController.cs      # JWT authentication, registration
│   │   ├── GameController.cs      # Game session management
│   │   ├── SurvivalController.cs  # Survival logic
│   │   └── TradeController.cs     # Trading system
│   ├── Services/
│   │   ├── AppDbContext.cs        # EF Core context
│   │   ├── GameEngine.cs          # Game business logic
│   │   ├── GameStore.cs           # Data storage
│   │   ├── SurvivalService.cs     # Survival service
│   │   ├── CreatureLogic.cs       # Creature AI logic
│   │   └── TradeService.cs        # Trading service
│   ├── Models/                    # Domain models
│   ├── Migrations/                # Database migrations
│   ├── Program.cs                 # DI configuration
│   └── NotAlone.csproj
│
├── Migrations/                    # EF Core migrations
├── README.md                      # Project documentation
├── ARCHITECTURE.md                # This file
└── .gitignore                     # Git ignore rules
```

## Backend and Frontend Integration

### API Configuration

- Backend listens on: `http://localhost:5000`
- Frontend connects to: `http://localhost:5000/api`
- CORS is configured to accept requests from any origin in development

### JWT Authentication

1. User logs in via frontend
2. Backend validates credentials and generates JWT token
3. Frontend stores token in localStorage
4. All subsequent requests include `Authorization: Bearer {token}` header
5. Backend validates token before processing protected endpoints
6. If token expires (401 response), frontend clears token and redirects to login

### API Endpoints

#### Authentication
```
POST   /api/auth/register      - User registration
POST   /api/auth/login         - User login
GET    /api/auth/profile       - Get user profile
PUT    /api/auth/profile       - Update user profile
```

#### Game Management
```
POST   /api/game/start         - Start new game session
GET    /api/game/{id}          - Get game session details
POST   /api/game/{id}/play     - Play a round (choose location)
POST   /api/game/{id}/next-round - Move to next round
POST   /api/game/{id}/end      - End game session
```

#### Survival Cards
```
GET    /api/survival/cards                - Get all available cards
POST   /api/survival/cards/play/{cardId}  - Play a card
GET    /api/survival/status/{sessionId}   - Get survival status
```

#### Will Power & Trading
```
POST   /api/game/{id}/resist   - Spend willpower to restore locations
POST   /api/game/{id}/giveup   - Give up (boost creature progress)
```

## Running the Project

### Backend

```bash
cd c:\NewNotAloneGame
dotnet run
# Listens on http://localhost:5000
# Swagger UI available at http://localhost:5000/swagger
```

**Requirements:**
- .NET 10 SDK
- SQL Server (LocalDB or Express)
- Database: NotAloneDb (automatically migrated on startup)

### Frontend

Create your frontend application using your framework of choice:
- Must support HTTP requests (fetch, axios, etc.)
- Must support localStorage for JWT token persistence
- Must handle CORS for requests to `http://localhost:5000`
- All requests need to include JWT token in Authorization header

**Required API integrations:**
- Authentication (register, login, profile)
- Game management (create session, play rounds)
- Survival cards (display, play cards)
- Optional: Trading system

## Technology Stack

### Backend
- **.NET 10** - Framework
- **Entity Framework Core 10** - ORM
- **ASP.NET Core Web API** - Web framework
- **JWT (System.IdentityModel.Tokens.Jwt)** - Authentication
- **SQL Server** - Database
- **Swagger/OpenAPI** - API documentation

### Frontend
- **Any JavaScript framework** - React, Vue, Angular, Svelte, or vanilla JS
- **HTTP client** - fetch API, axios, or equivalent
- **localStorage API** - For JWT token storage
- **CSS** - Styling (framework of choice)

### Communication Protocol
- **REST API** - HTTP/HTTPS
- **JSON** - Data format
- **JWT** - Token-based authentication

## Core Features

### Authentication (Auth)
- User registration with email/password
- JWT token-based login
- Protected endpoints requiring valid token
- User profile management
- Auto-logout on token expiration

### Game Logic (Game)
- Game session creation and management
- Round management (Selection and Result phases)
- Location selection and creature AI
- Game history tracking
- Session status monitoring

### Survival System (Survival)
- Survival card deck with 5 card types
- Card playing mechanics
- Location regeneration system
- Will power management
- Phase-based game flow

### Trading System (Trade)
- *To be implemented in future phases*
- Market offers
- Trading history
- Will power trading mechanics

## Game Flow

```
1. User Registration/Login
   ↓
2. Game Start (POST /api/game/start)
   ↓
3. Selection Phase
   - Player chooses location
   - Player can play cards
   - POST /api/game/{id}/play
   ↓
4. Result Phase
   - Creature location revealed
   - Cards resolve effects
   - POST /api/game/{id}/next-round
   ↓
5. Repeat 3-4 until game over
   ↓
6. Game End (POST /api/game/{id}/end)
   ↓
7. View game history
```

## CORS Configuration

The backend is configured to accept requests from any origin in development mode. In production, update the CORS policy:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("https://yourdomain.com")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

app.UseCors("AllowFrontend");
```

## Frontend Implementation Checklist

- [ ] Set up HTTP client with JWT token handling
- [ ] Implement authentication pages (login, register)
- [ ] Implement protected route middleware
- [ ] Create API service layer for each feature
- [ ] Build game board UI (location selection)
- [ ] Build survival cards UI
- [ ] Implement phase transition UI
- [ ] Add game history view
- [ ] Add user profile management
- [ ] Test all API integrations
- [ ] Implement error handling and user feedback
- [ ] Add loading states for async operations

## Debugging

### Backend
- Use Visual Studio debugger
- Check console logs
- Use Swagger UI at `http://localhost:5000/swagger` to test endpoints
- Check database with SQL Server Management Studio

### Frontend
- Use browser DevTools (Network tab for API requests)
- Check browser console for JavaScript errors
- Use localStorage to verify JWT token is stored
- Decode JWT token at `https://jwt.io` to verify claims
- Check CORS errors in Network tab

### Common Issues

**401 Unauthorized**: Token missing or expired
- Solution: Login again and ensure token is stored in localStorage

**CORS Error**: Frontend blocked from accessing backend
- Check backend CORS configuration
- Ensure frontend is making requests to `http://localhost:5000/api`

**Database Error**: SQL Server connection failed
- Verify SQL Server is running
- Check connection string in appsettings.json

**API Response Error**: Check backend logs and Swagger UI

## Next Steps

### Phase 1 (Current)
- [x] Backend API setup with JWT authentication
- [x] Game logic and survival card system
- [ ] Frontend implementation with chosen framework
- [ ] Integration testing between frontend and backend

### Phase 2 (Future)
- [ ] Complete trading system
- [ ] WebSocket for real-time multiplayer
- [ ] Notification system
- [ ] Advanced creature AI
- [ ] Game statistics and leaderboards

### Phase 3 (Enhancement)
- [ ] Mobile app version
- [ ] Database optimization
- [ ] Performance monitoring
- [ ] Advanced analytics
- [ ] Community features
