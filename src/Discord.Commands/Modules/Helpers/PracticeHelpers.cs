using System.Reflection;
using System.Text;
using Discord.Interactions;
using Discord.WebSocket;

namespace Discord.Commands.Modules.Helpers;

internal static class PracticeHelpers
{
    public static string CreateTrainingMessage(
        PracticeModule.Tracks track,
        string date,
        string time,
        int driversRequired,
        PracticeModule.QualifyingFormat qualifyingFormat,
        PracticeModule.RaceFormat raceFormat,
        IEnumerable<SocketRole> roles,
        SocketUser creator,
        string? comment)
    {
        var flagEmoji = GetFlagEmoji(track);
        var formattedTrackName = GetFormattedName(track);
        var formattedQualifying = GetFormattedName(qualifyingFormat);
        var formattedRace = GetFormattedName(raceFormat);

        var commentMessage = !string.IsNullOrEmpty(comment) ? $"\n\n*{comment}*" : "";

        var sb = new StringBuilder();
        sb.AppendLine($"{flagEmoji} {formattedTrackName} - trénink {flagEmoji} ");
        sb.AppendLine();
        sb.AppendLine($"🕗 {date} {time} 🕗");
        sb.AppendLine();
        sb.AppendLine($"🏎️  {formattedQualifying} Q - {formattedRace} Race 🏎️");
        sb.AppendLine();
        sb.AppendLine("🛠️ Ligový assisty 🛠️");
        sb.AppendLine();
        sb.AppendLine($"{PingRoles(roles)}");
        sb.AppendLine();
        sb.AppendLine("Dobrovolná účast, prosím potvrď");
        sb.AppendLine($"Trénink proběhne při účasti alespoň {driversRequired} pilotů");
        sb.Append(commentMessage);
        sb.AppendLine();
        sb.AppendLine($"*Trénink vytvořil:* {creator.Mention}");

        return sb.ToString();
    }

    public static MessageComponent BuildActionComponents()
    {
        return new ComponentBuilder()
            .WithButton("Zrušit trénink", "cancel_training", ButtonStyle.Danger)
            .Build();
    }

    public static async Task AddSessionReactionsAsync(IUserMessage message)
    {
        var checkMark = new Emoji("\u2705");
        var questionMark = new Emoji("\u2753");

        await message.AddReactionAsync(checkMark);
        await message.AddReactionAsync(questionMark);
    }

    public static async Task MarkPracticeMessageAsCanceledAsync(IUserMessage message)
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

    private static string GetFormattedName<TEnum>(TEnum value) where TEnum : Enum
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        var choiceDisplayAttr = fieldInfo?.GetCustomAttribute<ChoiceDisplayAttribute>();
        return choiceDisplayAttr?.Name ?? value.ToString();
    }

    private static string GetFlagEmoji(PracticeModule.Tracks track)
    {
        return track switch
        {
            PracticeModule.Tracks.Australia => ":flag_au:",
            PracticeModule.Tracks.China => ":flag_cn:",
            PracticeModule.Tracks.Japan => ":flag_jp:",
            PracticeModule.Tracks.Bahrain => ":flag_bh:",
            PracticeModule.Tracks.Jeddah => ":flag_sa:",
            PracticeModule.Tracks.Miami => ":flag_us:",
            PracticeModule.Tracks.Imola => ":flag_it:",
            PracticeModule.Tracks.Monaco => ":flag_mc:",
            PracticeModule.Tracks.Spain => ":flag_es:",
            PracticeModule.Tracks.Canada => ":flag_ca:",
            PracticeModule.Tracks.Austria => ":flag_at:",
            PracticeModule.Tracks.GreatBritain => ":flag_gb:",
            PracticeModule.Tracks.Belgium => ":flag_be:",
            PracticeModule.Tracks.Hungary => ":flag_hu:",
            PracticeModule.Tracks.Netherlands => ":flag_nl:",
            PracticeModule.Tracks.Monza => ":flag_it:",
            PracticeModule.Tracks.Azerbaijan => ":flag_az:",
            PracticeModule.Tracks.Singapore => ":flag_sg:",
            PracticeModule.Tracks.Texas => ":flag_us:",
            PracticeModule.Tracks.Mexico => ":flag_mx:",
            PracticeModule.Tracks.Brazil => ":flag_br:",
            PracticeModule.Tracks.LasVegas => ":flag_us:",
            PracticeModule.Tracks.Qatar => ":flag_qa:",
            PracticeModule.Tracks.AbuDhabi => ":flag_ae:",
            _ => ":checkered_flag:"
        };
    }
}