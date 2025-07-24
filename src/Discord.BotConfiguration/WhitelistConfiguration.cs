using System.ComponentModel.DataAnnotations;

namespace Discord.BotConfiguration;

public sealed class WhitelistConfiguration
{
    public const string SectionName = "WhitelistConfiguration";

    [Required]
    public required ulong[] Servers { get; init; }
}
