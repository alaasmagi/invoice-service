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
    public DbSet<AppUserEntity> AppUsers => Set<AppUserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("invoice");

        modelBuilder.Entity<AppUserEntity>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.Property(e => e.Fullname).HasMaxLength(256);
            entity.Property(e => e.BankIban).HasMaxLength(64);
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
        });
    }
}
