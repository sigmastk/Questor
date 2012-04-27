namespace Questor
{
    //using global::Questor.Modules;
    //using System;
    //using System.Collections.Generic;
    
    //[Serializable()]

    partial class QuestorfrmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        //public class QuestorMode
        //{
        //    public string Name { get; set; }
        //    public string Value { get; set; }
        //}
        //public IList<QuestorMode> QuestorModeList = new List<QuestorMode>();

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoStartCheckBox = new System.Windows.Forms.CheckBox();
            this.tUpdateUI = new System.Windows.Forms.Timer(this.components);
            this.DamageTypeComboBox = new System.Windows.Forms.ComboBox();
            this.lblDamageType = new System.Windows.Forms.Label();
            this.lblQuestorState = new System.Windows.Forms.Label();
            this.QuestorStateComboBox = new System.Windows.Forms.ComboBox();
            this.StartButton = new System.Windows.Forms.Button();
            this.PauseCheckBox = new System.Windows.Forms.CheckBox();
            this.Disable3DCheckBox = new System.Windows.Forms.CheckBox();
            this.chkShowDetails = new System.Windows.Forms.CheckBox();
            this.lblMissionName = new System.Windows.Forms.Label();
            this.lblCurrentMissionInfo = new System.Windows.Forms.Label();
            this.lblPocketAction = new System.Windows.Forms.Label();
            this.lblCurrentPocketAction = new System.Windows.Forms.Label();
            this.buttonQuestormanager = new System.Windows.Forms.Button();
            this.buttonQuestorStatistics = new System.Windows.Forms.Button();
            this.buttonSettingsXML = new System.Windows.Forms.Button();
            this.buttonOpenMissionXML = new System.Windows.Forms.Button();
            this.buttonOpenLogDirectory = new System.Windows.Forms.Button();
            this.Console = new System.Windows.Forms.TabPage();
            this.txtComand = new System.Windows.Forms.TextBox();
            this.txtExtConsole = new System.Windows.Forms.TextBox();
            this.tabInterface1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label19 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label18 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.SalvageStateComboBox = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.LocalWatchStateComboBox = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.CleanupStateComboBox = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.DronesStateComboBox = new System.Windows.Forms.ComboBox();
            this.CombatStateComboBox = new System.Windows.Forms.ComboBox();
            this.PanicStateComboBox = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.AgentInteractionStateComboBox = new System.Windows.Forms.ComboBox();
            this.TravelerStateComboBox = new System.Windows.Forms.ComboBox();
            this.label17 = new System.Windows.Forms.Label();
            this.UnloadStateComboBox = new System.Windows.Forms.ComboBox();
            this.ArmStateComboBox = new System.Windows.Forms.ComboBox();
            this.MissionActionStateComboBox = new System.Windows.Forms.ComboBox();
            this.label14 = new System.Windows.Forms.Label();
            this.CombatMissionStateComboBox = new System.Windows.Forms.ComboBox();
            this.label13 = new System.Windows.Forms.Label();
            this.MissionStateComboBox = new System.Windows.Forms.ComboBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox4 = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBox3 = new System.Windows.Forms.ComboBox();
            this.CombatMissionsBehaviorComboBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.QuestorStateComboBox2 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Console.SuspendLayout();
            this.tabInterface1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // AutoStartCheckBox
            // 
            this.AutoStartCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
            this.AutoStartCheckBox.Location = new System.Drawing.Point(215, 4);
            this.AutoStartCheckBox.Name = "AutoStartCheckBox";
            this.AutoStartCheckBox.Size = new System.Drawing.Size(68, 23);
            this.AutoStartCheckBox.TabIndex = 2;
            this.AutoStartCheckBox.Text = "Autostart";
            this.AutoStartCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.AutoStartCheckBox.UseVisualStyleBackColor = true;
            this.AutoStartCheckBox.CheckedChanged += new System.EventHandler(this.AutoStartCheckBoxCheckedChanged);
            // 
            // tUpdateUI
            // 
            this.tUpdateUI.Enabled = true;
            this.tUpdateUI.Interval = 50;
            this.tUpdateUI.Tick += new System.EventHandler(this.UpdateUiTick);
            // 
            // DamageTypeComboBox
            // 
            this.DamageTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DamageTypeComboBox.FormattingEnabled = true;
            this.DamageTypeComboBox.Location = new System.Drawing.Point(79, 30);
            this.DamageTypeComboBox.Name = "DamageTypeComboBox";
            this.DamageTypeComboBox.Size = new System.Drawing.Size(130, 21);
            this.DamageTypeComboBox.TabIndex = 4;
            this.DamageTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.DamageTypeComboBoxSelectedIndexChanged);
            this.DamageTypeComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
            // 
            // lblDamageType
            // 
            this.lblDamageType.AutoSize = true;
            this.lblDamageType.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblDamageType.Location = new System.Drawing.Point(1, 34);
            this.lblDamageType.Name = "lblDamageType";
            this.lblDamageType.Size = new System.Drawing.Size(77, 13);
            this.lblDamageType.TabIndex = 90;
            this.lblDamageType.Text = "Damage Type:";
            // 
            // lblQuestorState
            // 
            this.lblQuestorState.AutoSize = true;
            this.lblQuestorState.Location = new System.Drawing.Point(3, 9);
            this.lblQuestorState.Name = "lblQuestorState";
            this.lblQuestorState.Size = new System.Drawing.Size(75, 13);
            this.lblQuestorState.TabIndex = 1;
            this.lblQuestorState.Text = "Questor State:";
            // 
            // QuestorStateComboBox
            // 
            this.QuestorStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.QuestorStateComboBox.FormattingEnabled = true;
            this.QuestorStateComboBox.Location = new System.Drawing.Point(79, 4);
            this.QuestorStateComboBox.Name = "QuestorStateComboBox";
            this.QuestorStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.QuestorStateComboBox.TabIndex = 1;
            this.QuestorStateComboBox.SelectedIndexChanged += new System.EventHandler(this.QuestorStateComboBoxSelectedIndexChanged);
            this.QuestorStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
            // 
            // StartButton
            // 
            this.StartButton.AutoSize = true;
            this.StartButton.Location = new System.Drawing.Point(285, 4);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(68, 23);
            this.StartButton.TabIndex = 3;
            this.StartButton.Text = "Start";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButtonClick);
            // 
            // PauseCheckBox
            // 
            this.PauseCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
            this.PauseCheckBox.Location = new System.Drawing.Point(285, 30);
            this.PauseCheckBox.Name = "PauseCheckBox";
            this.PauseCheckBox.Size = new System.Drawing.Size(68, 23);
            this.PauseCheckBox.TabIndex = 6;
            this.PauseCheckBox.Text = "Pause";
            this.PauseCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.PauseCheckBox.UseVisualStyleBackColor = true;
            this.PauseCheckBox.CheckedChanged += new System.EventHandler(this.PauseCheckBoxCheckedChanged);
            // 
            // Disable3DCheckBox
            // 
            this.Disable3DCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
            this.Disable3DCheckBox.Location = new System.Drawing.Point(215, 30);
            this.Disable3DCheckBox.Name = "Disable3DCheckBox";
            this.Disable3DCheckBox.Size = new System.Drawing.Size(68, 23);
            this.Disable3DCheckBox.TabIndex = 5;
            this.Disable3DCheckBox.Text = "Disable 3D";
            this.Disable3DCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.Disable3DCheckBox.UseVisualStyleBackColor = true;
            this.Disable3DCheckBox.CheckedChanged += new System.EventHandler(this.Disable3DCheckBoxCheckedChanged);
            // 
            // chkShowDetails
            // 
            this.chkShowDetails.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkShowDetails.Location = new System.Drawing.Point(270, 68);
            this.chkShowDetails.Name = "chkShowDetails";
            this.chkShowDetails.Size = new System.Drawing.Size(83, 23);
            this.chkShowDetails.TabIndex = 7;
            this.chkShowDetails.Text = "Show Details";
            this.chkShowDetails.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkShowDetails.UseVisualStyleBackColor = true;
            this.chkShowDetails.CheckedChanged += new System.EventHandler(this.ChkShowConsoleCheckedChanged);
            // 
            // lblMissionName
            // 
            this.lblMissionName.AutoSize = true;
            this.lblMissionName.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblMissionName.Location = new System.Drawing.Point(2, 55);
            this.lblMissionName.Name = "lblMissionName";
            this.lblMissionName.Size = new System.Drawing.Size(76, 13);
            this.lblMissionName.TabIndex = 92;
            this.lblMissionName.Text = "Mission Name:";
            // 
            // lblCurrentMissionInfo
            // 
            this.lblCurrentMissionInfo.Location = new System.Drawing.Point(76, 55);
            this.lblCurrentMissionInfo.MaximumSize = new System.Drawing.Size(250, 13);
            this.lblCurrentMissionInfo.MinimumSize = new System.Drawing.Size(275, 13);
            this.lblCurrentMissionInfo.Name = "lblCurrentMissionInfo";
            this.lblCurrentMissionInfo.Size = new System.Drawing.Size(275, 13);
            this.lblCurrentMissionInfo.TabIndex = 93;
            this.lblCurrentMissionInfo.Text = "[ No Mission Selected Yet ]";
            // 
            // lblPocketAction
            // 
            this.lblPocketAction.AutoSize = true;
            this.lblPocketAction.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblPocketAction.Location = new System.Drawing.Point(1, 73);
            this.lblPocketAction.Name = "lblPocketAction";
            this.lblPocketAction.Size = new System.Drawing.Size(77, 13);
            this.lblPocketAction.TabIndex = 94;
            this.lblPocketAction.Text = "PocketAction: ";
            // 
            // lblCurrentPocketAction
            // 
            this.lblCurrentPocketAction.Location = new System.Drawing.Point(76, 73);
            this.lblCurrentPocketAction.MaximumSize = new System.Drawing.Size(180, 15);
            this.lblCurrentPocketAction.MinimumSize = new System.Drawing.Size(180, 15);
            this.lblCurrentPocketAction.Name = "lblCurrentPocketAction";
            this.lblCurrentPocketAction.Size = new System.Drawing.Size(180, 15);
            this.lblCurrentPocketAction.TabIndex = 95;
            this.lblCurrentPocketAction.Text = "[  ]";
            // 
            // buttonQuestormanager
            // 
            this.buttonQuestormanager.Location = new System.Drawing.Point(371, 28);
            this.buttonQuestormanager.Name = "buttonQuestormanager";
            this.buttonQuestormanager.Size = new System.Drawing.Size(109, 23);
            this.buttonQuestormanager.TabIndex = 107;
            this.buttonQuestormanager.Text = "QuestorManager";
            this.buttonQuestormanager.UseVisualStyleBackColor = true;
            this.buttonQuestormanager.Click += new System.EventHandler(this.ButtonQuestormanagerClick);
            // 
            // buttonQuestorStatistics
            // 
            this.buttonQuestorStatistics.Location = new System.Drawing.Point(486, 29);
            this.buttonQuestorStatistics.Name = "buttonQuestorStatistics";
            this.buttonQuestorStatistics.Size = new System.Drawing.Size(109, 23);
            this.buttonQuestorStatistics.TabIndex = 108;
            this.buttonQuestorStatistics.Text = "QuestorStatistics";
            this.buttonQuestorStatistics.UseVisualStyleBackColor = true;
            this.buttonQuestorStatistics.Click += new System.EventHandler(this.ButtonQuestorStatisticsClick);
            // 
            // buttonSettingsXML
            // 
            this.buttonSettingsXML.Location = new System.Drawing.Point(601, 30);
            this.buttonSettingsXML.Name = "buttonSettingsXML";
            this.buttonSettingsXML.Size = new System.Drawing.Size(109, 23);
            this.buttonSettingsXML.TabIndex = 110;
            this.buttonSettingsXML.Text = "QuestorSettings";
            this.buttonSettingsXML.UseVisualStyleBackColor = true;
            // 
            // buttonOpenMissionXML
            // 
            this.buttonOpenMissionXML.Location = new System.Drawing.Point(486, 55);
            this.buttonOpenMissionXML.Name = "buttonOpenMissionXML";
            this.buttonOpenMissionXML.Size = new System.Drawing.Size(224, 23);
            this.buttonOpenMissionXML.TabIndex = 118;
            this.buttonOpenMissionXML.Text = "Open Current Mission XML";
            this.buttonOpenMissionXML.UseVisualStyleBackColor = true;
            this.buttonOpenMissionXML.Click += new System.EventHandler(this.ButtonOpenMissionXmlClick);
            // 
            // buttonOpenLogDirectory
            // 
            this.buttonOpenLogDirectory.Location = new System.Drawing.Point(371, 55);
            this.buttonOpenLogDirectory.Name = "buttonOpenLogDirectory";
            this.buttonOpenLogDirectory.Size = new System.Drawing.Size(109, 23);
            this.buttonOpenLogDirectory.TabIndex = 109;
            this.buttonOpenLogDirectory.Text = "Open Log Directory";
            this.buttonOpenLogDirectory.UseVisualStyleBackColor = true;
            this.buttonOpenLogDirectory.Click += new System.EventHandler(this.ButtonOpenLogDirectoryClick);
            // 
            // Console
            // 
            this.Console.Controls.Add(this.txtComand);
            this.Console.Controls.Add(this.txtExtConsole);
            this.Console.Location = new System.Drawing.Point(4, 22);
            this.Console.Name = "Console";
            this.Console.Padding = new System.Windows.Forms.Padding(3);
            this.Console.Size = new System.Drawing.Size(769, 276);
            this.Console.TabIndex = 0;
            this.Console.Text = "Console";
            this.Console.UseVisualStyleBackColor = true;
            // 
            // txtComand
            // 
            this.txtComand.Location = new System.Drawing.Point(3, 243);
            this.txtComand.Name = "txtComand";
            this.txtComand.Size = new System.Drawing.Size(760, 20);
            this.txtComand.TabIndex = 26;
            // 
            // txtExtConsole
            // 
            this.txtExtConsole.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtExtConsole.Location = new System.Drawing.Point(0, 3);
            this.txtExtConsole.Multiline = true;
            this.txtExtConsole.Name = "txtExtConsole";
            this.txtExtConsole.ReadOnly = true;
            this.txtExtConsole.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtExtConsole.Size = new System.Drawing.Size(773, 234);
            this.txtExtConsole.TabIndex = 25;
            this.txtExtConsole.TextChanged += new System.EventHandler(this.txtExtConsole_TextChanged);
            // 
            // tabInterface1
            // 
            this.tabInterface1.Controls.Add(this.Console);
            this.tabInterface1.Controls.Add(this.tabPage1);
            this.tabInterface1.Location = new System.Drawing.Point(4, 101);
            this.tabInterface1.Name = "tabInterface1";
            this.tabInterface1.SelectedIndex = 0;
            this.tabInterface1.Size = new System.Drawing.Size(777, 302);
            this.tabInterface1.TabIndex = 117;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label19);
            this.tabPage1.Controls.Add(this.label11);
            this.tabPage1.Controls.Add(this.panel2);
            this.tabPage1.Controls.Add(this.panel1);
            this.tabPage1.Controls.Add(this.comboBox4);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.comboBox3);
            this.tabPage1.Controls.Add(this.CombatMissionsBehaviorComboBox);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.QuestorStateComboBox2);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(769, 276);
            this.tabPage1.TabIndex = 1;
            this.tabPage1.Text = "States";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(310, 258);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(400, 13);
            this.label19.TabIndex = 168;
            this.label19.Text = "it is a very bad idea to change these states unless you understand what will happ" +
                "en";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(310, 3);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(400, 13);
            this.label11.TabIndex = 167;
            this.label11.Text = "it is a very bad idea to change these states unless you understand what will happ" +
                "en";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label18);
            this.panel2.Controls.Add(this.label16);
            this.panel2.Controls.Add(this.label15);
            this.panel2.Controls.Add(this.SalvageStateComboBox);
            this.panel2.Controls.Add(this.label10);
            this.panel2.Controls.Add(this.LocalWatchStateComboBox);
            this.panel2.Controls.Add(this.label9);
            this.panel2.Controls.Add(this.CleanupStateComboBox);
            this.panel2.Controls.Add(this.label8);
            this.panel2.Controls.Add(this.DronesStateComboBox);
            this.panel2.Controls.Add(this.CombatStateComboBox);
            this.panel2.Controls.Add(this.PanicStateComboBox);
            this.panel2.Location = new System.Drawing.Point(231, 19);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(257, 236);
            this.panel2.TabIndex = 154;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(11, 140);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(96, 13);
            this.label18.TabIndex = 166;
            this.label18.Text = "LocalWatch State:";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(30, 170);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(77, 13);
            this.label16.TabIndex = 165;
            this.label16.Text = "Salvage State:";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(42, 19);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(65, 13);
            this.label15.TabIndex = 164;
            this.label15.Text = "Panic State:";
            // 
            // SalvageStateComboBox
            // 
            this.SalvageStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SalvageStateComboBox.FormattingEnabled = true;
            this.SalvageStateComboBox.Location = new System.Drawing.Point(113, 167);
            this.SalvageStateComboBox.Name = "SalvageStateComboBox";
            this.SalvageStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.SalvageStateComboBox.TabIndex = 161;
            this.SalvageStateComboBox.SelectedIndexChanged += new System.EventHandler(this.SalvageStateComboBox_SelectedIndexChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(35, 79);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(72, 13);
            this.label10.TabIndex = 160;
            this.label10.Text = "Drones State:";
            // 
            // LocalWatchStateComboBox
            // 
            this.LocalWatchStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LocalWatchStateComboBox.FormattingEnabled = true;
            this.LocalWatchStateComboBox.Location = new System.Drawing.Point(113, 137);
            this.LocalWatchStateComboBox.Name = "LocalWatchStateComboBox";
            this.LocalWatchStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.LocalWatchStateComboBox.TabIndex = 159;
            this.LocalWatchStateComboBox.SelectedIndexChanged += new System.EventHandler(this.LocalWatchStateComboBox_SelectedIndexChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(33, 49);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(74, 13);
            this.label9.TabIndex = 158;
            this.label9.Text = "Combat State:";
            // 
            // CleanupStateComboBox
            // 
            this.CleanupStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CleanupStateComboBox.FormattingEnabled = true;
            this.CleanupStateComboBox.Location = new System.Drawing.Point(112, 107);
            this.CleanupStateComboBox.Name = "CleanupStateComboBox";
            this.CleanupStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.CleanupStateComboBox.TabIndex = 157;
            this.CleanupStateComboBox.SelectedIndexChanged += new System.EventHandler(this.CleanupStateComboBox_SelectedIndexChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(30, 110);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(77, 13);
            this.label8.TabIndex = 156;
            this.label8.Text = "Cleanup State:";
            // 
            // DronesStateComboBox
            // 
            this.DronesStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DronesStateComboBox.FormattingEnabled = true;
            this.DronesStateComboBox.Location = new System.Drawing.Point(113, 76);
            this.DronesStateComboBox.Name = "DronesStateComboBox";
            this.DronesStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.DronesStateComboBox.TabIndex = 155;
            this.DronesStateComboBox.SelectedIndexChanged += new System.EventHandler(this.DronesStateComboBox_SelectedIndexChanged);
            // 
            // CombatStateComboBox
            // 
            this.CombatStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CombatStateComboBox.FormattingEnabled = true;
            this.CombatStateComboBox.Location = new System.Drawing.Point(113, 46);
            this.CombatStateComboBox.Name = "CombatStateComboBox";
            this.CombatStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.CombatStateComboBox.TabIndex = 154;
            this.CombatStateComboBox.SelectedIndexChanged += new System.EventHandler(this.CombatStateComboBox_SelectedIndexChanged);
            // 
            // PanicStateComboBox
            // 
            this.PanicStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PanicStateComboBox.FormattingEnabled = true;
            this.PanicStateComboBox.Location = new System.Drawing.Point(113, 17);
            this.PanicStateComboBox.Name = "PanicStateComboBox";
            this.PanicStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.PanicStateComboBox.TabIndex = 153;
            this.PanicStateComboBox.SelectedIndexChanged += new System.EventHandler(this.PanicStateComboBox_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.AgentInteractionStateComboBox);
            this.panel1.Controls.Add(this.TravelerStateComboBox);
            this.panel1.Controls.Add(this.label17);
            this.panel1.Controls.Add(this.UnloadStateComboBox);
            this.panel1.Controls.Add(this.ArmStateComboBox);
            this.panel1.Controls.Add(this.MissionActionStateComboBox);
            this.panel1.Controls.Add(this.label14);
            this.panel1.Controls.Add(this.CombatMissionStateComboBox);
            this.panel1.Controls.Add(this.label13);
            this.panel1.Controls.Add(this.MissionStateComboBox);
            this.panel1.Controls.Add(this.label12);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Location = new System.Drawing.Point(494, 19);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(268, 236);
            this.panel1.TabIndex = 153;
            // 
            // AgentInteractionStateComboBox
            // 
            this.AgentInteractionStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.AgentInteractionStateComboBox.FormattingEnabled = true;
            this.AgentInteractionStateComboBox.Location = new System.Drawing.Point(131, 198);
            this.AgentInteractionStateComboBox.Name = "AgentInteractionStateComboBox";
            this.AgentInteractionStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.AgentInteractionStateComboBox.TabIndex = 167;
            // 
            // TravelerStateComboBox
            // 
            this.TravelerStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TravelerStateComboBox.FormattingEnabled = true;
            this.TravelerStateComboBox.Location = new System.Drawing.Point(131, 168);
            this.TravelerStateComboBox.Name = "TravelerStateComboBox";
            this.TravelerStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.TravelerStateComboBox.TabIndex = 166;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(48, 171);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(77, 13);
            this.label17.TabIndex = 165;
            this.label17.Text = "Traveler State:";
            // 
            // UnloadStateComboBox
            // 
            this.UnloadStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UnloadStateComboBox.FormattingEnabled = true;
            this.UnloadStateComboBox.Location = new System.Drawing.Point(131, 138);
            this.UnloadStateComboBox.Name = "UnloadStateComboBox";
            this.UnloadStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.UnloadStateComboBox.TabIndex = 164;
            // 
            // ArmStateComboBox
            // 
            this.ArmStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ArmStateComboBox.FormattingEnabled = true;
            this.ArmStateComboBox.Location = new System.Drawing.Point(131, 108);
            this.ArmStateComboBox.Name = "ArmStateComboBox";
            this.ArmStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.ArmStateComboBox.TabIndex = 163;
            // 
            // MissionActionStateComboBox
            // 
            this.MissionActionStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MissionActionStateComboBox.FormattingEnabled = true;
            this.MissionActionStateComboBox.Location = new System.Drawing.Point(131, 77);
            this.MissionActionStateComboBox.Name = "MissionActionStateComboBox";
            this.MissionActionStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.MissionActionStateComboBox.TabIndex = 162;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(52, 21);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(73, 13);
            this.label14.TabIndex = 161;
            this.label14.Text = "Mission State:";
            // 
            // CombatMissionStateComboBox
            // 
            this.CombatMissionStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CombatMissionStateComboBox.FormattingEnabled = true;
            this.CombatMissionStateComboBox.Location = new System.Drawing.Point(131, 47);
            this.CombatMissionStateComboBox.Name = "CombatMissionStateComboBox";
            this.CombatMissionStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.CombatMissionStateComboBox.TabIndex = 160;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(8, 50);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(109, 13);
            this.label13.TabIndex = 159;
            this.label13.Text = "CombatMission State:";
            // 
            // MissionStateComboBox
            // 
            this.MissionStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MissionStateComboBox.FormattingEnabled = true;
            this.MissionStateComboBox.Location = new System.Drawing.Point(131, 18);
            this.MissionStateComboBox.Name = "MissionStateComboBox";
            this.MissionStateComboBox.Size = new System.Drawing.Size(130, 21);
            this.MissionStateComboBox.TabIndex = 158;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(22, 80);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(103, 13);
            this.label12.TabIndex = 157;
            this.label12.Text = "MissionAction State:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 201);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(116, 13);
            this.label7.TabIndex = 156;
            this.label7.Text = "AgentInteraction State:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(52, 141);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(72, 13);
            this.label6.TabIndex = 155;
            this.label6.Text = "Unload State:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(69, 111);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 154;
            this.label3.Text = "Arm State:";
            // 
            // comboBox4
            // 
            this.comboBox4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox4.FormattingEnabled = true;
            this.comboBox4.Location = new System.Drawing.Point(9, 212);
            this.comboBox4.Name = "comboBox4";
            this.comboBox4.Size = new System.Drawing.Size(130, 21);
            this.comboBox4.TabIndex = 126;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 189);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(105, 13);
            this.label5.TabIndex = 125;
            this.label5.Text = "CombatHelper State:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 130);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(172, 13);
            this.label4.TabIndex = 124;
            this.label4.Text = "SalvageBookmarksBehavior State:";
            // 
            // comboBox3
            // 
            this.comboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox3.FormattingEnabled = true;
            this.comboBox3.Location = new System.Drawing.Point(9, 151);
            this.comboBox3.Name = "comboBox3";
            this.comboBox3.Size = new System.Drawing.Size(130, 21);
            this.comboBox3.TabIndex = 123;
            // 
            // CombatMissionsBehaviorComboBox
            // 
            this.CombatMissionsBehaviorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CombatMissionsBehaviorComboBox.FormattingEnabled = true;
            this.CombatMissionsBehaviorComboBox.Location = new System.Drawing.Point(9, 91);
            this.CombatMissionsBehaviorComboBox.Name = "CombatMissionsBehaviorComboBox";
            this.CombatMissionsBehaviorComboBox.Size = new System.Drawing.Size(130, 21);
            this.CombatMissionsBehaviorComboBox.TabIndex = 121;
            this.CombatMissionsBehaviorComboBox.SelectedIndexChanged += new System.EventHandler(this.CombatMissionsBehaviorComboBox_SelectedIndexChanged_1);
            this.CombatMissionsBehaviorComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(156, 13);
            this.label2.TabIndex = 120;
            this.label2.Text = "CombatMissionsBehavior State:";
            // 
            // QuestorStateComboBox2
            // 
            this.QuestorStateComboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.QuestorStateComboBox2.FormattingEnabled = true;
            this.QuestorStateComboBox2.Location = new System.Drawing.Point(9, 35);
            this.QuestorStateComboBox2.Name = "QuestorStateComboBox2";
            this.QuestorStateComboBox2.Size = new System.Drawing.Size(130, 21);
            this.QuestorStateComboBox2.TabIndex = 119;
            this.QuestorStateComboBox2.SelectedIndexChanged += new System.EventHandler(this.QuestorStateComboBox2_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 119;
            this.label1.Text = "Questor State:";
            // 
            // QuestorfrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.ClientSize = new System.Drawing.Size(782, 405);
            this.Controls.Add(this.buttonOpenMissionXML);
            this.Controls.Add(this.tabInterface1);
            this.Controls.Add(this.buttonSettingsXML);
            this.Controls.Add(this.buttonOpenLogDirectory);
            this.Controls.Add(this.buttonQuestorStatistics);
            this.Controls.Add(this.buttonQuestormanager);
            this.Controls.Add(this.lblCurrentPocketAction);
            this.Controls.Add(this.lblPocketAction);
            this.Controls.Add(this.lblCurrentMissionInfo);
            this.Controls.Add(this.lblMissionName);
            this.Controls.Add(this.chkShowDetails);
            this.Controls.Add(this.Disable3DCheckBox);
            this.Controls.Add(this.PauseCheckBox);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.QuestorStateComboBox);
            this.Controls.Add(this.lblQuestorState);
            this.Controls.Add(this.lblDamageType);
            this.Controls.Add(this.DamageTypeComboBox);
            this.Controls.Add(this.AutoStartCheckBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "QuestorfrmMain";
            this.Text = "Questor";
            this.Load += new System.EventHandler(this.FrmMainLoad);
            this.Console.ResumeLayout(false);
            this.Console.PerformLayout();
            this.tabInterface1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox AutoStartCheckBox;
        private System.Windows.Forms.Timer tUpdateUI;
        private System.Windows.Forms.ComboBox DamageTypeComboBox;
        private System.Windows.Forms.Label lblDamageType;
        private System.Windows.Forms.Label lblQuestorState;
        private System.Windows.Forms.ComboBox QuestorStateComboBox;
        private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.CheckBox PauseCheckBox;
        private System.Windows.Forms.CheckBox Disable3DCheckBox;
        //private System.Windows.Forms.Button chkTraveler;
        //private System.Windows.Forms.CheckBox Anomaly_chk;
        private System.Windows.Forms.CheckBox chkShowDetails;
        private System.Windows.Forms.Label lblMissionName;
        private System.Windows.Forms.Label lblCurrentMissionInfo;
        private System.Windows.Forms.Label lblPocketAction;
        private System.Windows.Forms.Label lblCurrentPocketAction;
        private System.Windows.Forms.Button buttonQuestormanager;
        private System.Windows.Forms.Button buttonQuestorStatistics;
        private System.Windows.Forms.Button buttonSettingsXML;
        private System.Windows.Forms.Button buttonOpenMissionXML;
        private System.Windows.Forms.Button buttonOpenLogDirectory;
        //private System.Windows.Forms.TabPage LiveScheduling;
        //private System.Windows.Forms.DateTimePicker dateTimePickerStopTime;
        //private System.Windows.Forms.Label lblStopTime;
        //private System.Windows.Forms.Label lblMaxRuntime2;
        //private System.Windows.Forms.TextBox textBoxMaxRunTime;
        //private System.Windows.Forms.DateTimePicker dateTimePickerStartTime;
        //private System.Windows.Forms.Label lblMaxRunTime1;
        //private System.Windows.Forms.Label lblStartTime1;
        private System.Windows.Forms.TabPage Console;
        private System.Windows.Forms.TextBox txtComand;
        private System.Windows.Forms.TextBox txtExtConsole;
        private System.Windows.Forms.TabControl tabInterface1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox SalvageStateComboBox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox LocalWatchStateComboBox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox CleanupStateComboBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox DronesStateComboBox;
        private System.Windows.Forms.ComboBox CombatStateComboBox;
        private System.Windows.Forms.ComboBox PanicStateComboBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox AgentInteractionStateComboBox;
        private System.Windows.Forms.ComboBox TravelerStateComboBox;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.ComboBox UnloadStateComboBox;
        private System.Windows.Forms.ComboBox ArmStateComboBox;
        private System.Windows.Forms.ComboBox MissionActionStateComboBox;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ComboBox CombatMissionStateComboBox;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox MissionStateComboBox;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBox4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBox3;
        private System.Windows.Forms.ComboBox CombatMissionsBehaviorComboBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox QuestorStateComboBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label11;
        //private System.Windows.Forms.Label lblQuestorMode;
        //public System.Windows.Forms.ComboBox comboBoxQuestorMode;
    }
}

