/******************************************************************************
 * File Name:    Main.cs
 * Project:      Longbow
 * Description:  This is the Main Form
 * 
 * Author:       Bradley Newman
 ******************************************************************************/

using FontAwesome.Sharp;
using Longbow.Forms;
using Longbow.Managers;
using Longbow.Models;
using Longbow.Utils;
using ReforgerServerApp.Components;
using ReforgerServerApp.Managers;
using ReforgerServerApp.Models;
using ReforgerServerApp.Utils;
using Serilog;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ReforgerServerApp
{
  public partial class Main : Form
  {
    private BindingSource m_availableModsBindingSource;
    private BindingSource m_enabledModsBindingSource;
    private ServerStatusParser m_serverStatusParser;
    private System.Timers.Timer m_statusRefreshTimer;
    private const int MAX_GRAPH_POINTS = 60;
    private const int STATUS_REFRESH_INTERVAL_MS = 500; // Refresh every 500ms for responsive UI

    // Mod dependency validation
    private bool m_modsValidated = false;
    private Button? m_checkModsBtn;
    private Button? m_loadConfigBtn;
    private Button? m_saveConfigBtn;
    private Label? m_checkModsStatusLabel;
    private ProgressBar? m_checkModsProgressBar;
    private CheckBox? m_appendSizeChk;
    private long m_lastTotalModSizeBytes;
    private ServerParameterString? m_serverNameCtrl;

    // Scenario Rotation UI controls
    private CheckBox m_rotationEnabledCheckBox;
    private ListView m_rotationListView;
    private Button m_addRotationScenarioBtn;
    private Button m_removeRotationScenarioBtn;
    private Button m_moveRotationUpBtn;
    private Button m_moveRotationDownBtn;
    private NumericUpDown m_rotationDurationSpinner;

    public Main()
    {
      InitializeComponent();

      ApplyUIThemeAndStyling();

      CreateServerParameterControls();
      CreateAdvancedServerParameterControls();

      serverRunningLabel.Text = string.Empty;

      ProcessManager.GetInstance().UpdateGuiControlsEvent += HandleUpdateGuiControlsEvent;
      ProcessManager.GetInstance().UpdateSteamCmdLogEvent += HandleUpdateSteamCmdLogEvent;
      ProcessManager.GetInstance().UpdateServerStatusEvent += HandleServerStatusEvent;
      ConfigurationManager.GetInstance().UpdateScenarioIdFromLoadedConfigEvent += HandleUpdateScenarioIdFromLoadedConfigEvent;
      ProcessManager.GetInstance().ScenarioRotationSwitchEvent += HandleScenarioRotationSwitchEvent;
      ConfigurationManager.GetInstance().ValidationStateChanged += HandleValidationStateChanged;

      useUpnp.Checked = SavedStateManager.GetInstance().GetLoadedAdvancedSettings().GetValueOrDefault("useUpnp", SavedState.DEFAULT_USE_UPNP).Enabled;
      NetworkManager.GetInstance().useUPnP = useUpnp.Checked;

      useExperimentalCheckBox.Checked = SavedStateManager.GetInstance().GetLoadedAdvancedSettings().GetValueOrDefault("useExperimental", SavedState.DEFAULT_USE_EXPERIMENTAL).Enabled;

      keepServerUpdated.Checked = SavedStateManager.GetInstance().GetLoadedAdvancedSettings().GetValueOrDefault("keepServerUpdated", SavedState.DEFAULT_KEEP_SERVER_UPDATED).Enabled;
      ProcessManager.GetInstance().KeepServerUpdated = keepServerUpdated.Checked;

      // Create tooltips
      CreateTooltips();

      loadedScenarioLabel.Text = "No scenario chosen.";

      UpdateSteamCmdInstallStatus();

      m_serverStatusParser = new();
      m_serverStatusParser.UpdateServerStatus += HandleServerStatusEvent;

      // Initialize Status Refresh Timer for responsive UI updates
      m_statusRefreshTimer = new System.Timers.Timer(STATUS_REFRESH_INTERVAL_MS);
      m_statusRefreshTimer.Elapsed += (s, e) => HandleServerStatusEvent(this, m_serverStatusParser.GetCurrentStatus());
      m_statusRefreshTimer.AutoReset = true;
      m_statusRefreshTimer.Enabled = false;

      HandleServerStatusEvent(this, new());


      m_availableModsBindingSource = new()
      {
        DataSource = ConfigurationManager.GetInstance().GetAvailableMods()
      };

      m_enabledModsBindingSource = new()
      {
        DataSource = ConfigurationManager.GetInstance().GetEnabledMods()
      };

      ResetModFilters();

      ConfigurationManager.GetInstance().AlphabetiseModLists();

      if (ToolPropertiesManager.GetInstance().GetToolProperties().checkForUpdatesOnStartup)
      {
        _ = FileIOManager.CheckForUpdates();
      }
      else
      {
        Log.Information("Main - Skipping update check, checkForUpdatesOnStartup is false in properties.json");
      }

      FileIOManager.CheckForVCRedist();

      // FIXME: Disable the Save Manager for now
      loadSaveGameBtn.Visible = false;
      loadSaveGameBtn.Enabled = false;
      ToolTip loadsaveGameBtnTooltip = new();
      loadsaveGameBtnTooltip.SetToolTip(loadSaveGameBtn, "Disabled for the time being, for now please use the Load Session Save option in Advanced Parameters");

      copyAddressBtn.Enabled = false;
      copyRconAddressBtn.Enabled = false;
      copyJoinCodeBtn.Enabled = false;

      chartFps.ChartAreas[0].BackColor = Color.Transparent;
      chartMem.ChartAreas[0].BackColor = Color.Transparent;

      chartFps.Legends[0].BackColor = Color.Transparent;
      chartMem.Legends[0].BackColor = Color.Transparent;

      var fpsSeries = chartFps.Series["FPS"];
      var memSeries = chartMem.Series["Memory (GB)"];

      fpsSeries.ToolTip = "FPS: #VALY\nTime: #VALX{HH:mm:ss}";
      fpsSeries.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
      chartFps.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss";

      memSeries.ToolTip = "Memory: #VALY GB\nTime: #VALX{HH:mm:ss}";
      memSeries.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
      chartMem.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss";
      memSeries.Color = Color.Orange;
      GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();
      double totalSystemMemoryGb = gcInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0);
      chartMem.ChartAreas[0].AxisY.Maximum = Math.Ceiling(totalSystemMemoryGb);
      chartMem.ChartAreas[0].AxisY.Minimum = 0; // Lock the bottom to 0 for proper scale

      CreateRotationTab();
      CreateModValidationControls();
      ConfigurationManager.GetInstance().GetEnabledMods().ListChanged += OnEnabledModsChanged;
    }

    /// <summary>
    /// Apply Sitrep brand color scheme and styling to all UI elements
    /// </summary>
    private void ApplyUIThemeAndStyling()
    {
      // Style the main form
      UIStyleHelper.StyleMainForm(this);

      // Style primary action buttons (server management)
      StyleIconButtonDark(startServerBtn, true);
      StyleIconButtonDark(downloadSteamCmdBtn, false);
      StyleIconButtonDark(locateServerFilesBtn, false);
      StyleIconButtonDark(deleteServerFilesBtn, false);

      // Style config load/save buttons
      StyleIconButtonDark(loadSettingsBtn, false);
      StyleIconButtonDark(saveSettingsBtn, false);

      // Style scenario and mission buttons
      StyleIconButtonDark(scenarioSelectBtn, false);
      StyleIconButtonDark(editMissionHeaderBtn, false);
      StyleIconButtonDark(loadSaveGameBtn, false);

      // Style mod management buttons
      StyleIconButtonDark(addModBtn, false);
      StyleIconButtonDark(editModBtn, false);
      StyleIconButtonDark(removeModBtn, false);
      StyleIconButtonDark(enableAllModsBtn, false);
      StyleIconButtonDark(disableAllModsBtn, false);
      StyleIconButtonDark(addToEnabledBtn, false);
      StyleIconButtonDark(removeFromEnabledBtn, false);
      StyleIconButtonDark(moveModPosUpBtn, false);
      StyleIconButtonDark(moveModPosDownBtn, false);
      StyleIconButtonDark(exportModsBtn, false);
      StyleIconButtonDark(importModsBtn, false);

      // Style utility buttons
      StyleIconButtonDark(clearLogBtn, false);
      StyleIconButtonDark(aboutBtn, false);

      // Style GroupBoxes for dark theme
      UIStyleHelper.StyleGroupBox(groupBox1);
      UIStyleHelper.StyleGroupBox(groupBox2);
      UIStyleHelper.StyleGroupBox(groupBox3);
      UIStyleHelper.StyleGroupBox(groupBox4);
      UIStyleHelper.StyleGroupBox(groupBox5);
      UIStyleHelper.StyleGroupBox(groupBox6);
      UIStyleHelper.StyleGroupBox(groupBox7);
      UIStyleHelper.StyleGroupBox(serverInfoGroupBox);

      // Style checkboxes
      UIStyleHelper.StyleCheckBox(useUpnp);
      UIStyleHelper.StyleCheckBox(useExperimentalCheckBox);
      UIStyleHelper.StyleCheckBox(keepServerUpdated);

      // Style text inputs
      UIStyleHelper.StyleTextBox(modsSearchTB);
      UIStyleHelper.StyleTextBox(steamCmdLog);
      UIStyleHelper.StyleComboBox(logLevelComboBox);

      // Style labels
      UIStyleHelper.StyleLabel(label15, false, false);
      UIStyleHelper.StyleLabel(label16, false, false);
      UIStyleHelper.StyleLabel(label30, false, false);
      UIStyleHelper.StyleLabel(serverRunningLabel, true, false);
      UIStyleHelper.StyleLabel(steamCmdAlert, false, true);
      UIStyleHelper.StyleLabel(loadedScenarioLabel, false, false);

      // Style tab control and pages
      if (tabControl1 != null)
      {
        tabControl1.BackColor = UIColors.DarkBackground;
        tabControl1.ForeColor = UIColors.TextLight;
        foreach (TabPage page in tabControl1.TabPages)
        {
          page.BackColor = UIColors.DarkBackground;
          page.ForeColor = UIColors.TextLight;
        }
      }

      // Style the advanced parameters panel
      if (advancedParametersPanel != null)
      {
        advancedParametersPanel.BackColor = UIColors.DarkBackground;
        advancedParametersPanel.ForeColor = UIColors.TextLight;
      }

      // Style server parameters panel
      if (serverParameters != null)
      {
        serverParameters.BackColor = UIColors.DarkBackground;
        serverParameters.ForeColor = UIColors.TextLight;
      }

      // Style list boxes
      if (availableMods != null)
      {
        availableMods.BackColor = UIColors.LightBackground;
        availableMods.ForeColor = UIColors.TextLight;
      }

      if (enabledMods != null)
      {
        enabledMods.BackColor = UIColors.LightBackground;
        enabledMods.ForeColor = UIColors.TextLight;
      }

      // Style status labels
      for (int i = 0; i < this.Controls.Count; i++)
      {
        if (this.Controls[i] is Label lbl)
        {
          UIStyleHelper.StyleLabel(lbl, false, false);
        }
      }
    }

    /// <summary>
    /// Helper to style icon buttons for dark theme
    /// </summary>
    private void StyleIconButtonDark(FontAwesome.Sharp.IconButton button, bool isPrimary)
    {
      button.BackColor = isPrimary ? UIColors.Primary : UIColors.LightBackground;
      button.ForeColor = UIColors.TextLight;
      button.IconColor = UIColors.TextLight;
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderColor = isPrimary ? UIColors.PrimaryDark : UIColors.ButtonBorder;
      button.FlatAppearance.BorderSize = 1;
      if (isPrimary)
      {
        button.FlatAppearance.MouseOverBackColor = UIColors.PrimaryDark;
        button.FlatAppearance.MouseDownBackColor = UIColors.Primary;
      }
      else
      {
        button.FlatAppearance.MouseOverBackColor = UIColors.Primary;
        button.FlatAppearance.MouseDownBackColor = UIColors.PrimaryDark;
      }
      button.Cursor = Cursors.Hand;
    }

    /// <summary>
    /// Initialise tool tips for certain UI elements.
    /// </summary>
    private void CreateTooltips()
    {
      ToolTip enableAllModsToolTip = new();
      enableAllModsToolTip.SetToolTip(enableAllModsBtn, Constants.ENABLE_ALL_MODS_STR);
      ToolTip disableAllModsToolTip = new();
      disableAllModsToolTip.SetToolTip(disableAllModsBtn, Constants.DISABLE_ALL_MODS_STR);
      ToolTip enableModToolTip = new();
      enableModToolTip.SetToolTip(addToEnabledBtn, Constants.ENABLE_MOD_STR);
      ToolTip disableModToolTip = new();
      disableModToolTip.SetToolTip(removeFromEnabledBtn, Constants.DISABLE_MOD_STR);
      ToolTip useUpnpToolTip = new();
      useUpnpToolTip.SetToolTip(useUpnp, Constants.USE_UPNP_STR);
      ToolTip exportModsToolTip = new();
      exportModsToolTip.SetToolTip(exportModsBtn, Constants.EXPORT_MODS_STR);
      ToolTip importModsToolTip = new();
      exportModsToolTip.SetToolTip(importModsBtn, Constants.IMPORT_MODS_STR);
      ToolTip startServerToolTip = new();
      startServerToolTip.SetToolTip(startServerBtn, Constants.START_SERVER_STR);
      ToolTip downloadToolTip = new();
      downloadToolTip.SetToolTip(downloadSteamCmdBtn, Constants.DOWNLOAD_SERVER_FILES_STR);
      ToolTip locateServerToolTip = new();
      locateServerToolTip.SetToolTip(locateServerFilesBtn, Constants.LOCATE_SERVER_FILES_STR);
      ToolTip deleteServerToolTip = new();
      deleteServerToolTip.SetToolTip(deleteServerFilesBtn, Constants.DELETE_SERVER_FILES_STR);
      ToolTip useExperimentalToolTip = new();
      useExperimentalToolTip.SetToolTip(useExperimentalCheckBox, Constants.USE_EXPERIMENTAL_STR);
      ToolTip keepServerUpdatedToolTip = new();
      keepServerUpdatedToolTip.SetToolTip(keepServerUpdated, Constants.KEEP_SERVER_UPDATED_STR);
    }

    /// <summary>
    /// This method is used to control the state of the controls used to Download SteamCMD and start the server.
    /// If SteamCMD is detected, The message telling the user to Download SteamCMD is hidden, 
    /// the Download button is disabled and the Start Server button is enabled.
    /// </summary>
    private void UpdateSteamCmdInstallStatus()
    {
      if (steamCmdAlert.InvokeRequired)
      {
        steamCmdAlert.Invoke(new Action(() => UpdateSteamCmdInstallStatus()));
      }
      else
      {
        if (FileIOManager.GetInstance().IsSteamCMDInstalled())
        {
          steamCmdAlert.Text = $"Using Arma Reforger Server found at: \"{FileIOManager.GetInstance().GetInstallDirectory()}\"";
          downloadSteamCmdBtn.Enabled = false;
          UpdateStartButtonState();
          deleteServerFilesBtn.Enabled = true;
          loadSaveGameBtn.Enabled = true;
        }
        else
        {
          steamCmdAlert.Text = "SteamCMD and the server files were not detected, please Download before continuing.";
          startServerBtn.Enabled = false;
          downloadSteamCmdBtn.Enabled = true;
          deleteServerFilesBtn.Enabled = false;
          loadSaveGameBtn.Enabled = false;
        }
      }
    }

    /// <summary>
    /// Get the Enabled Mods ListBox
    /// </summary>
    /// <returns>enabledMods ListBox</returns>
    public ListBox GetEnabledModsList()
    {
      return enabledMods;
    }

    /// <summary>
    /// Get the Available Mods ListBox
    /// </summary>
    /// <returns>availableMods ListBox</returns>
    public ListBox GetAvailableModsList()
    {
      return availableMods;
    }

    /// <summary>
    /// Show the Mod Dialog when the "Add Mod" button is pressed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AddModBtnPressed(object sender, EventArgs e)
    {
      AddModDialog addModDialog = new(this);
      addModDialog.ShowDialog();
    }

    /// <summary>
    /// Event Handler for when the Selected Mod changes in the Available Mods
    /// list
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AvailableModsSelectedIndexChanged(object sender, EventArgs e)
    {
      // If the server is running, we don't want to be able to edit / remove mods
      if (string.IsNullOrWhiteSpace(serverRunningLabel.Text))
      {
        editModBtn.Enabled = availableMods.SelectedItem != null;
        removeModBtn.Enabled = availableMods.SelectedItem != null;
      }
    }

    /// <summary>
    /// Event Handler for when the Edit Mod button is pressed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EditModBtnPressed(object sender, EventArgs e)
    {
      if (availableMods.SelectedItem != null)
      {
        AddModDialog addModDialog = new(this, (Mod)availableMods.SelectedItem, availableMods.SelectedIndex);
        addModDialog.ShowDialog();
      }
    }

    /// <summary>
    /// Remove the selected mod(s) from the Available Mods ListBox when the "Remove Mod" button is pressed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RemoveSelectedModBtnPressed(object sender, EventArgs e)
    {
      Mod[] modsToDelete = new Mod[GetAvailableModsList().SelectedItems.Count];
      GetAvailableModsList().SelectedItems.CopyTo(modsToDelete, 0);

      var availableMods = ConfigurationManager.GetInstance().GetAvailableMods();
      bool hasDeletedAtLeastOne = false;

      foreach (Mod mod in modsToDelete)
      {
        if (availableMods.Remove(mod))
          hasDeletedAtLeastOne = true;
      }

      if (hasDeletedAtLeastOne)
      {
        FileIOManager.GetInstance().WriteModsDatabase();
      }
    }

    /// <summary>
    /// When the "Add to Enabled Mods" button (which currently looks like '>') is pressed, 
    /// remove the entry from the Available Mods ListBox and add the entry to the Enabled Mods ListBox.
    /// This method also calls the AlphabetiseModListBox method so the ListBoxes are always 
    /// displaying the mods in alphabetical order.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AddToEnabledModsBtnPressed(object sender, EventArgs e)
    {
      Mod[] modsToMove = new Mod[GetAvailableModsList().SelectedItems.Count];
      GetAvailableModsList().SelectedItems.CopyTo(modsToMove, 0);
      foreach (Mod mod in modsToMove)
      {
        // Move mod from Available Mods -> Enabled Mods
        ConfigurationManager.MoveMod(mod, ConfigurationManager.GetInstance().GetAvailableMods(),
                                        ConfigurationManager.GetInstance().GetEnabledMods());
      }
      ConfigurationManager.GetInstance().AlphabetiseModLists();
      ResetModFilters();
    }

    /// <summary>
    /// When the "Remove From Enabled Mods" button (which currently looks like '<') is pressed, 
    /// remove the entry from the Enabled Mods ListBox and add the entry to the Available Mods ListBox.
    /// This method also calls the AlphabetiseModListBox method so the ListBoxes are always 
    /// displaying the mods in alphabetical order.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RemovedFromEnabledModsBtnPressed(object sender, EventArgs e)
    {
      Mod[] modsToMove = new Mod[GetEnabledModsList().SelectedItems.Count];
      GetEnabledModsList().SelectedItems.CopyTo(modsToMove, 0);
      foreach (Mod mod in modsToMove)
      {
        // Move mod from Enabled Mods -> Available Mods
        ConfigurationManager.MoveMod(mod, ConfigurationManager.GetInstance().GetEnabledMods(),
                                        ConfigurationManager.GetInstance().GetAvailableMods());
      }
      ConfigurationManager.GetInstance().AlphabetiseModLists();
      ResetModFilters();
    }

    /// <summary>
    /// Event handler for when the Mod Position Up button is pressed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MoveEnabledModPositionUpBtnPressed(object sender, EventArgs e)
    {
      if ((Mod) GetEnabledModsList().SelectedItem != null)
      {
        Mod m = (Mod)GetEnabledModsList().SelectedItem;

        // Set move backward to true as moving position 'up' actually means moving the mod earlier in the list
        Utilities.MoveItem(ConfigurationManager.GetInstance().GetEnabledMods(), m, true);

        // Re-select the mod so we can do multiple moves in a row if we like
        GetEnabledModsList().SelectedItems.Clear();
        GetEnabledModsList().SelectedItem = m;
      }
    }

    /// <summary>
    /// Event handler for when the Mod Position Down button is pressed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MoveEnabledModPositionDownBtnPressed(object sender, EventArgs e)
    {
      if ((Mod) GetEnabledModsList().SelectedItem != null)
      {
        Mod m = (Mod)GetEnabledModsList().SelectedItem;

        // Move forward is the default, this will mean moving the mod later in the list
        Utilities.MoveItem(ConfigurationManager.GetInstance().GetEnabledMods(), m);

        // Re-select the mod so we can do multiple moves in a row if we like
        GetEnabledModsList().SelectedItems.Clear();
        GetEnabledModsList().SelectedItem = m;
      }
    }

    /// <summary>
    /// Save the server settings to a JSON file
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SaveSettingsToFileBtnPressed(object sender, EventArgs e)
    {
      FileIOManager.SaveConfigurationToFile();
    }

    /// <summary>
    /// Load the server settings from a JSON file
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LoadSettingsFromFileBtnPressed(object sender, EventArgs e)
    {
      ResetModFilters();
      FileIOManager.LoadConfigurationFromFile();
    }

    /// <summary>
    /// Handler for the "Download" button under the "Server Management" tab.
    /// This method will allow the user to pick a destination for SteamCMD and the Arma Reforger 
    /// Server files before downloading SteamCMD in the chosen directory.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DownloadSteamCmdBtnPressed(object sender, EventArgs e)
    {
      Task steamCmdTask = FileIOManager.GetInstance().DownloadSteamCMD();
      steamCmdTask.ContinueWith(t =>
      {
        UpdateSteamCmdInstallStatus();
      });
      FileIOManager.GetInstance().InstallNoBackendScenarioLoader();
    }

    /// <summary>
    /// This is the handler for the Start Server Button. This is also used for the automatic server restart functionality.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void StartServerBtnPressed(object sender, EventArgs e)
    {
      AdvancedServerParameterTime? autoRestartDaily =
          ConfigurationManager.GetInstance().GetAdvancedServerParametersDictionary()["autoRestartDaily"] as AdvancedServerParameterTime;

      if (autoRestartDaily == null)
      {
        Log.Error("Main - Failed to start server due to issues with auto restart logic. Cannot continue.");
        return;
      }

      bool isStarting = !ProcessManager.GetInstance().IsServerStarted();

      // If we are starting the server for the first time and using the automatic restart functionality, configure the timer
      if (autoRestartDaily.Checked() && !ProcessManager.GetInstance().IsServerUsingTimer())
      {
        CreateLaunchArguments();
        ProcessManager.GetInstance().ConfigureAutomaticRestartTask();
      }

      // The user is turning the server off manually
      else if (autoRestartDaily.Checked() && ProcessManager.GetInstance().IsServerUsingTimer())
      {
        ProcessManager.GetInstance().CancelAutomaticRestartTask();
      }

      // User just normally pressed the button
      else if (!autoRestartDaily.Checked() && !ProcessManager.GetInstance().IsServerUsingTimer())
      {
        CreateLaunchArguments();
        ProcessManager.GetInstance().StartStopServer();
      }

      // Interval restart is independent of the daily restart — handle it separately
      AdvancedServerParameterNumeric? intervalRestartParam =
          ConfigurationManager.GetInstance().GetAdvancedServerParametersDictionary()["intervalRestartHours"] as AdvancedServerParameterNumeric;

      if (intervalRestartParam != null && intervalRestartParam.Checked())
      {
        if (isStarting)
          ProcessManager.GetInstance().ConfigureIntervalRestartTask(Convert.ToInt32(intervalRestartParam.ParameterValue));
        else
          ProcessManager.GetInstance().CancelIntervalRestartTask();
      }
      else if (!isStarting)
      {
        ProcessManager.GetInstance().CancelIntervalRestartTask();
      }

      // Scenario rotation — independent of other restart modes
      if (m_rotationEnabledCheckBox != null && m_rotationEnabledCheckBox.Checked && m_rotationListView != null && m_rotationListView.Items.Count > 0)
      {
        if (isStarting)
        {
          var entries = GetRotationEntriesFromListView();
          if (entries.Count > 0)
            ProcessManager.GetInstance().ConfigureRotationTask(entries);
        }
        else
        {
          ProcessManager.GetInstance().CancelRotationTask();
        }
      }
      else if (!isStarting)
      {
        ProcessManager.GetInstance().CancelRotationTask();
      }
    }

    /// <summary>
    /// Handler for the Enable All Mods Button (displayed as '>>' on the GUI).
    /// Adds all mods from the Available Mods list to the Enabled Mods list and then alphabetises the order.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EnableAllModsBtnPressed(object sender, EventArgs e)
    {
      foreach (Mod m in ConfigurationManager.GetInstance().GetAvailableMods())
      {
        if (!ConfigurationManager.GetInstance().GetEnabledMods().Contains(m))
        {
          ConfigurationManager.GetInstance().GetEnabledMods().Add(new(m));
        }
      }
      ConfigurationManager.GetInstance().GetAvailableMods().Clear();
      ConfigurationManager.GetInstance().AlphabetiseModLists();
      ResetModFilters();
    }

    /// <summary>
    /// Handler for the Disable All Mods Button (displayed as '<<' on the GUI).
    /// Adds all mods from the Enabled Mods list to the Available Mods list and then alphabetises the order.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DisableAllModsBtnPressed(object sender, EventArgs e)
    {
      foreach (Mod m in ConfigurationManager.GetInstance().GetEnabledMods())
      {
        if (!ConfigurationManager.GetInstance().GetAvailableMods().Contains(m))
        {
          ConfigurationManager.GetInstance().GetAvailableMods().Add(new(m));
        }
      }
      ConfigurationManager.GetInstance().GetEnabledMods().Clear();
      ConfigurationManager.GetInstance().AlphabetiseModLists();
      ResetModFilters();
    }

    /// <summary>
    /// Handler for the about button, displays information about the program itself.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AboutBtnPressed(object sender, EventArgs e)
    {
      AboutBox ab = new();
      ab.ShowDialog();
    }

    /// <summary>
    /// Handler for the "Delete Server Files" button.
    /// Deletes all server files and references to them.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DeleteServerFilesBtnPressed(object sender, EventArgs e)
    {
      if (FileIOManager.GetInstance().DeleteServerFiles())
      {
        UpdateSteamCmdInstallStatus();
      }
    }

    /// <summary>
    /// Handler for the "Locate Server Files" button.
    /// Allows the user to navigate to a directory and set the Server Files installation directory without downloading the files.
    /// Useful for moving installation directories around.
    /// Informs the user if the server files were not located.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LocateServerFilesBtnPressed(object sender, EventArgs e)
    {
      if (FileIOManager.GetInstance().LocateServerFiles())
      {
        UpdateSteamCmdInstallStatus();
      }
      FileIOManager.GetInstance().InstallNoBackendScenarioLoader();
    }

    /// <summary>
    /// Enable / Disable Server Configuration Fields
    /// </summary>
    /// <param name="enabled"></param>
    private void EnableServerFields(bool enabled)
    {
      foreach (KeyValuePair<string, ServerParameter> param in
          ConfigurationManager.GetInstance().GetServerParametersDictionary())
      {
        param.Value.SetFieldEnabled(enabled);
      }

      foreach (KeyValuePair<string, AdvancedServerParameter> param in
          ConfigurationManager.GetInstance().GetAdvancedServerParametersDictionary())
      {
        param.Value.SetEnabled(enabled);
      }

      enableAllModsBtn.Enabled = enabled;
      addToEnabledBtn.Enabled = enabled;
      disableAllModsBtn.Enabled = enabled;
      removeFromEnabledBtn.Enabled = enabled;
      loadSettingsBtn.Enabled = enabled;
      saveSettingsBtn.Enabled = enabled;
      addModBtn.Enabled = enabled;
      removeModBtn.Enabled = enabled;
      editModBtn.Enabled = enabled;
      deleteServerFilesBtn.Enabled = enabled;
      locateServerFilesBtn.Enabled = enabled;
      logLevelComboBox.Enabled = enabled;
      scenarioSelectBtn.Enabled = enabled;
      editMissionHeaderBtn.Enabled = enabled;
      useExperimentalCheckBox.Enabled = enabled;
      useUpnp.Enabled = enabled;
      moveModPosUpBtn.Enabled = enabled;
      moveModPosDownBtn.Enabled = enabled;
      loadSaveGameBtn.Enabled = enabled;
      keepServerUpdated.Enabled = enabled;

      if (m_checkModsBtn != null)
        m_checkModsBtn.Enabled = enabled;

      // The clipboard buttons are the opposite
      copyAddressBtn.Enabled = !enabled;
      copyRconAddressBtn.Enabled = !enabled;
      copyJoinCodeBtn.Enabled = !enabled;

      // Rotation tab controls
      if (m_rotationEnabledCheckBox != null) m_rotationEnabledCheckBox.Enabled = enabled;
      if (m_addRotationScenarioBtn != null) m_addRotationScenarioBtn.Enabled = enabled;
      if (m_removeRotationScenarioBtn != null) m_removeRotationScenarioBtn.Enabled = enabled;
      if (m_moveRotationUpBtn != null) m_moveRotationUpBtn.Enabled = enabled;
      if (m_moveRotationDownBtn != null) m_moveRotationDownBtn.Enabled = enabled;
      if (m_rotationDurationSpinner != null) m_rotationDurationSpinner.Enabled = enabled;
    }

    /// <summary>
    /// Handler for the "Clear Log" button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ClearLogBtnPressed(object sender, EventArgs e)
    {
      steamCmdLog.Text = string.Empty;
    }

    /// <summary>
    /// Handler for when the Scenario Select button is pressed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ScenarioSelectBtnClicked(object sender, EventArgs e)
    {
      SpawnScenarioSelect();
    }

    /// <summary>
    /// Little hacky method for refreshing the Scenario ID value, 
    /// this is called from within the Scenario Selector form
    /// At some stage I should probably make this whole thing MVC
    /// </summary>
    public void RefreshLoadedScenario()
    {
      loadedScenarioLabel.Text = ConfigurationManager.GetInstance().GetServerConfiguration().root.game.scenarioId;
    }

    /// <summary>
    /// Logic for starting the Scenario Select form
    /// </summary>
    private void SpawnScenarioSelect()
    {
      ScenarioSelector scenarioSelector = new(this);
      scenarioSelector.ShowDialog();
    }

    private void SpawnSaveSelect()
    {
      SaveSelector saveSelector = new();
      saveSelector.ShowDialog();
    }

    private void EditMissionHeaderBtnClicked(object sender, EventArgs e)
    {
      TextInputForm tif = new("Edit Mission Header", ConfigurationManager.GetInstance().GetServerConfiguration().MissionHeaderAsJsonString());
      tif.ShowDialog();
      ConfigurationManager.GetInstance().GetServerConfiguration().SetMissionHeaderFromJson(tif.GetText());
    }

    private void EditAdminsListBtnClicked(object sender, EventArgs e)
    {
      ListForm lf = new("Edit Admins", ConfigurationManager.GetInstance().GetServerConfiguration().root.game.admins);
      lf.ShowDialog();
      ConfigurationManager.GetInstance().GetServerConfiguration().root.game.admins = lf.GetItems();
    }

    /// <summary>
    /// Populate Config Managers Server Parameter UI controls to easily retrieve values and send them to the model
    /// </summary>
    private void CreateServerParameterControls()
    {
      ServerParameterString serverName = new()
      {
        ParameterName = "name",
        ParameterFriendlyName = "Server Name",
        ParameterTooltip = Constants.SERVER_PARAM_NAME_TOOLTIP_STR
      };
      m_serverNameCtrl = serverName;
      serverParameters.Controls.Add(serverName);
      ServerParameterString serverPassword = new()
      {
        ParameterName = "password",
        ParameterFriendlyName = "Server Password",
        ParameterTooltip = Constants.SERVER_PARAM_PASSWORD_TOOLTIP_STR
      };
      serverParameters.Controls.Add(serverPassword);
      ServerParameterString adminPassword = new()
      {
        ParameterName = "passwordAdmin",
        ParameterFriendlyName = "Admin Password",
        ParameterTooltip = Constants.SERVER_PARAM_ADMIN_PASSWORD_TOOLTIP_STR
      };
      serverParameters.Controls.Add(adminPassword);
      ServerParameterList admins = new()
      {
        ParameterName = "admins",
        ParameterFriendlyName = "Admins",
        ParameterTooltip = Constants.SERVER_PARAM_ADMINS_TOOLTIP_STR,
        ParameterList = ConfigurationManager.GetInstance()
                                              .GetServerConfiguration()
                                              .root.game.admins
      };
      serverParameters.Controls.Add(admins);
      ServerParameterBool modsRequiredByDefault = new()
      {
        ParameterName = "modsRequiredByDefault",
        ParameterFriendlyName = "Mods Required by Default",
        ParameterTooltip = Constants.SERVER_PARAM_MODS_REQUIRED_BY_DEFAULT_TOOLTIP_STR
      };
      serverParameters.Controls.Add(modsRequiredByDefault);
      ServerParameterNumeric maxPlayers = new()
      {
        ParameterName = "maxPlayers",
        ParameterFriendlyName = "Max Players",
        ParameterIncrement = 1,
        ParameterMin = Game.MIN_PLAYERS,
        ParameterMax = Game.MAX_PLAYERS,
        ParameterValue = Game.DEFAULT_PLAYERS,
        ParameterTooltip = Constants.SERVER_PARAM_MAX_PLAYERS_TOOLTIP_STR
      };
      serverParameters.Controls.Add(maxPlayers);
      ServerParameterBool visible = new()
      {
        ParameterName = "visible",
        ParameterFriendlyName = "Server Visible",
        ParameterValue = Game.DEFAULT_VISIBLE,
        ParameterTooltip = Constants.SERVER_PARAM_VISIBLE_TOOLTIP_STR
      };
      serverParameters.Controls.Add(visible);
      ServerParameterString bindAddress = new()
      {
        ParameterName = "bindAddress",
        ParameterFriendlyName = "Bind Address",
        ParameterValue = Root.DEFAULT_BIND_ADDRESS,
        ParameterTooltip = Constants.SERVER_PARAM_BIND_ADDRESS_TOOLTIP_STR
      };
      serverParameters.Controls.Add(bindAddress);
      ServerParameterNumeric bindPort = new()
      {
        ParameterName = "bindPort",
        ParameterFriendlyName = "Bind Port",
        ParameterIncrement = 1,
        ParameterMin = Constants.SERVER_PARAM_MIN_PORT,
        ParameterMax = Constants.SERVER_PARAM_MAX_PORT,
        ParameterValue = Root.DEFAULT_PORT,
        ParameterTooltip = Constants.SERVER_PARAM_BIND_PORT_TOOLTIP_STR
      };
      serverParameters.Controls.Add(bindPort);
      ServerParameterString publicAddress = new()
      {
        ParameterName = "publicAddress",
        ParameterFriendlyName = "Public Address",
        ParameterTooltip = Constants.SERVER_PARAM_PUBLIC_ADDRESS_TOOLTIP_STR
      };
      serverParameters.Controls.Add(publicAddress);
      ServerParameterNumeric publicPort = new()
      {
        ParameterName = "publicPort",
        ParameterFriendlyName = "Public Port",
        ParameterIncrement = 1,
        ParameterMin = Constants.SERVER_PARAM_MIN_PORT,
        ParameterMax = Constants.SERVER_PARAM_MAX_PORT,
        ParameterValue = Root.DEFAULT_PORT,
        ParameterTooltip = Constants.SERVER_PARAM_PUBLIC_PORT_TOOLTIP_STR
      };
      serverParameters.Controls.Add(publicPort);
      ServerParameterString a2sAddress = new()
      {
        ParameterName = "address",
        ParameterFriendlyName = "A2S Address",
        ParameterValue = A2S.DEFAULT_ADDRESS,
        ParameterTooltip = Constants.SERVER_PARAM_A2S_ADDRESS_TOOLTIP_STR
      };
      serverParameters.Controls.Add(a2sAddress);
      ServerParameterNumeric a2sPort = new()
      {
        ParameterName = "port",
        ParameterFriendlyName = "A2S Port",
        ParameterIncrement = 1,
        ParameterMin = Constants.SERVER_PARAM_MIN_PORT,
        ParameterMax = Constants.SERVER_PARAM_MAX_PORT,
        ParameterValue = A2S.DEFAULT_PORT,
        ParameterTooltip = Constants.SERVER_PARAM_A2S_PORT_TOOLTIP_STR
      };
      serverParameters.Controls.Add(a2sPort);
      ServerParameterBool rconEnabled = new()
      {
        ParameterName = "rconEnabled",
        ParameterFriendlyName = "Enable Rcon",
        ParameterTooltip = Constants.SERVER_PARAM_ENABLE_RCON_TOOLTIP_STR
      };
      serverParameters.Controls.Add(rconEnabled);
      ServerParameterString rconAddress = new()
      {
        ParameterName = "rconAddress",
        ParameterFriendlyName = "Rcon Address",
        ParameterTooltip = Constants.SERVER_PARAM_RCON_ADDRESS_TOOLTIP_STR
      };
      serverParameters.Controls.Add(rconAddress);
      ServerParameterNumeric rconPort = new()
      {
        ParameterName = "rconPort",
        ParameterFriendlyName = "Rcon Port",
        ParameterIncrement = 1,
        ParameterMin = Constants.SERVER_PARAM_MIN_PORT,
        ParameterMax = Constants.SERVER_PARAM_MAX_PORT,
        ParameterValue = Rcon.DEFAULT_PORT,
        ParameterTooltip = Constants.SERVER_PARAM_RCON_PORT_TOOLTIP_STR
      };
      serverParameters.Controls.Add(rconPort);
      ServerParameterString rconPassword = new()
      {
        ParameterName = "rconPassword",
        ParameterFriendlyName = "Rcon Password",
        ParameterTooltip = Constants.SERVER_PARAM_RCON_PASSWORD_TOOLTIP_STR
      };
      serverParameters.Controls.Add(rconPassword);
      ServerParameterNumeric rconMaxClients = new()
      {
        ParameterName = "rconMaxClients",
        ParameterFriendlyName = "Rcon Max Clients",
        ParameterTooltip = Constants.SERVER_PARAM_RCON_MAX_CLIENTS_TOOLTIP_STR,
        ParameterIncrement = 1,
        ParameterMin = Rcon.MIN_CLIENTS,
        ParameterMax = Rcon.MAX_CLIENTS,
        ParameterValue = Rcon.DEFAULT_CLIENTS
      };
      serverParameters.Controls.Add(rconMaxClients);
      ServerParameterSelect rconPermission = new()
      {
        ParameterName = "rconPermission",
        ParameterFriendlyName = "Rcon Permission",
        ParameterTooltip = Constants.SERVER_PARAM_RCON_PERMISSION_TOOLTIP_STR,
        ParameterValue = Rcon.PERMISSIONS,
      };
      rconPermission.ParameterValueSelection(Rcon.DEFAULT_PERMISSION);
      serverParameters.Controls.Add(rconPermission);
      ServerParameterList rconWhitelist = new()
      {
        ParameterName = "rconWhitelist",
        ParameterFriendlyName = "Rcon Whitelist",
        ParameterTooltip = Constants.SERVER_PARAM_RCON_WHITELIST_TOOLTIP_STR,
        ParameterList = ConfigurationManager.GetInstance()
                                              .GetServerConfiguration()
                                              .root.rcon.whitelist
      };
      serverParameters.Controls.Add(rconWhitelist);
      ServerParameterList rconBlacklist = new()
      {
        ParameterName = "rconBlacklist",
        ParameterFriendlyName = "Rcon Blacklist",
        ParameterTooltip = Constants.SERVER_PARAM_RCON_BLACKLIST_TOOLTIP_STR,
        ParameterList = ConfigurationManager.GetInstance()
                                              .GetServerConfiguration()
                                              .root.rcon.blacklist
      };
      serverParameters.Controls.Add(rconBlacklist);
      ServerParameterNumeric playerSaveTime = new()
      {
        ParameterName = "playerSaveTime",
        ParameterFriendlyName = "Player Save Time (secs)",
        ParameterIncrement = 1,
        ParameterMin = Operating.MIN_PLAYER_SAVE_TIME,
        ParameterMax = Operating.MAX_PLAYER_SAVE_TIME,
        ParameterValue = Operating.DEFAULT_PLAYER_SAVE_TIME,
        ParameterTooltip = Constants.SERVER_PARAM_PLAYER_SAVE_TIME_TOOLTIP_STR
      };
      serverParameters.Controls.Add(playerSaveTime);
      ServerParameterNumeric serverMaxViewDistance = new()
      {
        ParameterName = "serverMaxViewDistance",
        ParameterFriendlyName = "Server Max View Distance",
        ParameterIncrement = 1,
        ParameterMin = GameProperties.MIN_SERVER_VIEW_DISTANCE,
        ParameterMax = GameProperties.MAX_SERVER_VIEW_DISTANCE,
        ParameterValue = GameProperties.DEFAULT_SERVER_VIEW_DISTANCE,
        ParameterTooltip = Constants.SERVER_PARAM_SERVER_MAX_VIEW_DISTANCE_TOOLTIP_STR
      };
      serverParameters.Controls.Add(serverMaxViewDistance);
      ServerParameterNumeric serverMinGrassDistance = new()
      {
        ParameterName = "serverMinGrassDistance",
        ParameterFriendlyName = "Server Min Grass Distance",
        ParameterIncrement = 1,
        ParameterMin = GameProperties.MIN_SERVER_GRASS_DISTANCE,
        ParameterMax = GameProperties.MAX_SERVER_GRASS_DISTANCE,
        ParameterValue = GameProperties.DEFAULT_SERVER_GRASS_DISTANCE,
        ParameterTooltip = Constants.SERVER_PARAM_SERVER_MIN_GRASS_DISTANCE_TOOLTIP_STR
      };
      serverParameters.Controls.Add(serverMinGrassDistance);
      ServerParameterNumeric networkViewDistance = new()
      {
        ParameterName = "networkViewDistance",
        ParameterFriendlyName = "Network View Distance",
        ParameterIncrement = 1,
        ParameterMin = GameProperties.MIN_NETWORK_VIEW_DISTANCE,
        ParameterMax = GameProperties.MAX_NETWORK_VIEW_DISTANCE,
        ParameterValue = GameProperties.DEFAULT_NETWORK_VIEW_DISTANCE,
        ParameterTooltip = Constants.SERVER_PARAM_NETWORK_VIEW_DISTANCE_TOOLTIP_STR
      };
      serverParameters.Controls.Add(networkViewDistance);
      ServerParameterBool disableThirdPerson = new()
      {
        ParameterName = "disableThirdPerson",
        ParameterFriendlyName = "Disable Third Person",
        ParameterValue = GameProperties.DEFAULT_DISABLE_THIRD_PERSON,
        ParameterTooltip = Constants.SERVER_PARAM_DISABLE_THIRD_PERSON_TOOLTIP_STR
      };
      serverParameters.Controls.Add(disableThirdPerson);
      ServerParameterBool fastValidation = new()
      {
        ParameterName = "fastValidation",
        ParameterFriendlyName = "Fast Validation",
        ParameterValue = GameProperties.DEFAULT_FAST_VALIDATION,
        ParameterTooltip = Constants.SERVER_PARAM_FAST_VALIDATION_TOOLTIP_STR
      };
      serverParameters.Controls.Add(fastValidation);
      ServerParameterBool battlEye = new()
      {
        ParameterName = "battlEye",
        ParameterFriendlyName = "BattlEye",
        ParameterValue = GameProperties.DEFAULT_BATTLE_EYE,
        ParameterTooltip = Constants.SERVER_PARAM_BATTLEYE_TOOLTIP_STR
      };
      serverParameters.Controls.Add(battlEye);
      ServerParameterBool lobbyPlayerSynchronise = new()
      {
        ParameterName = "lobbyPlayerSynchronise",
        ParameterFriendlyName = "Lobby Player Synchronise",
        ParameterValue = Operating.DEFAULT_LOBBY_PLAYER_SYNCHRONISE,
        ParameterTooltip = Constants.SERVER_PARAM_LOBBY_PLAYER_SYNC_TOOLTIP_STR
      };
      serverParameters.Controls.Add(lobbyPlayerSynchronise);
      ServerParameterBool vonDisableUI = new()
      {
        ParameterName = "VONDisableUI",
        ParameterFriendlyName = "VON Disable UI",
        ParameterValue = GameProperties.DEFAULT_VON_DISABLE_UI,
        ParameterTooltip = Constants.SERVER_PARAM_VON_DISABLE_UI_TOOLTIP_STR
      };
      serverParameters.Controls.Add(vonDisableUI);
      ServerParameterBool vonDisableDirectSpeechUI = new()
      {
        ParameterName = "VONDisableDirectSpeechUI",
        ParameterFriendlyName = "VON Disable Direct Speech UI",
        ParameterValue = GameProperties.DEFAULT_VON_DISABLE_DIRECT_SPEECH_UI,
        ParameterTooltip = Constants.SERVER_PARAM_VON_DISABLE_DIRECT_SPEECH_UI_TOOLTIP_STR
      };
      serverParameters.Controls.Add(vonDisableDirectSpeechUI);
      ServerParameterBool vonCanTransmitCrossFaction = new()
      {
        ParameterName = "VONCanTransmitCrossFaction",
        ParameterFriendlyName = "VON Can Transmit Cross Faction",
        ParameterValue = GameProperties.DEFAULT_VON_CAN_TRANSMIT_CROSS_FACTION,
        ParameterTooltip = Constants.SERVER_PARAM_VON_CAN_TRANSMIT_ACROSS_FACTION_TOOLTIP_STR
      };
      serverParameters.Controls.Add(vonCanTransmitCrossFaction);
      ServerParameterBool crossPlatform = new()
      {
        ParameterName = "crossPlatform",
        ParameterFriendlyName = "Cross Platform",
        ParameterValue = Game.DEFAULT_CROSS_PLATFORM,
        ParameterTooltip = Constants.SERVER_PARAM_CROSS_PLATFORM_TOOLTIP_STR
      };
      serverParameters.Controls.Add(crossPlatform);
      ServerParameterNumeric aiLimit = new()
      {
        ParameterName = "aiLimit",
        ParameterFriendlyName = "AI Limit",
        ParameterIncrement = 1,
        ParameterMin = Operating.MIN_AI_LIMIT,
        ParameterMax = Operating.MAX_AI_LIMIT,
        ParameterValue = Operating.DEFAULT_AI_LIMIT,
        ParameterTooltip = Constants.SERVER_PARAM_AI_LIMIT_TOOLTIP_STR
      };
      serverParameters.Controls.Add(aiLimit);
      ServerParameterNumeric slotReservationTimeout = new()
      {
        ParameterName = "slotReservationTimeout",
        ParameterFriendlyName = "Slot Reservation Timeout (secs)",
        ParameterIncrement = 1,
        ParameterMin = Operating.MIN_SLOT_RESERVATION_TIMEOUT,
        ParameterMax = Operating.MAX_SLOT_RESERVATION_TIMEOUT,
        ParameterValue = Operating.DEFAULT_SLOT_RESERVATION_TIMEOUT,
        ParameterTooltip = Constants.SERVER_PARAM_SLOT_RESERVATION_TIMEOUT_TOOLTIP_STR
      };
      serverParameters.Controls.Add(slotReservationTimeout);
      ServerParameterBool toggleDisableNavmeshStreaming = new()
      {
        ParameterName = "toggleDisableNavmeshStreaming",
        ParameterFriendlyName = "Disable Navmesh Streaming",
        ParameterTooltip = Constants.SERVER_PARAM_DISABLE_NAVMESH_STREAMING_TOOLTIP_STR
      };
      serverParameters.Controls.Add(toggleDisableNavmeshStreaming);
      ServerParameterList disableNavmeshStreaming = new()
      {
        ParameterName = "disableNavmeshStreaming",
        ParameterFriendlyName = "Disable Specific Navmesh Streaming",
        ParameterTooltip = Constants.SERVER_PARAM_DISABLE_SPECIFIC_NAVMESH_STREAMING_TOOLTIP_STR,
        ParameterList = ConfigurationManager.GetInstance()
                                              .GetServerConfiguration()
                                              .root.operating.disableNavmeshStreaming
      };
      serverParameters.Controls.Add(disableNavmeshStreaming);
      ServerParameterBool disableServerShutdown = new()
      {
        ParameterName = "disableServerShutdown",
        ParameterFriendlyName = "Disable Server Shutdown",
        ParameterValue = Operating.DEFAULT_DISABLE_SERVER_SHUTDOWN,
        ParameterTooltip = Constants.SERVER_PARAM_DISABLE_SERVER_SHUTDOWN_TOOLTIP_STR
      };
      serverParameters.Controls.Add(disableServerShutdown);
      ServerParameterBool disableCrashReporter = new()
      {
        ParameterName = "disableCrashReporter",
        ParameterFriendlyName = "Disable Crash Reporter",
        ParameterValue = Operating.DEFAULT_DISABLE_CRASH_REPORTER,
        ParameterTooltip = Constants.SERVER_PARAM_DISABLE_CRASH_REPORT_TOOLTIP_STR
      };
      serverParameters.Controls.Add(disableCrashReporter);
      ServerParameterBool disableAI = new()
      {
        ParameterName = "disableAI",
        ParameterFriendlyName = "Disable AI",
        ParameterValue = Operating.DEFAULT_DISABLE_AI,
        ParameterTooltip = Constants.SERVER_PARAM_DISABLE_AI_TOOLTIP_STR
      };
      serverParameters.Controls.Add(disableAI);
      ServerParameterNumeric joinQueueMaxSize = new()
      {
        ParameterName = "maxSize",
        ParameterFriendlyName = "Join Queue Max Size",
        ParameterValue = JoinQueue.DEFAULT_MAX_SIZE,
        ParameterTooltip = Constants.SERVER_PARAM_JOIN_QUEUE_MAX_SIZE_TOOLTIP_STR
      };
      serverParameters.Controls.Add(joinQueueMaxSize);
      ServerParameterNumeric autoSaveInterval = new()
      {
        ParameterName = "autoSaveInterval",
        ParameterFriendlyName = "Auto Save Interval (mins)",
        ParameterValue = Persistence.DEFAULT_AUTOSAVE_INTERVAL_MINS,
        ParameterTooltip = Constants.SERVER_PARAM_AUTO_SAVE_INTERVAL_TOOLTIP_STR
      };
      serverParameters.Controls.Add(autoSaveInterval);
      ServerParameterNumeric hiveId = new()
      {
        ParameterName = "hiveId",
        ParameterFriendlyName = "Hive ID",
        ParameterValue = Persistence.DEFAULT_HIVE_ID,
        ParameterTooltip = Constants.SERVER_PARAM_HIVE_ID_TOOLTIP_STR
      };
      serverParameters.Controls.Add(hiveId);
      ServerParameterText databases = new()
      {
        ParameterName = "databases",
        ParameterFriendlyName = "Databases",
        ParameterValue = Persistence.DEFAULT_DATABASES,
        ParameterTooltip = Constants.SERVER_PARAM_DATABASES_TOOLTIP_STR
      };
      serverParameters.Controls.Add(databases);
      ServerParameterText storages = new()
      {
        ParameterName = "storages",
        ParameterFriendlyName = "Storages",
        ParameterValue = Persistence.DEFAULT_STORAGES,
        ParameterTooltip = Constants.SERVER_PARAM_STORAGES_TOOLTIP_STR
      };
      serverParameters.Controls.Add(storages);

      foreach (ServerParameter param in serverParameters.Controls)
      {
        ConfigurationManager.GetInstance().GetServerParametersDictionary()[param.ParameterName] = param;
      }
    }

    void CreateAdvancedServerParameterControls()
    {
      Dictionary<string, AdvancedSetting> loadedSettings = SavedStateManager.GetInstance().GetSavedState().advancedSettings;

      AdvancedServerParameterNumeric limitServerMaxFPS = new()
      {
        ParameterName = "maxFPS",
        ParameterFriendlyName = "Limit Server Max FPS",
        ParameterMin = 1,
        ParameterMax = 1000,
        ParameterIncrement = 1,
        ParameterValue = loadedSettings["maxFPS"].Value,
        Description = "Limits your server to the specified target FPS. Recommended."
      };
      limitServerMaxFPS.CheckBox.Checked = loadedSettings["maxFPS"].Enabled;
      advancedParametersPanel.Controls.Add(limitServerMaxFPS);

      AdvancedServerParameterTime autoRestartTime = new()
      {
        ParameterName = "autoRestartDaily",
        ParameterFriendlyName = "Auto Restart",
        Description = "Specify, in 24 hour time, at what time the server will restart every day.",
        ParameterMin = DateTime.Today,
        ParameterMax = DateTime.Today.AddDays(1).AddMinutes(-1),
        ParameterValue = DateTime.Today
      };
      advancedParametersPanel.Controls.Add(autoRestartTime);

      AdvancedServerParameterBool addonsRepair = new()
      {
        ParameterName = "addonsRepair",
        ParameterFriendlyName = "Verify and Repair Addons",
        Description ="Verifies the integrity of all installed addons. If any corrupted addons are found, they will be repaired automatically."
      };
      addonsRepair.CheckBox.Checked = loadedSettings["addonsRepair"].Enabled;
      advancedParametersPanel.Controls.Add(addonsRepair);

      AdvancedServerParameterBool autoRestartOnCrash = new()
      {
        ParameterName = "autoRestartOnCrash",
        ParameterFriendlyName = "Restart on Game Destroyed",
        Description = "The tool will monitor the server for crashes and attempt to restart it automatically."
      };
      autoRestartOnCrash.CheckBox.CheckedChanged += AutoRestartOnCrashCheckChanged;
      autoRestartOnCrash.CheckBox.Checked = loadedSettings.ContainsKey("autoRestartOnCrash") ? loadedSettings["autoRestartOnCrash"].Enabled : false;
      advancedParametersPanel.Controls.Add(autoRestartOnCrash);

      AdvancedServerParameterNumeric intervalRestart = new()
      {
        ParameterName = "intervalRestartHours",
        ParameterFriendlyName = "Interval Restart (hours)",
        Description = "Restart the server every N hours with RCON player warnings at 10, 5, and 1 minute(s) before shutdown. Requires RCON to be enabled.",
        ParameterMin = 1,
        ParameterMax = 24,
        ParameterIncrement = 1,
        ParameterValue = Convert.ToInt32(loadedSettings.GetValueOrDefault("intervalRestartHours", SavedState.DEFAULT_INTERVAL_RESTART_HOURS).Value)
      };
      intervalRestart.CheckBox.Checked = loadedSettings.ContainsKey("intervalRestartHours") && loadedSettings["intervalRestartHours"].Enabled;
      advancedParametersPanel.Controls.Add(intervalRestart);

      AdvancedServerParameterNumeric autoReload = new()
      {
        ParameterName = "autoreload",
        ParameterFriendlyName = "Auto Reload Scenario",
        Description = "Automatically reload the scenario when finished after the specified time (in seconds) has elapsed.",
        ParameterMin = 1,
        ParameterMax = int.MaxValue,
        ParameterIncrement = 1,
        ParameterValue = loadedSettings["autoreload"].Value
      };
      autoReload.CheckBox.Checked = loadedSettings["autoreload"].Enabled;
      advancedParametersPanel.Controls.Add(autoReload);

      AdvancedServerParameterString loadSessionSave = new()
      {
        ParameterName = "loadSessionSave",
        ParameterFriendlyName = "Load Session Save",
        Description = "Name of save excluding the path and file extension.\nLeave blank to use the latest save.",
        ParameterPlaceholder = "Using latest save..."
      };
      bool loadSessionSavedEnabled = loadedSettings["loadSessionSave"].Enabled;
      loadSessionSave.CheckBox.Checked = loadSessionSavedEnabled;
      if (loadSessionSavedEnabled)
      {
        loadSessionSave.ParameterValue = loadedSettings["loadSessionSave"].Value;
      }
      advancedParametersPanel.Controls.Add(loadSessionSave);

      AdvancedServerParameterBool noBackend = new()
      {
        ParameterName = "noBackend",
        ParameterFriendlyName = "No Backend",
        Description = "Enable this to host the server without using the Arma Reforger backend."
      };
      noBackend.CheckBox.CheckedChanged += NoBackendCheckChanged;
      noBackend.CheckBox.Checked = loadedSettings["noBackend"].Enabled;
      advancedParametersPanel.Controls.Add(noBackend);

      AdvancedServerParameterBool autoShutdown = new()
      {
        ParameterName = "autoShutdown",
        ParameterFriendlyName = "Auto Shutdown",
        Description = "Ensures the correct server shutdown process, use with \"Auto Reload\"."
      };
      autoShutdown.CheckBox.Checked = loadedSettings["autoShutdown"].Enabled;
      advancedParametersPanel.Controls.Add(autoShutdown);

      AdvancedServerParameterBool logVoting = new()
      {
        ParameterName = "logVoting",
        ParameterFriendlyName = "Log Voting",
        Description = "Adds logging info to the voting system with information about who created, voted, and against whom the vote was created."
      };
      logVoting.CheckBox.Checked = loadedSettings["logVoting"].Enabled;
      advancedParametersPanel.Controls.Add(logVoting);

      AdvancedServerParameterNumeric overridePort = new()
      {
        ParameterName = "bindPort",
        ParameterFriendlyName = "Override Port",
        ParameterMin = 1,
        ParameterMax = 65535,
        ParameterIncrement = 1,
        ParameterValue = loadedSettings["bindPort"].Value,
        Description = "Override the ports specified in server configuration."
      };
      overridePort.CheckBox.Checked = loadedSettings["bindPort"].Enabled;
      advancedParametersPanel.Controls.Add(overridePort);

      AdvancedServerParameterNumeric networkDynamicSim = new()
      {
        ParameterName = "nds",
        ParameterFriendlyName = "Network Dynamic Simulation",
        ParameterMin = 0,
        ParameterMax = 2,
        ParameterIncrement = 1,
        ParameterValue = loadedSettings["nds"].Value,
        Description = "This is set to '2' by default if unchecked."
      };
      networkDynamicSim.CheckBox.Checked = loadedSettings["nds"].Enabled;
      advancedParametersPanel.Controls.Add(networkDynamicSim);

      AdvancedServerParameterNumeric spatialMapRes = new()
      {
        ParameterName = "nwkResolution",
        ParameterFriendlyName = "Spatial Map Resolution",
        ParameterMin = 100,
        ParameterMax = 1000,
        ParameterIncrement = 1,
        ParameterValue = loadedSettings["nwkResolution"].Value,
        Description = "Defines what resolution Spatial Map cells should be set at in a 100 - 1000m range."
      };
      spatialMapRes.CheckBox.Checked = loadedSettings["nwkResolution"].Enabled;
      advancedParametersPanel.Controls.Add(spatialMapRes);

      AdvancedServerParameterNumeric staggeringBudget = new()
      {
        ParameterName = "staggeringBudget",
        ParameterFriendlyName = "Staggering Budget",
        ParameterMin = 1,
        ParameterMax = 10201,
        ParameterIncrement = 1,
        ParameterValue = loadedSettings["staggeringBudget"].Value,
        Description = "Defines how many stationary spatial map cells are allowed to be processed in one tick. If not set it uses \"-nds\" diameter."
      };
      staggeringBudget.CheckBox.Checked = loadedSettings["staggeringBudget"].Enabled;
      advancedParametersPanel.Controls.Add(staggeringBudget);

      AdvancedServerParameterNumeric streamingBudget = new()
      {
        ParameterName = "streamingBudget",
        ParameterFriendlyName = "Streaming Budget",
        ParameterMin = 100,
        ParameterMax = 10201,
        ParameterIncrement = 1,
        ParameterValue = loadedSettings["streamingBudget"].Value,
        Description = "Streaming budget is the global streaming budget that is equally distributed between all connections."
      };
      streamingBudget.CheckBox.Checked = loadedSettings["streamingBudget"].Enabled;
      advancedParametersPanel.Controls.Add(streamingBudget);

      AdvancedServerParameterNumeric streamsDelta = new()
      {
        ParameterName = "streamsDelta",
        ParameterFriendlyName = "Streams Delta",
        ParameterMin = 1,
        ParameterMax = 1000,
        ParameterIncrement = 1,
        ParameterValue = loadedSettings["streamsDelta"].Value,
        Description = "Streams delta is a tool to limit the amount of streams being opened for a client."
      };
      streamsDelta.CheckBox.Checked = loadedSettings["streamsDelta"].Enabled;
      advancedParametersPanel.Controls.Add(streamsDelta);

      AdvancedServerParameterNumeric rplTimeoutMs = new()
      {
        ParameterName = "rpl-timeout-ms",
        ParameterFriendlyName = "RPL Timeout",
        ParameterMin = 1,
        ParameterMax = int.MaxValue,
        ParameterIncrement = 1,
        ParameterValue = loadedSettings["rpl-timeout-ms"].Value,
        Description = "Sets the server's timeout value, in milliseconds."
      };
      rplTimeoutMs.CheckBox.Checked = loadedSettings["rpl-timeout-ms"].Enabled;
      advancedParametersPanel.Controls.Add(rplTimeoutMs);

      AdvancedServerParameterBool aiPartialSim = new()
      {
        ParameterName = "aiPartialSim",
        ParameterFriendlyName = "AI Partial Sim",
        Description = "Sets in how many batches all simulable AIs will divided and processed."
      };
      aiPartialSim.CheckBox.Checked = loadedSettings["aiPartialSim"].Enabled;
      advancedParametersPanel.Controls.Add(aiPartialSim);

      AdvancedServerParameterBool createDB = new()
      {
        ParameterName = "createDB",
        ParameterFriendlyName = "Force Recreate Database",
        Description = "Forces database file's regeneration. Useful after file directories changes, when some resources were moved elsewhere."
      };
      createDB.CheckBox.Checked = loadedSettings["createDB"].Enabled;
      advancedParametersPanel.Controls.Add(createDB);

      AdvancedServerParameterString debugger = new()
      {
        ParameterName = "debugger",
        ParameterFriendlyName = "Debugger Address",
        ParameterPlaceholder = "127.0.0.1",
        Description = "Sets the script debugger to a specific address."
      };
      bool debuggerEnabled = loadedSettings["debugger"].Enabled;
      debugger.CheckBox.Checked = debuggerEnabled;
      if (debuggerEnabled)
      {
        debugger.ParameterValue = loadedSettings["debugger"].Value;
      }
      advancedParametersPanel.Controls.Add(debugger);

      AdvancedServerParameterNumeric debuggerPort = new()
      {
        ParameterName = "debuggerPort",
        ParameterFriendlyName = "Debugger Port",
        ParameterIncrement = 1,
        ParameterMin = 1,
        ParameterMax = 65535,
        ParameterValue = loadedSettings["debuggerPort"].Value,
        Description = "Sets the script debugger to a specific port. "
      };
      debuggerPort.CheckBox.Checked = loadedSettings["debuggerPort"].Enabled;
      advancedParametersPanel.Controls.Add(debuggerPort);

      AdvancedServerParameterBool disableShadersBuild = new()
      {
        ParameterName = "disableShadersBuild",
        ParameterFriendlyName = "Disable Shaders Generation",
        Description = "Disables shaders generation."
      };
      disableShadersBuild.CheckBox.Checked = loadedSettings["disableShadersBuild"].Enabled;
      advancedParametersPanel.Controls.Add(disableShadersBuild);

      AdvancedServerParameterBool generateShaders = new()
      {
        ParameterName = "generateShaders",
        ParameterFriendlyName = "Force Generate Shaders",
        Description = "Forces shaders generation."
      };
      generateShaders.CheckBox.Checked = loadedSettings["generateShaders"].Enabled;
      advancedParametersPanel.Controls.Add(generateShaders);

      AdvancedServerParameterBool rplEncodeAsLongJobs = new()
      {
        ParameterName = "rplEncodeAsLongJobs",
        ParameterFriendlyName = "RPL Encode as Long Jobs",
        Description = "Makes replication use long encoding jobs instead of short ones."
      };
      rplEncodeAsLongJobs.CheckBox.Checked = loadedSettings["rplEncodeAsLongJobs"].Enabled;
      advancedParametersPanel.Controls.Add(rplEncodeAsLongJobs);

      AdvancedServerParameterNumeric jobsysShortWorkerCount = new()
      {
        ParameterName = "jobsysShortWorkerCount",
        ParameterFriendlyName = "Short Worker Count",
        Description = "Sets the number of threads working on short jobs (jobs that must finish in one update loop).",
        ParameterMin = 1,
        ParameterMax = Utilities.GetNumberAvailableThreads(),
        ParameterValue = loadedSettings["jobsysShortWorkerCount"].Value
      };
      jobsysShortWorkerCount.CheckBox.Checked = loadedSettings["jobsysShortWorkerCount"].Enabled;
      advancedParametersPanel.Controls.Add(jobsysShortWorkerCount);

      AdvancedServerParameterNumeric jobsysLongWorkerCount = new()
      {
        ParameterName = "jobsysLongWorkerCount",
        ParameterFriendlyName = "Long Worker Count",
        Description = "Sets the number of threads working on long jobs (jobs that can span multiple iterations of update loop).",
        ParameterMin = 1,
        ParameterMax = Utilities.GetNumberAvailableThreads(),
        ParameterValue = loadedSettings["jobsysLongWorkerCount"].Value
      };
      jobsysLongWorkerCount.CheckBox.Checked = loadedSettings["jobsysLongWorkerCount"].Enabled;
      advancedParametersPanel.Controls.Add(jobsysLongWorkerCount);

      AdvancedServerParameterNumeric freezeCheck = new()
      {
        ParameterName = "freezeCheck",
        ParameterFriendlyName = "Freeze Check",
        Description = "Overrides time in seconds to forcefully crash on application freeze or completely disable detection.",
        ParameterIncrement = 1,
        ParameterMin = 0,
        ParameterMax = 600,
        ParameterValue = loadedSettings["freezeCheck"].Value
      };
      freezeCheck.CheckBox.Checked = loadedSettings["freezeCheck"].Enabled;
      advancedParametersPanel.Controls.Add(freezeCheck);

      AdvancedServerParameterEnumerated freezeCheckMode = new()
      {
        ParameterName = "freezeCheckMode",
        ParameterFriendlyName = "Freeze Check Mode",
        Description = "Overrides behavior which should happen when freeze is detected.",
        ParameterAvailableValues = new List<string>() {"crash", "minidump", "kill"}
      };
      bool freezeCheckModeEnabled = loadedSettings["freezeCheckMode"].Enabled;
      freezeCheckMode.CheckBox.Checked = freezeCheckModeEnabled;
      if (freezeCheckModeEnabled)
      {
        freezeCheckMode.ParameterValue = loadedSettings["freezeCheckMode"].Value;
      }
      advancedParametersPanel.Controls.Add(freezeCheckMode);

      AdvancedServerParameterBool forceDisableNightGrain = new()
      {
        ParameterName = "forceDisableNightGrain",
        ParameterFriendlyName = "Force Disable Night Grain",
        Description = "Disables night grain in multiplayer.",
      };
      forceDisableNightGrain.CheckBox.Checked = loadedSettings["forceDisableNightGrain"].Enabled;
      advancedParametersPanel.Controls.Add(forceDisableNightGrain);

      foreach (AdvancedServerParameter param in advancedParametersPanel.Controls)
      {
        ConfigurationManager.GetInstance().GetAdvancedServerParametersDictionary()[param.ParameterName] = param;
      }
    }

    private void NoBackendCheckChanged(object? sender, EventArgs e)
    {
      ConfigurationManager.GetInstance().noBackend =
          ConfigurationManager.GetInstance().GetAdvancedServerParametersDictionary()["noBackend"].Checked();

      if (ConfigurationManager.GetInstance().noBackend)
      {
        bool enableNoBackend = Utilities.DisplayConfirmationMessage("Setting your server to use No Backend means it will not be visible in the server browser.\r\n" +
            "Mods not already downloaded will not work as they will not be fetched from the Workshop.\r\n" +
            "You must provide a valid Public Address in the Server Configuration section. It cannot be empty.\r\n" +
            "Clients will only be able to connect via the '-client' launch argument, and it is their responsibility to acquire required mods.\r\n\r\n" +
            "Continue?", true);

        if (!enableNoBackend)
        {
          ConfigurationManager.GetInstance().GetAdvancedServerParametersDictionary()["noBackend"].CheckBox.Checked = false;
        }
      }
    }

    private void AutoRestartOnCrashCheckChanged(object? sender, EventArgs e)
    {
      ConfigurationManager.GetInstance().autoRestartOnCrash =
          ConfigurationManager.GetInstance().GetAdvancedServerParametersDictionary()["autoRestartOnCrash"].Checked();
    }

    /// <summary>
    /// Event Handler for the 'UpdateSteamCmdLog' event
    /// This method is called twice if the call came from a non-UI thread
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">contains the line to append to the Steam CMD log</param>
    private void HandleUpdateSteamCmdLogEvent(object sender, SteamCmdLogEventArgs e)
    {
      if (steamCmdLog.InvokeRequired)
      {
        steamCmdLog.Invoke(new Action(() => HandleUpdateSteamCmdLogEvent(sender, e)));
      }
      else
      {
        steamCmdLog.AppendText(e.line);

        // Update the Server Status
        m_serverStatusParser.ParseServerStatus(e.line);
      }
    }

    private void HandleServerStatusEvent(object sender, ServerStatusEventArgs e)
    {
      if (serverAddressStatusLabel.InvokeRequired)
      {
        serverAddressStatusLabel.Invoke(() => HandleServerStatusEvent(sender, e));
        return;
      }

      const string serverOfflineString = "Server is offline.";
      if (!e.ServerOnline)
      {
        serverAddressStatusLabel.Text = serverOfflineString;
        rconAddressStatusLabel.Text = serverOfflineString;
        pingSiteStatusLabel.Text = serverOfflineString;
        joinCodeStatusLabel.Text = serverOfflineString;
        playerCountStatusLabel.Text = serverOfflineString;
        flagStatusPB.Image = null;

        // Stop refresh timer when server goes offline
        if (m_statusRefreshTimer != null)
        {
          m_statusRefreshTimer.Enabled = false;
        }
        return;
      }

      // Start refresh timer when server is online for responsive updates
      if (m_statusRefreshTimer != null && !m_statusRefreshTimer.Enabled)
      {
        m_statusRefreshTimer.Enabled = true;
      }
      if (e.LastFPS > 0 || e.LastMem > 0)
      {
        UpdatePerformanceGraphs(e.LastFPS, e.LastMem, e.LastUpdate);
      }
      serverAddressStatusLabel.Text = $"{e.LastIP}:{e.LastPort}";
      rconAddressStatusLabel.Text = $"{e.LastRconIP}:{e.LastRconPort}";

      if (!e.LastPingSite.Equals("Unknown"))
      {
        string pingSite = e.LastPingSite.Replace("_", " ").ToUpper();
        flagStatusPB.Image = FlagUtils.ChooseFlag(pingSite);
      }

      pingSiteStatusLabel.Text = string.Concat(e.LastPingSite.Substring(0, 1).ToUpper(), e.LastPingSite.AsSpan(1));
      joinCodeStatusLabel.Text = e.LastJoinCode;

      if (e.LastPlayerCount == 1)
      {
        playerCountStatusLabel.Text = $"{e.LastPlayerCount} connected player";
      } else
      {
        playerCountStatusLabel.Text = $"{e.LastPlayerCount} connected players";
      }
    }

    private void UpdatePerformanceGraphs(double fps, long memoryKb, DateTime time)
    {
      double memoryGb = Math.Round(memoryKb / 1024.0 / 1024.0, 2);

      if (this.InvokeRequired)
      {
        this.BeginInvoke(new Action(() => UpdatePerformanceGraphs(fps, memoryKb, time)));
        return;
      }

      // --- Update FPS Graph ---
      chartFps.Series["FPS"].Points.AddXY(time, fps);

      if (chartFps.Series["FPS"].Points.Count > MAX_GRAPH_POINTS)
      {
        chartFps.Series["FPS"].Points.RemoveAt(0);
      }

      // Auto-scale Y Axis
      double minFps = chartFps.Series["FPS"].Points.FindMinByValue().YValues[0];
      double maxFps = chartFps.Series["FPS"].Points.FindMaxByValue().YValues[0];
      chartFps.ChartAreas[0].AxisY.Minimum = Math.Max(0, minFps - 5);
      chartFps.ChartAreas[0].AxisY.Maximum = maxFps + 5;


      // --- Update Memory Graph ---
      chartMem.Series["Memory (GB)"].Points.AddXY(time, memoryGb);

      if (chartMem.Series["Memory (GB)"].Points.Count > MAX_GRAPH_POINTS)
      {
        chartMem.Series["Memory (GB)"].Points.RemoveAt(0);
      }
    }

    /// <summary>
    /// Event Handler for the 'UpdateGuiControls' event
    /// This method is called twice if the call came from a non-UI thread
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">contains values to update various GUI controls with</param>
    private void HandleUpdateGuiControlsEvent(object sender, GuiModelEventArgs e)
    {
      if (startServerBtn.InvokeRequired)
      {
        // We can invoke with any GUI element here
        startServerBtn.Invoke(new Action(() => HandleUpdateGuiControlsEvent(sender, e)));
      }
      else
      {
        if (e.startServerBtnEnabled)
          UpdateStartButtonState();
        else
          startServerBtn.Enabled = false;
        serverRunningLabel.Text = e.serverRunningLabelText;
        startServerBtn.IconChar = e.buttonIconChar;
        EnableServerFields(e.enableServerFields);
      }
    }

    /// <summary>
    /// Event Handler for the 'UpdateScenarioIdFromLoadedConfigEvent' event
    /// This method is called twice if the call came from a non-UI thread
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">contains values to update various GUI controls with</param>
    private void HandleUpdateScenarioIdFromLoadedConfigEvent(object sender, ScenarioIdEventArgs e)
    {
      if (loadedScenarioLabel.InvokeRequired)
      {
        loadedScenarioLabel.Invoke(new Action(() =>
            HandleUpdateScenarioIdFromLoadedConfigEvent(sender, e)));
      }
      else
      {
        if (string.IsNullOrWhiteSpace(e.scenarioId))
        {
          loadedScenarioLabel.Text = "No scenario ID chosen.";
        }
        else
        {
          loadedScenarioLabel.Text = e.scenarioId;
        }
      }
    }

    /// <summary>
    /// Event Handler for the 'ValidationStateChanged' event
    /// Called whenever mod validation state changes
    /// </summary>
    private void HandleValidationStateChanged(object sender, ValidationResult result)
    {
      Log.Information("[DEBUG] HandleValidationStateChanged fired! Result: IsValid={isValid}, HasFatalErrors={hasFatal}, Errors={errors}, Warnings={warnings}",
        result?.IsValid,
        result?.HasFatalErrors(),
        result?.Errors?.Count,
        result?.Warnings?.Count);

      if (startServerBtn.InvokeRequired)
      {
        Log.Information("[DEBUG] HandleValidationStateChanged: InvokeRequired=true, marshalling to UI thread");
        startServerBtn.Invoke(new Action(() => HandleValidationStateChanged(sender, result)));
      }
      else
      {
        Log.Information("[DEBUG] HandleValidationStateChanged: Calling UpdateStartButtonState()");
        UpdateStartButtonState();
      }
    }

    /// <summary>
    /// Create a string with all requried launch arguments
    /// </summary>
    /// <returns>String containing launch arguments for the Reforger Server</returns>
    private void CreateLaunchArguments()
    {
      // Begin by setting the mandatory parameters
      LaunchArguments args = new()
      {
        // Config will be placed in <server-files-dir>/server.json, wrap in quotes to capture potential spaces in paths
        config = new("config", $"\"{FileIOManager.GetInstance().GetInstallDirectory()}\\server.json\""),
        // Saves etc. will be placed in <server-files-dir>/saves/, wrap in quotes to capture potential spaces in paths
        profile = new("profile", $"\"{FileIOManager.GetInstance().GetInstallDirectory()}\\saves\""),
        // Addons will be placed in <server-files-dir>/addons/, wrap in quotes to capture potentional spaces in paths
        addonsDir = new("addonsDir", $"\"{FileIOManager.GetInstance().GetInstallDirectory()}\\addons\""),
        // Log performance stats every 5 seconds (represented in ms)
        logStats = new("logStats", $"{Convert.ToString(5000)}"),
        logLevel = new("logLevel", $"{logLevelComboBox.Text}")
      };

      var advParams = ConfigurationManager.GetInstance().GetAdvancedServerParametersDictionary();

      if (advParams["loadSessionSave"].Checked())
      {
        // As no parameter is also valid, check if there is a value
        string loadSessionSaveVal = Convert.ToString(advParams["loadSessionSave"].ParameterValue);
        if (String.IsNullOrWhiteSpace(loadSessionSaveVal))
        {
          args.loadSessionSave = new("loadSessionSave");
        }
        else
        {
          args.loadSessionSave = new("loadSessionSave", loadSessionSaveVal);
        }
      }

      if (advParams["maxFPS"].Checked())
      {
        args.maxFPS = new("maxFPS", Convert.ToString(advParams["maxFPS"].ParameterValue));
      }

      if (advParams["bindPort"].Checked())
      {
        args.bindPort = new("bindPort", Convert.ToString(advParams["bindPort"].ParameterValue));
      }

      if (advParams["nds"].Checked())
      {
        args.nds = new("nds", Convert.ToString(advParams["nds"].ParameterValue));
      }

      if (advParams["nwkResolution"].Checked())
      {
        args.nwkResolution = new("nwkResolution", Convert.ToString(advParams["nwkResolution"].ParameterValue));
      }

      if (advParams["staggeringBudget"].Checked())
      {
        args.staggeringBudget = new("staggeringBudget", Convert.ToString(advParams["staggeringBudget"].ParameterValue));
      }

      if (advParams["streamingBudget"].Checked())
      {
        args.streamingBudget = new("streamingBudget", Convert.ToString(advParams["streamingBudget"].ParameterValue));
      }

      if (advParams["streamsDelta"].Checked())
      {
        args.streamsDelta = new("streamsDelta", Convert.ToString(advParams["streamsDelta"].ParameterValue));
      }

      if (advParams["autoreload"].Checked())
      {
        args.autoReload = new("autoreload", Convert.ToString(advParams["autoreload"].ParameterValue));
      }

      if (advParams["rpl-timeout-ms"].Checked())
      {
        args.rplTimeoutMs = new("rpl-timeout-ms", Convert.ToString(advParams["rpl-timeout-ms"].ParameterValue));
      }

      if (advParams["freezeCheck"].Checked())
      {
        args.freezeCheck = new("freezeCheck", Convert.ToString(advParams["freezeCheck"].ParameterValue));
      }

      if (advParams["freezeCheckMode"].Checked())
      {
        args.freezeCheckMode = new("freezeCheckMode", ((AdvancedServerParameterEnumerated) advParams["freezeCheckMode"]).SelectedItem);
      }

      if (advParams["addonsRepair"].Checked())
      {
        args.addonsRepair = new("addonsRepair");
      }

      if (advParams["autoShutdown"].Checked())
      {
        args.autoShutdown = new("autoShutdown");
      }

      if (advParams["logVoting"].Checked())
      {
        args.logVoting = new("logVoting");
      }

      if (advParams["aiPartialSim"].Checked())
      {
        args.aiPartialSim = new("aiPartialSim");
      }

      if (advParams["createDB"].Checked())
      {
        args.createDB = new("createDB");
      }

      if (advParams["debugger"].Checked())
      {
        args.debugger = new("debugger", Convert.ToString(advParams["debugger"].ParameterValue));
      }

      if (advParams["debuggerPort"].Checked())
      {
        args.debuggerPort = new("debuggerPort", Convert.ToString(advParams["debuggerPort"].ParameterValue));
      }

      if (advParams["disableShadersBuild"].Checked())
      {
        args.disableShadersBuild = new("disableShadersBuild");
      }

      if (advParams["generateShaders"].Checked())
      {
        args.generateShaders = new("generateShaders");
      }

      if (advParams["rplEncodeAsLongJobs"].Checked())
      {
        args.rplEncodeAsLongJobs = new("rplEncodeAsLongJobs");
      }

      if (advParams["jobsysShortWorkerCount"].Checked())
      {
        args.jobSysShortWorkerCount = new("jobsysShortWorkerCount", Convert.ToString(advParams["jobsysShortWorkerCount"].ParameterValue));
      }

      if (advParams["jobsysLongWorkerCount"].Checked())
      {
        args.jobSysLongWorkerCount = new("jobsysLongWorkerCount", Convert.ToString(advParams["jobsysLongWorkerCount"].ParameterValue));
      }

      if (advParams["forceDisableNightGrain"].Checked())
      {
        args.forceDisableNightGrain = new("forceDisableNightGrain");
      }

      ProcessManager.GetInstance().SetLaunchArgumentsModel(args);
    }

    /// <summary>
    /// Filter a Source, searching for a given string
    /// </summary>
    /// <param name="filter">to use when searching for matching items</param>
    /// <param name="modList">source list to search</param>
    /// <returns>filtered list</returns>
    private static List<Mod> FilterModList(string filter, BindingList<Mod> modList)
    {
      return modList
          .Where(mod => mod.name.ToLower().Contains(filter))
          .ToList();
    }

    /// <summary>
    /// Convenience method for clearing filters and removing text 
    /// from the filter text fields
    /// </summary>
    private void ResetModFilters()
    {
      availableMods.DataSource = m_availableModsBindingSource;
      enabledMods.DataSource = m_enabledModsBindingSource;
      modsSearchTB.Text = string.Empty;
      availableMods.SelectedItem = enabledMods.SelectedItem = null;
    }

    /// <summary>
    /// Event Handler for when text is entered or removed in the 
    /// Search Available Mods text box
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">arguments</param>
    private void OnSearchModsTextChanged(object sender, EventArgs e)
    {
      string filter = modsSearchTB.Text.ToLower();
      if (string.IsNullOrEmpty(filter))
      {
        m_availableModsBindingSource.DataSource = ConfigurationManager.GetInstance().GetAvailableMods();
        m_enabledModsBindingSource.DataSource = ConfigurationManager.GetInstance().GetEnabledMods();
      }
      else
      {
        m_availableModsBindingSource.DataSource = FilterModList(filter, ConfigurationManager.GetInstance().GetAvailableMods());
        m_enabledModsBindingSource.DataSource = FilterModList(filter, ConfigurationManager.GetInstance().GetEnabledMods());
      }
    }

    private void UseExperimentalServerCheckboxChanged(object sender, EventArgs e)
    {
      ConfigurationManager.GetInstance().useExperimentalServer = useExperimentalCheckBox.Checked;
    }

    private void OnUseUPnPCheckChanged(object sender, EventArgs e)
    {
      NetworkManager.GetInstance().useUPnP = useUpnp.Checked;
    }

    private void ExportModsListBtnPressed(object sender, EventArgs e)
    {
      FileIOManager.SaveModsListToFile();
    }

    private void ImportModsListBtnPressed(object sender, EventArgs e)
    {
      FileIOManager.LoadModsListFromFile();
    }

    /// <summary>
    /// Utility method to update state of each advanced server parameter with the SavedStateManager
    /// </summary>
    private void UpdateStateForAdvancedSettings()
    {
      foreach (AdvancedServerParameter param in advancedParametersPanel.Controls)
      {
        SavedState ss = SavedStateManager.GetInstance().GetSavedState();
        if (ss.advancedSettings.ContainsKey(param.ParameterName))
        {
          ss.advancedSettings[param.ParameterName].Enabled = param.CheckBox.Checked;
          if (param is AdvancedServerParameterEnumerated)
          {
            AdvancedServerParameterEnumerated enumParam = (AdvancedServerParameterEnumerated) param;
            ss.advancedSettings[param.ParameterName].Value = enumParam.SelectedItem;
          }
          else
          {
            if (param is AdvancedServerParameterNumeric)
            {
              ss.advancedSettings[param.ParameterName].Value = Convert.ToInt32(param.ParameterValue);
            }
            else if (param is AdvancedServerParameterString)
            {
              ss.advancedSettings[param.ParameterName].Value = param.ParameterValue;
            }
          }
        }
      }
    }

    // -------------------------------------------------------------------------
    // Mod dependency validation
    // -------------------------------------------------------------------------

    private void CreateModValidationControls()
    {
      // Position relative to the existing button panel which is already correctly
      // anchored — avoids relying on ClientSize.Height before layout is complete.
      int stripHeight = 23;
      int stripY = tableLayoutPanel1.Top - stripHeight - 4;
      int x = 6;
      int rightEdge = groupBox2.ClientSize.Width - 6;

      m_checkModsBtn = new Button
      {
        Text = "Check Mods",
        Location = new Point(x, stripY),
        Size = new Size(100, stripHeight),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Left
      };
      UIStyleHelper.StylePrimaryButton(m_checkModsBtn);
      m_checkModsBtn.Click += StartModValidationClicked;

      m_loadConfigBtn = new Button
      {
        Text = "Load Config",
        Location = new Point(x + 106, stripY),
        Size = new Size(100, stripHeight),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Left
      };
      UIStyleHelper.StyleSecondaryButton(m_loadConfigBtn);
      m_loadConfigBtn.Click += LoadConfigBtnClicked;

      m_saveConfigBtn = new Button
      {
        Text = "Save Config",
        Location = new Point(x + 212, stripY),
        Size = new Size(100, stripHeight),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Left
      };
      UIStyleHelper.StyleSecondaryButton(m_saveConfigBtn);
      m_saveConfigBtn.Click += SaveConfigBtnClicked;

      m_checkModsProgressBar = new ProgressBar
      {
        Location = new Point(x + 318, stripY),
        Size = new Size(rightEdge - x - 318, stripHeight),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        Minimum = 0,
        Maximum = 100,
        Value = 0,
        Visible = false
      };

      int chkW = 174;
      m_checkModsStatusLabel = new Label
      {
        Text = "Mod check required before starting.",
        Location = new Point(x + 318, stripY + 4),
        Size = new Size(rightEdge - x - 318 - chkW - 6, stripHeight),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft
      };

      m_appendSizeChk = new CheckBox
      {
        Text = "Append to server name",
        Location = new Point(rightEdge - chkW, stripY + 3),
        Size = new Size(chkW, stripHeight - 2),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        AutoSize = false,
        Visible = false
      };
      m_appendSizeChk.CheckedChanged += AppendSizeChkChanged;

      groupBox2.Controls.Add(m_checkModsBtn);
      groupBox2.Controls.Add(m_loadConfigBtn);
      groupBox2.Controls.Add(m_saveConfigBtn);
      groupBox2.Controls.Add(m_checkModsProgressBar);
      groupBox2.Controls.Add(m_checkModsStatusLabel);
      groupBox2.Controls.Add(m_appendSizeChk);

      UpdateStartButtonState();
    }

    private void StartModValidationClicked(object? sender, EventArgs e)
    {
      if (m_checkModsBtn == null || m_checkModsProgressBar == null || m_checkModsStatusLabel == null) return;

      m_checkModsBtn.Enabled = false;
      m_checkModsProgressBar.Value = 0;
      m_checkModsProgressBar.Visible = true;
      m_checkModsStatusLabel.Visible = false;

      IList<Mod> snapshot = ConfigurationManager.GetInstance().GetEnabledMods().ToArray();

      Thread t = new(() =>
      {
        (List<Mod> sorted, List<Mod> addedMods, List<string> warnings, long totalSize) =
            ModDependencyManager.ResolveDependencies(snapshot, (done, total) =>
            {
              if (m_checkModsProgressBar.IsHandleCreated && total > 0)
                m_checkModsProgressBar.Invoke(() =>
                    m_checkModsProgressBar.Value = Math.Min(100, (int)(done * 100.0 / total)));
            });
        m_checkModsProgressBar.Invoke(() => HandleValidationComplete(sorted, addedMods, warnings, totalSize));
      });
      t.IsBackground = true;
      t.Start();
    }

    private void HandleValidationComplete(List<Mod> sorted, List<Mod> added, List<string> warnings, long totalSizeBytes)
    {
      if (m_checkModsBtn == null || m_checkModsProgressBar == null || m_checkModsStatusLabel == null) return;

      m_checkModsProgressBar.Visible = false;
      m_checkModsStatusLabel.Visible = true;
      m_checkModsBtn.Enabled = true;

      ConfigurationManager.GetInstance().ApplySortedModList(sorted);
      ResetModFilters();

      m_modsValidated = true;

      if (warnings.Count > 0)
        m_checkModsStatusLabel.Text = $"Check complete with {warnings.Count} warning(s). See log.";
      else if (added.Count > 0)
        m_checkModsStatusLabel.Text = $"Check passed — {added.Count} dependency mod(s) added, list reordered.";
      else
        m_checkModsStatusLabel.Text = "All mods OK.";

      m_lastTotalModSizeBytes = totalSizeBytes;
      if (m_appendSizeChk != null)
      {
        if (totalSizeBytes > 0)
        {
          m_appendSizeChk.Text = $"~{FormatModSize(totalSizeBytes)} — append to name";
          m_appendSizeChk.Visible = true;
          if (m_appendSizeChk.Checked)
            ApplySizeToServerName(totalSizeBytes);
        }
        else
        {
          m_appendSizeChk.Visible = false;
        }
      }

      UpdateStartButtonState();
    }

    private async void LoadConfigBtnClicked(object? sender, EventArgs e)
    {
      try
      {
        Log.Information("Load Config button clicked");

        // Get list of available configs
        var configManager = ConfigurationManager.GetInstance();
        var configs = await configManager.GetAvailableConfigs();

        if (configs.Count == 0)
        {
          MessageBox.Show(
            "No saved configurations found. Save a configuration first.",
            "No Configurations Available",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
          );
          return;
        }

        // Create a simple dialog to select config
        using (var form = new Form())
        {
          form.Text = "Load Mod Configuration";
          form.Size = new Size(400, 300);
          form.StartPosition = FormStartPosition.CenterParent;
          form.FormBorderStyle = FormBorderStyle.FixedDialog;
          form.MaximizeBox = false;
          form.MinimizeBox = false;

          var listBox = new ListBox();
          listBox.Dock = DockStyle.Fill;
          foreach (var config in configs)
          {
            listBox.Items.Add(config.Name);
          }

          var btnPanel = new Panel();
          btnPanel.Dock = DockStyle.Bottom;
          btnPanel.Height = 40;
          btnPanel.Padding = new Padding(5);

          var btnLoad = new Button { Text = "Load", Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
          var btnCancel = new Button { Text = "Cancel", Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
          btnLoad.Width = 80;
          btnCancel.Width = 80;
          btnLoad.Location = new Point(btnPanel.Width - 165, 5);
          btnCancel.Location = new Point(btnPanel.Width - 85, 5);

          btnLoad.Click += async (s, e) =>
          {
            if (listBox.SelectedItem != null)
            {
              string selectedConfig = listBox.SelectedItem.ToString() ?? "";
              bool success = await configManager.LoadConfigFromSitrep(selectedConfig);
              if (success)
              {
                MessageBox.Show(
                  $"Configuration '{selectedConfig}' loaded successfully.",
                  "Configuration Loaded",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
                m_modsValidated = false;
                if (m_checkModsStatusLabel != null)
                  m_checkModsStatusLabel.Text = "Configuration loaded. Click 'Check Mods' to validate.";
              }
              else
              {
                MessageBox.Show(
                  $"Failed to load configuration '{selectedConfig}'.",
                  "Load Failed",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Error
                );
              }
              form.Close();
            }
          };

          btnCancel.Click += (s, e) => form.Close();
          btnPanel.Controls.Add(btnLoad);
          btnPanel.Controls.Add(btnCancel);

          form.Controls.Add(listBox);
          form.Controls.Add(btnPanel);

          form.ShowDialog(this);
        }
      }
      catch (Exception ex)
      {
        Log.Error("Error in LoadConfigBtnClicked: {msg}", ex.Message);
        MessageBox.Show(
          $"Error loading configuration: {ex.Message}",
          "Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error
        );
      }
    }

    private async void SaveConfigBtnClicked(object? sender, EventArgs e)
    {
      try
      {
        Log.Information("Save Config button clicked");

        // Prompt for config name
        using (var form = new Form())
        {
          form.Text = "Save Mod Configuration";
          form.Size = new Size(400, 150);
          form.StartPosition = FormStartPosition.CenterParent;
          form.FormBorderStyle = FormBorderStyle.FixedDialog;
          form.MaximizeBox = false;
          form.MinimizeBox = false;

          var lblName = new Label { Text = "Configuration Name:", Left = 10, Top = 20, Width = 120 };
          var tbName = new TextBox { Left = 140, Top = 20, Width = 240, Height = 25 };
          var lblDesc = new Label { Text = "Description (optional):", Left = 10, Top = 55, Width = 120 };
          var tbDesc = new TextBox { Left = 140, Top = 55, Width = 240, Height = 25 };

          var btnSave = new Button { Text = "Save", Left = 250, Top = 100, Width = 80 };
          var btnCancel = new Button { Text = "Cancel", Left = 340, Top = 100, Width = 80 };

          btnSave.Click += async (s, e) =>
          {
            if (string.IsNullOrWhiteSpace(tbName.Text))
            {
              MessageBox.Show(
                "Please enter a configuration name.",
                "Name Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
              );
              return;
            }

            var configManager = ConfigurationManager.GetInstance();
            bool success = await configManager.SaveConfigToSitrep(tbName.Text, tbDesc.Text);
            if (success)
            {
              MessageBox.Show(
                $"Configuration '{tbName.Text}' saved successfully.",
                "Configuration Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
              );
            }
            else
            {
              MessageBox.Show(
                $"Failed to save configuration '{tbName.Text}'.",
                "Save Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
              );
            }
            form.Close();
          };

          btnCancel.Click += (s, e) => form.Close();

          form.Controls.Add(lblName);
          form.Controls.Add(tbName);
          form.Controls.Add(lblDesc);
          form.Controls.Add(tbDesc);
          form.Controls.Add(btnSave);
          form.Controls.Add(btnCancel);

          form.ShowDialog(this);
        }
      }
      catch (Exception ex)
      {
        Log.Error("Error in SaveConfigBtnClicked: {msg}", ex.Message);
        MessageBox.Show(
          $"Error saving configuration: {ex.Message}",
          "Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error
        );
      }
    }

    private void OnEnabledModsChanged(object? sender, ListChangedEventArgs e)
    {
      Log.Information("[DEBUG] OnEnabledModsChanged fired! Event: {eventType}, Index: {index}", e.ListChangedType, e.NewIndex);
      m_modsValidated = false;
      if (m_checkModsStatusLabel != null)
        m_checkModsStatusLabel.Text = "Mod list changed — re-check required.";
      if (m_appendSizeChk != null)
        m_appendSizeChk.Visible = false;

      // Trigger validation when mods change
      Log.Information("[DEBUG] OnEnabledModsChanged: Calling TriggerValidation()");
      ConfigurationManager.GetInstance().TriggerValidation();
      Log.Information("[DEBUG] OnEnabledModsChanged: TriggerValidation() completed");
    }

    private void AppendSizeChkChanged(object? sender, EventArgs e)
    {
      ApplySizeToServerName(m_appendSizeChk?.Checked == true ? m_lastTotalModSizeBytes : 0);
    }

    private void ApplySizeToServerName(long sizeBytes)
    {
      if (m_serverNameCtrl == null) return;
      string current = ((string)m_serverNameCtrl.ParameterValue).TrimEnd();
      current = Regex.Replace(current, @"\s*\|\s*~[\d.]+\s*[GM]B\s*mods",
          string.Empty, RegexOptions.IgnoreCase).TrimEnd();
      if (sizeBytes > 0)
        current += $" | ~{FormatModSize(sizeBytes)} mods";
      m_serverNameCtrl.ParameterValue = current;
    }

    private static string FormatModSize(long bytes)
    {
      if (bytes >= 1024L * 1024 * 1024)
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
      return $"{bytes / (1024.0 * 1024):F0} MB";
    }

    private void UpdateStartButtonState()
    {
      Log.Information("[DEBUG] UpdateStartButtonState() called");

      if (startServerBtn.InvokeRequired)
      {
        Log.Information("[DEBUG] UpdateStartButtonState: InvokeRequired=true, marshalling to UI thread");
        startServerBtn.Invoke(UpdateStartButtonState);
        return;
      }

      bool steamCmdInstalled = FileIOManager.GetInstance().IsSteamCMDInstalled();
      var validationResult = ConfigurationManager.GetInstance().GetLastValidationResult();
      bool hasFatalErrors = validationResult?.HasFatalErrors() ?? false;
      bool hasWarnings = (validationResult?.Warnings?.Count ?? 0) > 0;

      Log.Information("[DEBUG] UpdateStartButtonState: SteamCmdInstalled={installed}, HasFatalErrors={hasFatal}, HasWarnings={hasWarn}, ValidationResult={result}",
        steamCmdInstalled,
        hasFatalErrors,
        hasWarnings,
        validationResult == null ? "NULL" : $"Valid={validationResult.IsValid}, Errors={validationResult.Errors.Count}, Warnings={validationResult.Warnings.Count}");

      if (!steamCmdInstalled)
      {
        Log.Information("[DEBUG] UpdateStartButtonState: SteamCmd not installed, disabling button");
        startServerBtn.Enabled = false;
        UIStyleHelper.StyleDisabledButton(startServerBtn);
        startServerBtn.Text = "Start Server";
        return;
      }

      if (hasFatalErrors)
      {
        var fatalCount = validationResult?.Errors?.Count(e => e.Severity == ErrorSeverity.FATAL) ?? 0;
        Log.Information("[DEBUG] UpdateStartButtonState: Setting button to DISABLED/RED due to {fatalCount} fatal errors", fatalCount);
        startServerBtn.Enabled = false;
        UIStyleHelper.StyleErrorButton(startServerBtn);
        startServerBtn.Text = $"Start Server (Invalid: {fatalCount} error{(fatalCount != 1 ? "s" : "")})";
        string tooltip = $"Fix {fatalCount} validation error{(fatalCount != 1 ? "s" : "")} before launch";
        if (startServerBtn.Tag is ToolTip tt)
        {
          tt.SetToolTip(startServerBtn, tooltip);
        }
        Log.Information("[DEBUG] UpdateStartButtonState: Button.Enabled={enabled}, Button.BackColor={color}, Button.Text={text}",
          startServerBtn.Enabled,
          startServerBtn.BackColor,
          startServerBtn.Text);
      }
      else if (hasWarnings)
      {
        var warningCount = validationResult?.Warnings?.Count ?? 0;
        Log.Information("[DEBUG] UpdateStartButtonState: Setting button to ENABLED/ORANGE due to {warningCount} warnings", warningCount);
        startServerBtn.Enabled = true;
        UIStyleHelper.StyleWarningButton(startServerBtn);
        startServerBtn.Text = $"Start Server ({warningCount} warning{(warningCount != 1 ? "s" : "")})";
        string tooltip = $"Server will launch with {warningCount} warning{(warningCount != 1 ? "s" : "")}";
        if (startServerBtn.Tag is ToolTip tt)
        {
          tt.SetToolTip(startServerBtn, tooltip);
        }
        Log.Information("[DEBUG] UpdateStartButtonState: Button.Enabled={enabled}, Button.BackColor={color}", startServerBtn.Enabled, startServerBtn.BackColor);
      }
      else
      {
        Log.Information("[DEBUG] UpdateStartButtonState: Setting button to ENABLED/NORMAL (no errors/warnings)");
        startServerBtn.Enabled = true;
        UIStyleHelper.StyleSuccessButton(startServerBtn);
        startServerBtn.Text = "Start Server";
        Log.Information("[DEBUG] UpdateStartButtonState: Button.Enabled={enabled}, Button.BackColor={color}", startServerBtn.Enabled, startServerBtn.BackColor);
      }
    }

    /// <summary>
    /// Event Handler for when the application is closing
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnFormClosing(object sender, FormClosingEventArgs e)
    {
      if (ProcessManager.GetInstance().IsServerStarted())
      {
        ProcessManager.GetInstance().KillServer();
      }

      ProcessManager.GetInstance().UpdateGuiControlsEvent -= HandleUpdateGuiControlsEvent;
      ProcessManager.GetInstance().UpdateSteamCmdLogEvent -= HandleUpdateSteamCmdLogEvent;
      ProcessManager.GetInstance().UpdateServerStatusEvent -= HandleServerStatusEvent;
      ConfigurationManager.GetInstance().UpdateScenarioIdFromLoadedConfigEvent -= HandleUpdateScenarioIdFromLoadedConfigEvent;
      ProcessManager.GetInstance().ScenarioRotationSwitchEvent -= HandleScenarioRotationSwitchEvent;
      ConfigurationManager.GetInstance().ValidationStateChanged -= HandleValidationStateChanged;
      m_serverStatusParser.UpdateServerStatus -= HandleServerStatusEvent;
      ConfigurationManager.GetInstance().GetEnabledMods().ListChanged -= OnEnabledModsChanged;

      // Cleanup status refresh timer
      if (m_statusRefreshTimer != null)
      {
        m_statusRefreshTimer.Stop();
        m_statusRefreshTimer.Dispose();
      }

      m_availableModsBindingSource.Dispose();
      m_enabledModsBindingSource.Dispose();
      ProcessManager.GetInstance().Dispose();

      UpdateStateForAdvancedSettings();

      // Update state of the checkboxes
      SavedStateManager.GetInstance().GetSavedState().advancedSettings["useUpnp"].Enabled = useUpnp.Checked;
      SavedStateManager.GetInstance().GetSavedState().advancedSettings["useExperimental"].Enabled = useExperimentalCheckBox.Checked;
      SavedStateManager.GetInstance().GetSavedState().advancedSettings["keepServerUpdated"].Enabled = keepServerUpdated.Checked;

      // Persist scenario rotation state
      if (m_rotationEnabledCheckBox != null && m_rotationListView != null)
      {
        SavedStateManager.GetInstance().GetSavedState().scenarioRotationEnabled = m_rotationEnabledCheckBox.Checked;
        SavedStateManager.GetInstance().GetSavedState().scenarioRotation = GetRotationEntriesFromListView();
      }

      FileIOManager.GetInstance().WriteStateFile();
      FileIOManager.GetInstance().WriteModsDatabase();
    }

    private void LoadSaveGameBtnPressed(object sender, EventArgs e)
    {
      SpawnSaveSelect();
    }

    private void KeepServerUpdatedCheckedChanged(object sender, EventArgs e)
    {
      ProcessManager.GetInstance().KeepServerUpdated = keepServerUpdated.Checked;
    }

    private void OnServerAddressToClipboard(object sender, EventArgs e)
    {
      Clipboard.SetText(serverAddressStatusLabel.Text);
    }

    private void OnRconAddressToClipboard(object sender, EventArgs e)
    {
      Clipboard.SetText(rconAddressStatusLabel.Text);
    }

    private void OnJoinCodeToClipboard(object sender, EventArgs e)
    {
      Clipboard.SetText(joinCodeStatusLabel.Text);
    }

    private void HandleScenarioRotationSwitchEvent(object sender, ScenarioRotationSwitchEventArgs e)
    {
      if (InvokeRequired)
      {
        Invoke(new Action(() => HandleScenarioRotationSwitchEvent(sender, e)));
        return;
      }
      // Update the active scenario in ConfigurationManager so the restarted server picks it up
      ConfigurationManager.GetInstance().GetServerConfiguration().root.game.scenarioId = e.Entry.ScenarioPath;
      loadedScenarioLabel.Text = e.Entry.ScenarioPath;
    }

    private List<ScenarioRotationEntry> GetRotationEntriesFromListView()
    {
      var entries = new List<ScenarioRotationEntry>();
      if (m_rotationListView == null) return entries;
      foreach (ListViewItem item in m_rotationListView.Items)
      {
        entries.Add(new ScenarioRotationEntry(
          item.SubItems[0].Text,
          item.SubItems[1].Text,
          int.TryParse(item.SubItems[2].Text, out int h) ? h : 4));
      }
      return entries;
    }

    private void CreateRotationTab()
    {
      TabPage rotationTab = new TabPage("Rotation");

      // Enable checkbox
      m_rotationEnabledCheckBox = new CheckBox
      {
        Text = "Enable Scenario Rotation",
        Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
        Location = new Point(10, 10),
        AutoSize = true,
        Checked = SavedStateManager.GetInstance().GetSavedState().scenarioRotationEnabled
      };
      rotationTab.Controls.Add(m_rotationEnabledCheckBox);

      // Description label
      Label descLabel = new Label
      {
        Text = "Add scenarios below. When the server is running with rotation enabled, each scenario runs for the\r\n" +
               "specified duration. RCON countdown warnings are sent at 10, 5, and 1 minute before each switch.\r\n" +
               "Requires RCON to be enabled for player warnings (rotation still works without RCON).",
        Location = new Point(10, 38),
        Size = new Size(900, 52),
        ForeColor = SystemColors.GrayText
      };
      rotationTab.Controls.Add(descLabel);

      // ListView
      m_rotationListView = new ListView
      {
        Location = new Point(10, 100),
        Size = new Size(880, 300),
        View = View.Details,
        FullRowSelect = true,
        GridLines = true,
        MultiSelect = false
      };
      m_rotationListView.Columns.Add("Scenario Name", 380);
      m_rotationListView.Columns.Add("Scenario Path", 360);
      m_rotationListView.Columns.Add("Duration (hours)", 120);
      rotationTab.Controls.Add(m_rotationListView);

      // Load persisted rotation entries
      foreach (var entry in SavedStateManager.GetInstance().GetSavedState().scenarioRotation)
      {
        ListViewItem item = new ListViewItem(entry.ScenarioName);
        item.SubItems.Add(entry.ScenarioPath);
        item.SubItems.Add(entry.DurationHours.ToString());
        m_rotationListView.Items.Add(item);
      }

      // Duration spinner label
      Label durationLabel = new Label
      {
        Text = "Duration (hours) for new entry:",
        Location = new Point(10, 410),
        AutoSize = true
      };
      rotationTab.Controls.Add(durationLabel);

      m_rotationDurationSpinner = new NumericUpDown
      {
        Location = new Point(210, 407),
        Size = new Size(60, 23),
        Minimum = 1,
        Maximum = 48,
        Value = 4
      };
      rotationTab.Controls.Add(m_rotationDurationSpinner);

      // Buttons
      m_addRotationScenarioBtn = new Button
      {
        Text = "Add Scenario",
        Location = new Point(10, 440),
        Size = new Size(120, 30)
      };
      m_addRotationScenarioBtn.Click += (s, e) =>
      {
        ScenarioSelector selector = new ScenarioSelector(this, scenario =>
        {
          if (scenario == null) return;
          int duration = (int)m_rotationDurationSpinner.Value;
          ListViewItem item = new ListViewItem(scenario.Name);
          item.SubItems.Add(scenario.Path);
          item.SubItems.Add(duration.ToString());
          m_rotationListView.Items.Add(item);
        });
        selector.ShowDialog();
      };
      rotationTab.Controls.Add(m_addRotationScenarioBtn);

      m_removeRotationScenarioBtn = new Button
      {
        Text = "Remove",
        Location = new Point(140, 440),
        Size = new Size(80, 30)
      };
      m_removeRotationScenarioBtn.Click += (s, e) =>
      {
        if (m_rotationListView.SelectedItems.Count > 0)
          m_rotationListView.Items.Remove(m_rotationListView.SelectedItems[0]);
      };
      rotationTab.Controls.Add(m_removeRotationScenarioBtn);

      m_moveRotationUpBtn = new Button
      {
        Text = "▲",
        Location = new Point(230, 440),
        Size = new Size(40, 30)
      };
      m_moveRotationUpBtn.Click += (s, e) =>
      {
        if (m_rotationListView.SelectedItems.Count == 0) return;
        ListViewItem item = m_rotationListView.SelectedItems[0];
        int idx = item.Index;
        if (idx == 0) return;
        m_rotationListView.Items.RemoveAt(idx);
        m_rotationListView.Items.Insert(idx - 1, item);
        item.Selected = true;
      };
      rotationTab.Controls.Add(m_moveRotationUpBtn);

      m_moveRotationDownBtn = new Button
      {
        Text = "▼",
        Location = new Point(280, 440),
        Size = new Size(40, 30)
      };
      m_moveRotationDownBtn.Click += (s, e) =>
      {
        if (m_rotationListView.SelectedItems.Count == 0) return;
        ListViewItem item = m_rotationListView.SelectedItems[0];
        int idx = item.Index;
        if (idx == m_rotationListView.Items.Count - 1) return;
        m_rotationListView.Items.RemoveAt(idx);
        m_rotationListView.Items.Insert(idx + 1, item);
        item.Selected = true;
      };
      rotationTab.Controls.Add(m_moveRotationDownBtn);

      tabControl1.TabPages.Add(rotationTab);
    }
  }
}
