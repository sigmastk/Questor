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
    using DirectEve;

    /// <summary>
    ///   The drones class will manage any and all drone related combat
    /// </summary>
    /// <remarks>
    ///   Drones will always work their way from lowest value target to highest value target and will only attack entities (not structures)
    /// </remarks>
    public class Drones
    {
        private double _armorPctTotal;
        private int _lastDroneCount;
        private DateTime _lastEngageCommand;
        private DateTime _lastRecallCommand;

        private int _recallCount;
        private DateTime _lastLaunch;
        private DateTime _lastRecall;

        private long _lastTarget;
        private DateTime _launchTimeout;
        private int _launchTries;
        private double _shieldPctTotal;
        private double _structurePctTotal;
        public bool Recall = false;

        public DroneState State { get; set; }

        private double GetShieldPctTotal()
        {
            if (!Cache.Instance.ActiveDrones.Any())
                return 0;

            return Cache.Instance.ActiveDrones.Sum(d => d.ShieldPct);
        }

        private double GetArmorPctTotal()
        {
            if (!Cache.Instance.ActiveDrones.Any())
                return 0;

            return Cache.Instance.ActiveDrones.Sum(d => d.ArmorPct);
        }

        private double GetStructurePctTotal()
        {
            if (!Cache.Instance.ActiveDrones.Any())
                return 0;

            return Cache.Instance.ActiveDrones.Sum(d => d.StructurePct);
        }

        /// <summary>
        ///   Return the best possible target
        /// </summary>
        /// <remarks>
        ///   Note this GetTarget works differently then the one from Combat
        /// </remarks>
        /// <returns></returns>
        private EntityCache GetTarget()
        {
            // Find the first active weapon's target
            EntityCache droneTarget = Cache.Instance.EntityById(_lastTarget);

            if (Cache.Instance.DronesKillHighValueTargets)
            {
                // Return best possible high value target
                return Cache.Instance.GetBestTarget(droneTarget, Settings.Instance.DroneControlRange, false);
            }
            else
            {
                // Return best possible low value target
                return Cache.Instance.GetBestTarget(droneTarget, Settings.Instance.DroneControlRange, true);
            }
        }

        /// <summary>
        ///   Engage the target
        /// </summary>
        private void EngageTarget()
        {
            EntityCache target = GetTarget();

            // Nothing to engage yet, probably retargeting 
            if (target == null)
                return;

            if (target.IsBadIdea)
                return;

            // Is our current target still the same and is the last Engage command no longer then 15s ago?
            if (_lastTarget == target.Id && DateTime.Now.Subtract(_lastEngageCommand).TotalSeconds < 15)
                return;

            // Are we still actively shooting at the target?
            bool mustEngage = false;
            foreach (EntityCache drone in Cache.Instance.ActiveDrones)
                mustEngage |= drone.FollowId != target.Id;
            if (!mustEngage)
                return;

            // Is the last target our current active target?
            if (target.IsActiveTarget)
            {
                // Save target id (so we do not constantly switch)
                _lastTarget = target.Id;

                // Engage target
                Logging.Log("Drones: Engaging drones on [" + target.Name + "][ID: " + target.Id + "]" + Math.Round(target.Distance / 1000, 0) + "k away]");
                Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesEngage);
                _lastEngageCommand = DateTime.Now;
            }
            else // Make the target active
            {
                target.MakeActiveTarget();
                Logging.Log("Drones: [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance / 1000, 0) + "k away] is now the target for drones");
            }
        }

        public void ProcessState()
        {
            if (!Settings.Instance.UseDrones)
                return;

            switch (State)
            {
                case DroneState.WaitingForTargets:
                    // Are we in the right state ?
                    if (Cache.Instance.ActiveDrones.Any())
                    {
                        // Apparently not, we have drones out, go into fight mode
                        State = DroneState.Fighting;
                        break;
                    }

                    // Should we launch drones?
                    bool launch = true;
                    // Always launch if we're scrambled
                    if (!Cache.Instance.PriorityTargets.Any(pt => pt.IsWarpScramblingMe))
                    {
                        launch &= Cache.Instance.UseDrones;
                        // Are we done with this mission pocket?
                        launch &= !Cache.Instance.IsMissionPocketDone;

                        // If above minimums
                        launch &= Cache.Instance.DirectEve.ActiveShip.ShieldPercentage >= Settings.Instance.DroneMinimumShieldPct;
                        launch &= Cache.Instance.DirectEve.ActiveShip.ArmorPercentage >= Settings.Instance.DroneMinimumArmorPct;
                        launch &= Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage >= Settings.Instance.DroneMinimumCapacitorPct;

                        // yes if there are targets to kill 
                        launch &= Cache.Instance.TargetedBy.Count(e => !e.IsSentry && e.CategoryId == (int)CategoryID.Entity && e.IsNpc && !e.IsContainer && e.GroupId != (int)Group.LargeCollidableStructure && e.Distance < Settings.Instance.DroneControlRange) > 0;

                        // If drones get aggro'd within 30 seconds, then wait (5 * _recallCount + 5) seconds since the last recall
                        if (_lastLaunch < _lastRecall && _lastRecall.Subtract(_lastLaunch).TotalSeconds < 30)
                        {
                            if (_lastRecall.AddSeconds(5 * _recallCount + 5) < DateTime.Now)
                            {
                                // Increase recall count and allow the launch
                                _recallCount++;

                                // Never let _recallCount go above 5
                                if (_recallCount > 5)
                                    _recallCount = 5;
                            }
                            else
                            {
                                // Do not launch the drones until the delay has passed
                                launch = false;
                            }
                        }
                        else // Drones have been out for more then 30s 
                            _recallCount = 0;
                    }

                    if (launch)
                    {
                        // Reset launch tries
                        _launchTries = 0;
                        _lastLaunch = DateTime.Now;
                        State = DroneState.Launch;
                    }
                    break;

                case DroneState.Launch:
                    // Launch all drones
                    Recall = false;
                    _launchTimeout = DateTime.Now;
                    Cache.Instance.DirectEve.ActiveShip.LaunchAllDrones();
                    State = DroneState.Launching;
                    break;

                case DroneState.Launching:
                    // We haven't launched anything yet, keep waiting
                    if (!Cache.Instance.ActiveDrones.Any())
                    {
                        if (DateTime.Now.Subtract(_launchTimeout).TotalSeconds > 10)
                        {
                            // Relaunch if tries < 10
                            if (_launchTries < 10)
                            {
                                _launchTries++;
                                State = DroneState.Launch;
                                break;
                            }
                            else
                                State = DroneState.OutOfDrones;
                        }
                        break;
                    }

                    // Are we done launching?
                    if (_lastDroneCount == Cache.Instance.ActiveDrones.Count())
                        State = DroneState.Fighting;
                    break;

                case DroneState.OutOfDrones:
                    //if (DateTime.Now.Subtract(_launchTimeout).TotalSeconds > 1000)
                    //{
                    //    State = DroneState.WaitingForTargets;
                    //}
                    break;

                case DroneState.Fighting:
                    // Should we recall our drones? This is a possible list of reasons why we should
                    
                    // Are we done (for now) ? 
                    if (Cache.Instance.TargetedBy.Count(e => !e.IsSentry && e.IsNpc && e.Distance < Settings.Instance.DroneControlRange) == 0)
                    {
                        Logging.Log("Drones: Recalling drones because no NPC is targeting us within dronerange");
                        Recall = true;
                    }

                    if (Cache.Instance.IsMissionPocketDone)
                    {
                        Logging.Log("Drones: Recalling drones because we are done with this pocket.");
                        Recall = true;
                    }
                    else if (_shieldPctTotal > GetShieldPctTotal())
                    {
                        Logging.Log("Drones: Recalling drones because drones have lost some shields! [Old: " + _shieldPctTotal.ToString("N2") + "][New: " + GetShieldPctTotal().ToString("N2") + "]");
                        Recall = true;
                    }
                    else if (_armorPctTotal > GetArmorPctTotal())
                    {
                        Logging.Log("Drones: Recalling drones because drones have lost some armor! [Old:" + _armorPctTotal.ToString("N2") + "][New: " + GetArmorPctTotal().ToString("N2") + "]");
                        Recall = true;
                    }
                    else if (_structurePctTotal > GetStructurePctTotal())
                    {
                        Logging.Log("Drones: Recalling drones because drones have lost some structure! [Old:" + _structurePctTotal.ToString("N2") + "][New: " + GetStructurePctTotal().ToString("N2") + "]");
                        Recall = true;
                    }
                    else if (Cache.Instance.ActiveDrones.Count() < _lastDroneCount)
                    {
                        // Did we lose a drone? (this should be covered by total's as well though)
                        Logging.Log("Drones: Recalling drones because we have lost a drone! [Old:" + _lastDroneCount + "][New: " + Cache.Instance.ActiveDrones.Count() + "]");
                        Recall = true;
                    }
                    else
                    {
                        // Default to long range recall
                        int lowShieldWarning = Settings.Instance.LongRangeDroneRecallShieldPct;
                        int lowArmorWarning = Settings.Instance.LongRangeDroneRecallArmorPct;
                        int lowCapWarning = Settings.Instance.LongRangeDroneRecallCapacitorPct;

                        if (Cache.Instance.ActiveDrones.Average(d => d.Distance) < (Settings.Instance.DroneControlRange/2d))
                        {
                            lowShieldWarning = Settings.Instance.DroneRecallShieldPct;
                            lowArmorWarning = Settings.Instance.DroneRecallArmorPct;
                            lowCapWarning = Settings.Instance.DroneRecallCapacitorPct;
                        }

                        if (Cache.Instance.DirectEve.ActiveShip.ShieldPercentage < lowShieldWarning)
                        {
                            Logging.Log("Drones: Recalling drones due to shield [" + Cache.Instance.DirectEve.ActiveShip.ShieldPercentage + "%] below [" + lowShieldWarning + "%] minimum");
                            Recall = true;
                        }
                        else if (Cache.Instance.DirectEve.ActiveShip.ArmorPercentage < lowArmorWarning)
                        {
                            Logging.Log("Drones: Recalling drones due to armor [" + Cache.Instance.DirectEve.ActiveShip.ArmorPercentage + "%] below [" + lowArmorWarning + "%] minimum");
                            Recall = true;
                        }
                        else if (Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage < lowCapWarning)
                        {
                            Logging.Log("Drones: Recalling drones due to capacitor [" + Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage + "%] below [" + lowCapWarning + "%] minimum");
                            Recall = true;
                        }
                    }

                    if (!Cache.Instance.ActiveDrones.Any())
                    {
                        Logging.Log("Drones: Apparently we have lost all our drones");
                        Recall = true;
                    }
                    else
                    {
                        if (Cache.Instance.PriorityTargets.Any(pt => pt.IsWarpScramblingMe) && Recall)
                        {
                            Logging.Log("Drones: Overriding drone recall, we are scrambled!");
                            Recall = false;
                        }
                    }

                    // Recall or engage
                    if (Recall)
                        State = DroneState.Recalling;
                    else
                    {
                        EngageTarget();

                        // We lost a drone and did not recall, assume panicking and launch (if any) additional drones
                        if (Cache.Instance.ActiveDrones.Count() < _lastDroneCount)
                            State = DroneState.Launch;
                    }
                    break;

                case DroneState.Recalling:
                    // Are we done?
                    if (!Cache.Instance.ActiveDrones.Any())
                    {
                        _lastRecall = DateTime.Now;
                        Recall = false;
                        State = DroneState.WaitingForTargets;
                        break;
                    }

                    // Give recall command every 5 seconds
                    if (DateTime.Now.Subtract(_lastRecallCommand).TotalSeconds > 5)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnToBay);
                        _lastRecallCommand = DateTime.Now;
                    }
                    break;
            }

            // Update health values
            _shieldPctTotal = GetShieldPctTotal();
            _armorPctTotal = GetArmorPctTotal();
            _structurePctTotal = GetStructurePctTotal();
            _lastDroneCount = Cache.Instance.ActiveDrones.Count();
        }
    }
}