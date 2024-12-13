using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Models;

internal sealed class TrainingSession : IEntityTypeConfiguration<TrainingSession>
{
    [Key]
    public Guid Id { get; set; }

    public ulong MessageId { get; set; }

    public ulong CreatorId { get; set; }

    public DateTime CreatedAt { get; set; }

    public ulong? GuildId { get; set; }

    public ulong? ChannelId { get; set; }

    public void Configure(EntityTypeBuilder<TrainingSession> builder)
    {
        builder.HasKey(ts => ts.Id);

        builder.Property(rc => rc.Id)
            .ValueGeneratedNever();

        builder.Property(rc => rc.MessageId)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(rc => rc.CreatedAt)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(rc => rc.GuildId)
            .IsRequired(required: false)
            .ValueGeneratedNever();

        builder.Property(rc => rc.ChannelId)
            .IsRequired(required: false)
            .ValueGeneratedNever();
    }
}