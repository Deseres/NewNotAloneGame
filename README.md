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

### Survival Card
```json
{
  "id": 1,
  "name": "Heal",
  "type": "Heal",
  "phase": "Selection",
  "description": "Restore +1 willpower (max 3)"
}
```

---

## 🗄️ Database

Uses **SQL Server** with Entity Framework Core.

**Connection String:** `Server=localhost\SQLEXPRESS;Initial Catalog=NotAloneDb;`

Database is automatically migrated on backend startup.

---

## 🚀 Deployment Checklist

- [ ] Update API_BASE_URL for production environment
- [ ] Implement proper error handling and user feedback
- [ ] Add loading states for async operations
- [ ] Test all authentication flows
- [ ] Implement game state caching if needed
- [ ] Add analytics/logging
- [ ] Set up CI/CD pipeline

---

## 📝 Development Tips

1. **Testing API Endpoints**: Use Swagger UI at `http://localhost:5000/swagger`
2. **Check Token**: Decode JWT at `jwt.io` to verify claims
3. **CORS Issues**: Backend allows requests from `http://localhost:3000`
4. **Database Errors**: Check SQL Server is running and connection string is correct
5. **State Management**: Consider Redux/Context API for game state

---

## 📚 Useful Resources

- [JWT Explained](https://jwt.io/introduction)
- [REST API Best Practices](https://restfulapi.net/)
- [CORS Explained](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [HTTP Status Codes](https://httpwg.org/specs/rfc9110.html#status.codes)
- [ASP.NET Core API](https://learn.microsoft.com/aspnet/core)

---

## ⚙️ Backend Setup (For Reference)

Backend requires:
- .NET 10 SDK
- SQL Server (LocalDB or Express)
- Run: `dotnet run` from project root
- Listens on: `http://localhost:5000`
- Swagger available at: `http://localhost:5000/swagger`

**Backend folder structure:**
```
Controllers/      - API endpoints
Services/         - Business logic & database access
Models/           - Data models
Migrations/       - Database schema migrations
```
