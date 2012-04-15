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
    using System.Collections.Generic;
    using System.Linq;
    using DirectEve;
    using Questor.Modules;

    /// <summary>
    ///   The combat class will target and kill any NPC that is targeting the questor.
    ///   It will also kill any NPC that is targeted but not aggressing the questor.
    /// </summary>
    public class Combat
    {
        private readonly Dictionary<long, DateTime> _lastModuleActivation = new Dictionary<long, DateTime>();
        private readonly Dictionary<long, DateTime> _lastWeaponReload = new Dictionary<long, DateTime>();
        private bool _isJammed;
        public CombatState State { get; set; }
        private int MaxCharges { get; set; }

        /// <summary> Reload correct (tm) ammo for the NPC
        /// </summary>
        /// <param name = "weapon"></param>
        /// <param name = "entity"></param>
        /// <returns>True if the (enough/correct) ammo is loaded, false if wrong/not enough ammo is loaded</returns>
        public bool ReloadNormalAmmo(ModuleCache weapon, EntityCache entity)
        {
            DirectContainer cargo = Cache.Instance.DirectEve.GetShipsCargo();

            // Get ammo based on damage type
            IEnumerable<Ammo> correctAmmo = Settings.Instance.Ammo.Where(a => a.DamageType == Cache.Instance.DamageType);

            // Check if we still have that ammo in our cargo
            correctAmmo = correctAmmo.Where(a => cargo.Items.Any(i => i.TypeId == a.TypeId && i.Quantity >= Settings.Instance.MinimumAmmoCharges));

            //check if mission specific ammo is defined
            if (Cache.Instance.MissionAmmo.Count() != 0)
            {
                correctAmmo = Cache.Instance.MissionAmmo.Where(a => a.DamageType == Cache.Instance.DamageType);
            }

            // Check if we still have that ammo in our cargo
            correctAmmo = correctAmmo.Where(a => cargo.Items.Any(i => i.TypeId == a.TypeId && i.Quantity >= Settings.Instance.MinimumAmmoCharges));
            if (Cache.Instance.MissionAmmo.Count() != 0)
            {
                correctAmmo = Cache.Instance.MissionAmmo;
            }


            // We are out of ammo! :(
            if (!correctAmmo.Any())
            {
                State = CombatState.OutOfAmmo;
                return false;
            }

            // Get the best possible ammo
            Ammo ammo = correctAmmo.Where(a => a.Range > entity.Distance).OrderBy(a => a.Range).FirstOrDefault();

            // We do not have any ammo left that can hit targets at that range!
            if (ammo == null)
                return false;

            // We have enough ammo loaded
            if (weapon.Charge != null && weapon.Charge.TypeId == ammo.TypeId && weapon.CurrentCharges >= Settings.Instance.MinimumAmmoCharges)
                return true;

            // Retry later, assume its ok now
            if (!weapon.MatchingAmmo.Any())
                return true;

            DirectItem charge = cargo.Items.FirstOrDefault(i => i.TypeId == ammo.TypeId && i.Quantity >= Settings.Instance.MinimumAmmoCharges);
            // This should have shown up as "out of ammo"
            if (charge == null)
                return false;

            // We are reloading, wait Time.ReloadWeaponDelayBeforeUsable_seconds (see time.cs)
            if (_lastWeaponReload.ContainsKey(weapon.ItemId) && DateTime.Now < _lastWeaponReload[weapon.ItemId].AddSeconds((int)Time.ReloadWeaponDelayBeforeUsable_seconds))
                return false;
            _lastWeaponReload[weapon.ItemId] = DateTime.Now;

            // Reload or change ammo
            if (weapon.Charge != null && weapon.Charge.TypeId == charge.TypeId)
            {
                if (DateTime.Now.Subtract(Cache.Instance._lastLoggingAction).TotalSeconds > 10)
                { 
                    Cache.Instance.TimeSpentReloading_seconds = Cache.Instance.TimeSpentReloading_seconds + (int)Time.ReloadWeaponDelayBeforeUsable_seconds;
                    Cache.Instance._lastLoggingAction = DateTime.Now;
                }
                Logging.Log("Combat: Reloading [" + weapon.ItemId + "] with [" + charge.TypeName + "][TypeID: " + charge.TypeId + "]");
                weapon.ReloadAmmo(charge);
            }
            else
            {
                if (DateTime.Now.Subtract(Cache.Instance._lastLoggingAction).TotalSeconds > 10)
                {
                    Cache.Instance.TimeSpentReloading_seconds = Cache.Instance.TimeSpentReloading_seconds + (int)Time.ReloadWeaponDelayBeforeUsable_seconds;
                    Cache.Instance._lastLoggingAction = DateTime.Now;
                }
                Logging.Log("Combat: Changing [" + weapon.ItemId + "] with [" + charge.TypeName + "][TypeID: " + charge.TypeId + "]");
                weapon.ChangeAmmo(charge);
            }

            // Return false as we are reloading ammo
            return false;
        }

        public bool ReloadEnergyWeaponAmmo(ModuleCache weapon, EntityCache entity)
        {
            DirectContainer cargo = Cache.Instance.DirectEve.GetShipsCargo();

            // Get ammo based on damage type
            IEnumerable<Ammo> correctAmmo = Settings.Instance.Ammo.Where(a => a.DamageType == Cache.Instance.DamageType);

            // Check if we still have that ammo in our cargo
            correctAmmo = correctAmmo.Where(a => cargo.Items.Any(i => i.TypeId == a.TypeId));

            // We are out of ammo! :(
            if (!correctAmmo.Any())
            {
                State = CombatState.OutOfAmmo;
                return false;
            }

            // Get the best possible ammo - energy weapons change ammo near instantly
            Ammo ammo = correctAmmo.Where(a => a.Range > (entity.Distance)).OrderBy(a => a.Range).FirstOrDefault(); //default

            // We do not have any ammo left that can hit targets at that range!
            if (ammo == null)
                return false;

            DirectItem charge = cargo.Items.OrderBy(i => i.Quantity).FirstOrDefault(i => i.TypeId == ammo.TypeId);
            // We do not have any ammo left that can hit targets at that range!
            if (charge == null)
                return false;

            // We have enough ammo loaded
            if (weapon.Charge != null && weapon.Charge.TypeId == ammo.TypeId)
                return true;

            // We are reloading, wait at least 5 seconds
            if (_lastWeaponReload.ContainsKey(weapon.ItemId) && DateTime.Now < _lastWeaponReload[weapon.ItemId].AddSeconds(5))
                return false;
            _lastWeaponReload[weapon.ItemId] = DateTime.Now;

            // Reload or change ammo
            if (weapon.Charge != null && weapon.Charge.TypeId == charge.TypeId)
            {
                Logging.Log("Combat: Reloading [" + weapon.ItemId + "] with [" + charge.TypeName + "][TypeID: " + charge.TypeId + "]");
                weapon.ReloadAmmo(charge);
            }
            else
            {
                Logging.Log("Combat: Changing [" + weapon.ItemId + "] with [" + charge.TypeName + "][TypeID: " + charge.TypeId + "]");
                weapon.ChangeAmmo(charge);
            }

            // Return false as we are reloading ammo
            return false;
        }

        /// <summary> Reload correct (tm) ammo for the NPC
        /// </summary>
        /// <param name = "weapon"></param>
        /// <param name = "entity"></param>
        /// <returns>True if the (enough/correct) ammo is loaded, false if wrong/not enough ammo is loaded</returns>
        public bool ReloadAmmo(ModuleCache weapon, EntityCache entity)
        {
            // We need the cargo bay open for both reload actions
            DirectContainer cargo = Cache.Instance.DirectEve.GetShipsCargo();
            if (cargo.Window == null)
            {
                Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
                return false;
            }

            if (!cargo.IsReady)
                return false;

            return weapon.IsEnergyWeapon ? ReloadEnergyWeaponAmmo(weapon, entity) : ReloadNormalAmmo(weapon, entity);
        }

       /// <summary> Returns true if it can activate the weapon on the target
        /// </summary>
        /// <remarks>
        ///   The idea behind this function is that a target that explodes isn't being fired on within 5 seconds
        /// </remarks>
        /// <param name = "module"></param>
        /// <param name = "entity"></param>
        /// <param name = "isWeapon"></param>
        /// <returns></returns>
        public bool CanActivate(ModuleCache module, EntityCache entity, bool isWeapon)
        {
            // We have changed target, allow activation
            if (entity.Id != module.LastTargetId)
                return true;

            // We have reloaded, allow activation
            if (isWeapon && module.CurrentCharges == MaxCharges)
                return true;

            // We haven't reloaded, insert a wait-time
            if (_lastModuleActivation.ContainsKey(module.ItemId))
            {
                if (DateTime.Now.Subtract(_lastModuleActivation[module.ItemId]).TotalSeconds < 3)
                    return false;

                _lastModuleActivation.Remove(module.ItemId);
                return true;
            }

            _lastModuleActivation.Add(module.ItemId, DateTime.Now);
            return false;
        }

        /// <summary> Returns the target we need to activate everything on
        /// </summary>
        /// <returns></returns>
        private EntityCache GetTarget()
        {
            // Find the first active weapon's target
            EntityCache weaponTarget = null;
            foreach (ModuleCache weapon in Cache.Instance.Weapons.Where(m => m.IsActive))
            {
                // Find the target associated with the weapon
                weaponTarget = Cache.Instance.EntityById(weapon.TargetId);
                if (weaponTarget != null)
                    break;
            }

            // Return best possible target
            return Cache.Instance.GetBestTarget(weaponTarget, Cache.Instance.WeaponRange, false);
        }

        /// <summary> Activate weapons
        /// </summary>
        private void ActivateWeapons(EntityCache target)
        {
           if (DateTime.Now < Cache.Instance.NextWeaponAction) //if we just did something wait a fraction of a second
                return;

         if (Settings.Instance.SpeedTank && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != target.Id))
         {
            if (DateTime.Now > Cache.Instance.NextOrbit)
            {
               target.Orbit(Cache.Instance.OrbitDistance);
               Logging.Log("Combat.ActivateWeapons: Initiating Orbit [" + target.Name + "][ID: " + target.Id + "]");
               Cache.Instance.NextOrbit = DateTime.Now.AddSeconds((int)Time.OrbitDelay_seconds);
            }
         }
            //
            // Do we really want a non-mission action moving the ship around at all!! (other than speed tanking)?
            // If you are not in a mission by all means let combat actions move you around as needed
            if (!Cache.Instance.InMission)
            {
                //why not keep here approach and orbit? it would work for all of of mission controller states as well as combat helper
                if (DateTime.Now > Cache.Instance.NextOrbit)
                {
                    if (target.Distance + (int)Cache.Instance.OrbitDistance < Cache.Instance.MaxRange)
                    {
                        Logging.Log("MissionController.StartOrbiting: Target in range");
                        if (!Cache.Instance.IsApproachingOrOrbiting)
                        {
                            Logging.Log("We are not approaching nor orbiting");
                            var orbitStructure = false;
                            var structure = Cache.Instance.Entities.Where(i => i.GroupId == (int)Group.LargeCollidableStructure || i.Name.Contains("Gate") || i.Name.Contains("Beacon")).OrderBy(t => t.Distance).OrderBy(t => t.Distance).FirstOrDefault();

                            if (orbitStructure && structure != null)
                            {
                                structure.Orbit((int)Cache.Instance.OrbitDistance);
                                Logging.Log("MissionController.Combat: Initiating Orbit [" + structure.Name + "][ID: " + structure.Id + "]");
                            }
                            else
                            {
                                target.Orbit(Cache.Instance.OrbitDistance);
                                Logging.Log("Combat.ActivateWeapons: Initiating Orbit [" + target.Name + "][ID: " + target.Id + "]");
                            }
                            Cache.Instance.NextOrbit = DateTime.Now.AddSeconds((int)Time.OrbitDelay_seconds);
                            return;
                         }
                    }
                    else
                    {
                        Logging.Log("Combat: Possible out of range. ignoring orbit around structure");
                        target.Orbit(Cache.Instance.OrbitDistance);
                        Logging.Log("Combat.Combat: Initiating Orbit [" + target.Name + "][ID: " + target.Id + "]");
                        Cache.Instance.NextOrbit = DateTime.Now.AddSeconds((int)Time.OrbitDelay_seconds);
                        return;
                    }
                    //else Logging.Log("MissionControler:Too soon to orbit. Next orbiting delayed until [" + Cache.Instance.NextOrbit.ToString("HH:mm:ss") + "]");
                }
                else
                {
                  if (DateTime.Now > Cache.Instance.NextApproachAction)
                {
                    if (Settings.Instance.OptimalRange != 0)
                    {
                        if (target.Distance > Settings.Instance.OptimalRange + (int)Distance.OptimalRangeCushion && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != target.Id))
                        {
                            target.Approach(Settings.Instance.OptimalRange);
                            Logging.Log("Combat.ActivateWeapons:: Using Optimal Range: Approaching target [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance / 1000, 0) + "k away]");
                        }
                        //ahaw: I think when approach distance will be reached ship will be stoppedd so this is not needed
                        if (target.Distance <= Settings.Instance.OptimalRange && Cache.Instance.Approaching != null)
                        {
                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                            Cache.Instance.Approaching = null;
                            Logging.Log("Combat.ActivateWeapons: Using Optimal Range: Stop ship, target at [" + Math.Round(target.Distance / 1000, 0) + "k away] is inside optimal");
                        }
                    }
                    else
                    {
                        if (target.Distance > Cache.Instance.MaxRange && (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != target.Id))
                        {
                            target.Approach((int)(Cache.Instance.WeaponRange * 0.8d));
                            Logging.Log("Combat.ActivateWeapons: Using Weapons Range: Approaching target [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance / 1000, 0) + "k away]");
                        }
                        //ahaw: I think when approach distance will be reached ship will be stoppedd so this is not needed
                        if (target.Distance <= Cache.Instance.MaxRange && Cache.Instance.Approaching != null)
                        {
                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                            Cache.Instance.Approaching = null;
                            Logging.Log("Combat.ActivateWeapons: Using Weapons Range: Stop ship, target is in orbit range");
                        }
                    }
                    Cache.Instance.NextApproachAction = DateTime.Now.AddSeconds((int)Time.ApproachDelay_seconds);
                    return;
                 }
                }
            }

            // Get the weapons
            IEnumerable<ModuleCache> weapons = Cache.Instance.Weapons;

            // TODO: Add check to see if there is better ammo to use! :)
            // Get distance of the target and compare that with the ammo currently loaded
            foreach (ModuleCache weapon in weapons)
            {
                // don't waste ammo on small target if you use autocannon or siege i hope you use drone
                if (Settings.Instance.DontShootFrigatesWithSiegeorAutoCannons) //this defaults to false and needs to be changed in your characters settings xml file if you want to enable this option
                {
                    if (Settings.Instance.WeaponGroupId == 55 || Settings.Instance.WeaponGroupId == 508 || Settings.Instance.WeaponGroupId == 506)
                    {
                       if (target.Distance <= (int)Distance.InsideThisRangeIsLIkelyToBeMostlyFrigates && !target.TargetValue.HasValue && target.GroupId != (int)Group.LargeCollidableStructure)
                       {
                          weapon.Click();
                       }
                    }
                }
                if (!weapon.IsActive)
                    continue;

                if (weapon.IsReloadingAmmo || weapon.IsDeactivating || weapon.IsChangingAmmo)
                    continue;

                if (DateTime.Now < Cache.Instance.NextReload) //if we should not yet reload we are likely in the middle of a reload and should wait!
                    return;

                // No ammo loaded
                if (weapon.Charge == null)
                    continue;

                Ammo ammo = Settings.Instance.Ammo.FirstOrDefault(a => a.TypeId == weapon.Charge.TypeId);

                //use mission specific ammo
                if (Cache.Instance.MissionAmmo.Count() != 0)
                {
                    ammo = Cache.Instance.MissionAmmo.FirstOrDefault(a => a.TypeId == weapon.Charge.TypeId);
                }

                // How can this happen? Someone manually loaded ammo
                if (ammo == null)
                    continue;

                // If we have already activated warp, deactivate the weapons
                if (!Cache.Instance.DirectEve.ActiveShip.Entity.IsWarping)
                {
                    // Target is in range
                    if(target.Distance <= ammo.Range)
                    continue;
                }
                // Target is out of range, stop firing
                weapon.Click();
            }

            // Hax for max charges returning incorrect value
            if (!weapons.Any(w => w.IsEnergyWeapon))
            {
                MaxCharges = Math.Max(MaxCharges, weapons.Max(l => l.MaxCharges));
                MaxCharges = Math.Max(MaxCharges, weapons.Max(l => l.CurrentCharges));
            }

            // Activate the weapons (it not yet activated)))
            foreach (ModuleCache weapon in weapons)
            {
                // Are we reloading, deactivating or changing ammo?
                if (weapon.IsReloadingAmmo || weapon.IsDeactivating || weapon.IsChangingAmmo)
                    continue;
                // Are we on the right target?
                if (weapon.IsActive)
                {
                    if (weapon.TargetId != target.Id)
                       weapon.Click();

                    continue;
                }

                // No, check ammo type and if that is correct, activate weapon
                if (ReloadAmmo(weapon, target) && CanActivate(weapon, target, true))
                {
                    Logging.Log("Combat: Activating weapon  [" + weapon.ItemId + "] on [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance/1000,0) + "k away]");
                    weapon.Activate(target.Id);
                    Cache.Instance.NextWeaponAction = DateTime.Now.AddMilliseconds((int)Time.WeaponDelay_milliseconds);
                    //we know we are connected if we were able to get this far - update the lastknownGoodConnectedTime
                    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    return;
                }
            }
        }

        /// <summary> Activate target painters
        /// </summary>
        public void ActivateTargetPainters(EntityCache target)
        {
            if (DateTime.Now < Cache.Instance.NextWeaponAction) //if we just did something wait a fraction of a second
                return;

            List<ModuleCache> targetPainters = Cache.Instance.Modules.Where(m => m.GroupId == (int)Group.TargetPainter).ToList();

            // Find the first active weapon
            // Assist this weapon
            foreach (ModuleCache painter in targetPainters)
            {
                // Are we on the right target?
                if (painter.IsActive)
                {
                    if (painter.TargetId != target.Id)
                       painter.Click();

                    continue;
                }

                // Are we deactivating?
                if (painter.IsDeactivating)
                    continue;

                if (CanActivate(painter, target, false))
                {
                    Logging.Log("Combat: Activating painter [" + painter.ItemId + "] on [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance/1000,0) + "k away]");
                    painter.Activate(target.Id);
                    Cache.Instance.NextPainterAction = DateTime.Now.AddMilliseconds((int)Time.PainterDelay_milliseconds);
                    return;
                }
            }
        }

        /// <summary> Activate Nos
        /// </summary>
        public void ActivateNos(EntityCache target)
        {
            if (DateTime.Now < Cache.Instance.NextWeaponAction) //if we just did something wait a fraction of a second
                return;

            List<ModuleCache> noses = Cache.Instance.Modules.Where(m => m.GroupId == (int)Group.NOS).ToList();
            //Logging.Log("Combat: we have " + noses.Count.ToString() + " Nos modules");
            // Find the first active weapon
            // Assist this weapon
            foreach (ModuleCache nos in noses)
            {
                // Are we on the right target?
                if (nos.IsActive)
                {
                    if (nos.TargetId != target.Id)
                       nos.Click();

                    continue;
                }

                // Are we deactivating?
                if (nos.IsDeactivating)
                    continue;
                //Logging.Log("Combat: Distances Target[ " + Math.Round(target.Distance,0) + " Optimal[" + nos.OptimalRange.ToString()+"]");
                // Target is out of Nos range
                if (target.Distance >= Settings.Instance.NosDistance)
                    continue;

                if (CanActivate(nos, target, false))
                {
                    Logging.Log("Combat: Activating Nos     [" + nos.ItemId + "] on [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance/1000,0) + "k away]");
                    nos.Activate(target.Id);
                    Cache.Instance.NextNosAction = DateTime.Now.AddMilliseconds((int)Time.NosDelay_milliseconds);
                    return;
                }
                else
                {
                    Logging.Log("Combat: Cannot Activate Nos [" + nos.ItemId + "] on [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance / 1000, 0) + "k away]");
                }
            }
        }

        /// <summary> Activate StasisWeb
        /// </summary>
        public void ActivateStasisWeb(EntityCache target)
        {
            if (DateTime.Now < Cache.Instance.NextWeaponAction) //if we just did something wait a fraction of a second
                return;

            List<ModuleCache> webs = Cache.Instance.Modules.Where(m => m.GroupId == (int)Group.StasisWeb).ToList();

            // Find the first active weapon
            // Assist this weapon
            foreach (ModuleCache web in webs)
            {
                // Are we on the right target?
                if (web.IsActive)
                {
                    if (web.TargetId != target.Id)
                       web.Click();

                    continue;
                }

                // Are we deactivating?
                if (web.IsDeactivating)
                    continue;

                // Target is out of web range
                if (target.Distance >= web.OptimalRange)
                    continue;

                if (CanActivate(web, target, false))
                {
                    Logging.Log("Combat: Activating web     [" + web.ItemId + "] on [" + target.Name + "][ID: " + target.Id + "]");
                    web.Activate(target.Id);
                    Cache.Instance.NextWebAction = DateTime.Now.AddMilliseconds((int)Time.WebDelay_milliseconds);
                    return; 
                }
            }
        }

        /// <summary> Target combatants
        /// </summary>
        /// <remarks>
        ///   This only targets ships that are targeting you
        /// </remarks>
        private void TargetCombatants()
        {
            if (DateTime.Now < Cache.Instance.NextTargetAction) //if we just did something wait a fraction of a second
                return;

            // We are jammed, forget targeting anything...
            if (Cache.Instance.DirectEve.ActiveShip.MaxLockedTargets == 0)
            {
                if (!_isJammed)
                {
                    Logging.Log("Combat: We are jammed and can't target anything");
                }

                _isJammed = true;
                return;
            }

            if (_isJammed)
            {
                // Clear targeting list as it doesn't apply
                Cache.Instance.TargetingIDs.Clear();
                Logging.Log("Combat: We are no longer jammed, retargeting");
            }
            _isJammed = false;
            
            //
            // ???bounty tracking code goes here???
            //

            // What is the range that we can target at
            double maxRange = Math.Min(Cache.Instance.DirectEve.ActiveShip.MaxTargetRange, Cache.Instance.WeaponRange);

            // Get a list of combat targets (combine targets + targeting)
            var targets = new List<EntityCache>();
            targets.AddRange(Cache.Instance.Targets);
            targets.AddRange(Cache.Instance.Targeting);
            List<EntityCache> combatTargets = targets.Where(e => e.CategoryId == (int)CategoryID.Entity && e.IsNpc && !e.IsContainer && e.GroupId != (int)Group.LargeCollidableStructure).ToList();

            // Remove any target that is too far out of range (Weapon Range * 1.5)
            for (int i = combatTargets.Count - 1; i >= 0; i--)
            {
                EntityCache target = combatTargets[i];
                if (target.Distance > Cache.Instance.MaxRange * 1.5d)
                {
                    Logging.Log("Combat: Target [" + target.Name + "][ID: " + target.Id + "] out of range [" + Math.Round(target.Distance/1000,0) + "k away]");
                }
                else if (Cache.Instance.IgnoreTargets.Contains(target.Name.Trim()))
                {
                    Logging.Log("Combat: Target [" + target.Name + "][ID: " + target.Id + "] on ignore list [" + Math.Round(target.Distance/1000,0) + "k away]");                    
                }
                else continue;

                target.UnlockTarget();
                Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds((int)Time.TargetDelay_milliseconds);
                combatTargets.RemoveAt(i);
                return; //this does kind of negates the 'for' loop, but we want the pause between commands sent to the server
            }

            // Get a list of current high and low value targets
            List<EntityCache> highValueTargets = combatTargets.Where(t => t.TargetValue.HasValue || Cache.Instance.PriorityTargets.Any(pt => pt.Id == t.Id)).ToList();
            List<EntityCache> lowValueTargets = combatTargets.Where(t => !t.TargetValue.HasValue && !Cache.Instance.PriorityTargets.Any(pt => pt.Id == t.Id)).ToList();

            // Build a list of things targeting me
            List<EntityCache> targetingMe = Cache.Instance.TargetedBy.Where(t => t.IsNpc && t.CategoryId == (int)CategoryID.Entity && !t.IsContainer && t.Distance < Cache.Instance.MaxRange && !targets.Any(c => c.Id == t.Id) && !Cache.Instance.IgnoreTargets.Contains(t.Name.Trim())).ToList();
            List<EntityCache> highValueTargetingMe = targetingMe.Where(t => t.TargetValue.HasValue).OrderByDescending(t => t.TargetValue != null ? t.TargetValue.Value : 0).ThenBy(t => t.Distance).ToList();
            List<EntityCache> lowValueTargetingMe = targetingMe.Where(t => !t.TargetValue.HasValue).OrderBy(t => t.Distance).ToList();

            // Get the number of maximum targets, if there are no low or high value targets left, use the combined total of targets
            int maxHighValueTarget = (lowValueTargetingMe.Count + lowValueTargets.Count) == 0 ? Settings.Instance.MaximumLowValueTargets + Settings.Instance.MaximumHighValueTargets : Settings.Instance.MaximumHighValueTargets;
            int maxLowValueTarget = (highValueTargetingMe.Count + highValueTargets.Count) == 0 ? Settings.Instance.MaximumLowValueTargets + Settings.Instance.MaximumHighValueTargets : Settings.Instance.MaximumLowValueTargets;

            // Do we have too many high (none-priority) value targets targeted?
            while (highValueTargets.Count(t => !Cache.Instance.PriorityTargets.Any(pt => pt.Id == t.Id)) > Math.Max(maxHighValueTarget - Cache.Instance.PriorityTargets.Count(), 0))
            {
                // Unlock any target
                EntityCache target = highValueTargets.OrderByDescending(t => t.Distance).FirstOrDefault(t => !Cache.Instance.PriorityTargets.Any(pt => pt.Id == t.Id));
                if (target == null)
                    break;

                Logging.Log("Combat: unlocking high value target [" + target.Name + "][ID:" + target.Id + "]{" + highValueTargets.Count + "} [" + Math.Round(target.Distance/1000,0) + "k away]");
                target.UnlockTarget();
                highValueTargets.Remove(target);
                Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds((int)Time.TargetDelay_milliseconds);
                return;
            }

            // Do we have too many low value targets targeted?
            while (lowValueTargets.Count > maxLowValueTarget)
            {
                // Unlock any target
                EntityCache target = lowValueTargets.OrderByDescending(t => t.Distance).First();
                Logging.Log("Combat: unlocking low value target [" + target.Name + "][ID:" + target.Id + "]{" + lowValueTargets.Count + "} [" + Math.Round(target.Distance/1000,0) + "k away]");
                target.UnlockTarget();
                lowValueTargets.Remove(target);
                Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds((int)Time.TargetDelay_milliseconds);
                return;
            }

            // Do we have enough targeted?
            if ((highValueTargets.Count >= maxHighValueTarget && lowValueTargets.Count >= maxLowValueTarget) ||
                ((highValueTargets.Count + lowValueTargets.Count) >= (maxHighValueTarget + maxLowValueTarget)))
                return;

            // Do we have any priority targets?
            IEnumerable<EntityCache> priority = Cache.Instance.PriorityTargets.Where(t => t.Distance < Cache.Instance.MaxRange && !targets.Any(c => c.Id == t.Id) && !Cache.Instance.IgnoreTargets.Contains(t.Name.Trim()));
            foreach (EntityCache entity in priority)
            {
                // Have we reached the limit of high value targets?
                if (highValueTargets.Count >= maxHighValueTarget)
                    break;

                if (entity.IsTarget || entity.IsTargeting) //This target is already targeted no need to target it again
                {
                    return;
                }
                else
                {
                    Logging.Log("Combat: Targeting priority target [" + entity.Name + "][ID:" + entity.Id + "]{" + highValueTargets.Count + "} [" + Math.Round(entity.Distance/1000,0) + "k away]");
                    entity.LockTarget();
                    highValueTargets.Add(entity);
                    Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds((int)Time.TargetDelay_milliseconds);
                    return;
                }
            }

            foreach (EntityCache entity in highValueTargetingMe)
            {
                // Have we reached the limit of high value targets?
                if (highValueTargets.Count >= maxHighValueTarget)
                    break;

                if (entity.IsTarget || entity.IsTargeting) //This target is already targeted no need to target it again
                {
                    return;
                }
                else
                {
                    Logging.Log("Combat: Targeting high value target [" + entity.Name + "][ID:" + entity.Id + "]{" + highValueTargets.Count + "} [" + Math.Round(entity.Distance/1000,0) + "k away]");
                    entity.LockTarget();
                    highValueTargets.Add(entity);
                    Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds((int)Time.TargetDelay_milliseconds);
                    return;
                }
            }

            foreach (EntityCache entity in lowValueTargetingMe)
            {
                // Have we reached the limit of low value targets?
                if (lowValueTargets.Count >= maxLowValueTarget)
                    break;

                if (entity.IsTarget || entity.IsTargeting) //This target is already targeted no need to target it again
                {
                    return;
                }
                else
                {
                    Logging.Log("Combat: Targeting low value target [" + entity.Name + "][ID:" + entity.Id + "]{" + lowValueTargets.Count + "} [" + Math.Round(entity.Distance/1000,0) + "k away]");
                    entity.LockTarget();
                    lowValueTargets.Add(entity);
                    Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds((int)Time.TargetDelay_milliseconds);
                    return;
                }
            }
        }

        public void ProcessState()
        {
            // There is really no combat in stations (yet)
            if (Cache.Instance.InStation)
                return;

            // What? No ship entity?
            if (Cache.Instance.DirectEve.ActiveShip.Entity == null)
                return;

            // There is no combat when cloaked
            if (Cache.Instance.DirectEve.ActiveShip.Entity.IsCloaked)
                return;

            if (!Cache.Instance.Weapons.Any())
            {
                Logging.Log("Combat: No weapons with GroupId [" + Settings.Instance.WeaponGroupId + "] found!");
                State = CombatState.OutOfAmmo;
            }

            switch (State)
            {
                case CombatState.CheckTargets:
                    // Next state
                    State = CombatState.KillTargets;

                    TargetCombatants();
                    break;

                case CombatState.KillTargets:
                    // Next state
                    State = CombatState.CheckTargets;

                    EntityCache target = GetTarget();
                    if (target != null)
                    {
                        ActivateTargetPainters(target);
                        ActivateStasisWeb(target);
                        ActivateNos(target);
                        ActivateWeapons(target);
                    }
                    break;

                case CombatState.OutOfAmmo:
                    break;

                default:
                    // Next state
                    State = CombatState.CheckTargets;
                    break;
            }
        }
    }
}