using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Domain;
using Domain.Repositories;

namespace Discord.Commands.Modules;

public sealed partial class PracticeModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<PracticeModule> _logger;
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private static readonly List<IEmote> NotifyEmojis =
    [
        new Emoji("\u2705"),    // ✅ White check mark
        new Emoji("\u2753")     // ❓ Question mark
    ];


    public PracticeModule(ILogger<PracticeModule> logger, ITrainingSessionRepository trainingSessionRepository)
    {
        _logger = logger;
        _trainingSessionRepository = trainingSessionRepository;
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
                r.Name.Contains($"{role}", StringComparison.InvariantCultureIgnoreCase) &&
                r.Name.Contains("Driver", StringComparison.InvariantCultureIgnoreCase));

        var flagEmoji = GetFlagEmoji(track);
        var formattedTrackName = GetFormattedTrackName(track);
        var formattedTimeSlot = GetFormattedTimeSlot(time);
        var formattedQualifying = GetFormattedQualifyingFormat(qualifyingFormat);
        var formattedRace = GetFormattedRaceFormat(raceFormat);

        var commentMessage = !string.IsNullOrEmpty(comment) ? $"\n\n*{comment}*" : "";

        var message = ComposeFinalMessage(date, driversRequired, roles, flagEmoji, formattedTrackName,
            formattedTimeSlot, formattedQualifying, formattedRace, commentMessage);

        var components = new ComponentBuilder()
            .WithButton("Zrušit trénink", "cancel_training", ButtonStyle.Danger)
            .Build();

        var followupMessage = await FollowupAsync(message, components: components, allowedMentions: AllowedMentions.All);

        await _trainingSessionRepository.AddAsync(new TrainingSession
        {
            MessageId = followupMessage.Id,
            CreatorId = Context.User.Id,
            GuildId = Context.Guild?.Id,
            ChannelId = Context.Channel?.Id
        });

        var checkMark = new Emoji("\u2705");
        var questionMark = new Emoji("\u2753");

        await followupMessage.AddReactionAsync(checkMark);
        await followupMessage.AddReactionAsync(questionMark);
    }


    private static string ComposeFinalMessage(string date, int driversRequired, IEnumerable<SocketRole> roles,
        string flagEmoji, string formattedTrackName, string formattedTimeSlot, string formattedQualifying,
        string formattedRace, string commentMessage)
    {
        return $@"
{flagEmoji} {formattedTrackName} - trénink {flagEmoji} 

🕗 {date} {formattedTimeSlot} 🕗

🏎️  {formattedQualifying} Q - {formattedRace} Race 🏎️

🛠️ Ligový assisty 🛠️

{PingRoles(roles)}

Dobrovolná účast, prosím potvrď
Trénink proběhne při účasti alespoň {driversRequired} pilotů
{commentMessage}
";
    }
    
    [ComponentInteraction("cancel_training")]
    public async Task CancelTraining()
    {
        var interaction = (IComponentInteraction)Context.Interaction;
        var messageId = interaction.Message.Id;

        var message = interaction.Message;

        var session = await _trainingSessionRepository.GetByMessageIdAsync(messageId);
        
        if (session is null)
        {
            await RespondAsync("Tenhle trénink neexistuje nebo už byl zrušen.", ephemeral: true);
            return;
        }

        if (Context.User.Id != session.CreatorId)
        {
            await RespondAsync("Tenhle trénink nemůžeš zrušit.", ephemeral: true);
            return;
        }

        await UpdateMessageAsCanceled(message);
        await NotifyReactingUsers();
        await _trainingSessionRepository.RemoveAsync(messageId);
    }
    
    private async Task NotifyReactingUsers()
    {
        var interaction = (IComponentInteraction)Context.Interaction;
        var message = interaction.Message;

        var notifiedUsers = new List<string>();

        foreach (var emoji in NotifyEmojis)
        {
            var users = await message.GetReactionUsersAsync(emoji, int.MaxValue).FlattenAsync();

            foreach (var user in users)
            {
                if (user.IsBot || notifiedUsers.Contains(user.Username))
                    continue;

                try
                {
                    await NotifyUser(user);
                    notifiedUsers.Add(user.Username);
                }
                catch
                {
                    // ignore errors
                }
            }
        }
    }

    private async Task NotifyUser(IUser user)
    {
        var dmChannel = await user.CreateDMChannelAsync();

        await dmChannel.SendMessageAsync(
            $"Čau {user.Username}, někdo právě zrušil trénink na GRL, tak se nelekej. Klidně založ svůj. Stačí vlézt do [#treninkove-registrace](https://discord.com/channels/706625870269251625/1294748282265927762) a založit vlastní.");
    }

    private async Task UpdateMessageAsCanceled(IUserMessage message)
    {
        await message.ModifyAsync(msg =>
        {
            msg.Content = "🚫 **TRÉNINK ZRUŠEN**";
            msg.Components = new ComponentBuilder().Build();
        });
    }

    private static string PingRoles(IEnumerable<SocketRole> roles)
    {
        return string.Join(" ", roles.Select(r => r.Mention));
    }

    // Method to return a properly formatted track name based on the ChoiceDisplay attribute
    private static string GetFormattedTrackName(Tracks track)
    {
        var trackInfo = track.GetType().GetField(track.ToString());
        var choiceDisplayAttr = trackInfo?.GetCustomAttribute<ChoiceDisplayAttribute>();
        return choiceDisplayAttr?.Name ?? track.ToString();
    }

    private static string GetFormattedTimeSlot(TimeSlots time)
    {
        var timeInfo = time.GetType().GetField(time.ToString());
        var choiceDisplayAttr = timeInfo?.GetCustomAttribute<ChoiceDisplayAttribute>();
        return choiceDisplayAttr?.Name ?? time.ToString();
    }

    // Method to get formatted race format
    private static string GetFormattedRaceFormat(RaceFormat format)
    {
        var formatInfo = format.GetType().GetField(format.ToString());
        var choiceDisplayAttr = formatInfo?.GetCustomAttribute<ChoiceDisplayAttribute>();
        return choiceDisplayAttr?.Name ?? format.ToString();
    }

    // Method to get formatted qualifying format
    private static string GetFormattedQualifyingFormat(QualifyingFormat format)
    {
        var formatInfo = format.GetType().GetField(format.ToString());
        var choiceDisplayAttr = formatInfo?.GetCustomAttribute<ChoiceDisplayAttribute>();
        return choiceDisplayAttr?.Name ?? format.ToString();
    }

    private static string GetFlagEmoji(Tracks track)
    {
        return track switch
        {
            Tracks.Bahrain => ":flag_bh:",
            Tracks.Jeddah => ":flag_sa:",
            Tracks.Australia => ":flag_au:",
            Tracks.Japan => ":flag_jp:",
            Tracks.China => ":flag_cn:",
            Tracks.Miami => ":flag_us:",
            Tracks.Imola => ":flag_it:",
            Tracks.Monaco => ":flag_mc:",
            Tracks.Canada => ":flag_ca:",
            Tracks.Spain => ":flag_es:",
            Tracks.Austria => ":flag_at:",
            Tracks.GreatBritain => ":flag_gb:",
            Tracks.Hungary => ":flag_hu:",
            Tracks.Belgium => ":flag_be:",
            Tracks.Netherlands => ":flag_nl:",
            Tracks.Monza => ":flag_it:",
            Tracks.Azerbaijan => ":flag_az:",
            Tracks.Singapore => ":flag_sg:",
            Tracks.Texas => ":flag_us:",
            Tracks.Mexico => ":flag_mx:",
            Tracks.LasVegas => ":flag_us:",
            Tracks.AbuDhabi => ":flag_ae:",
            _ => throw new ArgumentOutOfRangeException(nameof(track), track, message: null)
        };
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