using System;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public sealed class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<CartItemEntity> CartItems => Set<CartItemEntity>();
    public DbSet<CartItemFailureEntity> CartItemFailures => Set<CartItemFailureEntity>();
    public DbSet<BookInventoryEntity> BookInventories => Set<BookInventoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookInventoryEntity>(entity =>
        {
            entity.HasKey(b => b.BookId);
            entity.Property(b => b.BookId).HasMaxLength(50);
            entity.Property(b => b.Stock);
            entity.HasData(
                new BookInventoryEntity { BookId = "b1", Stock = 10 },
                new BookInventoryEntity { BookId = "b2", Stock = 10 },
                new BookInventoryEntity { BookId = "b3", Stock = 10 }
            );
        });

        modelBuilder.Entity<CartItemEntity>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.UserId).IsRequired().HasMaxLength(100);
            entity.Property(c => c.BookId).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Author).IsRequired().HasMaxLength(200);

            entity.HasIndex(c => c.UserId);
        });

        modelBuilder.Entity<CartItemFailureEntity>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.UserId).IsRequired().HasMaxLength(100);
            entity.Property(c => c.BookId).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Author).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Reason).IsRequired().HasMaxLength(500);

            entity.HasIndex(c => c.UserId);
        });
    }
}

