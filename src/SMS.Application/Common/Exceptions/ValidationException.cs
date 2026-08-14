namespace SMS.Application;

public sealed class ValidationException(string message) : AppException(message);
