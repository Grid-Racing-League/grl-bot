using Discord.BotConfiguration.Extensions;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Discord.Commands.Services;

public sealed class InteractionHandlingService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _commands;
    private readonly ILogger<InteractionHandlingService> _logger;
    private readonly IServiceProvider _services;

    public InteractionHandlingService(IServiceProvider services, DiscordSocketClient client,
        InteractionService commands,
        ILogger<InteractionHandlingService> logger)
    {
        _services = services;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _commands.Log += LoggerHelper.LogAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Ready += async () =>
        {
            await _commands.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Commands registered globally.");
        };

        _client.InteractionCreated += OnInteractionAsync;

        _logger.LogInformation("Registering commands...");
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            await _commands.AddModulesAsync(assembly, _services);
        }

        _commands.InteractionExecuted += InteractionExecutedAsync;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _commands.Dispose();
        return Task.CompletedTask;
    }

    private async Task OnInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);

            // Execute the interaction command and log results
            var result = await _commands.ExecuteCommandAsync(context, _services);

            if (result.IsSuccess is false)
            {
                _logger.LogError("Error handling interaction: {ErrorMessage}", result.ErrorReason);

                if (interaction is SocketMessageComponent)
                {
                    await interaction.RespondAsync($"An error occurred: {result.ErrorReason}", ephemeral: true);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling interaction.");

            if (interaction.Type is InteractionType.ApplicationCommand)
            {
                try
                {
                    var response = await interaction.GetOriginalResponseAsync();
                    await response.DeleteAsync();
                }
                catch
                {
                    // Ignore deletion errors
                }
            }
        }
    }

    private Task InteractionExecutedAsync(ICommandInfo? commandInfo, IInteractionContext interactionContext,
        Interactions.IResult result)
    {
        _logger.LogInformation("Interaction executed: {CommandName}. Server: {Server}. User: {User}",
            commandInfo?.Name ?? "Unknown",
            interactionContext.Guild?.Name ?? "Direct Message",
            interactionContext.User?.Username ?? "Unknown");

        return Task.CompletedTask;
    }
}