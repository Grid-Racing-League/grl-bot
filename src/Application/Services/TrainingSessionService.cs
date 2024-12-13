using Domain;
using Domain.Repositories;

namespace Application.Services;

public class TrainingSessionService : ITrainingSessionService
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;

    public TrainingSessionService(ITrainingSessionRepository trainingSessionRepository)
    {
        _trainingSessionRepository = trainingSessionRepository;
    }

    public async Task RegisterNewSessionAsync(ulong messageId, ulong creatorId, ulong? guildId, ulong? channelId)
    {
        await _trainingSessionRepository.AddAsync(new TrainingSession
        {
            MessageId = messageId,
            CreatorId = creatorId,
            GuildId = guildId,
            ChannelId = channelId
        });

        await _trainingSessionRepository.SaveChangesAsync();
    }

    public async Task<(bool IsSuccessful, string? ErrorMessage)> CancelSessionAsync(ulong messageId, ulong userId)
    {
        var session = await _trainingSessionRepository.GetByMessageIdAsync(messageId);

        if (session is null)
        {
            return (false, "Tenhle trénink neexistuje nebo už byl zrušen.");
        }

        if (userId != session.CreatorId)
        {
            return (false, "Tenhle trénink nemůžeš zrušit.");
        }

        await _trainingSessionRepository.RemoveAsync(messageId);
        return (true, null);
    }
}