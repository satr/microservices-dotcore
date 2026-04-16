using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public sealed class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<CartItemEntity> CartItems => Set<CartItemEntity>();
    public DbSet<CartItemFailureEntity> CartItemFailures => Set<CartItemFailureEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

