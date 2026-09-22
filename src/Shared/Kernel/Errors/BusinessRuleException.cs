namespace NafasLand.Admin.Shared.Kernel.Errors;

public sealed class BusinessRuleException(string message) : Exception(message);
