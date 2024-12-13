using Discord;

namespace Application.Services;

public interface IUserNotificationService
{
    Task NotifyUsersAsync(IEnumerable<IUser> users, string notificationMessage);
}