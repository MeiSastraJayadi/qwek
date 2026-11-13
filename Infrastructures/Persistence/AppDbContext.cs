using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Qwek.Domains.Entities;
using Qwek.Models;

namespace Qwek.Infrastructures.Persistence;

public class AppDbContext : IdentityDbContext<PersonalUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Server> Servers => Set<Server>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<ServerMember> ServerMembers => Set<ServerMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PersonalUser>(entity =>
        {
            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            entity.Property(u => u.UpdatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAddOrUpdate();
        });

        builder.Entity<Server>(entity =>
        {
            entity.Property(s => s.Id).ValueGeneratedNever();

            entity.Property(s => s.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            entity.HasOne<PersonalUser>()
                .WithMany()
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Channel>(entity =>
        {
            entity.Property(c => c.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            entity.HasOne<Server>()
                .WithMany(s => s.Channels)
                .HasForeignKey(c => c.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Message>(entity =>
        {
            entity.Property(m => m.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            entity.HasOne<Channel>()
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PersonalUser>()
                .WithMany()
                .HasForeignKey(m => m.PersonalUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Message>()
                .WithMany()
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<MessageReaction>(entity =>
        {
            entity.Property(reaction => reaction.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            entity.HasIndex(reaction => new
            {
                reaction.MessageId,
                reaction.PersonalUserId,
                reaction.Emoji
            }).IsUnique();

            entity.HasOne<Message>()
                .WithMany()
                .HasForeignKey(reaction => reaction.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PersonalUser>()
                .WithMany()
                .HasForeignKey(reaction => reaction.PersonalUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ServerMember>(entity =>
        {
            entity.HasKey(sm => new { sm.PersonalUserId, sm.ServerId });

            entity.Property(sm => sm.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            entity.HasOne<PersonalUser>()
                .WithMany()
                .HasForeignKey(sm => sm.PersonalUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Server>()
                .WithMany()
                .HasForeignKey(sm => sm.ServerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
