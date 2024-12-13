using Microsoft.EntityFrameworkCore;

namespace Persistence;

internal class GrlBotDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Models.TrainingSession> TrainingSessions { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GrlBotDbContext).Assembly);
    }
}