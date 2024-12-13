using MongoDB.Driver;

namespace Persistence;

internal class GrlBotDbContext
{
    private readonly IMongoDatabase _database;

    public GrlBotDbContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
        
        TrainingSessions.SetupTrainingSessionIndexes();
    }

    public IMongoCollection<Models.TrainingSession> TrainingSessions =>
        _database.GetCollection<Models.TrainingSession>("TrainingSessions");
}

internal static class GrlBotDbContextExtensions
{
    public static void SetupTrainingSessionIndexes(this IMongoCollection<Models.TrainingSession> collection)
    {
        var retentionPeriod = TimeSpan.FromDays(14);
        
        var indexKeys = Builders<Models.TrainingSession>.IndexKeys.Ascending(ts => ts.CreatedAt);
        var indexOptions = new CreateIndexOptions
        {
            ExpireAfter = retentionPeriod
        };
        var indexModel = new CreateIndexModel<Models.TrainingSession>(indexKeys, indexOptions);

        collection.Indexes.CreateOne(indexModel);
    }
}