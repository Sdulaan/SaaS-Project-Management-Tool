namespace SaaS.ProjectManagement.Application.Common.Exceptions;

public sealed class AppException(string message) : Exception(message);
public sealed class NotFoundException(string message) : Exception(message);
public sealed class UnauthorizedAppException(string message) : Exception(message);
public sealed class ForbiddenException(string message) : Exception(message);
