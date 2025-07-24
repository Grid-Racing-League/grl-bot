using Discord.BotConfiguration;
using Discord.BotConfiguration.Extensions;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Discord.Commands.Services;

public sealed class InteractionHandlingService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _commands;
    private readonly ILogger<InteractionHandlingService> _logger;
    private readonly IServiceProvider _services;
    private readonly WhitelistConfiguration _whitelistConfig;

    public InteractionHandlingService(
        IServiceProvider services,
        DiscordSocketClient client,
        InteractionService commands,
        ILogger<InteractionHandlingService> logger,
        IOptions<WhitelistConfiguration> whitelistConfig)
    {
        _services = services;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _whitelistConfig = whitelistConfig.Value ?? throw new ArgumentNullException(nameof(whitelistConfig));

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

            // Check if the interaction is not from an authorized server
            if (context.Guild is not null && !_whitelistConfig.Servers.Contains(context.Guild.Id))
            {
                await ExecutePrankAsync(context, interaction);
                return;
            }

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

    private async Task ExecutePrankAsync(SocketInteractionContext context, SocketInteraction interaction)
    {
        _logger.LogInformation("User {User} triggered command in unauthorized server {Server}. Showing contact message.",
            context.User.Username, context.Guild?.Name ?? "Unknown");

        // Create the message with better instructions
        var message = "Tento server není na seznamu autorizovaných serverů pro tento bot. " +
                      "Pro přidání serveru na whitelist kontaktujte philnexes přímo přes Discord. ";

        // Determine interaction type and respond appropriately
        if (interaction.Type is InteractionType.ApplicationCommand)
        {
            await interaction.RespondAsync(message, ephemeral: true);
        }
        else if (interaction is SocketMessageComponent messageComponent)
        {
            await messageComponent.RespondAsync(message, ephemeral: true);
        }
        else if (interaction is SocketModal modal)
        {
            await modal.RespondAsync(message, ephemeral: true);
        }

        _logger.LogInformation("Contact message displayed to {User} in unauthorized server {Server}",
            context.User.Username, context.Guild?.Name ?? "Unknown");
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
