namespace SMS.Application;

public sealed class NotFoundException(string message) : AppException(message);
