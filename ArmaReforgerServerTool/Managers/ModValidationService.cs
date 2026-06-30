/******************************************************************************
 * File Name:    ModValidationService.cs
 * Project:      Longbow
 * Description:  Validates mod configurations by checking dependencies,
 *               detecting circular dependencies, and verifying load order.
 *               Works with ModDependencyManager to resolve dependency graph.
 *
 * Author:       Longbow contributors
 ******************************************************************************/

using ReforgerServerApp.Models;
using Serilog;

namespace ReforgerServerApp.Managers
{
  /// <summary>
  /// Result of a validation pass, including auto-fixes applied
  /// </summary>
  public class ValidationAndFixResult
  {
    public ValidationResult ValidationResult { get; set; }
    public List<Mod> SortedMods { get; set; }
    public List<Mod> AddedMods { get; set; }
    public Dictionary<string, int> ModPositionChanges { get; set; } // modId -> new index

    public ValidationAndFixResult()
    {
      ValidationResult = new ValidationResult();
      SortedMods = new List<Mod>();
      AddedMods = new List<Mod>();
      ModPositionChanges = new Dictionary<string, int>();
    }
  }

  internal class ModValidationService
  {
    private static ModValidationService? m_instance;

    private ModValidationService() { }

    public static ModValidationService GetInstance()
    {
      m_instance ??= new ModValidationService();
      return m_instance;
    }

    /// <summary>
    /// Validates the current enabled mod list for dependency errors and auto-fixes issues.
    /// Returns a ValidationAndFixResult with:
    /// - ValidationResult (errors/warnings)
    /// - SortedMods (topologically sorted by dependencies)
    /// - AddedMods (missing dependencies that were auto-added)
    /// - ModPositionChanges (tracking which mods were reordered)
    ///
    /// This performs validation with auto-fix:
    /// - Resolves missing dependencies (auto-adds them)
    /// - Reorders mods by topological sort (dependencies before dependents)
    /// - Detects circular dependencies (warnings, can't auto-fix)
    /// - Detects network failures (warnings)
    ///
    /// A mod configuration is VALID if:
    /// - No FATAL errors exist (all dependencies resolved correctly)
    ///
    /// A mod configuration has WARNINGS if:
    /// - Circular dependencies detected
    /// - Network issues during dependency fetch
    /// </summary>
    public ValidationAndFixResult ValidateAndFixMods(IList<Mod> enabledMods)
    {
      var result = new ValidationAndFixResult();

      if (enabledMods == null || enabledMods.Count == 0)
      {
        Log.Information("=== MOD VALIDATION & FIX START ===");
        Log.Information("No mods to validate");
        Log.Information("=== MOD VALIDATION & FIX END ===");
        result.ValidationResult.IsValid = true;
        return result;
      }

      Log.Information("=== MOD VALIDATION & FIX START ===");
      Log.Information("Validating & fixing {count} enabled mods against game version 1.6.0.119", enabledMods.Count);

      try
      {
        // Use ModDependencyManager to resolve dependencies
        // This will auto-add missing dependencies and detect circular references
        var (sorted, added, warnings, totalSize) = ModDependencyManager.ResolveDependencies(enabledMods);

        // Store the sorted mods and added mods
        result.SortedMods = sorted;
        result.AddedMods = added;

        // Log info about auto-added mods
        if (added.Count > 0)
        {
          Log.Information("Auto-adding {count} missing dependency/ies during validation", added.Count);
          var addedNames = string.Join(", ", added.Select(m => $"{m.GetModName()} ({m.GetModID()})"));
          Log.Debug("Auto-added mods: {mods}", addedNames);
          Log.Information("Auto-fix: Added {count} missing dependencies", added.Count);
        }

        // Track position changes (which mods were reordered)
        for (int i = 0; i < sorted.Count; i++)
        {
          var modId = sorted[i].GetModID();
          var originalIndex = enabledMods.ToList().FindIndex(m => m.GetModID().Equals(modId, StringComparison.OrdinalIgnoreCase));

          if (originalIndex >= 0 && originalIndex != i)
          {
            result.ModPositionChanges[modId] = i;
            Log.Information("Auto-fix: Reordered {name} ({id}) from position {old} to {new}",
              sorted[i].GetModName(), modId, originalIndex, i);
          }
        }

        // Convert warnings from dependency resolution to ValidationError objects
        if (warnings.Count > 0)
        {
          foreach (var warning in warnings)
          {
            Log.Warning("  [WARNING] Dependency System: {msg}", warning);
            // Add as WARNING-level errors (not FATAL - we can still launch with warnings)
            var validationError = new ValidationError(
              modId: "DEPENDENCY_MANAGER",
              modName: "Mod Dependency System",
              message: warning,
              severity: ErrorSeverity.WARNING
            );
            result.ValidationResult.Warnings.Add(validationError);
          }
        }

        // If we got here with no FATAL errors, validation passed
        result.ValidationResult.IsValid = !result.ValidationResult.Errors.Any(e => e.Severity == ErrorSeverity.FATAL);

        if (result.ValidationResult.IsValid)
        {
          Log.Information("✅ MOD VALIDATION PASSED - All {count} mods are valid (after auto-fixes)", sorted.Count);
        }
        else
        {
          var fatalCount = result.ValidationResult.Errors.Count(e => e.Severity == ErrorSeverity.FATAL);
          var warningCount = result.ValidationResult.Warnings.Count;
          Log.Warning("⚠️ MOD VALIDATION FAILED - {fatalCount} fatal, {warningCount} warnings",
            fatalCount, warningCount);

          foreach (var error in result.ValidationResult.Errors.Where(e => e.Severity == ErrorSeverity.FATAL))
          {
            Log.Error("  [FATAL] {modId}: {message}", error.ModId, error.Message);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("Validation system error: {msg}", ex.Message);
        // Add a FATAL error for unexpected failures
        result.ValidationResult.Errors.Add(new ValidationError(
          modId: "SYSTEM",
          modName: "Validation System",
          message: $"Validation system error: {ex.Message}",
          severity: ErrorSeverity.FATAL
        ));
        result.ValidationResult.IsValid = false;

        Log.Error("❌ MOD VALIDATION FAILED - System error during validation");
      }

      Log.Information("=== MOD VALIDATION & FIX END ===");

      return result;
    }

    /// <summary>
    /// Legacy method for backward compatibility - calls ValidateAndFixMods
    /// </summary>
    public ValidationResult ValidateMods(IList<Mod> enabledMods)
    {
      var result = ValidateAndFixMods(enabledMods);
      return result.ValidationResult;
    }
  }
}
