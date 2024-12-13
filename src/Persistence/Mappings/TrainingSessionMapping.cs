

using Domain;

namespace Persistence.Mappings;

internal static class TrainingSessionMapping
{
    public static Models.TrainingSession ToModel(this TrainingSession session)
    {
        return new Models.TrainingSession
        {
            MessageId = session.MessageId,
            CreatorId = session.CreatorId,
            GuildId = session.GuildId,
            ChannelId = session.ChannelId
        };
    }

    public static TrainingSession ToDomain(this Models.TrainingSession model)
    {
        return new TrainingSession
        {
            MessageId = model.MessageId,
            CreatorId = model.CreatorId,
            GuildId = model.GuildId,
            ChannelId = model.ChannelId
        };
    }
}