using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Persistence.Models;

internal sealed class TrainingSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("MessageId")]
    public ulong MessageId { get; set; }

    [BsonElement("CreatorId")]
    public ulong CreatorId { get; set; }

    [BsonElement("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("GuildId")]
    public ulong? GuildId { get; set; }

    [BsonElement("ChannelId")]
    public ulong? ChannelId { get; set; }
}