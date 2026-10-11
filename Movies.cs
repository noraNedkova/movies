using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Movies.Models;

namespace Movies.Data;

public class MoviesContext : IdentityDbContext<User>
{
    public MoviesContext(DbContextOptions<MoviesContext> options)
        : base(options)
    {
    }

    public DbSet<Movie> Movie { get; set; } = default!;
    public DbSet<WatchlistItem> WatchlistItem { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<WatchlistItem>()
            .HasIndex(w => new { w.UserId, w.MovieId })
            .IsUnique();
    }
}
