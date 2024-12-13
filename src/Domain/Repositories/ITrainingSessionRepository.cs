namespace Domain.Repositories;

public interface ITrainingSessionRepository
{
    Task AddAsync(TrainingSession session);
    Task<TrainingSession?> GetByMessageIdAsync(ulong messageId);
    Task RemoveAsync(ulong messageId);
    Task SaveChangesAsync();
}