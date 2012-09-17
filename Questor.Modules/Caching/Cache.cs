// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using InnerSpaceAPI;

namespace Questor.Modules.Caching
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using global::Questor.Modules.Actions;
    using global::Questor.Modules.BackgroundTasks;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.States;
    using global::Questor.Modules.Logging;
    using DirectEve;

    public class Cache
    {
        /// <summary>
        ///   Singleton implementation
        /// </summary>
        private static Cache _instance = new Cache();

        /// <summary>
        ///   Active Drones
        /// </summary>
        private List<EntityCache> _activeDrones;

        private DirectAgent _agent;

        /// <summary>
        ///   Agent cache
        /// </summary>
        private long? _agentId;

        /// <summary>
        ///   Current Storyline Mission Agent
        /// </summary>
        public long CurrentStorylineAgentId { get; set; }

        /// <summary>
        ///   Agent blacklist
        /// </summary>
        public List<long> AgentBlacklist;

        /// <summary>
        ///   Approaching cache
        /// </summary>
        //private int? _approachingId;
        private EntityCache _approaching;

        /// <summary>
        ///   BigObjects we are likely to bump into (mainly LCOs)
        /// </summary>
        private List<EntityCache> _bigobjects;

        /// <summary>
        ///   BigObjects we are likely to bump into (mainly LCOs)
        /// </summary>
        private List<EntityCache> _gates;

        /// <summary>
        ///   BigObjects we are likely to bump into (mainly LCOs)
        /// </summary>
        private List<EntityCache> _bigobjectsandgates;

        /// <summary>
        ///   objects we are likely to bump into (Anything that isnt an NPC a wreck or a can)
        /// </summary>
        private List<EntityCache> _objects;

        /// <summary>
        ///   Returns all non-empty wrecks and all containers
        /// </summary>
        private List<EntityCache> _containers;

        /// <summary>
        ///   Entities cache (all entities within 256km)
        /// </summary>
        private List<EntityCache> _entities;

        /// <summary>
        ///   Damaged drones
        /// </summary>
        public IEnumerable<EntityCache> DamagedDrones;

        /// <summary>
        ///   Entities by Id
        /// </summary>
        private readonly Dictionary<long, EntityCache> _entitiesById;

        /// <summary>
        ///   Module cache
        /// </summary>
        private List<ModuleCache> _modules;

        /// <summary>
        ///   Priority targets (e.g. warp scramblers or mission kill targets)
        /// </summary>
        public List<PriorityTarget> _priorityTargets;

        public String _priorityTargets_text;

        /// <summary>
        ///   Star cache
        /// </summary>
        private EntityCache _star;

        /// <summary>
        ///   Station cache
        /// </summary>
        private List<EntityCache> _stations;

        /// <summary>
        ///   Station cache
        /// </summary>
        private EntityCache _closeststation;

        /// <summary>
        ///   Stargate cache
        /// </summary>
        private List<EntityCache> _stargates;

        /// <summary>
        ///   Stargate by name
        /// </summary>
        private EntityCache _closeststargate;

        /// <summary>
        ///   Stargate by name
        /// </summary>
        private EntityCache _stargate;

        /// <summary>
        ///   Targeted by cache
        /// </summary>
        private List<EntityCache> _targetedBy;

        /// <summary>
        ///   Targeting cache
        /// </summary>
        private List<EntityCache> _targeting;

        /// <summary>
        ///   Targets cache
        /// </summary>
        private List<EntityCache> _targets;

        /// <summary>
        ///   Aggressed cache
        /// </summary>
        private List<EntityCache> _aggressed;

        /// <summary>
        ///   IDs in Inventory window tree (on left)
        /// </summary>
        public List<long> _idsinInventoryTree;
        
        /// <summary>
        ///   Returns all unlooted wrecks & containers
        /// </summary>
        private List<EntityCache> _unlootedContainers;

        private List<EntityCache> _unlootedWrecksAndSecureCans;

        private List<DirectWindow> _windows;

        public void DirecteveDispose()
        {
            Logging.Log("QuestorUI", "started calling DirectEve.Dispose()", Logging.white);
            Cache.Instance.DirectEve.Dispose(); //could this hang?
            Logging.Log("QuestorUI", "finished calling DirectEve.Dispose()", Logging.white);
        }

        public Cache()
        {
            //string line = "Cache: new cache instance being instantiated";
            //InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now, line));
            //line = string.Empty;

            string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (path != null)
            {
                ShipTargetValues = new List<ShipTargetValue>();
                XDocument values = XDocument.Load(System.IO.Path.Combine(path, "ShipTargetValues.xml"));
                if (values.Root != null)
                    foreach (XElement value in values.Root.Elements("ship"))
                        ShipTargetValues.Add(new ShipTargetValue(value));

                InvTypesById = new Dictionary<int, InvType>();
                XDocument invTypes = XDocument.Load(System.IO.Path.Combine(path, "InvTypes.xml"));
                if (invTypes.Root != null)
                    foreach (XElement element in invTypes.Root.Elements("invtype"))
                        InvTypesById.Add((int)element.Attribute("id"), new InvType(element));
            }

            _priorityTargets = new List<PriorityTarget>();
            LastModuleTargetIDs = new Dictionary<long, long>();
            TargetingIDs = new Dictionary<long, DateTime>();
            _entitiesById = new Dictionary<long, EntityCache>();

            LootedContainers = new HashSet<long>();
            IgnoreTargets = new HashSet<string>();
            MissionItems = new List<string>();
            ChangeMissionShipFittings = false;
            UseMissionShip = false;
            ArmLoadedCache = false;
            MissionAmmo = new List<Ammo>();
            MissionUseDrones = null;

            PanicAttemptsThisPocket = 0;
            LowestShieldPercentageThisPocket = 100;
            LowestArmorPercentageThisPocket = 100;
            LowestCapacitorPercentageThisPocket = 100;
            PanicAttemptsThisMission = 0;
            LowestShieldPercentageThisMission = 100;
            LowestArmorPercentageThisMission = 100;
            LowestCapacitorPercentageThisMission = 100;
            LastKnownGoodConnectedTime = DateTime.Now;
        }

        /// <summary>
        ///   List of containers that have been looted
        /// </summary>
        public HashSet<long> LootedContainers { get; private set; }

        /// <summary>
        ///   List of targets to ignore
        /// </summary>
        public HashSet<string> IgnoreTargets { get; private set; }

        public static Cache Instance
        {
            get { return _instance; }
        }

        public bool ExitWhenIdle = false;
        public bool StopBot = false;
        public bool DoNotBreakInvul = false;
        public bool UseDrones = true;
        public bool LootAlreadyUnloaded = false;
        public bool MissionLoot = false;
        public bool SalvageAll = false;
        public bool RouteIsAllHighSecBool = false;

        public double Wealth { get; set; }

        public double WealthatStartofPocket { get; set; }

        public int PocketNumber { get; set; }

        public bool OpenWrecks = false;
        public bool NormalApproch = true;
        public bool CourierMission = false;
        public string MissionName = "";
        public int MissionsThisSession = 0;
        public int StopSessionAfterMissionNumber = int.MaxValue;
        public bool ConsoleLogOpened = false;
        public int TimeSpentReloading_seconds = 0;
        public int TimeSpentInMission_seconds = 0;
        public int TimeSpentInMissionInRange = 0;
        public int TimeSpentInMissionOutOfRange = 0;
        public int GreyListedMissionsDeclined = 0;
        public string LastGreylistMissionDeclined = string.Empty;
        public int BlackListedMissionsDeclined = 0;
        public string LastBlacklistMissionDeclined = string.Empty;

        public DirectAgentMission Mission;

        public bool DronesKillHighValueTargets { get; set; }

        public bool InMission { get; set; }

        public DateTime QuestorStarted_DateTime = DateTime.Now;

        public DateTime NextSalvageTrip = DateTime.Now;

        public bool MissionXMLIsAvailable { get; set; }

        public string missionXmlPath { get; set; }

        public XDocument InvTypes;
        public string Path;

        public bool LocalSafe(int maxBad, double stand)
        {
            int number = 0;
            var local = (DirectChatWindow)GetWindowByName("Local");
            foreach (DirectCharacter localMember in local.Members)
            {
                float[] alliance = { DirectEve.Standings.GetPersonalRelationship(localMember.AllianceId), DirectEve.Standings.GetCorporationRelationship(localMember.AllianceId), DirectEve.Standings.GetAllianceRelationship(localMember.AllianceId) };
                float[] corporation = { DirectEve.Standings.GetPersonalRelationship(localMember.CorporationId), DirectEve.Standings.GetCorporationRelationship(localMember.CorporationId), DirectEve.Standings.GetAllianceRelationship(localMember.CorporationId) };
                float[] personal = { DirectEve.Standings.GetPersonalRelationship(localMember.CharacterId), DirectEve.Standings.GetCorporationRelationship(localMember.CharacterId), DirectEve.Standings.GetAllianceRelationship(localMember.CharacterId) };

                if (alliance.Min() <= stand || corporation.Min() <= stand || personal.Min() <= stand)
                {
                    Logging.Log("Cache.LocalSafe", "Bad Standing Pilot Detected: [ " + localMember.Name + "] " + " [ " + number + " ] so far... of [ " + maxBad + " ] allowed", Logging.orange);
                    number++;
                }
                if (number > maxBad)
                {
                    Logging.Log("Cache.LocalSafe", "[" + number + "] Bad Standing pilots in local, We should stay in station", Logging.orange);
                    return false;
                }
            }
            return true;
        }

        public DirectEve DirectEve { get; set; }

        public Dictionary<int, InvType> InvTypesById { get; private set; }

        /// <summary>
        ///   List of ship target values, higher target value = higher kill priority
        /// </summary>
        public List<ShipTargetValue> ShipTargetValues { get; private set; }

        /// <summary>
        ///   Best damage type for the mission
        /// </summary>
        public DamageType DamageType { get; set; }

        /// <summary>
        ///   Best orbit distance for the mission
        /// </summary>
        public int OrbitDistance { get; set; }

        /// <summary>
        ///   Force Salvaging after mission
        /// </summary>
        public bool AfterMissionSalvaging { get; set; }

        public double MaxRange
        {
            get { return Math.Min(Cache.Instance.WeaponRange, Cache.Instance.DirectEve.ActiveShip.MaxTargetRange); }
        }

        /// <summary>
        ///   Returns the maximum weapon distance
        /// </summary>
        public int WeaponRange
        {
            get
            {
                // Get ammo based on current damage type
                IEnumerable<Ammo> ammo = Settings.Instance.Ammo.Where(a => a.DamageType == DamageType).ToList();

                try
                {
                    // Is our ship's cargo available?
                    if ((Cache.Instance.CargoHold != null) && (Cache.Instance.CargoHold.Window.IsReady))
                        ammo = ammo.Where(a => Cache.Instance.CargoHold.Items.Any(i => a.TypeId == i.TypeId && i.Quantity >= Settings.Instance.MinimumAmmoCharges));
                    else
                        return System.Convert.ToInt32(Cache.Instance.DirectEve.ActiveShip.MaxTargetRange);

                    // Return ship range if there's no ammo left
                    if (!ammo.Any())
                        return System.Convert.ToInt32(Cache.Instance.DirectEve.ActiveShip.MaxTargetRange);

                    return ammo.Max(a => a.Range);
                }
                catch (Exception)
                {
                    // Return max range
                    if (Cache.Instance.DirectEve.ActiveShip != null)
                    {
                        return System.Convert.ToInt32(Cache.Instance.DirectEve.ActiveShip.MaxTargetRange);
                    }
                    return 0;
                }
            }
        }

        /// <summary>
        ///   Last target for a certain module
        /// </summary>
        public Dictionary<long, long> LastModuleTargetIDs { get; private set; }

        /// <summary>
        ///   Targeting delay cache (used by LockTarget)
        /// </summary>
        public Dictionary<long, DateTime> TargetingIDs { get; private set; }

        /// <summary>
        ///   Used for Drones to know that it should retract drones
        /// </summary>
        public bool IsMissionPocketDone { get; set; }

        public string ExtConsole { get; set; }

        public string ConsoleLog { get; set; }

        public string ConsoleLogRedacted { get; set; }

        public bool AllAgentsStillInDeclineCoolDown { get; set; }

        private string _agentName = "";

        private DateTime _nextOpenContainerInSpaceAction;

        public DateTime NextOpenContainerInSpaceAction
        {
            get
            {
                return _nextOpenContainerInSpaceAction;
            }
            set
            {
                _nextOpenContainerInSpaceAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextOpenJournalWindowAction;

        public DateTime NextOpenJournalWindowAction
        {
            get
            {
                return _nextOpenJournalWindowAction;
            }
            set
            {
                _nextOpenJournalWindowAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextOpenLootContainerAction;

        public DateTime NextOpenLootContainerAction
        {
            get
            {
                return _nextOpenLootContainerAction;
            }
            set
            {
                _nextOpenLootContainerAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextOpenCorpBookmarkHangarAction;

        public DateTime NextOpenCorpBookmarkHangarAction
        {
            get
            {
                return _nextOpenCorpBookmarkHangarAction;
            }
            set
            {
                _nextOpenCorpBookmarkHangarAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextDroneBayAction;

        public DateTime NextDroneBayAction
        {
            get
            {
                return _nextDroneBayAction;
            }
            set
            {
                _nextDroneBayAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextOpenHangarAction;

        public DateTime NextOpenHangarAction
        {
            get { return _nextOpenHangarAction; }
            set
            {
                _nextOpenHangarAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextOpenCargoAction;

        public DateTime NextOpenCargoAction
        {
            get
            {
                return _nextOpenCargoAction;
            }
            set
            {
                _nextOpenCargoAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _lastAction = DateTime.Now;

        public DateTime LastAction
        {
            get
            {
                return _lastAction;
            }
            set
            {
                _lastAction = value;
            }
        }

        private DateTime _nextArmAction = DateTime.Now;

        public DateTime NextArmAction
        {
            get
            {
                return _nextArmAction;
            }
            set
            {
                _nextArmAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextSalvageAction = DateTime.Now;

        public DateTime NextSalvageAction
        {
            get
            {
                return _nextSalvageAction;
            }
            set
            {
                _nextSalvageAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextLootAction = DateTime.Now;

        public DateTime NextLootAction
        {
            get
            {
                return _nextLootAction;
            }
            set
            {
                _nextLootAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _lastJettison = DateTime.Now;

        public DateTime LastJettison
        {
            get
            {
                return _lastJettison;
            }
            set
            {
                _lastJettison = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextDefenceModuleAction = DateTime.Now;

        public DateTime NextDefenceModuleAction
        {
            get
            {
                return _nextDefenceModuleAction;
            }
            set
            {
                _nextDefenceModuleAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextAfterburnerAction = DateTime.Now;

        public DateTime NextAfterburnerAction
        {
            get { return _nextAfterburnerAction; }
            set
            {
                _nextAfterburnerAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextRepModuleAction = DateTime.Now;

        public DateTime NextRepModuleAction
        {
            get { return _nextRepModuleAction; }
            set
            {
                _nextRepModuleAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextActivateSupportModules = DateTime.Now;

        public DateTime NextActivateSupportModules
        {
            get { return _nextActivateSupportModules; }
            set
            {
                _nextActivateSupportModules = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextRemoveBookmarkAction = DateTime.Now;

        public DateTime NextRemoveBookmarkAction
        {
            get { return _nextRepModuleAction; }
            set
            {
                _nextRemoveBookmarkAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextApproachAction = DateTime.Now;

        public DateTime NextApproachAction
        {
            get { return _nextApproachAction; }
            set
            {
                _nextApproachAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextOrbit;

        public DateTime NextOrbit
        {
            get { return _nextOrbit; }
            set
            {
                _nextOrbit = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextWarpTo;

        public DateTime NextWarpTo
        {
            get { return _nextWarpTo; }
            set
            {
                _nextWarpTo = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextTravelerAction = DateTime.Now;

        public DateTime NextTravelerAction
        {
            get { return _nextTravelerAction; }
            set
            {
                _nextTravelerAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextTargetAction = DateTime.Now;

        public DateTime NextTargetAction
        {
            get { return _nextTargetAction; }
            set
            {
                _nextTargetAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextWeaponAction = DateTime.Now;
        private DateTime _nextReload = DateTime.Now;

        public DateTime NextReload
        {
            get { return _nextReload; }
            set
            {
                _nextReload = value;
                _lastAction = DateTime.Now;
            }
        }

        public DateTime NextWeaponAction
        {
            get { return _nextWeaponAction; }
            set
            {
                _nextWeaponAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextWebAction = DateTime.Now;

        public DateTime NextWebAction
        {
            get { return _nextWebAction; }
            set
            {
                _nextWebAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextNosAction = DateTime.Now;

        public DateTime NextNosAction
        {
            get { return _nextNosAction; }
            set
            {
                _nextNosAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextPainterAction = DateTime.Now;

        public DateTime NextPainterAction
        {
            get { return _nextPainterAction; }
            set
            {
                _nextPainterAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextActivateAction = DateTime.Now;

        public DateTime NextActivateAction
        {
            get { return _nextActivateAction; }
            set
            {
                _nextActivateAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextBookmarkPocketAttempt = DateTime.Now;

        public DateTime NextBookmarkPocketAttempt
        {
            get { return _nextBookmarkPocketAttempt; }
            set
            {
                _nextBookmarkPocketAttempt = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextAlign = DateTime.Now;

        public DateTime NextAlign
        {
            get { return _nextAlign; }
            set
            {
                _nextAlign = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextUndockAction = DateTime.Now;

        public DateTime NextUndockAction
        {
            get { return _nextUndockAction; }
            set
            {
                _nextUndockAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextDockAction = DateTime.Now; //unused

        public DateTime NextDockAction
        {
            get { return _nextDockAction; }
            set
            {
                _nextDockAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextDroneRecall;

        public DateTime NextDroneRecall
        {
            get { return _nextDroneRecall; }
            set
            {
                _nextDroneRecall = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextStartupAction;

        public DateTime NextStartupAction
        {
            get { return _nextStartupAction; }
            set
            {
                _nextStartupAction = value;
                _lastAction = DateTime.Now;
            }
        }

        private DateTime _nextRepairItemsAction;

        public DateTime NextRepairItemsAction
        {
            get { return _nextRepairItemsAction; }
            set
            {
                _nextRepairItemsAction = value;
                _lastAction = DateTime.Now;
            }
        }

        public DateTime LastLocalWatchAction = DateTime.Now;
        public DateTime LastWalletCheck = DateTime.Now;
        public DateTime LastScheduleCheck = DateTime.Now;

        public DateTime LastupdateofSessionRunningTime;
        public DateTime NextInSpaceorInStation;
        public DateTime NextTimeCheckAction = DateTime.Now;

        public DateTime LastFrame = DateTime.Now;
        public DateTime LastSessionIsReady = DateTime.Now;
        public DateTime LastLogMessage = DateTime.Now;

        public int WrecksThisPocket;
        public int WrecksThisMission;
        public DateTime LastLoggingAction = DateTime.MinValue;

        public DateTime LastSessionChange = DateTime.Now;

        public bool Paused { get; set; }

        public int RepairCycleTimeThisPocket { get; set; }

        public int PanicAttemptsThisPocket { get; set; }

        public double LowestShieldPercentageThisMission { get; set; }

        public double LowestArmorPercentageThisMission { get; set; }

        public double LowestCapacitorPercentageThisMission { get; set; }

        public double LowestShieldPercentageThisPocket { get; set; }

        public double LowestArmorPercentageThisPocket { get; set; }

        public double LowestCapacitorPercentageThisPocket { get; set; }

        public int PanicAttemptsThisMission { get; set; }

        public DateTime StartedBoosting { get; set; }

        public int RepairCycleTimeThisMission { get; set; }

        public DateTime LastKnownGoodConnectedTime { get; set; }

        public long TotalMegaBytesOfMemoryUsed { get; set; }

        public double MyWalletBalance { get; set; }

        public string CurrentPocketAction { get; set; }

        public float AgentEffectiveStandingtoMe;
        public string AgentEffectiveStandingtoMe_text;
        public bool Missionbookmarktimerset = false;
        public DateTime Missionbookmarktimeout = DateTime.MaxValue;

        public long AgentStationID { get; set; }

        public string AgentStationName { get; set; }

        public long AgentSolarSystemID { get; set; }

        public string AgentSolarSystemName { get; set; }

        public string CurrentAgent_text = string.Empty;
        public string CurrentAgent
        {
            get
            {
                if (Settings.Instance.CharacterXMLExists)
                {
                    if (_agentName == "")
                    {
                        try
                        {
                            _agentName = SwitchAgent;
                            Logging.Log("Cache.CurrentAgent", "[ " + CurrentAgent + " ] AgentID [ " + AgentId + " ]",
                                        Logging.white);
                            Cache.Instance.CurrentAgent_text = CurrentAgent.ToString();
                        }
                        catch (Exception)
                        {
                            Logging.Log("Cache", "AgentId", "Unable to get agent details: trying again in a moment");
                            return "";
                        }

                    }

                    return _agentName;
                }
                return "";
            }
            set
            {
                _agentName = value;
            }
        }

        public string SwitchAgent
        {
            get
            {
                AgentsList agent = Settings.Instance.AgentsList.OrderBy(j => j.Priorit).FirstOrDefault(i => DateTime.Now >= i.DeclineTimer);
                if (agent == null)
                {
                    try
                    {
                        agent = Settings.Instance.AgentsList.OrderBy(j => j.Priorit).FirstOrDefault();
                    }
                    catch (Exception)
                    {
                        Logging.Log("Cache","SwitchAgent","Unable to process agent section of [" + Settings.Instance.SettingsPath + "] make sure you have a valid agent listed! Pausing so you can fix it.");
                        Cache.Instance.Paused = true;
                    }
                    AllAgentsStillInDeclineCoolDown = true; //this literally means we have no agents available at the moment (decline timer likely)
                }
                else
                    AllAgentsStillInDeclineCoolDown = false; //this literally means we DO have agents available (at least one agents decline timer has expired and is clear to use)

                return agent.Name;
            }
        }

        public long AgentId
        {
            get
            {
                if (Settings.Instance.CharacterXMLExists)
                {
                    try
                    {
                        _agent = DirectEve.GetAgentByName(CurrentAgent);
                        _agentId = _agent.AgentId;

                        return _agentId ?? -1;
                    }
                    catch (Exception)
                    {
                        Logging.Log("Cache", "AgentId", "Unable to get agent details: trying again in a moment");
                        return -1;
                    }
                    
                }
                return -1;
            }
        }

        public DirectAgent Agent
        {
            get
            {
                if (Settings.Instance.CharacterXMLExists)
                {
                    try
                    {
                        _agent = DirectEve.GetAgentByName(CurrentAgent);
                        if (_agent != null)
                        {
                            _agentId = _agent.AgentId;
                            //Logging.Log("Cache: CurrentAgent", "Processing Agent Info...", Logging.white);
                            Cache.Instance.AgentStationName = Cache.Instance.DirectEve.GetLocationName(Cache.Instance._agent.StationId);
                            Cache.Instance.AgentStationID = Cache.Instance._agent.StationId;
                            Cache.Instance.AgentSolarSystemName = Cache.Instance.DirectEve.GetLocationName(Cache.Instance._agent.SolarSystemId);
                            Cache.Instance.AgentSolarSystemID = Cache.Instance._agent.SolarSystemId;
                            //Logging.Log("Cache: CurrentAgent", "AgentStationName [" + Cache.Instance.AgentStationName + "]", Logging.white);
                            //Logging.Log("Cache: CurrentAgent", "AgentStationID [" + Cache.Instance.AgentStationID + "]", Logging.white);
                            //Logging.Log("Cache: CurrentAgent", "AgentSolarSystemName [" + Cache.Instance.AgentSolarSystemName + "]", Logging.white);
                            //Logging.Log("Cache: CurrentAgent", "AgentSolarSystemID [" + Cache.Instance.AgentSolarSystemID + "]", Logging.white);
                        }
                    }
                    catch (Exception)
                    {
                        Logging.Log("Cache", "Agent", "Unable to process agent section of [" + Settings.Instance.SettingsPath + "] make sure you have a valid agent listed! Pausing so you can fix it.");
                        Cache.Instance.Paused = true;
                    }
                    if (_agentId != null) return _agent ?? (_agent = DirectEve.GetAgentById(_agentId.Value));
                }
                return null;
            }
        }

        public IEnumerable<ModuleCache> Modules
        {
            get { return _modules ?? (_modules = DirectEve.Modules.Select(m => new ModuleCache(m, 0)).ToList()); }
        }

        //
        // this CAN and should just list all possible weapon system groupIDs
        //
        public IEnumerable<ModuleCache> Weapons
        {
            get
            {
                if (Cache.Instance.MissionWeaponGroupId != 0)
                    return Modules.Where(m => m.GroupId == Cache.Instance.MissionWeaponGroupId);
                else return Modules.Where(m =>
                    m.GroupId == Settings.Instance.WeaponGroupId); // ||
                //m.GroupId == (int)Group.ProjectileWeapon ||
                //m.GroupId == (int)Group.EnergyWeapon ||
                //m.GroupId == (int)Group.HybridWeapon ||
                //m.GroupId == (int)Group.CruiseMissileLaunchers ||
                //m.GroupId == (int)Group.RocketLaunchers ||
                //m.GroupId == (int)Group.StandardMissileLaunchers ||
                //m.GroupId == (int)Group.TorpedoLaunchers ||
                //m.GroupId == (int)Group.AssaultMissilelaunchers ||
                //m.GroupId == (int)Group.HeavyMissilelaunchers ||
                //m.GroupId == (int)Group.DefenderMissilelaunchers);
            }
        }

        public IEnumerable<EntityCache> Containers
        {
            get
            {
                return _containers ?? (_containers = Entities.Where(e =>
                          e.IsContainer && e.HaveLootRights && (e.GroupId != (int)Group.Wreck || !e.IsWreckEmpty) &&
                          (e.Name != (String)"Abandoned Container")).
                          ToList());
            }
        }

        public IEnumerable<EntityCache> Wrecks
        {
            get { return _containers ?? (_containers = Entities.Where(e => (e.GroupId != (int)Group.Wreck)).ToList()); }
        }

        public IEnumerable<EntityCache> UnlootedContainers
        {
            get
            {
                return _unlootedContainers ?? (_unlootedContainers = Entities.Where(e =>
                          e.IsContainer &&
                          e.HaveLootRights &&
                          (!LootedContainers.Contains(e.Id) || e.GroupId == (int)Group.Wreck)).OrderBy(
                              e => e.Distance).
                              ToList());
            }
        }

        //This needs to include items you can steal from (thus gain aggro)
        public IEnumerable<EntityCache> UnlootedWrecksAndSecureCans
        {
            get
            {
                return _unlootedWrecksAndSecureCans ?? (_unlootedWrecksAndSecureCans = Entities.Where(e =>
                          (e.GroupId == (int)Group.Wreck || e.GroupId == (int)Group.SecureContainer ||
                           e.GroupId == (int)Group.AuditLogSecureContainer ||
                           e.GroupId == (int)Group.FreightContainer) && !e.IsWreckEmpty).OrderBy(e => e.Distance).
                          ToList());
            }
        }

        public IEnumerable<EntityCache> Targets
        {
            get
            {
                if (_targets == null)
                    _targets = Entities.Where(e => e.IsTarget).ToList();

                // Remove the target info (its been targeted)
                foreach (EntityCache target in _targets.Where(t => TargetingIDs.ContainsKey(t.Id)))
                    TargetingIDs.Remove(target.Id);

                return _targets;
            }
        }

        public IEnumerable<EntityCache> Targeting
        {
            get { return _targeting ?? (_targeting = Entities.Where(e => e.IsTargeting).ToList()); }
        }

        public DateTime IDsInInventoryTreeTimeStamp;

        public List<long> IDsinInventoryTree
        {
            get 
            { 
                if (_idsinInventoryTree.Any())
                {
                    if (DateTime.Now > IDsInInventoryTreeTimeStamp.AddSeconds(30))
                    {
                        Logging.Log("Cache.IDsinInventoryTree", "Refreshing IDs from inventory tree, it has been longer than 30 seconds since the last refresh", Logging.teal);
                        IDsInInventoryTreeTimeStamp = DateTime.Now;
                        return _idsinInventoryTree = Cache.Instance.InventoryWindow.GetIdsFromTree(false);
                    }
                    else
                    {
                        return _idsinInventoryTree ?? (_idsinInventoryTree = Cache.Instance.InventoryWindow.GetIdsFromTree(false));
                    }
                }
                else
                {
                    IDsInInventoryTreeTimeStamp = DateTime.Now;
                    return _idsinInventoryTree = Cache.Instance.InventoryWindow.GetIdsFromTree(false);
                }

            }
        }


        public IEnumerable<EntityCache> TargetedBy
        {
            get { return _targetedBy ?? (_targetedBy = Entities.Where(e => e.IsTargetedBy).ToList()); }
        }

        public IEnumerable<EntityCache> Aggressed
        {
            get { return _aggressed ?? (_aggressed = Entities.Where(e => e.IsTargetedBy && e.IsAttacking).ToList()); }
        }

        public IEnumerable<EntityCache> Entities
        {
            get
            {
                if (!InSpace)
                    return new List<EntityCache>();

                return _entities ?? (_entities = DirectEve.Entities.Select(e => new EntityCache(e)).Where(e => e.IsValid).ToList());
            }
        }

        public IEnumerable<EntityCache> EntitiesNotSelf
        {
            get
            {
                if (!InSpace)
                    return new List<EntityCache>();

                return _entities ?? (_entities = DirectEve.Entities.Select(e => new EntityCache(e)).Where(e => e.IsValid && e.Name != Settings.Instance.CharacterName).ToList());
            }
        }

        public bool InSpace
        {
            get
            {
                try
                {
                    if (DirectEve.Session.IsInSpace && !DirectEve.Session.IsInStation && DirectEve.Session.IsReady && DirectEve.ActiveShip.Entity != null)
                    {
                        Cache.Instance.LastInSpace = DateTime.Now;
                        return true;
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool InStation
        {
            get
            {
                try
                {
                    if (DirectEve.Session.IsInStation && !DirectEve.Session.IsInSpace && DirectEve.Session.IsReady)
                    {
                        Cache.Instance.LastInStation = DateTime.Now;
                        return true;
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool InWarp
        {
            get { return DirectEve.ActiveShip != null && (DirectEve.ActiveShip.Entity != null && DirectEve.ActiveShip.Entity.Mode == 3); }
        }

        public bool IsOrbiting
        {
            get { return DirectEve.ActiveShip.Entity != null && DirectEve.ActiveShip.Entity.Mode == 4; }
        }

        public bool IsApproaching
        {
            get
            {
                //Logging.Log("Cache.IsApproaching: " + DirectEve.ActiveShip.Entity.Mode.ToString(CultureInfo.InvariantCulture));
                return DirectEve.ActiveShip.Entity != null && DirectEve.ActiveShip.Entity.Mode == 1;
            }
        }

        public bool IsApproachingOrOrbiting
        {
            get { return DirectEve.ActiveShip.Entity != null && (DirectEve.ActiveShip.Entity.Mode == 1 || DirectEve.ActiveShip.Entity.Mode == 4); }
        }

        public IEnumerable<EntityCache> ActiveDrones
        {
            get { return _activeDrones ?? (_activeDrones = DirectEve.ActiveDrones.Select(d => new EntityCache(d)).ToList()); }
        }

        public IEnumerable<EntityCache> Stations
        {
            get { return _stations ?? (_stations = Entities.Where(e => e.CategoryId == (int)CategoryID.Station).ToList()); }
        }

        public EntityCache ClosestStation
        {
            get { return _closeststation ?? (_closeststation = Entities.Where(e => e.CategoryId == (int)CategoryID.Station).ToList().OrderBy(s => s.Distance).FirstOrDefault() ?? Entities.OrderByDescending(s => s.Distance).FirstOrDefault()); }
        }

        public EntityCache StationByName(string stationName)
        {
            EntityCache _station = Stations.First(x => x.Name.ToLower() == stationName.ToLower());
            return _station;
        }

        public IEnumerable<DirectSolarSystem> SolarSystems
        {
            get
            {
                var _solarSystems = DirectEve.SolarSystems.Values.OrderBy(s => s.Name).ToList();
                return _solarSystems;
            }
        }

        public IEnumerable<EntityCache> Stargates
        {
            get { return _stargates ?? (_stargates = Entities.Where(e => e.GroupId == (int)Group.Stargate).ToList()); }
        }

        public EntityCache ClosestStargate
        {
            get { return _closeststargate ?? (_closeststargate = Entities.Where(e => e.GroupId == (int)Group.Stargate).ToList().OrderBy(s => s.Distance).FirstOrDefault() ?? Entities.OrderByDescending(s => s.Distance).FirstOrDefault()); }
        }

        public EntityCache StargateByName(string locationName)
        {
            {
                return _stargate ??
                       (_stargate =
                        Cache.Instance.EntitiesByName(locationName).FirstOrDefault(
                            e => e.GroupId == (int)Group.Stargate));
            }
        }

        public IEnumerable<EntityCache> BigObjects
        {
            get
            {
                return _bigobjects ?? (_bigobjects = Entities.Where(e =>
                       e.GroupId == (int)Group.LargeCollidableStructure ||
                       e.GroupId == (int)Group.LargeCollidableObject ||
                       e.GroupId == (int)Group.LargeCollidableShip ||
                       e.CategoryId == (int)CategoryID.Asteroid ||
                       e.GroupId == (int)Group.SpawnContainer &&
                       e.Distance < (double)Distance.DirectionalScannerCloseRange).OrderBy(t => t.Distance).ToList());
            }
        }

        public IEnumerable<EntityCache> AccelerationGates
        {
            get
            {
                return _gates ?? (_gates = Entities.Where(e =>
                       e.GroupId == (int)Group.AccellerationGate &&
                       e.Distance < (double)Distance.OnGridWithMe).OrderBy(t => t.Distance).ToList());
            }
        }

        public IEnumerable<EntityCache> BigObjectsandGates
        {
            get
            {
                return _bigobjectsandgates ?? (_bigobjectsandgates = Entities.Where(e =>
                       e.GroupId == (int)Group.LargeCollidableStructure ||
                       e.GroupId == (int)Group.LargeCollidableObject ||
                       e.GroupId == (int)Group.LargeCollidableShip ||
                       e.CategoryId == (int)CategoryID.Asteroid ||
                       e.GroupId == (int)Group.AccellerationGate ||
                       e.GroupId == (int)Group.SpawnContainer &&
                       e.Distance < (double)Distance.DirectionalScannerCloseRange).OrderBy(t => t.Distance).ToList());
            }
        }

        public IEnumerable<EntityCache> Objects
        {
            get
            {
                return _objects ?? (_objects = Entities.Where(e =>
                       //e.CategoryId != (int)CategoryID.Entity && 
                       !e.IsPlayer &&
                       e.GroupId != (int)Group.SpawnContainer &&
                       e.GroupId != (int)Group.Wreck &&
                       //e.GroupId != (int)Group.Stargate &&
                       //e.GroupId != (int)Group.Station &&
                       e.Distance < 200000).OrderBy(t => t.Distance).ToList());
            }
        }

        public EntityCache Star
        {
            get { return _star ?? (_star = Entities.FirstOrDefault(e => e.CategoryId == (int)CategoryID.Celestial && e.GroupId == (int)Group.Star)); }
        }

        public IEnumerable<EntityCache> PriorityTargets
        {
            get
            {
                _priorityTargets.RemoveAll(pt => pt.Entity == null);
                return _priorityTargets.OrderBy(pt => pt.Priority).ThenBy(pt => (pt.Entity.ShieldPct + pt.Entity.ArmorPct + pt.Entity.StructurePct)).ThenBy(pt => pt.Entity.Distance).Select(pt => pt.Entity);
            }
        }

        public EntityCache Approaching
        {
            get
            {
                if (_approaching == null)
                {
                    DirectEntity ship = DirectEve.ActiveShip.Entity;
                    if (ship != null && ship.IsValid)
                        _approaching = EntityById(ship.FollowId);
                }

                return _approaching != null && _approaching.IsValid ? _approaching : null;
            }
            set { _approaching = value; }
        }

        public List<DirectWindow> Windows
        {
            get
            {
                if (Cache.Instance.InSpace || Cache.Instance.InStation)
                {
                    return _windows ?? (_windows = DirectEve.Windows);
                }
                return null;
            }
        }

        /// <summary>
        ///   Returns the mission for a specific agent
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns>null if no mission could be found</returns>
        public DirectAgentMission GetAgentMission(long agentId)
        {
            return DirectEve.AgentMissions.FirstOrDefault(m => m.AgentId == agentId);
        }

        /// <summary>
        ///   Returns the mission objectives from
        /// </summary>
        public List<string> MissionItems { get; private set; }

        /// <summary>
        ///   Returns the item that needs to be brought on the mission
        /// </summary>
        /// <returns></returns>
        public string BringMissionItem { get; private set; }

        public int BringMissionItemQuantity { get; private set; }

        public string BringOptionalMissionItem { get; private set; }

        public int BringOptionalMissionItemQuantity { get; private set; }

        /// <summary>         
        ///   Range for warp to mission bookmark         
        /// </summary>
        public double MissionWarpAtDistanceRange { get; set; } //in km

        public string Fitting { get; set; } // stores name of the final fitting we want to use

        public string MissionShip { get; set; } //stores name of mission specific ship

        public string DefaultFitting { get; set; } //stores name of the default fitting

        public string CurrentFit { get; set; }

        public string FactionFit { get; set; }

        public string FactionName { get; set; }

        public bool ArmLoadedCache { get; set; } // flags whether arm has already loaded the mission

        public bool UseMissionShip { get; set; } // flags whether we're using a mission specific ship

        public bool ChangeMissionShipFittings { get; set; } // used for situations in which missionShip's specified, but no faction or mission fittings are; prevents default

        public List<Ammo> MissionAmmo;

        public int MissionWeaponGroupId { get; set; }

        public bool? MissionUseDrones { get; set; }

        public bool? MissionKillSentries { get; set; }

        public bool StopTimeSpecified = true;

        public DateTime StopTime = DateTime.Now.AddHours(10);

        public DateTime ManualStopTime = DateTime.Now.AddHours(10);

        public DateTime ManualRestartTime = DateTime.Now.AddHours(10);

        public DateTime StartTime { get; set; }

        public int MaxRuntime { get; set; }

        public DateTime LastInStation { get; set; }

        public DateTime LastInSpace { get; set; }

        public bool CloseQuestorCMDLogoff; //false;

        public bool CloseQuestorCMDExitGame = true;

        public bool CloseQuestorEndProcess = false;

        public bool GotoBaseNow; //false;

        public string ReasonToStopQuestor { get; set; }

        public string SessionState { get; set; }

        public double SessionIskGenerated { get; set; }

        public double SessionLootGenerated { get; set; }

        public double SessionLPGenerated { get; set; }

        public int SessionRunningTime { get; set; }

        public double SessionIskPerHrGenerated { get; set; }

        public double SessionLootPerHrGenerated { get; set; }

        public double SessionLPPerHrGenerated { get; set; }

        public double SessionTotalPerHrGenerated { get; set; }

        public bool QuestorJustStarted = true;
        public DateTime EnteredCloseQuestor_DateTime;
        public bool DropMode { get; set; }

        public DirectWindow GetWindowByCaption(string caption)
        {
            return Windows.FirstOrDefault(w => w.Caption.Contains(caption));
        }

        public DirectWindow GetWindowByName(string name)
        {
            // Special cases
            if (name == "Local")
                return Windows.FirstOrDefault(w => w.Name.StartsWith("chatchannel_solarsystemid"));

            return Windows.FirstOrDefault(w => w.Name == name);
        }

        /// <summary>
        ///   Return entities by name
        /// </summary>
        /// <param name = "name"></param>
        /// <returns></returns>
        public IEnumerable<EntityCache> EntitiesByName(string name)
        {
            return Entities.Where(e => e.Name == name).ToList();
        }

        /// <summary>
        ///   Return entity by name
        /// </summary>
        /// <param name = "name"></param>
        /// <returns></returns>
        public EntityCache EntityByName(string name)
        {
            return Entities.FirstOrDefault(e => System.String.Compare(e.Name, name, System.StringComparison.OrdinalIgnoreCase) == 0);
        }

        public IEnumerable<EntityCache> EntitiesByNamePart(string name)
        {
            return Entities.Where(e => e.Name.Contains(name)).ToList();
        }

        /// <summary>
        ///   Return entities that contain the name
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EntityCache> EntitiesThatContainTheName(string label)
        {
            return Entities.Where(e => !string.IsNullOrEmpty(e.Name) && e.Name.Contains(label)).ToList();
        }

        /// <summary>
        ///   Return a cached entity by Id
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        public EntityCache EntityById(long id)
        {
            if (_entitiesById.ContainsKey(id))
                return _entitiesById[id];

            EntityCache entity = Entities.FirstOrDefault(e => e.Id == id);
            _entitiesById[id] = entity;
            return entity;
        }

        /// <summary>
        ///   Returns the first mission bookmark that starts with a certain string
        /// </summary>
        /// <returns></returns>
        public DirectAgentMissionBookmark GetMissionBookmark(long agentId, string startsWith)
        {
            // Get the missions
            DirectAgentMission missionforbookmarkinfo = GetAgentMission(agentId);
            if (missionforbookmarkinfo == null)
            {
                Logging.Log("Cache.DirectAgentMissionBookmark", "missionforbookmarkinfo [null] <---bad  parameters passed to us:  agentid [" + agentId + "] startswith [" + startsWith + "]", Logging.white);
                return null;
            }

            // Did we accept this mission?
            if (missionforbookmarkinfo.State != (int)MissionState.Accepted || missionforbookmarkinfo.AgentId != agentId)
            {
                //Logging.Log("missionforbookmarkinfo.State: [" + missionforbookmarkinfo.State.ToString(CultureInfo.InvariantCulture) + "]");
                //Logging.Log("missionforbookmarkinfo.AgentId: [" + missionforbookmarkinfo.AgentId.ToString(CultureInfo.InvariantCulture) + "]");
                //Logging.Log("agentId: [" + agentId.ToString(CultureInfo.InvariantCulture) + "]");
                return null;
            }

            return missionforbookmarkinfo.Bookmarks.FirstOrDefault(b => b.Title.ToLower().StartsWith(startsWith.ToLower()));
        }

        /// <summary>
        ///   Return a bookmark by id
        /// </summary>
        /// <param name = "bookmarkId"></param>
        /// <returns></returns>
        public DirectBookmark BookmarkById(long bookmarkId)
        {
            return DirectEve.Bookmarks.FirstOrDefault(b => b.BookmarkId == bookmarkId);
        }

        /// <summary>
        ///   Returns bookmarks that start with the supplied label
        /// </summary>
        /// <param name = "label"></param>
        /// <returns></returns>
        public List<DirectBookmark> BookmarksByLabel(string label)
        {
            // Does not seems to refresh the Corporate Bookmark list so it's having troubles to find Corporate Bookmarks
            return DirectEve.Bookmarks.Where(b => !string.IsNullOrEmpty(b.Title) && b.Title.StartsWith(label)).OrderBy(f => f.LocationId).ToList();
        }

        /// <summary>
        ///   Returns bookmarks that contain the supplied label anywhere in the title
        /// </summary>
        /// <param name = "label"></param>
        /// <returns></returns>
        public List<DirectBookmark> BookmarksThatContain(string label)
        {
            return DirectEve.Bookmarks.Where(b => !string.IsNullOrEmpty(b.Title) && b.Title.Contains(label)).ToList();
        }

        /// <summary>
        ///   Invalidate the cached items
        /// </summary>
        public void InvalidateCache()
        {
            _windows = null;
            _unlootedContainers = null;
            _star = null;
            _stations = null;
            _stargates = null;
            _modules = null;
            _targets = null;
            _targeting = null;
            _targetedBy = null;
            _entities = null;
            _agent = null;
            _approaching = null;
            _activeDrones = null;
            _containers = null;
            _priorityTargets.ForEach(pt => pt.ClearCache());
            _entitiesById.Clear();
        }

        public string FilterPath(string path)
        {
            if (path == null)
                return string.Empty;

            path = path.Replace("\"", "");
            path = path.Replace("?", "");
            path = path.Replace("\\", "");
            path = path.Replace("/", "");
            path = path.Replace("'", "");
            path = path.Replace("*", "");
            path = path.Replace(":", "");
            path = path.Replace(">", "");
            path = path.Replace("<", "");
            path = path.Replace(".", "");
            path = path.Replace(",", "");
            while (path.IndexOf("  ", System.StringComparison.Ordinal) >= 0)
                path = path.Replace("  ", " ");
            return path.Trim();
        }

        /// <summary>
        ///   Loads mission objectives from XML file
        /// </summary>
        /// <param name = "agentId"> </param>
        /// <param name = "pocketId"> </param>
        /// <param name = "missionMode"> </param>
        /// <returns></returns>
        public IEnumerable<Actions.Action> LoadMissionActions(long agentId, int pocketId, bool missionMode)
        {
            DirectAgentMission missiondetails = GetAgentMission(agentId);
            if (missiondetails == null && missionMode)
                return new Actions.Action[0];

            if (missiondetails != null)
            {
                string missionName = FilterPath(missiondetails.Name);
                Cache.Instance.missionXmlPath = System.IO.Path.Combine(Settings.Instance.MissionsPath, missionName + ".xml");
                if (!File.Exists(Cache.Instance.missionXmlPath))
                {
                    //No mission file but we need to set some cache settings
                    OrbitDistance = Settings.Instance.OrbitDistance;
                    AfterMissionSalvaging = Settings.Instance.AfterMissionSalvaging;
                    return new Actions.Action[0];
                }
                //
                // this loads the settings from each pocket... but NOT any settings global to the mission
                //
                try
                {
                    XDocument xdoc = XDocument.Load(Cache.Instance.missionXmlPath);
                    if (xdoc.Root != null)
                    {
                        XElement xElement = xdoc.Root.Element("pockets");
                        if (xElement != null)
                        {
                            IEnumerable<XElement> pockets = xElement.Elements("pocket");
                            foreach (XElement pocket in pockets)
                            {
                                if ((int)pocket.Attribute("id") != pocketId)
                                    continue;

                                if (pocket.Element("damagetype") != null)
                                    DamageType = (DamageType)Enum.Parse(typeof(DamageType), (string)pocket.Element("damagetype"), true);

                                if (pocket.Element("orbitdistance") != null) 	//Load OrbitDistance from mission.xml, if present
                                {
                                    OrbitDistance = (int)pocket.Element("orbitdistance");
                                    Logging.Log("Cache", "Using Mission Orbit distance [" + OrbitDistance + "]", Logging.white);
                                }
                                else //Otherwise, use value defined in charname.xml file
                                {
                                    OrbitDistance = Settings.Instance.OrbitDistance;
                                    Logging.Log("Cache", "Using Settings Orbit distance [" + OrbitDistance + "]", Logging.white);
                                }
                                if (pocket.Element("afterMissionSalvaging") != null) 	//Load afterMissionSalvaging setting from mission.xml, if present
                                {
                                    AfterMissionSalvaging = (bool)pocket.Element("afterMissionSalvaging");
                                }
                                if (pocket.Element("dronesKillHighValueTargets") != null) 	//Load afterMissionSalvaging setting from mission.xml, if present
                                {
                                    DronesKillHighValueTargets = (bool)pocket.Element("dronesKillHighValueTargets");
                                }
                                else //Otherwise, use value defined in charname.xml file
                                {
                                    DronesKillHighValueTargets = Settings.Instance.DronesKillHighValueTargets;
                                    //Logging.Log(string.Format("Cache: Using Character Setting DroneKillHighValueTargets  {0}", DronesKillHighValueTargets));
                                }
                                var actions = new List<Actions.Action>();
                                XElement elements = pocket.Element("actions");
                                if (elements != null)
                                {
                                    foreach (XElement element in elements.Elements("action"))
                                    {
                                        var action = new Actions.Action();
                                        action.State = (ActionState)Enum.Parse(typeof(ActionState), (string)element.Attribute("name"), true);
                                        XAttribute xAttribute = element.Attribute("name");
                                        if (xAttribute != null && xAttribute.Value == "ClearPocket")
                                        {
                                            action.AddParameter("", "");
                                        }
                                        else
                                        {
                                            foreach (XElement parameter in element.Elements("parameter"))
                                                action.AddParameter((string)parameter.Attribute("name"), (string)parameter.Attribute("value"));
                                        }
                                        actions.Add(action);
                                    }
                                }
                                return actions;
                            }
                            //actions.Add(action);
                        }
                        else
                        {
                            return new Actions.Action[0];
                        }
                    }
                    else
                    {
                        { return new Actions.Action[0]; }
                    }

                    // if we reach this code there is no mission XML file, so we set some things -- Assail

                    OrbitDistance = Settings.Instance.OrbitDistance;
                    Logging.Log("Cache", "Using Settings Orbit distance [" + Settings.Instance.OrbitDistance + "]", Logging.white);

                    return new Actions.Action[0];
                }
                catch (Exception ex)
                {
                    Logging.Log("Cache", "Error loading mission XML file [" + ex.Message + "]", Logging.orange);
                    return new Actions.Action[0];
                }
            }
            else
            {
                { return new Actions.Action[0]; }
            }
        }

        /// <summary>
        ///   Refresh the mission items
        /// </summary>
        public void RefreshMissionItems(long agentId)
        {
            // Clear out old items
            MissionItems.Clear();
            BringMissionItem = string.Empty;
            BringOptionalMissionItem = string.Empty;

            DirectAgentMission missiondetailsformittionitems = GetAgentMission(agentId);
            if (missiondetailsformittionitems == null)
                return;
            if (string.IsNullOrEmpty(FactionName))
                FactionName = "Default";

            if (Settings.Instance.UseFittingManager)
            {
                //Set fitting to default
                DefaultFitting = Settings.Instance.DefaultFitting.Fitting;
                Fitting = DefaultFitting;
                MissionShip = "";
                ChangeMissionShipFittings = false;
                if (Settings.Instance.MissionFitting.Any(m => m.Mission.ToLower() == missiondetailsformittionitems.Name.ToLower())) //priority goes to mission-specific fittings
                {
                    MissionFitting missionFitting;

                    // if we've got multiple copies of the same mission, find the one with the matching faction
                    if (Settings.Instance.MissionFitting.Any(m => m.Faction.ToLower() == FactionName.ToLower() && (m.Mission.ToLower() == missiondetailsformittionitems.Name.ToLower())))
                        missionFitting = Settings.Instance.MissionFitting.FirstOrDefault(m => m.Faction.ToLower() == FactionName.ToLower() && (m.Mission.ToLower() == missiondetailsformittionitems.Name.ToLower()));
                    else //otherwise just use the first copy of that mission
                        missionFitting = Settings.Instance.MissionFitting.FirstOrDefault(m => m.Mission.ToLower() == missiondetailsformittionitems.Name.ToLower());

                    var missionFit = (string)missionFitting.Fitting;
                    var missionShip = (string)missionFitting.Ship;
                    if (!(missionFit == "" && missionShip != "")) // if we've both specified a mission specific ship and a fitting, then apply that fitting to the ship
                    {
                        ChangeMissionShipFittings = true;
                        Fitting = missionFit;
                    }
                    else if (!string.IsNullOrEmpty(FactionFit))
                        Fitting = FactionFit;
                    Logging.Log("Cache", "Mission: " + missionFitting.Mission + " - Faction: " + FactionName + " - Fitting: " + missionFit + " - Ship: " + missionShip + " - ChangeMissionShipFittings: " + ChangeMissionShipFittings, Logging.white);
                    MissionShip = missionShip;
                }
                else if (!string.IsNullOrEmpty(FactionFit)) // if no mission fittings defined, try to match by faction
                    Fitting = FactionFit;

                if (Fitting == "") // otherwise use the default
                    Fitting = DefaultFitting;
            }

            string missionName = FilterPath(missiondetailsformittionitems.Name);
            Cache.Instance.missionXmlPath = System.IO.Path.Combine(Settings.Instance.MissionsPath, missionName + ".xml");
            if (!File.Exists(Cache.Instance.missionXmlPath))
                return;

            try
            {
                XDocument xdoc = XDocument.Load(Cache.Instance.missionXmlPath);
                IEnumerable<string> items = ((IEnumerable)xdoc.XPathEvaluate("//action[(translate(@name, 'LOT', 'lot')='loot') or (translate(@name, 'LOTIEM', 'lotiem')='lootitem')]/parameter[translate(@name, 'TIEM', 'tiem')='item']/@value")).Cast<XAttribute>().Select(a => ((string)a ?? string.Empty).ToLower());
                MissionItems.AddRange(items);

                if (xdoc.Root != null) BringMissionItem = (string)xdoc.Root.Element("bring") ?? string.Empty;
                BringMissionItem = BringMissionItem.ToLower();

                if (xdoc.Root != null) BringMissionItemQuantity = (int?)xdoc.Root.Element("bringquantity") ?? 1;
                BringMissionItemQuantity = BringMissionItemQuantity;

                if (xdoc.Root != null) BringOptionalMissionItem = (string)xdoc.Root.Element("trytobring") ?? string.Empty;
                BringOptionalMissionItem = BringOptionalMissionItem.ToLower();

                if (xdoc.Root != null) BringOptionalMissionItemQuantity = (int?)xdoc.Root.Element("trytobringquantity") ?? 1;
                BringOptionalMissionItemQuantity = BringOptionalMissionItemQuantity;

                //load fitting setting from the mission file
                //Fitting = (string)xdoc.Root.Element("fitting") ?? "default";
            }
            catch (Exception ex)
            {
                Logging.Log("Cache", "Error loading mission XML file [" + ex.Message + "]", Logging.orange);
            }
        }

        /// <summary>
        ///   Remove targets from priority list
        /// </summary>
        /// <param name = "targets"></param>
        public bool RemovePriorityTargets(IEnumerable<EntityCache> targets)
        {
            return _priorityTargets.RemoveAll(pt => targets.Any(t => t.Id == pt.EntityID)) > 0;
        }

        /// <summary>
        ///   Add priority targets
        /// </summary>
        /// <param name = "targets"></param>
        /// <param name = "priority"></param>
        public void AddPriorityTargets(IEnumerable<EntityCache> targets, Priority priority)
        {
            foreach (EntityCache target in targets)
            {
                if (_priorityTargets.Any(pt => pt.EntityID == target.Id))
                    continue;

                _priorityTargets.Add(new PriorityTarget { EntityID = target.Id, Priority = priority });
            }
        }

        /// <summary>
        ///   Calculate distance from me
        /// </summary>
        /// <param name = "x"></param>
        /// <param name = "y"></param>
        /// <param name = "z"></param>
        /// <returns></returns>
        public double DistanceFromMe(double x, double y, double z)
        {
            if (DirectEve.ActiveShip.Entity == null)
                return double.MaxValue;

            double curX = DirectEve.ActiveShip.Entity.X;
            double curY = DirectEve.ActiveShip.Entity.Y;
            double curZ = DirectEve.ActiveShip.Entity.Z;

            return Math.Sqrt((curX - x) * (curX - x) + (curY - y) * (curY - y) + (curZ - z) * (curZ - z));
        }

        /// <summary>
        ///   Calculate distance from entity
        /// </summary>
        /// <param name = "x"></param>
        /// <param name = "y"></param>
        /// <param name = "z"></param>
        /// <param name="entity"> </param>
        /// <returns></returns>
        public double DistanceFromEntity(double x, double y, double z, DirectEntity entity)
        {
            if (entity == null)
                return double.MaxValue;

            double curX = entity.X;
            double curY = entity.Y;
            double curZ = entity.Z;

            return Math.Sqrt((curX - x) * (curX - x) + (curY - y) * (curY - y) + (curZ - z) * (curZ - z));
        }

        /// <summary>
        ///   Create a bookmark
        /// </summary>
        /// <param name = "label"></param>
        public void CreateBookmark(string label)
        {
            if (Cache.Instance.AfterMissionSalvageBookmarks.Count() < 100)
            {
                if (Settings.Instance.CreateSalvageBookmarksIn.ToLower() == "corp".ToLower())
                    DirectEve.CorpBookmarkCurrentLocation(label, "", null);
                else
                    DirectEve.BookmarkCurrentLocation(label, "", null);
            }
            else
            {
                Logging.Log("CreateBookmark","We already have over 100 AfterMissionSalvage bookmarks: their must be a issue processing or deleting bookmarks. No additional bookmarks will be created until the number of salvage bookmarks drops below 100.",Logging.orange);
            }
        }

        /// <summary>
        ///   Create a bookmark of the closest wreck
        /// </summary>
        //public void CreateBookmarkofWreck(IEnumerable<EntityCache> containers, string label)
        //{
        //    DirectEve.BookmarkEntity(Cache.Instance.Containers.FirstOrDefault, "a", "a", null);
        //}

        private Func<EntityCache, int> OrderByLowestHealth()
        {
            return t => (int)(t.ShieldPct + t.ArmorPct + t.StructurePct);
        }

        //public List <long> BookMarkToDestination(DirectBookmark bookmark)
        //{
            //Directdestination = new MissionBookmarkDestination(Cache.Instance.GetMissionBookmark(Cache.Instance.AgentId, "Encounter"));
            //return List<long> destination;
        //}

        public bool CheckifRouteIsAllHighSec()
        {
            Cache.Instance.RouteIsAllHighSecBool = false;
            // Find the first waypoint
            List<long> currentPath = DirectEve.Navigation.GetDestinationPath();
            if (currentPath == null || !currentPath.Any()) return false;

            for (int i = currentPath.Count - 1; i >= 0; i--)
            {
                DirectSolarSystem solarSystemInRoute = Cache.Instance.DirectEve.SolarSystems[currentPath[i]];
                if (solarSystemInRoute.Security < 0.5)
                {
                    //Bad bad bad
                    Cache.Instance.RouteIsAllHighSecBool = false;
                    return true;
                }
            }
            Cache.Instance.RouteIsAllHighSecBool = true;
            return true;
        }

        /// <summary>
        ///   Return the best possible target (based on current target, distance and low value first)
        /// </summary>
        /// <param name="currentTarget"></param>
        /// <param name="distance"></param>
        /// <param name="lowValueFirst"></param>
        /// <param name="callingroutine"> </param>
        /// <returns></returns>
        public EntityCache GetBestTarget(EntityCache currentTarget, double distance, bool lowValueFirst, string callingroutine)
        {
            // Do we have a 'current target' and if so, is it an actual target?
            // If not, clear current target
            if (currentTarget != null && !currentTarget.IsTarget)
                currentTarget = null;

            // Is our current target a warp scrambling priority target?
            if (currentTarget != null && PriorityTargets.Any(pt => pt.Id == currentTarget.Id && pt.IsWarpScramblingMe && pt.IsTarget))
                return currentTarget;

            // Get the closest warp scrambling priority target
            EntityCache warpscramblingtarget = PriorityTargets.OrderBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault(pt => pt.Distance < distance && pt.IsWarpScramblingMe && pt.IsTarget);
            if (warpscramblingtarget != null)
                return warpscramblingtarget;

            if (Settings.Instance.SpeedTank) //all webbers have to be relatively close so processing them all is ok
            {
                // Is our current target a webbing priority target?
                if (currentTarget != null && !Cache.Instance.IgnoreTargets.Contains(currentTarget.Name.Trim()) && PriorityTargets.Any(pt => pt.Id == currentTarget.Id && pt.IsWebbingMe && pt.IsTarget))
                    return currentTarget;

                // Get the closest webbing priority target frigate
                EntityCache webbingtarget = PriorityTargets.OrderBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault(pt => pt.Distance < distance && pt.IsWebbingMe && pt.IsNPCFrigate && pt.IsTarget); //frigates
                if (webbingtarget != null && !Cache.Instance.IgnoreTargets.Contains(webbingtarget.Name.Trim()))
                    return webbingtarget;

                // Get the closest webbing priority target cruiser
                webbingtarget = PriorityTargets.OrderBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault(pt => pt.Distance < distance && pt.IsWebbingMe && pt.IsNPCCruiser && pt.IsTarget); //cruisers
                if (webbingtarget != null && !Cache.Instance.IgnoreTargets.Contains(webbingtarget.Name.Trim()))
                    return webbingtarget;

                // Get the closest webbing priority target (anything else)
                webbingtarget = PriorityTargets.OrderBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault(pt => pt.Distance < distance && pt.IsWebbingMe && pt.IsTarget); //everything else
                if (webbingtarget != null && !Cache.Instance.IgnoreTargets.Contains(webbingtarget.Name.Trim()))
                    return webbingtarget;
            }

            // Is our current target any other priority target?
            if (currentTarget != null && !Cache.Instance.IgnoreTargets.Contains(currentTarget.Name.Trim()) && PriorityTargets.Any(pt => pt.Id == currentTarget.Id))
                return currentTarget;

            bool currentTargetHealthLogNow = true;
            if (Settings.Instance.DetailedCurrentTargetHealthLogging)
            {
                if (currentTarget != null && (int)currentTarget.Id != (int)TargetingCache.CurrentTargetID)
                    if ((int)currentTarget.ArmorPct == 0 && (int)currentTarget.ShieldPct == 0 && (int)currentTarget.StructurePct == 0)
                    {
                        //assume that any NPC with no shields, armor or hull is dead or does not yet have valid data associated with it
                    }
                    else
                    {
                        //
                        // assign shields and armor to targetingcache variables - compare them to each other
                        // to see if we need to send another log message to the console, if the values have not changed no need to log it.
                        //
                        if ((int)currentTarget.ShieldPct >= TargetingCache.CurrentTargetShieldPct ||
                            (int)currentTarget.ArmorPct >= TargetingCache.CurrentTargetArmorPct ||
                            (int)currentTarget.StructurePct >= TargetingCache.CurrentTargetStructurePct)
                        {
                            currentTargetHealthLogNow = false;
                        }
                        //
                        // now that we are done comparing - assign new values for this tick
                        //
                        TargetingCache.CurrentTargetShieldPct = (int)currentTarget.ShieldPct;
                        TargetingCache.CurrentTargetArmorPct = (int)currentTarget.ArmorPct;
                        TargetingCache.CurrentTargetStructurePct = (int)currentTarget.StructurePct;
                        if (currentTargetHealthLogNow)
                        {
                            Logging.Log(callingroutine, ".GetBestTarget: CurrentTarget is [" + currentTarget.Name +                              //name
                                        "][" + (Math.Round(currentTarget.Distance / 1000, 0)).ToString(CultureInfo.InvariantCulture) +           //distance
                                        "k][Shield%:[" + Math.Round(currentTarget.ShieldPct * 100, 0).ToString(CultureInfo.InvariantCulture) +   //shields
                                        "][Armor%:[" + Math.Round(currentTarget.ArmorPct * 100, 0).ToString(CultureInfo.InvariantCulture) + "]" //armor
                                        , Logging.white);
                        }
                    }
            }
            // Is our current target already in armor? keep shooting the same target if so...
            if (currentTarget != null && currentTarget.ArmorPct * 100 < 60 && !Cache.Instance.IgnoreTargets.Contains(currentTarget.Name.Trim()))
            {
                //Logging.Log(callingroutine + ".GetBestTarget: CurrentTarget has less than 60% armor, keep killing this target");
                return currentTarget;
            }

            // Get the closest priority target
            EntityCache prioritytarget = PriorityTargets.OrderBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault(pt => pt.Distance < distance && pt.IsTarget);
            if (prioritytarget != null && !Cache.Instance.IgnoreTargets.Contains(prioritytarget.Name.Trim()))
                return prioritytarget;

            // Do we have a target?
            if (currentTarget != null)
                return currentTarget;

            // Get all entity targets
            IEnumerable<EntityCache> targets = Targets.Where(e => e.CategoryId == (int)CategoryID.Entity && e.IsNpc && !e.IsContainer && e.GroupId != (int)Group.LargeCollidableStructure).ToList();

            EWarEffectsOnMe(); //updates data that is displayed in the Questor GUI (and possibly used elsewhere later)

            // Get the closest high value target
            EntityCache highValueTarget = targets.Where(t => t.TargetValue.HasValue && t.Distance < distance).OrderByDescending(t => t.TargetValue != null ? t.TargetValue.Value : 0).ThenBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault();
            // Get the closest low value target
            EntityCache lowValueTarget = targets.Where(t => !t.TargetValue.HasValue && t.Distance < distance).OrderBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault();

            //if (Settings.Instance.DontShootFrigatesWithSiegeorAutoCannons && (lowValueTarget != null)) //this defaults to false and needs to be changed in your characters settings xml file if you want to enable this option
            //{
            //    if (Settings.Instance.WeaponGroupId == 55 || Settings.Instance.WeaponGroupId == 508 || Settings.Instance.WeaponGroupId == 506)
            //    {
            //        if (lowValueTarget.Distance <= (int)Distance.InsideThisRangeIsLIkelyToBeMostlyFrigates && !lowValueTarget.TargetValue.HasValue && lowValueTarget.GroupId != (int)Group.LargeCollidableStructure)
            //        {
            //           //we really need a reliable way to determine if a particular NPC is a particular size ship, database of typeIDs or grouIDs maybe?
            //        }
            //    }
            //}

            if (lowValueFirst && lowValueTarget != null)
                return lowValueTarget;
            if (!lowValueFirst && highValueTarget != null)
                return highValueTarget;

            // Return either one or the other
            return lowValueTarget ?? highValueTarget;
        }

        private void EWarEffectsOnMe()
        {
            // Get all entity targets
            IEnumerable<EntityCache> targets = Targets.Where(e => e.CategoryId == (int)CategoryID.Entity && e.IsNpc && !e.IsContainer && e.GroupId != (int)Group.LargeCollidableStructure).ToList();

            //
            //Start of Current EWar Effects On Me (below)
            //
            //Dampening
            TargetingCache.EntitiesDampeningMe = targets.Where(e => e.IsSensorDampeningMe).ToList();
            TargetingCache.EntitiesDampeningMe_text = String.Empty;
            foreach (EntityCache entityDampeningMe in TargetingCache.EntitiesDampeningMe)
            {
                TargetingCache.EntitiesDampeningMe_text = TargetingCache.EntitiesDampeningMe_text + " [" +
                                                          entityDampeningMe.Name + "][" +
                                                          Math.Round(entityDampeningMe.Distance / 1000, 0) +
                                                          "k] , ";
            }

            //Neutralizing
            TargetingCache.EntitiesNeutralizingMe = targets.Where(e => e.IsNeutralizingMe).ToList();
            TargetingCache.EntitiesNeutralizingMe_text = String.Empty;
            foreach (EntityCache entityNeutralizingMe in TargetingCache.EntitiesNeutralizingMe)
            {
                TargetingCache.EntitiesNeutralizingMe_text = TargetingCache.EntitiesNeutralizingMe_text + " [" +
                                                             entityNeutralizingMe.Name + "][" +
                                                             Math.Round(entityNeutralizingMe.Distance / 1000, 0) +
                                                             "k] , ";
            }

            //TargetPainting
            TargetingCache.EntitiesTargetPatingingMe = targets.Where(e => e.IsTargetPaintingMe).ToList();
            TargetingCache.EntitiesTargetPaintingMe_text = String.Empty;
            foreach (EntityCache entityTargetpaintingMe in TargetingCache.EntitiesTargetPatingingMe)
            {
                TargetingCache.EntitiesTargetPaintingMe_text = TargetingCache.EntitiesTargetPaintingMe_text + " [" +
                                                               entityTargetpaintingMe.Name + "][" +
                                                               Math.Round(entityTargetpaintingMe.Distance / 1000, 0) +
                                                               "k] , ";
            }

            //TrackingDisrupting
            TargetingCache.EntitiesTrackingDisruptingMe = targets.Where(e => e.IsTrackingDisruptingMe).ToList();
            TargetingCache.EntitiesTrackingDisruptingMe_text = String.Empty;
            foreach (EntityCache entityTrackingDisruptingMe in TargetingCache.EntitiesTrackingDisruptingMe)
            {
                TargetingCache.EntitiesTrackingDisruptingMe_text = TargetingCache.EntitiesTrackingDisruptingMe_text +
                                                                   " [" + entityTrackingDisruptingMe.Name + "][" +
                                                                   Math.Round(entityTrackingDisruptingMe.Distance / 1000, 0) +
                                                                   "k] , ";
            }

            //Jamming (ECM)
            TargetingCache.EntitiesJammingMe = targets.Where(e => e.IsJammingMe).ToList();
            TargetingCache.EntitiesJammingMe_text = String.Empty;
            foreach (EntityCache entityJammingMe in TargetingCache.EntitiesJammingMe)
            {
                TargetingCache.EntitiesJammingMe_text = TargetingCache.EntitiesJammingMe_text + " [" +
                                                        entityJammingMe.Name + "][" +
                                                        Math.Round(entityJammingMe.Distance / 1000, 0) +
                                                        "k] , ";
            }

            //Warp Disrupting (and warp scrambling)
            TargetingCache.EntitiesWarpDisruptingMe = targets.Where(e => e.IsWarpScramblingMe).ToList();
            TargetingCache.EntitiesWarpDisruptingMe_text = String.Empty;
            foreach (EntityCache entityWarpDisruptingMe in TargetingCache.EntitiesWarpDisruptingMe)
            {
                TargetingCache.EntitiesWarpDisruptingMe_text = TargetingCache.EntitiesWarpDisruptingMe_text + " [" +
                                                               entityWarpDisruptingMe.Name + "][" +
                                                               Math.Round(entityWarpDisruptingMe.Distance / 1000, 0) +
                                                               "k] , ";
            }

            //Webbing
            TargetingCache.EntitiesWebbingMe = targets.Where(e => e.IsWebbingMe).ToList();
            TargetingCache.EntitiesWebbingMe_text = String.Empty;
            foreach (EntityCache entityWebbingMe in TargetingCache.EntitiesWebbingMe)
            {
                TargetingCache.EntitiesWebbingMe_text = TargetingCache.EntitiesWebbingMe_text + " [" +
                                                        entityWebbingMe.Name + "][" +
                                                        Math.Round(entityWebbingMe.Distance / 1000, 0) +
                                                        "k] , ";
            }
            //
            //End of Current EWar Effects On Me (above)
            //
        }

        public int RandomNumber(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max);
        }

        public DirectContainer ItemHangar { get; set; }

        public bool OpenItemsHangarSingleInstance(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                DirectContainerWindow lootHangarWindow =
                    (DirectContainerWindow)
                    Cache.Instance.DirectEve.Windows.OfType<DirectWindow>().FirstOrDefault(
                        w => w.Type == "form.Inventory" && w.Caption.Contains("Item hangar"));
                // Is the items hangar open?
                if (lootHangarWindow == null)
                {
                    // No, command it to open
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                    Cache.Instance.NextOpenHangarAction =
                        DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                    Logging.Log(module, "Opening Item Hangar: waiting [" +
                                        Math.Round(
                                            Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                                        "sec]", Logging.white);
                    return false;
                }
                else
                {
                    Cache.Instance.ItemHangar = Cache.Instance.DirectEve.GetContainer(lootHangarWindow.currInvIdItem);
                    return true;
                }
            }
            return false;
        }

        public bool OpenItemsHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangar", "We are in Station", Logging.teal);
                Cache.Instance.ItemHangar = Cache.Instance.DirectEve.GetItemHangar();

                if (Cache.Instance.ItemHangar == null)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar was null", Logging.teal);
                    return false;
                }
                if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar exists", Logging.teal);

                // Is the items hangar open?
                if (Cache.Instance.ItemHangar.Window == null)
                {
                    // No, command it to open
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                    Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 4));
                    Logging.Log(module, "Opening Item Hangar: waiting [" +
                                Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                                "sec]", Logging.white);
                    return false;
                }

                if (!Cache.Instance.ItemHangar.Window.IsReady)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar.window is not yet ready", Logging.teal);
                    return false;
                }
                if (Cache.Instance.ItemHangar.Window.IsReady)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar.window ready", Logging.teal);
                    //if (Cache.Instance.ItemHangar.Window.IsPrimary())
                    //{
                    //    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar.window is primary, opening as secondary", Logging.teal);
                    //    Cache.Instance.ItemHangar.Window.OpenAsSecondary();
                    //    return false;
                    //}
                    return true;
                }
            }
            return false;
        }

        public bool OpenItemsHangarAsLootHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsLootHangar", "We are in Station", Logging.teal);
                Cache.Instance.LootHangar = Cache.Instance.DirectEve.GetItemHangar();

                if (Cache.Instance.LootHangar == null)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsLootHangar", "LootHangar was null", Logging.teal);
                    return false;
                }
                if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsLootHangar", "LootHangar exists", Logging.teal);

                // Is the items hangar open?
                if (Cache.Instance.LootHangar.Window == null)
                {
                    // No, command it to open
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                    Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 4));
                    Logging.Log(module, "Opening Item Hangar: waiting [" +
                            Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                            "sec]", Logging.white);
                    return false;
                }
                if (!Cache.Instance.LootHangar.Window.IsReady)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsLootHangar", "LootHangar.window is not yet ready", Logging.teal);
                    return false;
                }
                if (Cache.Instance.LootHangar.Window.IsReady)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsLootHangar", "LootHangar.window ready", Logging.teal);
                    if (Cache.Instance.LootHangar.Window.IsPrimary())
                    {
                        if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsLootHangar", "LootHangar.window is primary, opening as secondary", Logging.teal);
                        Cache.Instance.LootHangar.Window.OpenAsSecondary();
                        return false;
                    }
                    if (Cache.Instance.LootHangar.Window.Type.Contains("form.InventorySecondary"))
                    {
                        if (Settings.Instance.DebugHangars) Logging.Log("Questor", "LootHangar.Window is a secondary inventory window", Logging.teal);
                        return true;
                    }

                    if (Settings.Instance.DebugHangars)
                    {
                        Logging.Log("Questor", "-----LootHangar.Window-----", Logging.orange);
                        Logging.Log("Questor", "Cache.Instance.LootHangar.Window.Name: [" + Cache.Instance.LootHangar.Window.Name + "]", Logging.white);
                        Logging.Log("Questor", "Cache.Instance.LootHangar.Window.Caption: [" + Cache.Instance.LootHangar.Window.Caption + "]", Logging.white);
                        Logging.Log("Questor", "Cache.Instance.LootHangar.Window.Type: [" + Cache.Instance.LootHangar.Window.Type + "]", Logging.white);
                        Logging.Log("Questor", "Cache.Instance.LootHangar.Window.IsModal: [" + Cache.Instance.LootHangar.Window.IsModal + "]", Logging.white);
                        Logging.Log("Questor", "Cache.Instance.LootHangar.Window.IsDialog: [" + Cache.Instance.LootHangar.Window.IsDialog + "]", Logging.white);
                        Logging.Log("Questor", "Cache.Instance.LootHangar.Window.Id: [" + Cache.Instance.LootHangar.Window.Id + "]", Logging.white);
                        Logging.Log("Questor", "Cache.Instance.LootHangar.Window.IsKillable: [" + Cache.Instance.LootHangar.Window.IsKillable + "]", Logging.white);
                    }
                    return false;
                }
            }
            return false;
        }

        public bool OpenItemsHangarAsAmmoHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;

            if (Cache.Instance.InStation)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsAmmoHangar", "We are in Station", Logging.teal);
                Cache.Instance.AmmoHangar = Cache.Instance.DirectEve.GetItemHangar();

                if (Cache.Instance.AmmoHangar == null)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsAmmoHangar", "AmmoHangar was null", Logging.teal);
                    return false;
                }
                if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsAmmoHangar", "AmmoHangar exists", Logging.teal);

                // Is the items hangar open?
                if (Cache.Instance.AmmoHangar.Window == null)
                {
                    // No, command it to open
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                    Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 4));
                    Logging.Log(module, "Opening Item Hangar: waiting [" +
                                Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                                "sec]", Logging.white);
                    return false;
                }
                if (!Cache.Instance.AmmoHangar.Window.IsReady)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsAmmoHangar", "AmmoHangar.window is not yet ready", Logging.teal);
                    return false;
                }
                if (Cache.Instance.AmmoHangar.Window.IsReady)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsAmmoHangar", "AmmoHangar.window ready", Logging.teal);
                    if (Cache.Instance.AmmoHangar.Window.IsPrimary())
                    {
                        if (Settings.Instance.DebugHangars) Logging.Log("OpenItemsHangarAsAmmoHangar", "AmmoHangar.window is primary, opening as secondary", Logging.teal);
                        Cache.Instance.AmmoHangar.Window.OpenAsSecondary();
                        return false;
                    }
                    return true;
                }
                return false;
            }
            return false;
        }

        public bool StackItemsHangarAsLootHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                if (!Cache.Instance.OpenItemsHangarAsLootHangar("Cache.StackItemsHangar")) return false;
                if (Settings.Instance.DebugHangars) Logging.Log("StackItemsHangarAsLootHangar", "OpenItemsHangarAsLootHangar returned true", Logging.teal);
                if (Cache.Instance.LootHangar != null && Cache.Instance.LootHangar.Window.IsReady)
                {
                Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                Logging.Log(module, "Stacking Item Hangar: waiting [" +
                            Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                            "sec]", Logging.white);
                    Cache.Instance.LootHangar.StackAll();
                    return true;
                }
                return false;
            }
            return false;
        }

        public bool StackItemsHangarAsAmmoHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                if (!Cache.Instance.OpenItemsHangarAsAmmoHangar("Cache.StackItemsHangar")) return false;
                if (Cache.Instance.AmmoHangar != null && Cache.Instance.AmmoHangar.Window.IsReady)
                {
                    Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                    Logging.Log(module, "Stacking Item Hangar: waiting [" +
                            Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                            "sec]", Logging.white);
                    Cache.Instance.AmmoHangar.StackAll();
                    return true;
                }
                return false;
            }
            return false;
        }

        public DirectContainer CargoHold { get; set; }

        public bool OpenCargoHold(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenCargoAction)
            {
                if (DateTime.Now.Subtract(Cache.Instance.NextOpenCargoAction).TotalSeconds > 0)
                {
                    Logging.Log(module, "Opening CargoHold: waiting [" +
                                Math.Round(Cache.Instance.NextOpenCargoAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                                "sec]", Logging.white);
                }
                return false;
            }

            Cache.Instance.CargoHold = Cache.Instance.DirectEve.GetShipsCargo();
            if (Cache.Instance.InStation || Cache.Instance.InSpace) //do we need to special case pods here?
            {
                if (Cache.Instance.CargoHold.Window == null)
                {
                    // No, command it to open
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
                    Cache.Instance.NextOpenCargoAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                    Logging.Log(module, "Opening Cargohold of active ship: waiting [" +
                                Math.Round(Cache.Instance.NextOpenCargoAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                                "sec]", Logging.white);
                    return false;
                }

                if (!Cache.Instance.CargoHold.Window.IsReady)
                {
                    Logging.Log(module, "cargo window is not ready", Logging.white);
                    return false;
                }

                if (Cache.Instance.CargoHold.Window.IsPrimary())
                {
                    Logging.Log(module, "Opening cargo window as secondary", Logging.white);
                    Cache.Instance.CargoHold.Window.OpenAsSecondary();
                    //Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
                    Cache.Instance.NextOpenCargoAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                    return true; // just assume everything went well. the window is already open and is ready anyway
                }
                return true;
            }
            return false;
        }

        public bool StackCargoHold(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenCargoAction)
                return false;

            if (!Cache.Instance.OpenCargoHold("Cache.StackCargoHold")) return false;
            Logging.Log(module, "Stacking CargoHold: waiting [" +
                        Math.Round(Cache.Instance.NextOpenCargoAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                        "sec]", Logging.white);
            if (Cache.Instance.CargoHold != null && Cache.Instance.CargoHold.Window.IsReady)
            {
                Cache.Instance.CargoHold.StackAll();
                return true;
            }
            return false;
        }

        public DirectContainer ShipHangar { get; set; }

        public bool OpenShipsHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
            {
                return false;
            }

            if (Cache.Instance.InStation)
            {
                Cache.Instance.ShipHangar = Cache.Instance.DirectEve.GetShipHangar();
                if (Cache.Instance.ShipHangar == null)
                {
                    Cache.Instance.NextOpenHangarAction = DateTime.Now.AddMilliseconds(500);
                    return false;
                }

                // Is the ship hangar open?
                if (Cache.Instance.ShipHangar.Window == null)
                {
                    // No, command it to open
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenShipHangar);
                    Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                    Logging.Log(module, "Opening Ship Hangar: waiting [" +
                                Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds,
                                           0) + "sec]", Logging.white);
                    return false;
                }
                if (!Cache.Instance.ShipHangar.Window.IsReady)
                {
                    Cache.Instance.NextOpenHangarAction = DateTime.Now.AddMilliseconds(500);
                    return false;
                }
                if (Cache.Instance.ShipHangar.Window.IsReady)
                {
                    if (Cache.Instance.ShipHangar.Window.IsPrimary())
                    {
                        Logging.Log(module, "Opening Ship Hangar as secondary", Logging.white);
                        Cache.Instance.ShipHangar.Window.OpenAsSecondary();
                        //Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenShipHangar);
                        Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool StackShipsHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;

            if (Cache.Instance.InStation)
            {
                if (!Cache.Instance.OpenShipsHangar("Cache.StackShipsHangar")) return false;
                Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                Logging.Log(module, "Stacking Ship Hangar: waiting [" +
                                Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds,
                                           0) + "sec]", Logging.white);
                if (Cache.Instance.ShipHangar != null && Cache.Instance.ShipHangar.Window.IsReady)
                {
                    Cache.Instance.ShipHangar.StackAll();
                    return true;
                }
                return false;
            }
            return false;
        }

        //public DirectContainer CorpAmmoHangar { get; set; }
        public bool OpenCorpAmmoHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("OpenCorpAmmoHangar", "Debug: if (Cache.Instance.InStation)", Logging.teal);
                if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenCorpAmmoHangar", "Debug: if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))", Logging.teal);
                
                    Cache.Instance.AmmoHangar = Cache.Instance.DirectEve.GetCorporationHangar(Settings.Instance.AmmoHangar);

                    if (Cache.Instance.AmmoHangar != null)
                    {
                        DirectContainerWindow ammoHangarWindow = (DirectContainerWindow)Cache.Instance.DirectEve.Windows.OfType<DirectWindow>().FirstOrDefault(w => w.Type == "form.InventorySecondary" && w.Caption == Settings.Instance.AmmoHangar);
                        if (Settings.Instance.DebugHangars) Logging.Log("OpenCorpAmmoHangar", "Debug: if (Cache.Instance.AmmoHangar != null)", Logging.teal);

                        if (ammoHangarWindow == null)
                        {
                            if (Settings.Instance.DebugHangars) Logging.Log("OpenCorpAmmoHangar", "Debug: if (ammoHangarWindow == null)", Logging.teal);
                            long ammoHangarID = Cache.Instance.DirectEve.GetCorpHangarId(Settings.Instance.AmmoHangar) - 1;
                            if (!Cache.Instance.OpenAndSelectInvItem("Cache.OpenCorpAmmoHangar", ammoHangarID)) return false;

                            if (Settings.Instance.DebugHangars) Logging.Log("OpenCorpAmmoHangar", "Debug: AmmoHangar must already be open", Logging.teal);
                            if (Cache.Instance.InventoryWindow != null)
                            {
                                if (Cache.Instance.InventoryWindow.OpenAsSecondary())
                                    Logging.Log(module, "OpenCorpAmmoHangar: Open Secondary Window", Logging.white);

                                Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber(3,5));
                                return false;
                            }
                        }

                        if (ammoHangarWindow != null)
                        {
                            if (Settings.Instance.DebugHangars) Logging.Log("OpenCorpAmmoHangar", "Debug: if (ammoHangarWindow != null)", Logging.teal);
                            
                            if (!ammoHangarWindow.IsReady)
                            {
                                Logging.Log(module, "OpenCorpAmmoHangar: Window is not ready", Logging.white);
                                return false;
                            }
                            if (ammoHangarWindow.IsReady)
                            {
                                Logging.Log(module, "OpenCorpAmmoHangar: Window is ready", Logging.white);
                                return true;
                            }
                        }
                    }
                    if (Cache.Instance.AmmoHangar == null)
                    {
                        if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))
                            Logging.Log(module, "Opening Corporate Ammo Hangar: failed! No Corporate Hangar in this station! lag?", Logging.orange);
                        return false;
                    }
                }
                else
                {
                    Cache.Instance.AmmoHangar = null;
                    return true;
                }
            }
            return false;
        }

        public bool StackCorpAmmoHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))
                {
                    if (!Cache.Instance.OpenCorpAmmoHangar("Cache.StackCorpAmmoHangar")) return false;
                    
                    if (Cache.Instance.AmmoHangar.Window.IsReady)
                    {
                        Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                        Logging.Log(module, "Stacking Corporate Ammo Hangar: waiting [" +
                                Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds,
                                           0) + "sec]", Logging.white);
                        Cache.Instance.AmmoHangar.StackAll();
                        return true;
                    }
                    return false;
                }
                else
                {
                    Cache.Instance.AmmoHangar = null;
                    return true;
                }
            }
            return false;
        }

        //public DirectContainer CorpLootHangar { get; set; }
        public DirectContainerWindow InventoryWindow { get; set; }

        public bool OpenInventoryWindow(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            Cache.Instance.InventoryWindow = (DirectContainerWindow)Cache.Instance.DirectEve.Windows.OfType<DirectWindow>().FirstOrDefault(
                            w => w.Type == "form.Inventory" &&
                   w.Name == "('Inventory', None)");

            if (Cache.Instance.InventoryWindow == null)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("debug", "Cache.Instance.InventoryWindow is null, opening InventoryWindow", Logging.teal);
                // No, command it to open
                Cache.Instance.DirectEve.OpenInventory();
                Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(2, 3));
                Logging.Log(module, "Opening Inventory Window: waiting [" + Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds, 0) + "sec]", Logging.white);
                return false;
            }
            if (Cache.Instance.InventoryWindow != null)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("debug", "Cache.Instance.InventoryWindow exists", Logging.teal);
                if (Cache.Instance.InventoryWindow.IsReady)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("debug", "Cache.Instance.InventoryWindow exists and is ready", Logging.teal);
                    return true;
                }
                //
                // if the InventoryWindow "hangs" and is never ready we will hang... it would be better if we set a timer
                // and closed the inventorywindow that is not ready after 10-20seconds. (can we close a window that is in a state if !window.isready?)
                //
                return false;
            }
            return false;
        }

        public bool OpenCorpLootHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.LootHangar))
                {
                    Cache.Instance.LootHangar = Cache.Instance.DirectEve.GetCorporationHangar(Settings.Instance.LootHangar);

                    if (Cache.Instance.LootHangar != null)
                    {
                        DirectContainerWindow lootHangarWindow = (DirectContainerWindow)Cache.Instance.DirectEve.Windows.OfType<DirectWindow>().FirstOrDefault(w => w.Type == "form.InventorySecondary" && w.Caption == Settings.Instance.LootHangar);

                        if (lootHangarWindow == null)
                        {
                            long lootHangarID = Cache.Instance.DirectEve.GetCorpHangarId(Settings.Instance.LootHangar) - 1;
                            if (!Cache.Instance.OpenAndSelectInvItem("Cache.OpenCorpAmmoHangar", lootHangarID)) return false;

                            if (Cache.Instance.InventoryWindow != null)
                            {
                                if (Cache.Instance.InventoryWindow.OpenAsSecondary())
                                    Logging.Log(module, "OpenCorpLootHangar: Open Secondary Window", Logging.white);

                                Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                                return false;
                            }
                        }

                        if (lootHangarWindow != null)
                        {
                            if (!lootHangarWindow.IsReady)
                            {
                                Logging.Log(module, "OpenCorpLootHangar: Window is not ready", Logging.white);
                                return false;
                            }
                            if (lootHangarWindow.IsReady)
                            {
                                Logging.Log(module, "OpenCorpLootHangar: Window is ready", Logging.white);
                                return true;
                            }
                        }
                    }
                    if (Cache.Instance.LootHangar == null)
                    {
                        if (!string.IsNullOrEmpty(Settings.Instance.LootHangar))
                            Logging.Log(module, "Opening Corporate Loot Hangar: failed! No Corporate Hangar in this station! lag?", Logging.orange);
                        return false;
                    }
                }
                else
                {
                    Cache.Instance.LootHangar = null;
                    return true;
                }
            }
            return false;
        }

        public bool StackCorpLootHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.LootHangar))
                {
                    if (!Cache.Instance.OpenCorpLootHangar("Cache.StackCorpLootHangar")) return false;

                    if (Cache.Instance.LootHangar.Window.IsReady)
                    {
                        Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                        Logging.Log(module, "Stacking Corporate Loot Hangar: waiting [" +
                                    Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds,
                                               0) + "sec]", Logging.white);
                        Cache.Instance.LootHangar.StackAll();
                        return true;
                    }
                    return false;
                }
                else
                {
                    Cache.Instance.LootHangar = null;
                    return true;
                }
            }
            return false;
        }

        public DirectContainer CorpBookmarkHangar { get; set; }

        public bool OpenCorpBookmarkHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenCorpBookmarkHangarAction)
                return false;
            if (Cache.Instance.InStation)
            {
                Cache.Instance.CorpBookmarkHangar = !string.IsNullOrEmpty(Settings.Instance.BookmarkHangar)
                                      ? Cache.Instance.DirectEve.GetCorporationHangar(Settings.Instance.BookmarkHangar)
                                      : null;
                // Is the corpHangar open?
                if (Cache.Instance.CorpBookmarkHangar != null)
                {
                    if (Cache.Instance.CorpBookmarkHangar.Window == null)
                    {
                        // No, command it to open
                        //Cache.Instance.DirectEve.OpenCorporationHangar();
                        Cache.Instance.NextOpenCorpBookmarkHangarAction =
                            DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                        Logging.Log(module, "Opening Corporate Bookmark Hangar: waiting [" +
                                    Math.Round(
                                        Cache.Instance.NextOpenCorpBookmarkHangarAction.Subtract(DateTime.Now).TotalSeconds,
                                        0) + "sec]", Logging.white);
                        return false;
                    }
                    if (!Cache.Instance.CorpBookmarkHangar.Window.IsReady)
                        return false;
                    if (Cache.Instance.CorpBookmarkHangar.Window.IsReady)
                    {
                        if (Cache.Instance.CorpBookmarkHangar.Window.IsPrimary())
                        {
                            Cache.Instance.CorpBookmarkHangar.Window.OpenAsSecondary();
                            return false;
                        }
                        return true;
                    }
                }
                if (Cache.Instance.CorpBookmarkHangar == null)
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.BookmarkHangar))
                        Logging.Log(module, "Opening Corporate Bookmark Hangar: failed! No Corporate Hangar in this station! lag?", Logging.orange);
                    return false;
                }
            }
            return false;
        }

        public bool CloseCorpHangar(String module, String window)
        {
            if (Cache.Instance.InStation && !String.IsNullOrEmpty(window))
            {
                DirectContainerWindow CorpHangarWindow = (DirectContainerWindow)Cache.Instance.DirectEve.Windows.OfType<DirectWindow>().FirstOrDefault(w => w.Type == "form.InventorySecondary" && w.Caption == window);

                if (CorpHangarWindow != null)
                {
                    Logging.Log(module, "Closing Corp Window: " + window, Logging.teal);
                    CorpHangarWindow.Close();
                    return false;
                }
                return true;
            }
            return true;
        }

        //public DirectContainer LootContainer { get; set; }

        public bool OpenLootContainer(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenLootContainerAction)
                return false;
            if (Cache.Instance.InStation)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))
                {
                    if (!Cache.Instance.OpenItemsHangar("Cache.OpenLootContainer")) return false;

                    DirectItem firstlootcontainer = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.GivenName != null && i.IsSingleton && i.GroupId == (int)Group.FreightContainer && i.GivenName.ToLower() == Settings.Instance.LootContainer.ToLower());
                    if (firstlootcontainer != null)
                    {
                        long lootContainerID = firstlootcontainer.ItemId;
                        Cache.Instance.LootHangar = Cache.Instance.DirectEve.GetContainer(lootContainerID);

                        DirectContainerWindow lootHangarWindow = (DirectContainerWindow)Cache.Instance.DirectEve.Windows.OfType<DirectWindow>().FirstOrDefault(w => w.Type == "form.InventorySecondary" && w.Caption == Settings.Instance.LootContainer);

                        if (lootHangarWindow == null)
                        {
                            if (!Cache.Instance.OpenAndSelectInvItem("Cache.OpenLootContainer", lootContainerID)) return false;

                            if (Cache.Instance.InventoryWindow != null)
                            {
                                if (Cache.Instance.InventoryWindow.OpenAsSecondary())
                                    Logging.Log(module, "OpenLootContainer: Open Secondary Window", Logging.white);

                                Cache.Instance.NextOpenLootContainerAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                                return false;
                            }
                        }

                        if (lootHangarWindow != null)
                        {
                            if (!lootHangarWindow.IsReady)
                            {
                                Logging.Log(module, "OpenLootContainer: Window is not ready", Logging.white);
                                return false;
                            }
                            if (lootHangarWindow.IsReady)
                            {
                                Logging.Log(module, "OpenLootContainer: Window is ready", Logging.white);
                                return true;
                            }
                        }
                    }
                    else
                    {
                        Logging.Log(module, "unable to find LootContainer named [ " + Settings.Instance.LootContainer.ToLower() + " ]", Logging.orange);
                        var firstothercontainer = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.GivenName != null && i.IsSingleton && i.GroupId == (int)Group.FreightContainer);
                        if (firstothercontainer != null)
                        {
                            if (!string.IsNullOrEmpty(Settings.Instance.BookmarkHangar))
                                Logging.Log(module, "we did however find a container named [ " + firstothercontainer.GivenName + " ]", Logging.orange);
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool OpenAndSelectInvItem(string module, long ID)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                if (Settings.Instance.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.teal);
                return false;
            }
            if (DateTime.Now < NextOpenHangarAction)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: if (DateTime.Now < NextOpenHangarAction)", Logging.teal);
                return false;
            }

            if (Settings.Instance.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: about to: if (!Cache.Instance.OpenInventoryWindow", Logging.teal);

            if (!Cache.Instance.OpenInventoryWindow("Cache.OpenAndSelectInvItem")) return false;

            if (ID < 0)
            {
                Logging.Log("OpenAndSelectInvItem", "Inventory item ID from tree cannot be less than 0, retrying", Logging.white);
                return false;
            }

            if (!Cache.Instance.InventoryWindow.GetIdsFromTree(false).Contains(ID))
            {
                if (Settings.Instance.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: if (!Cache.Instance.InventoryWindow.GetIdsFromTree(false).Contains(ID))", Logging.teal);

                if (ID >= 0 && ID <= 6 && Cache.Instance.InventoryWindow.ExpandCorpHangarView())
                {
                    Logging.Log(module, "ExpandCorpHangar executed", Logging.teal);
                    Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(4);
                    return false;
                }

                Logging.Log(module, "Error: can't find inventory item in the tree. Found the following ids:", Logging.red);
                List<long> idsintree = Cache.Instance.InventoryWindow.GetIdsFromTree(false);

                foreach (Int64 itemintree in idsintree)
                {
                    Logging.Log(module, "ID: " + itemintree, Logging.red);
                }
                Logging.Log(module, "Was looking for: " + ID, Logging.red);

                return false;
            }
            else //inventory contains the id we want to select
            {
                if (Settings.Instance.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: else -- inventory contains the id we want to select", Logging.teal);

                if (Cache.Instance.InventoryWindow.currInvIdItem != ID)
                {
                    if (Settings.Instance.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: if (Cache.Instance.InventoryWindow.currInvIdItem != ID)", Logging.teal);

                    if (Settings.Instance.DebugUnloadLoot)
                    {
                        Logging.Log(module, "--- Start Debug Logging --- for ID# [" + ID + "]", Logging.teal);

                        List<long> idsintree = Cache.Instance.InventoryWindow.GetIdsFromTree(false);
                        foreach (Int64 itemintree in idsintree)
                        {
                            Logging.Log(module, "ID: " + itemintree, Logging.red);
                        }
                        Logging.Log(module, "Was looking for: " + ID, Logging.red);
                        Logging.Log(module, "--- Stop Debug Logging ---" + ID, Logging.teal);

                        Logging.Log(module, "SelectHangarID: " + ID, Logging.teal);
                    }
                    Cache.Instance.InventoryWindow.SelectTreeEntryByID(ID);
                    return true; //was false
                }
                return true;
            }
        }

        public bool OpenAndSelectInvItemByName(string module, string hangarName)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (Cache.Instance.DirectEve.GetCorpHangarId(hangarName) > -1) //should likely be changed to 0 once the DE corp hangar ID bug is resolved
            {
                if (!Cache.Instance.OpenInventoryWindow("Cache.OpenAndSelectInvItemByName")) return false;

                    if (Cache.Instance.InventoryWindow != null & Cache.Instance.InventoryWindow.IsReady)
                {
                    if (!Cache.Instance.InventoryWindow.Name.Contains(hangarName))
                    {
                        Logging.Log("OpenAndSelectInvItemByName", "InventoryTree Entry named [" + hangarName + "] is being selected", Logging.white);
                        if (Settings.Instance.DebugHangars)
                        {
                            Logging.Log("OpenAndSelectInvItemByName", "--------------------------------------------------", Logging.orange);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.Name: [" + Cache.Instance.InventoryWindow.Name + "]", Logging.white);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.Caption: [" + Cache.Instance.InventoryWindow.Caption + "]", Logging.white);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.Type: [" + Cache.Instance.InventoryWindow.Type + "]", Logging.white);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.IsModal: [" + Cache.Instance.InventoryWindow.IsModal + "]", Logging.white);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.IsDialog: [" + Cache.Instance.InventoryWindow.IsDialog + "]", Logging.white);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.Id: [" + Cache.Instance.InventoryWindow.Id + "]", Logging.white);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.IsKillable: [" + Cache.Instance.InventoryWindow.IsKillable + "]", Logging.white);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.IsReady: [" + Cache.Instance.InventoryWindow.IsReady + "]", Logging.white);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.LocationFlag: [" + Cache.Instance.InventoryWindow.LocationFlag + "]", Logging.white);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.currInvIdName: " + Cache.Instance.InventoryWindow.currInvIdName, Logging.red);
                            Logging.Log("OpenAndSelectInvItemByName", "InventoryWindow.currInvIdName: " + Cache.Instance.InventoryWindow.currInvIdItem, Logging.red);
                            
                            List<long> idsintree = Cache.Instance.InventoryWindow.GetIdsFromTree(false);
                            foreach (Int64 itemintree in idsintree)
                            {
                                Cache.Instance.InventoryWindow.SelectTreeEntryByID(itemintree);
                            }
                        }
                        //Cache.Instance.InventoryWindow.SelectTreeEntryByName(HangarName); - this needs to be fixed is currently using the divionname and needs to use the window name?
                        Cache.Instance.NextOpenLootContainerAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(2, 4));
                        Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(2, 4));
                        Cache.Instance.NextOpenCargoAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(2, 4));
                        return true;
                    }
                    return true;
                }
                return false;
            }
            Logging.Log("OpenAndSelectInvItemByName", "Inventory Item Selection named [" + hangarName + "] was not found! Check your settings", Logging.white);
            return false;
        }

        public bool StackLootContainer(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenLootContainerAction)
                return false;

            if (Cache.Instance.InStation)
            {
                if (!Cache.Instance.OpenLootContainer("Cache.StackLootContainer")) return false;
                Cache.Instance.NextOpenLootContainerAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                if (LootHangar.Window == null)
                {
                    var firstlootcontainer = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.GivenName != null && i.IsSingleton && i.GroupId == (int)Group.FreightContainer && i.GivenName.ToLower() == Settings.Instance.LootContainer.ToLower());
                    if (firstlootcontainer != null)
                    {
                        long lootContainerID = firstlootcontainer.ItemId;
                        if (!OpenAndSelectInvItem(module, lootContainerID))
                            return false;
                    }
                    else return false;
                }
                if (LootHangar.Window == null || !LootHangar.Window.IsReady) return false;

                Logging.Log(module, "Loot Container window named: [ " + LootHangar.Window.Name + " ] was found and its contents are being stacked", Logging.white);
                LootHangar.StackAll();
                Cache.Instance.NextOpenLootContainerAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                return true;
            }
            return false;
        }

        public DirectContainer LootHangar { get; set; }

        public bool CloseLootHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;

            if (Cache.Instance.InStation)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.LootHangar))
                {
                    Cache.Instance.LootHangar = Cache.Instance.DirectEve.GetCorporationHangar(Settings.Instance.LootHangar);

                    // Is the corp loot Hangar open?
                    if (Cache.Instance.LootHangar != null)
                    {
                        if (Cache.Instance.LootHangar.Window != null)
                        {
                            // if open command it to close
                            Cache.Instance.LootHangar.Window.Close();
                            Cache.Instance.NextOpenHangarAction =
                                DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                            Logging.Log(module, "Closing Corporate Loot Hangar: waiting [" +
                                        Math.Round(
                                            Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).
                                                TotalSeconds,
                                            0) + "sec]", Logging.white);
                            return false;
                        }
                    }
                    if (Cache.Instance.LootHangar == null)
                    {
                        if (!string.IsNullOrEmpty(Settings.Instance.LootHangar))
                            Logging.Log(module, "Closing Corporate Hangar: failed! No Corporate Hangar in this station! lag?", Logging.orange);
                        return false;
                    }
                }
                else if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))
                {
                    if (!Cache.Instance.OpenItemsHangarAsLootHangar("Cache.OpenLootContainer")) return false;

                    var firstlootcontainer = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.GivenName != null && i.GivenName.ToLower() == Settings.Instance.LootContainer.ToLower());
                    if (firstlootcontainer != null)
                    {
                        long lootContainerID = firstlootcontainer.ItemId;
                        Cache.Instance.LootHangar = Cache.Instance.DirectEve.GetContainer(lootContainerID);
                        Cache.Instance.NextOpenLootContainerAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                    }
                    else
                    {
                        Logging.Log(module, "unable to find LootContainer named [ " + Settings.Instance.LootContainer.ToLower() + " ]", Logging.orange);
                        var firstothercontainer = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.GivenName != null);
                        if (firstothercontainer != null)
                        {
                            if (!string.IsNullOrEmpty(Settings.Instance.BookmarkHangar))
                                Logging.Log(module, "we did however find a container named [ " + firstothercontainer.GivenName + " ]", Logging.white);
                            return false;
                        }
                    }
                }
                else //use local items hangar
                {
                    Cache.Instance.LootHangar = Cache.Instance.DirectEve.GetItemHangar();
                    if (Cache.Instance.LootHangar == null)
                        return false;

                    // Is the items hangar open?
                    if (Cache.Instance.LootHangar.Window != null)
                    {
                        // if open command it to close
                        Cache.Instance.LootHangar.Window.Close();
                        Cache.Instance.NextOpenHangarAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 4));
                        Logging.Log(module, "Closing Item Hangar: waiting [" +
                                    Math.Round(Cache.Instance.NextOpenHangarAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                                    "sec]", Logging.white);
                        return false;
                    }
                }
            }
            return false;
        }

        public bool OpenLootHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;

            if (Cache.Instance.InStation)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.LootHangar)) // Corporate hangar = LootHangar
                {
                    if (!Cache.Instance.OpenCorpLootHangar("Cache.OpenCorpLootHangar")) return false;
                    return true;
                }
                else if (!string.IsNullOrEmpty(Settings.Instance.LootContainer)) // Freight Container in my local items hangar = LootHangar
                {
                    //if (!Cache.Instance.OpenItemsHangarAsLootHangar("Cache.OpenLItemsHangar")) return false;
                    if (!Cache.Instance.OpenLootContainer("Cache.OpenLootContainer")) return false;
                    return true;
                }
                else // local items hangar = LootHangar
                {
                    if (!Cache.Instance.OpenItemsHangarAsLootHangar("Cache.OpenItemsHangar")) return false;
                    return true;
                }
            }
            return false;
        }

        public bool StackLootHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;

            if (Cache.Instance.InStation)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.LootHangar))
                {
                    if (!Cache.Instance.StackCorpLootHangar("Cache.StackLootHangar")) return false;
                    return true;
                }
                else if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))
                {
                    if (!Cache.Instance.StackLootContainer("Cache.StackLootHangar")) return false;
                    return true;
                }
                else //use local items hangar
                {
                    if (!Cache.Instance.StackItemsHangarAsLootHangar("Cache.StackLootHangar")) return false;
                    return true;
                }
            }
            return false;
        }

        public DirectContainer AmmoHangar { get; set; }

        public bool OpenAmmoHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;

            if (Cache.Instance.InStation)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))
                {
                    if (Settings.Instance.DebugHangars) Logging.Log(module, "using Corporate hangar as Ammo hangar", Logging.white);
                    if (!Cache.Instance.OpenCorpAmmoHangar("Cache.OpenAmmoHangar")) return false;
                }
                else
                {
                    if (Settings.Instance.DebugHangars) Logging.Log(module, "using Local items hangar as Ammo hangar", Logging.white);
                    if (!Cache.Instance.OpenItemsHangarAsAmmoHangar("Cache.OpenAmmoHangar")) return false;
                }
                return true;
            }
            return false;
        }

        public bool StackAmmoHangar(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
                return false;

            if (Cache.Instance.InStation)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))
                {
                    if (!Cache.Instance.StackCorpAmmoHangar("Cache.StackCorpAmmoHangar")) return false;
                    return true;
                }
                else //use local items hangar
                {
                    if (!Cache.Instance.StackItemsHangarAsAmmoHangar("Cache.StackAmmoHangar")) return false;
                    return true;
                }
            }
            return false;
        }

        public DirectContainer DroneBay { get; set; }
        //{
        //    get { return _dronebay ?? (_dronebay = Cache.Instance.DirectEve.GetShipsDroneBay()); }
        //}

        public bool OpenDroneBay(String module)
        {
            if (DateTime.Now < Cache.Instance.NextDroneBayAction)
            {
                //Logging.Log(module + ": Opening Drone Bay: waiting [" + Math.Round(Cache.Instance.NextOpenDroneBayAction.Subtract(DateTime.Now).TotalSeconds, 0) + "sec]",Logging.white);
                return false;
            }
            if ((!Cache.Instance.InSpace && !Cache.Instance.InStation))
            {
                Logging.Log(module, "Opening Drone Bay: We aren't in station or space?!", Logging.orange);
                return false;
            }
            //if(Cache.Instance.DirectEve.ActiveShip.Entity == null || Cache.Instance.DirectEve.ActiveShip.GroupId == 31)
            //{
            //    Logging.Log(module + ": Opening Drone Bay: we are in a shuttle or not in a ship at all!");
            //    return false;
            //}
            if (Cache.Instance.InStation || Cache.Instance.InSpace)
            {
                Cache.Instance.DroneBay = Cache.Instance.DirectEve.GetShipsDroneBay();
            }
            else return false;

            if (Cache.Instance.DroneBay == null)
            {
                Cache.Instance.NextDroneBayAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                Logging.Log(module, "Opening Drone Bay: --- waiting [" +
                                Math.Round(Cache.Instance.NextDroneBayAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                                "sec]", Logging.white);

                return false;
            }
            // Is the drone bay open?
            if (Cache.Instance.DroneBay.Window == null)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("cache", "DroneBay window is null at the moment", Logging.white);
                Cache.Instance.NextDroneBayAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                // No, command it to open
                Logging.Log(module, "Opening Drone Bay: waiting [" +
                            Math.Round(Cache.Instance.NextDroneBayAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                            "sec]", Logging.white);
                Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenDroneBayOfActiveShip);
                return false;
            }
            if (!Cache.Instance.DroneBay.Window.IsReady)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("cache", "DroneBay window is not ready yet", Logging.white);
                return false;
            }

            if (Cache.Instance.DroneBay.Window.IsReady)
            {
                if (Settings.Instance.DebugHangars) Logging.Log("cache", "DroneBay window is ready", Logging.white);
                //if (Cache.Instance.DroneBay.Window.IsPrimary())
                //{
                //    Logging.Log("cache", "DroneBay window name is [" + Cache.Instance.DroneBay.Window.Name.ToLower() + "]", Logging.white);
                //    Logging.Log("cache", "DroneBay currInvIdName is [" + Cache.Instance.DroneBay.Window.currInvIdName + "]", Logging.white);
                //    Logging.Log("cache", "DroneBay currInvIdItem is [" + Cache.Instance.DroneBay.Window.currInvIdItem + "]", Logging.white);
                //    Cache.Instance.DroneBay.Window.OpenAsSecondary();
                //    Logging.Log("cache", "DroneBay window name is [" + Cache.Instance.DroneBay.Window.Name.ToLower() + "]", Logging.white);
                //    Logging.Log("cache", "DroneBay currInvIdName is [" + Cache.Instance.DroneBay.Window.currInvIdName + "]", Logging.white);
                //    Logging.Log("cache", "DroneBay currInvIdItem is [" + Cache.Instance.DroneBay.Window.currInvIdItem + "]", Logging.white);
                //
                //
                //    return false;
                //}
                return true;
            }
            Logging.Log("cache", "DroneBay is not ready but made it past the return above?!? how?", Logging.white);
            return false;
        }

        public bool CloseDroneBay(String module)
        {
            if (DateTime.Now < Cache.Instance.NextDroneBayAction)
            {
                //Logging.Log(module + ": Closing Drone Bay: waiting [" + Math.Round(Cache.Instance.NextOpenDroneBayAction.Subtract(DateTime.Now).TotalSeconds, 0) + "sec]",Logging.white);
                return false;
            }
            if ((!Cache.Instance.InSpace && !Cache.Instance.InStation))
            {
                Logging.Log(module, "Closing Drone Bay: We aren't in station or space?!", Logging.orange);
                return false;
            }
            if (Cache.Instance.InStation || Cache.Instance.InSpace)
            {
                Cache.Instance.DroneBay = Cache.Instance.DirectEve.GetShipsDroneBay();
            }
            else return false;

            // Is the drone bay open? if so, close it
            if (Cache.Instance.DroneBay.Window != null)
            {
                Cache.Instance.NextDroneBayAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                Logging.Log(module, "Closing Drone Bay: waiting [" +
                            Math.Round(Cache.Instance.NextDroneBayAction.Subtract(DateTime.Now).TotalSeconds, 0) +
                            "sec]", Logging.white);
                Cache.Instance.DroneBay.Window.Close();
                return true;
            }
            return true;
        }

        public DirectLoyaltyPointStoreWindow LPStore { get; set; }

        public bool OpenLPStore(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
            {
                //Logging.Log(module + ": Opening Drone Bay: waiting [" + Math.Round(Cache.Instance.NextOpenDroneBayAction.Subtract(DateTime.Now).TotalSeconds, 0) + "sec]",Logging.white);
                return false;
            }
            if (!Cache.Instance.InStation)
            {
                Logging.Log(module, "Opening LP Store: We aren't in station?! There is no LP Store in space, waiting...", Logging.orange);
                return false;
            }
            if (Cache.Instance.InStation)
            {
                Cache.Instance.LPStore = Cache.Instance.DirectEve.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
                if (Cache.Instance.LPStore == null)
                {
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenLpstore);
                    Logging.Log(module, "Opening loyalty point store", Logging.white);
                    return false;
                }
                return true;
            }
            return false;
        }

        public bool CloseLPStore(String module)
        {
            if (DateTime.Now < Cache.Instance.NextOpenHangarAction)
            {
                return false;
            }
            if (!Cache.Instance.InStation)
            {
                Logging.Log(module, "Closing LP Store: We aren't in station?!", Logging.orange);
                return false;
            }
            if (Cache.Instance.InStation)
            {
                Cache.Instance.LPStore = Cache.Instance.DirectEve.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
                if (Cache.Instance.LPStore != null)
                {
                    Logging.Log(module, "Closing loyalty point store", Logging.white);
                    Cache.Instance.LPStore.Close();
                    return false;
                }
                return true;
            }
            return true; //if we aren't in station then the LP Store should have auto closed already.
        }

        public DirectWindow JournalWindow { get; set; }

        public bool OpenJournalWindow(String module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.Now < Cache.Instance.NextOpenJournalWindowAction)
                return false;

            if (Cache.Instance.InStation)
            {
                Cache.Instance.JournalWindow = Cache.Instance.GetWindowByName("journal");

                // Is the journal window open?
                if (Cache.Instance.JournalWindow == null)
                {
                    // No, command it to open
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenJournal);
                    Cache.Instance.NextOpenJournalWindowAction = DateTime.Now.AddSeconds(2 + Cache.Instance.RandomNumber(10, 20));
                    Logging.Log(module, "Opening Journal Window: waiting [" +
                                Math.Round(Cache.Instance.NextOpenJournalWindowAction.Subtract(DateTime.Now).TotalSeconds,
                                           0) + "sec]", Logging.white);
                    return false;
                }
                return true; //if JournalWindow is not null then the window must be open.
            }
            return false;
        }

        public DirectContainer ContainerInSpace { get; set; }

        public bool OpenContainerInSpace(String module, EntityCache containerToOpen)
        {
            if (DateTime.Now < Cache.Instance.NextLootAction)
                return false;

            if (Cache.Instance.InSpace && containerToOpen.Distance <= (int)Distance.ScoopRange)
            {
                Cache.Instance.ContainerInSpace = Cache.Instance.DirectEve.GetContainer(containerToOpen.Id);

                if (Cache.Instance.ContainerInSpace != null)
                {
                    if (Cache.Instance.ContainerInSpace.Window == null)
                    {
                        containerToOpen.OpenCargo();
                        Cache.Instance.NextLootAction = DateTime.Now.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
                        Logging.Log(module, "Opening Container: waiting [" + Math.Round(Cache.Instance.NextLootAction.Subtract(DateTime.Now).TotalSeconds, 0) + " sec]", Logging.white);
                        return false;
                    }

                    if (!Cache.Instance.ContainerInSpace.Window.IsReady)
                    {
                        Logging.Log(module, "Container window is not ready", Logging.white);
                        return false;
                    }

                    if (Cache.Instance.ContainerInSpace.Window.IsPrimary())
                    {
                        Logging.Log(module, "Opening Container window as secondary", Logging.white);
                        Cache.Instance.ContainerInSpace.Window.OpenAsSecondary();
                        Cache.Instance.NextLootAction = DateTime.Now.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
                        return true;
                    }
                }
                return true;
            }
            Logging.Log(module, "Not in space or not in scoop range", Logging.orange);
            return true;
        }

        public List<DirectBookmark> AfterMissionSalvageBookmarks
        {
            get
            {
                if (Settings.Instance.CharacterMode.ToLower() == "Salvage".ToLower())
                {
                    return
                        Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Where(
                            e => e.CreatedOn != null && e.CreatedOn.Value.CompareTo(AgedDate) < 0).ToList(); 
                }
                else
                {
                    return
                        Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").ToList();
                }

            }
        }

        //Represents date when bookmarks are eligble for salvage. This should not be confused with when the bookmarks are too old to salvage.
        public DateTime AgedDate
        {
            get
            {
                return DateTime.UtcNow.AddMinutes(-Settings.Instance.AgeofBookmarksForSalvageBehavior);
            }
        }

        public DirectBookmark GetSalvagingBookmark
        {
            get
            {
                //Delete bookmarks older than 2 hours.
                DateTime bmExpirationDate = DateTime.UtcNow.AddMinutes(-Settings.Instance.AgeofSalvageBookmarksToExpire);
                List<DirectBookmark> List_oldBktoDelete = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Where(e => e.CreatedOn != null && e.CreatedOn.Value.CompareTo(bmExpirationDate) < 0).ToList();
                foreach (DirectBookmark oldBktoDelete in List_oldBktoDelete)
                {
                    Logging.Log("CombatMissionsBehavior.BeginAftermissionSalvaging", "Remove old Bookmark: " + oldBktoDelete.Title + " BookmarExpirationDate: " + bmExpirationDate, Logging.teal);
                    oldBktoDelete.Delete();
                }

                if (Settings.Instance.FirstSalvageBookmarksInSystem)
                {
                    Logging.Log("CombatMissionsBehavior.BeginAftermissionSalvaging", "Salvaging at first bookmark from system", Logging.white);
                    return Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").OrderBy(b => b.CreatedOn).FirstOrDefault(c => c.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId);
                }
                else
                {
                    Logging.Log("CombatMissionsBehavior.BeginAftermissionSalvaging", "Salvaging at first oldest bookmarks", Logging.white);
                    return Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").OrderBy(b => b.CreatedOn).FirstOrDefault();

                }
            }
        }
        public bool GateInGrid()
        {
            var gates = Cache.Instance.Entities.Where(a => a.GroupId == (int)Group.AccellerationGate);
            if (gates == null || !gates.Any())
                return false;
            else return true;
        }
        private int _bookmarkdeletionattempt = 0;
        public DateTime _nextBookmarkDeletionAttempt = DateTime.Now;
        public bool DeleteBookmarksOnGrid(string module)
        {
            if (DateTime.Now < _nextBookmarkDeletionAttempt)
            {
                return false;
            }
            _nextBookmarkDeletionAttempt = DateTime.Now.AddSeconds(5 + Settings.Instance.RandomNumber(1,5));

            //
            // remove all salvage bookmarks over 48hrs old - they have long since been rendered useless
            //
            try
            {
                var uselessSalvageBookmarks = new List<DirectBookmark>(AfterMissionSalvageBookmarks.Where(b => b.CreatedOn < DateTime.Now.AddDays(-2)).OrderByDescending(b => b.CreatedOn));

                DirectBookmark uselessSalvageBookmark = uselessSalvageBookmarks.FirstOrDefault();
                if (uselessSalvageBookmark != null)
                {
                    _bookmarkdeletionattempt++;
                    if (_bookmarkdeletionattempt <= 5)
                    {
                        Logging.Log(module, "removing salvage bookmark that aged more than 48 hours (is their a dedicated or aftermissions salvager cleaning these up?):" + uselessSalvageBookmark.Title, Logging.white);
                        uselessSalvageBookmark.Delete();
                    }
                    if (_bookmarkdeletionattempt > 5)
                    {
                        Logging.Log(module, "error removing bookmark!" + uselessSalvageBookmark.Title, Logging.white);
                        _States.CurrentQuestorState = QuestorState.Error;
                    }
                    return false;
                } 
            }
            catch (Exception ex)
            {
                Logging.Log("Cache.DeleteBookmarksOnGrid", "Delete old unprocessed salvage bookmarks: exception generated:" + ex, Logging.white);
            }
            

            var bookmarksinlocal = new List<DirectBookmark>(AfterMissionSalvageBookmarks.Where(b => b.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId).
                                                                   OrderBy(b => b.CreatedOn));

            DirectBookmark onGridBookmark = bookmarksinlocal.FirstOrDefault(b => Cache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distance.OnGridWithMe);
            if (onGridBookmark != null)
            {
                _bookmarkdeletionattempt++;
                if (_bookmarkdeletionattempt <= 5)
                {
                    Logging.Log(module, "removing salvage bookmark:" + onGridBookmark.Title, Logging.white);
                    onGridBookmark.Delete();
                }
                if (_bookmarkdeletionattempt > 5)
                {
                    Logging.Log(module, "error removing bookmark!" + onGridBookmark.Title, Logging.white);
                    _States.CurrentQuestorState = QuestorState.Error;
                }
                return false;
            }
            else
            {
                _bookmarkdeletionattempt = 0;
                Cache.Instance.NextSalvageTrip = DateTime.Now;
                Statistics.Instance.FinishedSalvaging = DateTime.Now;
                _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.CheckBookmarkAge;
                return true;
            }
        }

        public bool RepairItems(string module)
        {
            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(5) && !Cache.Instance.InSpace || DateTime.Now < _nextRepairItemsAction) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                Logging.Log(module, "Waiting...", Logging.orange);
                return false;
            }

            _nextRepairItemsAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber(2, 4));

            if (Cache.Instance.InStation && !Cache.Instance.DirectEve.hasRepairFacility())
            {
                Logging.Log(module, "This station does not have repair facilities to use! aborting attempt to use non-existant repair facility.", Logging.orange);
                return true;
            }

            if (Cache.Instance.InStation)
            {
                //Cache.Instance.DirectEve.OpenRepairShop();
                DirectRepairShopWindow repairWindow = Cache.Instance.Windows.OfType<DirectRepairShopWindow>().FirstOrDefault();

                if (repairWindow == null)
                {
                    Logging.Log(module, "Opening repairshop window", Logging.white);
                    Cache.Instance.DirectEve.OpenRepairShop();

                    _nextRepairItemsAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber(2, 4));
                    return false;
                }
                else
                {
                    if (!Cache.Instance.OpenShipsHangar(module)) return false;
                    if (!Cache.Instance.OpenItemsHangar(module)) return false;

                    List<DirectItem> items = Cache.Instance.ShipHangar.Items;
                    //items.AddRange(Cache.Instance.ItemHangar.Items);

                    if (items.Any())
                    {
                        if (String.IsNullOrEmpty(repairWindow.AvgDamage()))
                        {
                            Logging.Log(module, "Add items to repair list", Logging.white);
//                          foreach (DirectItem item in items)
//                          {
//                              Logging.Log(module, "Items: " + item.TypeName, Logging.white);
//                          }
                            repairWindow.RepairItems(items);

                            _nextRepairItemsAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber(2, 4));
                            return false;
                        }
                        else
                        {
                            Logging.Log(module, "Repairing Items", Logging.white);
                            repairWindow.RepairAll();
                        }
                    }
                    else
                        Logging.Log(module, "No items are damaged, nothing to repair.", Logging.orange);

                    return true;
                }
            }
            Logging.Log(module, "Not in station.", Logging.orange);
            return false;
        }


    }
}