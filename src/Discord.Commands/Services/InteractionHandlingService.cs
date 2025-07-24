using Discord.BotConfiguration;
using Discord.BotConfiguration.Extensions;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Commands.Services;

public sealed class InteractionHandlingService : IHostedService, IAsyncDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _commands;
    private readonly ILogger<InteractionHandlingService> _logger;
    private readonly IServiceProvider _services;
    private readonly WhitelistConfiguration _whitelistConfig;
    private bool _isDisposed;

    public InteractionHandlingService(
        IServiceProvider services,
        DiscordSocketClient client,
        InteractionService commands,
        ILogger<InteractionHandlingService> logger,
        IOptions<WhitelistConfiguration> whitelistConfig)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _whitelistConfig = whitelistConfig?.Value ?? throw new ArgumentNullException(nameof(whitelistConfig));

        _commands.Log += LoggerHelper.LogAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Subscribe to client ready event to register commands
            _client.Ready += ClientReadyAsync;

            // Subscribe to interaction events
            _client.InteractionCreated += OnInteractionAsync;

            // Register command modules
            await RegisterCommandModulesAsync(cancellationToken);

            // Subscribe to interaction execution events
            _commands.InteractionExecuted += InteractionExecutedAsync;

            _logger.LogInformation("InteractionHandlingService started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start InteractionHandlingService");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_isDisposed)
            {
                return;
            }

            // Unsubscribe from events
            _client.Ready -= ClientReadyAsync;
            _client.InteractionCreated -= OnInteractionAsync;
            _commands.InteractionExecuted -= InteractionExecutedAsync;

            _logger.LogInformation("InteractionHandlingService stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping InteractionHandlingService");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _commands.Dispose();

        await StopAsync(CancellationToken.None);

        GC.SuppressFinalize(this);
    }

    private async Task ClientReadyAsync()
    {
        try
        {
            await _commands.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Commands registered globally successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register commands globally");
        }
    }

    private async Task RegisterCommandModulesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Registering command modules...");

            // Get only relevant assemblies to avoid searching through all loaded assemblies
            var relevantAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic &&
                           (a.FullName?.Contains("Discord") is true ||
                            a.FullName?.Contains("GRL") is true))
                .ToList();

            _logger.LogDebug("Found {Count} relevant assemblies for command registration", relevantAssemblies.Count);

            foreach (var assembly in relevantAssemblies)
            {
                try
                {
                    await _commands.AddModulesAsync(assembly, _services);
                    _logger.LogDebug("Registered commands from assembly: {Assembly}", assembly.GetName().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register commands from assembly: {Assembly}",
                        assembly.GetName().Name);
                }
            }

            var registeredCommands = _commands.SlashCommands.Count +
                                    _commands.ContextCommands.Count +
                                    _commands.ComponentCommands.Count;

            _logger.LogInformation("Successfully registered {Count} commands", registeredCommands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register command modules");
            throw;
        }
    }

    private async Task OnInteractionAsync(SocketInteraction? interaction)
    {
        if (interaction is null)
        {
            _logger.LogWarning("Received null interaction");
            return;
        }

        var context = new SocketInteractionContext(_client, interaction);
        var userInfo = $"{context.User?.Username ?? "Unknown"} ({context.User?.Id})";
        var serverInfo = context.Guild != null ? $"{context.Guild.Name} ({context.Guild.Id})" : "Direct Message";

        _logger.LogDebug("Received interaction from user {User} in {Server}", userInfo, serverInfo);

        try
        {
            // Check if the interaction is from an authorized server
            if (context.Guild is not null && !IsServerAuthorized(context.Guild.Id))
            {
                await HandleUnauthorizedServerAsync(context, interaction);
                return;
            }

            // Execute the interaction command
            var result = await _commands.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
            {
                await HandleFailedInteractionAsync(interaction, result);
            }
        }
        catch (Exception ex)
        {
            await HandleInteractionExceptionAsync(interaction, ex);
        }
    }

    private bool IsServerAuthorized(ulong guildId)
    {
        if (_whitelistConfig.Servers.Length is 0)
        {
            _logger.LogWarning("Whitelist configuration contains no servers");
            return false;
        }

        return _whitelistConfig.Servers.Contains(guildId);
    }

    private async Task HandleUnauthorizedServerAsync(SocketInteractionContext context, SocketInteraction interaction)
    {
        var userInfo = $"{context.User?.Username ?? "Unknown"} ({context.User?.Id})";
        var serverInfo = context.Guild != null ? $"{context.Guild.Name} ({context.Guild.Id})" : "Unknown";

        _logger.LogInformation("Access denied: User {User} attempted to use command in unauthorized server {Server}",
            userInfo, serverInfo);

        // Create the message with instructions
        var message = "Tento server není na seznamu autorizovaných serverů pro tento bot. " +
                      "Pro přidání serveru na whitelist kontaktujte philnexes přímo přes Discord.";

        try
        {
            // Respond based on interaction state
            if (!interaction.HasResponded)
            {
                await interaction.RespondAsync(message, ephemeral: true);
            }
            else
            {
                await interaction.FollowupAsync(message, ephemeral: true);
            }

            _logger.LogInformation("Authorization message sent to {User} in unauthorized server {Server}",
                userInfo, serverInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send unauthorized server message to {User} in {Server}",
                userInfo, serverInfo);
        }
    }

    private async Task HandleFailedInteractionAsync(SocketInteraction? interaction, Interactions.IResult result)
    {
        _logger.LogError("Error handling interaction: {ErrorType} - {ErrorMessage}",
            result.Error, result.ErrorReason);

        var errorMessage = "An error occurred while processing your request.";

        // Customize error message based on error type
        if (result.Error.HasValue)
        {
            errorMessage = result.Error.Value switch
            {
                InteractionCommandError.UnmetPrecondition => "You don't have permission to use this command.",
                InteractionCommandError.UnknownCommand => "This command is not available.",
                InteractionCommandError.BadArgs => "Invalid command arguments provided.",
                InteractionCommandError.Exception => "An error occurred while processing the command.",
                InteractionCommandError.Unsuccessful => "The command could not be executed successfully.",
                _ => $"An error occurred: {result.ErrorReason}"
            };
        }

        // Respond with error message if the interaction hasn't been responded to yet
        try
        {
            if (interaction is { HasResponded: false })
            {
                await interaction.RespondAsync(errorMessage, ephemeral: true);
            }
            else if (interaction is SocketMessageComponent)
            {
                await interaction.FollowupAsync(errorMessage, ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send error response for interaction");
        }
    }

    private async Task HandleInteractionExceptionAsync(SocketInteraction? interaction, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception processing interaction {InteractionId}",
            interaction.Id);

        try
        {
            // If interaction hasn't been responded to, send an error message
            if (!interaction.HasResponded)
            {
                await interaction.RespondAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
            }

            // Clean up response for application commands if needed
            else if (interaction.Type is InteractionType.ApplicationCommand)
            {
                try
                {
                    var response = await interaction.GetOriginalResponseAsync();
                    await response.DeleteAsync();
                    await interaction.FollowupAsync("An unexpected error occurred. Please try again later.", ephemeral: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up interaction response");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to handle interaction exception");
        }
    }

    private Task InteractionExecutedAsync(ICommandInfo? commandInfo, IInteractionContext interactionContext,
        Interactions.IResult result)
    {
        var commandName = commandInfo?.Name ?? "Unknown";
        var moduleName = commandInfo?.Module?.Name ?? "Unknown";
        var userInfo = $"{interactionContext.User?.Username ?? "Unknown"} ({interactionContext.User?.Id})";
        var serverInfo = interactionContext.Guild != null ?
            $"{interactionContext.Guild.Name} ({interactionContext.Guild.Id})" : "Direct Message";

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Interaction executed successfully: {CommandName} in module {ModuleName}. Server: {Server}. User: {User}",
                commandName, moduleName, serverInfo, userInfo);
        }
        else
        {
            _logger.LogWarning(
                "Interaction failed: {CommandName} in module {ModuleName}. Error: {ErrorType} - {ErrorMessage}. Server: {Server}. User: {User}",
                commandName, moduleName, result.Error, result.ErrorReason, serverInfo, userInfo);
        }

        return Task.CompletedTask;
    }
}
