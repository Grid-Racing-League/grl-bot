namespace Application.Services;


public interface ITrainingSessionService
{
    Task RegisterNewSessionAsync(ulong messageId, ulong creatorId, ulong? guildId, ulong? channelId);
    Task<(bool IsSuccessful, string? ErrorMessage)> CancelSessionAsync(ulong messageId, ulong userId);
}