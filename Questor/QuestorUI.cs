
namespace Questor
{
    using System;
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

        //private DateTime _lastlogmessage;

        public QuestorfrmMain()
        {
            InitializeComponent();
            _questor = new Questor(this);

            //Declaring the event: stolen from: http://www.dotnetspider.com/resources/30389-To-detect-when-system-gets.aspx
            //SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            
            //
            // populate combo boxes with the various states that are possible
            //
            // ComboxBoxes on main windows (at top)
            //
            
            foreach (string text in Enum.GetNames(typeof(DamageType)))
                DamageTypeComboBox.Items.Add(text);

            //
            // left column
            //
            
            foreach (string text in Enum.GetNames(typeof(QuestorState)))
                QuestorStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(CombatMissionsBehaviorState)))
                CombatMissionsBehaviorComboBox.Items.Add(text);

            //
            // middle column
            //
            
            foreach (string text in Enum.GetNames(typeof(PanicState)))
                PanicStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(CombatState)))
                CombatStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(DroneState)))
                DronesStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(CleanupState)))
                CleanupStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(LocalWatchState)))
                LocalWatchStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(SalvageState)))
                SalvageStateComboBox.Items.Add(text);

            //
            // right column
            //
            foreach (string text in Enum.GetNames(typeof(CombatMissionCtrlState)))
                CombatMissionCtrlStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(StorylineState)))
                StorylineStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(ArmState)))
                ArmStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(UnloadLootState)))
                UnloadStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(TravelerState)))
                TravelerStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(AgentInteractionState)))
                AgentInteractionStateComboBox.Items.Add(text);

            
            LavishScript.Commands.AddCommand("SetAutoStart", SetAutoStart);
            LavishScript.Commands.AddCommand("SetDisable3D", SetDisable3D);
            LavishScript.Commands.AddCommand("SetExitWhenIdle", SetExitWhenIdle);
            LavishScript.Commands.AddCommand("SetQuestorStatetoCloseQuestor", SetQuestorStatetoCloseQuestor);
            LavishScript.Commands.AddCommand("SetQuestorStatetoIdle", SetQuestorStatetoIdle);

            //Event definition
            //void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
            //    {
            //Use switch case to identify the session switch reason.
            //Code accordingly.
            //         switch (e.Reason)
            //            {
            //                case SessionSwitchReason.SessionLock:
            //                  //code here
            //                    break;
            //                case SessionSwitchReason.SessionLogoff:
            //                    //code
            //                    break;
            //                 default:
            //                     break;
            //             }
            //     }
        }

        private int SetAutoStart(string[] args)
        {
            bool value;
            if (args.Length != 2 || !bool.TryParse(args[1], out value))
            {
                Logging.Log("SetAutoStart true|false");
                return -1;
            }

            Settings.Instance.AutoStart = value;

            Logging.Log("AutoStart is turned " + (value ? "[on]" : "[off]"));
            return 0;
        }

        private int SetDisable3D(string[] args)
        {
            bool value;
            if (args.Length != 2 || !bool.TryParse(args[1], out value))
            {
                Logging.Log("SetDisable3D true|false");
                return -1;
            }

            Settings.Instance.Disable3D = value;

            Logging.Log("Disable3D is turned " + (value ? "[on]" : "[off]"));
            return 0;
        }

        private int SetExitWhenIdle(string[] args)
        {
            bool value;
            if (args.Length != 2 || !bool.TryParse(args[1], out value))
            {
                Logging.Log("SetExitWhenIdle true|false");
                Logging.Log("Note: AutoStart is automatically turned off when ExitWhenIdle is turned on");
                return -1;
            }

            //_questor.ExitWhenIdle = value;

            Logging.Log("ExitWhenIdle is turned " + (value ? "[on]" : "[off]"));

            if (value && Settings.Instance.AutoStart)
            {
                Settings.Instance.AutoStart = false;
                Logging.Log("AutoStart is turned [off]");
            }
            return 0;
        }

        private int SetQuestorStatetoCloseQuestor(string[] args)
        {
            if (args.Length != 1 )
            {
                Logging.Log("SetQuestorStatetoCloseQuestor - Changes the QuestorState to CloseQuestor which will GotoBase and then Exit");
                return -1;
            }

            _States.CurrentQuestorState = QuestorState.CloseQuestor;

            Logging.Log("QuestorState is now: CloseQuestor ");
            return 0;
        }

        private int SetQuestorStatetoIdle(string[] args)
        {
            if (args.Length != 1)
            {
                Logging.Log("SetQuestorStatetoIdle - Changes the QuestorState to Idle which will GotoBase and then Exit");
                return -1;
            }

            _States.CurrentQuestorState = QuestorState.Idle;

            Logging.Log("QuestorState is now: Idle ");
            return 0;
        }

        private void UpdateUiTick(object sender, EventArgs e)
        {
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

            //
            // Left Group
            //  
            text = _States.CurrentQuestorState.ToString();
            if ((string)QuestorStateComboBox.SelectedItem != text && !QuestorStateComboBox.DroppedDown)
                QuestorStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentCombatMissionBehaviorState.ToString();
            if ((string)CombatMissionsBehaviorComboBox.SelectedItem != text && !CombatMissionsBehaviorComboBox.DroppedDown)
                CombatMissionsBehaviorComboBox.SelectedItem = text;
            text = string.Empty;

            text = Cache.Instance.DamageType.ToString();
            if ((string)DamageTypeComboBox.SelectedItem != text && !DamageTypeComboBox.DroppedDown)
                DamageTypeComboBox.SelectedItem = text;
            text = string.Empty;
            //
            // Middle group
            //
            text = _States.CurrentPanicState.ToString();
            if ((string)PanicStateComboBox.SelectedItem != text && !PanicStateComboBox.DroppedDown)
                PanicStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentCombatState.ToString();
            if ((string)CombatStateComboBox.SelectedItem != text && !CombatStateComboBox.DroppedDown)
                CombatStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentDroneState.ToString();
            if ((string)DronesStateComboBox.SelectedItem != text && !DronesStateComboBox.DroppedDown)
                DronesStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentCleanupState.ToString();
            if ((string)CleanupStateComboBox.SelectedItem != text && !CleanupStateComboBox.DroppedDown)
                CleanupStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentLocalWatchState.ToString();
            if ((string)LocalWatchStateComboBox.SelectedItem != text && !LocalWatchStateComboBox.DroppedDown)
                LocalWatchStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentSalvageState.ToString();
            if ((string)SalvageStateComboBox.SelectedItem != text && !SalvageStateComboBox.DroppedDown)
                SalvageStateComboBox.SelectedItem = text;
            text = string.Empty;

            /*
            if (Cache.Instance.Mission.State != null)
            {
                text = Cache.Instance.Mission.State.ToString(CultureInfo.InvariantCulture);
                if ((string)MissionStateComboBox.SelectedItem != text && !MissionStateComboBox.DroppedDown)
                    MissionStateComboBox.SelectedItem = text;
            }
            */

            //
            // Right Group
            //
            text = _States.CurrentCombatMissionCtrlState.ToString();
            if ((string)CombatMissionCtrlStateComboBox.SelectedItem != text && !CombatMissionCtrlStateComboBox.DroppedDown)
                CombatMissionCtrlStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentStorylineState.ToString();
            if ((string)StorylineStateComboBox.SelectedItem != text && !StorylineStateComboBox.DroppedDown)
                StorylineStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentArmState.ToString();
            if ((string)ArmStateComboBox.SelectedItem != text && !ArmStateComboBox.DroppedDown)
                ArmStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentUnloadLootState.ToString();
            if ((string)UnloadStateComboBox.SelectedItem != text && !UnloadStateComboBox.DroppedDown)
                UnloadStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentTravelerState.ToString();
            if ((string)TravelerStateComboBox.SelectedItem != text && !TravelerStateComboBox.DroppedDown)
                TravelerStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = _States.CurrentAgentInteractionState.ToString();
            if ((string)AgentInteractionStateComboBox.SelectedItem != text && !AgentInteractionStateComboBox.DroppedDown)
                AgentInteractionStateComboBox.SelectedItem = text;
            text = string.Empty;

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
            else if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Salvage)
            {
                const string newlblCurrentPocketActiontext = "[ " + "Salvaging" + " ] ";
                if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
            }
            else
            {
                const string newlblCurrentPocketActiontext = "[ ]";
                if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
            }
            if (Cache.Instance.MissionName != string.Empty)
            {
                string missionXmlPath = Path.Combine(Settings.Instance.MissionsPath, Cache.Instance.MissionName + ".xml");
                if (File.Exists(missionXmlPath))
                {
                    string newlblCurrentMissionInfotext = "[ " + Cache.Instance.MissionName + " ][ " + Math.Round(DateTime.Now.Subtract(Statistics.Instance.StartedMission).TotalMinutes, 0) + " min][ #" + Statistics.Instance.MissionsThisSession + " ]";
                    if (lblCurrentMissionInfo.Text != newlblCurrentMissionInfotext)
                    {
                        lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                        buttonOpenMissionXML.Enabled = true;
                    }
                }
                else
                {
                    string newlblCurrentMissionInfotext = "[ " + Cache.Instance.MissionName + " ][ " + Math.Round(DateTime.Now.Subtract(Statistics.Instance.StartedMission).TotalMinutes, 0) + " min][ #" + Statistics.Instance.MissionsThisSession + " ]";
                    if (lblCurrentMissionInfo.Text != newlblCurrentMissionInfotext)
                    {
                        lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                        buttonOpenMissionXML.Enabled = false;
                    }
                    
                }
            }
            else if (Cache.Instance.MissionName == string.Empty)
            {
                lblCurrentMissionInfo.Text = "No Mission Selected Yet";
                buttonOpenMissionXML.Enabled = false;
            }
            else
            {
                //lblCurrentMissionInfo.Text = "No Mission XML exists for this mission";
                buttonOpenMissionXML.Enabled = false;
            }
            if (Cache.Instance.ExtConsole != null)
            {
                if (txtExtConsole.Lines.Count() >= Settings.Instance.MaxLineConsole)
                    txtExtConsole.Text = "";

                txtExtConsole.AppendText(Cache.Instance.ExtConsole);
                Cache.Instance.ExtConsole = null;
            }
            if (DateTime.Now.Subtract(Cache.Instance.LastFrame).TotalSeconds > 90 && DateTime.Now.Subtract(Program.AppStarted).TotalSeconds > 300)
            {
                if (DateTime.Now.Subtract(Cache.Instance.LastLogMessage).TotalSeconds > 60)
                {
                    Logging.Log("The Last UI Frame Drawn by EVE was more than 90 seconds ago! This is bad.");
                    //
                    // closing eve would be a very good idea here
                    //
                    Cache.Instance.LastLogMessage = DateTime.Now;
                }
            }
            if (DateTime.Now.Subtract(Cache.Instance.LastSessionIsReady).TotalSeconds > 90 && DateTime.Now.Subtract(Program.AppStarted).TotalSeconds > 300)
            {
                if (DateTime.Now.Subtract(Cache.Instance.LastLogMessage).TotalSeconds > 60)
                {
                    Logging.Log("The Last Session.IsReady = true was more than 90 seconds ago! This is bad.");
                    //
                    // closing eve would be a very good idea here
                    //
                    Cache.Instance.LastLogMessage = DateTime.Now;
                }
            }

            //
            // Targets Tab
            //
            //
            
            // Current Mission Action
            if (Cache.Instance.MissionName != string.Empty && Cache.Instance.CurrentPocketAction != null)
            {
                string newlblMissionActiontext = "[ " + Cache.Instance.CurrentPocketAction + " ]";
                if (CurrentMissionActionData.Text != newlblMissionActiontext)
                    CurrentMissionActionData.Text = newlblMissionActiontext;
            }
            else
            {
                CurrentMissionActionData.Text = "[ ]";
                buttonOpenMissionXML.Enabled = false;
            }
            
            //
            // Current Weapons Target
            //
            if (Cache.Instance.MissionName != string.Empty & (TargetingCache.CurrentWeaponsTarget != null))
            {
                string newlblCombatTargettext = "[ " + TargetingCache.CurrentWeaponsTarget.Name + " ][" + TargetingCache.CurrentWeaponsTarget.Id + "][" + Math.Round(TargetingCache.CurrentWeaponsTarget.Distance / 1000, 0) + "k][" + TargetingCache.CurrentWeaponsTarget.Health + "TH][" + TargetingCache.CurrentWeaponsTarget.ShieldPct + "S%][" + Math.Round(TargetingCache.CurrentWeaponsTarget.ArmorPct, 0) + "A%][" + Math.Round(TargetingCache.CurrentWeaponsTarget.StructurePct, 0) + "H%][" + TargetingCache.CurrentWeaponsTarget.TargetValue.GetValueOrDefault(-1) + "value]";
                if (CurrentWeaponsTargetData.Text != newlblCombatTargettext)
                    CurrentWeaponsTargetData.Text = newlblCombatTargettext;
            }
            else
            {
                CurrentWeaponsTargetData.Text = "[ ]";
            }
            //
            // Current Drones Target   
            //
            if (Cache.Instance.MissionName != string.Empty && (TargetingCache.CurrentDronesTarget != null))
            {
                string newlblDroneTargettext = "[ " + TargetingCache.CurrentDronesTarget.Name + " ][" + TargetingCache.CurrentDronesTarget.Id + "][" + Math.Round(TargetingCache.CurrentDronesTarget.Distance / 1000, 0) + "k][" + TargetingCache.CurrentDronesTarget.Health + "TH][" + TargetingCache.CurrentDronesTarget.ShieldPct + "S%][" + Math.Round(TargetingCache.CurrentDronesTarget.ArmorPct, 0) + "A%][" + Math.Round(TargetingCache.CurrentDronesTarget.StructurePct, 0) + "H%][" + TargetingCache.CurrentDronesTarget.TargetValue.GetValueOrDefault(-1) + "value]";
                if (CurrentDroneTargetData.Text != newlblDroneTargettext)
                    CurrentDroneTargetData.Text = newlblDroneTargettext;
            }
            else
            {
                CurrentDroneTargetData.Text = "[ ]";
            }

            //
            // Current Pocket Number   
            //
            if (Cache.Instance.MissionName != string.Empty)
            {
                string newlblPocketNumbertext = "[ " + Cache.Instance.PocketNumber + " ]";
                if (CurrentPocketNumberData.Text != newlblPocketNumbertext)
                    CurrentPocketNumberData.Text = newlblPocketNumbertext;
            }
            else
            {
                CurrentPocketNumberData.Text = "[ ]";
            }



            //
            // Current Priority Targets   
            //
            if (Cache.Instance.MissionName != string.Empty && !String.IsNullOrEmpty(Cache.Instance._priorityTargets_text))
            {
                if (CurrentPriorityTargetsData.Text != Cache.Instance._priorityTargets_text)
                    CurrentPriorityTargetsData.Text = Cache.Instance._priorityTargets_text;
            }
            else
            {
                CurrentPriorityTargetsData.Text = "[ ]";
            }
            //CurrentPriorityTargetsData

            
            //if (Cache.Instance.MaxRuntime > 0 && Cache.Instance.MaxRuntime != Int32.MaxValue) //if runtime is specified, overrides stop time
            //{
            //    if (DateTime.Now.Subtract(Program.startTime).TotalSeconds > 120)
            //    {
            //        if (Cache.Instance.MaxRuntime.ToString() != textBoxMaxRunTime.Text)
            //        {
            //            textBoxMaxRunTime.Text = Cache.Instance.MaxRuntime.ToString();
            //        }
            //    }
            //}
            //else
            //{
            //    textBoxMaxRunTime.Text = string.Empty;
            //}

            //if (Cache.Instance.StartTime != null)
            //{
            //    if (dateTimePickerStartTime.Value != Cache.Instance.StartTime)
            //    {
            //        dateTimePickerStartTime.Value = Cache.Instance.StartTime;
            //    }
            //}

            //if (Cache.Instance.StopTimeSpecified)
            // {
            //     if (dateTimePickerStopTime.Value == Cache.Instance.StartTime)
            //     {
            //         dateTimePickerStopTime.Value = Cache.Instance.StopTime;
            //     }
            // }

            //if (dateTimePickerStopTime.Value > Cache.Instance.StartTime.AddMinutes(5))
            // {
            //     Cache.Instance.StopTime = dateTimePickerStopTime.Value;
            // }
            // else
            // {
            //     dateTimePickerStopTime.Value = Cache.Instance.StartTime;
            // }
        }

        private void DamageTypeComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            Cache.Instance.DamageType = (DamageType)Enum.Parse(typeof(DamageType), DamageTypeComboBox.Text);
        }
        
        //private void MainBehaviorStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (_States.CurrentQuestorState != null && _States.CurrentQuestorState != QuestorState.Idle)
        //    {
        //        if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
        //        {
        //           _States.CurrentCombatMissionBehaviorState = (CombatMissionsBehaviorState)Enum.Parse(typeof (CombatMissionsBehaviorState), MainBehaviorComboBox.Text);
        //        }
        //    }
        //}

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
                LavishScript.ExecuteCommand(txtComand.Text);
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
        //        Cache.Instance.MaxRuntime = number2;
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
                    Logging.Log("QuestorStatistics could not be launched the error was: " + ex.Message);
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
            string missionXmlPath = Path.Combine(Settings.Instance.MissionsPath, Cache.Instance.MissionName + ".xml");
            Logging.Log("QuestorUI: Launching [" + missionXmlPath + "]");
            System.Diagnostics.Process.Start(missionXmlPath);
        }

        private void QuestorStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentQuestorState = (QuestorState)Enum.Parse(typeof(QuestorState), QuestorStateComboBox.Text);
            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event
        }

        private void CombatMissionsBehaviorComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentCombatMissionBehaviorState = (CombatMissionsBehaviorState)Enum.Parse(typeof(CombatMissionsBehaviorState), CombatMissionsBehaviorComboBox.Text);
            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event   
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
        
        private void TxtExtConsoleTextChanged(object sender, EventArgs e)
        {

        }

        private void AutoStartCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance.AutoStart = AutoStartCheckBox.Checked;
        }

        private void ButtonOpenCharacterXMLClick(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI: Launching [" + Settings.Instance.SettingsPath + "]");
            System.Diagnostics.Process.Start(Settings.Instance.SettingsPath);
        }

        private void ButtonOpenSchedulesXMLClick(object sender, EventArgs e)
        {
            string schedulesXmlPath = Path.Combine(Settings.Instance.Path, "Schedules.xml");
            Logging.Log("QuestorUI: Launching [" + schedulesXmlPath + "]");
            System.Diagnostics.Process.Start(schedulesXmlPath);
        }

        private void ButtonQuestormanagerClick(object sender, EventArgs e)
        {
            LavishScript.ExecuteCommand("dotnet QuestorManager QuestorManager");
        }

        //private void comboBoxQuestorMode_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    Settings.Instance.CharacterMode = comboBoxQuestorMode.Text;
        //    // If you are at the controls enough to change modes... assume that panic needs to do nothing
        //    _questor.panicstatereset = true;
        //}

        //
        // all the GUI stoptime stuff needs new plumbing as a different deature... and the stoptime stuff likely needs
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