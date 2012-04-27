
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using LavishScriptAPI;
using Questor.Behaviors;
using Questor.Modules.Actions;
using Questor.Modules.Activities;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor
{
    public partial class QuestorfrmMain : Form
    {
        public Questor _questor;
        //public CombatMissionsBehavior _combatMissionsBehavior;
        //public Panic _panic;
        //public Combat _combat;
        //public Drones _drones;
        //public Cleanup _cleanup;
        //public LocalWatch _localwatch;
        //public Salvage _salvage;

        //private DateTime _lastlogmessage;

        public QuestorfrmMain()
        {
            InitializeComponent();
            //Declaring the event: stolen from: http://www.dotnetspider.com/resources/30389-To-detect-when-system-gets.aspx
            //SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            foreach (string text in Enum.GetNames(typeof(DamageType)))
                DamageTypeComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(QuestorState)))
                QuestorStateComboBox.Items.Add(text);
            
            foreach (string text in Enum.GetNames(typeof(QuestorState)))
                QuestorStateComboBox2.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(CombatMissionsBehaviorState)))
                CombatMissionsBehaviorComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(SalvageState)))
                SalvageStateComboBox.Items.Add(text);

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

            foreach (string text in Enum.GetNames(typeof(MissionState)))
                MissionStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(CombatMissionState)))
                CombatMissionStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(MissionActionState)))
                MissionActionStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(ArmState)))
                ArmStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(UnloadLootState)))
                UnloadStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(TravelerState)))
                TravelerStateComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(AgentInteractionState)))
                AgentInteractionStateComboBox.Items.Add(text);

            _questor = new Questor(this);
            //_combatMissionsBehavior = new CombatMissionsBehavior();
            //_panic = new Panic();
            //_combat = new Combat();
            //_drones = new Drones();
            //_cleanup = new Cleanup();
            //_localwatch = new LocalWatch();
            //_salvage = new Salvage();

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

        private void UpdateUiTick(object sender, EventArgs e)
        {
            // The if's in here stop the UI from flickering
            string text = "Questor";
            if (_questor.CharacterName != string.Empty)
            {
                text = "Questor [" + _questor.CharacterName + "]";
            }
            if (_questor.CharacterName != string.Empty && Cache.Instance.Wealth > 10000000)
            {
                text = "Questor [" + _questor.CharacterName + "][" + String.Format("{0:0,0}", Cache.Instance.Wealth / 1000000) + "mil isk]";
            }

            if (Text != text)
                Text = text;

            text = Questor.State.ToString();
            if ((string)QuestorStateComboBox.SelectedItem != text && !QuestorStateComboBox.DroppedDown)
                QuestorStateComboBox.SelectedItem = text;
            text = string.Empty;    

            text = Questor.State.ToString();
            if ((string)QuestorStateComboBox2.SelectedItem != text && !QuestorStateComboBox2.DroppedDown)
                QuestorStateComboBox2.SelectedItem = text;
            text = string.Empty;

            text = Cache.Instance.DamageType.ToString();
            if ((string)DamageTypeComboBox.SelectedItem != text && !DamageTypeComboBox.DroppedDown)
                DamageTypeComboBox.SelectedItem = text;
            text = string.Empty;

            text = CombatMissionsBehavior.State.ToString();
            if ((string)CombatMissionStateComboBox.SelectedItem != text && !CombatMissionStateComboBox.DroppedDown)
                CombatMissionStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = Panic.State.ToString();
            if ((string)PanicStateComboBox.SelectedItem != text && !PanicStateComboBox.DroppedDown)
                PanicStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = Combat.State.ToString();
            if ((string)CombatStateComboBox.SelectedItem != text && !CombatStateComboBox.DroppedDown)
                CombatStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = Drones.State.ToString();
            if ((string)DronesStateComboBox.SelectedItem != text && !DronesStateComboBox.DroppedDown)
                DronesStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = Cleanup.State.ToString();
            if ((string)CleanupStateComboBox.SelectedItem != text && !CleanupStateComboBox.DroppedDown)
                CleanupStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = LocalWatch.State.ToString();
            if ((string)LocalWatchStateComboBox.SelectedItem != text && !LocalWatchStateComboBox.DroppedDown)
                LocalWatchStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = Salvage.State.ToString();
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

            text = CombatMission.State.ToString();
            if ((string)CombatMissionStateComboBox.SelectedItem != text && !CombatMissionStateComboBox.DroppedDown)
                CombatMissionStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = MissionAction.State.ToString();
            if ((string)MissionActionStateComboBox.SelectedItem != text && !MissionActionStateComboBox.DroppedDown)
                MissionActionStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = Arm.State.ToString();
            if ((string)SalvageStateComboBox.SelectedItem != text && !SalvageStateComboBox.DroppedDown)
                SalvageStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = Arm.State.ToString();
            if ((string)ArmStateComboBox.SelectedItem != text && !ArmStateComboBox.DroppedDown)
                ArmStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = UnloadLoot.State.ToString();
            if ((string)UnloadStateComboBox.SelectedItem != text && !UnloadStateComboBox.DroppedDown)
                UnloadStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = Traveler.State.ToString();
            if ((string)TravelerStateComboBox.SelectedItem != text && !TravelerStateComboBox.DroppedDown)
                TravelerStateComboBox.SelectedItem = text;
            text = string.Empty;

            text = AgentInteraction.State.ToString();
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
                StartButton.Enabled = !Settings.Instance.AutoStart;
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
            //if (_questor.State == QuestorState.ExecuteMission)
            //{
            //    string newlblCurrentPocketActiontext = "[ " + Cache.Instance.CurrentPocketAction + " ] Action";
            //    if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
            //        lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
            //}
            //else if (_questor.State == QuestorState.Salvage)
            //{
            //    const string newlblCurrentPocketActiontext = "[ " + "Salvaging" + " ] ";
            //    if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
            //        lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
            //}
            else
            {
                const string newlblCurrentPocketActiontext = "[ " + "" + " ] ";
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
                        lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                    buttonOpenMissionXML.Enabled = true;
                }
                else
                {
                    string newlblCurrentMissionInfotext = "[ " + Cache.Instance.MissionName + " ][ " + Math.Round(DateTime.Now.Subtract(Statistics.Instance.StartedMission).TotalMinutes, 0) + " min][ #" + Statistics.Instance.MissionsThisSession + " ]";
                    if (lblCurrentMissionInfo.Text != newlblCurrentMissionInfotext)
                        lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                    buttonOpenMissionXML.Enabled = false;
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

        private void QuestorStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            Questor.State = (QuestorState)Enum.Parse(typeof(QuestorState), QuestorStateComboBox.Text);
            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event
        }

        private void AutoStartCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance.AutoStart = AutoStartCheckBox.Checked;
            StartButton.Enabled = !Settings.Instance.AutoStart;
        }

        private void PauseCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Cache.Instance.Paused = PauseCheckBox.Checked;
        }

        private void StartButtonClick(object sender, EventArgs e)
        {
            Questor.State = QuestorState.Start;
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
            Size = chkShowDetails.Checked ? new System.Drawing.Size(901, 406) : new System.Drawing.Size(362, 124);
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

        private void ButtonQuestormanagerClick(object sender, EventArgs e)
        {
            LavishScript.ExecuteCommand("dotnet QuestorManager QuestorManager");
        }

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
            System.Diagnostics.Process.Start(missionXmlPath);
        }

        private void QuestorStateComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Questor.State = (QuestorState)Enum.Parse(typeof(QuestorState), QuestorStateComboBox.Text);
            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event
        }

        private void CombatMissionsBehaviorComboBox_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            CombatMissionsBehavior.State = (CombatMissionsBehaviorState)Enum.Parse(typeof(CombatMissionsBehaviorState), CombatMissionsBehaviorComboBox.Text);
            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event   
        }

        private void PanicStateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Panic.State = (PanicState)Enum.Parse(typeof(PanicState), PanicStateComboBox.Text);
            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event   
       
        }

        private void CombatStateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Combat.State = (CombatState)Enum.Parse(typeof(CombatState), CombatStateComboBox.Text); 
        }

        private void DronesStateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Drones.State = (DroneState)Enum.Parse(typeof(DroneState), DronesStateComboBox.Text);
        }

        private void CleanupStateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Cleanup.State = (CleanupState)Enum.Parse(typeof(CleanupState), CleanupStateComboBox.Text);
        }

        private void LocalWatchStateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LocalWatch.State = (LocalWatchState)Enum.Parse(typeof(LocalWatchState), LocalWatchStateComboBox.Text);
        }

        private void SalvageStateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Salvage.State = (SalvageState)Enum.Parse(typeof(SalvageState), SalvageStateComboBox.Text);
        }

        private void txtExtConsole_TextChanged(object sender, EventArgs e)
        {

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