using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;

namespace Transfers.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", table =>
        {
            table.HasCheckConstraint("ck_accounts_overdraft_limit_not_negative", "overdraft_limit >= 0");
            table.HasCheckConstraint("ck_accounts_balance_within_overdraft", "balance >= -overdraft_limit");
        });

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.Property(account => account.HolderName).HasMaxLength(150).IsRequired();
        builder.Property(account => account.HolderDocument).HasMaxLength(11).IsFixedLength().IsRequired();
        builder.HasIndex(account => account.HolderDocument).IsUnique();
        builder.Property<uint>("Version").IsRowVersion();

        builder.ComplexProperty(account => account.Limits, limits =>
        {
            limits.ComplexProperty(policy => policy.Daytime);
            limits.ComplexProperty(policy => policy.Nighttime);
        });

        builder.Ignore(account => account.DomainEvents);
    }
}
