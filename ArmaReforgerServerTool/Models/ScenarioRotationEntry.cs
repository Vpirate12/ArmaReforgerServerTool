/******************************************************************************
 * File Name:    ScenarioRotationEntry.cs
 * Project:      Sentinel Desktop
 * Description:  A single entry in the scenario rotation playlist
 *
 * Author:       Claude Code
 ******************************************************************************/

namespace ReforgerServerApp.Models
{
  /// <summary>
  /// Represents one scenario in the rotation playlist.
  /// Holds the scenario ID, display label, and duration in minutes.
  /// </summary>
  internal class ScenarioRotationEntry
  {
    /// <summary>
    /// Unique scenario identifier (e.g., "{ECC61978EDCC2B5A}Missions/23_Campaign.conf")
    /// </summary>
    public string ScenarioId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable scenario name (e.g., "Conflict — Everon")
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Duration of this scenario in the rotation, in minutes (typically 5–1440, i.e., 5 min to 24 hrs)
    /// </summary>
    public int DurationMin { get; set; } = 60;

    public ScenarioRotationEntry() { }

    public ScenarioRotationEntry(string scenarioId, string label, int durationMin)
    {
      ScenarioId = scenarioId;
      Label = label;
      DurationMin = durationMin;
    }

    public override string ToString() => $"{Label} ({DurationMin} min)";
  }
}
