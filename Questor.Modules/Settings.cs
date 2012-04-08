// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System.Globalization;
using InnerSpaceAPI;

namespace Questor.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml.Linq;
    using LavishScriptAPI;

    public class Settings
    {
        /// <summary>
        ///   Singleton implementation
        /// </summary>
        public static Settings Instance = new Settings();

        public string CharacterName;
        private DateTime _lastModifiedDate;
        private readonly Random _random = new Random();

        public int RandomNumber5To15()
        {
            return _random.Next(5, 15);
        }

        public int RandomNumber5To10()
        {
            return _random.Next(5, 10);
        }
        
        public int RandomNumber5To7()
        {
            return _random.Next(5, 7);
        }

        public int RandomNumber3To7()
        {
            return _random.Next(3, 7);
        }

        public int RandomNumber3To5()
        {
            return _random.Next(3, 5);
        }

        public Settings()
        {
            Ammo = new List<Ammo>();
            ItemsBlackList = new List<int>();
            WreckBlackList = new List<int>();
            AgentsList = new List<AgentsList>();
            FactionFitting = new List<FactionFitting>();
            MissionFitting = new List<MissionFitting>();
            Blacklist = new List<string>();
            FactionBlacklist = new List<string>();
            UseFittingManager = true;
            DefaultFitting = new FactionFitting();
        }

        public bool AtLoginScreen { get; set; }
        //
        // Debug Variables
        //
        public bool DebugStates { get; set; }
        public bool DebugPerformance { get; set; }

        //
        // Misc Settings
        //
        public string CharacterMode { get; set; }
        public bool AutoStart { get; set; }
        public bool Disable3D { get; set; }
        public int MinimumDelay { get; set; }
        public int RandomDelay { get; set; }

        //
        // Console Log Settings
        //
        public bool SaveConsoleLog { get; set; }
        public int MaxLineConsole { get; set; }
        
        //
        // Enable / Disable Major Features that dont have categories of their own below
        //
        public bool EnableStorylines { get; set; }
        public bool UseLocalWatch { get; set; }
        public bool UseFittingManager { get; set; }
        
        //
        // Agent and mission settings
        //
        public string MissionName { get; set; }
        public float MinStandings { get; set; }
        public string MissionsPath { get; set; }
        public bool LowSecMissions { get; set; }
        public bool WaitDecline { get; set; }
        public bool MultiAgentSupport { get; private set; }
        
        //
        // Local Watch settings - if enabled
        //
        public int LocalBadStandingPilotsToTolerate { get; set; }
        public double LocalBadStandingLevelToConsiderBad { get; set; }

        //
        // Invastion Settings
        //
        public int BattleshipInvasionLimit { get; set; }
        public int BattlecruiserInvasionLimit { get; set; }
        public int CruiserInvasionLimit { get; set; }
        public int FrigateInvasionLimit { get; set; }
        public int InvasionMinimumDelay { get; set; }
        public int InvasionRandomDelay { get; set; }

        //
        // Ship Names
        //
        public string CombatShipName { get; set; }
        public string SalvageShipName { get; set; }
        public string TransportShipName { get; set; }
        //public string MaterialShipName { get; set; }
        
        //
        // Storage location for loot, ammo, and bookmarks
        //
        public string LootHangar { get; set; }
        public string AmmoHangar { get; set; }
        public string BookmarkHangar { get; set; }
		public string LootContainer { get; set; }
        //
        // Salvage and Loot settings
        //
        public bool CreateSalvageBookmarks { get; set; }
        public string CreateSalvageBookmarksIn { get; set; }
        public bool SalvageMultpleMissionsinOnePass { get; set; }
        public string BookmarkPrefix { get; set; }
        public string UndockPrefix { get; set; }
        public int UndockDelay { get; set; }
        public int MinimumWreckCount { get; set; }
        public bool AfterMissionSalvaging { get; set; }
        public bool UnloadLootAtStation { get; set; }
        public bool UseGatesInSalvage { get; set; }
        public bool LootEverything { get; set; }
        public int ReserveCargoCapacity { get; set; }
        public int MaximumWreckTargets { get; set; }

        //
        // undocking settings
        //
        public string BookmarkWarpOut { get; set; }
        
        //
        // EVE Process Memory Ceiling and EVE wallet balance Change settings
        //
        public int Walletbalancechangelogoffdelay { get; set; }
        public string WalletbalancechangelogoffdelayLogofforExit { get; set; }
        public Int64 EVEProcessMemoryCeiling { get; set; }
        public string EVEProcessMemoryCeilingLogofforExit { get; set; }
        public bool CloseQuestorCMDUplinkInnerspaceProfile { get; set; }
        public bool CloseQuestorCMDUplinkIsboxerCharacterSet { get; set; }
        
        public string LavishIsBoxerCharacterSet { get; set; }
        public string LavishInnerspaceProfile { get; set; }
        public string LavishGame { get; set; }

        //public int missionbookmarktoagentloops { get; set; }  //not yet used - although it is likely a good ide to fix it so it is used - it would eliminate going back and fourth to the same mission over and over
        
        public List<int> ItemsBlackList { get; set; }
        public List<int> WreckBlackList { get; set; }
        public bool WreckBlackListSmallWrecks { get; set; }
        public bool WreckBlackListMediumWrecks { get; set; }

        public string Logpath { get; set; }

        public bool   SessionsLog { get; set; }
        public string SessionsLogPath { get; set; }
        public string SessionsLogFile { get; set; }
        public bool   ConsoleLog { get; set; }
        public string ConsoleLogPath { get; set; }
        public string ConsoleLogFile { get; set; }
        public bool   DroneStatsLog { get; set; }
        public string DroneStatsLogPath { get; set; }
        public string DroneStatslogFile { get; set; }
        public bool   WreckLootStatistics { get; set; }
        public string WreckLootStatisticsPath { get; set; }
        public string WreckLootStatisticsFile { get; set; }
        public bool   MissionStats1Log { get; set; }
        public string MissionStats1LogPath { get; set; }
        public string MissionStats1LogFile { get; set; }
        public bool   MissionStats2Log { get; set; }
        public string MissionStats2LogPath { get; set; }
        public string MissionStats2LogFile { get; set; }
        public bool   MissionStats3Log { get; set; }
        public string MissionStats3LogPath { get; set; }
        public string MissionStats3LogFile { get; set; }
        public bool   PocketStatistics { get; set; }
        public string PocketStatisticsPath { get; set; }
        public string PocketStatisticsFile { get; set; }
        public bool PocketStatsUseIndividualFilesPerPocket = true;

        //
        // Fitting Settings - if enabled
        //
        public List<FactionFitting> FactionFitting { get; private set; }
        public List<AgentsList> AgentsList { get; set; }
        public List<MissionFitting> MissionFitting { get; private set; }
        public FactionFitting DefaultFitting { get; set; }

        //
        // Weapon Settings
        //
        public bool DontShootFrigatesWithSiegeorAutoCannons { get; set; }
        public int WeaponGroupId { get; set; }
        public int MaximumHighValueTargets { get; set; }
        public int MaximumLowValueTargets { get; set; }
        public int MinimumAmmoCharges { get; set; }
        public List<Ammo> Ammo { get; private set; }

        //
        // Speed and Movement Settings
        //
        public bool SpeedTank { get; set; }
        public int OrbitDistance { get; set; }
        public int OptimalRange { get; set; }
        public int NosDistance { get; set; }
        public int MinimumPropulsionModuleDistance { get; set; }
        public int MinimumPropulsionModuleCapacitor { get; set; }
        //
        // Tank Settings
        //
        public int ActivateRepairModules { get; set; }
        public int DeactivateRepairModules { get; set; }
        //
        // Panic Settings
        //
        public int MinimumShieldPct { get; set; }
        public int MinimumArmorPct { get; set; }
        public int MinimumCapacitorPct { get; set; }
        public int SafeShieldPct { get; set; }
        public int SafeArmorPct { get; set; }
        public int SafeCapacitorPct { get; set; }
        
        public double IskPerLP { get; set; }

        //
        // Drone Settings
        //
        private bool _useDrones;
        public bool UseDrones
        {
            get
            {
                if (Cache.Instance.MissionUseDrones != null)
                    return (bool)Cache.Instance.MissionUseDrones;
                else return _useDrones;
            }
            set
            {
                _useDrones = value;
            }
        }
        public int DroneTypeId { get; set; }
        public int DroneControlRange { get; set; }
        public int DroneMinimumShieldPct { get; set; }
        public int DroneMinimumArmorPct { get; set; }
        public int DroneMinimumCapacitorPct { get; set; }
        public int DroneRecallShieldPct { get; set; }
        public int DroneRecallArmorPct { get; set; }
        public int DroneRecallCapacitorPct { get; set; }
        public int LongRangeDroneRecallShieldPct { get; set; }
        public int LongRangeDroneRecallArmorPct { get; set; }
        public int LongRangeDroneRecallCapacitorPct { get; set; }
        public bool DronesKillHighValueTargets {get; set;}

        public int MaterialsForWarOreID { get; set; }
        public int MaterialsForWarOreQty { get; set; }
        //
        // Mission Blacklist Settings 
        //
        public List<string> Blacklist { get; private set; }
        public List<string> FactionBlacklist { get; private set; }
        //
        // Questor GUI location settings
        //
        public int? WindowXPosition { get; set; }
        public int? WindowYPosition { get; set; }

        //
        // path information - used to load the XML and used in other modules
        //
        public string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public string CharacterNameXML { get; private set; }

        public string SettingsPath { get; private set; }
        public event EventHandler<EventArgs> SettingsLoaded;

        public void LoadSettings()
        {
            var repairstopwatch = new Stopwatch();
            Settings.Instance.CharacterNameXML = Cache.Instance.DirectEve.Me.Name;
            Settings.Instance.SettingsPath = System.IO.Path.Combine(Settings.Instance.Path, Cache.Instance.FilterPath(Settings.Instance.CharacterNameXML) + ".xml");
            bool reloadSettings = Settings.Instance.CharacterNameXML != Cache.Instance.DirectEve.Me.Name;
            if (File.Exists(Settings.Instance.SettingsPath))
                reloadSettings = _lastModifiedDate != File.GetLastWriteTime(Settings.Instance.SettingsPath);

            if (!reloadSettings)
                return;

            _lastModifiedDate = File.GetLastWriteTime(SettingsPath);

            if (!File.Exists(Settings.Instance.SettingsPath)) //if the settings file does not exist initialize these values. Should we not halt when missing the settings XML?
            {
                //
                // Misc Settings
                //
                CharacterMode = "Combat Missions";
                AutoStart = false;
                Disable3D = false;
                RandomDelay = 0;
                //
                // Console Log Settings
                //
                SaveConsoleLog = true;
                MaxLineConsole = 1000;
                //
                // Agent and mission settings
                //
                MissionsPath = System.IO.Path.Combine(Path, "Missions");
                MinStandings = 10;
                MinimumDelay = 0;
                MinStandings = 10;
                WaitDecline = false;
                //
                // Questor GUI Window Position
                //
                WindowXPosition = null;
                WindowYPosition = null;
                //
                // Salvage and loot settings
                //
                ReserveCargoCapacity = 0;
                MaximumWreckTargets = 0;
                
                //
                // Storage Location for Loot, Ammo, Bookmarks, default is local hangar
                //
                LootHangar = string.Empty;
                AmmoHangar = string.Empty;
                BookmarkHangar = string.Empty;
                LootContainer = string.Empty;

                MaximumHighValueTargets = 0;
                MaximumLowValueTargets = 0;

                //
                // Clear various lists
                //
                Ammo.Clear();
                ItemsBlackList.Clear();
                WreckBlackList.Clear();
                FactionFitting.Clear();
                AgentsList.Clear();
                MissionFitting.Clear();
                //
                // Weapon Settings
                //
                MinimumAmmoCharges = 0;
                WeaponGroupId = 0;
                //
                // Speed and movement settings
                //
                SpeedTank = false;
                OrbitDistance = 0;
                OptimalRange = 0;
                NosDistance = 38000;
                MinimumPropulsionModuleDistance = 5000;
                MinimumPropulsionModuleCapacitor = 0;
                //
                // Tank Settings
                //
                ActivateRepairModules = 0;
                DeactivateRepairModules = 0;
                //
                // Panic Settings
                //
                MinimumShieldPct = 0;
                MinimumArmorPct = 0;
                MinimumCapacitorPct = 0;
                SafeShieldPct = 0;
                SafeArmorPct = 0;
                SafeCapacitorPct = 0;
                //
                // Drone Settings
                //
                UseDrones = false;
                DroneTypeId = 0;
                DroneControlRange = 0;
                DroneMinimumShieldPct = 0;
                DroneMinimumArmorPct = 0;
                DroneMinimumCapacitorPct = 0;
                DroneRecallCapacitorPct = 0;
                LongRangeDroneRecallCapacitorPct = 0;
                DronesKillHighValueTargets = false;
                //
                // Clear the Blacklist
                //
                Blacklist.Clear();
                FactionBlacklist.Clear();

                MissionName = null;
                //missionbookmarktoagentloops = 0;
                return;
            }

            XElement xml = XDocument.Load(Settings.Instance.SettingsPath).Root;
            if (xml == null)
            {
               Logging.Log("Settings: unable to find [" + Settings.Instance.SettingsPath + "] FATAL ERROR - use the provided settings.xml to create that file.");
            }
            else
            {
               //
               // these are listed by feature and should likely be re-ordered to reflect that
               //

               //
               // Debug Settings
               //

               DebugStates = (bool?) xml.Element("debugStates") ?? false;
                  //enables more console logging having to do with the sub-states within each state
               DebugPerformance = (bool?) xml.Element("debugPerformance") ?? false;
                  //enabled more console logging having to do with the time it takes to execute each state
               //
               // Misc Settings
               //
               CharacterMode = (string) xml.Element("characterMode") ?? "Combat Missions"; //other option is "salvage"

               if (Settings.Instance.CharacterMode.ToLower() == "dps")
               {
                  Settings.Instance.CharacterMode = "Combat Missions";
               }
               AutoStart = (bool?) xml.Element("autoStart") ?? false; // auto Start enabled or disabled by default?
               SaveConsoleLog = (bool?) xml.Element("saveLog") ?? true; // save the console log to file
               MaxLineConsole = (int?) xml.Element("maxLineConsole") ?? 1000;
                  // maximum console log lines to show in the GUI      
               Disable3D = (bool?) xml.Element("disable3D") ?? false; // Disable3d graphics while in space
               RandomDelay = (int?) xml.Element("randomDelay") ?? 0;
               MinimumDelay = (int?) xml.Element("minimumDelay") ?? 0;
               //
               // Enable / Disable Major Features that dont have categories of their own below
               //
               UseFittingManager = (bool?) xml.Element("UseFittingManager") ?? true;
               EnableStorylines = (bool?) xml.Element("enableStorylines") ?? false;
               UseLocalWatch = (bool?) xml.Element("UseLocalWatch") ?? true;

               //
               // Agent Standings and Mission Settings
               //
               MinStandings = (float?) xml.Element("minStandings") ?? 10;
               WaitDecline = (bool?) xml.Element("waitDecline") ?? false;
               var missionsPath = (string) xml.Element("missionsPath");
               MissionsPath = !string.IsNullOrEmpty(missionsPath)
                                 ? System.IO.Path.Combine(Path, missionsPath)
                                 : System.IO.Path.Combine(Path, "Missions");
               LowSecMissions = (bool?) xml.Element("LowSecMissions") ?? false;
               MaterialsForWarOreID = (int?) xml.Element("MaterialsForWarOreID") ?? 20;
               MaterialsForWarOreQty = (int?) xml.Element("MaterialsForWarOreQty") ?? 8000;

               //
               // Local Watch Settings - if enabled
               //
               LocalBadStandingPilotsToTolerate = (int?) xml.Element("LocalBadStandingPilotsToTolerate") ?? 1;
               LocalBadStandingLevelToConsiderBad = (double?) xml.Element("LocalBadStandingLevelToConsiderBad") ?? -0.1;
               //
               // Invasion Settings
               //
               BattleshipInvasionLimit = (int?) xml.Element("battleshipInvasionLimit") ?? 0;
                  // if this number of battleships lands on grid while in a mission we will enter panic
               BattlecruiserInvasionLimit = (int?) xml.Element("battlecruiserInvasionLimit") ?? 0;
                  // if this number of battlecruisers lands on grid while in a mission we will enter panic
               CruiserInvasionLimit = (int?) xml.Element("cruiserInvasionLimit") ?? 0;
                  // if this number of cruisers lands on grid while in a mission we will enter panic
               FrigateInvasionLimit = (int?) xml.Element("frigateInvasionLimit") ?? 0;
                  // if this number of frigates lands on grid while in a mission we will enter panic
               InvasionRandomDelay = (int?) xml.Element("invasionRandomDelay") ?? 0; // random relay to stay docked
               InvasionMinimumDelay = (int?) xml.Element("invasionMinimumDelay") ?? 0; // minimum delay to stay docked

               //
               // Value - Used in calculations
               //
               IskPerLP = (double?) xml.Element("IskPerLP") ?? 600; //used in value calculations

               //
               // Undock settings
               //
               UndockDelay = (int?) xml.Element("undockdelay") ?? 10; //Delay when undocking - not in use
               UndockPrefix = (string) xml.Element("undockprefix") ?? "Insta";
                  //Undock bookmark prefix - used by traveler - not in use
               BookmarkWarpOut = (string) xml.Element("bookmarkWarpOut") ?? "";

               //
               // Location of the Questor GUI on startup (default is off the screen)
               //
               WindowXPosition = (int?) xml.Element("windowXPosition") ?? 1600;
                  //windows position (needs to be changed, default is off screen)
               WindowYPosition = (int?) xml.Element("windowYPosition") ?? 1050;
                  //windows position (needs to be changed, default is off screen)

               //
               // Ship Names
               //
               CombatShipName = (string) xml.Element("combatShipName") ?? "";
               SalvageShipName = (string) xml.Element("salvageShipName") ?? "";
               TransportShipName = (string) xml.Element("transportShipName") ?? "";
               //MaterialShipName = (string)xml.Element("materialShipName") ?? "";

               //
               // Storage Location for Loot, Ammo, Bookmarks
               //
               LootHangar = (string) xml.Element("lootHangar");
               AmmoHangar = (string) xml.Element("ammoHangar");
               BookmarkHangar = (string) xml.Element("bookmarkHangar");
               LootContainer = (string) xml.Element("lootContainer");

               //
               // Loot and Salvage Settings
               //
               LootEverything = (bool?) xml.Element("lootEverything") ?? true;
               UseGatesInSalvage = (bool?) xml.Element("useGatesInSalvage") ?? false;
                  // if our mission does not despawn (likely someone in the mission looting our stuff?) use the gates when salvaging to get to our bookmarks
               CreateSalvageBookmarks = (bool?) xml.Element("createSalvageBookmarks") ?? false;
               CreateSalvageBookmarksIn = (string) xml.Element("createSalvageBookmarksIn") ?? "Player";
                  //other setting is "Corp"
               BookmarkPrefix = (string) xml.Element("bookmarkPrefix") ?? "Salvage:";
               MinimumWreckCount = (int?) xml.Element("minimumWreckCount") ?? 1;
               AfterMissionSalvaging = (bool?) xml.Element("afterMissionSalvaging") ?? false;
               SalvageMultpleMissionsinOnePass = (bool?) xml.Element("salvageMultpleMissionsinOnePass") ?? false;
               UnloadLootAtStation = (bool?) xml.Element("unloadLootAtStation") ?? false;
               ReserveCargoCapacity = (int?) xml.Element("reserveCargoCapacity") ?? 0;
               MaximumWreckTargets = (int?) xml.Element("maximumWreckTargets") ?? 0;
               WreckBlackListSmallWrecks = (bool?) xml.Element("WreckBlackListSmallWrecks") ?? false;
               WreckBlackListMediumWrecks = (bool?) xml.Element("WreckBlackListMediumWrecks") ?? false;

               //
               // at what memory usage do we need to restart this session?
               //
               EVEProcessMemoryCeiling = (int?) xml.Element("EVEProcessMemoryCeiling") ?? 900;
               EVEProcessMemoryCeilingLogofforExit = (string) xml.Element("EVEProcessMemoryCeilingLogofforExit") ??
                                                     "exit";

               CloseQuestorCMDUplinkInnerspaceProfile = (bool?) xml.Element("CloseQuestorCMDUplinkInnerspaceProfile") ??
                                                        true;
               CloseQuestorCMDUplinkIsboxerCharacterSet =
                  (bool?) xml.Element("CloseQuestorCMDUplinkIsboxerCharacterSet") ?? false;

               Walletbalancechangelogoffdelay = (int?) xml.Element("walletbalancechangelogoffdelay") ?? 30;
               WalletbalancechangelogoffdelayLogofforExit =
                  (string) xml.Element("walletbalancechangelogoffdelayLogofforExit") ?? "exit";


               LavishScriptObject lavishsriptObject = LavishScript.Objects.GetObject("LavishScript");
               if (lavishsriptObject == null)
               {
                  InnerSpace.Echo("Testing: object not found");
               }
               else
               {
                  /* "LavishScript" object's ToString value is its version number, which follows the form of a typical float */
                  var version = lavishsriptObject.GetValue<float>();
                  //var TestISVariable = "Game"
                  //LavishIsBoxerCharacterSet = LavishsriptObject.
                  Logging.Log("Testing: LavishScript Version " + version.ToString(CultureInfo.InvariantCulture));
               }


               //
               // Enable / Disable the different types of logging that are available
               //
               SessionsLog = (bool?) xml.Element("SessionsLog") ?? true;
               DroneStatsLog = (bool?) xml.Element("DroneStatsLog") ?? true;
               WreckLootStatistics = (bool?) xml.Element("WreckLootStatistics") ?? true;
               MissionStats1Log = (bool?) xml.Element("MissionStats1Log") ?? true;
               MissionStats2Log = (bool?) xml.Element("MissionStats2Log") ?? true;
               MissionStats3Log = (bool?) xml.Element("MissionStats3Log") ?? true;
               PocketStatistics = (bool?) xml.Element("PocketStatistics") ?? true;
               PocketStatsUseIndividualFilesPerPocket = (bool?) xml.Element("PocketStatsUseIndividualFilesPerPocket") ??
                                                        true;

               //
               // Weapon and targeting Settings
               //
               WeaponGroupId = (int?) xml.Element("weaponGroupId") ?? 0;
               DontShootFrigatesWithSiegeorAutoCannons =
                  (bool?) xml.Element("DontShootFrigatesWithSiegeorAutoCannons") ?? false;
               MaximumHighValueTargets = (int?) xml.Element("maximumHighValueTargets") ?? 2;
               MaximumLowValueTargets = (int?) xml.Element("maximumLowValueTargets") ?? 2;

               //
               // Speed and Movement Settings
               //
               SpeedTank = (bool?) xml.Element("speedTank") ?? false;
               OrbitDistance = (int?) xml.Element("orbitDistance") ?? 0;
               OptimalRange = (int?) xml.Element("optimalRange") ?? 0;
               NosDistance = (int?) xml.Element("NosDistance") ?? 38000;
               MinimumPropulsionModuleDistance = (int?) xml.Element("minimumPropulsionModuleDistance") ?? 5000;
               MinimumPropulsionModuleCapacitor = (int?) xml.Element("minimumPropulsionModuleCapacitor") ?? 0;

               //
               // Tanking Settings
               //
               ActivateRepairModules = (int?) xml.Element("activateRepairModules") ?? 65;
               DeactivateRepairModules = (int?) xml.Element("deactivateRepairModules") ?? 95;

               //
               // Panic Settings
               //
               MinimumShieldPct = (int?) xml.Element("minimumShieldPct") ?? 100;
               MinimumArmorPct = (int?) xml.Element("minimumArmorPct") ?? 100;
               MinimumCapacitorPct = (int?) xml.Element("minimumCapacitorPct") ?? 50;
               SafeShieldPct = (int?) xml.Element("safeShieldPct") ?? 0;
               SafeArmorPct = (int?) xml.Element("safeArmorPct") ?? 0;
               SafeCapacitorPct = (int?) xml.Element("safeCapacitorPct") ?? 0;

               //
               // Drone Settings
               //
               UseDrones = (bool?) xml.Element("useDrones") ?? true;
               DroneTypeId = (int?) xml.Element("droneTypeId") ?? 0;
               DroneControlRange = (int?) xml.Element("droneControlRange") ?? 0;
               DroneMinimumShieldPct = (int?) xml.Element("droneMinimumShieldPct") ?? 50;
               DroneMinimumArmorPct = (int?) xml.Element("droneMinimumArmorPct") ?? 50;
               DroneMinimumCapacitorPct = (int?) xml.Element("droneMinimumCapacitorPct") ?? 0;
               DroneRecallShieldPct = (int?) xml.Element("droneRecallShieldPct") ?? 0;
               DroneRecallArmorPct = (int?) xml.Element("droneRecallArmorPct") ?? 0;
               DroneRecallCapacitorPct = (int?) xml.Element("droneRecallCapacitorPct") ?? 0;
               LongRangeDroneRecallShieldPct = (int?) xml.Element("longRangeDroneRecallShieldPct") ?? 0;
               LongRangeDroneRecallArmorPct = (int?) xml.Element("longRangeDroneRecallArmorPct") ?? 0;
               LongRangeDroneRecallCapacitorPct = (int?) xml.Element("longRangeDroneRecallCapacitorPct") ?? 0;
               DronesKillHighValueTargets = (bool?) xml.Element("dronesKillHighValueTargets") ?? false;

               //
               // Ammo settings
               //
               Ammo.Clear();
               XElement ammoTypes = xml.Element("ammoTypes");
               if (ammoTypes != null)
                  foreach (XElement ammo in ammoTypes.Elements("ammoType"))
                     Ammo.Add(new Ammo(ammo));

               MinimumAmmoCharges = (int?) xml.Element("minimumAmmoCharges") ?? 0;

               //
               // List of Agents we should use
               //
               AgentsList.Clear();
               XElement agentList = xml.Element("agentsList");
               if (agentList != null)
               {
                  if (agentList.HasElements)
                  {

                     int i = 0;
                     foreach (XElement agent in agentList.Elements("agentList"))
                     {
                        AgentsList.Add(new AgentsList(agent));
                        i++;
                     }
                     if (i >= 2)
                     {
                        MultiAgentSupport = true;
                        Logging.Log(
                           "Settings: Found more than one agent in your character XML: MultiAgentSupport is true");
                     }
                     else
                     {
                        MultiAgentSupport = false;
                        Logging.Log("Settings: Found only one agent in your character XML: MultiAgentSupport is false");
                     }
                  }
                  else
                  {
                     Logging.Log("Settings: agentList exists in your characters config but no agents were listed.");
                  }
               }
               else
                  Logging.Log("Settings: Error! No Agents List specified.");

               //
               // Fittings chosen based on the faction of the mission
               //
               FactionFitting.Clear();
               XElement factionFittings = xml.Element("factionfittings");
               if (UseFittingManager) //no need to look for or load these settings if FittingManager is disabled
               {
                  if (factionFittings != null)
                  {
                     foreach (XElement factionfitting in factionFittings.Elements("factionfitting"))
                        FactionFitting.Add(new FactionFitting(factionfitting));
                     if (FactionFitting.Exists(m => m.Faction.ToLower() == "default"))
                     {
                        DefaultFitting = FactionFitting.Find(m => m.Faction.ToLower() == "default");
                        if (string.IsNullOrEmpty(DefaultFitting.Fitting))
                        {
                           UseFittingManager = false;
                           Logging.Log("Settings: Error! No default fitting specified or fitting is incorrect.  Fitting manager will not be used.");
                        }
                        Logging.Log("Settings: Faction Fittings defined. Fitting manager will be used when appropriate.");
                     }
                     else
                     {
                        UseFittingManager = false;
                        Logging.Log(
                           "Settings: Error! No default fitting specified or fitting is incorrect.  Fitting manager will not be used.");
                     }
                  }
                  else
                  {
                     UseFittingManager = false;
                     Logging.Log("Settings: No faction fittings specified.  Fitting manager will not be used.");
                  }
               }
               //
               // Fitting based on the name of the mission
               //
               MissionFitting.Clear();
               XElement missionFittings = xml.Element("missionfittings");
               if (UseFittingManager) //no need to look for or load these settings if FittingManager is disabled
               {
                  if (missionFittings != null)
                     foreach (XElement missionfitting in missionFittings.Elements("missionfitting"))
                        MissionFitting.Add(new MissionFitting(missionfitting));
               }


               //
               // Mission Blacklist
               //
               Blacklist.Clear();
               XElement blacklist = xml.Element("blacklist");
               if (blacklist != null)
                  foreach (XElement mission in blacklist.Elements("mission"))
                     Blacklist.Add((string) mission);
               //
               // Faction Blacklist
               //
               FactionBlacklist.Clear();
               XElement factionblacklist = xml.Element("factionblacklist");
               if (factionblacklist != null)
                  foreach (XElement faction in factionblacklist.Elements("faction"))
                     FactionBlacklist.Add((string) faction);
            }
            //
            // if enabled the following would keep you from looting or salvaging small wrecks
            //
            //list of small wreck
            if (WreckBlackListSmallWrecks)
            {
                WreckBlackList.Add(26557);
                WreckBlackList.Add(26561);
                WreckBlackList.Add(26564);
                WreckBlackList.Add(26567);
                WreckBlackList.Add(26570);
                WreckBlackList.Add(26573);
                WreckBlackList.Add(26576);
                WreckBlackList.Add(26579);
                WreckBlackList.Add(26582);
                WreckBlackList.Add(26585);
                WreckBlackList.Add(26588);
                WreckBlackList.Add(26591);
                WreckBlackList.Add(26594);
                WreckBlackList.Add(26935);
            }

            //
            // if enabled the following would keep you from looting or salvaging medium wrecks
            //
            //list of medium wreck
            if (WreckBlackListMediumWrecks)
            {
                WreckBlackList.Add(26558);
                WreckBlackList.Add(26562);
                WreckBlackList.Add(26568);
                WreckBlackList.Add(26574);
                WreckBlackList.Add(26580);
                WreckBlackList.Add(26586);
                WreckBlackList.Add(26592);
                WreckBlackList.Add(26934);
            }

            //
            // Log location and log names defined here
            //
            Logpath = (System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log\\" + Cache.Instance.DirectEve.Me.Name + "\\");
            //logpath_s = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log\\";
            ConsoleLogPath = System.IO.Path.Combine(Logpath + "Console\\");
            ConsoleLogFile = System.IO.Path.Combine(ConsoleLogPath + string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + Cache.Instance.DirectEve.Me.Name + "-" + "console" + ".log");
            SessionsLogPath = System.IO.Path.Combine(Logpath);
            SessionsLogFile = System.IO.Path.Combine(SessionsLogPath + Cache.Instance.DirectEve.Me.Name + ".Sessions.log");
            DroneStatsLogPath = System.IO.Path.Combine(Logpath);
            DroneStatslogFile = System.IO.Path.Combine(DroneStatsLogPath + Cache.Instance.DirectEve.Me.Name + ".DroneStats.log");
            WreckLootStatisticsPath = System.IO.Path.Combine(Logpath);
            WreckLootStatisticsFile = System.IO.Path.Combine(WreckLootStatisticsPath + Cache.Instance.DirectEve.Me.Name + ".WreckLootStatisticsDump.log");
            MissionStats1LogPath = System.IO.Path.Combine(Logpath, "missionstats\\");
            MissionStats1LogFile = System.IO.Path.Combine(MissionStats1LogPath +  Cache.Instance.DirectEve.Me.Name + ".Statistics.log");
            MissionStats2LogPath = System.IO.Path.Combine(Logpath, "missionstats\\");
            MissionStats2LogFile = System.IO.Path.Combine(MissionStats2LogPath + Cache.Instance.DirectEve.Me.Name + ".DatedStatistics.log");
            MissionStats3LogPath = System.IO.Path.Combine(Logpath, "missionstats\\");
            MissionStats3LogFile = System.IO.Path.Combine(MissionStats3LogPath + Cache.Instance.DirectEve.Me.Name + ".CustomDatedStatistics.csv");
            PocketStatisticsPath = System.IO.Path.Combine(Logpath, "pocketstats\\");
            PocketStatisticsFile = System.IO.Path.Combine(PocketStatisticsPath + Cache.Instance.DirectEve.Me.Name + "pocketstats-combined.csv");
            //create all the logging directories even if they aren't configured to be used - we can adjust this later if it really bugs people to have some potentially empty directories. 
            Directory.CreateDirectory(Logpath);

            Directory.CreateDirectory(ConsoleLogPath); 
            Directory.CreateDirectory(SessionsLogPath);
            Directory.CreateDirectory(DroneStatsLogPath);
            Directory.CreateDirectory(WreckLootStatisticsPath);
            Directory.CreateDirectory(MissionStats1LogPath);
            Directory.CreateDirectory(MissionStats2LogPath);
            Directory.CreateDirectory(MissionStats3LogPath);
            Directory.CreateDirectory(PocketStatisticsPath);
            
            if (SettingsLoaded != null)
                SettingsLoaded(this, new EventArgs());
        }
    }
}