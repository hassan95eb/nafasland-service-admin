namespace NafasLand.Admin.Shared.Kernel.Errors;

public sealed class ResourceNotFoundException(string message) : Exception(message);
