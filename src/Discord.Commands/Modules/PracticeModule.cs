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
        string time,
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
            Creator = Context.User,
            Comment = comment
        };

        var driverRoles = Context.Guild.Roles
            .Where(r => r.Name.Contains("Driver", StringComparison.InvariantCultureIgnoreCase) ||
                        r.Name.Contains("Rezerva", StringComparison.InvariantCultureIgnoreCase))
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
            socketRoles, data.Creator, data.Comment
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
        [ChoiceDisplay("Australia")] Australia,
        [ChoiceDisplay("China")] China,
        [ChoiceDisplay("Japan")] Japan,
        [ChoiceDisplay("Bahrain")] Bahrain,
        [ChoiceDisplay("Jeddah")] Jeddah,
        [ChoiceDisplay("Miami")] Miami,
        [ChoiceDisplay("Imola")] Imola,
        [ChoiceDisplay("Monaco")] Monaco,
        [ChoiceDisplay("Spain")] Spain,
        [ChoiceDisplay("Canada")] Canada,
        [ChoiceDisplay("Austria")] Austria,
        [ChoiceDisplay("Great Britain")] GreatBritain,
        [ChoiceDisplay("Belgium")] Belgium,
        [ChoiceDisplay("Hungary")] Hungary,
        [ChoiceDisplay("Netherlands")] Netherlands,
        [ChoiceDisplay("Monza")] Monza,
        [ChoiceDisplay("Azerbaijan")] Azerbaijan,
        [ChoiceDisplay("Singapore")] Singapore,
        [ChoiceDisplay("Texas")] Texas,
        [ChoiceDisplay("Mexico")] Mexico,
        [ChoiceDisplay("Brazil")] Brazil,
        [ChoiceDisplay("Las Vegas")] LasVegas,
        [ChoiceDisplay("Qatar")] Qatar,
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

    private class PracticeData
    {
        public Tracks Track { get; set; }
        public string Date { get; set; } = null!;
        public string Time { get; set; } = null!;
        public int DriversRequired { get; set; }
        public QualifyingFormat QualifyingFormat { get; set; }
        public RaceFormat RaceFormat { get; set; }
        public SocketUser Creator { get; set; }
        public string? Comment { get; set; }
    }
}