/******************************************************************************
 * File Name:    ScenarioRotationConfig.cs
 * Project:      Sentinel Desktop
 * Description:  Complete rotation configuration with entries, warnings, restart policy
 *
 * Author:       Claude Code
 ******************************************************************************/

using System.Collections.Generic;

namespace ReforgerServerApp.Models
{
  /// <summary>
  /// Configuration for the entire scenario rotation feature.
  /// Serialized to/from JSON and persisted in SavedState.
  /// </summary>
  internal class ScenarioRotationConfig
  {
    /// <summary>
    /// Master enable flag for the rotation feature.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Rotation mode: "sequential" (play in order) or "shuffle" (randomize each cycle).
    /// </summary>
    public string Mode { get; set; } = "sequential";

    /// <summary>
    /// Ordered list of scenarios to rotate through.
    /// </summary>
    public List<ScenarioRotationEntry> Entries { get; set; } = new();

    /// <summary>
    /// Player warning configuration (broadcast before map changes).
    /// </summary>
    public ScenarioRotationWarnings Warnings { get; set; } = new();

    /// <summary>
    /// Server restart policy (when/how to restart during rotation).
    /// </summary>
    public ScenarioRotationRestart Restart { get; set; } = new();
  }

  /// <summary>
  /// Player warning configuration for rotation.
  /// </summary>
  internal class ScenarioRotationWarnings
  {
    /// <summary>
    /// Enable/disable player warnings via RCON.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Lead times (in minutes) before scenario change when warnings fire (e.g., [15, 5, 1]).
    /// </summary>
    public List<int> LeadTimesMin { get; set; } = new() { 15, 5, 1 };

    /// <summary>
    /// Message template sent to players. Supports tokens: {next}, {minutes}, {players}.
    /// </summary>
    public string Template { get; set; } = "Map changes to {next} in {minutes} min";
  }

  /// <summary>
  /// Restart policy for the rotation cycle.
  /// </summary>
  internal class ScenarioRotationRestart
  {
    /// <summary>
    /// Restart policy: "afterCycle" (after full rotation), "everyNHours", "dailyAt".
    /// </summary>
    public string Policy { get; set; } = "afterCycle";

    /// <summary>
    /// Used if Policy == "everyNHours". Restart every N hours.
    /// </summary>
    public int EveryNHours { get; set; } = 6;

    /// <summary>
    /// Used if Policy == "dailyAt". Restart at this time (HH:MM format, e.g., "03:00").
    /// </summary>
    public string DailyAt { get; set; } = "03:00";

    /// <summary>
    /// If true, only restart when the server is empty (no players connected).
    /// </summary>
    public bool OnlyWhenEmpty { get; set; } = false;
  }
}
