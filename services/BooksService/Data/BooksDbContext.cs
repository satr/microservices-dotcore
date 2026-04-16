using BooksService.Models;
using Microsoft.EntityFrameworkCore;

namespace BooksService.Data;

public sealed class BooksDbContext : DbContext
{
    public BooksDbContext(DbContextOptions<BooksDbContext> options) : base(options) { }

    public DbSet<BookRecord> Books => Set<BookRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookRecord>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id).HasMaxLength(50);
            entity.Property(b => b.Title).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Author).IsRequired().HasMaxLength(200);

            entity.HasData(
                new BookRecord("b1", "Book1", "Author1"),
                new BookRecord("b2", "Book2", "Author2"),
                new BookRecord("b3", "Book3", "Author3")
            );
        });
    }
}

