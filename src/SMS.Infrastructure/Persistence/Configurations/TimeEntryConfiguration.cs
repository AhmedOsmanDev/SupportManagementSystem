using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain;

namespace SMS.Infrastructure.Persistence.Configurations;

public sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Description).HasMaxLength(1000).IsRequired();
        builder.HasIndex(entry => new { entry.TicketNumber, entry.WorkDate });
        builder.HasOne(entry => entry.Agent)
            .WithMany()
            .HasForeignKey(entry => entry.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
