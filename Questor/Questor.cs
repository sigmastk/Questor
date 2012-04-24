// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------


namespace Questor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    //using System.Reflection;
    using DirectEve;
    using global::Questor.Modules;
    using global::Questor.Storylines;
    using LavishScriptAPI;


    public class Questor
    {
        private readonly QuestorfrmMain m_Parent;
        private readonly AgentInteraction _agentInteraction;
        private readonly Arm _arm;
        private readonly SwitchShip _switch;
        private readonly Combat _combat;
        private readonly CourierMission _courier;
        private readonly LocalWatch _localwatch;
        private readonly ScanInteraction _scanInteraction;
        private readonly Defense _defense;
        private readonly DirectEve _directEve;
        private readonly Drones _drones;

        private DateTime _lastPulse;
        private DateTime _lastSalvageTrip = DateTime.MinValue;
        private readonly MissionController _missionController;
        private readonly Panic _panic;
        private readonly Storyline _storyline;
        private readonly Cleanup _cleanup;
        private readonly Statistics _statistics;

        private readonly Salvage _salvage;
        private readonly Traveler _traveler;
        private readonly UnloadLoot _unloadLoot;

        public DateTime LastFrame;
        public DateTime LastAction;
        private readonly Random _random;
        private int _randomDelay;
        public static long AgentID;

        private double _lastX;
        private double _lastY;
        private double _lastZ;
        private bool _gatesPresent;
        private bool _firstStart = true;
        public  bool Panicstatereset = false;

        //DateTime _nextAction = DateTime.Now;

        public Questor(QuestorfrmMain form1)
        {
            m_Parent = form1;
            _lastPulse = DateTime.MinValue;

            _random = new Random();

            //_scoop = new Scoop();
            _salvage = new Salvage();
            _defense = new Defense();
            _localwatch = new LocalWatch();
            _scanInteraction = new ScanInteraction();
            _combat = new Combat();
            _traveler = new Traveler();
            _unloadLoot = new UnloadLoot();
            _agentInteraction = new AgentInteraction();
            _arm = new Arm();
            _courier = new CourierMission();
            _switch = new SwitchShip();
            _missionController = new MissionController();
            _drones = new Drones();
            _panic = new Panic();
            _storyline = new Storyline();
            _cleanup = new Cleanup();
            _statistics = new Statistics();

            Settings.Instance.SettingsLoaded += SettingsLoaded;

            // State fixed on ExecuteMission
            State = QuestorState.Idle;

            _directEve = new DirectEve();
            Cache.Instance.DirectEve = _directEve;

            Cache.Instance.StopTimeSpecified = Program.StopTimeSpecified;
            Cache.Instance.MaxRuntime = Program.maxRuntime;
            Cache.Instance.StopTime = Program.StopTime;
            Cache.Instance.StartTime = Program.startTime;
            Cache.Instance.QuestorStarted_DateTime = DateTime.Now;

            _directEve.OnFrame += OnFrame;
        }

        public QuestorState State { get; set; }

        private bool ValidSettings { get; set; }
        public bool ExitWhenIdle { get; set; }
        private bool _closeQuestorCMDUplink = true;
        public bool CloseQuestorflag = true;
        private DateTime CloseQuestorDelay { get; set; }
        private bool _closeQuestor10SecWarningDone = false;

        public string CharacterName { get; set; }

        public void SettingsLoaded(object sender, EventArgs e)
        {
            ApplySettings();
            ValidateSettings();
        }

        public void ValidateSettings()
        {
            ValidSettings = true;
            if (Settings.Instance.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)
            {
                if (!Settings.Instance.Ammo.Any(a => a.DamageType == DamageType.EM))
                    Logging.Log("Settings: Missing EM damage type!");
                if (!Settings.Instance.Ammo.Any(a => a.DamageType == DamageType.Thermal))
                    Logging.Log("Settings: Missing Thermal damage type!");
                if (!Settings.Instance.Ammo.Any(a => a.DamageType == DamageType.Kinetic))
                    Logging.Log("Settings: Missing Kinetic damage type!");
                if (!Settings.Instance.Ammo.Any(a => a.DamageType == DamageType.Explosive))
                    Logging.Log("Settings: Missing Explosive damage type!");

                Logging.Log("Settings: You are required to specify all 4 damage types in your settings xml file!");
                ValidSettings = false;
            }

            DirectAgent agent = Cache.Instance.DirectEve.GetAgentByName(Cache.Instance.CurrentAgent);

            if (agent == null || !agent.IsValid)
            {
                Logging.Log("Settings: Unable to locate agent [" + Cache.Instance.CurrentAgent + "]");
                ValidSettings = false;
            }
            else
            {
                _agentInteraction.AgentId = agent.AgentId;
                _missionController.AgentId = agent.AgentId;
                _arm.AgentId = agent.AgentId;
                _statistics.AgentID = agent.AgentId;
                AgentID = agent.AgentId;
            }
        }

        public void ApplySettings()
        {
            _salvage.Ammo = Settings.Instance.Ammo;
            _salvage.MaximumWreckTargets = Settings.Instance.MaximumWreckTargets;
            _salvage.ReserveCargoCapacity = Settings.Instance.ReserveCargoCapacity;
            _salvage.LootEverything = Settings.Instance.LootEverything;
        }

        private void BeginClosingQuestor()
        {
           Cache.Instance.EnteredCloseQuestor_DateTime = DateTime.Now;
           State = QuestorState.CloseQuestor;
        }

        private void RecallDrones()
        {
           if (Cache.Instance.InSpace && Cache.Instance.ActiveDrones.Any() && DateTime.Now > Cache.Instance.NextDroneRecall)
           {
              Logging.Log("QuestorState." + State + ": We are not scrambled and will be warping soon: pulling drones");
              // Tell the drones module to retract drones
              Cache.Instance.IsMissionPocketDone = true;
              Cache.Instance.NextDroneRecall = DateTime.Now.AddSeconds(10);
           }
        }

        private void TravelToAgentsStation()
        {
           var baseDestination = _traveler.Destination as StationDestination;
           if (baseDestination == null || baseDestination.StationId != Cache.Instance.Agent.StationId)
              _traveler.Destination = new StationDestination(Cache.Instance.Agent.SolarSystemId, Cache.Instance.Agent.StationId, Cache.Instance.DirectEve.GetLocationName(Cache.Instance.Agent.StationId));
           //
           // is there a reason we do not just let combat.cs pick targets? 
           // I am not seeing why we are limiting ourselves to priority targets
           //
           if (Cache.Instance.PriorityTargets.Any(pt => pt != null && pt.IsValid))
           {
              Logging.Log("QuestorState." + State+ ": TravelToAgentsStation: Priority targets found, engaging!");
              _combat.ProcessState();
              _drones.ProcessState(); //do we really want to use drones here? 
           }
           else
           {
              if (Cache.Instance.InSpace && Cache.Instance.ActiveDrones.Any() && DateTime.Now > Cache.Instance.NextDroneRecall)
              {
                 Logging.Log("QuestorState." + State + ": We are not scrambled and will be warping soon: pulling drones");
                 // Tell the drones module to retract drones
                 Cache.Instance.IsMissionPocketDone = true;
                 Cache.Instance.NextDroneRecall = DateTime.Now.AddSeconds(10);
              }
           }
           _traveler.ProcessState();
           if (Settings.Instance.DebugStates)
           {
              Logging.Log("Traveler.State = " + _traveler.State);
           }
        }

        private void AvoidBumpingThings()
        {
           // anti bump
           EntityCache bigObjects = Cache.Instance.Entities.Where(i => i.GroupId == (int)Group.LargeCollidableStructure || i.GroupId == (int)Group.SpawnContainer).OrderBy(t => t.Distance).FirstOrDefault();
           //
           // always shoot at NPCs while getting un-hung
           //
           _combat.ProcessState();

           //
           // only use drones if warp scrambled as we do not want to leave them behind accidentally
           //
           if (Cache.Instance.InSpace && Cache.Instance.TargetedBy.Any(t => t.IsWarpScramblingMe))
           {
              _drones.ProcessState();
           }
           //
           // if we are "too close" to the bigObject move away... (is orbit the best thing to do here?)
           //
           if (Cache.Instance.InSpace && bigObjects != null && bigObjects.Distance < (int)Distance.TooCloseToStructure)
           {
              if (DateTime.Now > Cache.Instance.NextOrbit)
              {
                 bigObjects.Orbit((int)Distance.SafeDistancefromStructure);
                 Logging.Log("QuestorState: " + State + ": initiating Orbit of [" + bigObjects.Name + "] orbiting at [" + Cache.Instance.OrbitDistance + "]");
                 Cache.Instance.NextOrbit = DateTime.Now.AddSeconds((int)Time.OrbitDelay_seconds);
              }
              return; //we are still too close, do not continue through the rest until we are not "too close" anymore
           }
           else
           {
              //we are no longer "too close" and can proceed. 
           }
        }


        private void OnFrame(object sender, EventArgs e)
        {
            var watch = new Stopwatch();
            LastFrame = DateTime.Now;
            // Only pulse state changes every 1.5s
            if (DateTime.Now.Subtract(_lastPulse).TotalMilliseconds < (int)Time.QuestorPulse_milliseconds) //default: 1500ms
                return;
            _lastPulse = DateTime.Now;

            // Session is not ready yet, do not continue
            if (!Cache.Instance.DirectEve.Session.IsReady)
                return;


            // We are not in space or station, don't do shit yet!
            if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
            {
                Cache.Instance._nextInSpaceorInStation = DateTime.Now.AddSeconds(7);
                return;
            }

            if (DateTime.Now < Cache.Instance._nextInSpaceorInStation)
                return;

            // New frame, invalidate old cache
            Cache.Instance.InvalidateCache();

            // Update settings (settings only load if character name changed)
            Settings.Instance.LoadSettings();
            CharacterName = Cache.Instance.DirectEve.Me.Name;

            // Check 3D rendering
            if (Cache.Instance.DirectEve.Session.IsInSpace && Cache.Instance.DirectEve.Rendering3D != !Settings.Instance.Disable3D)
                Cache.Instance.DirectEve.Rendering3D = !Settings.Instance.Disable3D;

            // Invalid settings, quit while we're ahead
            if (!ValidSettings)
            {
                if (DateTime.Now.Subtract(LastAction).TotalSeconds < (int)Time.ValidateSettings_seconds) //default is a 15 second interval
                {
                    ValidateSettings();
                    LastAction = DateTime.Now;
                }
                return;
            }

            if (DateTime.Now.Subtract(Cache.Instance._lastupdateofSessionRunningTime).TotalSeconds < (int)Time.SessionRunningTimeUpdate_seconds)
            {
                Cache.Instance.SessionRunningTime = (int)DateTime.Now.Subtract(Cache.Instance.QuestorStarted_DateTime).TotalMinutes;
                Cache.Instance._lastupdateofSessionRunningTime = DateTime.Now;
            }

            if ((DateTime.Now.Subtract(Cache.Instance.QuestorStarted_DateTime).TotalSeconds > 10) && (DateTime.Now.Subtract(Cache.Instance.QuestorStarted_DateTime).TotalSeconds < 60))
            {
                if (Cache.Instance.QuestorJustStarted)
                {
                    Cache.Instance.QuestorJustStarted = false;
                    Cache.Instance.SessionState = "Starting Up";

                    // get the current process
                    Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

                    // get the physical mem usage
                    Cache.Instance.TotalMegaBytesOfMemoryUsed = ((currentProcess.WorkingSet64 / 1024) / 1024);
                    Logging.Log("Questor: EVE instance: totalMegaBytesOfMemoryUsed - " + Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");
                    Cache.Instance.SessionIskGenerated = 0;
                    Cache.Instance.SessionLootGenerated = 0;
                    Cache.Instance.SessionLPGenerated = 0;
                    // write session log
                    Statistics.WriteSessionLogStarting();
                }
            }

            if (!Cache.Instance.Paused)
            {
                if (DateTime.Now.Subtract(Cache.Instance._lastWalletCheck).TotalMinutes > (int)Time.WalletCheck_minutes)
                {
                    Cache.Instance._lastWalletCheck = DateTime.Now;
                    //Logging.Log("[Questor] Wallet Balance Debug Info: lastknowngoodconnectedtime = " + Settings.Instance.lastKnownGoodConnectedTime);
                    //Logging.Log("[Questor] Wallet Balance Debug Info: DateTime.Now - lastknowngoodconnectedtime = " + DateTime.Now.Subtract(Settings.Instance.lastKnownGoodConnectedTime).TotalSeconds);
                    if (Math.Round(DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalMinutes) > 1)
                    {
                        Logging.Log(String.Format("Questor: Wallet Balance Has Not Changed in [ {0} ] minutes.", Math.Round(DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)));
                    }

                    //Settings.Instance.walletbalancechangelogoffdelay = 2;  //used for debugging purposes
                    //Logging.Log("Cache.Instance.lastKnownGoodConnectedTime is currently: " + Cache.Instance.lastKnownGoodConnectedTime);
                    if (Math.Round(DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalMinutes) < Settings.Instance.Walletbalancechangelogoffdelay)
                    {
                        if (State == QuestorState.Salvage)
                        {
                            Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                            Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        }
                        else
                        {
                            if (Cache.Instance.MyWalletBalance != Cache.Instance.DirectEve.Me.Wealth)
                            {
                                Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                                Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                            }
                        }
                    }
                    else
                    {
                        Logging.Log(String.Format("Questor: Wallet Balance Has Not Changed in [ {0} ] minutes. Switching to QuestorState.CloseQuestor", Math.Round(DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)));
                        Cache.Instance.ReasonToStopQuestor = "Wallet Balance did not change for over " + Settings.Instance.Walletbalancechangelogoffdelay + "min";

                        if (Settings.Instance.WalletbalancechangelogoffdelayLogofforExit == "logoff")
                        {
                            Logging.Log("Questor: walletbalancechangelogoffdelayLogofforExit is set to: " + Settings.Instance.WalletbalancechangelogoffdelayLogofforExit);
                            Cache.Instance.CloseQuestorCMDLogoff = true;
                            Cache.Instance.CloseQuestorCMDExitGame = false;
                            Cache.Instance.SessionState = "LoggingOff";
                        }
                        if (Settings.Instance.WalletbalancechangelogoffdelayLogofforExit == "exit")
                        {
                            Logging.Log("Questor: walletbalancechangelogoffdelayLogofforExit is set to: " + Settings.Instance.WalletbalancechangelogoffdelayLogofforExit);
                            Cache.Instance.CloseQuestorCMDLogoff = false;
                            Cache.Instance.CloseQuestorCMDExitGame = true;
                            Cache.Instance.SessionState = "Exiting";
                        }
                        BeginClosingQuestor();
                        return;
                    }
                }
            }
            //If local unsafe go to base and do not start mission again
            if (Settings.Instance.FinishWhenNotSafe && (State != QuestorState.GotoNearestStation /*|| State!=QuestorState.GotoBase*/))
            {
                //need to remove spam
                if (Cache.Instance.InSpace && !Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    var station = Cache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();
                    if (station != null)
                    {
                        Logging.Log("Local not safe.Station found. Going to nearest station");
                        if (State != QuestorState.GotoNearestStation)
                            State = QuestorState.GotoNearestStation;
                    }
                    else
                    {
                        Logging.Log("Local not safe.Station not found. Going back to base");
                        if (State != QuestorState.GotoBase)
                            State = QuestorState.GotoBase;
                    }
                    Cache.Instance.StopBot = true;
                }
            }
            // We always check our defense state if we're in space, regardless of questor state
            // We also always check panic
            if (Cache.Instance.InSpace)
            {
                watch.Reset();
                watch.Start();
                if (!Cache.Instance.DoNotBreakInvul)
                {
                    _defense.ProcessState();
                }
                watch.Stop();

                if (Settings.Instance.DebugPerformance)
                    Logging.Log("Defense.ProcessState took " + watch.ElapsedMilliseconds + "ms");
            }
            if (Cache.Instance.Paused)
            {
                Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                Cache.Instance.GotoBaseNow = false;
                Cache.Instance.SessionState = string.Empty;
                return;
            }

            if (Cache.Instance.SessionState == "Quitting")
            {
                if (State != QuestorState.CloseQuestor)
                {
                    BeginClosingQuestor();
                }
            }
            if (Cache.Instance.GotoBaseNow)
            {
                if (State != QuestorState.GotoBase)
                {
                    State = QuestorState.GotoBase;
                }
            }

            // Start _cleanup.ProcessState
            // Description: Closes Windows, and eventually other things considered 'cleanup' useful to more than just Questor(Missions) but also Anomalies, Mining, etc
            //
            watch.Reset();
            watch.Start();
            _cleanup.ProcessState();
            watch.Stop();
            if (Settings.Instance.DebugPerformance)
                Logging.Log("Cleanup.ProcessState took " + watch.ElapsedMilliseconds + "ms");

            if (Settings.Instance.DebugStates)
                Logging.Log("Cleanup.State = " + _cleanup.State);
            // Done
            // Cleanup State: ProcessState

            // Start _salvage.ProcessState
            // Description: salvages, and watches for bookmarks in people and places, a no-op if you are in station and aren't set with characterMode=salvage
            //
            watch.Reset();
            watch.Start();
            _salvage.ProcessState();
            watch.Stop();
            // Done
            // Salvage State: ProcessState
            //
            if (Settings.Instance.DebugPerformance)
                Logging.Log("Salvage.ProcessState took " + watch.ElapsedMilliseconds + "ms");

            if (Settings.Instance.DebugStates)
                Logging.Log("Salvage.State = " + _salvage.State);
            //
            // Panic always runs, not just in space
            //
            watch.Reset();
            watch.Start();
            Cache.Instance.InMission = State == QuestorState.ExecuteMission;
            if (State == QuestorState.Storyline && _storyline.State == StorylineState.ExecuteMission)
            {
                Cache.Instance.InMission |= _storyline.StorylineHandler is GenericCombatStoryline && (_storyline.StorylineHandler as GenericCombatStoryline).State == GenericCombatStorylineState.ExecuteMission;
            }
            _panic.ProcessState();
            watch.Stop();

            if (Settings.Instance.DebugPerformance)
                Logging.Log("Panic.ProcessState took " + watch.ElapsedMilliseconds + "ms");

            if (_panic.State == PanicState.Panic || _panic.State == PanicState.Panicking)
            {
                // If Panic is in panic state, questor is in panic state :)
                State = State == QuestorState.Storyline ? QuestorState.StorylinePanic : QuestorState.Panic;

                if (Settings.Instance.DebugStates)
                    Logging.Log("State = " + State);
                if (Panicstatereset)
                {
                    _panic.State = PanicState.Normal;
                    Panicstatereset = false;
                }
            }
            else if (_panic.State == PanicState.Resume)
            {
                // Reset panic state
                _panic.State = PanicState.Normal;

                // Ugly storyline resume hack
                if (State == QuestorState.StorylinePanic)
                {
                    State = QuestorState.Storyline;
                    if (_storyline.StorylineHandler is GenericCombatStoryline)
                        (_storyline.StorylineHandler as GenericCombatStoryline).State = GenericCombatStorylineState.GotoMission;
                }
                else
                {
                    // Head back to the mission
                    _traveler.State = TravelerState.Idle;
                    State = QuestorState.GotoMission;
                }

                if (Settings.Instance.DebugStates)
                    Logging.Log("State = " + State);
            }

            if (Settings.Instance.DebugStates)
                Logging.Log("Panic.State = " + _panic.State);

            // When in warp there's nothing we can do, so ignore everything
            if (Cache.Instance.InWarp)
                return;

            //DirectAgentMission mission;
            switch (State)
            {
                case QuestorState.Idle:
                    // Every 5 min of idle check and make sure we aren't supposed to stop...
                    if (Math.Round(DateTime.Now.Subtract(Cache.Instance._lastTimeCheckAction).TotalMinutes) > 5)
                    {
                        Cache.Instance._lastTimeCheckAction = DateTime.Now;
                        if (DateTime.Now.Subtract(Cache.Instance.QuestorStarted_DateTime).TotalMinutes > Cache.Instance.MaxRuntime)
                        {
                            // quit questor
                            Logging.Log("Questor: Maximum runtime exceeded.  Quiting...");
                            Cache.Instance.ReasonToStopQuestor = "Maximum runtime specified and reached.";
                            Settings.Instance.AutoStart = false;
                            Cache.Instance.CloseQuestorCMDLogoff = false;
                            Cache.Instance.CloseQuestorCMDExitGame = true;
                            Cache.Instance.SessionState = "Exiting";
                            if (State == QuestorState.Idle)
                            {
                                BeginClosingQuestor();
                            }
                            return;
                        }
                        if (Cache.Instance.StopTimeSpecified)
                        {
                            if (DateTime.Now >= Cache.Instance.StopTime)
                            {
                                Logging.Log("Questor: Time to stop.  Quitting game.");
                                Cache.Instance.ReasonToStopQuestor = "StopTimeSpecified and reached.";
                                Settings.Instance.AutoStart = false;
                                Cache.Instance.CloseQuestorCMDLogoff = false;
                                Cache.Instance.CloseQuestorCMDExitGame = true;
                                Cache.Instance.SessionState = "Exiting";
                                if (State == QuestorState.Idle)
                                {
                                    BeginClosingQuestor();
                                }
                                return;
                            }
                        }
                        if (ExitWhenIdle && !Settings.Instance.AutoStart)
                        {
                            Cache.Instance.ReasonToStopQuestor = "Settings: ExitWhenIdle is true, and we are idle... exiting";
                            Logging.Log(Cache.Instance.ReasonToStopQuestor);
                            Settings.Instance.AutoStart = false;
                            Cache.Instance.CloseQuestorCMDLogoff = false;
                            Cache.Instance.CloseQuestorCMDExitGame = true;
                            Cache.Instance.SessionState = "Exiting";
                            if (State == QuestorState.Idle)
                            {
                                BeginClosingQuestor();
                            }
                            return;
                        }
                    }
                    if (Cache.Instance.StopBot)
                        return;

                    if (Cache.Instance.InSpace)
                    {
                        // Questor does not handle in space starts very well, head back to base to try again
                        Logging.Log("Questor: Started questor while in space, heading back to base in 15 seconds");
                        LastAction = DateTime.Now;
                        if (State == QuestorState.Idle)
                        {
                            State = QuestorState.DelayedGotoBase;
                        }
                        break;
                    }

                    if ((Settings.Instance.CharacterMode.ToLower() == "Combat Missions".ToLower()) || (Settings.Instance.CharacterMode.ToLower() == "dps".ToLower())) //only write combat mission logs is we are actually doing missions
                    {
                        // only attempt to write the mission statistics logs if one of the mission stats logs is enabled in settings
                        if (Settings.Instance.MissionStats1Log || Settings.Instance.MissionStats3Log || Settings.Instance.MissionStats3Log)
                        {
                            if (!Statistics.Instance.MissionLoggingCompleted)
                            {
                                Statistics.WriteMissionStatistics();
                                break;
                            }
                        }
                    }
                    else
                    {
                        //Logging.Log("Character Mode is [" + Settings.Instance.CharacterMode + "] no need to write any mission stats");
                    }

                    if (Settings.Instance.AutoStart)
                    {
                        // Don't start a new action an hour before downtime
                        if (DateTime.UtcNow.Hour == 10)
                            break;

                        // Don't start a new action near downtime
                        if (DateTime.UtcNow.Hour == 11 && DateTime.UtcNow.Minute < 15)
                            break;

                        if (Settings.Instance.RandomDelay > 0 || Settings.Instance.MinimumDelay > 0)
                        {
                            _randomDelay = (Settings.Instance.RandomDelay > 0 ? _random.Next(Settings.Instance.RandomDelay) : 0) + Settings.Instance.MinimumDelay;
                            LastAction = DateTime.Now;
                            if (State == QuestorState.Idle)
                            {
                                State = QuestorState.DelayedStart;
                            }
                            Logging.Log("Questor: Random start delay of [" + _randomDelay + "] seconds");
                            return;
                        }
                        else
                        {
                            if (State == QuestorState.Idle)
                            {
                                State = QuestorState.Cleanup;
                            }
                            return;
                        }
                    }
                    break;

                case QuestorState.DelayedStart:
                    if (DateTime.Now.Subtract(LastAction).TotalSeconds < _randomDelay)
                        break;

                    _storyline.Reset();
                    if (State == QuestorState.DelayedStart)
                    {
                        State = QuestorState.Cleanup;
                    }
                    break;

                case QuestorState.DelayedGotoBase:
                    if (DateTime.Now.Subtract(LastAction).TotalSeconds < (int)Time.DelayedGotoBase_seconds)
                        break;

                    Logging.Log("Questor: Heading back to base");
                    if (State == QuestorState.DelayedGotoBase)
                    {
                        State = QuestorState.GotoBase;
                    }
                    break;

                case QuestorState.Cleanup:
                    //
                    // this state is needed because forced disconnects
                    // and crashes can leave "extra" cargo in the
                    // cargo hold that is undesirable and causes
                    // problems loading the correct ammo on occasion
                    //
                    if (Cache.Instance.LootAlreadyUnloaded == false)
                    {
                        if (State == QuestorState.Cleanup)
                        {
                            State = QuestorState.GotoBase;
                        }
                        break;
                    }
                    else
                    {
                        if (State == QuestorState.Cleanup)
                        {
                            State = QuestorState.CheckEVEStatus;
                        }
                        break;
                    }

                case QuestorState.Start:
                    if (Settings.Instance.CharacterMode.ToLower() == "salvage")
                    {
                        if (State == QuestorState.Start)
                        {
                            Logging.Log("Questor: Start After Mission Salvaging");
                            State = QuestorState.BeginAfterMissionSalvaging;
                        }
                        break;
                    }
                    if (_firstStart && Settings.Instance.MultiAgentSupport)
                    {
                        //if you are in wrong station and is not first agent
                        if (State == QuestorState.Start)
                        {
                            State = QuestorState.Switch;
                        }
                        _firstStart = false;
                        break;
                    }
                    Cache.Instance.OpenWrecks = false;
                    if (_agentInteraction.State == AgentInteractionState.Idle)
                    {
                        Cache.Instance.Wealth = Cache.Instance.DirectEve.Me.Wealth;

                        Cache.Instance.wrecksThisMission = 0;
                        if (Settings.Instance.EnableStorylines && _storyline.HasStoryline())
                        {
                            Logging.Log("Questor: Storyline detected, doing storyline.");
                            _storyline.Reset();
                            if (State == QuestorState.Start)
                            {
                                State = QuestorState.Storyline;
                            }
                            break;
                        }
                        Logging.Log("AgentInteraction: Start conversation [Start Mission]");
                        _agentInteraction.State = AgentInteractionState.StartConversation;
                        _agentInteraction.Purpose = AgentInteractionPurpose.StartMission;
                    }

                    _agentInteraction.ProcessState();

                    if (Settings.Instance.DebugStates)
                        Logging.Log("AgentInteraction.State = " + _agentInteraction.State);

                    if (_agentInteraction.State == AgentInteractionState.Done)
                    {
                        Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID);
                        if (Cache.Instance.Mission != null)
                        {
                            // Update loyalty points again (the first time might return -1)
                            Statistics.Instance.LoyaltyPoints = Cache.Instance.Agent.LoyaltyPoints;
                            Cache.Instance.MissionName = Cache.Instance.Mission.Name;
                        }

                        _agentInteraction.State = AgentInteractionState.Idle;
                        if (State == QuestorState.Start)
                        {
                            State = QuestorState.Arm;
                        }
                        return;
                    }

                    if (_agentInteraction.State == AgentInteractionState.ChangeAgent)
                    {
                        _agentInteraction.State = AgentInteractionState.Idle;
                        ValidateSettings();
                        if (State == QuestorState.Start)
                        {
                            State = QuestorState.Switch;
                        }
                        break;
                    }

                    break;

                case QuestorState.Switch:

                    if (_switch.State == SwitchShipState.Idle)
                    {
                        Logging.Log("Switch: Begin");
                        _switch.State = SwitchShipState.Begin;
                    }

                    _switch.ProcessState();

                    if (_switch.State == SwitchShipState.Done)
                    {
                        _switch.State = SwitchShipState.Idle;
                        if (State == QuestorState.Switch)
                        {
                            State = QuestorState.GotoBase;
                        }
                    }
                    break;

                case QuestorState.Arm:
                    if (_arm.State == ArmState.Idle)
                    {
                        if (Cache.Instance.CourierMission)
                            _arm.State = ArmState.SwitchToTransportShip;
                        else
                        {
                            Logging.Log("Arm: Begin");
                            _arm.State = ArmState.Begin;

                            // Load right ammo based on mission
                            _arm.AmmoToLoad.Clear();
                            _arm.AmmoToLoad.AddRange(_agentInteraction.AmmoToLoad);
                        }
                    }

                    _arm.ProcessState();

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Arm.State = " + _arm.State);

                    if (_arm.State == ArmState.NotEnoughAmmo)
                    {
                        // we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                        // we may be out of drones/ammo but disconnecting/reconnecting will not fix that so update the timestamp
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        Logging.Log("Arm: Armstate.NotEnoughAmmo");
                        _arm.State = ArmState.Idle;
                        if (State == QuestorState.Arm)
                        {
                            State = QuestorState.Error;
                        }
                    }

                    if (_arm.State == ArmState.NotEnoughDrones)
                    {
                        // we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                        // we may be out of drones/ammo but disconnecting/reconnecting will not fix that so update the timestamp
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        Logging.Log("Arm: Armstate.NotEnoughDrones");
                        _arm.State = ArmState.Idle;
                        if (State == QuestorState.Arm)
                        {
                            State = QuestorState.Error;
                        }
                    }

                    if (_arm.State == ArmState.Done)
                    {
                        //we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        _arm.State = ArmState.Idle;
                        _drones.State = DroneState.WaitingForTargets;

                        if (Cache.Instance.CourierMission)
                        {
                            if (State == QuestorState.Arm)
                            {
                                State = QuestorState.CourierMission;
                            }
                        }
                        else
                        {
                            if (State == QuestorState.Arm)
                            {
                                State = QuestorState.LocalWatch;
                            }
                        }
                    }

                    break;

                case QuestorState.LocalWatch:
                    if (Settings.Instance.UseLocalWatch)
                    {
                        Cache.Instance._lastLocalWatchAction = DateTime.Now;
                        if (Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                        {
                            Logging.Log("Questor.LocalWatch: local is clear");
                            if (State == QuestorState.LocalWatch)
                            {
                                State = QuestorState.WarpOutStation;
                            }
                        }
                        else
                        {
                            Logging.Log("Questor.LocalWatch: Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                            if (State == QuestorState.LocalWatch)
                            {
                                State = QuestorState.WaitingforBadGuytoGoAway;
                            }
                            Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                            Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        }
                    }
                    else
                    {
                        if (State == QuestorState.LocalWatch)
                        {
                            State = QuestorState.WarpOutStation;
                        }
                    }
                    break;

                case QuestorState.WaitingforBadGuytoGoAway:
                    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    if (DateTime.Now.Subtract(Cache.Instance._lastLocalWatchAction).TotalMinutes < (int)Time.WaitforBadGuytoGoAway_minutes)
                        break;
                    if (State == QuestorState.WaitingforBadGuytoGoAway)
                    {
                        State = QuestorState.LocalWatch;
                    }
                    break;

                case QuestorState.WarpOutStation:
                    DirectBookmark warpOutBookmark = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkWarpOut ?? "").OrderByDescending(b => b.CreatedOn).FirstOrDefault(b => b.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId);
                    //DirectBookmark _bookmark = Cache.Instance.BookmarksByLabel(Settings.Instance.bookmarkWarpOut + "-" + Cache.Instance.CurrentAgent ?? "").OrderBy(b => b.CreatedOn).FirstOrDefault();
                    long solarid = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Logging.Log("Questor.WarpOut: No Bookmark");
                        if (State == QuestorState.WarpOutStation)
                        {
                            State = QuestorState.GotoMission;
                        }
                    }
                    else if (warpOutBookmark.LocationId == solarid)
                    {
                        if (_traveler.Destination == null)
                        {
                            Logging.Log("Questor.WarpOut: Warp at " + warpOutBookmark.Title);
                            _traveler.Destination = new BookmarkDestination(warpOutBookmark);
                            Cache.Instance.DoNotBreakInvul = true;
                        }

                        _traveler.ProcessState();
                        if (_traveler.State == TravelerState.AtDestination)
                        {
                            Logging.Log("Questor.WarpOut: Safe!");
                            Cache.Instance.DoNotBreakInvul = false;
                            if (State == QuestorState.WarpOutStation)
                            {
                                State = QuestorState.GotoMission;
                            }
                            _traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Logging.Log("Questor.WarpOut: No Bookmark in System");
                        if (State == QuestorState.WarpOutStation)
                        {
                            State = QuestorState.GotoMission;
                        }
                    }
                    break;

                case QuestorState.GotoMission:
                    Statistics.Instance.MissionLoggingCompleted = false;
                    var missionDestination = _traveler.Destination as MissionBookmarkDestination;
                    if (missionDestination == null || missionDestination.AgentId != AgentID) // We assume that this will always work "correctly" (tm)
                    {
                         const string nameOfBookmark = "Encounter";
                         Logging.Log("Setting Destination to 1st bookmark from AgentID:" + AgentID + "with [" + nameOfBookmark  + "] in the title" );
                         _traveler.Destination = new MissionBookmarkDestination(Cache.Instance.GetMissionBookmark(AgentID, nameOfBookmark));
                    //   _traveler.Destination = new MissionBookmarkDestination(Cache.Instance.GetMissionBookmark(AgentID, "Encounter"));
                    }
                  //}
                    //if (missionDestination == null)
                    //{
                    //    Logging.Log("Invalid bookmark loop! Mission Controller: Error");
                    //    State = QuestorState.Error;
                    //}
                    if (Cache.Instance.PriorityTargets.Any(pt => pt != null && pt.IsValid))
                    {
                        Logging.Log("Questor.GotoMission: Priority targets found, engaging!");
                        _combat.ProcessState();
                    }

                    _traveler.ProcessState();
                    if (Settings.Instance.DebugStates)
                        Logging.Log("Traveler.State = " + _traveler.State);

                    if (_traveler.State == TravelerState.AtDestination)
                    {
                        if (State == QuestorState.GotoMission)
                        {
                            State = QuestorState.ExecuteMission;
                        }
                        // Seeing as we just warped to the mission, start the mission controller
                        _missionController.State = MissionControllerState.Start;
                        _combat.State = CombatState.CheckTargets;
                        _traveler.Destination = null;
                    }
                    break;

                case QuestorState.CombatHelper:
                    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    _combat.ProcessState();
                    _drones.ProcessState();
                    _salvage.ProcessState();
                    break;

                case QuestorState.Scanning:
                    _localwatch.ProcessState();
                    _scanInteraction.ProcessState();
                    if (_scanInteraction.State == ScanInteractionState.Idle)
                        _scanInteraction.State = ScanInteractionState.Scan;
                    /*
                    if(_scanInteraction.State == ScanInteractionState.Done)
                        State = QuestorState.CombatHelper_anomaly;
                    */
                    break;

                case QuestorState.CombatHelperAnomaly:
                    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    _combat.ProcessState();
                    _drones.ProcessState();
                    _salvage.ProcessState();
                    _localwatch.ProcessState();
                    break;

                case QuestorState.ExecuteMission:
                    watch.Reset();
                    watch.Start();
                    _combat.ProcessState();
                    watch.Stop();

                    if (Settings.Instance.DebugPerformance)
                        Logging.Log("Combat.ProcessState took " + watch.ElapsedMilliseconds + "ms");

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Combat.State = " + _combat.State);

                    watch.Reset();
                    watch.Start();
                    _drones.ProcessState();
                    watch.Stop();

                    if (Settings.Instance.DebugPerformance)
                        Logging.Log("Drones.ProcessState took " + watch.ElapsedMilliseconds + "ms");

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Drones.State = " + _drones.State);

                    watch.Reset();
                    watch.Start();
                    _salvage.ProcessState();
                    watch.Stop();

                    if (Settings.Instance.DebugPerformance)
                        Logging.Log("Salvage.ProcessState took " + watch.ElapsedMilliseconds + "ms");

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Salvage.State = " + _salvage.State);

                    watch.Reset();
                    watch.Start();
                    _missionController.ProcessState();
                    watch.Stop();

                    if (Settings.Instance.DebugPerformance)
                        Logging.Log("MissionController.ProcessState took " + watch.ElapsedMilliseconds + "ms");

                    if (Settings.Instance.DebugStates)
                        Logging.Log("MissionController.State = " + _missionController.State);

                    // If we are out of ammo, return to base, the mission will fail to complete and the bot will reload the ship
                    // and try the mission again
                    if (_combat.State == CombatState.OutOfAmmo)
                    {
                        Logging.Log("Combat: Out of Ammo!");
                        if (State == QuestorState.ExecuteMission)
                        {
                            State = QuestorState.GotoBase;
                        }
                        // Clear looted containers
                        Cache.Instance.LootedContainers.Clear();
                    }

                    if (_missionController.State == MissionControllerState.Done)
                    {
                        if (State == QuestorState.ExecuteMission)
                        {
                            State = QuestorState.GotoBase;
                        }

                        // Clear looted containers
                        Cache.Instance.LootedContainers.Clear();
                    }

                    // If in error state, just go home and stop the bot
                    if (_missionController.State == MissionControllerState.Error)
                    {
                        Logging.Log("MissionController: Error");
                        if (State == QuestorState.ExecuteMission)
                        {
                            State = QuestorState.GotoBase;
                        }

                        // Clear looted containers
                        Cache.Instance.LootedContainers.Clear();
                    }
                    break;

                case QuestorState.GotoBase:
                    AvoidBumpingThings();
                    TravelToAgentsStation();
                        if (_traveler.State == TravelerState.AtDestination)
                        {
                            Cache.Instance.GotoBaseNow = false; //we are there - turn off the 'forced' gotobase
                            Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID);

                            if (_missionController.State == MissionControllerState.Error)
                            {
                                if (State == QuestorState.GotoBase)
                                {
                                    State = QuestorState.Error;
                                }
                            }
                            else if (_combat.State != CombatState.OutOfAmmo && Cache.Instance.Mission != null && Cache.Instance.Mission.State == (int)MissionState.Accepted)
                            {
                                if (State == QuestorState.GotoBase)
                                {
                                    State = QuestorState.CompleteMission;
                                }
                            }
                            else
                            {
                                if (State == QuestorState.GotoBase)
                                {
                                    State = QuestorState.UnloadLoot;
                                }
                            }
                            _traveler.Destination = null;
                        }
                    break;

                case QuestorState.CheckEVEStatus:
                    // get the current process
                    Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

                    // get the physical mem usage (this only runs between missions)
                    Cache.Instance.TotalMegaBytesOfMemoryUsed = ((currentProcess.WorkingSet64 / 1024) / 1024);
                    Logging.Log("Questor: EVE instance: totalMegaBytesOfMemoryUsed - " + Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");

                    // If Questor window not visible, schedule a restart of questor in the uplink so that the GUI will start normally
                    if (!m_Parent.Visible && CloseQuestorflag) //GUI isn't visible and CloseQuestorflag is true, so that his code block only runs once
                    {
                        CloseQuestorflag = false;
                        //m_Parent.Visible = true; //this does not work for some reason - innerspace issue?
                        Cache.Instance.ReasonToStopQuestor = "The Questor GUI is not visible: did EVE get restarted due to a crash or lag?";
                        Logging.Log(Cache.Instance.ReasonToStopQuestor);
                        Cache.Instance.CloseQuestorCMDLogoff = false;
                        Cache.Instance.CloseQuestorCMDExitGame = true;
                        Cache.Instance.SessionState = "Exiting";
                        if (State == QuestorState.CheckEVEStatus)
                        {
                            BeginClosingQuestor();
                        }
                        return;
                    }
                    else if (Cache.Instance.TotalMegaBytesOfMemoryUsed > (Settings.Instance.EVEProcessMemoryCeiling - 50) && Settings.Instance.EVEProcessMemoryCeilingLogofforExit != "")
                    {
                        Logging.Log("Questor: Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " + Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");
                        Cache.Instance.ReasonToStopQuestor = "Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " + Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB";
                        if (Settings.Instance.EVEProcessMemoryCeilingLogofforExit == "logoff")
                        {
                            Cache.Instance.CloseQuestorCMDLogoff = true;
                            Cache.Instance.CloseQuestorCMDExitGame = false;
                            Cache.Instance.SessionState = "LoggingOff";
                            if (State == QuestorState.CheckEVEStatus)
                            {
                                BeginClosingQuestor();
                            }
                            return;
                        }
                        if (Settings.Instance.EVEProcessMemoryCeilingLogofforExit == "exit")
                        {
                            Cache.Instance.CloseQuestorCMDLogoff = false;
                            Cache.Instance.CloseQuestorCMDExitGame = true;
                            Cache.Instance.SessionState = "Exiting";
                            if (State == QuestorState.CheckEVEStatus)
                            {
                                BeginClosingQuestor();
                            }
                            return;
                        }
                        Logging.Log("Questor: EVEProcessMemoryCeilingLogofforExit was not set to exit or logoff - doing nothing ");
                        return;
                    }
                    else
                    {
                        Cache.Instance.SessionState = "Running";
                        if (State == QuestorState.CheckEVEStatus)
                        {
                            State = QuestorState.Start;
                        }
                    }
                    break;

                case QuestorState.CloseQuestor:
                    if (!Cache.Instance.CloseQuestorCMDLogoff && !Cache.Instance.CloseQuestorCMDExitGame)
                    {
                        Cache.Instance.CloseQuestorCMDExitGame = true;
                    }
                    if (_traveler.State == TravelerState.Idle)
                    {
                        Logging.Log("QuestorState.CloseQuestor: Entered Traveler - making sure we will be docked at Home Station");
                    }
                    AvoidBumpingThings();
                    TravelToAgentsStation();
                    
                    if (_traveler.State == TravelerState.AtDestination || DateTime.Now.Subtract(Cache.Instance.EnteredCloseQuestor_DateTime).TotalMinutes > 10)
                    {
                        //Logging.Log("QuestorState.CloseQuestor: At Station: Docked");
                        // Write to Session log
                        if (!Statistics.WriteSessionLogClosing()) break;

                        if (Settings.Instance.AutoStart) //if autostart is disabled do not schedule a restart of questor - let it stop gracefully.
                        {
                            if (Cache.Instance.CloseQuestorCMDLogoff)
                            {
                                if (CloseQuestorflag)
                                {
                                    Logging.Log("Questor: We are in station: Logging off EVE: In theory eve and questor will restart on their own when the client comes back up");
                                    LavishScript.ExecuteCommand("uplink echo Logging off EVE:  \\\"${Game}\\\" \\\"${Profile}\\\"");
                                    Logging.Log("Questor: you can change this option by setting the wallet and eveprocessmemoryceiling options to use exit instead of logoff: see the settings.xml file");
                                    Logging.Log("Questor: Logging Off eve in 15 seconds.");
                                    CloseQuestorflag = false;
                                    CloseQuestorDelay = DateTime.Now.AddSeconds((int)Time.CloseQuestorDelayBeforeExit_seconds);
                                }
                                if (CloseQuestorDelay.AddSeconds(-10) < DateTime.Now)
                                {
                                    Logging.Log("Questor: Exiting eve in 10 seconds");
                                }
                                if (CloseQuestorDelay < DateTime.Now)
                                {
                                    Logging.Log("Questor: Exiting eve now.");
                                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdLogOff);
                                }
                                Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdLogOff);
                                break;
                            }
                            if (Cache.Instance.CloseQuestorCMDExitGame)
                            {
                                //Logging.Log("Questor: We are in station: Exit option has been configured.");
                                if ((Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet) && (Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile))
                                {
                                    Logging.Log("Questor: We are in station: Don't be silly you cant use both the CloseQuestorCMDUplinkIsboxerProfile and the CloseQuestorCMDUplinkIsboxerProfile setting, choose one");
                                }
                                else
                                {
                                    if (Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile) //if configured as true we will use the innerspace profile to restart this session
                                    {
                                        //Logging.Log("Questor: We are in station: CloseQuestorCMDUplinkInnerspaceProfile is ["+ CloseQuestorCMDUplinkInnerspaceProfile.tostring() +"]");
                                        if (_closeQuestorCMDUplink)
                                        {
                                            Logging.Log("Questor: We are in station: Starting a timer in the innerspace uplink to restart this innerspace profile session");
                                            LavishScript.ExecuteCommand("uplink exec timedcommand 350 open \\\"${Game}\\\" \\\"${Profile}\\\"");
                                            Logging.Log("Questor: Done: quitting this session so the new innerspace session can take over");
                                            Logging.Log("Questor: Exiting eve in 15 seconds.");
                                            _closeQuestorCMDUplink = false;
                                            CloseQuestorDelay = DateTime.Now.AddSeconds((int)Time.CloseQuestorDelayBeforeExit_seconds);
                                        }
                                        if ((CloseQuestorDelay.AddSeconds(-10) == DateTime.Now) && (!_closeQuestor10SecWarningDone))
                                        {
                                            _closeQuestor10SecWarningDone = true;
                                            Logging.Log("Questor: Exiting eve in 10 seconds");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        if (CloseQuestorDelay < DateTime.Now)
                                        {
                                            Logging.Log("Questor: Exiting eve now.");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        return;
                                    }
                                    else if (Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet) //if configured as true we will use isboxer to restart this session
                                    {
                                        //Logging.Log("Questor: We are in station: CloseQuestorCMDUplinkIsboxerProfile is ["+ CloseQuestorCMDUplinkIsboxerProfile.tostring() +"]");
                                        if (_closeQuestorCMDUplink)
                                        {
                                            Logging.Log("Questor: We are in station: Starting a timer in the innerspace uplink to restart this isboxer character set");
                                            LavishScript.ExecuteCommand("uplink timedcommand 350 runscript isboxer -launch \\\"${ISBoxerCharacterSet}\\\"");
                                            Logging.Log("Questor: Done: quitting this session so the new isboxer session can take over");
                                            Logging.Log("Questor: We are in station: Exiting eve.");
                                            _closeQuestorCMDUplink = false;
                                            CloseQuestorDelay = DateTime.Now.AddSeconds((int)Time.CloseQuestorDelayBeforeExit_seconds);
                                        }
                                        if ((CloseQuestorDelay.AddSeconds(-10) == DateTime.Now) && (!_closeQuestor10SecWarningDone))
                                        {
                                            _closeQuestor10SecWarningDone = true;
                                            Logging.Log("Questor: Exiting eve in 10 seconds");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        if (CloseQuestorDelay < DateTime.Now)
                                        {
                                            Logging.Log("Questor: Exiting eve now.");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        return;
                                    }
                                    else if (!Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile && !Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet)
                                    {
                                        Logging.Log("Questor: CloseQuestorCMDUplinkInnerspaceProfile and CloseQuestorCMDUplinkIsboxerProfile both false");
                                        if (_closeQuestorCMDUplink)
                                        {
                                            _closeQuestorCMDUplink = false;
                                            CloseQuestorDelay = DateTime.Now.AddSeconds((int)Time.CloseQuestorDelayBeforeExit_seconds);
                                        }
                                        if ((CloseQuestorDelay.AddSeconds(-10) == DateTime.Now) && (!_closeQuestor10SecWarningDone))
                                        {
                                            _closeQuestor10SecWarningDone = true;
                                            Logging.Log("Questor: Exiting eve in 10 seconds");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        if (CloseQuestorDelay < DateTime.Now)
                                        {
                                            Logging.Log("Questor: Exiting eve now.");
                                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                        Logging.Log("Autostart is false: Stopping EVE with quit command");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                        break;
                    }
                    if (Settings.Instance.DebugStates)
                        Logging.Log("Traveler.State = " + _traveler.State);
                    break;

                case QuestorState.CompleteMission:
                    if (_agentInteraction.State == AgentInteractionState.Idle)
                    {

                        //Logging.Log("Questor: Starting: Statistics.WriteDroneStatsLog");
                        if (!Statistics.WriteDroneStatsLog()) break;
                        //Logging.Log("Questor: Starting: Statistics.AmmoConsumptionStatistics");
                        if (!Statistics.AmmoConsumptionStatistics()) break;

                        Logging.Log("AgentInteraction: Start Conversation [Complete Mission]");

                        _agentInteraction.State = AgentInteractionState.StartConversation;
                        _agentInteraction.Purpose = AgentInteractionPurpose.CompleteMission;
                    }

                    _agentInteraction.ProcessState();

                    if (Settings.Instance.DebugStates)
                        Logging.Log("AgentInteraction.State = " + _agentInteraction.State);

                    if (_agentInteraction.State == AgentInteractionState.Done)
                    {
                        // Cache.Instance.MissionName = String.Empty;  // Do Not clear the 'current' mission name until after we have done the mission logging
                        _agentInteraction.State = AgentInteractionState.Idle;
                        if (Cache.Instance.CourierMission)
                        {
                            Cache.Instance.CourierMission = false;
                            if (State == QuestorState.CompleteMission)
                            {
                                State = QuestorState.Idle;
                            }
                        }
                        else
                        {
                            if (State == QuestorState.CompleteMission)
                            {
                                State = QuestorState.UnloadLoot;
                            }
                        }
                        return;
                    }
                    break;

                case QuestorState.UnloadLoot:
                    if (_unloadLoot.State == UnloadLootState.Idle)
                    {
                        Logging.Log("Questor: UnloadLoot: Begin");
                        _unloadLoot.State = UnloadLootState.Begin;
                    }

                    _unloadLoot.ProcessState();

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Questor: UnloadLoot.State = " + _unloadLoot.State);

                    if (_unloadLoot.State == UnloadLootState.Done)
                    {
                        Cache.Instance.LootAlreadyUnloaded = true;
                        _unloadLoot.State = UnloadLootState.Idle;
                        Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID);
                        if (_combat.State == CombatState.OutOfAmmo || (!(Cache.Instance.Mission == null || Cache.Instance.Mission.State == (int)MissionState.Offered))) // on mission
                        {
                            Logging.Log("Questor: Unloadloot: We are on mission or out of ammo.");
                            State = QuestorState.Idle;
                            return;
                        }
                        //This salvaging decision tree does not belong here and should be separated out into a different questorstate
                        if (Settings.Instance.AfterMissionSalvaging)
                        {
                            if (Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Count == 0)
                            {
                                Logging.Log("Questor: Unloadloot: No more salvaging bookmarks. Setting FinishedSalvaging Update.");
                                //if (Settings.Instance.CharacterMode == "Salvager")
                                //{
                                //    Logging.Log("Salvager mode set and no bookmarks making delay");
                                //    State = QuestorState.Error; //or salvageonly. need to check difference
                                //}

                                if (Settings.Instance.CharacterMode.ToLower() == "salvage".ToLower())
                                {
                                    Logging.Log("Questor: Unloadloot: Character mode is BookmarkSalvager and no bookmarks salvage.");
                                    //We just need a NextSalvagerSession timestamp to key off of here to add the delay
                                    State = QuestorState.Idle;
                                }
                                else
                                {
                                    //Logging.Log("Questor: Character mode is not salvage going to next mission.");
                                    State = QuestorState.Idle; //add pause here
                                }
                                Statistics.Instance.FinishedSalvaging = DateTime.Now;
                                return;
                            }
                            else //There is at least 1 salvage bookmark
                            {
                                Logging.Log("Questor: Unloadloot: There are [" + Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Count + " ] more salvage bookmarks left to process");
                                // Salvage only after multiple missions have been completed
                                if (Settings.Instance.SalvageMultpleMissionsinOnePass)
                                {
                                    //if we can still complete another mission before the Wrecks disappear and still have time to salvage
                                    if (DateTime.Now.Subtract(Statistics.Instance.FinishedSalvaging).TotalMinutes > ((int)Time.WrecksDisappearAfter_minutes - (int)Time.AverageTimeToCompleteAMission_minutes - (int)Time.AverageTimetoSalvageMultipleMissions_minutes))
                                    {
                                        Logging.Log("Questor: UnloadLoot: The last finished after mission salvaging session was [" + DateTime.Now.Subtract(Statistics.Instance.FinishedSalvaging).TotalMinutes + "] ago ");
                                        Logging.Log("Questor: UnloadLoot: we are after mission salvaging again because it has been at least [" + ((int)Time.WrecksDisappearAfter_minutes - (int)Time.AverageTimeToCompleteAMission_minutes - (int)Time.AverageTimetoSalvageMultipleMissions_minutes) + "] min since the last session. ");
                                        if (State == QuestorState.UnloadLoot)
                                        {
                                            State = QuestorState.BeginAfterMissionSalvaging;
                                            Statistics.Instance.StartedSalvaging = DateTime.Now;
                                            //FIXME: should we be overwriting this timestamp here? What if this is the 3rd run back and fourth to the station?
                                        }
                                    }
                                    else //we are salvaging mission 'in one pass' and it has not been enough time since our last run... do another mission
                                    {
                                        Logging.Log("Questor: UnloadLoot: The last finished after mission salvaging session was [" + DateTime.Now.Subtract(Statistics.Instance.FinishedSalvaging).TotalMinutes + "] ago ");
                                        Logging.Log("Questor: UnloadLoot: we are going to the next mission because it has not been [" + ((int)Time.WrecksDisappearAfter_minutes - (int)Time.AverageTimeToCompleteAMission_minutes - (int)Time.AverageTimetoSalvageMultipleMissions_minutes) + "] min since the last session. ");
                                        Statistics.Instance.FinishedMission = DateTime.Now;
                                        if (State == QuestorState.UnloadLoot)
                                        {
                                            State = QuestorState.Idle;
                                        }
                                    }
                                }
                                else //begin after mission salvaging now, rather than later
                                {
                                    if (Settings.Instance.CharacterMode == "salvage".ToLower())
                                    {
                                        Logging.Log("Questor: Unloadloot: CharacterMode: [" + Settings.Instance.CharacterMode + "], AfterMissionSalvaging: [" + Settings.Instance.AfterMissionSalvaging + "], QuestorState: [" + State + "]");
                                        State = QuestorState.BeginAfterMissionSalvaging;
                                        Statistics.Instance.StartedSalvaging = DateTime.Now;
                                    }
                                    else
                                    {
                                        Logging.Log("Questor: UnloadLoot: The last after mission salvaging session was [" + Math.Round(DateTime.Now.Subtract(Statistics.Instance.FinishedSalvaging).TotalMinutes,0) + "min] ago ");
                                        State = QuestorState.BeginAfterMissionSalvaging;
                                        Statistics.Instance.StartedSalvaging = DateTime.Now;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Settings.Instance.CharacterMode.ToLower() == "Combat Missions".ToLower() || Settings.Instance.CharacterMode.ToLower() == "dps".ToLower())
                            {
                                State = QuestorState.Idle;
                                Logging.Log("Questor: Unloadloot: CharacterMode: [" + Settings.Instance.CharacterMode + "], AfterMissionSalvaging: [" + Settings.Instance.AfterMissionSalvaging + "], QuestorState: [" + State + "]");
                                Statistics.Instance.FinishedMission = DateTime.Now;
                                return;
                            }
                        }
                    }
                    break;

                case QuestorState.BeginAfterMissionSalvaging:
                    Statistics.Instance.StartedSalvaging = DateTime.Now; //this will be reset for each "run" between the station and the field if using <unloadLootAtStation>true</unloadLootAtStation>
                    if (DateTime.Now.Subtract(_lastSalvageTrip).TotalMinutes < (int)Time.DelayBetweenSalvagingSessions_minutes && Settings.Instance.CharacterMode.ToLower() == "salvage".ToLower())
                    {
                        Logging.Log("Too early for next salvage trip");
                        break;
                    }
                    Cache.Instance.OpenWrecks = true;
                    if (_arm.State == ArmState.Idle)
                        _arm.State = ArmState.SwitchToSalvageShip;

                    _arm.ProcessState();
                    if (_arm.State == ArmState.Done)
                    {
                        DirectBookmark bookmark = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").OrderBy(b => b.CreatedOn).FirstOrDefault();
                        _arm.State = ArmState.Idle;
                        if (Settings.Instance.FirstSalvageBookmarksInSystem)
                        {
                            Logging.Log("Questor.Salvager: Salvaging at first bookmark from system");
                            bookmark = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").OrderBy(b => b.CreatedOn).FirstOrDefault(c => c.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId);
                        }
                        else Logging.Log("Questor.Salvager: Salvaging at first oldest bookmarks");
                        if (bookmark == null)
                        {
                            bookmark = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").OrderBy(b => b.CreatedOn).FirstOrDefault();
                            if (bookmark == null)
                            {
                                State = QuestorState.Idle;
                                return;
                            }
                        }

                        if (State == QuestorState.BeginAfterMissionSalvaging)
                        {
                            State = QuestorState.GotoSalvageBookmark;
                            _lastSalvageTrip = DateTime.Now;
                        }
                        _traveler.Destination = new BookmarkDestination(bookmark);
                        return;
                    }
                    break;

                case QuestorState.GotoSalvageBookmark:
                    _traveler.ProcessState();
                    string target = "Acceleration Gate";
                    IEnumerable<EntityCache> targets;
                    Cache.Instance.EntitiesByName(target);
                    if (_traveler.State == TravelerState.AtDestination || GateInSalvage())
                    {
                        //we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;

                        if (State == QuestorState.GotoSalvageBookmark)
                        {
                            State = QuestorState.Salvage;
                        }
                        _traveler.Destination = null;
                        return;
                    }

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Traveler.State = " + _traveler.State);
                    break;

                case QuestorState.Salvage:
                    DirectContainer salvageCargo = Cache.Instance.DirectEve.GetShipsCargo();
                    Cache.Instance.SalvageAll = true;
                    Cache.Instance.OpenWrecks = true;

                    const int distancetoccheck = (int)Distance.OnGridWithMe;
                    // is there any NPCs within distancetoconsidertargets?
                    EntityCache deadlyNPC = Cache.Instance.Entities.Where(t => t.Distance < distancetoccheck && !t.IsEntityIShouldLeaveAlone && !t.IsContainer && t.IsNpc && t.CategoryId == (int)CategoryID.Entity && t.GroupId != (int)Group.LargeCollidableStructure).OrderBy(t => t.Distance).FirstOrDefault();

                    if (deadlyNPC != null)
                    {
                        // found NPCs that will likely kill out fragile salvage boat!
                        List<DirectBookmark> missionSalvageBookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                        Logging.Log("Questor.Salvage: could not be completed because of NPCs left in the mission: deleting salvage bookmarks");
                        bool _deleteBookmarkWithNpc_tmp = false;
                        if (_deleteBookmarkWithNpc_tmp)
                        {
                            while (true)
                            {
                                // Remove all bookmarks from address book
                                DirectBookmark pocketSalvageBookmark = missionSalvageBookmarks.FirstOrDefault(b => Cache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distance.DirectionalScannerCloseRange);
                                if (pocketSalvageBookmark == null)
                                    break;
                                else
                                {
                                    pocketSalvageBookmark.Delete();
                                    missionSalvageBookmarks.Remove(pocketSalvageBookmark);
                                }
                                return;
                            }
                        }
                        if (!missionSalvageBookmarks.Any())
                        {
                            Logging.Log("Questor.Salvage: could not be completed because of NPCs left in the mission: salvage bookmarks deleted");
                            Cache.Instance.SalvageAll = false;
                            if (State == QuestorState.Salvage)
                            {
                                Statistics.Instance.FinishedSalvaging = DateTime.Now;
                                State = QuestorState.GotoBase;
                            }
                            return;
                        }
                    }
                    else
                    {
                        if (!Cache.OpenCargoHold("Questor: Salvage")) break;
                        
                        if (Settings.Instance.UnloadLootAtStation && salvageCargo.IsReady && (salvageCargo.Capacity - salvageCargo.UsedCapacity) < 100)
                        {
                            Logging.Log("Questor.Salvage: We are full, go to base to unload");
                            if (State == QuestorState.Salvage)
                            {
                                State = QuestorState.GotoBase;
                            }
                            break;
                        }

                        if (!Cache.Instance.UnlootedContainers.Any())
                        {
                            Logging.Log("Questor.Salvage: Finished salvaging the room");

                            bool gatesInRoom = GateInSalvage();
                            List<DirectBookmark> bookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");

                            while (true)
                            {
                                // Remove all bookmarks from address book
                                var bookmark = bookmarks.FirstOrDefault(b => Cache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distance.OnGridWithMe);
                                if (!gatesInRoom && _gatesPresent) // if there were gates, but we've gone through them all, delete all bookmarks
                                    bookmark = bookmarks.FirstOrDefault();
                                else if (gatesInRoom)
                                    break;
                                if (bookmark == null)
                                    break;

                                bookmark.Delete();
                                bookmarks.Remove(bookmark);
                                Cache.Instance.NextRemoveBookmarkAction = DateTime.Now.AddSeconds((int)Time.RemoveBookmarkDelay_seconds);
                                return;
                            }

                            if (bookmarks.Count == 0 && !gatesInRoom)
                            {
                                Logging.Log("Questor.Salvage: We have salvaged all bookmarks, go to base");
                                Cache.Instance.SalvageAll = false;
                                if (State == QuestorState.Salvage)
                                {
                                    Statistics.Instance.FinishedSalvaging = DateTime.Now;
                                    State = QuestorState.GotoBase;
                                }
                                return;
                            }
                            else
                            {
                                if (!gatesInRoom)
                                {
                                    Logging.Log("Questor.Salvage: Go to the next salvage bookmark");
                                    var bookmark = bookmarks.FirstOrDefault(c => c.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId) ?? bookmarks.FirstOrDefault();
                                    if (State == QuestorState.Salvage)
                                    {
                                        State = QuestorState.GotoSalvageBookmark;
                                    }
                                    _traveler.Destination = new BookmarkDestination(bookmark);
                                }
                                else if (Settings.Instance.UseGatesInSalvage)
                                {
                                    Logging.Log("Questor.Salvage: Acceleration gate found - moving to next pocket");
                                    if (State == QuestorState.Salvage)
                                    {
                                        State = QuestorState.SalvageUseGate;
                                    }
                                }
                                else
                                {
                                    Logging.Log("Questor.Salvage: Acceleration gate found, useGatesInSalvage set to false - Returning to base");
                                    if (State == QuestorState.Salvage)
                                    {
                                        Statistics.Instance.FinishedSalvaging = DateTime.Now;
                                        State = QuestorState.GotoBase;
                                    }
                                    _traveler.Destination = null;
                                }
                            }
                            break;
                        }
                        //we __cannot ever__ approach in salvage.cs so this section _is_ needed.
                        EntityCache closestWreck = Cache.Instance.UnlootedContainers.First();
                        if (Math.Round(closestWreck.Distance, 0) > (int)Distance.SafeScoopRange && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closestWreck.Id))
                        {
                            if (closestWreck.Distance > (int)Distance.WarptoDistance)
                            {
                                if (DateTime.Now > Cache.Instance.NextWarpTo)
                                {
                                    Logging.Log("Questor.Salvage: Warping to [" + closestWreck.Name + "] which is [" + Math.Round(closestWreck.Distance / 1000, 0) + "k away]");
                                    closestWreck.WarpTo();
                                    Cache.Instance.NextWarpTo = DateTime.Now.AddSeconds((int)Time.WarptoDelay_seconds);
                                }
                            }
                            else
                            {
                                if (Cache.Instance.NextApproachAction < DateTime.Now && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closestWreck.Id))
                                {
                                    Logging.Log("Questor.Salvage: Approaching [" + closestWreck.Name + "] which is [" + Math.Round(closestWreck.Distance / 1000, 0) + "k away]");
                                    closestWreck.Approach();
                                    Cache.Instance.NextApproachAction = DateTime.Now.AddSeconds((int)Time.ApproachDelay_seconds);
                                }
                            }
                        }
                        else if (closestWreck.Distance <= (int)Distance.SafeScoopRange && Cache.Instance.Approaching != null)
                        {
                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                            Logging.Log("Questor.Questor: Salvage: Stop ship, ClosestWreck [" + Math.Round(closestWreck.Distance, 0) + "] is in scooprange + [" + (int)Distance.SafeScoopRange + "] and we were approaching");
                        }
                        try
                        {
                            // Overwrite settings, as the 'normal' settings do not apply
                            _salvage.MaximumWreckTargets = Math.Min(Cache.Instance.DirectEve.ActiveShip.MaxLockedTargets, Cache.Instance.DirectEve.Me.MaxLockedTargets);
                            _salvage.ReserveCargoCapacity = 80;
                            _salvage.LootEverything = true;
                            _salvage.ProcessState();
                            //Logging.Log("number of max cache ship: " + Cache.Instance.DirectEve.ActiveShip.MaxLockedTargets);
                            //Logging.Log("number of max cache me: " + Cache.Instance.DirectEve.Me.MaxLockedTargets);
                            //Logging.Log("number of max math.min: " + _salvage.MaximumWreckTargets);
                        }
                        finally
                        {
                            ApplySettings();
                        }
                    }
                    break;

                //case QuestorState.ScoopStep1SetupBookmarkLocation:
                //    //if (_arm.State == ArmState.Idle)
                //    //    _arm.State = ArmState.SwitchToLootWrecksShip;
                //    //_arm.ProcessState();
                //    //if (_arm.State == ArmState.Done)
                //    //{
                //    _arm.State = ArmState.Idle;
                //    var Scoopbookmark = Cache.Instance.BookmarksByLabel("ScoopSpot").OrderBy(b => b.CreatedOn).FirstOrDefault();
                //    if (Scoopbookmark == null)
                //    {
                //        Logging.Log("Bookmark named [ ScoopSpot ] not found");
                //        State = QuestorState.Idle;
                //        break;
                //    }
                //
                //    State = QuestorState.ScoopStep2GotoScoopBookmark;
                //    _traveler.Destination = new BookmarkDestination(Scoopbookmark);
                //
                //    //}
                //    break;

                //case QuestorState.ScoopStep2GotoScoopBookmark:
                //
                //
                //    _traveler.ProcessState();
                //    if (_traveler.State == TravelerState.AtDestination)
                //    {
                //        State = QuestorState.ScoopStep3WaitForWrecks;
                //        _scoop.State = ScoopState.LootHostileWrecks;
                //        _traveler.Destination = null;
                //    }
                //
                //    if (Settings.Instance.DebugStates)
                //        Logging.Log("Traveler.State = " + _traveler.State);
                //    break;
                //
                //case QuestorState.ScoopStep3WaitForWrecks:
                //    // We are not in space yet, wait...
                //    if (!Cache.Instance.InSpace)
                //        break;
                //
                //    //
                //    // Loot All wrecks on grid of 'here'
                //    //
                //    var MyScoopshipCargo = Cache.Instance.DirectEve.GetShipsCargo();
                //
                //    // Is our cargo window open?
                //    if (MyScoopshipCargo.Window == null)
                //    {
                //        // No, command it to open
                //        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
                //        break;
                //    }
                //
                //    //if (MyScoopshipCargo.IsReady && (MyScoopshipCargo.Capacity - MyScoopshipCargo.UsedCapacity) < 3500)
                //    //{
                //    //Logging.Log("Salvage: We are full, go to base to unload");
                //    //this needs to be changed to dock at the closest station
                //    //State = QuestorState . DockAtNearestStation;
                //    //    break;
                //    //}
                //
                //    if (Cache.Instance.UnlootedContainers.Count() == 0)
                //    {
                //        break;
                //    }
                //    var closestWreck2 = Cache.Instance.UnlootedWrecksAndSecureCans.First();
                //    if (closestWreck2.Distance > (int)Distance.SafeScoopRange && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closestWreck2.Id))
                //    {
                //        if (closestWreck2.Distance > (int)Distance.WarptoDistance)
                //        {
                //            closestWreck2.WarpTo();
                //            break;
                //        }
                //        else
                //            closestWreck2.Approach();
                //    }
                //    else if (closestWreck2.Distance <= (int)Distance.SafeScoopRange && Cache.Instance.Approaching != null)
                //        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                //        Logging.Log("Questor: ScoopStep3WaitForWrecks: Stop ship, ClosestWreck [" + closestWreck2.Distance + "] is in scooprange + [" + (int)Distance.SafeScoopRange + "] and we were approaching");
                //
                //    try
                //    {
                //        // Overwrite settings, as the 'normal' settings do not apply
                //        _scoop.MaximumWreckTargets = Math.Min(Cache.Instance.DirectEve.ActiveShip.MaxLockedTargets, Cache.Instance.DirectEve.Me.MaxLockedTargets);
                //        _scoop.ReserveCargoCapacity = 5;
                //        _scoop.ProcessState();
                //    }
                //    finally
                //    {
                //        ApplySettings();
                //    }
                //    break;

                case QuestorState.SalvageUseGate:
                    Cache.Instance.OpenWrecks = true;

                    target = "Acceleration Gate";
                    targets = Cache.Instance.EntitiesByName(target);
                    if (targets == null || !targets.Any())
                    {
                        if (State == QuestorState.SalvageUseGate)
                        {
                            State = QuestorState.GotoSalvageBookmark;
                        }
                        return;
                    }

                    _lastX = Cache.Instance.DirectEve.ActiveShip.Entity.X;
                    _lastY = Cache.Instance.DirectEve.ActiveShip.Entity.Y;
                    _lastZ = Cache.Instance.DirectEve.ActiveShip.Entity.Z;

                    EntityCache closest = targets.OrderBy(t => t.Distance).First();
                    if (closest.Distance < (int)Distance.DecloakRange)
                    {
                        Logging.Log("Questor.Salvage: Acceleration gate found - GroupID=" + closest.GroupId);

                        // Activate it and move to the next Pocket
                        closest.Activate();

                        // Do not change actions, if NextPocket gets a timeout (>2 mins) then it reverts to the last action
                        Logging.Log("Questor.Salvage: Activate [" + closest.Name + "] and change state to 'NextPocket'");

                        if (State == QuestorState.SalvageUseGate)
                        {
                            State = QuestorState.SalvageNextPocket;
                        }
                        _lastPulse = DateTime.Now;
                        return;
                    }
                    else
                    {
                        if (closest.Distance < (int)Distance.WarptoDistance)
                        {
                            // Move to the target
                            if (Cache.Instance.NextApproachAction < DateTime.Now && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closest.Id))
                            {
                                Cache.Instance.NextApproachAction = DateTime.Now.AddSeconds((int)Time.ApproachDelay_seconds);
                                Logging.Log("Questor.Salvage: Approaching target [" + closest.Name + "][ID: " + closest.Id + "][" + Math.Round(closest.Distance / 1000, 0) + "k away]");
                                closest.Approach();
                            }
                        }
                        else
                        {
                            // Probably never happens
                            if (DateTime.Now > Cache.Instance.NextWarpTo)
                            {
                                Logging.Log("Questor.Salvage: Warping to [" + closest.Name + "] which is [" + Math.Round(closest.Distance / 1000, 0) + "k away]");
                                closest.WarpTo();
                                Cache.Instance.NextWarpTo = DateTime.Now.AddSeconds((int)Time.WarptoDelay_seconds);
                            }
                        }
                    }
                    _lastPulse = DateTime.Now.AddSeconds(10);
                    break;

                case QuestorState.SalvageNextPocket:
                    Cache.Instance.OpenWrecks = true;
                    double distance = Cache.Instance.DistanceFromMe(_lastX, _lastY, _lastZ);
                    if (distance > (int)Distance.NextPocketDistance)
                    {
                        //we know we are connected here...
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;

                        Logging.Log("Questor.Salvage: We've moved to the next Pocket [" + Math.Round(distance / 1000, 0) + "k away]");

                        if (State == QuestorState.SalvageNextPocket)
                        {
                            State = QuestorState.Salvage;
                        }
                        return;
                    }
                    else //we havent moved to the next pocket quite yet
                    {
                        if (DateTime.Now.Subtract(_lastPulse).TotalMinutes > 2)
                        {
                            Logging.Log("Questor.Salvage: We've timed out, retry last action");
                            // We have reached a timeout, revert to ExecutePocketActions (e.g. most likely Activate)
                            if (State == QuestorState.SalvageNextPocket)
                            {
                                State = QuestorState.SalvageUseGate;
                            }
                        }
                    }
                    break;

                case QuestorState.Storyline:
                    _storyline.ProcessState();

                    if (_storyline.State == StorylineState.Done)
                    {
                        Logging.Log("Questor: We have completed the storyline, returning to base");
                        if (State == QuestorState.Storyline)
                        {
                            State = QuestorState.GotoBase;
                        }
                        break;
                    }
                    break;

                case QuestorState.CourierMission:

                    if (_courier.State == CourierMissionState.Idle)
                        _courier.State = CourierMissionState.GotoPickupLocation;

                    _courier.ProcessState();

                    if (_courier.State == CourierMissionState.Done)
                    {
                        _courier.State = CourierMissionState.Idle;
                        Cache.Instance.CourierMission = false;
                        if (State == QuestorState.CourierMission)
                        {
                            State = QuestorState.GotoBase;
                        }
                    }
                    break;

                case QuestorState.DebugCloseQuestor:
                    //Logging.Log("ISBoxerCharacterSet: " + Settings.Instance.Lavish_ISBoxerCharacterSet);
                    //Logging.Log("Profile: " + Settings.Instance.Lavish_InnerspaceProfile);
                    //Logging.Log("Game: " + Settings.Instance.Lavish_Game);
                    Logging.Log("CloseQuestorCMDUplinkInnerspaceProfile: " + Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile);
                    Logging.Log("CloseQuestorCMDUplinkISboxerCharacterSet: " + Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet);
                    Logging.Log("walletbalancechangelogoffdelay: " + Settings.Instance.Walletbalancechangelogoffdelay);
                    Logging.Log("walletbalancechangelogoffdelayLogofforExit: " + Settings.Instance.WalletbalancechangelogoffdelayLogofforExit);
                    Logging.Log("walletbalancechangelogoffdelayLogofforExit: " + Settings.Instance.WalletbalancechangelogoffdelayLogofforExit);
                    Logging.Log("EVEProcessMemoryCeiling: " + Settings.Instance.EVEProcessMemoryCeiling);
                    Logging.Log("EVEProcessMemoryCielingLogofforExit: " + Settings.Instance.EVEProcessMemoryCeilingLogofforExit);
                    if (State == QuestorState.SalvageNextPocket)
                    {
                        State = QuestorState.Error;
                    }
                    return;

                case QuestorState.DebugWindows:
                    List<DirectWindow> windows = Cache.Instance.Windows;

                    foreach (DirectWindow window in windows)
                    {
                        Logging.Log("Debug_Questor_WindowNames: [" + window.Name + "]");
                    }
                    foreach (DirectWindow window in windows)
                    {
                        Logging.Log("Debug_Windowcaptions: [" + window.Name + window.Caption + "]");
                    }
                    foreach (DirectWindow window in windows)
                    {
                        Logging.Log("Debug_WindowTypes: [" + window.Name + window.Type + "]");
                    }
                    foreach (DirectWindow window in windows)
                    {
                        Logging.Log("Debug_Questor_WindowNames: [" + window.Name + "]");
                        Logging.Log("Debug_WindowTypes: [" + window.Html + "]");
                    }
                    if (State == QuestorState.DebugWindows)
                    {
                        State = QuestorState.Error;
                    }
                    return;

                case QuestorState.SalvageOnly:
                    //I think this should be repurposed and renamed to better operate in 0.0 or lowsec with a station in local
                    Cache.Instance.OpenWrecks = true;

                    if (!Cache.OpenCargoHold("Questor: SalvageOnly")) break;

                    if (!Cache.Instance.UnlootedContainers.Any())
                    {
                        Logging.Log("Questor.SalvageOnly: Finished salvaging the room");

                        List<DirectBookmark> bookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                        if (bookmarks.Count == 0)
                        {
                            Logging.Log("Questor.SalvageOnly: We have salvaged all bookmarks, waiting.");
                            if (State == QuestorState.SalvageOnly)
                            {
                                State = QuestorState.Idle;
                            }
                        }
                    }
                    EntityCache closestWreck2 = Cache.Instance.UnlootedContainers.First();
                    if (closestWreck2.Distance > (int)Distance.SafeScoopRange && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closestWreck2.Id))
                    {
                        if (closestWreck2.Distance > (int)Distance.WarptoDistance)
                        {
                            if (DateTime.Now > Cache.Instance.NextWarpTo)
                            {
                                Logging.Log("Questor.SalvageOnly: Warping to [" + closestWreck2.Name + "] which is [" + Math.Round(closestWreck2.Distance, 0) + "] meters away");
                                closestWreck2.WarpTo();
                                Cache.Instance.NextWarpTo = DateTime.Now.AddSeconds((int)Time.WarptoDelay_seconds);
                            }
                        }
                        else
                        {
                            Logging.Log("Questor.SalvageOnly: Warping to [" + closestWreck2.Name + "] which is [" + Math.Round(closestWreck2.Distance / 1000, 0) + "k away]");
                            closestWreck2.Approach();
                        }
                    }
                    else if (closestWreck2.Distance <= (int)Distance.SafeScoopRange && Cache.Instance.Approaching != null)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                        Logging.Log("Questor.SalvageOnly: Stop ship, ClosestWreck [" + Math.Round(closestWreck2.Distance, 0) + "] is in scooprange + [" + (int)Distance.SafeScoopRange + "] and we were approaching");
                    }

                    try
                    {
                        // Overwrite settings, as the 'normal' settings do not apply
                        _salvage.MaximumWreckTargets = Math.Min(Cache.Instance.DirectEve.ActiveShip.MaxLockedTargets, Cache.Instance.DirectEve.Me.MaxLockedTargets);
                        _salvage.ReserveCargoCapacity = 80;
                        _salvage.LootEverything = true;
                        _salvage.ProcessState();
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    }
                    finally
                    {
                        ApplySettings();
                    }
                    break;

                case QuestorState.GotoSalvageOnlyBookmarinLocal:
                    _traveler.ProcessState();
                    if (_traveler.State == TravelerState.AtDestination)
                    {
                        if (State == QuestorState.GotoSalvageOnlyBookmarinLocal)
                        {
                            State = QuestorState.SalvageOnlyBookmarksinLocal;
                        }
                        _traveler.Destination = null;
                    }
                    if (Settings.Instance.DebugStates)
                        Logging.Log("Traveler.State = " + _traveler.State);
                    break;

                case QuestorState.SalvageOnlyBookmarksinLocal:
                    DirectContainer salvageOnlyBookmarksCargo = Cache.Instance.DirectEve.GetShipsCargo();
                    if (Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").FirstOrDefault() == null)
                    {
                        break;
                    }
                    if (Cache.Instance.InStation)
                    {
                        // We are in a station,
                        Logging.Log("Questor.SalvageOnlyBookmarksinLocal: We're docked, undocking");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                        break;
                    }

                    if (!Cache.OpenCargoHold("Questor: SalvageOnlyBookmarksinLocal")) break;

                    if (Settings.Instance.UnloadLootAtStation && salvageOnlyBookmarksCargo.IsReady && (salvageOnlyBookmarksCargo.Capacity - salvageOnlyBookmarksCargo.UsedCapacity) < 100)
                    {
                        Logging.Log("Questor.SalvageOnlyBookmarksinLocal: We are full");
                        if (State == QuestorState.SalvageOnlyBookmarksinLocal)
                        {
                            State = QuestorState.GotoBase;
                        }
                        return;
                    }
                    if (!Cache.Instance.UnlootedContainers.Any())
                    {
                        Logging.Log("Questor.SalvageOnlyBookmarksinLocal: Finished salvaging the room");
                        List<DirectBookmark> bookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                        while (true)
                        {
                            // Remove all bookmarks from address book
                            DirectBookmark bookmark = bookmarks.FirstOrDefault(b => Cache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distance.OnGridWithMe);
                            if (bookmark == null)
                                break;
                            bookmark.Delete();
                            bookmarks.Remove(bookmark);
                            Cache.Instance.NextRemoveBookmarkAction = DateTime.Now.AddSeconds((int)Time.RemoveBookmarkDelay_seconds);
                            return;
                        }

                        if (bookmarks.Count == 0)
                        {
                            Logging.Log("Questor.SalvageOnlyBookmarksInLocal: We have salvaged all bookmarks. Going to base. ");
                            if (State == QuestorState.SalvageOnlyBookmarksinLocal)
                            {
                                State = QuestorState.GotoBase;
                            }
                        }
                        else
                        {
                            Logging.Log("Questor.SalvageOnlyBookmarksInLocal: Go to the next salvage bookmark");
                            _traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).First());
                            if (State == QuestorState.SalvageOnlyBookmarksinLocal)
                            {
                                State = QuestorState.GotoSalvageOnlyBookmarinLocal;
                            }
                            return;
                        }
                        break;
                    }
                    EntityCache closestWreck3 = Cache.Instance.UnlootedContainers.First();
                    if (closestWreck3.Distance > (int)Distance.SafeScoopRange && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closestWreck3.Id))
                    {
                        if (closestWreck3.Distance > (int)Distance.WarptoDistance)
                        {
                            if (DateTime.Now > Cache.Instance.NextWarpTo)
                            {
                                Logging.Log("Questor.SalvageOnlyBookmarksInLocal: Warping to [" + closestWreck3.Name + "] which is [" + Math.Round(closestWreck3.Distance / 1000, 0) + "k away]");
                                closestWreck3.WarpTo();
                                Cache.Instance.NextWarpTo = DateTime.Now.AddSeconds((int)Time.TravelerInWarpedNextCommandDelay_seconds);
                            }
                        }
                        else
                        {
                            Logging.Log("Questor.SalvageOnlyBookmarksInLocal: Approaching [" + closestWreck3.Name + "] which is [" + Math.Round(closestWreck3.Distance / 1000, 0) + "k away]");
                            closestWreck3.Approach();
                            Cache.Instance.NextApproachAction = DateTime.Now.AddSeconds((int)Time.ApproachDelay_seconds);
                        }
                    }
                    else if (closestWreck3.Distance <= (int)Distance.SafeScoopRange && Cache.Instance.Approaching != null)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                        Logging.Log("Questor.SalvageOnlyBookmarksInLocal: Stop ship, ClosestWreck [" + Math.Round(closestWreck3.Distance, 0) + "] is in scooprange + [" + (int)Distance.SafeScoopRange + "] and we were approaching");
                    }

                    try
                    {
                        // Overwrite settings, as the 'normal' settings do not apply
                        _salvage.MaximumWreckTargets = Math.Min(Cache.Instance.DirectEve.ActiveShip.MaxLockedTargets,
                            Cache.Instance.DirectEve.Me.MaxLockedTargets);
                        _salvage.ReserveCargoCapacity = 80;
                        _salvage.LootEverything = true;
                        _salvage.ProcessState();
                    }
                    finally
                    {
                        ApplySettings();
                    }
                    break;

                case QuestorState.Traveler:
                    Cache.Instance.OpenWrecks = false;
                    List<long> destination = Cache.Instance.DirectEve.Navigation.GetDestinationPath();
                    if (destination == null || destination.Count == 0)
                    {
                        // happens if autopilot isn't set and this questorstate is choosen manually
                        // this also happens when we get to destination (!?)
                        Logging.Log("QuestorState.Traveler: No destination?");
                        if (State == QuestorState.Traveler)
                        {
                            State = QuestorState.Error;
                        }
                        return;
                    }
                    else
                        if (destination.Count == 1 && destination.First() == 0)
                            destination[0] = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                    if (_traveler.Destination == null || _traveler.Destination.SolarSystemId != destination.Last())
                    {
                        IEnumerable<DirectBookmark> bookmarks = Cache.Instance.DirectEve.Bookmarks.Where(b => b.LocationId == destination.Last());
                        if (bookmarks != null && bookmarks.Any())
                            _traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).First());
                        else
                        {
                            Logging.Log("QuestorState.Traveler: Destination: [" + Cache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                            _traveler.Destination = new SolarSystemDestination(destination.Last());
                        }
                    }
                    else
                    {
                        _traveler.ProcessState();
                        //we also assume you are connected during a manual set of questor into travel mode (safe assumption considering someone is at the kb)
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;

                        if (_traveler.State == TravelerState.AtDestination)
                        {
                            if (_missionController.State == MissionControllerState.Error)
                            {
                                Logging.Log("QuestorState.Traveler: an error has occurred");
                                if (State == QuestorState.Traveler)
                                {
                                    State = QuestorState.Error;
                                }
                                return;
                            }
                            else if (Cache.Instance.InSpace)
                            {
                                Logging.Log("QuestorState.Traveler: Arrived at destination (in space, Questor stopped)");
                                if (State == QuestorState.Traveler)
                                {
                                    State = QuestorState.Error;
                                }
                                return;
                            }
                            else
                            {
                                Logging.Log("QuestorState.Traveler: Arrived at destination");
                                if (State == QuestorState.Traveler)
                                {
                                    State = QuestorState.Idle;
                                }
                                return;
                            }
                        }
                    }
                    break;
                case QuestorState.GotoNearestStation:
                    if (!Cache.Instance.InSpace || Cache.Instance.InWarp) return;
                    var station = Cache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();
                    if (station != null)
                    {
                        if (station.Distance > (int)Distance.WarptoDistance)
                        {
                            Logging.Log("Questor.GotoNearestStation [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                            station.WarpToAndDock();
                            Cache.Instance.NextWarpTo = DateTime.Now.AddSeconds((int)Time.WarptoDelay_seconds);
                            if (State == QuestorState.GotoNearestStation)
                            {
                                State = QuestorState.Salvage;
                            }
                            break;
                        }
                        else
                        {
                            if (station.Distance < 1900)
                            {
                                if (DateTime.Now > Cache.Instance.NextDockAction)
                                {
                                    Logging.Log("Questor.GotoNearestStation [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                                    station.Dock();
                                    Cache.Instance.NextDockAction = DateTime.Now.AddSeconds((int)Time.DockingDelay_seconds);
                                }
                            }
                            else
                            {
                                if (Cache.Instance.NextApproachAction < DateTime.Now && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != station.Id))
                                {
                                    Cache.Instance.NextApproachAction = DateTime.Now.AddSeconds((int)Time.ApproachDelay_seconds);
                                    Logging.Log("Questor.GotoNearestStation Approaching [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                                    station.Approach();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (State == QuestorState.GotoNearestStation)
                        {
                            State = QuestorState.Error; //should we goto idle here?
                        }
                    }
                    break;
            }
        }

        private bool GateInSalvage()
        {
            const string target = "Acceleration Gate";

            var targets = Cache.Instance.EntitiesByName(target);
            if (targets == null || !targets.Any())
                return false;
            _gatesPresent = true;
            return true;
        }
    }
}