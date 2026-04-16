using Microsoft.EntityFrameworkCore;
using UsersService.Models;

namespace UsersService.Data;

public sealed class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

    public DbSet<UserRecord> Users => Set<UserRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRecord>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasMaxLength(50);
            entity.Property(u => u.UserName).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => u.UserName).IsUnique();

            entity.HasData(
                new UserRecord("u1", "user1"),
                new UserRecord("u2", "user2")
            );
        });
    }
}

