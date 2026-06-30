/******************************************************************************
 * File Name:    ValidationResult.cs
 * Project:      Longbow
 * Description:  Aggregates the result of a mod validation pass
 *
 * Author:       Longbow contributors
 ******************************************************************************/

namespace ReforgerServerApp.Models
{
  public class ValidationResult
  {
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; }
    public List<ValidationError> Warnings { get; set; }
    public DateTime ValidatedAt { get; set; }

    public ValidationResult()
    {
      IsValid = true;
      Errors = new List<ValidationError>();
      Warnings = new List<ValidationError>();
      ValidatedAt = DateTime.Now;
    }

    /// <summary>
    /// Check if there are any FATAL errors that would block server launch
    /// </summary>
    public bool HasFatalErrors()
    {
      return Errors.Any(e => e.Severity == ErrorSeverity.FATAL);
    }

    /// <summary>
    /// Get all FATAL errors
    /// </summary>
    public List<ValidationError> GetFatalErrors()
    {
      return Errors.Where(e => e.Severity == ErrorSeverity.FATAL).ToList();
    }

    /// <summary>
    /// Get a summary string for UI display
    /// </summary>
    public string GetSummary()
    {
      var fatalCount = Errors.Count(e => e.Severity == ErrorSeverity.FATAL);
      var warningCount = Warnings.Count;

      if (fatalCount > 0)
        return $"{fatalCount} fatal error(s), {warningCount} warning(s)";
      if (warningCount > 0)
        return $"{warningCount} warning(s)";
      return "All mods valid";
    }
  }
}
