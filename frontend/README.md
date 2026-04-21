# Not Alone Frontend

React-приложение для игры "Not Alone" с интеграцией .NET 10 бэкенда.

## Структура проекта

```
frontend/
├── public/
│   └── index.html
├── src/
│   ├── api/
│   │   └── apiClient.js          # Конфигурация axios с JWT
│   ├── components/
│   │   ├── Navigation.js         # Навигационная панель
│   │   └── PrivateRoute.js       # Защита маршрутов
│   ├── context/
│   │   └── AuthContext.js        # Контекст авторизации
│   ├── pages/
│   │   ├── LoginPage.js          # Страница входа
│   │   ├── RegisterPage.js       # Страница регистрации
│   │   └── GamePage.js           # Главная игровая страница
│   ├── services/
│   │   ├── authService.js        # Сервис авторизации
│   │   ├── gameService.js        # Сервис игры
│   │   ├── survivalService.js    # Сервис выживания
│   │   └── tradeService.js       # Сервис торговли
│   ├── styles/
│   │   ├── Auth.css
│   │   ├── Game.css
│   │   └── Navigation.css
│   ├── App.js
│   ├── App.css
│   ├── index.js
│   └── index.css
├── package.json
└── README.md
```

## Установка

```bash
cd frontend
npm install
```

## Конфигурация

Создайте файл `.env` в директории `frontend`:

```
REACT_APP_API_URL=http://localhost:5000/api
```

## Запуск разработки

```bash
npm start
```

Приложение откроется на `http://localhost:3000`

## Структура API сервисов

### AuthService
- `registerUser(username, email, password)` - Регистрация
- `loginUser(username, password)` - Вход
- `logoutUser()` - Выход
- `getCurrentUser()` - Получить профиль
- `updateUserProfile(userData)` - Обновить профиль

### GameService
- `getGameSessions()` - Получить все сессии
- `createGameSession(gameData)` - Создать новую сессию
- `getGameSession(sessionId)` - Получить детали сессии
- `updateGameSession(sessionId, updateData)` - Обновить сессию
- `endGameSession(sessionId)` - Завершить сессию
- `getGameHistory(userId)` - Получить историю

### SurvivalService
- `getSurvivalCards()` - Получить карты
- `getSurvivalCard(cardId)` - Деталь карты
- `playCard(sessionId, cardData)` - Сыграть карту
- `resistAction(sessionId, resistData)` - Совершить сопротивление
- `getSurvivalStatus(sessionId)` - Статус выживания
- `getAvailableActions(sessionId)` - Доступные действия

### TradeService
- `getAvailableItems()` - Получить товары
- `getMarketOffers()` - Получить предложения
- `createTradeOffer(offerData)` - Создать предложение
- `acceptTradeOffer(offerId)` - Принять предложение
- `rejectTradeOffer(offerId)` - Отклонить предложение
- `getTradeHistory(userId)` - История торговли
- `getUserInventory(userId)` - Инвентарь

## Особенности

- ✅ JWT авторизация с автоматическим добавлением токена в заголовки
- ✅ Контекст для управления состоянием аутентификации
- ✅ Защищенные маршруты (PrivateRoute)
- ✅ Интерцепторы для обработки ошибок 401
- ✅ Сервисы для работы с каждым контроллером бэкенда
- ✅ Локальное хранилище токена
- ✅ Темный интерфейс с красной акцентной палитрой

## Сборка для продакшена

```bash
npm run build
```

Собранные файлы находятся в папке `build/`

## Технологии

- React 18
- React Router v6
- Axios
- CSS3
