using System;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Questor.Modules;

namespace Questor
{
    using LavishScriptAPI;

    public partial class QuestorfrmMain : Form
    {
        public Questor _questor;
        private DateTime _lastlogmessage;

        public QuestorfrmMain()
        {
            InitializeComponent();
            //Declaring the event: stolen from: http://www.dotnetspider.com/resources/30389-To-detect-when-system-gets.aspx
            //SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            foreach (string text in Enum.GetNames(typeof(DamageType)))
                DamageTypeComboBox.Items.Add(text);

            foreach (string text in Enum.GetNames(typeof(QuestorState)))
                QuestorStateComboBox.Items.Add(text);

            _questor = new Questor(this);

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

            _questor.ExitWhenIdle = value;

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

            _questor.State = QuestorState.CloseQuestor;

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

            _questor.State = QuestorState.Idle;

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
            if (_questor.CharacterName != string.Empty && Cache.Instance.Wealth > 10000000)
            {
                text = "Questor [" + _questor.CharacterName + "][" + String.Format("{0:0,0}", Cache.Instance.Wealth / 1000000) + "mil isk]";
            }

            if (Text != text)
                Text = text;

            text = _questor.State.ToString();
            if ((string)QuestorStateComboBox.SelectedItem != text && !QuestorStateComboBox.DroppedDown)
                QuestorStateComboBox.SelectedItem = text;

            text = Cache.Instance.DamageType.ToString();
            if ((string)DamageTypeComboBox.SelectedItem != text && !DamageTypeComboBox.DroppedDown)
                DamageTypeComboBox.SelectedItem = text;

            if (AutoStartCheckBox.Checked != Settings.Instance.AutoStart)
            {
                AutoStartCheckBox.Checked = Settings.Instance.AutoStart;
                StartButton.Enabled = !Settings.Instance.AutoStart;
            }

            if (PauseCheckBox.Checked != _questor.Paused)
                PauseCheckBox.Checked = _questor.Paused;

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
            if (_questor.State == QuestorState.ExecuteMission)
            {
                string newlblCurrentPocketActiontext = "[ " + Cache.Instance.CurrentPocketAction + " ] Action";
                if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
            }
            else if (_questor.State == QuestorState.Salvage)
            {
                const string newlblCurrentPocketActiontext = "[ " + "Salvaging" + " ] ";
                if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
            }
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
            if (DateTime.Now.Subtract(_questor.LastFrame).TotalSeconds > 90 && DateTime.Now.Subtract(Program.AppStarted).TotalSeconds > 300)
            {
                if (DateTime.Now.Subtract(_lastlogmessage).TotalSeconds > 60)
                {
                    Logging.Log("The Last UI Frame Drawn by EVE was more than 90 seconds ago! This is bad.");
                    //
                    // closing eve would be a very good idea here
                    //
                    _lastlogmessage = DateTime.Now;
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
            Cache.Instance.DamageType = (DamageType) Enum.Parse(typeof (DamageType), DamageTypeComboBox.Text);
        }

        private void QuestorStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _questor.State = (QuestorState)Enum.Parse(typeof(QuestorState), QuestorStateComboBox.Text);
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
            _questor.Paused = PauseCheckBox.Checked;
        }

        private void StartButtonClick(object sender, EventArgs e)
        {
            _questor.State = QuestorState.Start;
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

        void DamageTypeComboBoxMouseWheel(object sender, MouseEventArgs e)
        {
            ((HandledMouseEventArgs)e).Handled = true;
        }
        void QuestorStateComboBoxMouseWheel(object sender, MouseEventArgs e)
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
                    Logging.Log("QuestorStatistics could not be launched the error was: " +  ex.Message); 
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
