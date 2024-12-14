using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Application.Services;
using Discord.Commands.Modules.Helpers;

namespace Discord.Commands.Modules;

public sealed partial class PracticeModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<PracticeModule> _logger;
    private readonly ITrainingSessionService _trainingSessionService;
    private readonly IUserNotificationService _userNotificationService;

    // Temporary in-memory storage for parameters until roles are selected
    private static readonly Dictionary<ulong, PracticeData> PendingPracticeData = new();

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
        QualifyingFormat qualifyingFormat,
        RaceFormat raceFormat,
        string? comment = null)
    {
        await DeferAsync(ephemeral: true);

        // Store initial data
        PendingPracticeData[Context.User.Id] = new PracticeData
        {
            Track = track,
            Date = date,
            Time = time,
            DriversRequired = driversRequired,
            QualifyingFormat = qualifyingFormat,
            RaceFormat = raceFormat,
            Comment = comment
        };

        var driverRoles = Context.Guild.Roles
            .Where(r => r.Name.Contains("Driver", StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        var selectMenu = new SelectMenuBuilder()
            .WithCustomId("role_select_menu")
            .WithPlaceholder("Vyber role pro ping (nepovinné)")
            .WithMinValues(0)
            .WithMaxValues(driverRoles.Count == 0 ? 1 : driverRoles.Count);

        foreach (var role in driverRoles)
        {
            selectMenu.AddOption(role.Name, role.Id.ToString());
        }

        var builder = new ComponentBuilder()
            .WithSelectMenu(selectMenu)
            .WithButton("Pokračovat bez rolí", "no_roles_selected", ButtonStyle.Secondary);

        var messageText = driverRoles.Count == 0
            ? "Nenalezena žádná role k výběru, můžeš pokračovat bez rolí:"
            : "Vyber prosím role, které chceš pingnout nebo pokračuj bez výběru:";

        await FollowupAsync(messageText, components: builder.Build());
    }

    [ComponentInteraction("role_select_menu")]
    public async Task HandleRoleSelection(string[] selectedRoles)
    {
        await DeferAsync(ephemeral: true);

        if (!PendingPracticeData.TryGetValue(Context.User.Id, out var data))
        {
            await FollowupAsync("Data pro trénink nenalezena. Prosím spusť /practice znovu.", ephemeral: true);
            return;
        }

        var roles = selectedRoles
            .Select(id => Context.Guild.GetRole(ulong.Parse(id)))
            .Where(r => r != null)
            .Cast<SocketRole>()
            .ToList();

        await FinalizePracticeCreation(roles, data);
    }

    [ComponentInteraction("no_roles_selected")]
    public async Task HandleNoRolesSelected()
    {
        await DeferAsync(ephemeral: true);

        if (!PendingPracticeData.TryGetValue(Context.User.Id, out var data))
        {
            await FollowupAsync("Data pro trénink nenalezena. Prosím spusť /practice znovu.", ephemeral: true);
            return;
        }

        var roles = Enumerable.Empty<SocketRole>();
        await FinalizePracticeCreation(roles, data);
    }

    private async Task FinalizePracticeCreation(IEnumerable<SocketRole> roles, PracticeData data)
    {
        var socketRoles = roles.ToList();

        var messageContent = PracticeHelpers.CreateTrainingMessage(
            data.Track, data.Date, data.Time,
            data.DriversRequired, data.QualifyingFormat, data.RaceFormat,
            socketRoles, data.Comment
        );

        var components = PracticeHelpers.BuildActionComponents();

        var publicMessage = await Context.Channel.SendMessageAsync(
            messageContent,
            components: components,
            allowedMentions: AllowedMentions.All
        );

        await _trainingSessionService.RegisterNewSessionAsync(
            publicMessage.Id, Context.User.Id, Context.Guild?.Id, Context.Channel?.Id
        );

        await PracticeHelpers.AddSessionReactionsAsync(publicMessage);

        if (Context.Channel is ITextChannel textChannel)
        {
            await textChannel.CreateThreadAsync(
                "Diskuze zde",
                autoArchiveDuration: ThreadArchiveDuration.OneWeek,
                message: publicMessage
            );
        }

        PendingPracticeData.Remove(Context.User.Id);

        await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg =>
        {
            msg.Content = socketRoles.Any()
                ? $"{Context.User.Mention} vytvořil trénink! Role vybrány! Trénink vytvořen."
                : $"{Context.User.Mention} vytvořil trénink! Žádné role vybrány! Trénink vytvořen.";

            msg.Components = new ComponentBuilder().Build();
        });
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

        var users = (await Task.WhenAll(emojis
                .Select(e => interaction.Message.GetReactionUsersAsync(e, int.MaxValue).FlattenAsync())))
            .SelectMany(u => u)
            .DistinctBy(u => u.Id);

        var notificationMessage =
            "Trénink na GRL zrušený. Klidně založ svůj vlastní v kanálu pro [#tréninkové-registrace](https://discord.com/channels/706625870269251625/1294748282265927762).";

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

    private class PracticeData
    {
        public Tracks Track { get; set; }
        public string Date { get; set; } = null!;
        public TimeSlots Time { get; set; }
        public int DriversRequired { get; set; }
        public QualifyingFormat QualifyingFormat { get; set; }
        public RaceFormat RaceFormat { get; set; }
        public string? Comment { get; set; }
    }
}