using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace Discord.Commands.Modules;

public class TestModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<TestModule> _logger;

    public TestModule(ILogger<TestModule> logger)
    {
        _logger = logger;
    }

    [SlashCommand("hi", "Tells you to fuck off.")]
    public async Task TestCommand(string? hi = null)
    {
        await DeferAsync();

        if (hi is not null)
        {
            await FollowupAsync("Oh hello!");
        }

        await FollowupAsync("Fuck off mate.");
    }
}