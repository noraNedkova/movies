using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Movies.Models;

public class Movie
{
    public int Id { get; set; }
    public int? TmdbId { get; set; }

    [Required]
    [StringLength(60, MinimumLength = 1)]
    public string? Title { get; set; }

    public DateOnly ReleaseDate { get; set; }

    [Required]
    [StringLength(200)]
    [RegularExpression(@"^[A-Z]+[a-zA-Z()\s-]*$")]
    public string? Genre { get; set; }

    [Required]
    [StringLength(150)]
    [RegularExpression(@"^[A-Z]+[a-zA-Z()\s-]*$")]
    public string? Director { get; set; }

    [Range(0, 10)]
    [Column(TypeName = "decimal(3, 1)")]
    public decimal Rating { get; set; }

    public string? PosterPath { get; set; } 

    public int VoteCount { get; set; }
}