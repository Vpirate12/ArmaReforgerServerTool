/******************************************************************************
 * File Name:    ValidationError.cs
 * Project:      Longbow
 * Description:  Represents a single validation error or warning for a mod
 *
 * Author:       Longbow contributors
 ******************************************************************************/

namespace ReforgerServerApp.Models
{
  public enum ErrorSeverity
  {
    INFO,
    WARNING,
    FATAL
  }

  public class ValidationError
  {
    public string ModId { get; set; }
    public string ModName { get; set; }
    public string Message { get; set; }
    public ErrorSeverity Severity { get; set; }

    public ValidationError() { }

    public ValidationError(string modId, string modName, string message, ErrorSeverity severity)
    {
      ModId = modId;
      ModName = modName;
      Message = message;
      Severity = severity;
    }

    public override string ToString()
    {
      return $"[{Severity}] {ModName} ({ModId}): {Message}";
    }
  }
}
