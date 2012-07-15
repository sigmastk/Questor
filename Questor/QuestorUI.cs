﻿
namespace Questor
{
    using System;
    using System.Drawing;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Linq;
    using System.Windows.Forms;
    using System.IO;
    using LavishScriptAPI;
    using global::Questor.Behaviors;
    using global::Questor.Modules.Actions;
    using global::Questor.Modules.Activities;
    using global::Questor.Modules.BackgroundTasks;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Combat;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.States;
    using Action = global::Questor.Modules.Actions.Action;

    public partial class QuestorfrmMain : Form
    {
        private readonly Questor _questor;
        //private DateTime _lastlogmessage
        private DateTime _nextConsoleLogRefresh = DateTime.MinValue;
        private DateTime _nextUIDataRefresh = DateTime.Now;
        private DateTime _nextScheduleUpdate = DateTime.Now;
        private DateTime _nextWreckUpdate = DateTime.Now;

        public QuestorfrmMain()
        {
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "QuestorfrmMain", Logging.white);
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "InitializeComponent", Logging.white);
            InitializeComponent();
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "_questor = new Questor(this);", Logging.white);
            _questor = new Questor(this);
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "PopulateStateComboBoxes", Logging.white);
            PopulateStateComboBoxes();
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "PopulateBehaviorStateComboBox", Logging.white);
            PopulateBehaviorStateComboBox();
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "CreateLavishCommands", Logging.white);
            CreateLavishCommands();
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "this.Show();", Logging.white);
            this.Show();
            if (Settings.Instance.DebugAttachVSDebugger)
            {
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    Logging.Log("QuestorUI", "VS Debugger is not yet attached: System.Diagnostics.Debugger.Launch()", Logging.teal);
                    System.Diagnostics.Debugger.Launch();
                }
            }
        }

        private void QuestorfrmMainFormClosed(object sender, FormClosedEventArgs e)
        {
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "QuestorfrmMainFormClosed", Logging.white);

            Cache.Instance.DirectEve.Dispose();
            Cache.Instance.DirectEve = null;
        }

        private void PopulateStateComboBoxes()
        {
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "PopulateStateComboBoxes", Logging.white);
            QuestorStateComboBox.Items.Clear();
            foreach (string text in Enum.GetNames(typeof(QuestorState)))
                QuestorStateComboBox.Items.Add(text);

            if (Settings.Instance.CharacterMode != null)
            {
                //
                // populate combo boxes with the various states that are possible
                //
                // ComboxBoxes on main windows (at top)
                //
                DamageTypeComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(DamageType)))
                    DamageTypeComboBox.Items.Add(text);

                //
                // middle column
                //
                PanicStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(PanicState)))
                    PanicStateComboBox.Items.Add(text);

                CombatStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(CombatState)))
                    CombatStateComboBox.Items.Add(text);

                DronesStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(DroneState)))
                    DronesStateComboBox.Items.Add(text);

                CleanupStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(CleanupState)))
                    CleanupStateComboBox.Items.Add(text);

                LocalWatchStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(LocalWatchState)))
                    LocalWatchStateComboBox.Items.Add(text);

                SalvageStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(SalvageState)))
                    SalvageStateComboBox.Items.Add(text);

                //
                // right column
                //
                CombatMissionCtrlStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(CombatMissionCtrlState)))
                    CombatMissionCtrlStateComboBox.Items.Add(text);

                StorylineStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(StorylineState)))
                    StorylineStateComboBox.Items.Add(text);

                ArmStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(ArmState)))
                    ArmStateComboBox.Items.Add(text);

                UnloadStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(UnloadLootState)))
                    UnloadStateComboBox.Items.Add(text);

                TravelerStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(TravelerState)))
                    TravelerStateComboBox.Items.Add(text);

                AgentInteractionStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(AgentInteractionState)))
                    AgentInteractionStateComboBox.Items.Add(text);
            }
        }

        private void PopulateMissionLists()
        {
            //Logging.Log("QuestorUI","populating MissionBlacklisttextbox",Logging.white);
            BlacklistedMissionstextbox.Text = "";
            foreach (string blacklistedmission in Settings.Instance.MissionBlacklist)
            {
                BlacklistedMissionstextbox.AppendText(blacklistedmission + "\r\n");
            }

            //Logging.Log("QuestorUI", "populating MissionBlacklisttextbox", Logging.white);
            GreyListedMissionsTextBox.Text = "";
            foreach (string greylistedmission in Settings.Instance.MissionGreylist)
            {
                GreyListedMissionsTextBox.AppendText(greylistedmission + "\r\n");
            }
        }

        private void RefreshInfoDisplayedInUI()
        {
            if (DateTime.Now > _nextUIDataRefresh && DateTime.Now > Cache.Instance.QuestorStarted_DateTime.AddSeconds(30) && (Cache.Instance.LastInSpace.AddSeconds(2) > DateTime.Now || Cache.Instance.LastInSpace.AddSeconds(2) > DateTime.Now))
            {
                _nextUIDataRefresh = DateTime.Now.AddMilliseconds(1000);
                try
                {
                    CurrentTimeData1.Text = DateTime.Now.ToLongTimeString();
                    CurrentTimeData2.Text = DateTime.Now.ToLongTimeString();
                    NextOpenContainerInSpaceActionData.Text = Cache.Instance.NextOpenContainerInSpaceAction.ToLongTimeString();
                    NextOpenJournalWindowActionData.Text = Cache.Instance.NextOpenJournalWindowAction.ToLongTimeString();
                    NextOpenLootContainerActionData.Text = Cache.Instance.NextOpenLootContainerAction.ToLongTimeString();
                    NextDroneBayActionData.Text = Cache.Instance.NextDroneBayAction.ToLongTimeString();
                    NextOpenHangarActionData.Text = Cache.Instance.NextOpenHangarAction.ToLongTimeString();
                    NextOpenCargoActionData.Text = Cache.Instance.NextOpenCargoAction.ToLongTimeString();
                    LastActionData.Text = Cache.Instance.LastAction.ToLongTimeString();
                    NextArmActionData.Text = Cache.Instance.NextArmAction.ToLongTimeString();
                    NextSalvageActionData.Text = Cache.Instance.NextSalvageAction.ToLongTimeString();
                    NextLootActionData.Text = Cache.Instance.NextLootAction.ToLongTimeString();
                    LastJettisonData.Text = Cache.Instance.LastJettison.ToLongTimeString();
                    NextDefenceModuleActionData.Text = Cache.Instance.NextDefenceModuleAction.ToLongTimeString();
                    NextAfterburnerActionlbl.Text = Cache.Instance.NextAfterburnerAction.ToLongTimeString();
                    NextRepModuleActionData.Text = Cache.Instance.NextRepModuleAction.ToLongTimeString();
                    NextActivateSupportModulesData.Text = Cache.Instance.NextActivateSupportModules.ToLongTimeString();
                    NextApproachActionData.Text = Cache.Instance.NextApproachAction.ToLongTimeString();
                    NextOrbitData.Text = Cache.Instance.NextOrbit.ToLongTimeString();
                    NextWarpToData.Text = Cache.Instance.NextWarpTo.ToLongTimeString();
                    NextTravelerActionData.Text = Cache.Instance.NextTravelerAction.ToLongTimeString();
                    NextTargetActionData.Text = Cache.Instance.NextTargetAction.ToLongTimeString();
                    NextReloadData.Text = Cache.Instance.NextReload.ToLongTimeString();
                    NextWeaponActionData.Text = Cache.Instance.NextWeaponAction.ToLongTimeString();
                    NextWebActionData.Text = Cache.Instance.NextWebAction.ToLongTimeString();
                    NextNosActionData.Text = Cache.Instance.NextNosAction.ToLongTimeString();
                    NextPainterActionData.Text = Cache.Instance.NextPainterAction.ToLongTimeString();
                    NextActivateActionData.Text = Cache.Instance.NextActivateAction.ToLongTimeString();
                    NextAlignData.Text = Cache.Instance.NextAlign.ToLongTimeString();
                    NextUndockActionData.Text = Cache.Instance.NextUndockAction.ToLongTimeString();
                    NextDockActionData.Text = Cache.Instance.NextDockAction.ToLongTimeString();
                    NextDroneRecallData.Text = Cache.Instance.NextDroneRecall.ToLongTimeString();
                    NextStartupActionData.Text = Cache.Instance.NextStartupAction.ToLongTimeString();
                    LastSessionChangeData.Text = Cache.Instance.LastSessionChange.ToLongTimeString();
                    AutostartData.Text = Settings.Instance.AutoStart.ToString(CultureInfo.InvariantCulture);

                    DamageTypeData.Text = Cache.Instance.DamageType.ToString();
                    //OrbitDistanceData.Text = Cache.Instance.OrbitDistance.ToString(CultureInfo.InvariantCulture);
                    //AgentStationIDData.Text = Cache.Instance.AgentStationID.ToString(CultureInfo.InvariantCulture);
                    //AgentIdData.Text = Cache.Instance.AgentId.ToString(CultureInfo.InvariantCulture);
                    //AgentData.Text = Cache.Instance.CurrentAgent.ToString(CultureInfo.InvariantCulture);
                    AgentInteractionPurposeData.Text = AgentInteraction.Purpose.ToString();
                    MissionsThisSessionData.Text = Cache.Instance.MissionsThisSession.ToString(CultureInfo.InvariantCulture);
                    if (Cache.Instance.LastInSpace.AddSeconds(2) > DateTime.Now)
                    {
                        //MaxRangeData.Text = Cache.Instance.MaxRange.ToString(CultureInfo.InvariantCulture);       //causes problems / crashes
                        //WeaponRangeData.Text = Cache.Instance.WeaponRange.ToString(CultureInfo.InvariantCulture); //causes problems / crashes
                        //ActiveDronesData.Text = Cache.Instance.ActiveDrones.Count().ToString();                   //causes problems / crashes
                        //if (!Cache.Instance.InWarp && DateTime.Now > _nextWreckUpdate)                            //this was causing exceptions we cant check inarp from the UI?
                        //{
                        //    _nextWreckUpdate = DateTime.Now.AddSeconds(10);
                            //WrecksData.Text = Cache.Instance.Wrecks.Count().ToString(CultureInfo.InvariantCulture);
                            //UnlootedContainersData.Text = Cache.Instance.UnlootedContainers.Count().ToString(CultureInfo.InvariantCulture); 
                            //ApproachingData.Text = Cache.Instance.IsApproaching.ToString(CultureInfo.InvariantCulture);
                        //}
                        //DamagedDronesData.Text = Cache.Instance.DamagedDrones.Count().ToString(CultureInfo.InvariantCulture);
                        //PriorityTargetsData.Text = Cache.Instance.PriorityTargets.Count().ToString(CultureInfo.InvariantCulture);
                        //if (Cache.Instance.IsMissionPocketDone) IsMissionPocketDoneData.Text = "true";
                        //else if (!Cache.Instance.IsMissionPocketDone) IsMissionPocketDoneData.Text = "false";
                        
                    }
                    if (Cache.Instance.LastInStation.AddSeconds(2) > DateTime.Now)
                    {
                        MaxRangeData.Text = "n/a";
                        ActiveDronesData.Text = "n/a";
                        ApproachingData.Text = "n/a";
                        DamagedDronesData.Text = "n/a";
                        PriorityTargetsData.Text = "n/a";
                        WeaponRangeData.Text = "n/a";
                        IsMissionPocketDoneData.Text = "n/a";
                        WrecksData.Text = "n/a";
                        UnlootedContainersData.Text = "n/a";
                    }
                }
                catch (Exception)
                {
                    if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "RefreshInfoDisplayedInUI: unable to update all UI labels", Logging.teal);
                }
            }
            if (DateTime.Now > _nextScheduleUpdate)
            {
                _nextScheduleUpdate = DateTime.Now.AddSeconds(90);
                if (Cache.Instance.StopTimeSpecified)
                {
                    ScheduledStopTimeData.Text = Cache.Instance.StopTime.ToShortTimeString();
                }
                //
                // if control is enabled (checked) then update ManualStopTime so that on next idle questor will check to see if it needs to stop
                //
                if (dateTimePicker1.Checked)
                {
                    Cache.Instance.ManualStopTime = dateTimePicker1.Value;
                    if (Cache.Instance.ManualStopTime > Cache.Instance.StopTime)
                        Cache.Instance.StopTimeSpecified = false;
                    if (Cache.Instance.ManualStopTime < Cache.Instance.StopTime)
                        Cache.Instance.StopTimeSpecified = true;
                }
                else 
                {
                    dateTimePicker1.Value = DateTime.Now.AddHours(1);
                    if (!dateTimePicker2.Checked)
                    {
                        Cache.Instance.StopTimeSpecified = true;
                    }
                }
                //
                // if control is enabled (checked) then update ManualRestartTime so that on next idle questor will check to see if it needs to stop/restart
                //
                if (dateTimePicker2.Checked)
                {
                    Cache.Instance.ManualRestartTime = dateTimePicker2.Value;
                    if (Cache.Instance.ManualRestartTime > Cache.Instance.StopTime)
                        Cache.Instance.StopTimeSpecified = false;
                    if (Cache.Instance.ManualRestartTime < Cache.Instance.StopTime)
                        Cache.Instance.StopTimeSpecified = true;
                }
                else
                {
                    dateTimePicker1.Value = DateTime.Now.AddHours(1);
                    if (!dateTimePicker1.Checked)
                    {
                        Cache.Instance.StopTimeSpecified = true;
                    }
                }
            }
        }

        private void PopulateBehaviorStateComboBox()
        {
            if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "PopulateBehaviorStateComboBox", Logging.white);
            if (Settings.Instance.CharacterMode != null)
            {
                //
                // populate combo boxes with the various states that are possible
                //
                // left column
                //
                if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                {
                    BehaviorComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(CombatMissionsBehaviorState)))
                        BehaviorComboBox.Items.Add(text);
                }
                if (_States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
                {
                    BehaviorComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(DedicatedBookmarkSalvagerBehaviorState)))
                        BehaviorComboBox.Items.Add(text);
                }
                if (_States.CurrentQuestorState == QuestorState.CombatHelperBehavior)
                {
                    BehaviorComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(CombatHelperBehaviorState)))
                        BehaviorComboBox.Items.Add(text);
                }
                if (_States.CurrentQuestorState == QuestorState.DirectionalScannerBehavior)
                {
                    BehaviorComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(DirectionalScannerBehaviorState)))
                        BehaviorComboBox.Items.Add(text);
                }
            }
        }

        private void CreateLavishCommands()
        {
            if (Settings.Instance.UseInnerspace)
            {
                LavishScript.Commands.AddCommand("SetAutoStart", SetAutoStart);
                LavishScript.Commands.AddCommand("SetDisable3D", SetDisable3D);
                LavishScript.Commands.AddCommand("SetExitWhenIdle", SetExitWhenIdle);
                LavishScript.Commands.AddCommand("SetQuestorStatetoCloseQuestor", SetQuestorStatetoCloseQuestor);
                LavishScript.Commands.AddCommand("SetQuestorStatetoIdle", SetQuestorStatetoIdle);
            }
        }

        public void CloseQuestor()
        {
            int secRestart = (600 * 3) + Cache.Instance.RandomNumber(3, 18) * 100 + Cache.Instance.RandomNumber(1, 9) * 10;

            Cache.Instance.SessionState = "Quitting!!";
            //so that IF we changed the state we would not be caught in a loop of re-entering closequestor
            if (!Cache.Instance.CloseQuestorCMDLogoff && !Cache.Instance.CloseQuestorCMDExitGame)
            {
                Cache.Instance.CloseQuestorCMDExitGame = true;
            }

            if (Settings.Instance.AutoStart)
            //if autostart is disabled do not schedule a restart of questor - let it stop gracefully.
            {
                if (Cache.Instance.CloseQuestorCMDLogoff)
                {
                    Logging.Log("QuestorUI",
                                "Logging off EVE: In theory eve and questor will restart on their own when the client comes back up",
                                Logging.white);
                    if (Settings.Instance.UseInnerspace)
                        LavishScript.ExecuteCommand(
                            "uplink echo Logging off EVE:  \\\"${Game}\\\" \\\"${Profile}\\\"");
                    Logging.Log("QuestorUI",
                                "you can change this option by setting the wallet and eveprocessmemoryceiling options to use exit instead of logoff: see the settings.xml file",
                                Logging.white);

                    Logging.Log("QuestorUI", "Exiting eve now.", Logging.white);

                    Cache.Instance.DirecteveDispose();
                    Process.GetCurrentProcess().Kill();
                    Environment.Exit(0);
                    //Application.Exit();
                    return;
                }
                if (Cache.Instance.CloseQuestorCMDExitGame)
                {
                    if (Settings.Instance.UseInnerspace)
                    {
                        //Logging.Log("Questor: We are in station: Exit option has been configured.");
                        if (((Settings.Instance.CloseQuestorArbitraryOSCmd) &&
                             (Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet)) ||
                            (Settings.Instance.CloseQuestorArbitraryOSCmd) &&
                            (Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile))
                        {
                            Logging.Log(
                                "QuestorUI",
                                "You can't combine CloseQuestorArbitraryOSCmd with either of the other two options, fix your settings",
                                Logging.white);
                        }
                        else
                        {
                            if ((Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet) &&
                                (Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile))
                            {
                                Logging.Log(
                                    "QuestorUI",
                                    "You cant use both the CloseQuestorCMDUplinkIsboxerProfile and the CloseQuestorCMDUplinkIsboxerProfile setting, choose one",
                                    Logging.white);
                            }
                            else
                            {
                                if (Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile)
                                //if configured as true we will use the innerspace profile to restart this session
                                {
                                    //Logging.Log("Questor: We are in station: CloseQuestorCMDUplinkInnerspaceProfile is ["+ CloseQuestorCMDUplinkInnerspaceProfile.tostring() +"]");

                                    Logging.Log(
                                        "QuestorUI",
                                        "Starting a timer in the innerspace uplink to restart this innerspace profile session",
                                        Logging.white);
                                    LavishScript.ExecuteCommand("uplink exec Echo [${Time}] " +
                                                                Settings.Instance.CharacterName +
                                                                "'s Questor is starting a timedcommand to restart itself in a moment");
                                    LavishScript.ExecuteCommand(
                                        "uplink exec Echo [${Time}] timedcommand " + secRestart + " open \\\"${Game}\\\" \\\"${Profile}\\\"");
                                    LavishScript.ExecuteCommand(
                                        "uplink exec timedcommand " + secRestart + " open \\\"${Game}\\\" \\\"${Profile}\\\"");
                                    Logging.Log(
                                        "QuestorUI",
                                        "Done: quitting this session so the new innerspace session can take over",
                                        Logging.white);

                                    Cache.Instance.DirecteveDispose();
                                    Process.GetCurrentProcess().Kill();
                                    Environment.Exit(0);
                                    //Application.Exit();
                                    return;
                                }
                                else if (Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet)
                                //if configured as true we will use isboxer to restart this session
                                {
                                    //Logging.Log("Questor: We are in station: CloseQuestorCMDUplinkIsboxerProfile is ["+ CloseQuestorCMDUplinkIsboxerProfile.tostring() +"]");

                                    Logging.Log(
                                        "QuestorUI",
                                        "Starting a timer in the innerspace uplink to restart this isboxer character set",
                                        Logging.white);
                                    LavishScript.ExecuteCommand("uplink exec Echo [${Time}] " +
                                                                Settings.Instance.CharacterName +
                                                                "'s Questor is starting a timedcommand to restart itself in a moment");
                                    LavishScript.ExecuteCommand(
                                        "uplink exec Echo [${Time}] timedcommand " + secRestart + " runscript isboxer -launch \\\"${ISBoxerCharacterSet}\\\"");
                                    LavishScript.ExecuteCommand(
                                        "uplink timedcommand " + secRestart + " runscript isboxer -launch \\\"${ISBoxerCharacterSet}\\\"");
                                    Logging.Log(
                                        "QuestorUI",
                                        "Done: quitting this session so the new isboxer session can take over",
                                        Logging.white);

                                    Cache.Instance.DirecteveDispose();
                                    Process.GetCurrentProcess().Kill();
                                    Environment.Exit(0);
                                    //Application.Exit();
                                    return;
                                }
                                else if (Settings.Instance.CloseQuestorArbitraryOSCmd)
                                // will execute an arbitrary OS command through the IS Uplink
                                {
                                    Logging.Log(
                                        "QuestorUI",
                                        "Starting a timer in the innerspace uplink to execute an arbitrary OS command",
                                        Logging.white);
                                    LavishScript.ExecuteCommand("uplink exec Echo [${Time}] " +
                                                                Settings.Instance.CharacterName +
                                                                "'s Questor is starting a timedcommand to restart itself in a moment");
                                    LavishScript.ExecuteCommand(
                                        "uplink exec Echo [${Time}] timedcommand " + secRestart + " OSExecute " +
                                        Settings.Instance.CloseQuestorOSCmdContents.ToString(CultureInfo.InvariantCulture));
                                    LavishScript.ExecuteCommand(
                                        "uplink exec timedcommand " + secRestart + " OSExecute " +
                                        Settings.Instance.CloseQuestorOSCmdContents.ToString(CultureInfo.InvariantCulture));
                                    Logging.Log("QuestorUI", "Done: quitting this session", Logging.white);

                                    Cache.Instance.DirecteveDispose();
                                    Process.GetCurrentProcess().Kill();
                                    Environment.Exit(0);
                                    //Application.Exit();
                                    return;
                                }
                                else if (!Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile &&
                                         !Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet &&
                                         !Settings.Instance.CloseQuestorArbitraryOSCmd)
                                {
                                    Logging.Log(
                                        "QuestorUI",
                                        "CloseQuestorArbitraryOSCmd, CloseQuestorCMDUplinkInnerspaceProfile and CloseQuestorCMDUplinkIsboxerProfile all false",
                                        Logging.white);

                                    Cache.Instance.DirecteveDispose();
                                    Process.GetCurrentProcess().Kill();
                                    Environment.Exit(0);
                                    //Application.Exit();

                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        Logging.Log("QuestorUI",
                                    "CloseQuestor: We are configured to NOT use innerspace. useInnerspace = false",
                                    Logging.white);
                        Logging.Log("QuestorUI",
                                    "CloseQuestor: Currently the questor will exit (and not restart itself) in this configuration, this likely needs additional work to make questor reentrant so we can use a scheduled task?!",
                                    Logging.white);

                        Process.GetCurrentProcess().Kill();
                        Environment.Exit(0);
                        //Application.Exit();
                    }
                }
            }
            Logging.Log("QuestorUI",
                        "Autostart is false: Stopping EVE with quit command (if EVE is going to restart it will do so externally)",
                        Logging.white);
            Cache.Instance.DirecteveDispose();
            Process.GetCurrentProcess().Kill();
            Application.Exit();
            return;
        }

        private int SetAutoStart(string[] args)
        {
            bool value;
            if (args.Length != 2 || !bool.TryParse(args[1], out value))
            {
                Logging.Log("QuestorUI", "SetAutoStart true|false", Logging.white);
                return -1;
            }

            Settings.Instance.AutoStart = value;

            Logging.Log("QuestorUI", "AutoStart is turned " + (value ? "[on]" : "[off]"), Logging.white);
            return 0;
        }

        private int SetDisable3D(string[] args)
        {
            bool value;
            if (args.Length != 2 || !bool.TryParse(args[1], out value))
            {
                Logging.Log("QuestorUI", "SetDisable3D true|false", Logging.white);
                return -1;
            }

            Settings.Instance.Disable3D = value;

            Logging.Log("QuestorUI", "Disable3D is turned " + (value ? "[on]" : "[off]"), Logging.white);
            return 0;
        }

        private int SetExitWhenIdle(string[] args)
        {
            bool value;
            if (args.Length != 2 || !bool.TryParse(args[1], out value))
            {
                Logging.Log("QuestorUI", "SetExitWhenIdle true|false", Logging.white);
                Logging.Log("QuestorUI", "Note: AutoStart is automatically turned off when ExitWhenIdle is turned on", Logging.white);
                return -1;
            }

            Cache.Instance.ExitWhenIdle = value;

            Logging.Log("QuestorUI", "ExitWhenIdle is turned " + (value ? "[on]" : "[off]"), Logging.white);

            if (value && Settings.Instance.AutoStart)
            {
                Settings.Instance.AutoStart = false;
                Logging.Log("QuestorUI", "AutoStart is turned [off]", Logging.white);
            }
            return 0;
        }

        private int SetQuestorStatetoCloseQuestor(string[] args)
        {
            if (args.Length != 1)
            {
                Logging.Log("QuestorUI", "SetQuestorStatetoCloseQuestor - Changes the QuestorState to CloseQuestor which will GotoBase and then Exit", Logging.white);
                return -1;
            }

            _States.CurrentQuestorState = QuestorState.CloseQuestor;

            Logging.Log("QuestorUI", "QuestorState is now: CloseQuestor ", Logging.white);
            return 0;
        }

        private int SetQuestorStatetoIdle(string[] args)
        {
            if (args.Length != 1)
            {
                Logging.Log("QuestorUI", "SetQuestorStatetoIdle - Changes the QuestorState to Idle which will GotoBase and then Exit", Logging.white);
                return -1;
            }

            _States.CurrentQuestorState = QuestorState.Idle;

            Logging.Log("QuestorUI", "QuestorState is now: Idle ", Logging.white);
            return 0;
        }

        private void UpdateUiTick(object sender, EventArgs e)
        {
            //if (Settings.Instance.DebugUI) Logging.Log("QuestorUI", "UpdateUiTick", Logging.white);
            // The if's in here stop the UI from flickering
            string text = "Questor";
            if (_questor.CharacterName != string.Empty)
            {
                text = "Questor [" + _questor.CharacterName + "]";
            }
            if (Settings.Instance.CharacterName != string.Empty && Cache.Instance.Wealth > 10000000)
            {
                text = "Questor [" + _questor.CharacterName + "][" + String.Format("{0:0,0}", Cache.Instance.Wealth / 1000000) + "mil isk]";
            }

            if (Text != text)
                Text = text;

            lastSessionisreadyData.Text = "[" + Math.Round(DateTime.Now.Subtract(Cache.Instance.LastSessionIsReady).TotalSeconds, 0) + "] sec ago";
            LastFrameData.Text = "[" + Math.Round(DateTime.Now.Subtract(Cache.Instance.LastFrame).TotalSeconds, 0) + "] sec ago";
            lastInSpaceData.Text = "[" + Math.Round(DateTime.Now.Subtract(Cache.Instance.LastInSpace).TotalSeconds, 0) + "] sec ago";
            lastInStationData.Text = "[" + Math.Round(DateTime.Now.Subtract(Cache.Instance.LastInStation).TotalSeconds, 0) + "] sec ago";
            lastKnownGoodConnectedTimeData.Text = "[" + Math.Round(DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalSeconds, 0) + "] min ago";

            if (Cache.Instance.SessionState == "Quitting")
            {
                if (Cache.Instance.ReasonToStopQuestor == "A message from ccp indicated we were disconnected")
                {
                    CloseQuestor();
                }
            }

            RefreshInfoDisplayedInUI();

            //
            // Left Group
            //
            if ((string)QuestorStateComboBox.SelectedItem != _States.CurrentQuestorState.ToString() && !QuestorStateComboBox.DroppedDown)
            {
                QuestorStateComboBox.SelectedItem = _States.CurrentQuestorState.ToString();
            }

            if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
            {
                if ((string)BehaviorComboBox.SelectedItem != _States.CurrentCombatMissionBehaviorState.ToString() && !BehaviorComboBox.DroppedDown)
                    BehaviorComboBox.SelectedItem = _States.CurrentCombatMissionBehaviorState.ToString(); ;
            }

            if (_States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
            {
                if ((string)BehaviorComboBox.SelectedItem != _States.CurrentDedicatedBookmarkSalvagerBehaviorState.ToString() && !BehaviorComboBox.DroppedDown)
                    BehaviorComboBox.SelectedItem = _States.CurrentDedicatedBookmarkSalvagerBehaviorState.ToString();
            }

            if (_States.CurrentQuestorState == QuestorState.CombatHelperBehavior)
            {
                if ((string)BehaviorComboBox.SelectedItem != _States.CurrentCombatHelperBehaviorState.ToString() && !BehaviorComboBox.DroppedDown)
                    BehaviorComboBox.SelectedItem = _States.CurrentCombatHelperBehaviorState.ToString();
            }

            if (_States.CurrentQuestorState == QuestorState.DirectionalScannerBehavior)
            {
                if ((string)BehaviorComboBox.SelectedItem != _States.CurrentDirectionalScannerBehaviorState.ToString() && !BehaviorComboBox.DroppedDown)
                    BehaviorComboBox.SelectedItem = _States.CurrentDirectionalScannerBehaviorState.ToString();
            }

            if ((string)DamageTypeComboBox.SelectedItem != Cache.Instance.DamageType.ToString() && !DamageTypeComboBox.DroppedDown)
                DamageTypeComboBox.SelectedItem = Cache.Instance.DamageType.ToString();
            //
            // Middle group
            //
            if ((string)PanicStateComboBox.SelectedItem != _States.CurrentPanicState.ToString() && !PanicStateComboBox.DroppedDown)
                PanicStateComboBox.SelectedItem = _States.CurrentPanicState.ToString();

            if ((string)CombatStateComboBox.SelectedItem != _States.CurrentCombatState.ToString() && !CombatStateComboBox.DroppedDown)
                CombatStateComboBox.SelectedItem = _States.CurrentCombatState.ToString();

            if ((string)DronesStateComboBox.SelectedItem != _States.CurrentDroneState.ToString() && !DronesStateComboBox.DroppedDown)
                DronesStateComboBox.SelectedItem = _States.CurrentDroneState.ToString();

            if ((string)CleanupStateComboBox.SelectedItem != _States.CurrentCleanupState.ToString() && !CleanupStateComboBox.DroppedDown)
                CleanupStateComboBox.SelectedItem = _States.CurrentCleanupState.ToString();

            if ((string)LocalWatchStateComboBox.SelectedItem != _States.CurrentLocalWatchState.ToString() && !LocalWatchStateComboBox.DroppedDown)
                LocalWatchStateComboBox.SelectedItem = _States.CurrentLocalWatchState.ToString();

            if ((string)SalvageStateComboBox.SelectedItem != _States.CurrentSalvageState.ToString() && !SalvageStateComboBox.DroppedDown)
                SalvageStateComboBox.SelectedItem = _States.CurrentSalvageState.ToString();

            //
            // Right Group
            //
            if ((string)CombatMissionCtrlStateComboBox.SelectedItem != text && !CombatMissionCtrlStateComboBox.DroppedDown)
                CombatMissionCtrlStateComboBox.SelectedItem = text;

            if ((string)StorylineStateComboBox.SelectedItem != _States.CurrentStorylineState.ToString() && !StorylineStateComboBox.DroppedDown)
                StorylineStateComboBox.SelectedItem = _States.CurrentStorylineState.ToString();

            if ((string)ArmStateComboBox.SelectedItem != _States.CurrentArmState.ToString() && !ArmStateComboBox.DroppedDown)
                ArmStateComboBox.SelectedItem = _States.CurrentArmState.ToString();

            if ((string)UnloadStateComboBox.SelectedItem != _States.CurrentUnloadLootState.ToString() && !UnloadStateComboBox.DroppedDown)
                UnloadStateComboBox.SelectedItem = _States.CurrentUnloadLootState.ToString();

            if ((string)TravelerStateComboBox.SelectedItem != _States.CurrentTravelerState.ToString() && !TravelerStateComboBox.DroppedDown)
                TravelerStateComboBox.SelectedItem = _States.CurrentTravelerState.ToString();

            if ((string)AgentInteractionStateComboBox.SelectedItem != _States.CurrentAgentInteractionState.ToString() && !AgentInteractionStateComboBox.DroppedDown)
                AgentInteractionStateComboBox.SelectedItem = _States.CurrentAgentInteractionState.ToString();

            //if (Settings.Instance.CharacterMode.ToLower() == "dps" || Settings.Instance.CharacterMode.ToLower() == "combat missions")
            //{
            //
            //}

            if (AutoStartCheckBox.Checked != Settings.Instance.AutoStart)
            {
                AutoStartCheckBox.Checked = Settings.Instance.AutoStart;
            }

            if (PauseCheckBox.Checked != Cache.Instance.Paused)
                PauseCheckBox.Checked = Cache.Instance.Paused;

            if (Disable3DCheckBox.Checked != Settings.Instance.Disable3D)
                Disable3DCheckBox.Checked = Settings.Instance.Disable3D;

            if (Settings.Instance.WindowXPosition.HasValue)
            {
                Left = Settings.Instance.WindowXPosition.Value;
                Settings.Instance.WindowXPosition = null;
            }

            if (Settings.Instance.WindowYPosition.HasValue)
            {
                Top = Settings.Instance.WindowYPosition.Value;
                Settings.Instance.WindowYPosition = null;
            }

            if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission && Cache.Instance.CurrentPocketAction != null)
            {
                string newlblCurrentPocketActiontext = "[ " + Cache.Instance.CurrentPocketAction + " ] Action";
                if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
            }
            else if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Salvage ||
                     _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoSalvageBookmark ||
                     _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.SalvageNextPocket ||
                     _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.BeginAfterMissionSalvaging ||
                     _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.SalvageUseGate)
            {
                const string newlblCurrentPocketActiontext = "[ " + "After Mission Salvaging" + " ] ";
                if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
            }
            else
            {
                const string newlblCurrentPocketActiontext = "[ ]";
                if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
            }

            if (!String.IsNullOrEmpty(Cache.Instance.MissionName))
            {
                if (!String.IsNullOrEmpty(Settings.Instance.MissionsPath))
                {
                    if (File.Exists(Cache.Instance.missionXmlPath))
                    {
                        string newlblCurrentMissionInfotext = "[ " + Cache.Instance.MissionName + " ][ " +
                                                              Math.Round(
                                                                  DateTime.Now.Subtract(
                                                                      Statistics.Instance.StartedMission).TotalMinutes,
                                                                  0) + " min][ #" +
                                                              Statistics.Instance.MissionsThisSession + " ]";
                        if (lblCurrentMissionInfo.Text != newlblCurrentMissionInfotext)
                        {
                            lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                            buttonOpenMissionXML.Enabled = true;
                        }
                    }
                    else
                    {
                        string newlblCurrentMissionInfotext = "[ " + Cache.Instance.MissionName + " ][ " +
                                                              Math.Round(
                                                                  DateTime.Now.Subtract(
                                                                      Statistics.Instance.StartedMission).TotalMinutes,
                                                                  0) + " min][ #" +
                                                              Statistics.Instance.MissionsThisSession + " ]";
                        if (lblCurrentMissionInfo.Text != newlblCurrentMissionInfotext)
                        {
                            lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                            buttonOpenMissionXML.Enabled = false;
                        }
                    }
                }
            }
            else if (String.IsNullOrEmpty(Cache.Instance.MissionName))
            {
                lblCurrentMissionInfo.Text = "No Mission Selected Yet";
                buttonOpenMissionXML.Enabled = false;
            }
            else
            {
                //lblCurrentMissionInfo.Text = "No Mission XML exists for this mission";
                buttonOpenMissionXML.Enabled = false;
            }

            if (Settings.Instance.Defaultsettingsloaded)
            {
                buttonOpenCharacterXML.Enabled = false;
                buttonOpenSchedulesXML.Enabled = false;
                buttonQuestormanager.Enabled = false;
                buttonQuestorSettings.Enabled = false;
                buttonQuestorStatistics.Enabled = false;
            }
            else
            {
                if (Settings.Instance.CharacterXMLExists)
                {
                    buttonOpenCharacterXML.Enabled = true;
                    Settings.Instance.CharacterXMLExists = true;
                }
                else
                {
                    buttonOpenCharacterXML.Enabled = false;
                    Settings.Instance.CharacterXMLExists = false;
                }
                //
                // Does Schedules.xml exist in the directory where we started questor?
                //
                if (Settings.Instance.SchedulesXMLExists)
                {
                    buttonOpenCharacterXML.Enabled = true;
                    Settings.Instance.SchedulesXMLExists = true;
                }
                else
                {
                    buttonOpenSchedulesXML.Enabled = false;
                    Settings.Instance.SchedulesXMLExists = false;
                }
                //
                // Does QuestorStatistics.exe exist in the directory where we started questor?
                //
                if (Settings.Instance.QuestorStatisticsExists)
                {
                    buttonQuestorStatistics.Enabled = true;
                    Settings.Instance.QuestorStatisticsExists = true;
                }
                else
                {
                    buttonQuestorStatistics.Enabled = false;
                    Settings.Instance.QuestorStatisticsExists = false;
                }
                //
                // Does QuestorSettings.exe exist in the directory where we started questor?
                //
                if (Settings.Instance.QuestorSettingsExists)
                {
                    buttonQuestorSettings.Enabled = true;
                    Settings.Instance.QuestorSettingsExists = true;
                }
                else
                {
                    buttonQuestorSettings.Enabled = false;
                    Settings.Instance.QuestorSettingsExists = false;
                }
                //
                // Does Questormanager.exe exist in the directory where we started questor?
                //
                if (Settings.Instance.QuestorManagerExists)
                {
                    buttonQuestormanager.Enabled = true;
                    Settings.Instance.QuestorManagerExists = true;
                }
                else
                {
                    buttonQuestormanager.Enabled = false;
                    Settings.Instance.QuestorManagerExists = false;
                }
            }

            if (!String.IsNullOrEmpty(Cache.Instance.ExtConsole))
            {
                if (DateTime.Now > _nextConsoleLogRefresh)
                {
                    if (txtExtConsole.Lines.Count() >= Settings.Instance.MaxLineConsole)
                        txtExtConsole.Text = "";
                    txtExtConsole.AppendText(Cache.Instance.ExtConsole);
                    Cache.Instance.ExtConsole = null;
                    _nextConsoleLogRefresh = DateTime.Now.AddSeconds(1);
                }
            }

            int extraWaitSeconds = 0;
            if (!System.Diagnostics.Debugger.IsAttached) //do not restart due to no frames or Session.Isready aging if a debugger is attached until it reaches absurdity...
            {
                extraWaitSeconds = 60;
            }

            if (DateTime.Now.Subtract(Cache.Instance.LastFrame).TotalSeconds > (Time.Instance.NoFramesRestart_seconds + extraWaitSeconds) && DateTime.Now.Subtract(Program.AppStarted).TotalSeconds > 300)
            {
                if (DateTime.Now.Subtract(Cache.Instance.LastLogMessage).TotalSeconds > 30)
                {
                    Logging.Log("QuestorUI",
                                "The Last UI Frame Drawn by EVE was [" +
                                Math.Round(DateTime.Now.Subtract(Cache.Instance.LastFrame).TotalSeconds, 0) +
                                "] seconds ago! This is bad. - Exiting EVE", Logging.red);
                    //
                    // closing eve would be a very good idea here
                    //
                    CloseQuestor();
                    //Application.Exit();
                }
            }

            if (DateTime.Now.Subtract(Cache.Instance.LastSessionIsReady).TotalSeconds > (Time.Instance.NoSessionIsReadyRestart_seconds + extraWaitSeconds) &&
                    DateTime.Now.Subtract(Program.AppStarted).TotalSeconds > 300)
            {
                if (DateTime.Now.Subtract(Cache.Instance.LastLogMessage).TotalSeconds > 60)
                {
                    Logging.Log("QuestorUI",
                                "The Last Session.IsReady = true was [" +
                                Math.Round(DateTime.Now.Subtract(Cache.Instance.LastSessionIsReady).TotalSeconds, 0) +
                                "] seconds ago! This is bad. - Exiting EVE", Logging.red);
                    CloseQuestor();
                    //Application.Exit();
                }
            }
        }

        private void DamageTypeComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            Cache.Instance.DamageType = (DamageType)Enum.Parse(typeof(DamageType), DamageTypeComboBox.Text);
        }

        private void PauseCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Cache.Instance.Paused = PauseCheckBox.Checked;
        }

        private void Disable3DCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance.Disable3D = Disable3DCheckBox.Checked;
        }

        private void TxtComandKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                if (Settings.Instance.UseInnerspace)
                {
                    LavishScript.ExecuteCommand(txtComand.Text);
                }
            }
        }

        private void ChkShowConsoleCheckedChanged(object sender, EventArgs e)
        {
            var frmMain = new Form();
            Size = chkShowDetails.Checked ? new System.Drawing.Size(707, 434) : new System.Drawing.Size(362, 124);
        }

        private void FrmMainLoad(object sender, EventArgs e)
        {
        }

        private void DisableMouseWheel(object sender, MouseEventArgs e)
        {
            ((HandledMouseEventArgs)e).Handled = true;
        }

        //private void textBoxMaxRunTime_TextChanged(object sender, EventArgs e)
        //{
        //    int number2;
        //    if (int.TryParse(textBoxMaxRunTime.Text, out number2))
        //    {
        //Cache.Instance.MaxRuntime = number2;
        //    }
        //    else
        //    {
        //        textBoxMaxRunTime.Text = Cache.Instance.MaxRuntime.ToString();
        //    }
        //}

        //private void textBoxMaxRunTime_KeyPress(object sender, KeyPressEventArgs e)
        // {
        //     if (!char.IsControl(e.KeyChar)
        //         && !char.IsDigit(e.KeyChar))
        //     {
        //        e.Handled = true;
        //     }
        //}

        private void ButtonQuestorStatisticsClick(object sender, EventArgs e)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Process[] processes = System.Diagnostics.Process.GetProcessesByName("QuestorStatistics");

            if (processes.Length == 0)
            {
                // QuestorStatistics
                try
                {
                    System.Diagnostics.Process.Start(path + "\\QuestorStatistics.exe");
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    Logging.Log("QuestorUI", "QuestorStatistics could not be launched the error was: " + ex.Message, Logging.orange);
                }
            }
        }

        private void ButtonOpenLogDirectoryClick(object sender, EventArgs e)
        {
            //string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            System.Diagnostics.Process.Start(Settings.Instance.Logpath);
        }

        private void ButtonOpenMissionXmlClick(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Launching [" + Cache.Instance.missionXmlPath + "]", Logging.white);
            System.Diagnostics.Process.Start(Cache.Instance.missionXmlPath);
        }

        private void QuestorStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentQuestorState = (QuestorState)Enum.Parse(typeof(QuestorState), QuestorStateComboBox.Text);
            if (Settings.Instance.DebugStates) Logging.Log("QuestorUI", "QuestorState has been changed to [" + QuestorStateComboBox.Text + "]", Logging.white);
            PopulateBehaviorStateComboBox();
            PopulateMissionLists();
            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event
        }

        private void BehaviorComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            //Logging.Log("QuestorUI","BehaviorComboBoxChanged: Current QuestorState is: [" + _States.CurrentQuestorState + "]",Logging.white);
            if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
            {
                _States.CurrentCombatMissionBehaviorState =
                    (CombatMissionsBehaviorState)
                    Enum.Parse(typeof(CombatMissionsBehaviorState), BehaviorComboBox.Text);
            }
            if (_States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
            {
                _States.CurrentDedicatedBookmarkSalvagerBehaviorState =
                    (DedicatedBookmarkSalvagerBehaviorState)
                    Enum.Parse(typeof(DedicatedBookmarkSalvagerBehaviorState), BehaviorComboBox.Text);
            }
            if (_States.CurrentQuestorState == QuestorState.CombatHelperBehavior)
            {
                _States.CurrentCombatHelperBehaviorState =
                    (CombatHelperBehaviorState)
                    Enum.Parse(typeof(CombatHelperBehaviorState), BehaviorComboBox.Text);
            }
            if (_States.CurrentQuestorState == QuestorState.DirectionalScannerBehavior)
            {
                _States.CurrentDirectionalScannerBehaviorState =
                    (DirectionalScannerBehaviorState)
                    Enum.Parse(typeof(DirectionalScannerBehaviorState), BehaviorComboBox.Text);
            }

            try
            {
                AgentNameData.Text = Cache.Instance.CurrentAgent_text;
                AgentEffectiveStandingsData.Text = Cache.Instance.AgentEffectiveStandingtoMe_text;
                //DeclinedTimeData.Text = Cache.Instance.CurrentAgent.DeclineTimer;
                //
                // greylist info
                //
                MinAgentGreyListStandingsData.Text = Math.Round(Settings.Instance.MinAgentGreyListStandings, 2).ToString(CultureInfo.InvariantCulture);
                LastGreylistedMissionDeclinedData.Text = Cache.Instance.LastGreylistMissionDeclined;
                greylistedmissionsdeclineddata.Text = Cache.Instance.GreyListedMissionsDeclined.ToString(CultureInfo.InvariantCulture);
                //
                // blacklist info
                //
                MinAgentBlackListStandingsData.Text = Math.Round(Settings.Instance.MinAgentBlackListStandings, 2).ToString(CultureInfo.InvariantCulture);
                LastBlacklistedMissionDeclinedData.Text = Cache.Instance.LastBlacklistMissionDeclined;
                blacklistedmissionsdeclineddata.Text = Cache.Instance.BlackListedMissionsDeclined.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                //if we get an exception here ignore it as it shouldnt effect anything, theu GUI is only displaying data collected and processed elsewhere
            }
        }

        private void PanicStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentPanicState = (PanicState)Enum.Parse(typeof(PanicState), PanicStateComboBox.Text);
            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event
        }

        private void CombatStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentCombatState = (CombatState)Enum.Parse(typeof(CombatState), CombatStateComboBox.Text);
        }

        private void DronesStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentDroneState = (DroneState)Enum.Parse(typeof(DroneState), DronesStateComboBox.Text);
        }

        private void CleanupStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentCleanupState = (CleanupState)Enum.Parse(typeof(CleanupState), CleanupStateComboBox.Text);
        }

        private void LocalWatchStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentLocalWatchState = (LocalWatchState)Enum.Parse(typeof(LocalWatchState), LocalWatchStateComboBox.Text);
        }

        private void SalvageStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentSalvageState = (SalvageState)Enum.Parse(typeof(SalvageState), SalvageStateComboBox.Text);
        }

        private void StorylineStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentStorylineState = (StorylineState)Enum.Parse(typeof(StorylineState), StorylineStateComboBox.Text);
        }

        private void ArmStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentArmState = (ArmState)Enum.Parse(typeof(ArmState), ArmStateComboBox.Text);
        }

        private void UnloadStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentUnloadLootState = (UnloadLootState)Enum.Parse(typeof(UnloadLootState), UnloadStateComboBox.Text);
        }

        private void TravelerStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentTravelerState = (TravelerState)Enum.Parse(typeof(TravelerState), TravelerStateComboBox.Text);
        }

        private void AgentInteractionStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentAgentInteractionState = (AgentInteractionState)Enum.Parse(typeof(AgentInteractionState), AgentInteractionStateComboBox.Text);
        }

        private void TxtExtConsoleTextChanged(object sender, EventArgs e)
        {
        
        }

        private void AutoStartCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance.AutoStart = AutoStartCheckBox.Checked;
        }

        private void ButtonOpenCharacterXMLClick(object sender, EventArgs e)
        {
            if (File.Exists(Settings.Instance.SettingsPath))
            {
                Logging.Log("QuestorUI", "Launching [" + Settings.Instance.SettingsPath + "]", Logging.white);
                System.Diagnostics.Process.Start(Settings.Instance.SettingsPath);
            }
            else
            {
                Logging.Log("QuestorUI", "Unable to open [" + Settings.Instance.SettingsPath + "] file not found", Logging.orange);
            }
        }

        private void ButtonOpenSchedulesXMLClick(object sender, EventArgs e)
        {
            string schedulesXmlPath = Path.Combine(Settings.Instance.Path, "Schedules.xml");
            if (File.Exists(schedulesXmlPath))
            {
                Logging.Log("QuestorUI", "Launching [" + schedulesXmlPath + "]", Logging.white);
                System.Diagnostics.Process.Start(schedulesXmlPath);
            }
            else
            {
                Logging.Log("QuestorUI", "Unable to open [" + schedulesXmlPath + "] file not found", Logging.orange);
            }
        }

        private void ButtonQuestormanagerClick(object sender, EventArgs e)
        {
            string questorManagerPath = Path.Combine(Settings.Instance.Path, "QuestorManager.exe");
            if (File.Exists(questorManagerPath))
            {
                if (Settings.Instance.UseInnerspace)
                {
                    Logging.Log("QuestorUI", "Launching [ dotnet QuestorManager QuestorManager ]", Logging.white);
                    LavishScript.ExecuteCommand("dotnet QuestorManager QuestorManager");
                }
                else
                {
                    Logging.Log("QuestorUI", "Launching [ dotnet QuestorManager QuestorManager ] - fix me",
                                Logging.white);
                }
            }
            else
            {
                Logging.Log("QuestorUI", "Unable to launch QuestorManager from [" + questorManagerPath + "] file not found", Logging.orange);
            }
        }

        private void ButtonQuestorSettingsXMLClick(object sender, EventArgs e)
        {
            string questorSettingsPath = Path.Combine(Settings.Instance.Path, "QuestorSettings.exe");
            if (File.Exists(questorSettingsPath))
            {
                Logging.Log("QuestorUI", "Launching [" + Settings.Instance.Path + "\\QuestorSettings.exe" + "]",
                            Logging.white);
                System.Diagnostics.Process.Start(Settings.Instance.Path + "\\QuestorSettings.exe");
            }
            else
            {
                Logging.Log("QuestorUI", "Unable to launch QuestorSettings from [" + questorSettingsPath + "] file not found", Logging.orange);
            }
        }

        private void ExitWhenIdleCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Cache.Instance.ExitWhenIdle = ExitWhenIdleCheckBox.Checked;
            AutoStartCheckBox.Checked = false;
            Settings.Instance.AutoStart = false;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Cache.Instance.StopSessionAfterMissionNumber = (int)numericUpDown1.Value;
        }

        private void ReloadAll_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI","ReloadAll botton was pressed: changing QuestorState to ReloadAll- when done reloading it shoud return to the configured behavior",Logging.teal);
            _States.CurrentQuestorState = QuestorState.DebugReloadAll;
        }

        private void OutOfAmmo_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "OutOfAmmo botton was pressed: changing CombatState to OutOfAmmo", Logging.teal);
            _States.CurrentCombatState = CombatState.OutOfAmmo;
        }

        //private void comboBoxQuestorMode_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    Settings.Instance.CharacterMode = comboBoxQuestorMode.Text;
        //    // If you are at the controls enough to change modes... assume that panic needs to do nothing
        //    _questor.panicstatereset = true;
        //}

        //
        // all the GUI stoptime stuff needs new plumbing as a different feature... and the stoptime stuff likely needs
        // to be combined with the 'pause' and 'wait' stuff planned in station and in combat...
        //
        //
        //private void checkBoxStopTimeSpecified_CheckedChanged(object sender, EventArgs e)
        //{
        //    if (checkBoxStopTimeSpecified.Checked)
        //    {
        //        dateTimePickerStopTime.Enabled = false;
        //        Cache.Instance.StopTimeSpecified = false;
        //    }
        //    else
        //    {
        //        dateTimePickerStopTime.Enabled = true;
        //        Cache.Instance.StopTimeSpecified = true;
        //    }
        //
        //}
    }
}