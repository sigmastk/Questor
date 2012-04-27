// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.Activities;
using Questor.Modules.States;
using Questor.Modules.Combat;
using Questor.Modules.Actions;
using Questor.Modules.BackgroundTasks;
using Questor.Storylines;
using LavishScriptAPI;

namespace Questor.Behaviors
{
    public class CombatMissionsBehavior
    {
        private readonly AgentInteraction _agentInteraction;
        private readonly Arm _arm;
        private readonly SwitchShip _switchShip;
        private readonly Combat _combat;
        private readonly CourierMissionCtrl _courierMissionCtrl;
        private readonly LocalWatch _localwatch;
        //private readonly Defense _defense;
        private readonly Drones _drones;

        private DateTime _lastPulse;
        private DateTime _lastSalvageTrip = DateTime.MinValue;
        private readonly CombatMissionCtrl _combatMissionCtrl;
        private readonly Panic _panic;
        private readonly Storyline _storyline;
        private readonly Statistics _statistics;
        private readonly Salvage _salvage;
        private readonly Traveler _traveler;
        private readonly UnloadLoot _unloadLoot;
        public DateTime LastAction;
        private readonly Random _random;
        private int _randomDelay;
        public static long AgentID;
        private  Stopwatch _watch;

        private double _lastX;
        private double _lastY;
        private double _lastZ;
        private bool _gatesPresent;
        private bool _firstStart = true;
        public  bool Panicstatereset = false;

        //DateTime _nextAction = DateTime.Now;

        public CombatMissionsBehavior()
        {
            _lastPulse = DateTime.MinValue;

            _random = new Random();
            _salvage = new Salvage();
            _localwatch = new LocalWatch();
            _combat = new Combat();
            _traveler = new Traveler();
            _unloadLoot = new UnloadLoot();
            _agentInteraction = new AgentInteraction();
            _arm = new Arm();
            _courierMissionCtrl = new CourierMissionCtrl();
            _switchShip = new SwitchShip();
            _combatMissionCtrl = new CombatMissionCtrl();
            _drones = new Drones();
            _panic = new Panic();
            _storyline = new Storyline();
            _statistics = new Statistics();
            _watch = new Stopwatch();

            //
            // this is combat mission specific and needs to be generalized
            //
            Settings.Instance.SettingsLoaded += SettingsLoaded;

            // State fixed on ExecuteMission
            State = CombatMissionsBehaviorState.Idle;
            _arm.State = ArmState.Idle;
            _combat.State = CombatState.Idle;
            _drones.State = DroneState.Idle;
            _unloadLoot.State = UnloadLootState.Idle;
        }

        public void SettingsLoaded(object sender, EventArgs e)
        {
            ApplySalvageSettings();
            ValidateCombatMissionSettings();
        }

        public void DebugCombatMissionsBehaviorStates()
        {
            if (Settings.Instance.DebugStates)
                Logging.Log("CombatMissionsBehavior.State = " + State);
        }

        public void DebugPanicstates()
        {
            if (Settings.Instance.DebugStates)
                Logging.Log("Panic.State = " + _panic.State);
        }

        public void DebugPerformanceClearandStartTimer()
        {
            _watch.Reset();
            _watch.Start();
        }

        public void DebugPerformanceStopandDisplayTimer(string whatWeAreTiming)
        {
            _watch.Stop();
            if (Settings.Instance.DebugPerformance)
                Logging.Log(whatWeAreTiming + " took " + _watch.ElapsedMilliseconds + "ms");
        }

        private bool ValidSettings { get; set; }
        public void ValidateCombatMissionSettings()
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
                _combatMissionCtrl.AgentId = agent.AgentId;
                _arm.AgentId = agent.AgentId;
                _statistics.AgentID = agent.AgentId;
                AgentID = agent.AgentId;
            }
        }
        public static CombatMissionsBehaviorState State { get; set; }       
        public bool CloseQuestorflag = true;
        public string CharacterName { get; set; }

        public void ApplySalvageSettings()
        {
            _salvage.Ammo = Settings.Instance.Ammo;
            _salvage.MaximumWreckTargets = Settings.Instance.MaximumWreckTargets;
            _salvage.ReserveCargoCapacity = Settings.Instance.ReserveCargoCapacity;
            _salvage.LootEverything = Settings.Instance.LootEverything;
        }

        private void BeginClosingQuestor()
        {
           Cache.Instance.EnteredCloseQuestor_DateTime = DateTime.Now;
           State = CombatMissionsBehaviorState.Idle;
        }

        public void CheckEVEStatus()
        {
            // get the current process
            Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

            // get the physical mem usage (this only runs between missions)
            Cache.Instance.TotalMegaBytesOfMemoryUsed = ((currentProcess.WorkingSet64 / 1024) / 1024);
            Logging.Log("Questor: EVE instance: totalMegaBytesOfMemoryUsed - " +
                        Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");

            // If Questor window not visible, schedule a restart of questor in the uplink so that the GUI will start normally

            /*
             * 
             if (!m_Parent.Visible)
            //GUI isn't visible and CloseQuestorflag is true, so that his code block only runs once
            {
                //m_Parent.Visible = true; //this does not work for some reason - innerspace issue?
                Cache.Instance.ReasonToStopQuestor =
                    "The Questor GUI is not visible: did EVE get restarted due to a crash or lag?";
                Logging.Log(Cache.Instance.ReasonToStopQuestor);
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cache.Instance.SessionState = "Exiting";
                BeginClosingQuestor();
            }
            else 
          
             */

            if (Cache.Instance.TotalMegaBytesOfMemoryUsed > (Settings.Instance.EVEProcessMemoryCeiling - 50) &&
                        Settings.Instance.EVEProcessMemoryCeilingLogofforExit != "")
            {
                Logging.Log(
                    "Questor: Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " +
                    Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");
                Cache.Instance.ReasonToStopQuestor =
                    "Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " +
                    Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB";
                if (Settings.Instance.EVEProcessMemoryCeilingLogofforExit == "logoff")
                {
                    Cache.Instance.CloseQuestorCMDLogoff = true;
                    Cache.Instance.CloseQuestorCMDExitGame = false;
                    Cache.Instance.SessionState = "LoggingOff";
                    BeginClosingQuestor();
                    return;
                }
                if (Settings.Instance.EVEProcessMemoryCeilingLogofforExit == "exit")
                {
                    Cache.Instance.CloseQuestorCMDLogoff = false;
                    Cache.Instance.CloseQuestorCMDExitGame = true;
                    Cache.Instance.SessionState = "Exiting";
                    BeginClosingQuestor();
                    return;
                }
                Logging.Log(
                    "Questor: EVEProcessMemoryCeilingLogofforExit was not set to exit or logoff - doing nothing ");
            }
            else
            {
                Cache.Instance.SessionState = "Running";
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
            if (!Cache.Instance.InSpace || bigObjects == null || bigObjects.Distance >= (int) Distance.TooCloseToStructure)
            {
                //we are no longer "too close" and can proceed. 
            }
            else
            {
                if (DateTime.Now > Cache.Instance.NextOrbit)
                {
                    bigObjects.Orbit((int) Distance.SafeDistancefromStructure);
                    Logging.Log("QuestorState: " + State + ": initiating Orbit of [" + bigObjects.Name +
                                "] orbiting at [" + Cache.Instance.OrbitDistance + "]");
                    Cache.Instance.NextOrbit = DateTime.Now.AddSeconds((int) Time.OrbitDelay_seconds);
                }
                return; //we are still too close, do not continue through the rest until we are not "too close" anymore
            }
        }


        public void ProcessState()
        {
            // Invalid settings, quit while we're ahead
            if (!ValidSettings)
            {
                if (DateTime.Now.Subtract(LastAction).TotalSeconds < (int)Time.ValidateSettings_seconds) //default is a 15 second interval
                {
                    ValidateCombatMissionSettings();
                    LastAction = DateTime.Now;
                }
                return;
            }

            //If local unsafe go to base and do not start mission again
            if (Settings.Instance.FinishWhenNotSafe && (State != CombatMissionsBehaviorState.GotoNearestStation /*|| State!=QuestorState.GotoBase*/))
            {
                //need to remove spam
                if (Cache.Instance.InSpace && !Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    var station = Cache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();
                    if (station != null)
                    {
                        Logging.Log("Local not safe.Station found. Going to nearest station");
                        if (State != CombatMissionsBehaviorState.GotoNearestStation)
                            State = CombatMissionsBehaviorState.GotoNearestStation;
                    }
                    else
                    {
                        Logging.Log("Local not safe.Station not found. Going back to base");
                        if (State != CombatMissionsBehaviorState.GotoBase)
                            State = CombatMissionsBehaviorState.GotoBase;
                    }
                    Cache.Instance.StopBot = true;
                }
            }
            
            if (Cache.Instance.SessionState == "Quitting")
            {
                    BeginClosingQuestor();
                }
            if (Cache.Instance.GotoBaseNow)
            {
                if (State != CombatMissionsBehaviorState.GotoBase)
                {
                    State = CombatMissionsBehaviorState.GotoBase;
                }
            }
            if ((DateTime.Now.Subtract(Cache.Instance.QuestorStarted_DateTime).TotalSeconds > 10) && (DateTime.Now.Subtract(Cache.Instance.QuestorStarted_DateTime).TotalSeconds < 60))
            {
                if (Cache.Instance.QuestorJustStarted)
                {
                    Cache.Instance.QuestorJustStarted = false;
                    Cache.Instance.SessionState = "Starting Up";

                    // write session log
                    Statistics.WriteSessionLogStarting();
                }
            }
            
            Cache.Instance.InMission = State == CombatMissionsBehaviorState.ExecuteMission;
            if (State == CombatMissionsBehaviorState.Storyline && _storyline.State == StorylineState.ExecuteMission)
            {
                Cache.Instance.InMission |= _storyline.StorylineHandler is GenericCombatStoryline && (_storyline.StorylineHandler as GenericCombatStoryline).State == GenericCombatStorylineState.ExecuteMission;
            }
            //
            // Panic always runs, not just in space
            //
            DebugPerformanceClearandStartTimer();
            _panic.ProcessState();
            DebugPerformanceStopandDisplayTimer("Panic.ProcessState");
            if (_panic.State == PanicState.Panic || _panic.State == PanicState.Panicking)
            {
                // If Panic is in panic state, questor is in panic state :)
                State = State == CombatMissionsBehaviorState.Storyline ? CombatMissionsBehaviorState.StorylinePanic : CombatMissionsBehaviorState.Panic;

                DebugCombatMissionsBehaviorStates();
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
                if (State == CombatMissionsBehaviorState.StorylinePanic)
                {
                    State = CombatMissionsBehaviorState.Storyline;
                    if (_storyline.StorylineHandler is GenericCombatStoryline)
                        (_storyline.StorylineHandler as GenericCombatStoryline).State = GenericCombatStorylineState.GotoMission;
                }
                else
                {
                    // Head back to the mission
                    _traveler.State = TravelerState.Idle;
                    State = CombatMissionsBehaviorState.GotoMission;
                }
                DebugCombatMissionsBehaviorStates();
            }
            DebugPanicstates();

            Logging.Log("test");
            switch (State)
            {
                case CombatMissionsBehaviorState.Idle:
                    // Every 5 min of idle check and make sure we aren't supposed to stop...
                    if (Math.Round(DateTime.Now.Subtract(Cache.Instance.LastTimeCheckAction).TotalMinutes) > 5)
                    {
                        Cache.Instance.LastTimeCheckAction = DateTime.Now;

                        /* 
                        if (Cache.Instance.ExitWhenIdle && !Settings.Instance.AutoStart)
                        {
                            Cache.Instance.ReasonToStopQuestor = "Settings: ExitWhenIdle is true, and we are idle... exiting";
                            Logging.Log(Cache.Instance.ReasonToStopQuestor);
                            Settings.Instance.AutoStart = false;
                            Cache.Instance.CloseQuestorCMDLogoff = false;
                            Cache.Instance.CloseQuestorCMDExitGame = true;
                            Cache.Instance.SessionState = "Exiting";
                            if (State == CombatMissionsModeState.Idle)
                            {
                                BeginClosingQuestor();
                            }
                            return;
                        }
                        */
                    }
                    if (Cache.Instance.StopBot)
                        return;

                    if (Cache.Instance.InSpace)
                    {
                        // Questor does not handle in space starts very well, head back to base to try again
                        Logging.Log("CombatMissionsBehavior: Started questor while in space, heading back to base in 15 seconds");
                        LastAction = DateTime.Now;
                        if (State == CombatMissionsBehaviorState.Idle) State = CombatMissionsBehaviorState.DelayedGotoBase;
                        break;
                    }

                    // only attempt to write the mission statistics logs if one of the mission stats logs is enabled in settings
                    if (Settings.Instance.MissionStats1Log || Settings.Instance.MissionStats3Log || Settings.Instance.MissionStats3Log)
                    {
                        if (!Statistics.Instance.MissionLoggingCompleted)
                        {
                            Statistics.WriteMissionStatistics();
                            break;
                        }
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
                            if (State == CombatMissionsBehaviorState.Idle) State = CombatMissionsBehaviorState.DelayedStart;
                            Logging.Log("CombatMissionsBehavior: Random start delay of [" + _randomDelay + "] seconds");
                            return;
                        }
                        else
                        {
                            if (State == CombatMissionsBehaviorState.Idle) State = CombatMissionsBehaviorState.Cleanup;
                            return;
                        }
                    }
                    break;

                case CombatMissionsBehaviorState.DelayedStart:
                    if (DateTime.Now.Subtract(LastAction).TotalSeconds < _randomDelay)
                        break;

                    _storyline.Reset();
                    if (State == CombatMissionsBehaviorState.DelayedStart) State = CombatMissionsBehaviorState.Cleanup;
                    break;

                case CombatMissionsBehaviorState.DelayedGotoBase:
                    if (DateTime.Now.Subtract(LastAction).TotalSeconds < (int)Time.DelayedGotoBase_seconds)
                        break;

                    Logging.Log("CombatMissionsBehavior: Heading back to base");
                    if (State == CombatMissionsBehaviorState.DelayedGotoBase) State = CombatMissionsBehaviorState.GotoBase;                    
                    break;

                case CombatMissionsBehaviorState.Cleanup:
                    //
                    // this state is needed because forced disconnects
                    // and crashes can leave "extra" cargo in the
                    // cargo hold that is undesirable and causes
                    // problems loading the correct ammo on occasion
                    //
                    if (Cache.Instance.LootAlreadyUnloaded == false)
                    {
                        if (State == CombatMissionsBehaviorState.Cleanup) State = CombatMissionsBehaviorState.GotoBase;
                        break;
                    }
                    else
                    {
                        CheckEVEStatus();
                        if (State == CombatMissionsBehaviorState.Cleanup) State = CombatMissionsBehaviorState.Start;
                        break;
                    }

                case CombatMissionsBehaviorState.Start:
                    if (_firstStart && Settings.Instance.MultiAgentSupport)
                    {
                        //if you are in wrong station and is not first agent
                        if (State == CombatMissionsBehaviorState.Start) State = CombatMissionsBehaviorState.Switch;
                        _firstStart = false;
                        break;
                    }
                    Cache.Instance.OpenWrecks = false;
                    if (_agentInteraction.State == AgentInteractionState.Idle)
                    {
                        Cache.Instance.Wealth = Cache.Instance.DirectEve.Me.Wealth;

                        Cache.Instance.WrecksThisMission = 0;
                        if (Settings.Instance.EnableStorylines && _storyline.HasStoryline())
                        {
                            Logging.Log("CombatMissionsBehavior: Storyline detected, doing storyline.");
                            _storyline.Reset();
                            if (State == CombatMissionsBehaviorState.Start) State = CombatMissionsBehaviorState.Storyline;
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
                        if (State == CombatMissionsBehaviorState.Start) State = CombatMissionsBehaviorState.Arm;
                        return;
                    }

                    if (_agentInteraction.State == AgentInteractionState.ChangeAgent)
                    {
                        _agentInteraction.State = AgentInteractionState.Idle;
                        ValidateCombatMissionSettings();
                        if (State == CombatMissionsBehaviorState.Start) State = CombatMissionsBehaviorState.Switch;
                        break;
                    }

                    break;

                case CombatMissionsBehaviorState.Switch:

                    if (_switchShip.State == SwitchShipState.Idle)
                    {
                        Logging.Log("Switch: Begin");
                        _switchShip.State = SwitchShipState.Begin;
                    }

                    _switchShip.ProcessState();

                    if (_switchShip.State == SwitchShipState.Done)
                    {
                        _switchShip.State = SwitchShipState.Idle;
                        if (State == CombatMissionsBehaviorState.Switch) State = CombatMissionsBehaviorState.GotoBase;
                    }
                    break;

                case CombatMissionsBehaviorState.Arm:
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

                    if (Settings.Instance.DebugStates) Logging.Log("Arm.State = " + _arm.State);

                    if (_arm.State == ArmState.NotEnoughAmmo)
                    {
                        // we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                        // we may be out of drones/ammo but disconnecting/reconnecting will not fix that so update the timestamp
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        Logging.Log("Arm: Armstate.NotEnoughAmmo");
                        _arm.State = ArmState.Idle;
                        if (State == CombatMissionsBehaviorState.Arm)  State = CombatMissionsBehaviorState.Error;
                    }

                    if (_arm.State == ArmState.NotEnoughDrones)
                    {
                        // we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                        // we may be out of drones/ammo but disconnecting/reconnecting will not fix that so update the timestamp
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        Logging.Log("Arm: Armstate.NotEnoughDrones");
                        _arm.State = ArmState.Idle;
                        if (State == CombatMissionsBehaviorState.Arm) State = CombatMissionsBehaviorState.Error;
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
                            if (State == CombatMissionsBehaviorState.Arm) State = CombatMissionsBehaviorState.CourierMission;
                        }
                        else
                        {
                            if (State == CombatMissionsBehaviorState.Arm) State = CombatMissionsBehaviorState.LocalWatch;
                        }
                    }

                    break;

                case CombatMissionsBehaviorState.LocalWatch:
                    if (Settings.Instance.UseLocalWatch)
                    {
                        Cache.Instance.LastLocalWatchAction = DateTime.Now;
                        if (Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                        {
                            Logging.Log("Questor.LocalWatch: local is clear");
                            if (State == CombatMissionsBehaviorState.LocalWatch) State = CombatMissionsBehaviorState.WarpOutStation;
                        }
                        else
                        {
                            Logging.Log("Questor.LocalWatch: Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                            if (State == CombatMissionsBehaviorState.LocalWatch)
                            {
                                State = CombatMissionsBehaviorState.WaitingforBadGuytoGoAway;
                            }
                            Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                            Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        }
                    }
                    else
                    {
                        if (State == CombatMissionsBehaviorState.LocalWatch) State = CombatMissionsBehaviorState.WarpOutStation;
                    }
                    break;

                case CombatMissionsBehaviorState.WaitingforBadGuytoGoAway:
                    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    if (DateTime.Now.Subtract(Cache.Instance.LastLocalWatchAction).TotalMinutes < (int)Time.WaitforBadGuytoGoAway_minutes)
                        break;
                    if (State == CombatMissionsBehaviorState.WaitingforBadGuytoGoAway) State = CombatMissionsBehaviorState.LocalWatch;
                    break;

                case CombatMissionsBehaviorState.WarpOutStation:
                    DirectBookmark warpOutBookmark = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkWarpOut ?? "").OrderByDescending(b => b.CreatedOn).FirstOrDefault(b => b.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId);
                    //DirectBookmark _bookmark = Cache.Instance.BookmarksByLabel(Settings.Instance.bookmarkWarpOut + "-" + Cache.Instance.CurrentAgent ?? "").OrderBy(b => b.CreatedOn).FirstOrDefault();
                    long solarid = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Logging.Log("Questor.WarpOut: No Bookmark");
                        if (State == CombatMissionsBehaviorState.WarpOutStation) State = CombatMissionsBehaviorState.GotoMission;
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
                            if (State == CombatMissionsBehaviorState.WarpOutStation) State = CombatMissionsBehaviorState.GotoMission;
                            _traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Logging.Log("Questor.WarpOut: No Bookmark in System");
                        if (State == CombatMissionsBehaviorState.WarpOutStation) State = CombatMissionsBehaviorState.GotoMission;
                    }
                    break;

                case CombatMissionsBehaviorState.GotoMission:
                    Statistics.Instance.MissionLoggingCompleted = false;
                    var missionDestination = _traveler.Destination as MissionBookmarkDestination;
                    //
                    // this is far from complete - do not enable unless you really like writing code and debugging
                    //
                    //if(Cache.Instance.panic_attempts_this_mission > 0 && !Cache.Instance.MissionIsDeadspace)
                    //{
                    //    var bookmark = Cache.Instance.BookmarksByLabel("spot" + " ").OrderBy(b => b.CreatedOn).FirstOrDefault();
                    //    if (bookmark == null)
                    //    {
                    //        State = CombatMissionsModeState.Idle;
                    //        return;
                    //    }
                    //    Traveler.ProcessState();
                    //    if (Traveler.State == TravelerState.AtDestination)
                    //    {
                    //        Traveler.Destination = null;
                    //        return;
                    //    }
                    //    bookmark.WarpTo(80000);
                    //
                    //    Cache.Instance.BookmarksByLabel("CurrentMission").OrderByDescending(b => b.CreatedOn).Where(b => b.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId).FirstOrDefault();
                    //}
                    //else
                    //{
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
                    //    State = CombatMissionsModeState.Error;
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
                        if (State == CombatMissionsBehaviorState.GotoMission) State = CombatMissionsBehaviorState.ExecuteMission;
                        //var bookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                        //var bookmark = bookmarks.FirstOrDefault(b => Cache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distance.BookmarksOnGridWithMe);
                        //// _bookmark = (Cache.Instance.BookmarksByLabel("warped off").OrderByDescending(b => b.CreatedOn).Where(b => b.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId).FirstOrDefault((b => Cache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distance.BookmarksOnGridWithMe));
                        //if (bookmark != null)
                        //{
                        //    Logging.Log("QuestorState: GotoMission: Pocket already bookmarked [" + bookmark.Title + "]");
                        //    return;
                        //}
                        //// No, create a bookmark
                        //var label = string.Format("{0} {1:HHmm}", "spot", DateTime.UtcNow);
                        //Logging.Log("QuestorState: GotoMission: Bookmarking pocket [" + label + "]");
                        //Cache.Instance.CreateBookmark(label);

                        // Seeing as we just warped to the mission, start the mission controller
                        _combatMissionCtrl.State = CombatMissionCtrlState.Start;
                        _combat.State = CombatState.CheckTargets;
                        _traveler.Destination = null;
                    }
                    break;

                case CombatMissionsBehaviorState.ExecuteMission:
                    DebugPerformanceClearandStartTimer();
                    _combat.ProcessState();
                    DebugPerformanceStopandDisplayTimer("Combat.ProcessState");

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Combat.State = " + _combat.State);

                    DebugPerformanceClearandStartTimer();
                    _drones.ProcessState();
                    DebugPerformanceStopandDisplayTimer("Drones.ProcessState");

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Drones.State = " + _drones.State);

                    DebugPerformanceClearandStartTimer();
                    _salvage.ProcessState();
                    DebugPerformanceStopandDisplayTimer("Salvage.ProcessState");

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Salvage.State = " + _salvage.State);

                    DebugPerformanceClearandStartTimer();
                    _combatMissionCtrl.ProcessState();
                    DebugPerformanceStopandDisplayTimer("MissionController.ProcessState");

                    if (Settings.Instance.DebugStates)
                        Logging.Log("CombatMissionsBehavior.State = " + _combatMissionCtrl.State);

                    // If we are out of ammo, return to base, the mission will fail to complete and the bot will reload the ship
                    // and try the mission again
                    if (_combat.State == CombatState.OutOfAmmo)
                    {
                        Logging.Log("Combat: Out of Ammo!");
                        if (State == CombatMissionsBehaviorState.ExecuteMission) State = CombatMissionsBehaviorState.GotoBase;
                        // Clear looted containers
                        Cache.Instance.LootedContainers.Clear();
                        //Cache.Instance.InvalidateBetweenMissionsCache();
                    }

                    if (_combatMissionCtrl.State == CombatMissionCtrlState.Done)
                    {
                        if (State == CombatMissionsBehaviorState.ExecuteMission) State = CombatMissionsBehaviorState.GotoBase;

                        // Clear looted containers
                        Cache.Instance.LootedContainers.Clear();
                        //Cache.Instance.InvalidateBetweenMissionsCache();
                    }

                    // If in error state, just go home and stop the bot
                    if (_combatMissionCtrl.State == CombatMissionCtrlState.Error)
                    {
                        Logging.Log("MissionController: Error");
                        if (State == CombatMissionsBehaviorState.ExecuteMission) State = CombatMissionsBehaviorState.GotoBase;

                        // Clear looted containers
                        Cache.Instance.LootedContainers.Clear();
                        //Cache.Instance.InvalidateBetweenMissionsCache();
                    }
                    break;

                case CombatMissionsBehaviorState.GotoBase:
                    bool debugGotoBase = false;
                        
                    if (debugGotoBase) Logging.Log("QuestorState: GotoBase: AvoidBumpingThings()");
                    
                    AvoidBumpingThings();
                    
                    if (debugGotoBase) Logging.Log("QuestorState: GotoBase: TravelToAgentsStation()");
                    
                    TravelToAgentsStation();
                    
                    if (_traveler.State == TravelerState.AtDestination || DateTime.Now.Subtract(Cache.Instance.EnteredCloseQuestor_DateTime).TotalMinutes > 10)
                        {
                        if (debugGotoBase) Logging.Log("QuestorState: GotoBase: We are at destination");
                    Cache.Instance.GotoBaseNow = false; //we are there - turn off the 'forced' gotobase
                    Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID);

                    if (_combatMissionCtrl.State == CombatMissionCtrlState.Error)
                    {
                        if (State == CombatMissionsBehaviorState.GotoBase) State = CombatMissionsBehaviorState.Error;
                    }
                    else if (_combat.State != CombatState.OutOfAmmo && Cache.Instance.Mission != null && Cache.Instance.Mission.State == (int)MissionState.Accepted)
                    {
                        if (State == CombatMissionsBehaviorState.GotoBase) State = CombatMissionsBehaviorState.CompleteMission;
                    }
                    else
                    {
                        if (State == CombatMissionsBehaviorState.GotoBase) State = CombatMissionsBehaviorState.UnloadLoot;
                    }
                    _traveler.Destination = null;
                    }
                    break;

                case CombatMissionsBehaviorState.CompleteMission:
                    if (_agentInteraction.State == AgentInteractionState.Idle)
                    {

                        //Logging.Log("CombatMissionsBehavior: Starting: Statistics.WriteDroneStatsLog");
                        if (!Statistics.WriteDroneStatsLog()) break;
                        //Logging.Log("CombatMissionsBehavior: Starting: Statistics.AmmoConsumptionStatistics");
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
                            if (State == CombatMissionsBehaviorState.CompleteMission) State = CombatMissionsBehaviorState.Idle;
                        }
                        else
                        {
                            if (State == CombatMissionsBehaviorState.CompleteMission) State = CombatMissionsBehaviorState.UnloadLoot;
                        }
                        return;
                    }
                    break;

                case CombatMissionsBehaviorState.UnloadLoot:
                    if (_unloadLoot.State == UnloadLootState.Idle)
                    {
                        Logging.Log("CombatMissionsBehavior: UnloadLoot: Begin");
                        _unloadLoot.State = UnloadLootState.Begin;
                    }

                    _unloadLoot.ProcessState();

                    if (Settings.Instance.DebugStates)
                        Logging.Log("CombatMissionsBehavior: UnloadLoot.State = " + _unloadLoot.State);

                    if (_unloadLoot.State == UnloadLootState.Done)
                    {
                        Cache.Instance.LootAlreadyUnloaded = true;
                        _unloadLoot.State = UnloadLootState.Idle;
                        Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID);
                        if (_combat.State == CombatState.OutOfAmmo || (!(Cache.Instance.Mission == null || Cache.Instance.Mission.State == (int)MissionState.Offered))) // on mission
                        {
                            Logging.Log("CombatMissionsBehavior: Unloadloot: We are on mission or out of ammo.");
                            State = CombatMissionsBehaviorState.Idle;
                            return;
                        }
                        //This salvaging decision tree does not belong here and should be separated out into a different questorstate
                        if (Settings.Instance.AfterMissionSalvaging)
                        {
                            if (Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Count == 0)
                            {
                                Logging.Log("CombatMissionsBehavior: Unloadloot: No more salvaging bookmarks. Setting FinishedSalvaging Update.");
                                //if (Settings.Instance.CharacterMode == "Salvager")
                                //{
                                //    Logging.Log("Salvager mode set and no bookmarks making delay");
                                //    State = CombatMissionsBehaviorStateState.Error; //or salvageonly. need to check difference
                                //}

                                if (Settings.Instance.CharacterMode.ToLower() == "salvage".ToLower())
                                {
                                    Logging.Log("CombatMissionsBehavior: Unloadloot: Character mode is BookmarkSalvager and no bookmarks salvage.");
                                    //We just need a NextSalvagerSession timestamp to key off of here to add the delay
                                    State = CombatMissionsBehaviorState.Idle;
                                }
                                else
                                {
                                    //Logging.Log("CombatMissionsBehavior: Character mode is not salvage going to next mission.");
                                    State = CombatMissionsBehaviorState.Idle; //add pause here
                                }
                                Statistics.Instance.FinishedSalvaging = DateTime.Now;
                                return;
                            }
                            else //There is at least 1 salvage bookmark
                            {
                                Logging.Log("CombatMissionsBehavior: Unloadloot: There are [" + Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Count + " ] more salvage bookmarks left to process");
                                // Salvage only after multiple missions have been completed
                                if (Settings.Instance.SalvageMultpleMissionsinOnePass)
                                {
                                    //if we can still complete another mission before the Wrecks disappear and still have time to salvage
                                    if (DateTime.Now.Subtract(Statistics.Instance.FinishedSalvaging).TotalMinutes > ((int)Time.WrecksDisappearAfter_minutes - (int)Time.AverageTimeToCompleteAMission_minutes - (int)Time.AverageTimetoSalvageMultipleMissions_minutes))
                                    {
                                        Logging.Log("CombatMissionsBehavior: UnloadLoot: The last finished after mission salvaging session was [" + DateTime.Now.Subtract(Statistics.Instance.FinishedSalvaging).TotalMinutes + "] ago ");
                                        Logging.Log("CombatMissionsBehavior: UnloadLoot: we are after mission salvaging again because it has been at least [" + ((int)Time.WrecksDisappearAfter_minutes - (int)Time.AverageTimeToCompleteAMission_minutes - (int)Time.AverageTimetoSalvageMultipleMissions_minutes) + "] min since the last session. ");
                                        if (State == CombatMissionsBehaviorState.UnloadLoot)
                                        {
                                            State = CombatMissionsBehaviorState.BeginAfterMissionSalvaging;
                                            Statistics.Instance.StartedSalvaging = DateTime.Now;
                                            //FIXME: should we be overwriting this timestamp here? What if this is the 3rd run back and fourth to the station?
                                        }
                                    }
                                    else //we are salvaging mission 'in one pass' and it has not been enough time since our last run... do another mission
                                    {
                                        Logging.Log("CombatMissionsBehavior: UnloadLoot: The last finished after mission salvaging session was [" + DateTime.Now.Subtract(Statistics.Instance.FinishedSalvaging).TotalMinutes + "] ago ");
                                        Logging.Log("CombatMissionsBehavior: UnloadLoot: we are going to the next mission because it has not been [" + ((int)Time.WrecksDisappearAfter_minutes - (int)Time.AverageTimeToCompleteAMission_minutes - (int)Time.AverageTimetoSalvageMultipleMissions_minutes) + "] min since the last session. ");
                                        Statistics.Instance.FinishedMission = DateTime.Now;
                                        if (State == CombatMissionsBehaviorState.UnloadLoot) State = CombatMissionsBehaviorState.Idle;
                                    }
                                }
                                else //begin after mission salvaging now, rather than later
                                {
                                    if (Settings.Instance.CharacterMode == "salvage".ToLower())
                                    {
                                        Logging.Log("CombatMissionsBehavior: Unloadloot: CharacterMode: [" + Settings.Instance.CharacterMode + "], AfterMissionSalvaging: [" + Settings.Instance.AfterMissionSalvaging + "], QuestorState: [" + State + "]");
                                        State = CombatMissionsBehaviorState.BeginAfterMissionSalvaging;
                                        Statistics.Instance.StartedSalvaging = DateTime.Now;
                                    }
                                    else
                                    {
                                        Logging.Log("CombatMissionsBehavior: UnloadLoot: The last after mission salvaging session was [" + Math.Round(DateTime.Now.Subtract(Statistics.Instance.FinishedSalvaging).TotalMinutes,0) + "min] ago ");
                                        State = CombatMissionsBehaviorState.BeginAfterMissionSalvaging;
                                        Statistics.Instance.StartedSalvaging = DateTime.Now;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Settings.Instance.CharacterMode.ToLower() == "Combat Missions".ToLower() || Settings.Instance.CharacterMode.ToLower() == "dps".ToLower())
                            {
                                State = CombatMissionsBehaviorState.Idle;
                                Logging.Log("CombatMissionsBehavior: Unloadloot: CharacterMode: [" + Settings.Instance.CharacterMode + "], AfterMissionSalvaging: [" + Settings.Instance.AfterMissionSalvaging + "], QuestorState: [" + State + "]");
                                Statistics.Instance.FinishedMission = DateTime.Now;
                                return;
                            }
                        }
                    }
                    break;

                case CombatMissionsBehaviorState.BeginAfterMissionSalvaging:
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
                                State = CombatMissionsBehaviorState.Idle;
                                return;
                            }
                        }

                        if (State == CombatMissionsBehaviorState.BeginAfterMissionSalvaging)
                        {
                            State = CombatMissionsBehaviorState.GotoSalvageBookmark;
                            _lastSalvageTrip = DateTime.Now;
                        }
                        _traveler.Destination = new BookmarkDestination(bookmark);
                        return;
                    }
                    break;

                case CombatMissionsBehaviorState.GotoSalvageBookmark:
                    _traveler.ProcessState();
                    string target = "Acceleration Gate";
                    IEnumerable<EntityCache> targets;
                    Cache.Instance.EntitiesByName(target);
                    if (_traveler.State == TravelerState.AtDestination || GateInSalvage())
                    {
                        //we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;

                        if (State == CombatMissionsBehaviorState.GotoSalvageBookmark) State = CombatMissionsBehaviorState.Salvage;
                        _traveler.Destination = null;
                        return;
                    }

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Traveler.State = " + _traveler.State);
                    break;

                case CombatMissionsBehaviorState.Salvage:
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
                            if (State == CombatMissionsBehaviorState.Salvage)
                            {
                                Statistics.Instance.FinishedSalvaging = DateTime.Now;
                                State = CombatMissionsBehaviorState.GotoBase;
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
                            if (State == CombatMissionsBehaviorState.Salvage)
                            {
                                State = CombatMissionsBehaviorState.GotoBase;
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
                                if (State == CombatMissionsBehaviorState.Salvage)
                                {
                                    Statistics.Instance.FinishedSalvaging = DateTime.Now;
                                    State = CombatMissionsBehaviorState.GotoBase;
                                }
                                return;
                            }
                            else
                            {
                                if (!gatesInRoom)
                                {
                                    Logging.Log("Questor.Salvage: Go to the next salvage bookmark");
                                    var bookmark = bookmarks.FirstOrDefault(c => c.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId) ?? bookmarks.FirstOrDefault();
                                    if (State == CombatMissionsBehaviorState.Salvage)
                                    {
                                        State = CombatMissionsBehaviorState.GotoSalvageBookmark;
                                    }
                                    _traveler.Destination = new BookmarkDestination(bookmark);
                                }
                                else if (Settings.Instance.UseGatesInSalvage)
                                {
                                    Logging.Log("Questor.Salvage: Acceleration gate found - moving to next pocket");
                                    if (State == CombatMissionsBehaviorState.Salvage)
                                    {
                                        State = CombatMissionsBehaviorState.SalvageUseGate;
                                    }
                                }
                                else
                                {
                                    Logging.Log("Questor.Salvage: Acceleration gate found, useGatesInSalvage set to false - Returning to base");
                                    if (State == CombatMissionsBehaviorState.Salvage)
                                    {
                                        Statistics.Instance.FinishedSalvaging = DateTime.Now;
                                        State = CombatMissionsBehaviorState.GotoBase;
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
                            Logging.Log("CombatMissionsBehavior.Salvage: Stop ship, ClosestWreck [" + Math.Round(closestWreck.Distance, 0) + "] is in scooprange + [" + (int)Distance.SafeScoopRange + "] and we were approaching");
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
                            ApplySalvageSettings();
                        }
                    }
                    break;

                case CombatMissionsBehaviorState.SalvageUseGate:
                    Cache.Instance.OpenWrecks = true;

                    target = "Acceleration Gate";
                    targets = Cache.Instance.EntitiesByName(target);
                    if (targets == null || !targets.Any())
                    {
                        if (State == CombatMissionsBehaviorState.SalvageUseGate)
                        {
                            State = CombatMissionsBehaviorState.GotoSalvageBookmark;
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

                        if (State == CombatMissionsBehaviorState.SalvageUseGate) State = CombatMissionsBehaviorState.SalvageNextPocket;
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

                case CombatMissionsBehaviorState.SalvageNextPocket:
                    Cache.Instance.OpenWrecks = true;
                    double distance = Cache.Instance.DistanceFromMe(_lastX, _lastY, _lastZ);
                    if (distance > (int)Distance.NextPocketDistance)
                    {
                        //we know we are connected here...
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;

                        Logging.Log("Questor.Salvage: We've moved to the next Pocket [" + Math.Round(distance / 1000, 0) + "k away]");

                        if (State == CombatMissionsBehaviorState.SalvageUseGate) State = CombatMissionsBehaviorState.Salvage;
                        return;
                    }
                    else //we have not moved to the next pocket quite yet
                    {
                        if (DateTime.Now.Subtract(_lastPulse).TotalMinutes > 2)
                        {
                            Logging.Log("Questor.Salvage: We've timed out, retry last action");
                            // We have reached a timeout, revert to ExecutePocketActions (e.g. most likely Activate)
                            if (State == CombatMissionsBehaviorState.SalvageNextPocket) State = CombatMissionsBehaviorState.SalvageUseGate;
                        }
                    }
                    break;

                case CombatMissionsBehaviorState.Storyline:
                    _storyline.ProcessState();

                    if (_storyline.State == StorylineState.Done)
                    {
                        Logging.Log("CombatMissionsBehavior: We have completed the storyline, returning to base");
                        if (State == CombatMissionsBehaviorState.Storyline) State = CombatMissionsBehaviorState.GotoBase;
                        break;
                    }
                    break;

                case CombatMissionsBehaviorState.CourierMission:

                    if (_courierMissionCtrl.State == CourierMissionCtrlState.Idle)
                        _courierMissionCtrl.State = CourierMissionCtrlState.GotoPickupLocation;

                    _courierMissionCtrl.ProcessState();

                    if (_courierMissionCtrl.State == CourierMissionCtrlState.Done)
                    {
                        _courierMissionCtrl.State = CourierMissionCtrlState.Idle;
                        Cache.Instance.CourierMission = false;
                        if (State == CombatMissionsBehaviorState.CourierMission) State = CombatMissionsBehaviorState.GotoBase;
                    }
                    break;
                
                case CombatMissionsBehaviorState.Traveler:
                    Cache.Instance.OpenWrecks = false;
                    List<long> destination = Cache.Instance.DirectEve.Navigation.GetDestinationPath();
                    if (destination == null || destination.Count == 0)
                    {
                        // happens if autopilot isn't set and this questorstate is choosen manually
                        // this also happens when we get to destination (!?)
                        Logging.Log("QuestorState.Traveler: No destination?");
                        if (State == CombatMissionsBehaviorState.Traveler) State = CombatMissionsBehaviorState.Error;
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
                            if (_combatMissionCtrl.State == CombatMissionCtrlState.Error)
                            {
                                Logging.Log("QuestorState.Traveler: an error has occurred");
                                if (State == CombatMissionsBehaviorState.Traveler)
                                {
                                    State = CombatMissionsBehaviorState.Error;
                                }
                                return;
                            }
                            else if (Cache.Instance.InSpace)
                            {
                                Logging.Log("QuestorState.Traveler: Arrived at destination (in space, Questor stopped)");
                                if (State == CombatMissionsBehaviorState.Traveler) State = CombatMissionsBehaviorState.Error;
                                return;
                            }
                            else
                            {
                                Logging.Log("QuestorState.Traveler: Arrived at destination");
                                if (State == CombatMissionsBehaviorState.Traveler) State = CombatMissionsBehaviorState.Idle;
                                return;
                            }
                        }
                    }
                    break;

                case CombatMissionsBehaviorState.GotoNearestStation:
                    if (!Cache.Instance.InSpace || Cache.Instance.InWarp) return;
                    var station = Cache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();
                    if (station != null)
                    {
                        if (station.Distance > (int)Distance.WarptoDistance)
                        {
                            Logging.Log("Questor.GotoNearestStation [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                            station.WarpToAndDock();
                            Cache.Instance.NextWarpTo = DateTime.Now.AddSeconds((int)Time.WarptoDelay_seconds);
                            if (State == CombatMissionsBehaviorState.GotoNearestStation) State = CombatMissionsBehaviorState.Salvage;
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
                        if (State == CombatMissionsBehaviorState.GotoNearestStation) State = CombatMissionsBehaviorState.Error; //should we goto idle here?
                    }
                    break;

                case CombatMissionsBehaviorState.Default:
                    if (State == CombatMissionsBehaviorState.Default) State = CombatMissionsBehaviorState.Idle;
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
