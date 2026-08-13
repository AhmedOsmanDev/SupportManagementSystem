using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain;

namespace SMS.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);
        builder.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(1000).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
    }
}

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(ticket => ticket.Number);
        builder.Property(ticket => ticket.Number).HasMaxLength(32);
        builder.Property(ticket => ticket.Title).HasMaxLength(200).IsRequired();
        builder.Property(ticket => ticket.Description).HasMaxLength(5000).IsRequired();
        builder.Property(ticket => ticket.RowVersion).IsRowVersion();
        builder.HasIndex(ticket => ticket.CustomerId);
        builder.HasIndex(ticket => ticket.AssignedSupportId);
        builder.HasIndex(ticket => ticket.Status);
        builder.HasIndex(ticket => ticket.Priority);
        builder.HasIndex(ticket => ticket.CreatedAt);

        builder.HasOne(ticket => ticket.Customer)
            .WithMany()
            .HasForeignKey(ticket => ticket.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(ticket => ticket.AssignedSupport)
            .WithMany()
            .HasForeignKey(ticket => ticket.AssignedSupportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ticket => ticket.Comments)
            .WithOne(comment => comment.Ticket)
            .HasForeignKey(comment => comment.TicketNumber)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(ticket => ticket.Activities)
            .WithOne(activity => activity.Ticket)
            .HasForeignKey(activity => activity.TicketNumber)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(ticket => ticket.TimeEntries)
            .WithOne(entry => entry.Ticket)
            .HasForeignKey(entry => entry.TicketNumber)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ticket => ticket.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(ticket => ticket.Activities).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(ticket => ticket.TimeEntries).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(comment => comment.Id);
        builder.Property(comment => comment.TicketNumber).HasMaxLength(32);
        builder.Property(comment => comment.Content).HasMaxLength(4000).IsRequired();
        builder.HasIndex(comment => new { comment.TicketNumber, comment.CreatedAt });
        builder.HasOne(comment => comment.User)
            .WithMany()
            .HasForeignKey(comment => comment.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TicketActivityConfiguration : IEntityTypeConfiguration<TicketActivity>
{
    public void Configure(EntityTypeBuilder<TicketActivity> builder)
    {
        builder.HasKey(activity => activity.Id);
        builder.Property(activity => activity.TicketNumber).HasMaxLength(32);
        builder.Property(activity => activity.ActivityType).HasMaxLength(50).IsRequired();
        builder.Property(activity => activity.Description).HasMaxLength(1000).IsRequired();
        builder.Property(activity => activity.OldValue).HasMaxLength(256);
        builder.Property(activity => activity.NewValue).HasMaxLength(256);
        builder.HasIndex(activity => new { activity.TicketNumber, activity.CreatedAt });
        builder.HasOne(activity => activity.User)
            .WithMany()
            .HasForeignKey(activity => activity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.TicketNumber).HasMaxLength(32);
        builder.Property(entry => entry.Description).HasMaxLength(1000).IsRequired();
        builder.HasIndex(entry => new { entry.TicketNumber, entry.WorkDate });
        builder.HasOne(entry => entry.Agent)
            .WithMany()
            .HasForeignKey(entry => entry.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
