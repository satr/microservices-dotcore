using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookingService.Data;

/// <summary>Tracks book inventory for the booking service (stock availability).</summary>
public sealed class BookInventoryEntity
{
    public string BookId { get; set; } = string.Empty;
    public int Stock { get; set; } = 10;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class BookingInventoryDbContext : DbContext
{
    public BookingInventoryDbContext(DbContextOptions<BookingInventoryDbContext> options) : base(options) { }

    public DbSet<BookInventoryEntity> BookInventories => Set<BookInventoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookInventoryEntity>(entity =>
        {
            entity.HasKey(b => b.BookId);
            entity.Property(b => b.BookId).HasMaxLength(50);
            entity.Property(b => b.Stock);

            // Seed 10 stock for each of the 3 books
            entity.HasData(
                new BookInventoryEntity { BookId = "b1", Stock = 10 },
                new BookInventoryEntity { BookId = "b2", Stock = 10 },
                new BookInventoryEntity { BookId = "b3", Stock = 10 }
            );
        });
    }
}

public sealed class BookingInventoryDbContextFactory : IDesignTimeDbContextFactory<BookingInventoryDbContext>
{
    public BookingInventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BookingInventoryDbContext>()
            .UseNpgsql("Host=localhost;Database=booking_db;Username=library;Password=library")
            .Options;
        return new BookingInventoryDbContext(options);
    }
}

