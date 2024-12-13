using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Application.Services;
using Discord.Commands.Modules.Helpers;

namespace Discord.Commands.Modules;

public sealed partial class PracticeModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<PracticeModule> _logger;
    private readonly ITrainingSessionService _trainingSessionService;
    private readonly IUserNotificationService _userNotificationService;

    public PracticeModule(
        ILogger<PracticeModule> logger,
        ITrainingSessionService trainingSessionService,
        IUserNotificationService userNotificationService)
    {
        _logger = logger;
        _trainingSessionService = trainingSessionService;
        _userNotificationService = userNotificationService;
    }

    [SlashCommand("practice", "Create a practice")]
    public async Task CreatePractice(
        Tracks track,
        string date,
        TimeSlots time,
        int driversRequired,
        Roles role,
        QualifyingFormat qualifyingFormat,
        RaceFormat raceFormat,
        string? comment = null)
    {
        await DeferAsync();

        var roles = Context.Guild.Roles
            .Where(r =>
                r.Name.Contains(role.ToString(), StringComparison.InvariantCultureIgnoreCase) &&
                r.Name.Contains("Driver", StringComparison.InvariantCultureIgnoreCase));

        var messageContent = PracticeHelpers.CreateTrainingMessage(
            track, date, time, driversRequired, role, qualifyingFormat, raceFormat, roles, comment
        );

        var components = PracticeHelpers.BuildActionComponents();

        var followupMessage =
            await FollowupAsync(messageContent, components: components, allowedMentions: AllowedMentions.All);

        await _trainingSessionService.RegisterNewSessionAsync(followupMessage.Id, Context.User.Id, Context.Guild?.Id,
            Context.Channel?.Id);

        await PracticeHelpers.AddSessionReactionsAsync(followupMessage);
    }

    [ComponentInteraction("cancel_training")]
    public async Task CancelTraining()
    {
        var interaction = (IComponentInteraction)Context.Interaction;
        var messageId = interaction.Message.Id;
        var userId = interaction.User.Id;

        var cancellationResult = await _trainingSessionService.CancelSessionAsync(messageId, userId);

        if (!cancellationResult.IsSuccessful)
        {
            await RespondAsync(cancellationResult.ErrorMessage, ephemeral: true);
            return;
        }

        await PracticeHelpers.MarkPracticeMessageAsCanceledAsync(interaction.Message);

        List<Emoji> emojis =
        [
            new Emoji("\u2705"), // ✅
            new Emoji("\u2753") // ❓
        ];

        var users = (await Task.WhenAll(emojis.Select(e =>
                interaction.Message.GetReactionUsersAsync(e, int.MaxValue).FlattenAsync())))
            .SelectMany(u => u)
            .DistinctBy(u => u.Id);

        var notificationMessage =
            "Trénink na GRL zrušený. Klidně založ svůj vlastní v kanálu pro tréninkové registrace.";

        await _userNotificationService.NotifyUsersAsync(users, notificationMessage);
    }

    public enum Tracks
    {
        [ChoiceDisplay("Bahrain")] Bahrain,
        [ChoiceDisplay("Jeddah")] Jeddah,
        [ChoiceDisplay("Australia")] Australia,
        [ChoiceDisplay("Japan")] Japan,
        [ChoiceDisplay("China")] China,
        [ChoiceDisplay("Miami")] Miami,
        [ChoiceDisplay("Imola")] Imola,
        [ChoiceDisplay("Monaco")] Monaco,
        [ChoiceDisplay("Canada")] Canada,
        [ChoiceDisplay("Spain")] Spain,
        [ChoiceDisplay("Austria")] Austria,
        [ChoiceDisplay("Great Britain")] GreatBritain,
        [ChoiceDisplay("Hungary")] Hungary,
        [ChoiceDisplay("Belgium")] Belgium,
        [ChoiceDisplay("Netherlands")] Netherlands,
        [ChoiceDisplay("Monza")] Monza,
        [ChoiceDisplay("Azerbaijan")] Azerbaijan,
        [ChoiceDisplay("Singapore")] Singapore,
        [ChoiceDisplay("Texas")] Texas,
        [ChoiceDisplay("Mexico")] Mexico,
        [ChoiceDisplay("Las Vegas")] LasVegas,
        [ChoiceDisplay("Abu Dhabi")] AbuDhabi
    }

    public enum RaceFormat
    {
        [ChoiceDisplay("5 laps")] VeryShort,
        [ChoiceDisplay("25%")] Short,
        [ChoiceDisplay("35%")] Medium,
        [ChoiceDisplay("50%")] Long,
        [ChoiceDisplay("100%")] Full
    }

    public enum QualifyingFormat
    {
        [ChoiceDisplay("None")] None,
        [ChoiceDisplay("One Shot")] OneShot,
        [ChoiceDisplay("Short")] Short,
        [ChoiceDisplay("Full")] Full
    }

    [Flags]
    public enum Roles
    {
        [ChoiceDisplay("Rookie")] Rookie = 1,
        [ChoiceDisplay("Junior")] Junior = 2,
        [ChoiceDisplay("Talent")] Talent = 4,
        [ChoiceDisplay("Academy")] Academy = 8,
        [ChoiceDisplay("Main")] Main = 16,
        [ChoiceDisplay("Driver")] Driver = 32
    }

    public enum TimeSlots
    {
        [ChoiceDisplay("Upřesníme později")] Tba,
        [ChoiceDisplay("16:00")] _1600,
        [ChoiceDisplay("16:30")] _1630,
        [ChoiceDisplay("17:00")] _1700,
        [ChoiceDisplay("17:30")] _1730,
        [ChoiceDisplay("18:00")] _1800,
        [ChoiceDisplay("18:30")] _1830,
        [ChoiceDisplay("19:00")] _1900,
        [ChoiceDisplay("19:30")] _1930,
        [ChoiceDisplay("20:00")] _2000,
        [ChoiceDisplay("20:30")] _2030,
        [ChoiceDisplay("21:00")] _2100,
        [ChoiceDisplay("21:30")] _2130,
        [ChoiceDisplay("22:00")] _2200,
        [ChoiceDisplay("22:30")] _2230,
        [ChoiceDisplay("23:00")] _2300
    }

    [GeneratedRegex(@"\s")]
    private static partial Regex MyRegex();
}