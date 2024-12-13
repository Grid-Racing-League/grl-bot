using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ITrainingSessionService, TrainingSessionService>();
        services.AddSingleton<IUserNotificationService, UserNotificationService>();

        return services;
    }
}