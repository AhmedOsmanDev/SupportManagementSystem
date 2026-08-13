namespace SMS.Application;

public class AppException(string message) : Exception(message);

public sealed class NotFoundException(string message) : AppException(message);

public sealed class ForbiddenException(string message = "You are not permitted to perform this action.") : AppException(message);

public sealed class ConflictException(string message) : AppException(message);

public sealed class ValidationException(string message) : AppException(message);
