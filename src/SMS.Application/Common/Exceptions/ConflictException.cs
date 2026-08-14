namespace SMS.Application;

public sealed class ConflictException(string message) : AppException(message);
