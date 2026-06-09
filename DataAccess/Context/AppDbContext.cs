using Base.Domain;
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
    public DbSet<AppUserEntity> AppUsers => Set<AppUserEntity>();
    public DbSet<ContactEntity> Contacts => Set<ContactEntity>();
    public DbSet<ContactMonthlyStatementEntity> ContactMonthlyStatements => Set<ContactMonthlyStatementEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceAllocationEntity> InvoiceAllocations => Set<InvoiceAllocationEntity>();
    public DbSet<MonthlyStatementEntity> MonthlyStatements => Set<MonthlyStatementEntity>();
    public DbSet<ServiceEntity> Services => Set<ServiceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("invoice");

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

            entity.HasMany(e => e.MonthlyStatements)
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
        });

        modelBuilder.Entity<ContactMonthlyStatementEntity>(entity =>
        {
            entity.HasIndex(e => e.MonthlyStatementId);
            entity.HasIndex(e => e.ContactId);

            entity.HasOne(e => e.MonthlyStatement)
                .WithMany(e => e.Contacts)
                .HasForeignKey(e => e.MonthlyStatementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contact)
                .WithMany()
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceEntity>(entity =>
        {
            entity.HasIndex(e => e.AddressId);
            entity.HasIndex(e => e.ServiceId);
            entity.HasIndex(e => e.InvoiceDate);

            entity.HasOne(e => e.Service)
                .WithMany()
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceAllocationEntity>(entity =>
        {
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.ContactId);

            entity.HasOne(e => e.Invoice)
                .WithMany(e => e.Allocations)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MonthlyStatementEntity>(entity =>
        {
            entity.HasIndex(e => e.AddressId);
            entity.HasIndex(e => new { e.Year, e.Month });
        });
    }
}
