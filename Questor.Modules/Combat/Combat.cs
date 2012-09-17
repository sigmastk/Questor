// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Activities;

namespace Questor.Modules.Combat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DirectEve;
    using Questor.Modules.Logging;
    using Questor.Modules.Lookup;
    using Questor.Modules.States;

    /// <summary>
    ///   The combat class will target and kill any NPC that is targeting the questor.
    ///   It will also kill any NPC that is targeted but not aggressing the questor.
    /// </summary>
    public class Combat
    {
        private readonly Dictionary<long, DateTime> _lastModuleActivation = new Dictionary<long, DateTime>();
        private static readonly Dictionary<long, DateTime> _lastWeaponReload = new Dictionary<long, DateTime>();
        private bool _isJammed;
        private static int weaponNumber;

        private int MaxCharges { get; set; }

        private DateTime _lastCombatProcessState;
        private static DateTime _lastReloadAll;
        private static int _reloadAllIteration;

        /// <summary> Reload correct (tm) ammo for the NPC
        /// </summary>
        /// <param name = "weapon"></param>
        /// <param name = "entity"></param>
        /// <param name = "weaponNumber"></param>
        /// <returns>True if the (enough/correct) ammo is loaded, false if wrong/not enough ammo is loaded</returns>
        public static bool ReloadNormalAmmo(ModuleCache weapon, EntityCache entity, int weaponNumber)
        {
            DirectContainer cargo = Cache.Instance.DirectEve.GetShipsCargo();

            // Get ammo based on damage type
            IEnumerable<Ammo> correctAmmo = Settings.Instance.Ammo.Where(a => a.DamageType == Cache.Instance.DamageType).ToList();

            // Check if we still have that ammo in our cargo
            IEnumerable<Ammo> correctAmmoIncargo = correctAmmo.Where(a => cargo.Items.Any(i => i.TypeId == a.TypeId && i.Quantity >= Settings.Instance.MinimumAmmoCharges)).ToList();

            //check if mission specific ammo is defined
            if (Cache.Instance.MissionAmmo.Count() != 0)
            {
                correctAmmoIncargo = Cache.Instance.MissionAmmo.Where(a => a.DamageType == Cache.Instance.DamageType).ToList();
            }

            // Check if we still have that ammo in our cargo
            correctAmmoIncargo = correctAmmoIncargo.Where(a => cargo.Items.Any(i => i.TypeId == a.TypeId && i.Quantity >= Settings.Instance.MinimumAmmoCharges)).ToList();
            if (Cache.Instance.MissionAmmo.Count() != 0)
            {
                correctAmmoIncargo = Cache.Instance.MissionAmmo;
            }

            // We are out of ammo! :(
            if (!correctAmmoIncargo.Any())
            {
                Logging.Log("Combat", "ReloadNormalAmmo: not enough [" + Cache.Instance.DamageType + "] ammo in cargohold: MinimumCharges: [" + Settings.Instance.MinimumAmmoCharges + "]", Logging.orange);
                _States.CurrentCombatState = CombatState.OutOfAmmo;
                return false;
            }

            /******
            if (weapon.Charge != null)
            {
                IEnumerable<Ammo> areWeMissingAmmo = correctAmmo.Where(a => a.TypeId == weapon.Charge.TypeId);
                if (!areWeMissingAmmo.Any())
                {
                    if (DateTime.Now.Subtract(Cache.Instance.LastLoggingAction).TotalSeconds > 4)
                    {
                        Logging.Log("Combat", "ReloadNormalAmmo: We have ammo loaded that does not have a full reload available, checking cargo for other ammo", Logging.orange); 
                        Cache.Instance.LastLoggingAction = DateTime.Now;
                        try
                        {
                            if (Settings.Instance.Ammo.Any())
                            {
                                DirectItem availableAmmo = cargo.Items.OrderByDescending(i => i.Quantity).Where(a => Settings.Instance.Ammo.Any(i => i.TypeId == a.TypeId)).ToList().FirstOrDefault();
                                if (availableAmmo != null)
                                {
                                    Cache.Instance.DamageType = Settings.Instance.Ammo.ToList().OrderByDescending(i => i.Quantity).Where(a => a.TypeId == availableAmmo.TypeId).ToList().FirstOrDefault().DamageType;
                                    Logging.Log("Combat", "ReloadNormalAmmo: found [" + availableAmmo.Quantity + "] units of  [" + availableAmmo.TypeName + "] changed DamageType to [" + Cache.Instance.DamageType.ToString() + "]", Logging.orange);
                                    return false;
                                }

                                Logging.Log("Combat", "ReloadNormalAmmo: unable to find any alternate ammo in your cargo", Logging.teal);
                                _States.CurrentCombatState = CombatState.OutOfAmmo;
                                return false;
                            }
                        }
                        catch (Exception)
                        {
                            Logging.Log("Combat", "ReloadNormalAmmo: unable to find any alternate ammo in your cargo", Logging.teal);
                            _States.CurrentCombatState = CombatState.OutOfAmmo;
                        }
                        return false;
                    }
                }
            }
            *****/

            // Get the best possible ammo
            Ammo ammo = correctAmmoIncargo.Where(a => a.Range > entity.Distance).OrderBy(a => a.Range).FirstOrDefault();

            // We do not have any ammo left that can hit targets at that range!
            if (ammo == null)
                return false;

            // We have enough ammo loaded
            if (weapon.Charge != null && weapon.Charge.TypeId == ammo.TypeId && weapon.CurrentCharges >= Settings.Instance.MinimumAmmoCharges)
            {
                _lastWeaponReload[weapon.ItemId] = DateTime.Now; //mark this weapon as reloaded... by the time we need to reload this timer will have aged enough... 
                return true;
            }

            // Retry later, assume its ok now
            if (!weapon.MatchingAmmo.Any())
            {
                _lastWeaponReload[weapon.ItemId] = DateTime.Now; //mark this weapon as reloaded... by the time we need to reload this timer will have aged enough... 
                return true;
            }

            DirectItem charge = cargo.Items.FirstOrDefault(i => i.TypeId == ammo.TypeId && i.Quantity >= Settings.Instance.MinimumAmmoCharges);
            // This should have shown up as "out of ammo"
            if (charge == null)
                return false;

            // We are reloading, wait Time.ReloadWeaponDelayBeforeUsable_seconds (see time.cs)
            if (_lastWeaponReload.ContainsKey(weapon.ItemId) && DateTime.Now < _lastWeaponReload[weapon.ItemId].AddSeconds(Time.Instance.ReloadWeaponDelayBeforeUsable_seconds))
                return true;
            _lastWeaponReload[weapon.ItemId] = DateTime.Now;

            // Reload or change ammo
            if (weapon.Charge != null && weapon.Charge.TypeId == charge.TypeId)
            {
                if (DateTime.Now.Subtract(Cache.Instance.LastLoggingAction).TotalSeconds > 10)
                {
                    Cache.Instance.TimeSpentReloading_seconds = Cache.Instance.TimeSpentReloading_seconds + Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                    Cache.Instance.LastLoggingAction = DateTime.Now;
                }
                Logging.Log("Combat", "Reloading [" + weaponNumber + "] with [" + charge.TypeName + "][" + Math.Round((double)ammo.Range / 1000, 0) + "][TypeID: " + charge.TypeId + "]", Logging.teal);
                weapon.ReloadAmmo(charge);
                weapon.ReloadTimeThisMission = weapon.ReloadTimeThisMission + Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
            }
            else
            {
                if (DateTime.Now.Subtract(Cache.Instance.LastLoggingAction).TotalSeconds > 10)
                {
                    Cache.Instance.TimeSpentReloading_seconds = Cache.Instance.TimeSpentReloading_seconds + Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                    Cache.Instance.LastLoggingAction = DateTime.Now;
                }
                Logging.Log("Combat", "Changing [" + weaponNumber + "] with [" + charge.TypeName + "][" + Math.Round((double)ammo.Range / 1000, 0) + "][TypeID: " + charge.TypeId + "] so we can hit [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 0) + "k]", Logging.teal);
                weapon.ChangeAmmo(charge);
                weapon.ReloadTimeThisMission = weapon.ReloadTimeThisMission + Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
            }

            // Return true as we are reloading ammo, assume it is the correct ammo...
            return true;
        }

        public static bool ReloadEnergyWeaponAmmo(ModuleCache weapon, EntityCache entity, int weaponNumber)
        {
            DirectContainer cargo = Cache.Instance.DirectEve.GetShipsCargo();

            // Get ammo based on damage type
            IEnumerable<Ammo> correctAmmo = Settings.Instance.Ammo.Where(a => a.DamageType == Cache.Instance.DamageType).ToList();

            // Check if we still have that ammo in our cargo
            IEnumerable<Ammo> correctAmmoInCargo = correctAmmo.Where(a => cargo.Items.Any(i => i.TypeId == a.TypeId)).ToList();

            //check if mission specific ammo is defined
            if (Cache.Instance.MissionAmmo.Count() != 0)
            {
                correctAmmoInCargo = Cache.Instance.MissionAmmo.Where(a => a.DamageType == Cache.Instance.DamageType).ToList();
            }

            // Check if we still have that ammo in our cargo
            correctAmmoInCargo = correctAmmoInCargo.Where(a => cargo.Items.Any(i => i.TypeId == a.TypeId && i.Quantity >= Settings.Instance.MinimumAmmoCharges)).ToList();
            if (Cache.Instance.MissionAmmo.Count() != 0)
            {
                correctAmmoInCargo = Cache.Instance.MissionAmmo;
            }

            // We are out of ammo! :(
            if (!correctAmmoInCargo.Any())
            {
                Logging.Log("Combat", "ReloadEnergyWeapon: not enough [" + Cache.Instance.DamageType + "] ammo in cargohold: MinimumCharges: [" + Settings.Instance.MinimumAmmoCharges + "]", Logging.orange);
                _States.CurrentCombatState = CombatState.OutOfAmmo;
                return false;
            }

            if (weapon.Charge != null)
            {
                IEnumerable<Ammo> areWeMissingAmmo = correctAmmoInCargo.Where(a => a.TypeId == weapon.Charge.TypeId);
                if (!areWeMissingAmmo.Any())
                {
                    Logging.Log("Combat", "ReloadEnergyWeaponAmmo: We have ammo loaded that does not have a full reload available in the cargo.", Logging.orange);
                }
            }

            // Get the best possible ammo - energy weapons change ammo near instantly
            Ammo ammo = correctAmmoInCargo.Where(a => a.Range > (entity.Distance)).OrderBy(a => a.Range).FirstOrDefault(); //default

            // We do not have any ammo left that can hit targets at that range!
            if (ammo == null)
            {
                if (Settings.Instance.DebugReloadorChangeAmmo) Logging.Log("Combat", "ReloadEnergyWeaponAmmo: best possible ammo: [ ammo == null]", Logging.white);
                return false;
            }
            else
            {
                if (Settings.Instance.DebugReloadorChangeAmmo) Logging.Log("Combat", "ReloadEnergyWeaponAmmo: best possible ammo: [" + ammo.TypeId + "][" + ammo.DamageType + "]", Logging.white);
                if (Settings.Instance.DebugReloadorChangeAmmo) Logging.Log("Combat", "ReloadEnergyWeaponAmmo: best possible ammo: [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 0) + "]", Logging.white);
            }

            DirectItem charge = cargo.Items.OrderBy(i => i.Quantity).FirstOrDefault(i => i.TypeId == ammo.TypeId);
            // We do not have any ammo left that can hit targets at that range!
            if (charge == null)
            {
                if (Settings.Instance.DebugReloadorChangeAmmo)
                    Logging.Log("Combat",
                                "ReloadEnergyWeaponAmmo: We do not have any ammo left that can hit targets at that range!",
                                Logging.orange);
                return false;
            }
            else
            {
                if (Settings.Instance.DebugReloadorChangeAmmo) Logging.Log("Combat", "ReloadEnergyWeaponAmmo: charge: [" + charge.TypeName + "][" + charge.TypeId + "]", Logging.white);
            }

            // We have enough ammo loaded
            if (weapon.Charge != null && weapon.Charge.TypeId == ammo.TypeId)
            {
                if (Settings.Instance.DebugReloadorChangeAmmo) Logging.Log("Combat", "ReloadEnergyWeaponAmmo: We have Enough Ammo of that type Loaded Already", Logging.white);
                return true;
            }

            // We are reloading, wait at least 5 seconds
            if (_lastWeaponReload.ContainsKey(weapon.ItemId) && DateTime.Now < _lastWeaponReload[weapon.ItemId].AddSeconds(5))
            {
                if (Settings.Instance.DebugReloadorChangeAmmo) Logging.Log("Combat", "ReloadEnergyWeaponAmmo: We are currently reloading: waiting", Logging.white);
                return false;
            }
            _lastWeaponReload[weapon.ItemId] = DateTime.Now;

            // Reload or change ammo
            if (weapon.Charge != null && weapon.Charge.TypeId == charge.TypeId)
            {
                Logging.Log("Combat", "Reloading [" + weaponNumber + "] with [" + charge.TypeName + "][" + Math.Round((double)ammo.Range / 1000, 0) + "][TypeID: " + charge.TypeId + "]", Logging.teal);
                weapon.ReloadAmmo(charge);
                weapon.ReloadTimeThisMission = weapon.ReloadTimeThisMission + 1;
            }
            else
            {
                Logging.Log("Combat", "Changing [" + weaponNumber + "] with [" + charge.TypeName + "][" + Math.Round((double)ammo.Range / 1000, 0) + "][TypeID: " + charge.TypeId + "] so we can hit [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 0) + "k]", Logging.teal);
                weapon.ChangeAmmo(charge);
                weapon.ReloadTimeThisMission = weapon.ReloadTimeThisMission + 1;
            }

            // Return false as we are reloading ammo
            return false;
        }

        /// <summary> Reload correct (tm) ammo for the NPC
        /// </summary>
        /// <param name = "weapon"></param>
        /// <param name = "entity"></param>
        /// <param name = "weaponNumber"></param>
        /// <returns>True if the (enough/correct) ammo is loaded, false if wrong/not enough ammo is loaded</returns>
        public static bool ReloadAmmo(ModuleCache weapon, EntityCache entity, int weaponNumber)
        {
            // We need the cargo bay open for both reload actions
            if (!Cache.Instance.OpenCargoHold("Questor: ReloadAmmo")) return false;

            return weapon.IsEnergyWeapon ? ReloadEnergyWeaponAmmo(weapon, entity, weaponNumber) : ReloadNormalAmmo(weapon, entity, weaponNumber);
        }

        public static bool ReloadAll(EntityCache entity)
        {

            _reloadAllIteration++;
            if (Settings.Instance.DebugReloadAll) Logging.Log("debug ReloadAll", "Entering reloadAll function (again) - it iterates through all weapon stacks [" + _reloadAllIteration + "]", Logging.white);
            if (_reloadAllIteration > 12)
            {
                if (Settings.Instance.DebugReloadAll) Logging.Log("debug ReloadAll:", "reset iteration counter", Logging.orange);
                return true;
            }

            IEnumerable<ModuleCache> weapons = Cache.Instance.Weapons;
            weaponNumber = 0;
            foreach (ModuleCache weapon in weapons)
            {
                // Reloading energy weapons prematurely just results in unnecessary error messages, so let's not do that
                if (weapon.IsEnergyWeapon)
                    continue;
                weaponNumber++;

                if (weapon.IsReloadingAmmo || weapon.IsDeactivating || weapon.IsChangingAmmo || weapon.IsActive)
                {
                    if (Settings.Instance.DebugReloadAll) Logging.Log("debug ReloadAll", "Weapon [" + weaponNumber + "] is busy, moving on to next weapon", Logging.white);
                    continue;
                }

                if (_lastWeaponReload.ContainsKey(weapon.ItemId) && DateTime.Now < _lastWeaponReload[weapon.ItemId].AddSeconds(Time.Instance.ReloadWeaponDelayBeforeUsable_seconds))
                {
                    if (Settings.Instance.DebugReloadAll) Logging.Log("debug ReloadAll", "Weapon [" + weaponNumber + "] has been reloaded recently, moving on to next weapon", Logging.white);
                    continue;
                }
                if(!ReloadAmmo(weapon, entity, weaponNumber)) continue; //by returning false here we make sure we only reload one gun (or stack) per iteraction (basically per second)
                return false;
            }
            if (Settings.Instance.DebugReloadAll) Logging.Log("debug ReloadAll", "completely reloaded all weapons", Logging.white);
            _lastReloadAll = DateTime.Now;
            _reloadAllIteration = 0;
            return true;
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

        public List<EntityCache> TargetingMe { get; set; }

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
            return Cache.Instance.GetBestTarget(weaponTarget, Cache.Instance.WeaponRange, false, "Combat");
        }

        /// <summary> Activate weapons
        /// </summary>
        private void ActivateWeapons(EntityCache target)
        {
            // When in warp there's nothing we can do, so ignore everything
            if (Cache.Instance.InWarp)
            {
                if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: we are in warp! doing nothing", Logging.teal);
                return;
            }
            if (DateTime.Now < Cache.Instance.NextWeaponAction) //if we just did something wait a fraction of a second
            {
                if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: waiting on NextWeaponAction", Logging.teal);
                return;
            }

            //
            // Do we really want a non-mission action moving the ship around at all!! (other than speed tanking)?
            // If you are not in a mission by all means let combat actions move you around as needed
            if (!Cache.Instance.InMission || target.IsNPCFrigate || Settings.Instance.SpeedTank)
            {
                if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: we are NOT in a mission: navigateintorange", Logging.teal);
                if (Settings.Instance.DebugNavigateOnGrid) Logging.Log("Combat", "ActivateWeapons: NavigateIntoRange: target is NPC Frigate", Logging.white);
                NavigateOnGrid.NavigateIntoRange(target, "Combat");
            }

            if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: after navigate into range...", Logging.teal);
            // Get the weapons
            IEnumerable<ModuleCache> weapons = Cache.Instance.Weapons.ToList();

            // TODO: Add check to see if there is better ammo to use! :)
            // Get distance of the target and compare that with the ammo currently loaded

            //Deactivate weapns that needs to be deactivated for this list of reasons...
            weaponNumber = 0;
            foreach (ModuleCache weapon in weapons)
            {
                weaponNumber++;
                if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: for each weapon [" + weaponNumber + "] in weapons", Logging.teal);
                // don't waste ammo on small target if you use autocannon or siege i hope you use drone
                if (Settings.Instance.DontShootFrigatesWithSiegeorAutoCannons) //this defaults to false and needs to be changed in your characters settings xml file if you want to enable this option
                {
                    if (Settings.Instance.WeaponGroupId == 55 || Settings.Instance.WeaponGroupId == 508 || Settings.Instance.WeaponGroupId == 506)
                    {
                        if (target.Distance <= (int)Distance.InsideThisRangeIsLIkelyToBeMostlyFrigates && !target.TargetValue.HasValue && target.GroupId != (int)Group.LargeCollidableStructure)
                        {
                            if (weapon.IsActive)
                            {
                                //if we are already shooting at it, stop
                                weapon.Click();
                                continue;
                            }
                            else
                            {
                                //if weapon are not yet shooting at it, do not attempt to shoot
                                continue;
                            }
                        }
                    }
                }
                if (!weapon.IsActive)
                {
                    if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: weapon [" + weaponNumber + "] is not active: no need to do anything", Logging.teal);
                    continue;
                }
                if (weapon.IsReloadingAmmo || weapon.IsDeactivating || weapon.IsChangingAmmo)
                {
                    if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: weapon [" + weaponNumber + "] is reloading, deactivating or changing ammo: no need to do anything", Logging.teal);
                    continue;
                }

                if (DateTime.Now < Cache.Instance.NextReload) //if we should not yet reload we are likely in the middle of a reload and should wait!
                {
                    if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: NextReload is still in the future: wait before doing anything with the weapon", Logging.teal);
                    return;
                }

                // No ammo loaded
                if (weapon.Charge == null)
                {
                    if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: no ammo loaded? [" + weaponNumber + "] reload will happen elsewhere", Logging.teal);
                    continue;
                }

                Ammo ammo = Settings.Instance.Ammo.FirstOrDefault(a => a.TypeId == weapon.Charge.TypeId);

                //use mission specific ammo
                if (Cache.Instance.MissionAmmo.Count() != 0)
                {
                    if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: MissionAmmocount is not 0", Logging.teal);
                    ammo = Cache.Instance.MissionAmmo.FirstOrDefault(a => a.TypeId == weapon.Charge.TypeId);
                }

                // How can this happen? Someone manually loaded ammo
                if (ammo == null)
                {
                    if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: ammo == null [" + weaponNumber + "] someone manually loaded ammo?", Logging.teal);
                    continue;
                }

                // If we have already activated warp, deactivate the weapons
                if (!Cache.Instance.DirectEve.ActiveShip.Entity.IsWarping)
                {
                    // Target is in range
                    if (target.Distance <= ammo.Range)
                    {
                        if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: target is in range: do nothing, wait until it is dead", Logging.teal);
                        continue;
                    }
                }

                // Target is out of range, stop firing
                if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: target is out of range, stop firing", Logging.teal);
                weapon.Click();
                return;
            }

            // Hack for max charges returning incorrect value
            if (!weapons.Any(w => w.IsEnergyWeapon))
            {
                MaxCharges = Math.Max(MaxCharges, weapons.Max(l => l.MaxCharges));
                MaxCharges = Math.Max(MaxCharges, weapons.Max(l => l.CurrentCharges));
            }

            int _WeaponsActivatedThisTick = 0;
            int _WeaponsToActivateThisTick = Cache.Instance.RandomNumber(1, 2);

            // Activate the weapons (it not yet activated)))
            weaponNumber = 0;
            foreach (ModuleCache weapon in weapons)
            {
                weaponNumber++;
                // Are we reloading, deactivating or changing ammo?
                if (weapon.IsReloadingAmmo || weapon.IsDeactivating || weapon.IsChangingAmmo)
                {
                    if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: Activate: weapon [" + weaponNumber + "] is reloading, deactivating or changing ammo", Logging.teal);
                    continue;
                }

                // Are we on the right target?
                if (weapon.IsActive)
                {
                    if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: Activate: weapon [" + weaponNumber + "] is active already", Logging.teal);
                    if (weapon.TargetId != target.Id)
                    {
                        if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: Activate: weapon [" + weaponNumber + "] is shooting at the wrong target: deactivating", Logging.teal);
                        weapon.Click();
                        return;
                    }
                    continue;
                }

                // No, check ammo type and if that is correct, activate weapon
                if (ReloadAmmo(weapon, target, weaponNumber) && CanActivate(weapon, target, true))
                {
                    if (_WeaponsActivatedThisTick > _WeaponsToActivateThisTick)
                        //if we have already activated x num of weapons return, which will wait until the next ProcessState
                        return;

                    if (Settings.Instance.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: Activate: weapon [" + weaponNumber + "] has the correct ammo: activate", Logging.teal);
                    _WeaponsActivatedThisTick++; //increment the num of weapons we've activated this ProcessState so that we might optionally activate more than one module per tick
                    Logging.Log("Combat", "Activating weapon  [" + weaponNumber + "] on [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance / 1000, 0) + "k away]", Logging.teal);
                    weapon.Activate(target.Id);
                    Cache.Instance.NextWeaponAction = DateTime.Now.AddMilliseconds(Time.Instance.WeaponDelay_milliseconds);
                    //we know we are connected if we were able to get this far - update the lastknownGoodConnectedTime
                    Cache.Instance.LastKnownGoodConnectedTime = DateTime.Now;
                    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    continue;
                }
            }
        }

        /// <summary> Activate target painters
        /// </summary>
        public void ActivateTargetPainters(EntityCache target)
        {
            if (DateTime.Now < Cache.Instance.NextPainterAction) //if we just did something wait a fraction of a second
                return;

            List<ModuleCache> targetPainters = Cache.Instance.Modules.Where(m => m.GroupId == (int)Group.TargetPainter).ToList();

            // Find the first active weapon
            // Assist this weapon
            weaponNumber = 0;
            foreach (ModuleCache painter in targetPainters)
            {
                weaponNumber++;
                // Are we on the right target?
                if (painter.IsActive)
                {
                    if (painter.TargetId != target.Id)
                    {
                        painter.Click();
                        return;
                    }


                    continue;
                }

                // Are we deactivating?
                if (painter.IsDeactivating)
                    continue;

                if (CanActivate(painter, target, false))
                {
                    Logging.Log("Combat", "Activating painter [" + weaponNumber + "] on [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance / 1000, 0) + "k away]", Logging.teal);
                    painter.Activate(target.Id);
                    Cache.Instance.NextPainterAction = DateTime.Now.AddMilliseconds(Time.Instance.PainterDelay_milliseconds);
                    return;
                }
            }
        }

        /// <summary> Activate Nos
        /// </summary>
        public void ActivateNos(EntityCache target)
        {
            if (DateTime.Now < Cache.Instance.NextNosAction) //if we just did something wait a fraction of a second
                return;

            List<ModuleCache> noses = Cache.Instance.Modules.Where(m => m.GroupId == (int)Group.NOS).ToList();
            //Logging.Log("Combat: we have " + noses.Count.ToString() + " Nos modules");
            // Find the first active weapon
            // Assist this weapon
            weaponNumber = 0;
            foreach (ModuleCache nos in noses)
            {
                weaponNumber++;
                // Are we on the right target?
                if (nos.IsActive)
                {
                    if (nos.TargetId != target.Id)
                    { 
                        nos.Click();
                        return;
                    }

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
                    Logging.Log("Combat", "Activating Nos     [" + weaponNumber + "] on [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance / 1000, 0) + "k away]", Logging.teal);
                    nos.Activate(target.Id);
                    Cache.Instance.NextNosAction = DateTime.Now.AddMilliseconds(Time.Instance.NosDelay_milliseconds);
                    return;
                }
                else
                {
                    Logging.Log("Combat", "Cannot Activate Nos [" + weaponNumber + "] on [" + target.Name + "][ID: " + target.Id + "][" + Math.Round(target.Distance / 1000, 0) + "k away]", Logging.teal);
                }
            }
        }

        /// <summary> Activate StasisWeb
        /// </summary>
        public void ActivateStasisWeb(EntityCache target)
        {
            if (DateTime.Now < Cache.Instance.NextWebAction) //if we just did something wait a fraction of a second
                return;

            List<ModuleCache> webs = Cache.Instance.Modules.Where(m => m.GroupId == (int)Group.StasisWeb).ToList();

            // Find the first active weapon
            // Assist this weapon
            weaponNumber = 0;
            foreach (ModuleCache web in webs)
            {
                weaponNumber++;
                // Are we on the right target?
                if (web.IsActive)
                {
                    if (web.TargetId != target.Id)
                    {
                        web.Click();
                        return;
                    }

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
                    Logging.Log("Combat", "Activating web     [" + weaponNumber + "] on [" + target.Name + "][ID: " + target.Id + "]", Logging.teal);
                    web.Activate(target.Id);
                    Cache.Instance.NextWebAction = DateTime.Now.AddMilliseconds(Time.Instance.WebDelay_milliseconds);
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
            // When in warp we should not try to target anything
            if (Cache.Instance.InWarp)
                return;

            if (DateTime.Now < Cache.Instance.NextTargetAction) //if we just did something wait a fraction of a second
                return;

            // We are jammed, forget targeting anything...
            if (Cache.Instance.DirectEve.ActiveShip.MaxLockedTargets == 0)
            {
                if (!_isJammed)
                {
                    Logging.Log("Combat", "We are jammed and can't target anything", Logging.orange);
                }

                _isJammed = true;
                return;
            }

            if (_isJammed)
            {
                // Clear targeting list as it doesn't apply
                Cache.Instance.TargetingIDs.Clear();
                Logging.Log("Combat", "We are no longer jammed, retargeting", Logging.teal);
            }
            _isJammed = false;

            //
            // ???bounty tracking code goes here???
            //
            if (!Cache.Instance.OpenCargoHold("Combat.TargetCombatants")) return;
            // What is the range that we can target at

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
                    Logging.Log("Combat", "Target [" + target.Name + "][ID: " + target.Id + "] out of range [" + Math.Round(target.Distance / 1000, 0) + "k away]", Logging.teal);
                }
                else if (Cache.Instance.IgnoreTargets.Contains(target.Name.Trim()))
                {
                    Logging.Log("Combat", "Target [" + target.Name + "][ID: " + target.Id + "] on ignore list [" + Math.Round(target.Distance / 1000, 0) + "k away]", Logging.teal);
                }
                else continue;

                target.UnlockTarget();
                Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                combatTargets.RemoveAt(i);
                return; //this does kind of negates the 'for' loop, but we want the pause between commands sent to the server
            }
            //
            // these should be moved into cache - or at least made public so that they can be accessed elsewhere, for debugging and logging purposes
            //

            // Get a list of current high and low value targets
            List<EntityCache> highValueTargets = combatTargets.Where(t => t.TargetValue.HasValue || Cache.Instance.PriorityTargets.Any(pt => pt.Id == t.Id)).ToList();
            List<EntityCache> lowValueTargets = combatTargets.Where(t => !t.TargetValue.HasValue && !Cache.Instance.PriorityTargets.Any(pt => pt.Id == t.Id)).ToList();

            // Build a list of things targeting me
            TargetingMe = Cache.Instance.TargetedBy.Where(t => t.IsNpc && t.CategoryId == (int)CategoryID.Entity && !t.IsContainer && t.Distance < Cache.Instance.MaxRange && !targets.Any(c => c.Id == t.Id) && !Cache.Instance.IgnoreTargets.Contains(t.Name.Trim())).ToList();
            List<EntityCache> highValueTargetingMe = TargetingMe.Where(t => t.TargetValue.HasValue).OrderByDescending(t => t.TargetValue != null ? t.TargetValue.Value : 0).ThenBy(t => t.Distance).ToList();
            List<EntityCache> lowValueTargetingMe = TargetingMe.Where(t => !t.TargetValue.HasValue).OrderBy(t => t.Distance).ToList();

            if (_States.CurrentQuestorState != QuestorState.CombatMissionsBehavior)
            {
                if (!TargetingMe.Any())
                {
                    //
                    // if nothing is targeting me and I am not currently configured for missions assume pew is the objective... and shoot NPCs (NOT players!) even though they are not targeting us.
                    //
                    TargetingMe = Cache.Instance.Entities.Where(t => t.IsNpc && !t.IsBadIdea && t.GroupId != (int)Group.LargeCollidableStructure && t.CategoryId == (int)CategoryID.Entity && !t.IsContainer && t.Distance < Cache.Instance.MaxRange && !targets.Any(c => c.Id == t.Id) && !Cache.Instance.IgnoreTargets.Contains(t.Name.Trim())).ToList();
                    highValueTargetingMe = TargetingMe.Where(t => t.TargetValue.HasValue).OrderByDescending(t => t.TargetValue != null ? t.TargetValue.Value : 0).ThenBy(t => t.Distance).ToList();
                    lowValueTargetingMe = TargetingMe.Where(t => !t.TargetValue.HasValue).OrderBy(t => t.Distance).ToList();
                }
            }

            // Get the number of maximum targets, if there are no low or high value targets left, use the combined total of targets
            int maxHighValueTarget = (lowValueTargetingMe.Count + lowValueTargets.Count) == 0 ? Settings.Instance.MaximumLowValueTargets + Settings.Instance.MaximumHighValueTargets : Settings.Instance.MaximumHighValueTargets;
            int maxLowValueTarget = (highValueTargetingMe.Count + highValueTargets.Count) == 0 ? Settings.Instance.MaximumLowValueTargets + Settings.Instance.MaximumHighValueTargets : Settings.Instance.MaximumLowValueTargets;

            // Do we have too many high (non-priority) value targets targeted?
            while (highValueTargets.Count(t => !Cache.Instance.PriorityTargets.Any(pt => pt.Id == t.Id)) > Math.Max(maxHighValueTarget - Cache.Instance.PriorityTargets.Count(), 0))
            {
                // Unlock any target
                EntityCache target = highValueTargets.OrderByDescending(t => t.Distance).FirstOrDefault(t => !Cache.Instance.PriorityTargets.Any(pt => pt.Id == t.Id));
                if (target == null)
                    break;

                Logging.Log("Combat", "unlocking high value target [" + target.Name + "][ID:" + target.Id + "]{" + highValueTargets.Count + "} [" + Math.Round(target.Distance / 1000, 0) + "k away]", Logging.teal);
                target.UnlockTarget();
                highValueTargets.Remove(target);
                Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                return;
            }

            // Do we have too many low value targets targeted?
            // can we calculate how many priority targets we already hav targeted? This would eliminate false positives and some unecessary unlocking/relocking of frigates
            while (lowValueTargets.Count > Math.Max(maxLowValueTarget - Cache.Instance.PriorityTargets.Count(), 0))
            {
                // Unlock any target
                EntityCache target = lowValueTargets.OrderByDescending(t => t.Distance).First();
                Logging.Log("Combat", "unlocking low  value target [" + target.Name + "][ID:" + target.Id + "]{" + lowValueTargets.Count + "} [" + Math.Round(target.Distance / 1000, 0) + "k away]", Logging.teal);
                target.UnlockTarget();
                lowValueTargets.Remove(target);
                Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
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
                    Logging.Log("Combat", "Targeting priority target [" + entity.Name + "][ID:" + entity.Id + "][" + Math.Round(entity.Distance / 1000, 0) + "k away] highValueTargets.Count [" + highValueTargets.Count + "]", Logging.teal);
                    entity.LockTarget();
                    highValueTargets.Add(entity);
                    //Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                    continue;
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
                    Logging.Log("Combat", "Targeting high value target [" + entity.Name + "][ID:" + entity.Id + "][" + Math.Round(entity.Distance / 1000, 0) + "k away] highValueTargets.Count [" + highValueTargets.Count + "]", Logging.teal);
                    entity.LockTarget();
                    highValueTargets.Add(entity);
                    Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
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
                    Logging.Log("Combat", "Targeting low  value target [" + entity.Name + "][ID:" + entity.Id + "][" + Math.Round(entity.Distance / 1000, 0) + "k away] lowValueTargets.Count [" + lowValueTargets.Count + "]", Logging.teal);
                    entity.LockTarget();
                    lowValueTargets.Add(entity);
                    Cache.Instance.NextTargetAction = DateTime.Now.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                    return;
                }
            }
        }

        public void ProcessState()
        {
            if (DateTime.Now < _lastCombatProcessState.AddMilliseconds(500)) //if it has not been 500ms since the last time we ran this ProcessState return. We can't do anything that close together anyway
                return;

            _lastCombatProcessState = DateTime.Now;

            if ((_States.CurrentCombatState != CombatState.Idle ||
                _States.CurrentCombatState != CombatState.OutOfAmmo) &&
                (Cache.Instance.InStation ||// There is really no combat in stations (yet)
                !Cache.Instance.InSpace || // if we are not in space yet, wait...
                Cache.Instance.DirectEve.ActiveShip.Entity == null || // What? No ship entity?
                Cache.Instance.DirectEve.ActiveShip.Entity.IsCloaked))  // There is no combat when cloaked
            {
                _States.CurrentCombatState = CombatState.Idle;
                return;
            }

            if (Cache.Instance.InStation)
            {
                _States.CurrentCombatState = CombatState.Idle;
                return;
            }

            if (!Cache.Instance.Weapons.Any() && Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower() == Settings.Instance.CombatShipName)
            {
                Logging.Log("Combat", "No weapons with GroupId [" + Settings.Instance.WeaponGroupId + "] found!", Logging.red);
                _States.CurrentCombatState = CombatState.OutOfAmmo;
            }

            switch (_States.CurrentCombatState)
            {
                case CombatState.CheckTargets:
                    _States.CurrentCombatState = CombatState.KillTargets; //this MUST be before TargetCombatants() of the combat state will potentially get reset (important for the outofammo state)
                    TargetCombatants();
                    break;

                case CombatState.KillTargets:
                    //
                    // iterate through priority targets here !!!!!!!!
                    //
                    //Cache.Instance._priorityTargets_text = "";
                    //for (int i = 0; i < Cache.Instance._priorityTargets.Count; i++) // Loop through List with for
                    //{
                    //    Cache.Instance._priorityTargets_text = Cache.Instance._priorityTargets_text + "[ " + i + " ][ " + Cache.Instance._priorityTargets[i].Entity.Name + " ][" + Math.Round(Cache.Instance._priorityTargets[i].Entity.Distance / 1000, 0) + "k away][" + Cache.Instance._priorityTargets[i].Entity.Health + "TH][" + Cache.Instance._priorityTargets[i].Entity.ShieldPct + "S][" + Math.Round(Cache.Instance._priorityTargets[i].Entity.ArmorPct, 0) + "A][" + Math.Round(Cache.Instance._priorityTargets[i].Entity.StructurePct, 0) + "H][" + NPCValue.Value.ToString() + "isk],";
                    //newlblPriorityTargetstext = newlblPriorityTargetstext + "[ " + i + " ][ "; //+ Cache.Instance._priorityTargets[i].Entity.Name + " ][" + Math.Round(Cache.Instance._priorityTargets[i].Entity.Distance / 1000, 0) + "],";
                    //}
                    if (!Cache.Instance.OpenCargoHold("Combat")) break;
                    _States.CurrentCombatState = CombatState.CheckTargets;
                    TargetingCache.CurrentWeaponsTarget = GetTarget();
                    if (TargetingCache.CurrentWeaponsTarget != null)
                    {
                        ActivateTargetPainters(TargetingCache.CurrentWeaponsTarget);
                        ActivateStasisWeb(TargetingCache.CurrentWeaponsTarget);
                        ActivateNos(TargetingCache.CurrentWeaponsTarget);
                        ActivateWeapons(TargetingCache.CurrentWeaponsTarget);
                    }
                    break;

                case CombatState.OutOfAmmo:
                    break;

                case CombatState.Idle:
                    //
                    // below is the reasons we will start the combat state(s) - if the below is not met do nothing
                    //
                    //Logging.Log("Cache.Instance.InSpace: " + Cache.Instance.InSpace);
                    //Logging.Log("Cache.Instance.DirectEve.ActiveShip.Entity.IsCloaked: " + Cache.Instance.DirectEve.ActiveShip.Entity.IsCloaked);
                    //Logging.Log("Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower(): " + Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower());
                    //Logging.Log("Cache.Instance.InSpace: " + Cache.Instance.InSpace);
                    if (Cache.Instance.InSpace && //we are in space (as opposed to being in station or in limbo between systems when jumping)
                        Cache.Instance.DirectEve.ActiveShip.Entity != null &&  // we are in a ship!
                        !Cache.Instance.DirectEve.ActiveShip.Entity.IsCloaked && //we aren't cloaked anymore
                        Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower() == Settings.Instance.CombatShipName.ToLower() && //we are in our combat ship
                        !Cache.Instance.InWarp) // no longer in warp
                    {
                        _States.CurrentCombatState = CombatState.CheckTargets;
                        return;
                    }
                    break;
                default:
                    // Next state
                    Logging.Log("Combat","CurrentCombatState was not set thus ended up at default",Logging.orange);
                    _States.CurrentCombatState = CombatState.CheckTargets;
                    break;
            }
        }
    }
}