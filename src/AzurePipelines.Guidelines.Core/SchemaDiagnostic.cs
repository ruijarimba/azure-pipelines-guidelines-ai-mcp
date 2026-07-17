namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Describes a structural Azure Pipelines YAML schema diagnostic.
/// </summary>
/// <param name="Code">A stable diagnostic code.</param>
/// <param name="Message">A human-readable diagnostic message.</param>
/// <param name="Line">The one-based source line, when available.</param>
public sealed record SchemaDiagnostic(string Code, string Message, int? Line = null);
