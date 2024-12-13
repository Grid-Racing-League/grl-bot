using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Mappings;

namespace Persistence.Repositories;

internal sealed class TrainingSessionRepository : ITrainingSessionRepository
{
    private readonly GrlBotDbContext _context;

    public TrainingSessionRepository(IDbContextFactory<GrlBotDbContext> dbContextFactory)
    {
        _context = dbContextFactory.CreateDbContext();
    }

    public async Task AddAsync(Domain.TrainingSession session)
    {
        var model = session.ToModel();
        model.Id = Guid.NewGuid();
        model.CreatedAt = DateTime.UtcNow;
        await _context.TrainingSessions.AddAsync(model);
    }

    public async Task<Domain.TrainingSession?> GetByMessageIdAsync(ulong messageId)
    {
        var model = await _context.TrainingSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(ts => ts.MessageId == messageId);

        return model?.ToDomain();
    }

    public async Task RemoveAsync(ulong messageId)
    {
        var model = await _context.TrainingSessions.FirstOrDefaultAsync(ts => ts.MessageId == messageId);

        if (model is null)
        {
            return;
        }

        _context.TrainingSessions.Remove(model);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}