using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Persistence.Repositories;

namespace Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoDb(configuration);

        services.AddTransient<ITrainingSessionRepository, TrainingSessionRepository>();

        return services;
    }

    private static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        var mongoSettings = MongoClientSettings.FromConnectionString(configuration.GetConnectionString("Database"));
        var mongoClient = new MongoClient(mongoSettings);

        services.AddSingleton<IMongoClient>(mongoClient);

        services.AddSingleton(_ =>
        {
            var databaseName = "grl-bot";
            return new GrlBotDbContext(mongoClient, databaseName);
        });

        return services;
    }
}