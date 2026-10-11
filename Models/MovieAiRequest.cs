namespace Movies.Models;

public class MovieAiRequest
{
    public string Intent { get; set; } = "chat";

    public string? Title { get; set; }

    public string? Genre { get; set; }

    public decimal? MinimumRating { get; set; }

    public string? Director { get; set; }
}
