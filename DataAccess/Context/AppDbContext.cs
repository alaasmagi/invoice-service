using DTO.DataAccess.DataAccess.DTO;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AddressEntity> Addresses => Set<AddressEntity>();
    public DbSet<AddressContactEntity> AddressContacts => Set<AddressContactEntity>();
    public DbSet<ContactEntity> Contacts => Set<ContactEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceAllocationEntity> InvoiceAllocations => Set<InvoiceAllocationEntity>();
    public DbSet<MonthlyStatementEntity> MonthlyStatements => Set<MonthlyStatementEntity>();
    public DbSet<MonthlyStatementLineEntity> MonthlyStatementLines => Set<MonthlyStatementLineEntity>();
    public DbSet<ServiceEntity> Services => Set<ServiceEntity>();
    public DbSet<AllocationRuleEntity> AllocationRules => Set<AllocationRuleEntity>();
    public DbSet<AllocationRuleShareEntity> AllocationRuleShares => Set<AllocationRuleShareEntity>();
    public DbSet<AppUserEntity> AppUsers => Set<AppUserEntity>();
    public DbSet<PublishedInvoiceEmailEntity> PublishedInvoiceEmails => Set<PublishedInvoiceEmailEntity>();
    public DbSet<ProcessedInboxMessageEntity> ProcessedInboxMessages => Set<ProcessedInboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("invoice");

        modelBuilder.Entity<PublishedInvoiceEmailEntity>(entity =>
        {
            entity.ToTable("PublishedInvoiceEmails");
            entity.Property(e => e.InvoiceIdentifier).HasMaxLength(64);
            entity.Property(e => e.Recipient).HasMaxLength(320);
            entity.Property(e => e.EnvelopeId).HasMaxLength(64);
            // Second layer of dedup after the natural key: the deterministic envelope id is unique.
            entity.HasIndex(e => e.EnvelopeId).IsUnique();
            // One row per send attempt: the first send and each explicit resend get their own row.
            entity.HasIndex(e => new { e.UserId, e.MonthlyStatementId, e.Attempt }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Year, e.Month });
        });

        modelBuilder.Entity<ProcessedInboxMessageEntity>(entity =>
        {
            entity.ToTable("ProcessedInboxMessages");
            entity.Property(e => e.EnvelopeId).HasMaxLength(64);
            entity.Property(e => e.Source).HasMaxLength(64);
            entity.Property(e => e.Action).HasMaxLength(64);
            entity.HasIndex(e => e.EnvelopeId).IsUnique();
            entity.HasIndex(e => e.ProcessedAt);
        });

        modelBuilder.Entity<AppUserEntity>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.Username).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(256);
            entity.Property(e => e.Locale).HasMaxLength(16);
            entity.Property(e => e.BankIban).HasMaxLength(64);
            entity.Property(e => e.Language).HasMaxLength(8);
            entity.Property(e => e.FormatCulture).HasMaxLength(16);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("EUR");
            entity.Property(e => e.TimeZone).HasMaxLength(64).HasDefaultValue("Europe/Tallinn");
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Username);
        });

        modelBuilder.Entity<AddressEntity>(entity =>
        {
            entity.HasIndex(e => e.Name);

            entity.HasMany(e => e.AddressContacts)
                .WithOne(e => e.Address)
                .HasForeignKey(e => e.AddressId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Invoices)
                .WithOne(e => e.Address)
                .HasForeignKey(e => e.AddressId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactEntity>(entity =>
        {
            entity.HasMany(e => e.AddressContacts)
                .WithOne(e => e.Contact)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.InvoiceAllocations)
                .WithOne(e => e.Contact)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.MonthlyStatements)
                .WithOne(e => e.Contact)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceEntity>(entity =>
        {
            entity.HasIndex(e => e.AddressId);
            entity.HasIndex(e => e.ServiceId);
            entity.HasIndex(e => e.InvoiceDate);
            entity.Property(e => e.TotalSum).HasPrecision(18, 2);

            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceAllocationEntity>(entity =>
        {
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.ContactId);
            entity.Property(e => e.AllocatedSum).HasPrecision(18, 2);

            entity.HasOne(e => e.Invoice)
                .WithMany(e => e.Allocations)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MonthlyStatementEntity>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ContactId, e.Year, e.Month })
                .IsUnique();
            entity.HasIndex(e => new { e.Year, e.Month });
            entity.Property(e => e.TotalSum).HasPrecision(18, 2);
        });

        modelBuilder.Entity<MonthlyStatementLineEntity>(entity =>
        {
            entity.HasIndex(e => e.MonthlyStatementId);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.AddressId);
            entity.HasIndex(e => e.ServiceId);
            entity.Property(e => e.InvoiceTotal).HasPrecision(18, 2);
            entity.Property(e => e.AllocatedAmount).HasPrecision(18, 2);
            entity.Property(e => e.AddressName).HasMaxLength(256);
            entity.Property(e => e.ServiceName).HasMaxLength(256);

            entity.HasOne(e => e.MonthlyStatement)
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.MonthlyStatementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Invoice)
                .WithMany()
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Address)
                .WithMany()
                .HasForeignKey(e => e.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.ShareValue).HasPrecision(18, 2);

            // Lines keep their amounts when the rule that produced them is deleted.
            entity.HasOne(e => e.AllocationRule)
                .WithMany()
                .HasForeignKey(e => e.AllocationRuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AllocationRuleEntity>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.AddressId, e.ServiceId, e.ValidFrom });

            entity.HasOne(e => e.Address)
                .WithMany(e => e.AllocationRules)
                .HasForeignKey(e => e.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Shares)
                .WithOne(e => e.AllocationRule)
                .HasForeignKey(e => e.AllocationRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AllocationRuleShareEntity>(entity =>
        {
            entity.HasIndex(e => new { e.AllocationRuleId, e.ContactId }).IsUnique();
            entity.HasIndex(e => e.ContactId);
            entity.Property(e => e.Value).HasPrecision(18, 2);

            entity.HasOne(e => e.Contact)
                .WithMany(e => e.AllocationRuleShares)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
