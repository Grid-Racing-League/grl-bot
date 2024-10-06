using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace Discord.Commands.Modules;

public sealed class TestModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<TestModule> _logger;

    public TestModule(ILogger<TestModule> logger)
    {
        _logger = logger;
    }

    [SlashCommand("practice", "Create a practice")]
    public async Task CreatePractice(Tracks track, string date, string time, int driversRequired, Roles role, string? comment = null)
    {
        await DeferAsync();

        var flagEmoji = GetFlagEmoji(track);
        
        var message = $@"
{flagEmoji}  {track}- trénink {flagEmoji} 

🕗 {date} {time} 🕗

🏎️  Short Q - 50% Race 🏎️

🛠️ Ligový assisty / no damage 🛠️

@F1 {role} Driver 
Dobrovolná účast, prosím potvrď
Trénink proběhne při účasti alespoň {driversRequired} pilotů
";
        
        _logger.LogInformation("Creating practice for {track}", track);

        var followupMessage = await FollowupAsync(message);
        
        var checkMark = new Emoji("\u2705");
        var redX = new Emoji("\u274C");
        var questionMark = new Emoji("\u2753");
        await followupMessage.AddReactionAsync(checkMark);
        await followupMessage.AddReactionAsync(redX);
        await followupMessage.AddReactionAsync(questionMark);
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
            _ => throw new ArgumentOutOfRangeException(nameof(track), track, null)
        };
    }

    public enum Tracks
    {
        [ChoiceDisplay("Bahrain")]
        Bahrain,
        [ChoiceDisplay("Jeddah")]
        Jeddah,
        [ChoiceDisplay("Australia")]
        Australia,
        [ChoiceDisplay("Japan")]
        Japan,
        [ChoiceDisplay("China")]
        China,
        [ChoiceDisplay("Miami")]
        Miami,
        [ChoiceDisplay("Imola")]
        Imola,
        [ChoiceDisplay("Monaco")]
        Monaco,
        [ChoiceDisplay("Canada")]
        Canada,
        [ChoiceDisplay("Spain")]
        Spain,
        [ChoiceDisplay("Austria")]
        Austria,
        [ChoiceDisplay("Great Britain")]
        GreatBritain,
        [ChoiceDisplay("Hungary")]
        Hungary,
        [ChoiceDisplay("Belgium")]
        Belgium,
        [ChoiceDisplay("Netherlands")]
        Netherlands,
        [ChoiceDisplay("Monza")]
        Monza,
        [ChoiceDisplay("Azerbaijan")]
        Azerbaijan,
        [ChoiceDisplay("Singapore")]
        Singapore,
        [ChoiceDisplay("Texas")]
        Texas,
        [ChoiceDisplay("Mexico")]
        Mexico,
        [ChoiceDisplay("Las Vegas")]
        LasVegas,
        [ChoiceDisplay("Abu Dhabi")]
        AbuDhabi
    }
    
    [Flags]
    public enum Roles
    {
        [ChoiceDisplay("Rookie")]
        Rookie = 1,
        [ChoiceDisplay("Junior")]
        Junior = 2,
        [ChoiceDisplay("Talent")]
        Talent = 4,
        [ChoiceDisplay("Academy")]
        Academy = 8,
        [ChoiceDisplay("Main")]
        Main = 16,
        [ChoiceDisplay("Driver")]
        Driver = 32
    }
}