/******************************************************************************
 * File Name:    Colors.cs
 * Project:      Longbow
 * Description:  This file contains all color constants for the Sitrep brand theme
 *
 * Author:       Claude Code
 ******************************************************************************/

using System.Drawing;

namespace ReforgerServerApp
{
  /// <summary>
  /// Color scheme for Longbow UI. Uses Sitrep brand colors with dark theme.
  /// Primary: Sky Blue (#0ea5e9)
  /// Background: Dark Gray (#1f2937)
  /// </summary>
  internal static class UIColors
  {
    // Primary brand colors
    public static readonly Color Primary = Color.FromArgb(0x0e, 0xa5, 0xe9);        // Sky Blue #0ea5e9
    public static readonly Color PrimaryDark = Color.FromArgb(0x0d, 0x94, 0xd4);    // Darker sky blue for hover
    public static readonly Color PrimaryLight = Color.FromArgb(0x38, 0xbd, 0xf8);   // Lighter sky blue

    // Background colors
    public static readonly Color DarkBackground = Color.FromArgb(0x1f, 0x29, 0x37); // #1f2937
    public static readonly Color DarkerBackground = Color.FromArgb(0x16, 0x1b, 0x22); // Darker for panels
    public static readonly Color LightBackground = Color.FromArgb(0x30, 0x3a, 0x47); // Slightly lighter

    // Text colors
    public static readonly Color TextLight = Color.FromArgb(0xf0, 0xf0, 0xf0);      // Light gray/white
    public static readonly Color TextMuted = Color.FromArgb(0xa0, 0xa0, 0xa0);      // Muted gray
    public static readonly Color TextDark = Color.FromArgb(0x20, 0x20, 0x20);       // Dark for light backgrounds

    // State colors
    public static readonly Color Success = Color.FromArgb(0x22, 0xc5, 0x5e);        // Green #22c55e
    public static readonly Color Warning = Color.FromArgb(0xf5, 0x97, 0x16);        // Orange/Amber #f59716
    public static readonly Color Error = Color.FromArgb(0xef, 0x44, 0x44);          // Red #ef4444
    public static readonly Color Info = Color.FromArgb(0x3b, 0x82, 0xf6);           // Blue #3b82f6

    // UI element colors
    public static readonly Color ButtonBorder = Color.FromArgb(0x4b, 0x55, 0x63);   // Subtle border
    public static readonly Color ButtonDisabled = Color.FromArgb(0x6b, 0x7b, 0x8c); // Disabled state
    public static readonly Color GroupBoxBorder = Color.FromArgb(0x3b, 0x4a, 0x57); // GroupBox border

    // Disabled state
    public static readonly Color DisabledBackground = Color.FromArgb(0x3b, 0x44, 0x51);
    public static readonly Color DisabledText = Color.FromArgb(0x80, 0x90, 0xa0);
  }
}
