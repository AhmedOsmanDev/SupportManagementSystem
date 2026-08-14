namespace SMS.Application;

public sealed class ForbiddenException(string message = "You are not permitted to perform this action.") : AppException(message);
