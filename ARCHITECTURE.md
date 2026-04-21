# Архитектура "Not Alone" - Fullstack

## Обзор

Проект состоит из двух частей:
- **Backend**: .NET 10 Web API с JWT авторизацией
- **Frontend**: React приложение с интеграцией API

## Структура проекта

```
c:\NewNotAloneGame/
├── backend (.NET 10)
│   ├── Controllers/
│   │   ├── AuthController.cs      # JWT авторизация, регистрация
│   │   ├── GameController.cs      # Управление игровыми сессиями
│   │   ├── SurvivalController.cs  # Логика выживания
│   │   └── TradeController.cs     # Система торговли
│   ├── Services/
│   │   ├── AppDbContext.cs        # EF Core контекст
│   │   ├── GameEngine.cs          # Бизнес-логика игры
│   │   ├── GameStore.cs           # Хранилище данных
│   │   ├── SurvivalService.cs     # Сервис выживания
│   │   ├── CreatureLogic.cs       # Логика существа
│   │   └── TradeService.cs        # Сервис торговли
│   ├── Models/
│   ├── Program.cs                 # Конфигурация DI
│   └── NotAlone.csproj
│
└── frontend/ (React)
    ├── src/
    │   ├── api/
    │   │   └── apiClient.js       # Конфигурация Axios с JWT
    │   ├── services/              # API сервисы
    │   │   ├── authService.js
    │   │   ├── gameService.js
    │   │   ├── survivalService.js
    │   │   └── tradeService.js
    │   ├── context/
    │   │   └── AuthContext.js     # Управление авторизацией
    │   ├── pages/                 # React страницы
    │   ├── components/            # Переиспользуемые компоненты
    │   └── App.js
    └── package.json
```

## Соединение Backend и Frontend

### API Конфигурация

Backend слушает на `http://localhost:5000`
Frontend подключается к `http://localhost:5000/api`

### JWT Авторизация

1. Пользователь входит через Frontend
2. Backend генерирует JWT токен
3. Frontend сохраняет токен в localStorage
4. Все последующие запросы содержат `Authorization: Bearer {token}`
5. Backend валидирует токен перед обработкой запроса

### Маршруты API

```
POST   /api/auth/register      - Регистрация
POST   /api/auth/login         - Вход
GET    /api/auth/profile       - Профиль
PUT    /api/auth/profile       - Обновить профиль

GET    /api/game/sessions      - Список сессий
POST   /api/game/sessions      - Создать сессию
GET    /api/game/sessions/:id  - Детали сессии
POST   /api/game/sessions/:id/end - Завершить

GET    /api/survival/cards     - Карты выживания
POST   /api/survival/play      - Сыграть карту
POST   /api/survival/resist    - Сопротивление
GET    /api/survival/status/:sessionId

GET    /api/trade/items        - Доступные товары
GET    /api/trade/offers       - Рыночные предложения
POST   /api/trade/offers       - Создать предложение
POST   /api/trade/offers/:id/accept
POST   /api/trade/offers/:id/reject
```

## Запуск проекта

### Backend

```bash
cd c:\NewNotAloneGame
dotnet run
# Слушает на http://localhost:5000
```

### Frontend

```bash
cd c:\NewNotAloneGame\frontend
npm install
npm start
# Откроется на http://localhost:3000
```

## Технологический стек

### Backend
- .NET 10
- Entity Framework Core (EF 10)
- ASP.NET Core Web API
- JWT (System.IdentityModel.Tokens.Jwt)

### Frontend
- React 18
- React Router v6
- Axios
- CSS3

## Основные функции

### Авторизация (Auth)
- Регистрация новых пользователей
- Вход с JWT токеном
- Защита маршрутов (PrivateRoute)
- Автоматическое управление сессией

### Игровая логика (Game)
- Создание игровых сессий
- Управление раундами
- История игр
- Статус сессии

### Выживание (Survival)
- Игровые карты выживания
- Система действий (play, resist)
- Отслеживание статуса выживания
- Доступные действия в раунде

### Торговля (Trade)
- Рыночные предложения
- Инвентарь
- История торговли
- Принятие/отклонение предложений

## CORS Конфигурация

Backend должен иметь CORS конфигурацию для Frontend:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

app.UseCors("AllowReactApp");
```

## Отладка

### Frontend
- Используйте React DevTools
- Chrome DevTools для сетевых запросов
- Проверьте localStorage для JWT токена

### Backend
- Используйте Visual Studio Debug
- Проверьте логи в консоли

## Следующие шаги

1. Дополнить страницы Survival и Trade
2. Добавить WebSocket для реал-тайма
3. Реализовать notification system
4. Добавить unit тесты
5. Оптимизировать производительность
