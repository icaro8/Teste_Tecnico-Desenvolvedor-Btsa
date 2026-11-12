using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers", table =>
        {
            table.HasCheckConstraint("ck_transfers_amount_positive", "amount > 0");
            table.HasCheckConstraint("ck_transfers_distinct_accounts", "source_account_id <> destination_account_id");
            table.HasCheckConstraint("ck_transfers_scheduled_has_date", "kind <> 'Scheduled' OR scheduled_for IS NOT NULL");
        });

        builder.HasKey(transfer => transfer.Id);
        builder.Property(transfer => transfer.Id).ValueGeneratedNever();
        builder.Property(transfer => transfer.IdempotencyKey).HasMaxLength(100);
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasOne<Account>().WithMany().HasForeignKey(transfer => transfer.SourceAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(transfer => transfer.DestinationAccountId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transfer => new { transfer.SourceAccountId, transfer.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        builder.HasIndex(transfer => new { transfer.SourceAccountId, transfer.ProcessedAt });
        builder.HasIndex(transfer => transfer.DestinationAccountId);
        builder.HasIndex(transfer => transfer.CreatedAt);
        builder.HasIndex(transfer => transfer.ScheduledFor)
            .HasFilter("status = 'Scheduled'")
            .HasDatabaseName("ix_transfers_due_scheduled");

        builder.Ignore(transfer => transfer.DomainEvents);
        builder.Ignore(transfer => transfer.CanBeCancelled);
    }
}
