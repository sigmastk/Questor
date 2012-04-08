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
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using System.Xml.XPath;
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
        ///   Approaching cache
        /// </summary>
        //private int? _approachingId;
        private EntityCache _approaching;

        /// <summary>
        ///   Returns all non-empty wrecks and all containers
        /// </summary>
        private List<EntityCache> _containers;

        /// <summary>
        ///   Entities cache (all entities within 256km)
        /// </summary>
        private List<EntityCache> _entities;

        /// <summary>
        ///   Entities by Id
        /// </summary>
        private Dictionary<long, EntityCache> _entitiesById;

        /// <summary>
        ///   Module cache
        /// </summary>
        private List<ModuleCache> _modules;

        /// <summary>
        ///   Priority targets (e.g. warp scramblers or mission kill targets)
        /// </summary>
        private List<PriorityTarget> _priorityTargets;

        /// <summary>
        ///   Star cache
        /// </summary>
        private EntityCache _star;

        /// <summary>
        ///   Station cache
        /// </summary>
        private List<EntityCache> _stations;

        /// <summary>
        ///   Stargate cache
        /// </summary>
        private List<EntityCache> _stargates;

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
        ///   Returns all unlooted wrecks & containers
        /// </summary>
        private List<EntityCache> _unlootedContainers;

        private List<EntityCache> _unlootedWrecksAndSecureCans;
        
        private List<DirectWindow> _windows;

        public Cache()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ShipTargetValues = new List<ShipTargetValue>();
            XDocument values = XDocument.Load(Path.Combine(path, "ShipTargetValues.xml"));
            foreach (XElement value in values.Root.Elements("ship"))
                ShipTargetValues.Add(new ShipTargetValue(value));

            InvTypesById = new Dictionary<int, InvType>();
            XDocument invTypes = XDocument.Load(Path.Combine(path, "InvTypes.xml"));
            foreach (XElement element in invTypes.Root.Elements("invtype"))
                InvTypesById.Add((int) element.Attribute("id"), new InvType(element));

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

        public bool DoNotBreakInvul = false;
        public bool UseDrones = true;

        public bool LootAlreadyUnloaded = false;

        public bool MissionLoot = false;

        public bool SalvageAll = false;

        public double Wealth { get; set; }
        public bool OpenWrecks = false;
        public bool NormalApproch = true;
        public bool CourierMission = false;
        public string MissionName = "";
        public int MissionsThisSession = 0;
        public bool ConsoleLogOpened = false;
        public int TimeSpentReloading_seconds = 0;
        public int TimeSpentInMission_seconds = 0;
        public int TimeSpentInMissionInRange = 0;
        public int TimeSpentInMissionOutOfRange = 0;
        public DirectAgentMission Mission;
        public bool DroneStatsWritten { get; set; }
        public bool DronesKillHighValueTargets { get; set; }
        public bool InMission { get; set; }

        public bool LocalSafe(int maxBad, double stand)
        {
            int number = 0;
            var local = (DirectChatWindow)GetWindowByName("Local");
            foreach(DirectCharacter localMember in local.Members)
            {
                float[] alliance = {DirectEve.Standings.GetPersonalRelationship(localMember.AllianceId), DirectEve.Standings.GetCorporationRelationship(localMember.AllianceId), DirectEve.Standings.GetAllianceRelationship(localMember.AllianceId)};
                float[] corporation = {DirectEve.Standings.GetPersonalRelationship(localMember.CorporationId), DirectEve.Standings.GetCorporationRelationship(localMember.CorporationId), DirectEve.Standings.GetAllianceRelationship(localMember.CorporationId)};
                float[] personal = {DirectEve.Standings.GetPersonalRelationship(localMember.CharacterId), DirectEve.Standings.GetCorporationRelationship(localMember.CharacterId), DirectEve.Standings.GetAllianceRelationship(localMember.CharacterId)};

                if(alliance.Min() <= stand || corporation.Min() <= stand || personal.Min() <= stand)
                {
                    Logging.Log("Cache.WatchLocal: Bad Standing Pilot Detected: [ " + localMember.Name + "] " + " [ " + number + " ] so far... of [ " + maxBad + " ] allowed");
                    number++;
                }
                if(number > maxBad)
                {
                    Logging.Log("Cache.WatchLocal: [" + number + "] Bad Standing pilots in local, We should stay in station");
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

		
		/// <summary>
        ///   Returns the maximum weapon distance
        /// </summary>
        public int WeaponRange
        {
            get
            {
                // Get ammmo based on current damage type
                IEnumerable<Ammo> ammo = Settings.Instance.Ammo.Where(a => a.DamageType == DamageType);

                // Is our ship's cargo available?
                DirectContainer cargo = DirectEve.GetShipsCargo();
                if (cargo.IsReady)
                    ammo = ammo.Where(a => cargo.Items.Any(i => a.TypeId == i.TypeId && i.Quantity >= Settings.Instance.MinimumAmmoCharges));

                // Return ship range if there's no ammo left
                if (!ammo.Any())
                    return System.Convert.ToInt32(Cache.Instance.DirectEve.ActiveShip.MaxTargetRange);

                // Return max range
                return ammo.Max(a => a.Range);
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
        public bool IsAgentLoop { get; set; }
        private string _agentName = "";

        public DateTime _lastDefence;
        public DateTime _lastModuleActivation;
        public DateTime _lastLoggingAction = DateTime.MinValue;
        public DateTime _nextTargetAction = DateTime.MinValue;
        public DateTime _nextWeaponAction = DateTime.MinValue;
        public DateTime _nextWebAction = DateTime.MinValue;
        public DateTime _nextNosAction = DateTime.MinValue;
        public DateTime _nextPainterAction = DateTime.MinValue;
        public DateTime _nextActivateAction = DateTime.Now;
        public DateTime _nextApproachAction = DateTime.Now;
        public DateTime _nextBookmarkPocketAttempt = DateTime.Now;
        public DateTime _nextAlign = DateTime.Now;
        public DateTime _nextOrbit = DateTime.Now;
        public DateTime _nextReload = DateTime.Now;
        public DateTime _nextUndockAction = DateTime.Now;
        
        public DateTime _nextDock;
        public DateTime _nextDroneRecall;
        public DateTime _lastLocalWatchAction;
        public DateTime _lastWalletCheck;
        public DateTime _nextWarpTo;
        public DateTime _lastupdateofSessionRunningTime;
        public DateTime _nextInSpaceorInStation;
        public DateTime _lastTimeCheckAction;

        public int PanicAttemptsThisMission { get; set; }
        public double LowestShieldPercentageThisPocket { get; set; }
        public double LowestArmorPercentageThisPocket { get; set; }
        public double LowestCapacitorPercentageThisPocket { get; set; }
        public int RepairCycleTimeThisPocket { get; set; }
        public int PanicAttemptsThisPocket { get; set; }
        public double LowestShieldPercentageThisMission { get; set; }
        public double LowestArmorPercentageThisMission { get; set; }
        public double LowestCapacitorPercentageThisMission { get; set; }
        public DateTime StartedBoosting { get; set; }
        public int RepairCycleTimeThisMission { get; set; }
        public DateTime LastKnownGoodConnectedTime { get; set; }
        public long TotalMegaBytesOfMemoryUsed { get; set; }
        public double MyWalletBalance { get; set; }
        public string CurrentPocketAction { get; set; }
        public string CurrentAgent
        {
            get
            {
                if(_agentName == "")
                {
                    _agentName = SwitchAgent;
                    Logging.Log("Cache.CurrentAgent is null set first agent: " + CurrentAgent);
                }

                return _agentName;
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
                if(agent == null)
                {
                    agent = Settings.Instance.AgentsList.OrderBy(j => j.Priorit).FirstOrDefault();
                    IsAgentLoop = true; //this literally means we have no agents available at the moment (decline timer likely)
                }
                else
                    IsAgentLoop = false; //this literally means we DO have agents available (at least one agents decline timer has expired and is clear to use)

                return agent.Name;
            }
        }

        public long AgentId
        {
            get
            {
                _agent = DirectEve.GetAgentByName(CurrentAgent);
                _agentId = _agent.AgentId;

                return _agentId ?? -1;
            }
        }

        public DirectAgent Agent
        {
            get
            {
                _agent = DirectEve.GetAgentByName(CurrentAgent);
                _agentId = _agent.AgentId;

                if (_agent == null)
                    _agent = DirectEve.GetAgentById(_agentId.Value);

                return _agent;
            }
        }

        public IEnumerable<ModuleCache> Modules
        {
            get
            {
                if (_modules == null)
                    _modules = DirectEve.Modules.Select(m => new ModuleCache(m)).ToList();

                return _modules;
            }
        }

        public IEnumerable<ModuleCache> Weapons
        {
            get
            { 
                if(Cache.Instance.MissionWeaponGroupId != 0)
                    return Modules.Where(m => m.GroupId == Cache.Instance.MissionWeaponGroupId); 
                else return Modules.Where(m => m.GroupId == Settings.Instance.WeaponGroupId); 
            }
        }

        public IEnumerable<EntityCache> Containers
        {
            get
            {
                if (_containers == null)
                    _containers = Entities.Where(e => e.IsContainer && e.HaveLootRights && (e.GroupId != (int)Group.Wreck || !e.IsWreckEmpty) && (e.Name != (String)"Abandoned Container")).ToList();

                return _containers;
            }
        }

        public IEnumerable<EntityCache> Wrecks
        {
            get
            {
                if (_containers == null)
                    _containers = Entities.Where(e => (e.GroupId != (int)Group.Wreck)).ToList();

                return _containers;
            }
        }

        public IEnumerable<EntityCache> UnlootedContainers
        {
            get
            {
                if (_unlootedContainers == null)
                    _unlootedContainers = Entities.Where(e => e.IsContainer && e.HaveLootRights && (!LootedContainers.Contains(e.Id) || e.GroupId == (int) Group.Wreck)).OrderBy(e => e.Distance).ToList();

                return _unlootedContainers;
            }
        }

        //This needs to include items you can steal from (thus gain aggro)
        public IEnumerable<EntityCache> UnlootedWrecksAndSecureCans
        {
            get
            {
                if (_unlootedWrecksAndSecureCans == null)
                    _unlootedWrecksAndSecureCans = Entities.Where(e => (e.GroupId == (int)Group.Wreck || e.GroupId == (int)Group.SecureContainer || e.GroupId == (int)Group.AuditLogSecureContainer || e.GroupId == (int)Group.FreightContainer) && !e.IsWreckEmpty).OrderBy(e => e.Distance).ToList();

                return _unlootedWrecksAndSecureCans;
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
            get
            {
                if (_targeting == null)
                    _targeting = Entities.Where(e => e.IsTargeting).ToList();

                return _targeting;
            }
        }

        public IEnumerable<EntityCache> TargetedBy
        {
            get
            {
                if (_targetedBy == null)
                    _targetedBy = Entities.Where(e => e.IsTargetedBy).ToList();

                return _targetedBy;
            }
        }

        public IEnumerable<EntityCache> Entities
        {
            get
            {
                if (!InSpace)
                    return new List<EntityCache>();

                if (_entities == null)
                    _entities = DirectEve.Entities.Select(e => new EntityCache(e)).Where(e => e.IsValid).ToList();

                return _entities;
            }
        }

        public bool InSpace
        {
            get { return DirectEve.Session.IsInSpace && !DirectEve.Session.IsInStation && DirectEve.Session.IsReady && DirectEve.ActiveShip.Entity != null; }
        }

        public bool InStation
        {
            get { return DirectEve.Session.IsInStation && !DirectEve.Session.IsInSpace && DirectEve.Session.IsReady; }
        }

        public bool InWarp
        {
            get { return DirectEve.ActiveShip.Entity != null ? DirectEve.ActiveShip.Entity.Mode == 3 : false; }
        }

        public IEnumerable<EntityCache> ActiveDrones
        {
            get
            {
                if (_activeDrones == null)
                    _activeDrones = DirectEve.ActiveDrones.Select(d => new EntityCache(d)).ToList();

                return _activeDrones;
            }
        }

        public IEnumerable<EntityCache> Stations
        {
            get
            {
                if (_stations == null)
                    _stations = Entities.Where(e => e.CategoryId == (int) CategoryID.Station).ToList();

                return _stations;
            }
        }

        public IEnumerable<EntityCache> Stargates
        {
            get
            {
                if (_stargates == null)
                    _stargates = Entities.Where(e => e.GroupId == (int)Group.Stargate).ToList();

                return _stargates;
            }
        }

        public EntityCache Star
        {
            get
            {
                if (_star == null)
                    _star = Entities.Where(e => e.CategoryId == (int) CategoryID.Celestial && e.GroupId == (int) Group.Star).FirstOrDefault();

                return _star;
            }
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
                if (_windows == null)
                    _windows = DirectEve.Windows;

                return _windows;
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
        public int MissionWeaponGroupId = 0;
        public bool? MissionUseDrones;
        public bool StopTimeSpecified { get; set; }
        public DateTime StopTime { get; set; }
        public DateTime StartTime { get; set; }
        public int MaxRuntime { get; set; }
        public bool CloseQuestorCMDLogoff = false;
        public bool CloseQuestorCMDExitGame = true;
        public bool GotoBaseNow = false;
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
                return null;

            // Did we accept this mission?
            if (missionforbookmarkinfo.State != (int) MissionState.Accepted || missionforbookmarkinfo.AgentId != agentId)
                return null;

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
            return DirectEve.Bookmarks.Where(b => !string.IsNullOrEmpty(b.Title) && b.Title.StartsWith(label)).ToList();
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
        public IEnumerable<Action> LoadMissionActions(long agentId, int pocketId, bool missionMode)
        {
            DirectAgentMission missiondetails = GetAgentMission(agentId);
            if(missiondetails == null && missionMode)
                return new Action[0];

            if (missiondetails != null)
            {
                string missionName = FilterPath(missiondetails.Name);
                string missionXmlPath = Path.Combine(Settings.Instance.MissionsPath, missionName + ".xml");
                if (!File.Exists(missionXmlPath))
                {
                    //No mission file but we need to set some cache settings
                    OrbitDistance = Settings.Instance.OrbitDistance;
                    AfterMissionSalvaging = Settings.Instance.AfterMissionSalvaging;
                    return new Action[0];
                }
                //
                // this loads the settings from each pocket... but NOT any settings global to the mission
                //
                try
                {
                    XDocument xdoc = XDocument.Load(missionXmlPath);
                    if (xdoc.Root != null)
                    {
                        XElement xElement = xdoc.Root.Element("pockets");
                        if (xElement != null)
                        {
                            IEnumerable<XElement> pockets = xElement.Elements("pocket");
                            foreach (XElement pocket in pockets)
                            {
                                if ((int) pocket.Attribute("id") != pocketId)
                                    continue;

                                if (pocket.Element("damagetype") != null)
                                    DamageType = (DamageType) Enum.Parse(typeof (DamageType), (string) pocket.Element("damagetype"), true);

                                if (pocket.Element("orbitdistance") != null) 	//Load OrbitDistance from mission.xml, if present
                                {
                        
                                    OrbitDistance = (int) pocket.Element("orbitdistance");
                                    Logging.Log(string.Format("Cache: Using Mission Orbit distance {0}",OrbitDistance));
                                }
                                else //Otherwise, use value defined in charname.xml file
                                {
                                    OrbitDistance = Settings.Instance.OrbitDistance;
                                    Logging.Log(string.Format("Cache: Using Settings Orbit distance {0}",OrbitDistance));
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
                                    Logging.Log(string.Format("Cache: Using Settings Orbit distance {0}", OrbitDistance));
                                }
                                var actions = new List<Action>();
                                XElement elements = pocket.Element("actions");
                                if (elements != null)
                                {
                                    foreach (XElement element in elements.Elements("action"))
                                    {
                                        var action = new Action();
                                        action.State = (ActionState) Enum.Parse(typeof (ActionState), (string) element.Attribute("name"), true);
                                        XAttribute xAttribute = element.Attribute("name");
                                        if (xAttribute != null && (string)xAttribute.Value == "ClearPocket")
                                        {
                                            action.AddParameter("", "");
                                        }
                                        else
                                        {
                                            foreach (XElement parameter in element.Elements("parameter"))
                                                action.AddParameter((string) parameter.Attribute("name"), (string) parameter.Attribute("value"));
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
                            return new Action[0];
                        }
                        
                    }
                    else
                    {
                        { return new Action[0]; }
                    }

                    // if we reach this code there is no mission XML file, so we set some things -- Assail

                    OrbitDistance = Settings.Instance.OrbitDistance;
                    Logging.Log(string.Format("Cache: Using Settings Orbit distance {0}", OrbitDistance));

                    return new Action[0];
                }
                catch (Exception ex)
                {
                    Logging.Log("Error loading mission XML file [" + ex.Message + "]");
                    return new Action[0];
                }
            }
            else
            {
                { return new Action[0]; }
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
                    Logging.Log("Cache: Mission: " + missionFitting.Mission + " - Faction: " + FactionName + " - Fitting: " + missionFit + " - Ship: " + missionShip + " - ChangeMissionShipFittings: " + ChangeMissionShipFittings);
                    MissionShip = missionShip;
                }
                else if (!string.IsNullOrEmpty(FactionFit)) // if no mission fittings defined, try to match by faction
                    Fitting = FactionFit;

                if (Fitting == "") // otherwise use the default
                    Fitting = DefaultFitting;
            }

            string missionName = FilterPath(missiondetailsformittionitems.Name);
            string missionXmlPath = Path.Combine(Settings.Instance.MissionsPath, missionName + ".xml");
            if (!File.Exists(missionXmlPath))
                return;

            try
            {
                XDocument xdoc = XDocument.Load(missionXmlPath);
                IEnumerable<string> items = ((IEnumerable)xdoc.XPathEvaluate("//action[(translate(@name, 'LOT', 'lot')='loot') or (translate(@name, 'LOTIEM', 'lotiem')='lootitem')]/parameter[translate(@name, 'TIEM', 'tiem')='item']/@value")).Cast<XAttribute>().Select(a => ((string)a ?? string.Empty).ToLower());
                MissionItems.AddRange(items);

               if (xdoc.Root != null) BringMissionItem = (string) xdoc.Root.Element("bring") ?? string.Empty;
               BringMissionItem = BringMissionItem.ToLower();

                //load fitting setting from the mission file
                //Fitting = (string)xdoc.Root.Element("fitting") ?? "default";  
            }
            catch (Exception ex)
            {
                Logging.Log("Error loading mission XML file [" + ex.Message + "]");
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

                _priorityTargets.Add(new PriorityTarget {EntityID = target.Id, Priority = priority});
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

            return Math.Sqrt((curX - x)*(curX - x) + (curY - y)*(curY - y) + (curZ - z)*(curZ - z));
        }

        /// <summary>
        ///   Create a bookmark
        /// </summary>
        /// <param name = "label"></param>
        public void CreateBookmark(string label)
        {
            if (Settings.Instance.CreateSalvageBookmarksIn=="Corp")
                DirectEve.CorpBookmarkCurrentLocation(label, "", null);
            else
                DirectEve.BookmarkCurrentLocation(label, "", null);
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

        /// <summary>
        ///   Return the best possible target (based on current target, distance and low value first)
        /// </summary>
        /// <param name="currentTarget"></param>
        /// <param name="distance"></param>
        /// <param name="lowValueFirst"></param>
        /// <returns></returns>
        public EntityCache GetBestTarget(EntityCache currentTarget, double distance, bool lowValueFirst)
        {
            // Do we have a 'current target' and if so, is it an actual target?
            // If not, clear current target
            if (currentTarget != null && !currentTarget.IsTarget)
                currentTarget = null;

            // Is our current target a warp scrambling priority target?
            if (currentTarget != null && PriorityTargets.Any(pt => pt.Id == currentTarget.Id && pt.IsWarpScramblingMe && pt.IsTarget))
                return currentTarget;

            // Get the closest warp scrambling priority target
            EntityCache target = PriorityTargets.OrderBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault(pt => pt.Distance < distance && pt.IsWarpScramblingMe && pt.IsTarget);
            if (target != null)
                return target;

            // Is our current target any other priority target?
            if (currentTarget != null && PriorityTargets.Any(pt => pt.Id == currentTarget.Id))
                return currentTarget;

            // Get the closest priority target
            target = PriorityTargets.OrderBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault(pt => pt.Distance < distance && pt.IsTarget);
            if (target != null)
                return target;

            // Do we have a target?
            if (currentTarget != null)
                return currentTarget;

            // Get all entity targets
            IEnumerable<EntityCache> targets = Targets.Where(e => e.CategoryId == (int)CategoryID.Entity && e.IsNpc && !e.IsContainer && e.GroupId != (int)Group.LargeCollidableStructure);

            // Get the closest high value target
            EntityCache highValueTarget = targets.Where(t => t.TargetValue.HasValue && t.Distance < distance).OrderByDescending(t => t.TargetValue != null ? t.TargetValue.Value : 0).ThenBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault();
            // Get the closest low value target
            EntityCache lowValueTarget = targets.Where(t => !t.TargetValue.HasValue && t.Distance < distance).OrderBy(OrderByLowestHealth()).ThenBy(t => t.Distance).FirstOrDefault();
            
            if (lowValueFirst && lowValueTarget != null)
                return lowValueTarget;
            if (!lowValueFirst && highValueTarget != null)
                return highValueTarget;

            // Return either one or the other
            return lowValueTarget ?? highValueTarget;
        }
    }
}