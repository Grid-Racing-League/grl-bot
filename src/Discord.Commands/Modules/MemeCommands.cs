using Discord.Interactions;

namespace Discord.Commands.Modules;

public class MemeCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("roulette", "Can you survive the roulette?")]
    [RequireBotPermission(GuildPermission.KickMembers)]
    [RequireContext(ContextType.Guild)]
    public async Task Roulette()
    {
        await DeferAsync();

        var guildUser = Context.Guild.GetUser(Context.User.Id);
        
        var message = await FollowupAsync($"{guildUser.Username} si troufá... Tak schválně, jestli přežiješ ruskou ruletu...", ephemeral: false);
        await Task.Delay(1000);
        await ModifyOriginalResponseAsync(msg => msg.Content = "3...");
        
        await Task.Delay(1000);
        await ModifyOriginalResponseAsync(msg => msg.Content = "2...");
        
        await Task.Delay(1000);
        await ModifyOriginalResponseAsync(msg => msg.Content = "1...");
        
        await Task.Delay(1000);
        await ModifyOriginalResponseAsync(msg => msg.Content = ":gun: :boom:");
        
        if (guildUser.GuildPermissions.Administrator)
        {
            await FollowupAsync("Administrátoři jsou imunní vůči smrti!");
            return;
        }
        
        var survived = new Random().Next(1, 7) != 1;

        if (survived)
        {
            await FollowupAsync($":eye: `{guildUser.Username} přežil ruskou ruletu, opravdový sigma!`");
        }
        else
        {
            await guildUser.KickAsync();
            await FollowupAsync($":eye: `{guildUser.Username} se stál obětí ruské rulety!`");
        }
    }
}
