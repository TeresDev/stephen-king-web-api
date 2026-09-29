namespace Investec.SK.Application.Queries;

/// <summary>A single rejected query parameter. Maps onto one entry of a ProblemDetails errors bag.</summary>
public sealed record ValidationError(string Parameter, string Message);
