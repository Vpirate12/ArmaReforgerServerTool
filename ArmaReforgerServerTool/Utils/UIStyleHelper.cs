/******************************************************************************
 * File Name:    UIStyleHelper.cs
 * Project:      Longbow
 * Description:  Helper methods for applying consistent UI styling
 *
 * Author:       Claude Code
 ******************************************************************************/

using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ReforgerServerApp.Utils
{
  /// <summary>
  /// Helper class for applying consistent UI styling across the application.
  /// Provides methods to style buttons, labels, and other controls.
  /// </summary>
  internal static class UIStyleHelper
  {
    /// <summary>
    /// Apply primary button styling (standard action buttons)
    /// </summary>
    public static void StylePrimaryButton(Button button)
    {
      button.BackColor = UIColors.Primary;
      button.ForeColor = UIColors.TextLight;
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderColor = UIColors.PrimaryDark;
      button.FlatAppearance.BorderSize = 1;
      button.FlatAppearance.MouseOverBackColor = UIColors.PrimaryDark;
      button.FlatAppearance.MouseDownBackColor = UIColors.Primary;
      button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
      button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// Apply secondary button styling (standard buttons with dark background)
    /// </summary>
    public static void StyleSecondaryButton(Button button)
    {
      button.BackColor = UIColors.LightBackground;
      button.ForeColor = UIColors.TextLight;
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderColor = UIColors.ButtonBorder;
      button.FlatAppearance.BorderSize = 1;
      button.FlatAppearance.MouseOverBackColor = UIColors.Primary;
      button.FlatAppearance.MouseDownBackColor = UIColors.PrimaryDark;
      button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
      button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// Apply danger button styling (for delete/destructive actions)
    /// </summary>
    public static void StyleDangerButton(Button button)
    {
      button.BackColor = UIColors.Error;
      button.ForeColor = UIColors.TextLight;
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderColor = Color.FromArgb(0xdc, 0x26, 0x26);
      button.FlatAppearance.BorderSize = 1;
      button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xdc, 0x26, 0x26);
      button.FlatAppearance.MouseDownBackColor = UIColors.Error;
      button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
      button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// Style an IconButton with the primary Sitrep brand color
    /// </summary>
    public static void StyleIconButton(IconButton button, bool isPrimary = true)
    {
      if (isPrimary)
      {
        button.BackColor = UIColors.Primary;
        button.ForeColor = UIColors.TextLight;
        button.IconColor = UIColors.TextLight;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = UIColors.PrimaryDark;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = UIColors.PrimaryDark;
        button.FlatAppearance.MouseDownBackColor = UIColors.Primary;
      }
      else
      {
        button.BackColor = UIColors.LightBackground;
        button.ForeColor = UIColors.TextLight;
        button.IconColor = UIColors.TextLight;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = UIColors.ButtonBorder;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = UIColors.Primary;
        button.FlatAppearance.MouseDownBackColor = UIColors.PrimaryDark;
      }
      button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// Style a success-state button (green)
    /// </summary>
    public static void StyleSuccessButton(Button button)
    {
      button.BackColor = UIColors.Success;
      button.ForeColor = UIColors.TextLight;
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderColor = Color.FromArgb(0x16, 0xa3, 0x4a);
      button.FlatAppearance.BorderSize = 1;
      button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x16, 0xa3, 0x4a);
      button.FlatAppearance.MouseDownBackColor = UIColors.Success;
      button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
      button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// Style a warning-state button (orange)
    /// </summary>
    public static void StyleWarningButton(Button button)
    {
      button.BackColor = UIColors.Warning;
      button.ForeColor = UIColors.TextDark;
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderColor = Color.FromArgb(0xd9, 0x67, 0x00);
      button.FlatAppearance.BorderSize = 1;
      button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xd9, 0x67, 0x00);
      button.FlatAppearance.MouseDownBackColor = UIColors.Warning;
      button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
      button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// Style an error-state button (red)
    /// </summary>
    public static void StyleErrorButton(Button button)
    {
      button.BackColor = UIColors.Error;
      button.ForeColor = UIColors.TextLight;
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderColor = Color.FromArgb(0xdc, 0x26, 0x26);
      button.FlatAppearance.BorderSize = 1;
      button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xdc, 0x26, 0x26);
      button.FlatAppearance.MouseDownBackColor = UIColors.Error;
      button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
      button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// Reset button to disabled state styling
    /// </summary>
    public static void StyleDisabledButton(Button button)
    {
      button.BackColor = UIColors.DisabledBackground;
      button.ForeColor = UIColors.DisabledText;
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderColor = UIColors.ButtonBorder;
      button.FlatAppearance.BorderSize = 1;
      button.Cursor = Cursors.Default;
    }

    /// <summary>
    /// Style a checkbox for dark theme
    /// </summary>
    public static void StyleCheckBox(CheckBox checkbox)
    {
      checkbox.BackColor = Color.Transparent;
      checkbox.ForeColor = UIColors.TextLight;
      checkbox.Font = new Font("Segoe UI", 9F);
    }

    /// <summary>
    /// Style a label for dark theme
    /// </summary>
    public static void StyleLabel(Label label, bool isBold = false, bool isMuted = false)
    {
      label.BackColor = Color.Transparent;
      label.ForeColor = isMuted ? UIColors.TextMuted : UIColors.TextLight;
      label.Font = new Font("Segoe UI", 9F, isBold ? FontStyle.Bold : FontStyle.Regular);
    }

    /// <summary>
    /// Style a GroupBox for dark theme
    /// </summary>
    public static void StyleGroupBox(GroupBox groupBox)
    {
      groupBox.BackColor = UIColors.DarkBackground;
      groupBox.ForeColor = UIColors.TextLight;
      groupBox.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
    }

    /// <summary>
    /// Style a TextBox for dark theme
    /// </summary>
    public static void StyleTextBox(TextBox textBox)
    {
      textBox.BackColor = UIColors.LightBackground;
      textBox.ForeColor = UIColors.TextLight;
      textBox.BorderStyle = BorderStyle.FixedSingle;
      textBox.Font = new Font("Segoe UI", 9F);
    }

    /// <summary>
    /// Style a ComboBox for dark theme
    /// </summary>
    public static void StyleComboBox(ComboBox comboBox)
    {
      comboBox.BackColor = UIColors.LightBackground;
      comboBox.ForeColor = UIColors.TextLight;
      comboBox.Font = new Font("Segoe UI", 9F);
    }

    /// <summary>
    /// Style the main form background
    /// </summary>
    public static void StyleMainForm(Form form)
    {
      form.BackColor = UIColors.DarkBackground;
      form.ForeColor = UIColors.TextLight;
    }
  }
}
