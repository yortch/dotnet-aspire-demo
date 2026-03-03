using Microsoft.EntityFrameworkCore;
using WeatherDashboard.PreferencesApi.Models;

namespace WeatherDashboard.PreferencesApi.Data;

public class PreferencesDbContext : DbContext
{
    public PreferencesDbContext(DbContextOptions<PreferencesDbContext> options)
        : base(options) { }

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.ToTable("user_preferences");

            entity.HasIndex(e => e.UserId);

            entity.HasIndex(e => new { e.UserId, e.City })
                  .IsUnique();

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("now() at time zone 'utc'");
        });

        // Seed data
        modelBuilder.Entity<UserPreference>().HasData(
            new UserPreference { Id = 1, UserId = "demo-user", City = "Seattle", DisplayOrder = 0, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new UserPreference { Id = 2, UserId = "demo-user", City = "Portland", DisplayOrder = 1, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new UserPreference { Id = 3, UserId = "demo-user", City = "Austin", DisplayOrder = 2, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
