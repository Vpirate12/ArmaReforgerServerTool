/******************************************************************************
 * File Name:    ScenarioRotationEntry.cs
 * Project:      Longbow
 * Description:  Model representing a single entry in the scenario rotation list
 *
 * Author:       Community Contribution
 ******************************************************************************/

namespace Longbow.Models
{
  public class ScenarioRotationEntry
  {
    public string ScenarioName { get; set; } = string.Empty;
    public string ScenarioPath { get; set; } = string.Empty;
    public int DurationHours { get; set; } = 4;

    public ScenarioRotationEntry() { }

    public ScenarioRotationEntry(string name, string path, int durationHours = 4)
    {
      ScenarioName = name;
      ScenarioPath = path;
      DurationHours = durationHours;
    }

    public override string ToString() => $"{ScenarioName} ({DurationHours}h)";
  }
}
