using System.Globalization;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace Discord.Commands.Modules;

public class MessageModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<MessageModule> _logger;

    public MessageModule(ILogger<MessageModule> logger)
    {
        _logger = logger;
    }
    
    [SlashCommand("prune", "Prune messages from recent until a specific date")]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [CommandContextType(InteractionContextType.Guild)]
    public async Task PruneMessages(string dateTo, bool ignoreFirstMessage = true)
    {
        await DeferAsync();
        string[] formats = {
            "dd.MM.yyyy", "d.M.yyyy", "dd.M.yyyy", "d.MM.yyyy", 
            "d.M.yy", "dd.M.yy", "d.MM.yy", "dd.MM.yy",
            "d/MM/yyyy", "dd/MM/yyyy", "d-MM-yyyy", "dd-MM-yyyy"
        };
        
        if (!DateTime.TryParseExact(dateTo, formats, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime date))
        {
            await FollowupAsync("Invalid date format. Please use the dd.MM.yyyy format.", ephemeral: true);
            return;
        }

        var m = Context.Channel.GetMessagesAsync();
        var messages = (await Context.Channel.GetMessagesAsync().FlattenAsync())
            .Where(m => m.Timestamp.DateTime >= date)
            .ToList();
        
        if (ignoreFirstMessage)
        {
            messages.RemoveAt(messages.Count - 1);
        }
        
        foreach (var message in messages)
        {
            await Context.Channel.DeleteMessageAsync(message.Id, new RequestOptions {AuditLogReason = $"Removing messages in {Context.Channel.Name} up to {dateTo}"});
        }
    }
}