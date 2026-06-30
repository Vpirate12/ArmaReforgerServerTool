/******************************************************************************
 * File Name:    IValidationEventListener.cs
 * Project:      Longbow
 * Description:  Interface for UI components to subscribe to validation events
 *
 * Author:       Longbow contributors
 ******************************************************************************/

using ReforgerServerApp.Models;

namespace ReforgerServerApp.Interfaces
{
  public interface IValidationEventListener
  {
    /// <summary>
    /// Called when the overall validation state changes
    /// </summary>
    void OnValidationStateChanged(ValidationResult result);

    /// <summary>
    /// Called when a specific validation error occurs
    /// </summary>
    void OnValidationError(ValidationError error);

    /// <summary>
    /// Called when validation begins (e.g., to show progress)
    /// </summary>
    void OnValidationStarted();

    /// <summary>
    /// Called when validation completes
    /// </summary>
    void OnValidationCompleted(bool isValid);
  }
}
