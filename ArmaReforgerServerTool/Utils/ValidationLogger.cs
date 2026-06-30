/******************************************************************************
 * File Name:    ValidationLogger.cs
 * Project:      Longbow
 * Description:  Helper utility for formatted logging of validation results.
 *               Provides structured logging output for audit trails and debugging.
 *
 * Author:       Longbow contributors
 ******************************************************************************/

using ReforgerServerApp.Models;
using Serilog;

namespace ReforgerServerApp.Utils
{
  /// <summary>
  /// Provides utility methods for formatted logging of validation results.
  /// Used to create structured, readable log output for validation events.
  /// </summary>
  public static class ValidationLogger
  {
    /// <summary>
    /// Log a complete validation result with summary and details.
    /// Logs at appropriate level (Information for pass, Warning/Error for failures).
    /// </summary>
    public static void LogValidationResult(ValidationResult result)
    {
      if (result == null)
      {
        Log.Warning("ValidationLogger - Attempted to log null validation result");
        return;
      }

      var summary = GenerateSummary(result);
      Log.Information("MOD VALIDATION SUMMARY: {summary}", summary);

      // Log detailed errors if present
      if (result.Errors.Any(e => e.Severity == ErrorSeverity.FATAL))
      {
        var fatalErrors = result.Errors.Where(e => e.Severity == ErrorSeverity.FATAL).ToList();
        Log.Error("Fatal validation errors ({count}): {@FatalErrors}",
          fatalErrors.Count,
          fatalErrors.Select(e => new { e.ModId, e.ModName, e.Message }));
      }

      if (result.Warnings.Any())
      {
        Log.Warning("Validation warnings ({count}): {@Warnings}",
          result.Warnings.Count,
          result.Warnings.Select(w => new { w.ModId, w.ModName, w.Message }));
      }
    }

    /// <summary>
    /// Generate a concise summary string of the validation result.
    /// Format: Valid=true/false, Mods=N, Errors=N, Warnings=N
    /// </summary>
    private static string GenerateSummary(ValidationResult result)
    {
      return $"Valid={result.IsValid}, " +
             $"Mods={result.Errors.Count + result.Warnings.Count}, " +
             $"Errors={result.Errors.Count}, " +
             $"Warnings={result.Warnings.Count}";
    }

    /// <summary>
    /// Log a server launch block event with reasons.
    /// </summary>
    public static void LogLaunchBlocked(ValidationResult result)
    {
      if (result == null || !result.HasFatalErrors())
        return;

      Log.Error("❌ Server launch BLOCKED due to {count} fatal validation error(s)",
        result.GetFatalErrors().Count);

      foreach (var error in result.GetFatalErrors())
      {
        Log.Error("  [FATAL] {modId} ({modName}): {message}",
          error.ModId, error.ModName, error.Message);
      }
    }

    /// <summary>
    /// Log a server launch proceeding with warnings.
    /// </summary>
    public static void LogLaunchWithWarnings(ValidationResult result)
    {
      if (result == null || !result.Warnings.Any())
        return;

      Log.Warning("⚠️ Server launching with {count} validation warning(s)",
        result.Warnings.Count);

      foreach (var warning in result.Warnings)
      {
        Log.Warning("  [WARNING] {modId} ({modName}): {message}",
          warning.ModId, warning.ModName, warning.Message);
      }
    }
  }
}
