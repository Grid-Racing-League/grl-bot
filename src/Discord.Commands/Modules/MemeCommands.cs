using Discord.Interactions;

namespace Discord.Commands.Modules;


public class MemeCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("roulette", "Can you survive the roulette?")]
    public async Task Roulette()
    {
        await DeferAsync(ephemeral: true);

        var survived = new Random().Next(1, 7) != 1;

        if (survived)
        {
            await FollowupAsync($"{Context.User.Mention} survived the Russian roulette! *Click*", ephemeral: true);
        }
        else
        {
            await FollowupAsync($"{Context.User.Mention} didn't survive the Russian roulette! 💥", ephemeral: false);
        }
    }
}
