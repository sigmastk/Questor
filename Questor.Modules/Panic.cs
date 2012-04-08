// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------
namespace Questor.Modules
{
    using System;
    using System.Linq;

    public class Panic
    {
        private readonly Random _random = new Random();

        private double _lastNormalX;
        private double _lastNormalY;
        private double _lastNormalZ;

        private DateTime _resumeTime;
        private DateTime _nextDock = DateTime.Now;
        private DateTime _nextWarpTo = DateTime.Now;
        //private DateTime _lastDockedorJumping;
        private DateTime _lastWarpScrambled = DateTime.MinValue;
        private bool _delayedResume;
        private int _randomDelay;

        public PanicState State { get; set; }
        //public bool InMission { get; set; }

        public void ProcessState()
        {
            switch (State)
            {
                case PanicState.Normal:

                        if (Cache.Instance.DirectEve.ActiveShip.Entity != null)
                        {
                            _lastNormalX = Cache.Instance.DirectEve.ActiveShip.Entity.X;
                            _lastNormalY = Cache.Instance.DirectEve.ActiveShip.Entity.Y;
                            _lastNormalZ = Cache.Instance.DirectEve.ActiveShip.Entity.Z;
                        }
                        if (Cache.Instance.DirectEve.ActiveShip.Entity == null)
                            return;

                        if (Cache.Instance.DirectEve.ActiveShip.GroupId == (int)Group.Capsule)
                        {
                            Logging.Log("Panic: You are in a Capsule, you must have died :(");
                            State = PanicState.StartPanicking;
                        }
                        else if (Cache.Instance.InMission && Cache.Instance.InSpace && Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage < Settings.Instance.MinimumCapacitorPct && Cache.Instance.DirectEve.ActiveShip.GroupId != 31)
                        {
                            // Only check for cap-panic while in a mission, not while doing anything else
                            Logging.Log("Panic: Start panicking, capacitor [" + Math.Round(Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage,0) + "%] below [" + Settings.Instance.MinimumCapacitorPct + "%]");
                            //Questor.panic_attempts_this_mission;
                            Cache.Instance.PanicAttemptsThisMission = (Cache.Instance.PanicAttemptsThisMission + 1);
                            Cache.Instance.PanicAttemptsThisPocket = (Cache.Instance.PanicAttemptsThisPocket + 1);
                            Cache.Instance.IsMissionPocketDone = true;
                            State = PanicState.StartPanicking;
                        }
                        else if (Cache.Instance.InSpace && Cache.Instance.DirectEve.ActiveShip.ShieldPercentage < Settings.Instance.MinimumShieldPct)
                        {
                            Logging.Log("Panic: Start panicking, shield [" + Math.Round(Cache.Instance.DirectEve.ActiveShip.ShieldPercentage,0) + "%] below [" + Settings.Instance.MinimumShieldPct + "%]");
                            Cache.Instance.PanicAttemptsThisMission = (Cache.Instance.PanicAttemptsThisMission + 1);
                            Cache.Instance.PanicAttemptsThisPocket = (Cache.Instance.PanicAttemptsThisPocket + 1);
                            Cache.Instance.IsMissionPocketDone = true;
                            State = PanicState.StartPanicking;
                        }
                        else if (Cache.Instance.InSpace && Cache.Instance.DirectEve.ActiveShip.ArmorPercentage < Settings.Instance.MinimumArmorPct)
                        {
                            Logging.Log("Panic: Start panicking, armor [" + Math.Round(Cache.Instance.DirectEve.ActiveShip.ArmorPercentage, 0) + "%] below [" + Settings.Instance.MinimumArmorPct + "%]");
                            Cache.Instance.PanicAttemptsThisMission = (Cache.Instance.PanicAttemptsThisMission + 1);
                            Cache.Instance.PanicAttemptsThisPocket = (Cache.Instance.PanicAttemptsThisPocket + 1);
                            Cache.Instance.IsMissionPocketDone = true;
                            State = PanicState.StartPanicking;
                        }

                        _delayedResume = false;
                        if (Cache.Instance.InMission)
                        {
                            int frigates = Cache.Instance.Entities.Count(e => e.IsFrigate && e.IsPlayer);
                            int cruisers = Cache.Instance.Entities.Count(e => e.IsCruiser && e.IsPlayer);
                            int battlecruisers = Cache.Instance.Entities.Count(e => e.IsBattlecruiser && e.IsPlayer);
                            int battleships = Cache.Instance.Entities.Count(e => e.IsBattleship && e.IsPlayer);

                            if (Settings.Instance.FrigateInvasionLimit > 0 && frigates >= Settings.Instance.FrigateInvasionLimit)
                            {
                                _delayedResume = true;

                                Cache.Instance.PanicAttemptsThisMission = (Cache.Instance.PanicAttemptsThisMission + 1);
                                Cache.Instance.PanicAttemptsThisPocket = (Cache.Instance.PanicAttemptsThisPocket + 1);
                                Cache.Instance.IsMissionPocketDone = true;
                                State = PanicState.StartPanicking;
                                Logging.Log("Panic: Start panicking, mission invaded by [" + frigates + "] frigates");
                            }

                            if (Settings.Instance.CruiserInvasionLimit > 0 && cruisers >= Settings.Instance.CruiserInvasionLimit)
                            {
                                _delayedResume = true;

                                Cache.Instance.PanicAttemptsThisMission = (Cache.Instance.PanicAttemptsThisMission + 1);
                                Cache.Instance.PanicAttemptsThisPocket = (Cache.Instance.PanicAttemptsThisPocket + 1);
                                Cache.Instance.IsMissionPocketDone = true;
                                State = PanicState.StartPanicking;
                                Logging.Log("Panic: Start panicking, mission invaded by [" + cruisers + "] cruisers");
                            }

                            if (Settings.Instance.BattlecruiserInvasionLimit > 0 && battlecruisers >= Settings.Instance.BattlecruiserInvasionLimit)
                            {
                                _delayedResume = true;

                                Cache.Instance.PanicAttemptsThisMission = (Cache.Instance.PanicAttemptsThisMission + 1);
                                Cache.Instance.PanicAttemptsThisPocket = (Cache.Instance.PanicAttemptsThisPocket + 1);
                                Cache.Instance.IsMissionPocketDone = true;
                                State = PanicState.StartPanicking;
                                Logging.Log("Panic: Start panicking, mission invaded by [" + battlecruisers + "] battlecruisers");
                            }

                            if (Settings.Instance.BattleshipInvasionLimit > 0 && battleships >= Settings.Instance.BattleshipInvasionLimit)
                            {
                                _delayedResume = true;

                                Cache.Instance.PanicAttemptsThisMission = (Cache.Instance.PanicAttemptsThisMission + 1);
                                Cache.Instance.PanicAttemptsThisPocket = (Cache.Instance.PanicAttemptsThisPocket + 1);
                                Cache.Instance.IsMissionPocketDone = true;
                                State = PanicState.StartPanicking;
                                Logging.Log("Panic: Start panicking, mission invaded by [" + battleships + "] battleships");
                            }

                            if (_delayedResume)
                            {
                                _randomDelay = (Settings.Instance.InvasionRandomDelay > 0 ? _random.Next(Settings.Instance.InvasionRandomDelay) : 0);
                                _randomDelay += Settings.Instance.InvasionMinimumDelay;
                            }
                        }

                        Cache.Instance.AddPriorityTargets(Cache.Instance.TargetedBy.Where(t => t.IsWarpScramblingMe), Priority.WarpScrambler);
                        if (Settings.Instance.SpeedTank)
                        {
                            Cache.Instance.AddPriorityTargets(Cache.Instance.TargetedBy.Where(t => t.IsWebbingMe), Priority.Webbing);
                            Cache.Instance.AddPriorityTargets(Cache.Instance.TargetedBy.Where(t => t.IsTargetPaintingMe), Priority.TargetPainting);
                        }
                        Cache.Instance.AddPriorityTargets(Cache.Instance.TargetedBy.Where(t => t.IsNeutralizingMe), Priority.Neutralizing);
                        Cache.Instance.AddPriorityTargets(Cache.Instance.TargetedBy.Where(t => t.IsJammingMe), Priority.Jamming);
                        Cache.Instance.AddPriorityTargets(Cache.Instance.TargetedBy.Where(t => t.IsSensorDampeningMe), Priority.Dampening);
                        if (Cache.Instance.Modules.Any(m => m.IsTurret))
                            Cache.Instance.AddPriorityTargets(Cache.Instance.TargetedBy.Where(t => t.IsTrackingDisruptingMe), Priority.TrackingDisrupting);
                        break;

                // NOTE: The difference between Panicking and StartPanicking is that the bot will move to "Panic" state once in warp & Panicking 
                //       and the bot wont go into Panic mode while still "StartPanicking"
                case PanicState.StartPanicking:
                case PanicState.Panicking:
                    // Add any warp scramblers to the priority list
                    Cache.Instance.AddPriorityTargets(Cache.Instance.TargetedBy.Where(t => t.IsWarpScramblingMe), Priority.WarpScrambler);

                    // Failsafe, in theory would/should never happen
                    if (State == PanicState.Panicking && Cache.Instance.TargetedBy.Any(t => t.IsWarpScramblingMe))
                    {
                        // Resume is the only state that will make Questor revert to combat mode
                        State = PanicState.Resume;
                        return;
                    }

                    if (Cache.Instance.InStation)
                    {
                        Logging.Log("Panic: Entered a station, lower panic mode");
                        State = PanicState.Panic;
                    }

                    // Once we have warped off 500km, assume we are "safer"
                    if (State == PanicState.StartPanicking && Cache.Instance.DistanceFromMe(_lastNormalX, _lastNormalY, _lastNormalZ) > (int)Distance.PanicDistanceToConsiderSafelyWarpedOff)
                    {
                        Logging.Log("Panic: We've warped off");
                        State = PanicState.Panicking;
                    }

                    // We leave the panicking state once we actually start warping off
                    EntityCache station = Cache.Instance.Stations.FirstOrDefault();
                    if (station != null)
                    {
                        if (Cache.Instance.InWarp)
                            break;

                        if (station.Distance > (int)Distance.WarptoDistance)
                        {
                            if (DateTime.Now > _nextWarpTo)
                            {
                                Logging.Log("Panic: Warping to [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                                station.WarpToAndDock();
                                _nextWarpTo = DateTime.Now.AddSeconds(5);
                                _nextDock = DateTime.MinValue;
                            }
                        }
                        else if (DateTime.Now > _nextDock)
                        {
                            Logging.Log("Panic: Docking with [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                            station.Dock();
                            _nextDock = DateTime.Now.AddSeconds(5);
                        }
                        else
                        {
                            Logging.Log("Panic: Docking has been delayed until [" + _nextDock.ToString("HH:mm:ss") + "]");
                        }
                        break;
                    }
                    else
                    {
                        if (DateTime.Now.Subtract(Cache.Instance._lastLoggingAction).TotalSeconds > 15)
                        {
                            Logging.Log("No station found in local?");
                        }
                    }

                    // What is this you say?  No star?
                    if (Cache.Instance.Star == null)
                        break;

                    if (Cache.Instance.Star.Distance > (int)Distance.WeCanWarpToStarFromHere)
                    {
                        if (Cache.Instance.InWarp)
                            break;

                        if (Cache.Instance.TargetedBy.Any(t => t.IsWarpScramblingMe))
                        {
                            Logging.Log("Panic: We are still warp scrambled!"); //This runs every 'tick' so we should see it every 1.5 seconds or so
                            _lastWarpScrambled = DateTime.Now;
                        }
                        else if (DateTime.Now > _nextWarpTo | DateTime.Now.Subtract(_lastWarpScrambled).TotalSeconds < 10) //this will effectively spam warpto as soon as you are free of warp disruption if you were warp disrupted in the past 10 seconds
                        {
                            Logging.Log("Panic: Warping to [" + Cache.Instance.Star.Name + "] which is [" + Math.Round(Cache.Instance.Star.Distance/1000, 0) + "k away]");
                            Cache.Instance.Star.WarpTo();
                            _nextWarpTo = DateTime.Now.AddSeconds(5);
                        }
                        else
                        {
                            Logging.Log("Panic: Warping has been delayed until [" + _nextWarpTo.ToString("HH:mm:ss") + "]");
                        }
                    }
                    else
                    {
                        Logging.Log("Panic: At the star, lower panic mode");
                        State = PanicState.Panic;
                    }
                    break;

                case PanicState.Panic:
                    // Do not resume until you're no longer in a capsule
                    if (Cache.Instance.DirectEve.ActiveShip.GroupId == (int)Group.Capsule)
                        break;

                    if (Cache.Instance.InStation)
                    {
                        Logging.Log("Panic: We're in a station, resume mission");
                        State = _delayedResume ? PanicState.DelayedResume : PanicState.Resume;
                    }

                    bool isSafe = Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage > Settings.Instance.SafeCapacitorPct;
                    isSafe &= Cache.Instance.DirectEve.ActiveShip.ShieldPercentage > Settings.Instance.SafeShieldPct;
                    isSafe &= Cache.Instance.DirectEve.ActiveShip.ArmorPercentage > Settings.Instance.SafeArmorPct;
                    if (isSafe)
                    {
                        Logging.Log("Panic: We've recovered, resume mission");
                        Cache.Instance.IsMissionPocketDone = false;
                        State = _delayedResume ? PanicState.DelayedResume : PanicState.Resume;
                    }

                    if (State == PanicState.DelayedResume)
                    {
                        Logging.Log("Panic: Delaying resume for " + _randomDelay + " seconds");
                        Cache.Instance.IsMissionPocketDone = false;
                        _resumeTime = DateTime.Now.AddSeconds(_randomDelay);
                    }
                    break;

                case PanicState.DelayedResume:
                    if (DateTime.Now > _resumeTime)
                        State = PanicState.Resume;
                    break;

                case PanicState.Resume:
                    // Don't do anything here
                    break;
            }
        }
    }
}