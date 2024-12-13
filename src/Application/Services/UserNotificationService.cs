using Discord;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class UserNotificationService : IUserNotificationService
{
    private readonly ILogger<UserNotificationService> _logger;
    
    public UserNotificationService(ILogger<UserNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task NotifyUsersAsync(IEnumerable<IUser> users, string notificationMessage)
    {
        var notifiedUserIds = new HashSet<ulong>();

        foreach (var user in users)
        {
            if (user.IsBot || notifiedUserIds.Contains(user.Id))
            {
                continue;
            }

            try
            {
                var dmChannel = await user.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync(notificationMessage);
                notifiedUserIds.Add(user.Id);
            }
            catch (Exception e)
            {
                _logger.LogError("Something went wrong while notifying user {UserId}: {Error}", user.Id, e.Message);
            }
        }
    }
}