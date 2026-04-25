# NotAlone - Full Stack Survival Game

A strategic location-based survival game with creature AI mechanics. Backend is built with .NET 10 Web API, frontend with React.

---

## 🎮 Quick Start for Frontend Developers

### Prerequisites
- Node.js 18+ and npm
- Backend running on `http://localhost:5000`

### Setup

```bash
# Install dependencies
npm install

# Start development server
npm start
```

The app will open at `http://localhost:3000`

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

### Setting Up API Client

Create `src/api/apiClient.js`:

```javascript
import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
});

// Add JWT token to all requests
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle 401 errors (token expired)
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

### Service Layer Structure

Create service files in `src/services/`:

- **authService.js** - Register, login, profile
- **gameService.js** - Game sessions, rounds, game lifecycle
- **survivalService.js** - Cards, card playing, survival status
- **tradeService.js** - Trading (future implementation)

Example `gameService.js`:
```javascript
import apiClient from '../api/apiClient';

export const startGame = () =>
  apiClient.post('/game/start');

export const getGame = (gameId) =>
  apiClient.get(`/game/${gameId}`);

export const playRound = (gameId, locationId) =>
  apiClient.post(`/game/${gameId}/play`, { locationId });

export const nextRound = (gameId) =>
  apiClient.post(`/game/${gameId}/next-round`);

export const endGame = (gameId) =>
  apiClient.post(`/game/${gameId}/end`);
```

### Pages to Implement

1. **AuthPages** (`/login`, `/register`)
   - Handle user authentication
   - Store JWT token in localStorage
   - Redirect to game on success

2. **GamePage** (`/game/:id`)
   - Display game session state
   - Show available locations
   - Allow location selection in Selection phase

3. **SurvivalPage**
   - Display player's survival cards
   - Show card effects and requirements
   - Allow card playing with proper validation

4. **GameHistoryPage**
   - List completed games
   - Show game results and statistics

### Core Features to Implement

✅ **Authentication**
- Login/Register forms
- JWT token management
- Protected routes

✅ **Game Dashboard**
- Current game status
- Round counter
- Willpower/location display
- Game history

✅ **Game Board**
- 5-6 location buttons (player selection)
- Creature location (revealed in Result phase)
- Visual feedback for matches/mismatches

✅ **Survival Cards UI**
- Card hand display
- Card selection/playing
- Modal for card details and target selection

✅ **Game Flow**
- Selection phase UI
- Result phase with animations
- Game over screen with statistics

---

## 🔄 Authentication Flow

```
1. User fills login form
2. POST /api/auth/login
3. Backend returns JWT token
4. Store token in localStorage
5. Include "Authorization: Bearer TOKEN" in all requests
6. If 401 response → clear token & redirect to login
```

**Frontend code:**
```javascript
const handleLogin = async (email, password) => {
  const response = await apiClient.post('/auth/login', { email, password });
  localStorage.setItem('authToken', response.data.token);
  setUser(response.data.user);
  navigate('/game');
};
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

- [React Documentation](https://react.dev)
- [Axios Documentation](https://axios-http.com)
- [JWT Explained](https://jwt.io/introduction)
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
