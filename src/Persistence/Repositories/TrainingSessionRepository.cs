
using Domain.Repositories;
using MongoDB.Driver;
using Persistence.Mappings;
using Persistence.Models;

namespace Persistence.Repositories;

internal sealed class TrainingSessionRepository : ITrainingSessionRepository
{
    private readonly IMongoCollection<TrainingSession> _trainingSessions;

    public TrainingSessionRepository(GrlBotDbContext dbContext)
    {
        _trainingSessions = dbContext.TrainingSessions;
    }

    public async Task AddAsync(Domain.TrainingSession session)
    {
        var model = session.ToModel();
        model.CreatedAt = DateTime.UtcNow;
        await _trainingSessions.InsertOneAsync(model);
    }

    public async Task<Domain.TrainingSession?> GetByMessageIdAsync(ulong messageId)
    {
        var filter = Builders<TrainingSession>.Filter.Eq(ts => ts.MessageId, messageId);
        var model = await _trainingSessions.Find(filter).FirstOrDefaultAsync();
        return model?.ToDomain();
    }

    public async Task RemoveAsync(ulong messageId)
    {
        var filter = Builders<TrainingSession>.Filter.Eq(ts => ts.MessageId, messageId);
        await _trainingSessions.DeleteOneAsync(filter);
    }
}