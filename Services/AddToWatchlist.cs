using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Movies.Data;
using Movies.Models;

namespace Movies.Services;

public class AddToWatchlist
{
    private readonly IDbContextFactory<MoviesContext> _dbFactory;
    private readonly AuthenticationStateProvider _authStateProvider;

    private HashSet<int>? _watchedMovieIds;
    private string? _userId;
    private Task? _loadTask;

    public AddToWatchlist(
        IDbContextFactory<MoviesContext> dbFactory,
        AuthenticationStateProvider authStateProvider)
    {
        _dbFactory = dbFactory;
        _authStateProvider = authStateProvider;
    }

    private Task EnsureLoadedAsync()
    {
        return _loadTask ??= LoadAsync();
    }

    private async Task LoadAsync()
    {
        var state =
            await _authStateProvider.GetAuthenticationStateAsync();

        _userId = state.User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (_userId == null)
        {
            _watchedMovieIds = new HashSet<int>();
            return;
        }

        await using var db =
            await _dbFactory.CreateDbContextAsync();

        _watchedMovieIds = await db.WatchlistItem
            .Where(w => w.UserId == _userId)
            .Select(w => w.MovieId)
            .ToHashSetAsync();
    }

    public async Task<bool> IsWatchedAsync(int movieId)
    {
        await EnsureLoadedAsync();

        return _watchedMovieIds!.Contains(movieId);
    }

    public async Task<bool> ToggleAsync(int movieId)
    {
        await EnsureLoadedAsync();

        if (_userId == null)
            return false;

        await using var db =
            await _dbFactory.CreateDbContextAsync();

        var item = await db.WatchlistItem
            .FirstOrDefaultAsync(w =>
                w.UserId == _userId &&
                w.MovieId == movieId);

        if (item == null)
        {
            db.WatchlistItem.Add(
                new WatchlistItem
                {
                    UserId = _userId,
                    MovieId = movieId
                });

            await db.SaveChangesAsync();

            _watchedMovieIds!.Add(movieId);

            return true;
        }

        db.WatchlistItem.Remove(item);

        await db.SaveChangesAsync();

        _watchedMovieIds!.Remove(movieId);

        return false;
    }
}