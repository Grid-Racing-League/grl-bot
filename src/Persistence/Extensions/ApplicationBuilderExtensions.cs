using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Persistence.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void ApplyMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GrlBotDbContext>();
        dbContext.Database.Migrate();
    }
}
