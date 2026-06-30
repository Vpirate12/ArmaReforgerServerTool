/******************************************************************************
 * File Name:    Colors.cs
 * Project:      Sentinel Desktop
 * Description:  STG brand color tokens. Reference: BRAND.md
 *
 * Author:       Claude Code
 ******************************************************************************/

using System.Drawing;

namespace ReforgerServerApp
{
  /// <summary>
  /// STG brand color system for Sentinel Desktop. Sky blue accent, deep slate neutrals.
  /// Follow BRAND.md for all color decisions.
  /// </summary>
  internal static class UIColors
  {
    // Brand — Sky Blue (primary accent)
    public static readonly Color BrandLight = Color.FromArgb(0x7d, 0xd3, 0xfc);      // brand-300 #7DD3FC
    public static readonly Color BrandAccent = Color.FromArgb(0x38, 0xbd, 0xf8);     // brand-400 #38BDF8
    public static readonly Color BrandPrimary = Color.FromArgb(0x0e, 0xa5, 0xe9);    // brand-500 #0EA5E9
    public static readonly Color BrandDefault = Color.FromArgb(0x02, 0x84, 0xc7);    // brand-600 (button default)
    public static readonly Color BrandHover = Color.FromArgb(0x03, 0x69, 0xa1);      // brand-700 (hover)
    public static readonly Color BrandActive = Color.FromArgb(0x07, 0x59, 0x85);     // brand-800 (active)
    public static readonly Color BrandDark = Color.FromArgb(0x0c, 0x4a, 0x6e);       // brand-900

    // Neutrals — Deep Slate (SteamOS-adjacent)
    public static readonly Color Canvas = Color.FromArgb(0x05, 0x08, 0x0c);          // #05080C
    public static readonly Color Surface = Color.FromArgb(0x0e, 0x15, 0x1d);         // #0E151D
    public static readonly Color Raised = Color.FromArgb(0x18, 0x22, 0x2e);          // #18222E
    public static readonly Color Line = Color.FromArgb(0x25, 0x32, 0x3f);            // #25323F
    public static readonly Color Muted = Color.FromArgb(0x8a, 0x97, 0xa6);           // #8A97A6
    public static readonly Color Faint = Color.FromArgb(0x5b, 0x68, 0x77);           // #5B6877
    public static readonly Color TextLight = Color.FromArgb(0xe7, 0xed, 0xf3);       // #E7EDF3

    // Semantic — Status only (use sparingly)
    public static readonly Color Online = Color.FromArgb(0x34, 0xd3, 0x99);          // ok/online #34D399
    public static readonly Color Offline = Color.FromArgb(0xf8, 0x71, 0x71);         // danger/offline #F87171
    public static readonly Color Warning = Color.FromArgb(0xfb, 0xbf, 0x24);         // admin/warning #FBBF24

    // Legacy aliases (deprecated, use above)
    [System.Obsolete("Use BrandDefault instead")]
    public static readonly Color Primary = Color.FromArgb(0x0e, 0xa5, 0xe9);

    [System.Obsolete("Use BrandHover instead")]
    public static readonly Color PrimaryDark = Color.FromArgb(0x03, 0x69, 0xa1);

    [System.Obsolete("Use BrandAccent instead")]
    public static readonly Color PrimaryLight = Color.FromArgb(0x38, 0xbd, 0xf8);

    [System.Obsolete("Use Surface instead")]
    public static readonly Color DarkBackground = Color.FromArgb(0x0e, 0x15, 0x1d);

    [System.Obsolete("Use Canvas instead")]
    public static readonly Color DarkerBackground = Color.FromArgb(0x05, 0x08, 0x0c);

    [System.Obsolete("Use Raised instead")]
    public static readonly Color LightBackground = Color.FromArgb(0x18, 0x22, 0x2e);

    [System.Obsolete("Use Offline instead")]
    public static readonly Color Error = Color.FromArgb(0xf8, 0x71, 0x71);

    [System.Obsolete("Use Online instead")]
    public static readonly Color Success = Color.FromArgb(0x34, 0xd3, 0x99);

    [System.Obsolete("Use Muted instead")]
    public static readonly Color TextMuted = Color.FromArgb(0x8a, 0x97, 0xa6);

    [System.Obsolete("Use Canvas for text backgrounds")]
    public static readonly Color TextDark = Color.FromArgb(0x05, 0x08, 0x0c);

    [System.Obsolete("Use Line instead")]
    public static readonly Color ButtonBorder = Color.FromArgb(0x25, 0x32, 0x3f);

    [System.Obsolete("Use Faint instead")]
    public static readonly Color ButtonDisabled = Color.FromArgb(0x5b, 0x68, 0x77);

    [System.Obsolete("Use Line instead")]
    public static readonly Color GroupBoxBorder = Color.FromArgb(0x25, 0x32, 0x3f);

    [System.Obsolete("Use Raised instead")]
    public static readonly Color DisabledBackground = Color.FromArgb(0x18, 0x22, 0x2e);

    [System.Obsolete("Use Faint instead")]
    public static readonly Color DisabledText = Color.FromArgb(0x5b, 0x68, 0x77);

    [System.Obsolete("Use BrandPrimary instead")]
    public static readonly Color Info = Color.FromArgb(0x0e, 0xa5, 0xe9);
  }
}
