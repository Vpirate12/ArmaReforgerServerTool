/******************************************************************************
 * File Name:    ProcessManager.cs
 * Project:      Longbow
 * Description:  This file contains the singleton ProcessManager class
 *               responsible for the management, launching and handling of
 *               threaded operations and spawned processes such as SteamCMD
 *               and the Arma Reforger Server executable itself
 * 
 * Author:       Bradley Newman
 ******************************************************************************/

using FontAwesome.Sharp;
using Longbow.Managers;
using Longbow.Models;
using Longbow.Utils;
using ReforgerServerApp.Components;
using ReforgerServerApp.Models;
using ReforgerServerApp.Utils;
using Serilog;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace ReforgerServerApp.Managers
{
  public enum ServerRestartIntervalUnit { MINUTES, HOURS, DAYS }

  internal class ScenarioRotationSwitchEventArgs : EventArgs
  {
    public ScenarioRotationEntry Entry { get; }
    public ScenarioRotationSwitchEventArgs(ScenarioRotationEntry entry) { Entry = entry; }
  }

  internal class SteamCmdLogEventArgs : EventArgs
  {
    public string line;
    public SteamCmdLogEventArgs(string line) { this.line = line; }
  }

  internal class GuiModelEventArgs : EventArgs
  {
    public IconChar buttonIconChar;
    public bool enableServerFields;
    public string serverRunningLabelText;
    public bool startServerBtnEnabled;
    public GuiModelEventArgs() { }
  }

  internal class ProcessManager
  {
    private static ProcessManager? m_instance;
    private bool m_isServerStarted;
    private bool m_isServerUsingTimer;
    private Process m_steamCmdUpdateProcess;
    private Process m_serverProcess;
    private BackgroundWorker? m_worker;
    private readonly CancellationTokenSource m_timerCancellationTokenSource;
    private CancellationTokenSource? m_intervalRestartCts;
    private CancellationTokenSource? m_rotationCts;
    private List<ScenarioRotationEntry>? m_rotationEntries;
    private int m_rotationIndex = 0;
    private LaunchArguments m_launchArgumentsModel;

    public delegate void UpdateSteamCmdLogEventHandler(object sender, SteamCmdLogEventArgs e);
    public event UpdateSteamCmdLogEventHandler UpdateSteamCmdLogEvent;

    public delegate void UpdateGuiControlsEventHandler(object sender, GuiModelEventArgs e);
    public event UpdateGuiControlsEventHandler UpdateGuiControlsEvent;

    public delegate void UpdateServerStatusEventHandler(object sender, ServerStatusEventArgs e);
    public event UpdateServerStatusEventHandler UpdateServerStatusEvent;

    public delegate void ScenarioRotationSwitchEventHandler(object sender, ScenarioRotationSwitchEventArgs e);
    public event ScenarioRotationSwitchEventHandler ScenarioRotationSwitchEvent;

    public bool KeepServerUpdated { get; set; }

    private ProcessManager()
    {
      m_steamCmdUpdateProcess = new();
      m_serverProcess = new();
      m_timerCancellationTokenSource = new();
      m_launchArgumentsModel = new();
    }

    public static ProcessManager GetInstance()
    {
      m_instance ??= new ProcessManager();
      return m_instance;
    }

    public bool IsServerStarted() { return m_isServerStarted; }
    public bool IsServerUsingTimer() { return m_isServerUsingTimer; }
    public LaunchArguments GetLaunchArgumentsModel() { return m_launchArgumentsModel; }
    public void SetLaunchArgumentsModel(LaunchArguments la) { m_launchArgumentsModel = la; }

    /// <summary>
    /// This method controls the logic for Starting and Stopping the Server.
    /// When Starting the server, this will spawn the Worker Thread that runs the SteamCMD and server processes.
    /// When Stopping the server, this will kill the server process and remove the Output / Error redirects.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void StartStopServer(bool triggeredByAutoRestart = false)
    {
      if (m_isServerStarted)
      {
        if (!FileIOManager.GetInstance().ResetServerFile())
        {
          Log.Error("ProcessManager - Unable to verify the initial state of 'server.json', server not starting.");
          return;
        }

        try
        {
          Log.Information("ProcessManager - User stopped server.");

          SteamCmdLogEventArgs steamCmd = new($"{Utilities.GetTimestamp()}: User stopped server.{Environment.NewLine}");
          if (triggeredByAutoRestart)
          {
            steamCmd = new($"{Utilities.GetTimestamp()}: Automatically stopped server.{Environment.NewLine}");
          }
          OnUpdateSteamCmdLogEvent(steamCmd);

          if (NetworkManager.GetInstance().useUPnP)
          {
            steamCmd = new($"{Utilities.GetTimestamp()}: Removing UPnP port mappings...{Environment.NewLine}");
            OnUpdateSteamCmdLogEvent(steamCmd);
            await NetworkManager.GetInstance().RemovePortMappings(Utilities.GetPortMappingsFromServerConfig());
            steamCmd = new($"{Utilities.GetTimestamp()}: UPnP port mappings removed.{Environment.NewLine}");
            OnUpdateSteamCmdLogEvent(steamCmd);
          }

          m_serverProcess.Kill();
          m_serverProcess.Dispose();
          m_isServerStarted = false;

          ServerStatusEventArgs statusArgs = new()
          {
            ServerOnline = false
          };
          OnUpdateServerStatusEvent(statusArgs);


          GuiModelEventArgs guiModel = new()
          {
            buttonIconChar = IconChar.Play,
            enableServerFields = true,
            serverRunningLabelText = string.Empty,
            startServerBtnEnabled = true
          };
          OnUpdateGuiControlsEvent(guiModel);

        }
        catch (Exception ex)
        {
          if (m_serverProcess != null && !m_serverProcess.HasExited)
          {
            m_serverProcess.Kill();
            m_serverProcess.Dispose();
          }
          m_isServerStarted = false;
          GuiModelEventArgs guiModel = new()
          {
            buttonIconChar = IconChar.Play,
            enableServerFields = true,
            serverRunningLabelText = string.Empty,
            startServerBtnEnabled = true
          };
          OnUpdateGuiControlsEvent(guiModel);
        }
      }
      else
      {
        if (!FileIOManager.SaveConfigurationToFile(FileIOManager.GetInstance().GetAbsolutePathToServerFile()))
        {
          Log.Error("ProcessManager - Failed to save server.json to file, server not starting.");
          return;
        }

        // Check validation before launch
        var configMgr = ConfigurationManager.GetInstance();
        var validationResult = configMgr.GetLastValidationResult();

        Log.Information("[DEBUG] ProcessManager.StartStopServer: Validation check - Result: {result}, HasFatalErrors: {hasFatal}",
          validationResult == null ? "NULL" : $"Valid={validationResult.IsValid}, Errors={validationResult.Errors.Count}",
          validationResult?.HasFatalErrors());

        if (validationResult?.HasFatalErrors() == true)
        {
          Log.Error("[DEBUG] ProcessManager.StartStopServer: BLOCKING LAUNCH - Fatal validation errors detected!");
          ValidationLogger.LogLaunchBlocked(validationResult);
          return;
        }

        if (validationResult?.Warnings.Any() == true)
        {
          Log.Warning("[DEBUG] ProcessManager.StartStopServer: Allowing launch with warnings");
          ValidationLogger.LogLaunchWithWarnings(validationResult);
        }

        Log.Information("[DEBUG] ProcessManager.StartStopServer: Proceeding with server launch");

        Log.Information("Starting Arma Reforger server...");
        Log.Information("ProcessManager - User started server.");
        m_isServerStarted = true;


        GuiModelEventArgs guiModel = new()
        {
          buttonIconChar = IconChar.Stop,
          enableServerFields = false,
          serverRunningLabelText = Constants.SERVER_CURRENTLY_RUNNING_STR,
          startServerBtnEnabled = false
        };
        OnUpdateGuiControlsEvent(guiModel);

        SteamCmdLogEventArgs steamCmd = new($"{Utilities.GetTimestamp()}: User started server.{Environment.NewLine}");

        if (triggeredByAutoRestart)
        {
          steamCmd = new($"{Utilities.GetTimestamp()}: Automatically restarted server.{Environment.NewLine}");
        }

        OnUpdateSteamCmdLogEvent(steamCmd);

        SteamCmdLogEventArgs dling = new($"{Utilities.GetTimestamp()}: Downloading / updating Arma Reforger dedicated server files. Please be patient...{Environment.NewLine}");
        OnUpdateSteamCmdLogEvent(dling);

        if (NetworkManager.GetInstance().useUPnP)
        {
          steamCmd = new($"{Utilities.GetTimestamp()}: Server is using UPnP, adding UPnP port mappings...{Environment.NewLine}");
          OnUpdateSteamCmdLogEvent(steamCmd);
          await NetworkManager.GetInstance().ConfigurePortMappings(Utilities.GetPortMappingsFromServerConfig());
          steamCmd = new($"{Utilities.GetTimestamp()}: UPnP port mappings added.{Environment.NewLine}");
          OnUpdateSteamCmdLogEvent(steamCmd);
        }

        if (ConfigurationManager.GetInstance().useExperimentalServer)
        {
          SteamCmdLogEventArgs exp = new($"{Utilities.GetTimestamp()}: Server is using Experimental Branch. " +
              $"It is important to note that your server may not work as intended as the experimental branch frequently contains breaking changes.{Environment.NewLine}");
          OnUpdateSteamCmdLogEvent(exp);
        }
        m_worker?.Dispose();
        m_worker = new() { WorkerSupportsCancellation = true };
        m_worker.DoWork += SteamCmdUpdateWorkerDoWork;
        m_worker.RunWorkerAsync();
      }
    }

    /// <summary>
    /// This is almost identical to the DoStartStopServerLogic method with the difference being it
    /// automatically restarts the server after stopping it as it's not a toggle.
    /// </summary>
    private async void StartStopServerUsingTimer()
    {
      GuiModelEventArgs guiModel;
      SteamCmdLogEventArgs steamCmd;

      if (m_isServerStarted)
      {
        if (!FileIOManager.GetInstance().ResetServerFile())
        {
          Log.Error("ProcessManager - Unable to verify the initial state of 'server.json', server not starting.");
          return;
        }

        m_isServerStarted = false;

        guiModel = new()
        {
          buttonIconChar = IconChar.Play,
          enableServerFields = true,
          serverRunningLabelText = string.Empty,
          startServerBtnEnabled = true
        };
        OnUpdateGuiControlsEvent(guiModel);

        try
        {

          Log.Information("ProcessManager - Automatically stopped server.");
          steamCmd = new($"{Utilities.GetTimestamp()}: Automatically stopped server.{Environment.NewLine}");
          OnUpdateSteamCmdLogEvent(steamCmd);

          if (NetworkManager.GetInstance().useUPnP)
          {
            steamCmd = new($"{Utilities.GetTimestamp()}: Removing UPnP port mappings...{Environment.NewLine}");
            OnUpdateSteamCmdLogEvent(steamCmd);
            await NetworkManager.GetInstance().RemovePortMappings(Utilities.GetPortMappingsFromServerConfig());
            steamCmd = new($"{Utilities.GetTimestamp()}: UPnP port mappings removed.{Environment.NewLine}");
            OnUpdateSteamCmdLogEvent(steamCmd);
          }

          m_serverProcess.Kill();

        }
        catch (Exception ex)
        {
          Utilities.DisplayErrorMessage("Error", ex.Message);
        }
      }

      if (!FileIOManager.SaveConfigurationToFile(FileIOManager.GetInstance().GetAbsolutePathToServerFile()))
      {
        Log.Error("ProcessManager - Failed to save server.json to file, server not starting.");
        return;
      }

      m_isServerStarted = true;

      guiModel = new()
      {
        buttonIconChar = IconChar.Stop,
        enableServerFields = false,
        serverRunningLabelText = Constants.SERVER_CURRENTLY_RUNNING_STR,
        startServerBtnEnabled = false
      };
      OnUpdateGuiControlsEvent(guiModel);

      steamCmd = new($"{Utilities.GetTimestamp()}: Automatically started server.{Environment.NewLine}");
      Log.Information("ProcessManager - Automatically (re)started server.");
      OnUpdateSteamCmdLogEvent(steamCmd);

      SteamCmdLogEventArgs dling = new($"{Utilities.GetTimestamp()}: Downloading / updating Arma Reforger dedicated server files. Please be patient...{Environment.NewLine}");
      OnUpdateSteamCmdLogEvent(dling);

      if (NetworkManager.GetInstance().useUPnP)
      {
        steamCmd = new($"{Utilities.GetTimestamp()}: Server is using UPnP, adding UPnP port mappings...{Environment.NewLine}");
        OnUpdateSteamCmdLogEvent(steamCmd);
        await NetworkManager.GetInstance().ConfigurePortMappings(Utilities.GetPortMappingsFromServerConfig());
        steamCmd = new($"{Utilities.GetTimestamp()}: UPnP port mappings added.{Environment.NewLine}");
        OnUpdateSteamCmdLogEvent(steamCmd);
      }

      m_worker?.Dispose();
      m_worker = new() { WorkerSupportsCancellation = true };
      m_worker.DoWork += SteamCmdUpdateWorkerDoWork;
      m_worker.RunWorkerAsync();
    }

    /// <summary>
    /// Worker Thread task, this task spawns SteamCMD which will install / update the Arma Server files and then close, 
    /// once it has closed, the Arma Server is launched with the generated server configuration.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SteamCmdUpdateWorkerDoWork(object sender, DoWorkEventArgs e)
    {
      string updateSwitch =  KeepServerUpdated ? "+app_update" : "";
      string updateNotifMsg = KeepServerUpdated ? "Longbow will ensure the server is up-to-date." : "Longbow will not update the dedicated server.";
      SteamCmdLogEventArgs updateNotif = new($"{Utilities.GetTimestamp()}: {updateNotifMsg} {Environment.NewLine}");
      OnUpdateSteamCmdLogEvent(updateNotif);

      string steamCommand = $"+force_install_dir ..\\Arma_Reforger +login anonymous anonymous {updateSwitch} 1874900 +quit";
      if (ConfigurationManager.GetInstance().useExperimentalServer)
      {
        steamCommand = $"+force_install_dir ..\\Arma_Reforger\\experimental +login anonymous anonymous {updateSwitch} 1890870 +quit";
      }

      ProcessStartInfo steamCmdStartInfo = new()
      {
        WindowStyle = ProcessWindowStyle.Hidden,
        FileName = FileIOManager.GetInstance().GetSteamCmdFile(),
        Arguments = steamCommand,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
      };

      m_steamCmdUpdateProcess = new()
      {
        EnableRaisingEvents = true,
        StartInfo = steamCmdStartInfo

      };

      m_steamCmdUpdateProcess.Start();

      Task steamStdout = Task.Run(() => ReadStreamAsync(m_steamCmdUpdateProcess.StandardOutput));
      Task steamStderr = Task.Run(() => ReadStreamAsync(m_steamCmdUpdateProcess.StandardError));
      m_steamCmdUpdateProcess.WaitForExit();
      // Wait a split second to ensure the stream readers finish emptying the pipe
      Task.WaitAll(steamStdout, steamStderr);
      bool steamCmdExited = m_steamCmdUpdateProcess.HasExited;
      m_steamCmdUpdateProcess.Dispose();

      if (steamCmdExited)
      {
        GuiModelEventArgs guiModel = new()
        {
          buttonIconChar = IconChar.Stop,
          enableServerFields = false,
          serverRunningLabelText = Constants.SERVER_CURRENTLY_RUNNING_STR,
          startServerBtnEnabled = true
        };
        OnUpdateGuiControlsEvent(guiModel);

        string serverWorkingDir = $"{FileIOManager.GetInstance().GetInstallDirectory()}\\arma_reforger";
        string serverFileName = $"{FileIOManager.GetInstance().GetInstallDirectory()}\\arma_reforger\\ArmaReforgerServer.exe";

        if (ConfigurationManager.GetInstance().useExperimentalServer)
        {
          serverWorkingDir = $"{FileIOManager.GetInstance().GetInstallDirectory()}\\arma_reforger\\experimental";
          serverFileName = $"{FileIOManager.GetInstance().GetInstallDirectory()}\\arma_reforger\\experimental\\ArmaReforgerServer.exe";
        }

        ProcessStartInfo serverStartInfo = new()
        {
          WindowStyle = ProcessWindowStyle.Hidden,
          UseShellExecute = false,
          WorkingDirectory = serverWorkingDir,
          FileName = serverFileName,
          Arguments = GetLaunchArguments(),
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          CreateNoWindow = true
        };

        m_serverProcess.Dispose();
        m_serverProcess = new()
        {
          EnableRaisingEvents = true,
          StartInfo = serverStartInfo
        };

        SteamCmdLogEventArgs starting = new($"{Utilities.GetTimestamp()}: Download / update complete. Starting the dedicated server...{Environment.NewLine}");
        OnUpdateSteamCmdLogEvent(starting);

        m_serverProcess.Start();
        Task armaStdout = Task.Run(() => ReadStreamAsync(m_serverProcess.StandardOutput));
        Task armaStderr = Task.Run(() => ReadStreamAsync(m_serverProcess.StandardError));
      }
    }

    /// <summary>
    /// Method for starting asynchronous periodic timer based functionality
    /// </summary>
    /// <param name="action">Method to run</param>
    /// <param name="cancellationToken">Cancellation token to cancel with</param>
    /// <returns></returns>
    public static async Task PeriodicAsync(Action action, CancellationToken cancellationToken = default)
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        if (ConfigurationManager.GetInstance().GetAdvancedServerParametersDictionary()["autoRestartDaily"] is AdvancedServerParameterTime
            autoRestartDaily && autoRestartDaily.Checked())
        {
          // Calculate time until the next run
          TimeSpan scheduledTime = ((DateTime)autoRestartDaily.ParameterValue).TimeOfDay;
          DateTime now = DateTime.Now;
          DateTime todayScheduled = now.Date + scheduledTime;
          DateTime nextRun = todayScheduled > now ? todayScheduled : todayScheduled.AddDays(1);
          TimeSpan delay = nextRun - now;
          // Wait until the next scheduled time
          await Task.Delay(delay, cancellationToken);
          action();
        }
      }
    }

    /// <summary>
    /// Handler for when data is received from the Std Output or Error from SteamCMD or the Arma Server processes
    /// </summary>
    /// <param name="reader"></param>
    private async Task ReadStreamAsync(StreamReader reader)
    {
      char[] buffer = new char[256]; // Read in small, fast 256-byte chunks
      StringBuilder lineBuilder = new StringBuilder();

      while (!reader.EndOfStream)
      {
        // Pull data immediately as it hits the pipe
        int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);

        if (bytesRead > 0)
        {
          string chunk = new string(buffer, 0, bytesRead);

          foreach (char c in chunk)
          {
            // Trigger on both \n and \r
            if (c == '\n' || c == '\r')
            {
              if (lineBuilder.Length > 0)
              {
                string fullLine = lineBuilder.ToString() + Environment.NewLine;
                lineBuilder.Clear();

                SteamCmdLogEventArgs steamCmd = new SteamCmdLogEventArgs($"{Utilities.GetTimestamp()}: {fullLine}");
                OnUpdateSteamCmdLogEvent(steamCmd);

                if (fullLine.Contains("Error while initializing game") || fullLine.Contains("Unable to initialize the game"))
                {
                  steamCmd = new($"{Utilities.GetTimestamp()}: System stopped server due to an error.{Environment.NewLine}");
                  Log.Information("ProcessManager - System stopped server due to an error.");
                  OnUpdateSteamCmdLogEvent(steamCmd);
                  m_serverProcess.Kill();

                  m_isServerStarted = false;

                  GuiModelEventArgs guiModel = new()
                  {
                    buttonIconChar = IconChar.Play,
                    enableServerFields = true,
                    serverRunningLabelText = string.Empty,
                    startServerBtnEnabled = true
                  };
                  OnUpdateGuiControlsEvent(guiModel);
                }

                // If server crashes, if the user has enabled auto restart on crash then attempt to restart it
                if (fullLine.Contains("Game destroyed"))
                {
                  if (ConfigurationManager.GetInstance().autoRestartOnCrash)
                  {
                    steamCmd = new($"{Utilities.GetTimestamp()}: Game destroyed detected. System attempting to restart server...{Environment.NewLine}");
                    OnUpdateSteamCmdLogEvent(steamCmd);
                    Log.Information("ProcessManager - Game destroyed detected. Attempting to restart server...");

                    // Stop the server (1st toggle)
                    StartStopServer(true);

                    _ = Task.Delay(ToolPropertiesManager.GetInstance().GetToolProperties().autoRestartTime_ms).ContinueWith(_ =>
                    {
                      Log.Information("ProcessManager - Restarting server now...");
                      StartStopServer(true); // Start the server (2nd toggle)
                    });
                  }
                }
              }
            }
            else
            {
              lineBuilder.Append(c);
            }
          }
        }
      }
    }

    /// <summary>
    /// Configure the timer to start the server
    /// </summary>
    public void ConfigureAutomaticRestartTask()
    {
      StartStopServerUsingTimer();
      Task automaticRestartTask = PeriodicAsync(StartStopServerUsingTimer, m_timerCancellationTokenSource.Token);
      m_isServerUsingTimer = true;
    }

    /// <summary>
    /// Cancel the Automatic Server Restart Task and timer
    /// This will also shut down the server
    /// </summary>
    public void CancelAutomaticRestartTask()
    {
      m_timerCancellationTokenSource.Cancel();
      StartStopServer();
      m_isServerUsingTimer = false;
    }

    public void KillServer()
    {
      m_serverProcess.Kill();
    }

    /// <summary>
    /// Sender for the 'UpdateSteamCmdLog' Event
    /// </summary>
    /// <param name="e">Arguments to pass to the GUI to inform it that it needs to update the Steam CMD Log</param>
    protected virtual void OnUpdateSteamCmdLogEvent(SteamCmdLogEventArgs e)
    {
      UpdateSteamCmdLogEvent?.Invoke(this, e);
    }

    /// <summary>
    /// Sender for the 'UpdateGuiControls' Event
    /// </summary>
    /// <param name="e">Arguments to pass to the GUI to inform it that it needs to update various controls</param>
    protected virtual void OnUpdateGuiControlsEvent(GuiModelEventArgs e)
    {
      UpdateGuiControlsEvent?.Invoke(this, e);
    }


    /// <summary>
    /// Sender for the 'UpdateServerStatusEvent' Event
    /// </summary>
    /// <param name="e">Arguments to pass to the GUI to inform it that it needs to update various controls</param>
    protected virtual void OnUpdateServerStatusEvent(ServerStatusEventArgs e)
    {
      UpdateServerStatusEvent?.Invoke(this, e);
    }

    /// <summary>
    /// Get Launch Arguments as a String
    /// </summary>
    /// <returns>String representation of Launch Arguments</returns>
    public string GetLaunchArguments()
    {
      // Check if we should be loading a save game and if so, add -loadSessionSave
      if (ConfigurationManager.GetInstance().usingSave)
      {
        // Default to switch style launch argument to load latest
        m_launchArgumentsModel.loadSessionSave = new("loadSessionSave");
        // Now check if we're using a specific save instead
        if (!ConfigurationManager.GetInstance().save.Equals(".LatestSave"))
        {
          // Need to wrap the save name in quotes as the tool allows for spaces
          m_launchArgumentsModel.loadSessionSave = new("loadSessionSave", $"\"{ConfigurationManager.GetInstance().save}\"");
        }
      }

      string args = string.Join(" ", new[] {
                                               m_launchArgumentsModel.profile,
                                               m_launchArgumentsModel.addonsDir,
                                               m_launchArgumentsModel.logStats,
                                               m_launchArgumentsModel.maxFPS,
                                               m_launchArgumentsModel.bindPort,
                                               m_launchArgumentsModel.autoReload,
                                               m_launchArgumentsModel.rplTimeoutMs,
                                               m_launchArgumentsModel.nds,
                                               m_launchArgumentsModel.nwkResolution,
                                               m_launchArgumentsModel.staggeringBudget,
                                               m_launchArgumentsModel.streamingBudget,
                                               m_launchArgumentsModel.streamsDelta,
                                               m_launchArgumentsModel.loadSessionSave,
                                               m_launchArgumentsModel.freezeCheck,
                                               m_launchArgumentsModel.freezeCheckMode,
                                               m_launchArgumentsModel.addonsRepair,
                                               m_launchArgumentsModel.autoShutdown,
                                               m_launchArgumentsModel.logVoting,
                                               m_launchArgumentsModel.aiPartialSim,
                                               m_launchArgumentsModel.createDB,
                                               m_launchArgumentsModel.debugger,
                                               m_launchArgumentsModel.debuggerPort,
                                               m_launchArgumentsModel.disableShadersBuild,
                                               m_launchArgumentsModel.generateShaders,
                                               m_launchArgumentsModel.rplEncodeAsLongJobs,
                                               m_launchArgumentsModel.jobSysShortWorkerCount,
                                               m_launchArgumentsModel.jobSysLongWorkerCount,
                                               m_launchArgumentsModel.forceDisableNightGrain,
                                               m_launchArgumentsModel.logLevel}.Where(arg => arg != null));

      if (!ConfigurationManager.GetInstance().noBackend)
      {
        // Concatenate the current args with the advanced parameters, if applicable
        args = string.Join(" ", args, string.Join(" ", new[] { m_launchArgumentsModel.config }.Where(arg => arg != null)));
      }
      else
      {
        // We are running the server with No Backend, use the no backend launch arguments
        args = string.Join(" ", new[] { args, ConfigurationManager.CreateNoBackendLaunchArguments() });

        // Because Bind Port can be specified as an 'override', ensure that that takes precedence
        if (m_launchArgumentsModel.bindPort == null)
        {
          args = string.Join(" ", new[] { args, $"-bindPort {ConfigurationManager.GetInstance().GetServerConfiguration().root.bindPort}" });
        }
      }
      Log.Information("Launching server with the following launch arguments: \"{args}\"", args);
      return args;
    }

    public bool IsIntervalRestartEnabled => m_intervalRestartCts != null && !m_intervalRestartCts.IsCancellationRequested;

    public void ConfigureIntervalRestartTask(int intervalHours)
    {
      m_intervalRestartCts?.Cancel();
      m_intervalRestartCts?.Dispose();
      m_intervalRestartCts = new CancellationTokenSource();
      _ = RunIntervalRestartAsync(TimeSpan.FromHours(intervalHours), m_intervalRestartCts.Token);
      Log.Information("ProcessManager - Interval restart configured for every {hours} hour(s).", intervalHours);
      OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: Interval restart active — server will restart every {intervalHours} hour(s) with player warnings.{Environment.NewLine}"));
    }

    public void CancelIntervalRestartTask()
    {
      if (m_intervalRestartCts == null) return;
      m_intervalRestartCts.Cancel();
      m_intervalRestartCts.Dispose();
      m_intervalRestartCts = null;
      Log.Information("ProcessManager - Interval restart cancelled.");
      OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: Interval restart cancelled.{Environment.NewLine}"));
    }

    private static readonly (string Message, TimeSpan WaitAfter)[] s_restartWarnings =
    {
      ("Server restarting in 10 minutes.", TimeSpan.FromMinutes(5)),
      ("Server restarting in 5 minutes.",  TimeSpan.FromMinutes(4)),
      ("Server restarting in 1 minute.",   TimeSpan.FromMinutes(1)),
    };

    private async Task RunIntervalRestartAsync(TimeSpan interval, CancellationToken ct)
    {
      while (!ct.IsCancellationRequested)
      {
        TimeSpan warningLeadTime = TimeSpan.FromMinutes(10);
        TimeSpan initialWait = interval > warningLeadTime ? interval - warningLeadTime : TimeSpan.Zero;

        if (initialWait > TimeSpan.Zero)
          await Task.Delay(initialWait, ct).ConfigureAwait(false);

        foreach (var (message, waitAfter) in s_restartWarnings)
        {
          if (ct.IsCancellationRequested) return;
          await RconManager.GetInstance().SendBroadcastAsync(message);
          OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: [Interval Restart] {message}{Environment.NewLine}"));
          Log.Information("ProcessManager - Interval restart warning: {msg}", message);
          await Task.Delay(waitAfter, ct).ConfigureAwait(false);
        }

        if (ct.IsCancellationRequested) return;

        await RconManager.GetInstance().SendBroadcastAsync("Server is restarting now. See you shortly!");
        OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: [Interval Restart] Restarting server now...{Environment.NewLine}"));
        Log.Information("ProcessManager - Interval restart: restarting server.");

        if (m_isServerStarted)
        {
          StartStopServer(true);
          int delayMs = ToolPropertiesManager.GetInstance().GetToolProperties().autoRestartTime_ms;
          await Task.Delay(delayMs, ct).ConfigureAwait(false);
          if (!ct.IsCancellationRequested)
            StartStopServer(true);
        }
      }
    }

    public bool IsRotationEnabled => m_rotationCts != null && !m_rotationCts.IsCancellationRequested;

    public void ConfigureRotationTask(List<ScenarioRotationEntry> entries)
    {
      if (entries == null || entries.Count == 0) return;
      m_rotationCts?.Cancel();
      m_rotationCts?.Dispose();
      m_rotationEntries = new List<ScenarioRotationEntry>(entries);
      m_rotationIndex = 0;
      m_rotationCts = new CancellationTokenSource();
      _ = RunRotationAsync(m_rotationCts.Token);
      Log.Information("ProcessManager - Scenario rotation configured with {count} scenario(s).", entries.Count);
      OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: Scenario rotation active — {entries.Count} scenario(s) in queue.{Environment.NewLine}"));
    }

    public void CancelRotationTask()
    {
      if (m_rotationCts == null) return;
      m_rotationCts.Cancel();
      m_rotationCts.Dispose();
      m_rotationCts = null;
      Log.Information("ProcessManager - Scenario rotation cancelled.");
      OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: Scenario rotation cancelled.{Environment.NewLine}"));
    }

    protected virtual void OnScenarioRotationSwitchEvent(ScenarioRotationSwitchEventArgs e)
    {
      ScenarioRotationSwitchEvent?.Invoke(this, e);
    }

    private async Task RunRotationAsync(CancellationToken ct)
    {
      while (!ct.IsCancellationRequested && m_rotationEntries != null && m_rotationEntries.Count > 0)
      {
        ScenarioRotationEntry current = m_rotationEntries[m_rotationIndex];
        TimeSpan interval = TimeSpan.FromHours(current.DurationHours);
        TimeSpan warningLeadTime = TimeSpan.FromMinutes(10);
        TimeSpan initialWait = interval > warningLeadTime ? interval - warningLeadTime : TimeSpan.Zero;

        if (initialWait > TimeSpan.Zero)
          await Task.Delay(initialWait, ct).ConfigureAwait(false);

        if (ct.IsCancellationRequested) return;

        // Advance to the next scenario
        m_rotationIndex = (m_rotationIndex + 1) % m_rotationEntries.Count;
        ScenarioRotationEntry next = m_rotationEntries[m_rotationIndex];

        foreach (var (message, waitAfter) in s_restartWarnings)
        {
          if (ct.IsCancellationRequested) return;
          string fullMsg = $"{message} Next scenario: {next.ScenarioName}";
          await RconManager.GetInstance().SendBroadcastAsync(fullMsg);
          OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: [Rotation] {fullMsg}{Environment.NewLine}"));
          Log.Information("ProcessManager - Rotation warning: {msg}", fullMsg);
          await Task.Delay(waitAfter, ct).ConfigureAwait(false);
        }

        if (ct.IsCancellationRequested) return;

        await RconManager.GetInstance().SendBroadcastAsync($"Switching to scenario: {next.ScenarioName}. Server restarting now!");
        OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: [Rotation] Switching to: {next.ScenarioName} ({next.ScenarioPath}){Environment.NewLine}"));
        Log.Information("ProcessManager - Rotation switching to: {name}", next.ScenarioName);

        // Notify UI to update the displayed scenario
        OnScenarioRotationSwitchEvent(new(next));

        // Give event handler time to update ConfigurationManager before the restart saves server.json
        await Task.Delay(500, ct).ConfigureAwait(false);

        if (m_isServerStarted)
        {
          StartStopServer(true);
          int delayMs = ToolPropertiesManager.GetInstance().GetToolProperties().autoRestartTime_ms;
          await Task.Delay(delayMs, ct).ConfigureAwait(false);
          if (!ct.IsCancellationRequested)
            StartStopServer(true);
        }
      }
    }

    public void Dispose()
    {
      m_timerCancellationTokenSource.Dispose();
      m_intervalRestartCts?.Cancel();
      m_intervalRestartCts?.Dispose();
      m_rotationCts?.Cancel();
      m_rotationCts?.Dispose();
      m_worker?.Dispose();
      m_serverProcess.Dispose();
      m_steamCmdUpdateProcess.Dispose();
    }
  }
}
