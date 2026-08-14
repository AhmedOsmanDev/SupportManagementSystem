using SMS.Domain;

namespace SMS.Application;

public sealed record CommentDto(Guid Id, string Content, Guid AuthorId, string AuthorName, UserRole AuthorRole, DateTime CreatedAt);
