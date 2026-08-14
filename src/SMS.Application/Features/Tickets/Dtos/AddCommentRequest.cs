using System.ComponentModel.DataAnnotations;

namespace SMS.Application;

public sealed class AddCommentRequest
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}
