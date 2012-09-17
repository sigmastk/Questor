﻿// ------------------------------------------------------------------------------
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
using System.Globalization;
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
    public class DedicatedBookmarkSalvagerBehavior
    {
        private readonly Arm _arm;
        private readonly LocalWatch _localWatch;

        private DateTime _lastPulse;
        
        private readonly Panic _panic;
        private readonly Statistics _statistics;
        private readonly Salvage _salvage;
        private readonly Traveler _traveler;
        private readonly UnloadLoot _unloadLoot;
        public DateTime LastAction;
        private DateTime _nextBookmarksrefresh = DateTime.Now;

        //private readonly Random _random;
        public static long AgentID;

        private readonly Stopwatch _watch;
        private DateTime _nextBookmarkRefreshCheck = DateTime.Now;

        public bool Panicstatereset = false;

        private bool ValidSettings { get; set; }

        public bool CloseQuestorflag = true;

        public string CharacterName { get; set; }

//        public List<DirectBookmark> AfterMissionSalvageBookmarks;
        public List<DirectBookmark> BookmarksThatAreNotReadyYet;

        //DateTime _nextAction = DateTime.Now;

        public DedicatedBookmarkSalvagerBehavior()
        {
            _lastPulse = DateTime.Now;

            //_random = new Random();
            _salvage = new Salvage();
            _localWatch = new LocalWatch();
            //_combat = new Combat();
            //_drones = new Drones();
            _traveler = new Traveler();
            _unloadLoot = new UnloadLoot();
            _arm = new Arm();
            _panic = new Panic();
            _statistics = new Statistics();
            _watch = new Stopwatch();

            //
            // this is combat mission specific and needs to be generalized
            //
            Settings.Instance.SettingsLoaded += SettingsLoaded;

            _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.Idle;
            _States.CurrentArmState = ArmState.Idle;
            //_States.CurrentDroneState = DroneState.Idle;
            _States.CurrentUnloadLootState = UnloadLootState.Idle;
            _States.CurrentTravelerState = TravelerState.Idle;
        }

        public void SettingsLoaded(object sender, EventArgs e)
        {
            ApplySalvageSettings();
            ValidateDedicatedSalvageSettings();
        }

        public void DebugDedicatedBookmarkSalvagerBehaviorStates()
        {
            if (Settings.Instance.DebugStates)
                Logging.Log("DedicateSalvagerBehavior.State is", _States.CurrentDedicatedBookmarkSalvagerBehaviorState.ToString(), Logging.white);
        }

        public void DebugPanicstates()
        {
            if (Settings.Instance.DebugStates)
                Logging.Log("Panic.State is", _States.CurrentPanicState.ToString(), Logging.white);
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
                Logging.Log(whatWeAreTiming, " took " + _watch.ElapsedMilliseconds + "ms", Logging.white);
        }

        public void ValidateDedicatedSalvageSettings()
        {
            ValidSettings = true;
            DirectAgent agent = Cache.Instance.DirectEve.GetAgentByName(Cache.Instance.CurrentAgent);

            if (agent == null || !agent.IsValid)
            {
                Logging.Log("Settings", "Unable to locate agent [" + Cache.Instance.CurrentAgent + "]", Logging.white);
                ValidSettings = false;
            }
            else
            {
                //_agentInteraction.AgentId = agent.AgentId;
                //_combatMissionCtrl.AgentId = agent.AgentId;
                _arm.AgentId = agent.AgentId;
                _statistics.AgentID = agent.AgentId;
                AgentID = agent.AgentId;
                _salvage.Ammo = Settings.Instance.Ammo;
                _salvage.MaximumWreckTargets = Settings.Instance.MaximumWreckTargets;
                _salvage.ReserveCargoCapacity = Settings.Instance.ReserveCargoCapacity;
                _salvage.LootEverything = Settings.Instance.LootEverything;
            }
        }

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
            _States.CurrentQuestorState = QuestorState.CloseQuestor;
        }

        private void TravelToAgentsStation()
        {
            var baseDestination = _traveler.Destination as StationDestination;
            if (baseDestination == null || baseDestination.StationId != Cache.Instance.Agent.StationId)
                _traveler.Destination = new StationDestination(Cache.Instance.Agent.SolarSystemId, Cache.Instance.Agent.StationId, Cache.Instance.DirectEve.GetLocationName(Cache.Instance.Agent.StationId));
            _traveler.ProcessState();
            if (Settings.Instance.DebugStates)
            {
                Logging.Log("Traveler.State is ", _States.CurrentTravelerState.ToString(), Logging.white);
            }
        }

        public void ProcessState()
        {
            // Invalid settings, quit while we're ahead
            if (!ValidSettings)
            {
                if (DateTime.Now.Subtract(LastAction).TotalSeconds < Time.Instance.ValidateSettings_seconds) //default is a 15 second interval
                {
                    ValidateDedicatedSalvageSettings();
                    LastAction = DateTime.Now;
                }
                return;
            }

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //this local is safe check is useless as their is no localwatch processstate running every tick...
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //If local unsafe go to base and do not start mission again
            if (Settings.Instance.FinishWhenNotSafe && (_States.CurrentDedicatedBookmarkSalvagerBehaviorState != DedicatedBookmarkSalvagerBehaviorState.GotoNearestStation /*|| State!=QuestorState.GotoBase*/))
            {
                //need to remove spam
                if (Cache.Instance.InSpace && !Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    var station = Cache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();
                    if (station != null)
                    {
                        Logging.Log("Local not safe", "Station found. Going to nearest station", Logging.white);
                        if (_States.CurrentDedicatedBookmarkSalvagerBehaviorState != DedicatedBookmarkSalvagerBehaviorState.GotoNearestStation)
                            _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoNearestStation;
                    }
                    else
                    {
                        Logging.Log("Local not safe", "Station not found. Going back to base", Logging.white);
                        if (_States.CurrentDedicatedBookmarkSalvagerBehaviorState != DedicatedBookmarkSalvagerBehaviorState.GotoBase)
                            _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
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
                if (_States.CurrentDedicatedBookmarkSalvagerBehaviorState != DedicatedBookmarkSalvagerBehaviorState.GotoBase)
                {
                    _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
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

            //
            // Panic always runs, not just in space
            //
            DebugPerformanceClearandStartTimer();
            _panic.ProcessState();
            DebugPerformanceStopandDisplayTimer("Panic.ProcessState");
            if (_States.CurrentPanicState == PanicState.Panic || _States.CurrentPanicState == PanicState.Panicking)
            {
                DebugDedicatedBookmarkSalvagerBehaviorStates();
                if (Panicstatereset)
                {
                    _States.CurrentPanicState = PanicState.Normal;
                    Panicstatereset = false;
                }
            }
            else if (_States.CurrentPanicState == PanicState.Resume)
            {
                // Reset panic state
                _States.CurrentPanicState = PanicState.Normal;
            }
            DebugPanicstates();

            switch (_States.CurrentDedicatedBookmarkSalvagerBehaviorState)
            {
                case DedicatedBookmarkSalvagerBehaviorState.Idle:

                    if (Cache.Instance.StopBot)
                        return;

                    _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
                    _States.CurrentArmState = ArmState.Idle;
                    _States.CurrentDroneState = DroneState.Idle;
                    _States.CurrentSalvageState = SalvageState.Idle;
                    _States.CurrentStorylineState = StorylineState.Idle;
                    _States.CurrentTravelerState = TravelerState.Idle;
                    _States.CurrentUnloadLootState = UnloadLootState.Idle;
                    _States.CurrentTravelerState = TravelerState.Idle;

                    if (Cache.Instance.InSpace)
                    {
                        // Questor does not handle in space starts very well, head back to base to try again
                        Logging.Log("DedicatedBookmarkSalvagerBehavior", "Started questor while in space, heading back to base in 15 seconds", Logging.white);
                        LastAction = DateTime.Now;
                        Cache.Instance.NextSalvageTrip = DateTime.Now;
                         _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.CheckBookmarkAge;
                        break;
                    }

                    // only attempt to write the mission statistics logs if one of the mission stats logs is enabled in settings
                    //if (Settings.Instance.SalvageStats1Log)
                    //{
                    //    if (!Statistics.Instance.SalvageLoggingCompleted)
                    //    {
                    //        Statistics.WriteSalvagerStatistics();
                    //        break;
                    //    }
                    //}

                    if (Settings.Instance.AutoStart)
                    {
                        //we know we are connected here
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;

                        // Don't start a new action an hour before downtime
                        if (DateTime.UtcNow.Hour == 10)
                        {
                            if (Settings.Instance.DebugAutoStart) Logging.Log("DedicatedBookmarkSalvagerBehavior", "Autostart: if (DateTime.UtcNow.Hour == 10)", Logging.white);
                            break;
                        }

                        // Don't start a new action near downtime
                        if (DateTime.UtcNow.Hour == 11 && DateTime.UtcNow.Minute < 15)
                        {
                            if (Settings.Instance.DebugAutoStart) Logging.Log("DedicatedBookmarkSalvagerBehavior", "if (DateTime.UtcNow.Hour == 11 && DateTime.UtcNow.Minute < 15)", Logging.white);
                            break;
                        }

                        //Logging.Log("DedicatedBookmarkSalvagerBehavior::: _nextBookmarksrefresh.subtract(datetime.now).totalminutes [" +
                        //            Math.Round(DateTime.Now.Subtract(_nextBookmarkRefreshCheck).TotalMinutes,0) + "]");

                        //Logging.Log("DedicatedBookmarkSalvagerBehavior::: Next Salvage Trip Scheduled in [" +
                        //            _Cache.Instance.NextSalvageTrip.ToString(CultureInfo.InvariantCulture) + "min]");

                        if (DateTime.Now > _nextBookmarkRefreshCheck)
                        {
                            _nextBookmarkRefreshCheck = DateTime.Now.AddMinutes(1);
                            if (Cache.Instance.InStation && (DateTime.Now > _nextBookmarksrefresh))
                            {
                                _nextBookmarksrefresh = DateTime.Now.AddMinutes(Cache.Instance.RandomNumber(18, 24));
                                Logging.Log("DedicatedBookmarkSalvagerBehavior", "Next Bookmark refresh in [" +
                                               Math.Round(_nextBookmarksrefresh.Subtract(DateTime.Now).TotalMinutes, 0) + "min]", Logging.white);
                                Cache.Instance.DirectEve.RefreshBookmarks();
                            }
                            else
                            {
                                Logging.Log("DedicatedBookmarkSalvagerBehavior", "Next Bookmark refresh in [" +
                                               Math.Round(_nextBookmarksrefresh.Subtract(DateTime.Now).TotalMinutes, 0) + "min]", Logging.white);

                                Logging.Log("DedicatedBookmarkSalvagerBehavior", "Next Salvage Trip Scheduled in [" +
                                               Math.Round(Cache.Instance.NextSalvageTrip.Subtract(DateTime.Now).TotalMinutes, 0) + "min]", Logging.white);
                            }
                        }

                        if (DateTime.Now > Cache.Instance.NextSalvageTrip)
                        {
                            Logging.Log("DedicatedBookmarkSalvagerBehavior.BeginAftermissionSalvaging", "Starting Another Salvage Trip", Logging.white);
                            LastAction = DateTime.Now;
                            _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.Start;
                            return;
                        }
                    }
                    else
                    {
                        Cache.Instance.LastScheduleCheck = DateTime.Now;
                        Questor.TimeCheck();   //Should we close questor due to stoptime or runtime?
                    }
                    break;

                case DedicatedBookmarkSalvagerBehaviorState.DelayedGotoBase:
                    if (DateTime.Now.Subtract(LastAction).TotalSeconds < Time.Instance.DelayedGotoBase_seconds)
                        break;

                    Logging.Log("DedicatedBookmarkSalvagerBehavior", "Heading back to base", Logging.white);
                    _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
                    break;

                case DedicatedBookmarkSalvagerBehaviorState.Start:
                    Cache.Instance.OpenWrecks = true;
                    ValidateDedicatedSalvageSettings();
                    _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.UnloadLoot;
                    break;

                case DedicatedBookmarkSalvagerBehaviorState.LocalWatch:
                    if (Settings.Instance.UseLocalWatch)
                    {
                        Cache.Instance.LastLocalWatchAction = DateTime.Now;
                        if (Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                        {
                            Logging.Log("DedicatedBookmarkSalvagerBehavior.LocalWatch", "local is clear", Logging.white);
                            if (_States.CurrentDedicatedBookmarkSalvagerBehaviorState == DedicatedBookmarkSalvagerBehaviorState.LocalWatch) _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.BeginAfterMissionSalvaging;
                        }
                        else
                        {
                            Logging.Log("DedicatedBookmarkSalvagerBehavior.LocalWatch", "Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again", Logging.white);
                            if (_States.CurrentDedicatedBookmarkSalvagerBehaviorState == DedicatedBookmarkSalvagerBehaviorState.LocalWatch)
                            {
                                _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.WaitingforBadGuytoGoAway;
                            }
                            Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                            Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        }
                    }
                    else
                    {
                        if (_States.CurrentDedicatedBookmarkSalvagerBehaviorState == DedicatedBookmarkSalvagerBehaviorState.LocalWatch) _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.BeginAfterMissionSalvaging;
                    }
                    break;

                case DedicatedBookmarkSalvagerBehaviorState.WaitingforBadGuytoGoAway:
                    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    if (DateTime.Now.Subtract(Cache.Instance.LastLocalWatchAction).TotalMinutes < Time.Instance.WaitforBadGuytoGoAway_minutes)
                        break;
                    if (_States.CurrentDedicatedBookmarkSalvagerBehaviorState == DedicatedBookmarkSalvagerBehaviorState.WaitingforBadGuytoGoAway) _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.LocalWatch;
                    break;

                case DedicatedBookmarkSalvagerBehaviorState.GotoBase:

                    if (Settings.Instance.DebugGotobase) Logging.Log("DedicatedBookmarkSalvagerBehavior", "GotoBase: AvoidBumpingThings()", Logging.white);
                    NavigateOnGrid.AvoidBumpingThings(Cache.Instance.BigObjects.FirstOrDefault(), "DedicatedBookmarkSalvagerBehaviorState.GotoBase");
                    if (Settings.Instance.DebugGotobase) Logging.Log("DedicatedBookmarkSalvagerBehavior", "GotoBase: TravelToAgentsStation()", Logging.white);
                    TravelToAgentsStation();
                    if (_States.CurrentTravelerState == TravelerState.AtDestination) // || DateTime.Now.Subtract(Cache.Instance.EnteredCloseQuestor_DateTime).TotalMinutes > 10)
                    {
                        if (Settings.Instance.DebugGotobase) Logging.Log("DedicatedBookmarkSalvagerBehavior", "GotoBase: We are at destination", Logging.white);
                        Cache.Instance.GotoBaseNow = false; //we are there - turn off the 'forced' gotobase
                        Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID);
                        if (_States.CurrentDedicatedBookmarkSalvagerBehaviorState == DedicatedBookmarkSalvagerBehaviorState.GotoBase) _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.UnloadLoot;
                        _traveler.Destination = null;
                    }
                    break;

                case DedicatedBookmarkSalvagerBehaviorState.UnloadLoot:
                    if (_States.CurrentUnloadLootState == UnloadLootState.Idle)
                    {
                        Logging.Log("DedicatedBookmarkSalvagerBehavior", "UnloadLoot: Begin", Logging.white);
                        _States.CurrentUnloadLootState = UnloadLootState.Begin;
                    }

                    _unloadLoot.ProcessState();

                    if (Settings.Instance.DebugStates)
                        Logging.Log("DedicatedBookmarkSalvagerBehavior", "UnloadLoot.State = " + _States.CurrentUnloadLootState, Logging.white);

                    if (_States.CurrentUnloadLootState == UnloadLootState.Done)
                    {
                        Cache.Instance.LootAlreadyUnloaded = true;
                        _States.CurrentUnloadLootState = UnloadLootState.Idle;
                        _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.CheckBookmarkAge;
                    }
                    break;


                case DedicatedBookmarkSalvagerBehaviorState.CheckBookmarkAge:

                    if (DateTime.Now >= Cache.Instance.NextSalvageTrip)
                    {
                        if (Cache.Instance.GetSalvagingBookmark == null)
                        {
                            BookmarksThatAreNotReadyYet = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                            if (BookmarksThatAreNotReadyYet.Any())
                            {
                                Logging.Log("DedicatedBookmarkSalvagerBehavior", "CheckBookmarkAge: There are [" + BookmarksThatAreNotReadyYet.Count() + "] Salvage Bookmarks that have not yet aged [" + Settings.Instance.AgeofBookmarksForSalvageBehavior + "] min.", Logging.white);
                            }
                            Logging.Log("DedicatedBookmarkSalvagerBehavior", "CheckBookmarkAge: Character mode is BookmarkSalvager and no bookmarks are ready to salvage.", Logging.white);
                            //We just need a NextSalvagerSession timestamp to key off of here to add the delay
                            if (Cache.Instance.InSpace)
                            {
                                // Questor does not handle in space starts very well, head back to base to try again
                                LastAction = DateTime.Now;
                                Cache.Instance.NextSalvageTrip = DateTime.Now.AddMinutes(Time.Instance.DelayBetweenSalvagingSessions_minutes);
                                _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
                                break;
                            }
                            else
                            {
                                _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.Idle;
                                _States.CurrentQuestorState = QuestorState.Idle;
                                Cache.Instance.NextSalvageTrip = DateTime.Now.AddMinutes(Time.Instance.DelayBetweenSalvagingSessions_minutes);
                            }

                            break;
                        }
                        else //There is at least 1 salvage bookmark
                        {
                            Logging.Log("DedicatedBookmarkSalvagerBehavior", "CheckBookmarkAge: There are [ " + Cache.Instance.AfterMissionSalvageBookmarks.Count()+ " ] more salvage bookmarks older then:" + Cache.Instance.AgedDate.ToString() + ", left to process", Logging.white);
                            _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.BeginAfterMissionSalvaging;
                            Statistics.Instance.StartedSalvaging = DateTime.Now;
                        }
                    }
                    else
                    {
                        Logging.Log("DedicatedBookmarkSalvagerBehavior", "CheckBookmarkAge: next salvage timer not expired. Waiting...", Logging.white);
                        _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.Idle;
                        _States.CurrentQuestorState = QuestorState.Idle;
                        return;
                    }
                    break;

                case DedicatedBookmarkSalvagerBehaviorState.BeginAfterMissionSalvaging:

                    if (DateTime.Now > Statistics.Instance.StartedSalvaging.AddMinutes(2))
                    {
                        Logging.Log("DedicatedBookmarkSalvagebehavior", "Found [" + Cache.Instance.AfterMissionSalvageBookmarks.Count() + "] salvage bookmarks ready to process.", Logging.white);
                        Statistics.Instance.StartedSalvaging = DateTime.Now; //this will be reset for each "run" between the station and the field if using <unloadLootAtStation>true</unloadLootAtStation>
                        Cache.Instance.NextSalvageTrip = DateTime.Now.AddMinutes(Time.Instance.DelayBetweenSalvagingSessions_minutes);
                    }
                    //we know we are connected here
                    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;

                    Cache.Instance.OpenWrecks = true;
                    if (Cache.Instance.InStation)
                    {
                        if (_States.CurrentArmState == ArmState.Idle)
                            _States.CurrentArmState = ArmState.SwitchToSalvageShip;

                        _arm.ProcessState();
                    }
                    if (_States.CurrentArmState == ArmState.Done || Cache.Instance.InSpace)
                    {

                        _States.CurrentArmState = ArmState.Idle;
                        DirectBookmark bookmark = Cache.Instance.AfterMissionSalvageBookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault();
                        if (bookmark == null)
                        {
                            _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
                            Cache.Instance.NextSalvageTrip = DateTime.Now.AddMinutes(Time.Instance.DelayBetweenSalvagingSessions_minutes);
                            return;
                        }
                        Logging.Log("DedicatedBookmarkSalvagerBehavior.Salvager", "Salvaging at first oldest bookmarks created on: " + bookmark.CreatedOn.ToString(), Logging.white);

                        var bookmarksinlocal = new List<DirectBookmark>(Cache.Instance.AfterMissionSalvageBookmarks.Where(b => b.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId).
                                                                               OrderBy(b => b.CreatedOn));
                        DirectBookmark localBookmark = bookmarksinlocal.FirstOrDefault();
                        if (localBookmark != null)
                        {
                            _traveler.Destination = new BookmarkDestination(localBookmark);
                        }
                        else
                        {
                            _traveler.Destination = new BookmarkDestination(bookmark);
                        }
                        _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoSalvageBookmark;
                        //we know we are connected here
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        return;
                    }
                    break;

                case DedicatedBookmarkSalvagerBehaviorState.GotoSalvageBookmark:
                    _traveler.ProcessState();
                    if (Cache.Instance.GateInGrid())
                    {
                        Logging.Log("DedicatedBookmarkSalvagerBehavior", "GotoSalvageBookmark: We found gate in salvage bookmark. Going back to Base", Logging.white);
                        //we know we are connected here
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
                        _traveler.Destination = null;
                        Cache.Instance.NextSalvageTrip = DateTime.Now.AddMinutes(Time.Instance.DelayBetweenSalvagingSessions_minutes);
                        return;
                    }
                    else 
                    if (_States.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        Logging.Log("DedicatedBookmarkSalvagerBehavior", "GotoSalvageBookmark: Gate not found, we can start salvaging", Logging.white);
                        //we know we are connected here
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;

                        _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.Salvage;
                        _traveler.Destination = null;
                        return;
                    }

                    if (Settings.Instance.DebugStates)
                        Logging.Log("Traveler.State is ", _States.CurrentTravelerState.ToString(), Logging.white);
                    break;

                case DedicatedBookmarkSalvagerBehaviorState.Salvage:
                    if (Settings.Instance.DebugSalvage) Logging.Log("DedicatedBookmarkSalvagerBehavior", "salvage: attempting to open cargo hold", Logging.white);
                    if (!Cache.Instance.OpenCargoHold("DedicatedSalvageBehavior: Salvage")) break;
                    if (Settings.Instance.DebugSalvage) Logging.Log("DedicatedBookmarkSalvagerBehavior", "salvage: done opening cargo hold", Logging.white);
                    Cache.Instance.SalvageAll = true;
                    Cache.Instance.OpenWrecks = true;

                    const int distancetoccheck = (int)Distance.OnGridWithMe;
                    // is there any NPCs within distancetoconsidertargets?
                    EntityCache deadlyNPC = Cache.Instance.Entities.Where(t => t.Distance < distancetoccheck && !t.IsEntityIShouldLeaveAlone && !t.IsContainer && t.IsNpc && t.CategoryId == (int)CategoryID.Entity && t.GroupId != (int)Group.LargeCollidableStructure).OrderBy(t => t.Distance).FirstOrDefault();

                    if (deadlyNPC != null)
                    {
                        Logging.Log("DedicatedBookmarkSalvagerBehavior.Salvage", "Npc name:[" + deadlyNPC.Name + "] with groupId:[" + deadlyNPC.GroupId + "].", Logging.white);
                       
                        // found NPCs that will likely kill out fragile salvage boat!

                        DirectBookmark bookmark = Cache.Instance.AfterMissionSalvageBookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault();
                        if (bookmark != null)
                        {
                            Cache.Instance.DeleteBookmarksOnGrid("DedicatedBookmarkSalvageBehavior");
                            return;
                        }
                        else
                        {
                            Statistics.Instance.FinishedSalvaging = DateTime.Now;
                            Cache.Instance.NextSalvageTrip = DateTime.Now;
                            _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
                            return;
                        }
                    }
                    else
                    {
                        if (!Cache.Instance.OpenCargoHold("DedicatedBookmarkSalvageBehavior: Salvage")) break;

                        if (Settings.Instance.UnloadLootAtStation && Cache.Instance.CargoHold.Window.IsReady && (Cache.Instance.CargoHold.Capacity - Cache.Instance.CargoHold.UsedCapacity) < Settings.Instance.ReserveCargoCapacity)
                        {
                            Logging.Log("DedicatedBookmarkSalvageBehavior.Salvage", "We are full, go to base to unload", Logging.white);
                            _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
                            break;
                        }

                        if (!Cache.Instance.UnlootedContainers.Any())
                        {
                            Cache.Instance.DeleteBookmarksOnGrid("DedicatedBookmarkSalvageBehavior");
                            return;
                        }
                        if (Settings.Instance.DebugSalvage) Logging.Log("DedicatedBookmarkSalvagerBehavior", "salvage: we have more wrecks to salvage", Logging.white);
                        //we __cannot ever__ approach in salvage.cs so this section _is_ needed.
                        Salvage.MoveIntoRangeOfWrecks();
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

                case DedicatedBookmarkSalvagerBehaviorState.Default:
                    _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.Idle;
                    break;
            }
        }
    }
}