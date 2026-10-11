namespace Movies.Models;

public class WatchlistItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = default!;
}
