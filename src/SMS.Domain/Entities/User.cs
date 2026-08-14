namespace SMS.Domain;

public sealed class User
{
    private User() { }

    private User(Guid id, string firstName, string lastName, string email, string passwordHash, UserRole role)
    {
        Id = id;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsActive { get; private set; }

    public static User Create(string firstName, string lastName, string email, string passwordHash, UserRole role) =>
        new(Guid.NewGuid(), firstName, lastName, email, passwordHash, role);

    public static User Create(Guid id, string firstName, string lastName, string email, string passwordHash, UserRole role) =>
        new(id, firstName, lastName, email, passwordHash, role);

    public void SetActive(bool isActive) => IsActive = isActive;

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
