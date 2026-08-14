using FluentAssertions;
using SMS.Domain;

namespace SMS.UnitTests;

public sealed class UserAndCommentTests
{
    [Fact]
    public void UserCreate_NormalizesIdentityAndStartsActive()
    {
        var user = User.Create(
            "  Ada  ",
            "  Lovelace  ",
            "  ADA@EXAMPLE.COM  ",
            "a-password-hash",
            UserRole.SupportAgent);

        user.FirstName.Should().Be("Ada");
        user.LastName.Should().Be("Lovelace");
        user.FullName.Should().Be("Ada Lovelace");
        user.Email.Should().Be("ada@example.com");
        user.Role.Should().Be(UserRole.SupportAgent);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetActive_ChangesAccountAvailability()
    {
        var user = User.Create(
            "Ada",
            "Lovelace",
            "ada@example.com",
            "a-password-hash",
            UserRole.Customer);

        user.SetActive(false);

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CommentCreate_AssociatesAuthorAndNormalizesContent()
    {
        var authorId = Guid.NewGuid();

        var comment = Comment.Create(1, authorId, "  Please investigate.  ");

        comment.Id.Should().NotBeEmpty();
        comment.TicketNumber.Should().Be(1);
        comment.UserId.Should().Be(authorId);
        comment.Content.Should().Be("Please investigate.");
        comment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
