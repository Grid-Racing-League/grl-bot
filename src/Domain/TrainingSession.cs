namespace Domain;

public class TrainingSession
{
    public ulong MessageId { get; set; }

    public ulong CreatorId { get; set; }

    public ulong? GuildId { get; set; }

    public ulong? ChannelId { get; set; }
}