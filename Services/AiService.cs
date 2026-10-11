using Google.GenAI;
using System.Text.Json;
using Movies.Models;
using Microsoft.EntityFrameworkCore;
using Movies.Data;

namespace Movies.Services;

public class AiService
{
    private readonly Client _client;
    private readonly IDbContextFactory<MoviesContext> _dbFactory;

    public AiService(
        IConfiguration configuration,
        IDbContextFactory<MoviesContext> dbFactory)
    {
        var apiKey = configuration["GeminiApiKey"]
            ?? throw new InvalidOperationException(
                "GeminiApiKey was not found.");

        _client = new Client(apiKey: apiKey);
        _dbFactory = dbFactory;
    }

    public async Task<MovieAiRequest> AskAsync(string question)
    {
        var prompt = string.Concat(
            """
            You are Movie AI's intent analyzer.

            Return ONLY valid JSON.
            Do not use markdown.
            Do not use ```.

            There are ONLY two possible intents:

            "search" = the user wants to find or recommend movies.

            "chat" = the user wants general conversation or information
            about movies, actors, directors, cinema, plots, or this website.

            IMPORTANT:
            If the user gives only a movie genre, the intent MUST be "search".

            Genre normalization:
            - romantic -> Romance
            - romance -> Romance
            - romantic movie -> Romance
            - action -> Action
            - comedy -> Comedy
            - horror -> Horror
            - thriller -> Thriller
            - drama -> Drama
            - science fiction -> Science Fiction
            - sci-fi -> Science Fiction
            - fantasy -> Fantasy
            - animation -> Animation
            - adventure -> Adventure

            Extract only these fields:

            title
            genre
            minimumRating
            director

            Never invent values.

            Examples:

            User: recommend me a romantic movie
            {
              "intent":"search",
              "title":null,
              "genre":"Romance",
              "minimumRating":null,
              "director":null
            }

            User: romance
            {
              "intent":"search",
              "title":null,
              "genre":"Romance",
              "minimumRating":null,
              "director":null
            }

            User: action
            {
              "intent":"search",
              "title":null,
              "genre":"Action",
              "minimumRating":null,
              "director":null
            }

            User: movies rated above 8
            {
              "intent":"search",
              "title":null,
              "genre":null,
              "minimumRating":8,
              "director":null
            }

            User: tell me about Christopher Nolan
            {
              "intent":"chat",
              "title":null,
              "genre":null,
              "minimumRating":null,
              "director":null
            }

            USER:
            """,
            question);

        var response = await _client.Models.GenerateContentAsync(
            model: "gemini-3.5-flash-lite",
            contents: prompt);

        var json = response.Text?.Trim();

        if (string.IsNullOrWhiteSpace(json))
            return new MovieAiRequest();

        if (json.StartsWith("```"))
        {
            json = json
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
        }

        try
        {
            var request =
                JsonSerializer.Deserialize<MovieAiRequest>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return request ?? new MovieAiRequest();
        }
        catch
        {
            return new MovieAiRequest();
        }
    }

    public async Task<string> ChatAsync(string question)
    {
        var prompt = string.Concat(
            """
            You are Movie AI, a friendly movie assistant.

            You can talk about:
            - movies
            - actors
            - directors
            - genres
            - cinema
            - movie plots and themes
            - this movie website

            Keep answers short and easy to read.

            Style:
            - Usually 1–3 short sentences.
            - Keep most answers under 50 words.
            - Do not repeat the user's question.
            - Do not start with "Sure!", "Of course!", or similar filler.
            - Use short sentences.
            - Use a small amount of visual structure when helpful.
            - You may use "𖦹", "⋆˙⟡", or "📽.ᐟ" sparingly.
            - Do not use decorative symbols on every line.
            - Never make the answer feel overloaded or overly formatted.

            If the question is unrelated to movies or this website,
            briefly explain that you are a movie assistant and redirect
            the conversation toward movies.

            Do not provide URLs or images.
            Do not make up information.

            USER:
            """,
            question);

        var response = await _client.Models.GenerateContentAsync(
            model: "gemini-3.5-flash-lite",
            contents: prompt);

        return response.Text
            ?? "Sorry, I couldn't create an answer.";
    }

    public async Task<string> SearchMoviesAsync(
        MovieAiRequest request)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync();

        IQueryable<Movie> query =
            db.Movie.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            query = query.Where(m =>
                m.Title!.Contains(request.Title));
        }

        if (!string.IsNullOrWhiteSpace(request.Genre))
        {
            query = query.Where(m =>
                m.Genre!.Contains(request.Genre));
        }

        if (!string.IsNullOrWhiteSpace(request.Director))
        {
            query = query.Where(m =>
                m.Director!.Contains(request.Director));
        }

        if (request.MinimumRating.HasValue)
        {
            query = query.Where(m =>
                m.Rating >= request.MinimumRating.Value);
        }

        var movies = await query
            .OrderByDescending(m => m.VoteCount)
            .ThenByDescending(m => m.Rating)
            .Take(5)
            .ToListAsync();

        if (movies.Count == 0)
        {
            return "I couldn't find any movies matching your request.";
        }

        var lines = movies.Select(movie =>
        {
            var genre = string.IsNullOrWhiteSpace(movie.Genre)
                ? "Unknown genre"
                : movie.Genre;

            return
                $"[ ▶︎ ] {movie.Title} · " +
                $"{genre} · " +
                $"{movie.ReleaseDate.Year} · " +
                $"★ {movie.Rating}";
        });

        return string.Join(
            Environment.NewLine,
            new[]
            {
                "📽.ᐟ MOVIES FOR YOU",
                "",
                string.Join(
                    Environment.NewLine,
                    lines)
            });
    }
}