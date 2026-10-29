namespace Feedback.Infrastructure.Persistence;

/// <summary>Налаштування підключення (секція MongoDb в appsettings.json). Параметри пулу й таймаутів задаються в рядку підключення.</summary>
public class MongoDbOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017/?maxPoolSize=50&serverSelectionTimeoutMS=5000";
    public string DatabaseName { get; set; } = "outfitFeedbackDb";

    /// <summary>Створювати індекси й наповнювати початковими даними під час старту застосунку.</summary>
    public bool InitializeOnStartup { get; set; } = true;
}
