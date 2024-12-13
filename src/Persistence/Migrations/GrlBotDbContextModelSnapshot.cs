﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Persistence;

#nullable disable

namespace Persistence.Migrations
{
    [DbContext(typeof(GrlBotDbContext))]
    partial class GrlBotDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Persistence.Models.TrainingSession", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<decimal?>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("CreatorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("MessageId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.ToTable("TrainingSessions");
                });
#pragma warning restore 612, 618
        }
    }
}