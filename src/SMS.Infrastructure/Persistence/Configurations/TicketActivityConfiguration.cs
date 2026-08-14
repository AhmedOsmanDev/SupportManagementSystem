using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain;

namespace SMS.Infrastructure.Persistence.Configurations;

public sealed class TicketActivityConfiguration : IEntityTypeConfiguration<TicketActivity>
{
    public void Configure(EntityTypeBuilder<TicketActivity> builder)
    {
        builder.HasKey(activity => activity.Id);
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
