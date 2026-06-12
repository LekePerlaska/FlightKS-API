namespace FlightKS.Exceptions;

public sealed class BusinessRuleException(string message) : AppException(message, 422, "business_rule_violation");
