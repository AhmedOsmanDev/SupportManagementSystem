using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain;

namespace SMS.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(ticket => ticket.Number);
        builder.Property(ticket => ticket.Number)
            .ValueGeneratedNever();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Tickets_Number_Positive",
            "[Number] > 0"));
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
