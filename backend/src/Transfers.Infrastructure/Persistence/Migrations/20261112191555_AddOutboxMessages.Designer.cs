using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Transfers.Infrastructure.Persistence;

#nullable disable

namespace Transfers.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(TransfersDbContext))]
    [Migration("20261112191555_AddOutboxMessages")]
    partial class AddOutboxMessages
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Transfers.Domain.Accounts.Account", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<decimal>("Balance")
                        .HasPrecision(18, 2)
                        .HasColumnType("numeric(18,2)")
                        .HasColumnName("balance");

                    b.Property<string>("HolderDocument")
                        .IsRequired()
                        .HasMaxLength(11)
                        .HasColumnType("character(11)")
                        .HasColumnName("holder_document")
                        .IsFixedLength();

                    b.Property<string>("HolderName")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("character varying(150)")
                        .HasColumnName("holder_name");

                    b.Property<DateTimeOffset>("OpenedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("opened_at");

                    b.Property<decimal>("OverdraftLimit")
                        .HasPrecision(18, 2)
                        .HasColumnType("numeric(18,2)")
                        .HasColumnName("overdraft_limit");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("character varying(40)")
                        .HasColumnName("status");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.ComplexProperty(typeof(Dictionary<string, object>), "Limits", "Transfers.Domain.Accounts.Account.Limits#TransferLimitPolicy", b1 =>
                        {
                            b1.IsRequired();

                            b1.ComplexProperty(typeof(Dictionary<string, object>), "Daytime", "Transfers.Domain.Accounts.Account.Limits#TransferLimitPolicy.Daytime#HourlyLimit", b2 =>
                                {
                                    b2.IsRequired();

                                    b2.Property<decimal>("MaxAmount")
                                        .HasPrecision(18, 2)
                                        .HasColumnType("numeric(18,2)")
                                        .HasColumnName("limits_daytime_max_amount");

                                    b2.Property<int>("MaxAttempts")
                                        .HasColumnType("integer")
                                        .HasColumnName("limits_daytime_max_attempts");
                                });

                            b1.ComplexProperty(typeof(Dictionary<string, object>), "Nighttime", "Transfers.Domain.Accounts.Account.Limits#TransferLimitPolicy.Nighttime#HourlyLimit", b2 =>
                                {
                                    b2.IsRequired();

                                    b2.Property<decimal>("MaxAmount")
                                        .HasPrecision(18, 2)
                                        .HasColumnType("numeric(18,2)")
                                        .HasColumnName("limits_nighttime_max_amount");

                                    b2.Property<int>("MaxAttempts")
                                        .HasColumnType("integer")
                                        .HasColumnName("limits_nighttime_max_attempts");
                                });
                        });

                    b.HasKey("Id")
                        .HasName("pk_accounts");

                    b.HasIndex("HolderDocument")
                        .IsUnique()
                        .HasDatabaseName("ix_accounts_holder_document");

                    b.ToTable("accounts", null, t =>
                        {
                            t.HasCheckConstraint("ck_accounts_balance_within_overdraft", "balance >= -overdraft_limit");

                            t.HasCheckConstraint("ck_accounts_overdraft_limit_not_negative", "overdraft_limit >= 0");
                        });
                });

            modelBuilder.Entity("Transfers.Domain.Transfers.Transfer", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<decimal>("Amount")
                        .HasPrecision(18, 2)
                        .HasColumnType("numeric(18,2)")
                        .HasColumnName("amount");

                    b.Property<DateTimeOffset?>("CancelledAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("cancelled_at");

                    b.Property<DateTimeOffset?>("CompletedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("completed_at");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<Guid>("DestinationAccountId")
                        .HasColumnType("uuid")
                        .HasColumnName("destination_account_id");

                    b.Property<DateTimeOffset?>("FailedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("failed_at");

                    b.Property<string>("FailureReason")
                        .HasMaxLength(40)
                        .HasColumnType("character varying(40)")
                        .HasColumnName("failure_reason");

                    b.Property<string>("IdempotencyKey")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("idempotency_key");

                    b.Property<string>("Kind")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("character varying(40)")
                        .HasColumnName("kind");

                    b.Property<DateTimeOffset?>("ProcessedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("processed_at");

                    b.Property<DateTimeOffset?>("ScheduledFor")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("scheduled_for");

                    b.Property<Guid>("SourceAccountId")
                        .HasColumnType("uuid")
                        .HasColumnName("source_account_id");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("character varying(40)")
                        .HasColumnName("status");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id")
                        .HasName("pk_transfers");

                    b.HasIndex("CreatedAt")
                        .HasDatabaseName("ix_transfers_created_at");

                    b.HasIndex("DestinationAccountId")
                        .HasDatabaseName("ix_transfers_destination_account_id");

                    b.HasIndex("ScheduledFor")
                        .HasDatabaseName("ix_transfers_due_scheduled")
                        .HasFilter("status = 'Scheduled'");

                    b.HasIndex("SourceAccountId", "IdempotencyKey")
                        .IsUnique()
                        .HasDatabaseName("ix_transfers_source_account_id_idempotency_key")
                        .HasFilter("idempotency_key IS NOT NULL");

                    b.HasIndex("SourceAccountId", "ProcessedAt")
                        .HasDatabaseName("ix_transfers_source_account_id_processed_at");

                    b.ToTable("transfers", null, t =>
                        {
                            t.HasCheckConstraint("ck_transfers_amount_positive", "amount > 0");

                            t.HasCheckConstraint("ck_transfers_distinct_accounts", "source_account_id <> destination_account_id");

                            t.HasCheckConstraint("ck_transfers_scheduled_has_date", "kind <> 'Scheduled' OR scheduled_for IS NOT NULL");
                        });
                });

            modelBuilder.Entity("Transfers.Infrastructure.Persistence.Outbox.OutboxMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<int>("Attempts")
                        .HasColumnType("integer")
                        .HasColumnName("attempts");

                    b.Property<string>("LastError")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("last_error");

                    b.Property<DateTimeOffset>("OccurredAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("occurred_at");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("payload");

                    b.Property<DateTimeOffset?>("ProcessedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("processed_at");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_outbox_messages");

                    b.HasIndex("OccurredAt", "Id")
                        .HasDatabaseName("ix_outbox_messages_pending")
                        .HasFilter("processed_at IS NULL");

                    b.ToTable("outbox_messages", (string)null);
                });

            modelBuilder.Entity("Transfers.Domain.Transfers.Transfer", b =>
                {
                    b.HasOne("Transfers.Domain.Accounts.Account", null)
                        .WithMany()
                        .HasForeignKey("DestinationAccountId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_transfers_accounts_destination_account_id");

                    b.HasOne("Transfers.Domain.Accounts.Account", null)
                        .WithMany()
                        .HasForeignKey("SourceAccountId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_transfers_accounts_source_account_id");
                });
#pragma warning restore 612, 618
        }
    }
}
