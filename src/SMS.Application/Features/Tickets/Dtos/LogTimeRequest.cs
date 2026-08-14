using System.ComponentModel.DataAnnotations;

namespace SMS.Application;

public sealed class LogTimeRequest
{
    [Required]
    public DateTime? WorkDate { get; init; }

    [Range(1, 1440)]
    public int DurationMinutes { get; init; }

    [Required, StringLength(1000, MinimumLength = 2)]
    public string Description { get; init; } = string.Empty;
}
