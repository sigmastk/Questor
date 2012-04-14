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
    using System.Diagnostics;

    public class Defense
    {
        private DateTime _lastSessionChange = DateTime.MinValue;

        private void ActivateOnce()
        {
         if (DateTime.Now < Cache.Instance.NextActivateSupportModules) //if we just did something wait a fraction of a second
                return;

            foreach (ModuleCache module in Cache.Instance.Modules)
            {
                if (!module.IsActivatable)
                    continue;

                bool activate = false;
                activate |= module.GroupId == (int)Group.CloakingDevice;
                activate |= module.GroupId == (int)Group.ShieldHardeners;
                activate |= module.GroupId == (int)Group.DamageControl;
                activate |= module.GroupId == (int)Group.ArmorHardeners;
                activate |= module.GroupId == (int)Group.SensorBooster;
                activate |= module.GroupId == (int)Group.TrackingComputer;
                activate |= module.GroupId == (int)Group.ECCM;
                

                if (!activate)
                    continue;

                if (module.IsActive | module.IsGoingOnline | module.IsDeactivating | module.InLimboState)
                    continue;

                if (module.GroupId == (int)Group.CloakingDevice)
                {
                    //Logging.Log("This module has a typeID of: " + module.TypeId + " !!");
                    if (module.TypeId != 11578)  //11578 Covert Ops Cloaking Device - if you don't have a covert ops cloak try the next module
                    {
                        continue;
                    }
                    EntityCache stuffThatMayDecloakMe = Cache.Instance.Entities.Where(t => t.Name != Cache.Instance.DirectEve.Me.Name || t.IsBadIdea || t.IsContainer || t.IsNpc || t.IsPlayer).OrderBy(t => t.Distance).FirstOrDefault();
                    if (stuffThatMayDecloakMe != null && (stuffThatMayDecloakMe.Distance <= (int)Distance.SafeToCloakDistance)) //if their is anything within 2300m do not attempt to cloak
                    {
                        if (stuffThatMayDecloakMe.Distance != 0)
                        {
                            //Logging.Log(StuffThatMayDecloakMe.Name + " is very close at: " + StuffThatMayDecloakMe.Distance + " meters");
                            continue;
                        }
                    }
                }
                //
                // at this point the module should be active but isn't: activate it, set the delay and return. The process will resume on the next tick
                //
                module.Click();
                Cache.Instance.NextActivateSupportModules = DateTime.Now.AddMilliseconds((int)Time.DefenceDelay_milliseconds);
                Logging.Log("Defense: Defensive module activated: [ " + module.ItemId + "] next Activation delayed until [" + Cache.Instance.NextActivateSupportModules.ToString("HH:mm:ss") + "]");
                continue;
            }
        }

        private void ActivateRepairModules()
        {
            //var watch = new Stopwatch();
            if (DateTime.Now < Cache.Instance.NextRepModuleAction) //if we just did something wait a fraction of a second
                return;

            foreach (ModuleCache module in Cache.Instance.Modules)
            {
                if (module.InLimboState | module.IsDeactivating | module.IsGoingOnline | !module.IsOnline | !module.IsActivatable)
                    continue;

                double perc;
                if (module.GroupId == (int)Group.ShieldBoosters)
                {
                    perc = Cache.Instance.DirectEve.ActiveShip.ShieldPercentage;
                }
                else if (module.GroupId == (int)Group.ArmorRepairer)
                {
                    perc = Cache.Instance.DirectEve.ActiveShip.ArmorPercentage;
                }
                else
                    continue;

                bool inCombat = Cache.Instance.TargetedBy.Any();
                if (!module.IsActive && ((inCombat && perc < Settings.Instance.ActivateRepairModules) || (!inCombat && perc < Settings.Instance.DeactivateRepairModules && Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage > Settings.Instance.SafeCapacitorPct)))
                {
                    if (Cache.Instance.DirectEve.ActiveShip.ShieldPercentage < Cache.Instance.LowestShieldPercentageThisPocket)
                    {
                        Cache.Instance.LowestShieldPercentageThisPocket = Cache.Instance.DirectEve.ActiveShip.ShieldPercentage;
                        Cache.Instance.LowestShieldPercentageThisMission = Cache.Instance.DirectEve.ActiveShip.ShieldPercentage;
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    }
                    if (Cache.Instance.DirectEve.ActiveShip.ArmorPercentage < Cache.Instance.LowestArmorPercentageThisPocket)
                    {
                        Cache.Instance.LowestArmorPercentageThisPocket = Cache.Instance.DirectEve.ActiveShip.ArmorPercentage;
                        Cache.Instance.LowestArmorPercentageThisMission = Cache.Instance.DirectEve.ActiveShip.ArmorPercentage;
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    }
                    if (Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage < Cache.Instance.LowestCapacitorPercentageThisPocket)
                    {
                        Cache.Instance.LowestCapacitorPercentageThisPocket = Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage;
                        Cache.Instance.LowestCapacitorPercentageThisMission = Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage;
                        Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    }
                    if ((Cache.Instance.UnlootedContainers != null) && Cache.Instance.wrecksThisPocket != Cache.Instance.UnlootedContainers.Count())
                       Cache.Instance.wrecksThisPocket = Cache.Instance.UnlootedContainers.Count();

                    module.Click();
                    Cache.Instance.StartedBoosting = DateTime.Now;
                    Cache.Instance.NextRepModuleAction = DateTime.Now.AddMilliseconds((int)Time.DefenceDelay_milliseconds);
                    Logging.Log("Defense: RepModule   activated: [ " + module.ItemId + "]");
                    //Logging.Log("LowestShieldPercentage(pocket) [ " + Cache.Instance.lowest_shield_percentage_this_pocket + " ] ");
                    //Logging.Log("LowestArmorPercentage(pocket) [ " + Cache.Instance.lowest_armor_percentage_this_pocket + " ] ");
                    //Logging.Log("LowestCapacitorPercentage(pocket) [ " + Cache.Instance.lowest_capacitor_percentage_this_pocket + " ] ");
                    //Logging.Log("LowestShieldPercentage(mission) [ " + Cache.Instance.lowest_shield_percentage_this_mission + " ] ");
                    //Logging.Log("LowestArmorPercentage(mission) [ " + Cache.Instance.lowest_armor_percentage_this_mission + " ] ");
                    //Logging.Log("LowestCapacitorPercentage(mission) [ " + Cache.Instance.lowest_capacitor_percentage_this_mission + " ] ");
                    continue;
                }
                else if (module.IsActive && perc >= Settings.Instance.DeactivateRepairModules)
                {
                    //More human behavior
                    //System.Threading.Thread.Sleep(333);
                    module.Click();
                    Cache.Instance.NextRepModuleAction = DateTime.Now.AddMilliseconds((int)Time.DefenceDelay_milliseconds);
                    Cache.Instance.RepairCycleTimeThisPocket = Cache.Instance.RepairCycleTimeThisPocket + ((int)DateTime.Now.Subtract(Cache.Instance.StartedBoosting).TotalSeconds);
                    Cache.Instance.RepairCycleTimeThisMission = Cache.Instance.RepairCycleTimeThisMission + ((int)DateTime.Now.Subtract(Cache.Instance.StartedBoosting).TotalSeconds);
                    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    Cache.Instance.NextRepModuleAction = DateTime.Now.AddMilliseconds((int)Time.DefenceDelay_milliseconds);
                    Logging.Log("Defense: RepModule deactivated: [ " + module.ItemId + "][" + Math.Round(DateTime.Now.Subtract(Cache.Instance.NextRepModuleAction).TotalSeconds, 0) + "] sec reactivation delay");
                    //Cache.Instance.repair_cycle_time_this_pocket = Cache.Instance.repair_cycle_time_this_pocket + ((int)watch.Elapsed);
                    //Cache.Instance.repair_cycle_time_this_mission = Cache.Instance.repair_cycle_time_this_mission + watch.Elapsed.TotalMinutes;
                    continue;
                }
            }
        }

        private void ActivateAfterburner()
        {
            if (DateTime.Now < Cache.Instance.NextAfterburnerAction) //if we just did something wait a fraction of a second
                return; 
            
            foreach (ModuleCache module in Cache.Instance.Modules)
            {
                if (module.GroupId != (int)Group.Afterburner)
                    continue;

                if (module.InLimboState)
                    continue;

                // Should we activate the module
                bool activate = Cache.Instance.Approaching != null;
                activate &= !module.IsActive;
                activate &= !module.IsDeactivating;

                // Should we deactivate the module?
                bool deactivate = Cache.Instance.Approaching == null;
                deactivate &= module.IsActive;
                deactivate &= !module.IsDeactivating;
                deactivate &= (!Cache.Instance.Entities.Any(e => e.IsAttacking) || !Settings.Instance.SpeedTank);

                // This only applies when not speed tanking
                if (!Settings.Instance.SpeedTank && Cache.Instance.Approaching != null)
                {
                    // Activate if target is far enough
                    activate &= Cache.Instance.Approaching.Distance > Settings.Instance.MinimumPropulsionModuleDistance;
                    // Deactivate if target is too close
                    deactivate |= Cache.Instance.Approaching.Distance < Settings.Instance.MinimumPropulsionModuleDistance;
                }

                // If we have less then x% cap, do not activate or deactivate the module
                //Logging.Log("Defense: Current Cap [" + Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage + "]" + "Settings: minimumPropulsionModuleCapacitor [" + Settings.Instance.MinimumPropulsionModuleCapacitor + "]");              
                activate &= Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage > Settings.Instance.MinimumPropulsionModuleCapacitor;
                deactivate |= Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage < Settings.Instance.MinimumPropulsionModuleCapacitor;

                if (activate)
                {
                    module.Click();
                    Cache.Instance.NextAfterburnerAction = DateTime.Now.AddMilliseconds((int)Time.AfterburnerDelay_milliseconds);
                    return;
                }
                else if (deactivate && module.IsActive)
                {
                    module.Click();
                    Cache.Instance.NextAfterburnerAction = DateTime.Now.AddMilliseconds((int)Time.AfterburnerDelay_milliseconds);
                    return;
                }
            }
        }

        public void ProcessState()
        {
            // Thank god stations are safe ! :)
            if (Cache.Instance.InStation)
                return;

            if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
            {
                _lastSessionChange = DateTime.Now;
                return;
            }

            // What? No ship entity?
            if (Cache.Instance.DirectEve.ActiveShip.Entity == null)
            {
                _lastSessionChange = DateTime.Now;
                return;
            }

            if (DateTime.Now.Subtract(_lastSessionChange).TotalSeconds < 5)
            {
                Logging.Log("Defense: we just completed a session change less than 5 seconds ago... waiting.");
                return;
            }



            // There is no better defense then being cloaked ;)
            if (Cache.Instance.DirectEve.ActiveShip.Entity.IsCloaked)
                return;

            // Cap is SO low that we shouldn't care about hardeners/boosters as we aren't being targeted anyhow
            if (Cache.Instance.DirectEve.ActiveShip.CapacitorPercentage < 10 && !Cache.Instance.TargetedBy.Any())
                return;

            ActivateOnce();
            ActivateRepairModules();
            ActivateAfterburner();
        }
    }
}