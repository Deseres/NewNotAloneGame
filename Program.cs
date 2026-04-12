using NotAlone.Services; // Замени на свое пространство имен

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// РЕГИСТРИРУЕМ СЕРВИСЫ
builder.Services.AddSingleton<GameStore>();
builder.Services.AddSingleton<GameEngine>();
builder.Services.AddSingleton<SurvivalService>();
builder.Services.AddSingleton<TradeService>();

var app = builder.Build();

// 2. Настраиваем пайплайн (только для разработки)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Это создаст ту самую страницу в браузере
}

app.UseAuthorization();
app.MapControllers(); // Это связывает твои файлы в папке Controllers с URL

app.Run();