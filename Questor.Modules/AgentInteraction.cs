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
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using DirectEve;

    public class AgentInteraction
    {
        public const string RequestMission = "Request Mission";
        public const string ViewMission = "View Mission";
        public const string CompleteMission = "Complete Mission";
        public const string LocateCharacter = "Locate Character";
        public const string Accept = "Accept";
        public const string Decline = "Decline";
        public const string Close = "Close";
        private DateTime _nextJournalOpenRequest;
        private DateTime _nextAgentAction;
        //private DateTime _waitingOnAgentResponse;
        private DateTime _waitingonmissiontimer = DateTime.Now;
        private DateTime _waitingonagentwindowtimer = DateTime.Now;
        private DateTime _waitingonagentwindowtimer2 = DateTime.Now;

        public bool WaitDecline { get; set; }
        public float MinStandings { get; set; }

        public AgentInteraction()
        {
            AmmoToLoad = new List<Ammo>();
        }

        public long AgentId { get; set; }
        public DirectAgent Agent
        {
            get { return Cache.Instance.DirectEve.GetAgentById(AgentId); }
        }

        public bool ForceAccept { get; set; }
        public AgentInteractionState State { get; set; }
        public AgentInteractionPurpose Purpose { get; set; }

        public List<Ammo> AmmoToLoad { get; private set; }

        private void LoadSpecificAmmo(IEnumerable<DamageType> damageTypes)
        {
            AmmoToLoad.Clear();
            AmmoToLoad.AddRange(Settings.Instance.Ammo.Where(a => damageTypes.Contains(a.DamageType)).Select(a => a.Clone()));
        }

        private void WaitForConversation()
        {
            WaitDecline = Settings.Instance.WaitDecline;
            MinStandings = Settings.Instance.MinStandings;

            DirectAgentWindow agentWindow = Agent.Window;
            if (agentWindow == null || !agentWindow.IsReady)
                return;

            if (Purpose == AgentInteractionPurpose.AmmoCheck)
            {
                Logging.Log("AgentInteraction: Checking ammo type");
                State = AgentInteractionState.WaitForMission;
            }
            else
            {
                Logging.Log("AgentInteraction: Replying to agent");
                State = AgentInteractionState.ReplyToAgent;
                _nextAgentAction = DateTime.Now.AddSeconds(7);
            }
        }

        private void ReplyToAgent()
        {
            DirectAgentWindow agentWindow = Agent.Window;
            if (agentWindow == null || !agentWindow.IsReady)
                return;

            List<DirectAgentResponse> responses = agentWindow.AgentResponses;
            if (responses == null || responses.Count == 0)
            {
                if (DateTime.Now.Subtract(_waitingonagentwindowtimer2).TotalSeconds > 30)
                {
                    Logging.Log("AgentInteraction: ReplyToAgent: agentWindow == null : trying to close the agent window");
                    agentWindow.Close();
                    _waitingonagentwindowtimer = DateTime.Now;
                    
                }
                return;
            }

            DirectAgentResponse request = responses.FirstOrDefault(r => r.Text.Contains(RequestMission));
            DirectAgentResponse complete = responses.FirstOrDefault(r => r.Text.Contains(CompleteMission));
            DirectAgentResponse view = responses.FirstOrDefault(r => r.Text.Contains(ViewMission));
            DirectAgentResponse accept = responses.FirstOrDefault(r => r.Text.Contains(Accept));
            DirectAgentResponse decline = responses.FirstOrDefault(r => r.Text.Contains(Decline));

            if (complete != null)
            {
                if (Purpose == AgentInteractionPurpose.CompleteMission)
                {
                    // Complete the mission, close convo
                    Logging.Log("AgentInteraction: Saying [Complete Mission]");
                    complete.Say();

                    Logging.Log("AgentInteraction: Closing conversation");

                    State = AgentInteractionState.CloseConversation;
                    _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber5To10());
                }
                else
                {
                    Logging.Log("AgentInteraction: Waiting for mission");

                    // Apparently someone clicked "accept" already
                    State = AgentInteractionState.WaitForMission;
                    _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To7());
                }
            }
            else if (request != null)
            {
                if (Purpose == AgentInteractionPurpose.StartMission)
                {
                    // Request a mission and wait for it
                    Logging.Log("AgentInteraction: Saying [Request Mission]");
                    request.Say();

                    Logging.Log("AgentInteraction: Waiting for mission");
                    State = AgentInteractionState.WaitForMission;
                    _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber5To10());
                }
                else
                {
                    Logging.Log("AgentInteraction: Unexpected dialog options");
                    State = AgentInteractionState.UnexpectedDialogOptions;
                }
            }
            else if (view != null)
            {
                // View current mission
                Logging.Log("AgentInteraction: Saying [View Mission]");

                view.Say();
                _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber5To10());
                // No state change
            }
            else if (accept != null || decline != null)
            {
                if (Purpose == AgentInteractionPurpose.StartMission)
                {
                    Logging.Log("AgentInteraction: Waiting for mission");

                    State = AgentInteractionState.WaitForMission; // Do not say anything, wait for the mission
                    _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber5To15());
                }
                else
                {
                    Logging.Log("AgentInteraction: Unexpected dialog options");

                    State = AgentInteractionState.UnexpectedDialogOptions;
                }
            }
        }

        private DamageType GetMissionDamageType(string html)
        {
            // We are going to check damage types
            var logoRegex = new Regex("img src=\"factionlogo:(?<factionlogo>\\d+)");

            Match logoMatch = logoRegex.Match(html);
            if (logoMatch.Success)
            {
                var logo = logoMatch.Groups["factionlogo"].Value;

                // Load faction xml
                XDocument xml = XDocument.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Factions.xml"));
                XElement faction = xml.Root.Elements("faction").FirstOrDefault(f => (string)f.Attribute("logo") == logo);
                if (faction != null)
                    return (DamageType)Enum.Parse(typeof(DamageType), (string)faction.Attribute("damagetype"));
            }

            return DamageType.EM;
        }

        private void WaitForMission()
        {
            DirectAgentWindow agentWindow = Agent.Window;
            if (agentWindow == null || !agentWindow.IsReady)
            {
                if (DateTime.Now.Subtract(_waitingonagentwindowtimer).TotalSeconds > 60)
                {
                    Logging.Log("AgentInteraction: WaitForMission: agentWindow == null : waiting");
                    _waitingonagentwindowtimer = DateTime.Now;
                    if (Math.Round(DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalMinutes) > 7)
                    {
                        Cache.Instance.CloseQuestorCMDLogoff = false;
                        Cache.Instance.CloseQuestorCMDExitGame = true;
                        Cache.Instance.ReasonToStopQuestor = "AgentInteraction: WaitforMission: Journal would not open/refresh- journalwindows was null: restarting EVE Session";
                        Logging.Log(Cache.Instance.ReasonToStopQuestor);
                        Cache.Instance.SessionState = "Quitting";
                    }
                }
                return;
            }

            DirectWindow journalWindow = Cache.Instance.GetWindowByName("journal");
            if (journalWindow == null)
            {
                if (DateTime.Now > _nextJournalOpenRequest)
                {
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenJournal);
                    _nextJournalOpenRequest = DateTime.Now.AddSeconds(30);

                }
                return;
            }

            DirectAgentMission mission = Cache.Instance.DirectEve.AgentMissions.FirstOrDefault(m => m.AgentId == AgentId);
            if (mission == null)
            { 
                if (DateTime.Now.Subtract(_waitingonmissiontimer).TotalSeconds > 60)
                {
                    Logging.Log("AgentInteraction: WaitForMission: mission == null : waiting");
                    _waitingonmissiontimer = DateTime.Now;
                    if (Math.Round(DateTime.Now.Subtract(Cache.Instance.LastKnownGoodConnectedTime).TotalMinutes) > 7)
                    {
                        Cache.Instance.CloseQuestorCMDLogoff = false;
                        Cache.Instance.CloseQuestorCMDExitGame = true;
                        Cache.Instance.ReasonToStopQuestor = "AgentInteraction: WaitforMission: Journal would not open/refresh - mission was null: restarting EVE Session";
                        Logging.Log(Cache.Instance.ReasonToStopQuestor);
                        Cache.Instance.SessionState = "Quitting";
                    }
                }

                return;
            }

            string missionName = Cache.Instance.FilterPath(mission.Name);

            string html = agentWindow.Objective;
            if (CheckFaction() || Settings.Instance.Blacklist.Any(m => m.ToLower() == missionName.ToLower()))
            {
                if (Purpose != AgentInteractionPurpose.AmmoCheck)
                    Logging.Log("AgentInteraction: Declining blacklisted faction mission");

                State = AgentInteractionState.DeclineMission;
                _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber5To10());
                return;
            }


            if (html.Contains("The route generated by current autopilot settings contains low security systems!"))
            {
                if ((missionName != "Enemies Abound (2 of 5)") || (missionName == "Enemies Abound (2 of 5)" && !Settings.Instance.LowSecMissions))
                {
                    if (Purpose != AgentInteractionPurpose.AmmoCheck)
                        Logging.Log("AgentInteraction: Declining low-sec mission");

                    State = AgentInteractionState.DeclineMission;
                    _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To7());
                    return;
                }
            }

            if (!ForceAccept)
            {
                // Is the mission offered?
                if (mission.State == (int)MissionState.Offered && (mission.Type == "Mining" || mission.Type == "Trade" || (mission.Type == "Courier" && missionName != "Enemies Abound (2 of 5)")))
                {
                    Logging.Log("AgentInteraction: Declining courier/mining/trade");

                    State = AgentInteractionState.DeclineMission;
                    _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber5To10());
                    return;
                }

            }

            if (missionName != "Enemies Abound (2 of 5)")
            {
                bool loadedAmmo = false;

                string missionXmlPath = Path.Combine(Settings.Instance.MissionsPath, missionName + ".xml");
                Cache.Instance.MissionAmmo = new List<Ammo>();
                if (File.Exists(missionXmlPath))
                {
                    Logging.Log("AgentInteraction: Loading mission xml [" + missionName + "]");
                    //
                    // this loads the settings global to the mission, NOT individual pockets
                    //
                    try
                    {
                        XDocument missionXml = XDocument.Load(missionXmlPath);
                        //load mission specific ammo and weapongroupid if specified in the mission xml
                        XElement ammoTypes = missionXml.Root.Element("missionammo");
                        if (ammoTypes != null)
                            foreach (XElement ammo in ammoTypes.Elements("ammo"))
                                Cache.Instance.MissionAmmo.Add(new Ammo(ammo));
                        Cache.Instance.MissionWeaponGroupId = (int?)missionXml.Root.Element("weaponGroupId") ?? 0;
                        Cache.Instance.MissionUseDrones = (bool?)missionXml.Root.Element("useDrones"); //should this default to true?
                        //Cache.Instance.MissionDroneTypeID = (int?)missionXml.Root.Element("DroneTypeId") ?? Settings.Instance.DroneTypeId;
                        IEnumerable<DamageType> damageTypes = missionXml.XPathSelectElements("//damagetype").Select(e => (DamageType)Enum.Parse(typeof(DamageType), (string)e, true));
                        if (damageTypes.Any())
                        {
                            LoadSpecificAmmo(damageTypes.Distinct());
                            loadedAmmo = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Log("AgentInteraction: Error parsing damage types for mission [" + mission.Name + "], " + ex.Message);
                    }
                }

                if (!loadedAmmo)
                {
                    Logging.Log("AgentInteraction: Detecting damage type for [" + missionName + "]");
                    Cache.Instance.DamageType = GetMissionDamageType(html);
                    LoadSpecificAmmo(new[] { Cache.Instance.DamageType });
                }

                if (Purpose == AgentInteractionPurpose.AmmoCheck)
                {
                    Logging.Log("AgentInteraction: Closing conversation");

                    State = AgentInteractionState.CloseConversation;
                    return;
                }
            }

            if (missionName == "Enemies Abound (2 of 5)")
                Cache.Instance.CourierMission = true;
            else
                Cache.Instance.CourierMission = false;

            Cache.Instance.MissionName = missionName;

            if (mission.State == (int)MissionState.Offered)
            {
                Logging.Log("AgentInteraction: Accepting mission [" + missionName + "]");

                State = AgentInteractionState.AcceptMission;
                _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To7());
            }
            else // If we already accepted the mission, close the convo
            {
                Logging.Log("AgentInteraction: Mission [" + missionName + "] already accepted");
                Logging.Log("AgentInteraction: Closing conversation");
                //CheckFaction();
                State = AgentInteractionState.CloseConversation;
                _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To7());
            }
        }

        private void AcceptMission()
        {
            DirectAgentWindow agentWindow = Agent.Window;
            if (agentWindow == null || !agentWindow.IsReady)
                return;

            List<DirectAgentResponse> responses = agentWindow.AgentResponses;
            if (responses == null || responses.Count == 0)
                return;

            DirectAgentResponse accept = responses.FirstOrDefault(r => r.Text.Contains(Accept));
            if (accept == null)
                return;

            Logging.Log("AgentInteraction: Saying [Accept]");
            Cache.Instance.Wealth = Cache.Instance.DirectEve.Me.Wealth;
            accept.Say();

            foreach (DirectWindow window in Cache.Instance.Windows)
            {
                if (window.Name == "modal")
                {
                    bool sayyes = false;
                    if (!string.IsNullOrEmpty(window.Html))
                    {
                        //
                        // Modal Dialogs the need "yes" pressed
                        //
                        sayyes |= window.Html.Contains("objectives requiring a total capacity");
                        sayyes |= window.Html.Contains("your ship only has space for");
                    }
                    if (sayyes)
                    {
                        Logging.Log("Cleanup: Found a window that needs 'yes' chosen...");
                        Logging.Log("Cleanup: Content of modal window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") + "]");
                        window.AnswerModal("Yes");
                        continue;
                    }
                }
            }
            Logging.Log("AgentInteraction: Closing conversation");
            State = AgentInteractionState.CloseConversation;
            _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To7());
        }

        private void DeclineMission()
        {
            // If we are doing an ammo check then Decline Mission is an end-state!
            if (Purpose == AgentInteractionPurpose.AmmoCheck)
                return;

            DirectAgentWindow agentWindow = Agent.Window;
            if (agentWindow == null || !agentWindow.IsReady)
                return;

            List<DirectAgentResponse> responses = agentWindow.AgentResponses;
            if (responses == null || responses.Count == 0)
                return;

            DirectAgentResponse decline = responses.FirstOrDefault(r => r.Text.Contains(Decline));
            if (decline == null)
                return;

            // Check for agent decline timer
            if (WaitDecline)
            {
                string html = agentWindow.Briefing;
                if (html.Contains("Declining a mission from this agent within the next"))
                {
                    var standingRegex = new Regex("Effective Standing:\\s(?<standing>\\d+.\\d+)");
                    Match standingMatch = standingRegex.Match(html);
                    float standings = 0;
                    if (standingMatch.Success)
                    {
                        string standingValue = standingMatch.Groups["standing"].Value;
                        standingValue = standingValue.Replace('.', ','); // necessary for systems w/ comma-delimited number formatting
                        standings = float.Parse(standingValue);
                        if (MinStandings > 10)
                        {
                            standings = standings / 10;
                        }
                        if (MinStandings > 10)
                        {
                            MinStandings = MinStandings / 10;
                        }
                        Logging.Log("AgentInteraction: Agent decline timer detected. Current standings: " + standings + ". Minimum standings: " + MinStandings);
                    }

                    var hourRegex = new Regex("\\s(?<hour>\\d+)\\shour");
                    var minuteRegex = new Regex("\\s(?<minute>\\d+)\\sminute");
                    Match hourMatch = hourRegex.Match(html);
                    Match minuteMatch = minuteRegex.Match(html);
                    int hours = 0;
                    int minutes = 0;
                    if (hourMatch.Success)
                    {
                        string hourValue = hourMatch.Groups["hour"].Value;
                        hours = Convert.ToInt32(hourValue);
                    }
                    if (minuteMatch.Success)
                    {
                        string minuteValue = minuteMatch.Groups["minute"].Value;
                        minutes = Convert.ToInt32(minuteValue);
                    }

                    int secondsToWait = ((hours * 3600) + (minutes * 60) + 60);
                    AgentsList currentAgent = Settings.Instance.AgentsList.FirstOrDefault(i => i.Name == Cache.Instance.CurrentAgent);

                    if (standings <= MinStandings && !Cache.Instance.IsAgentLoop)
                    {
                        _nextAgentAction = DateTime.Now.AddSeconds(secondsToWait);
                        Logging.Log("AgentInteraction: Current standings at or below minimum.  Waiting " + (secondsToWait / 60) + " minutes to try decline again.");
                        CloseConversation();

                        State = AgentInteractionState.StartConversation;

                        return;
                    }

                    //add timer to current agent
                    if (Cache.Instance.IsAgentLoop && Settings.Instance.MultiAgentSupport)
                    {
                        if (currentAgent != null) currentAgent.DeclineTimer = DateTime.Now.AddSeconds(secondsToWait);
                        CloseConversation();

                        Cache.Instance.CurrentAgent = Cache.Instance.SwitchAgent;
                        Logging.Log("AgentInteraction: new agent is " + Cache.Instance.CurrentAgent);
                        State = AgentInteractionState.ChangeAgent;

                        return;
                    }
                    Logging.Log("AgentInteraction: Current standings above minimum.  Declining mission.");
                }
            }

            // Decline and request a new mission
            Logging.Log("AgentInteraction: Saying [Decline]");
            decline.Say();

            Logging.Log("AgentInteraction: Replying to agent");
            State = AgentInteractionState.ReplyToAgent;
            _nextAgentAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To7());
        }

        public bool CheckFaction()
        {
            DirectAgentWindow agentWindow = Agent.Window;
            string html = agentWindow.Objective;
            var logoRegex = new Regex("img src=\"factionlogo:(?<factionlogo>\\d+)");
            Match logoMatch = logoRegex.Match(html);
            if (logoMatch.Success)
            {
                string logo = logoMatch.Groups["factionlogo"].Value;

                // Load faction xml
                XDocument xml = XDocument.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path2: "Factions.xml"));
                if (xml.Root != null)
                {
                    XElement faction = xml.Root.Elements("faction").FirstOrDefault(f => (string)f.Attribute("logo") == logo);
                    //Cache.Instance.factionFit = "Default";
                    //Cache.Instance.Fitting = "Default";
                    if (faction != null)
                    {
                        var factionName = ((string)faction.Attribute("name"));
                        Cache.Instance.FactionName = factionName;
                        Logging.Log("AgentInteraction: Mission enemy faction: " + factionName);
                        if (Settings.Instance.FactionBlacklist.Any(m => m.ToLower() == factionName.ToLower()))
                            return true;
                        if (Settings.Instance.UseFittingManager && Settings.Instance.FactionFitting.Any(m => m.Faction.ToLower() == factionName.ToLower()))
                        {
                            FactionFitting factionFitting = Settings.Instance.FactionFitting.FirstOrDefault(m => m.Faction.ToLower() == factionName.ToLower());
                            if (factionFitting != null)
                            {
                                Cache.Instance.FactionFit = factionFitting.Fitting;
                                Logging.Log("AgentInteraction: Faction fitting: " + factionFitting.Faction);
                            }
                            else
                            {
                                Logging.Log("AgentInteraction: Faction fitting: No fittings defined for [ " + factionName + " ]");
                            }
                            //Cache.Instance.Fitting = Cache.Instance.factionFit;
                            return false;
                        }
                    }/*
                else if (Settings.Instance.FittingsDefined)
                {
                    Cache.Instance.factionName = "Default";
                    var FactionFitting = Settings.Instance.FactionFitting.FirstOrDefault(m => m.Faction.ToLower() == "default");
                    Cache.Instance.factionFit = (string)FactionFitting.Fitting;
                    Logging.Log("AgentInteraction: Faction fitting " + FactionFitting.Faction);
                    //Cache.Instance.Fitting = Cache.Instance.factionFit;
                    return false;
                }
                return false;  */
                }
                else
                {
                    Logging.Log("AgentInteraction: Faction fitting: Missing Factions.xml :aborting faction fittings");
                }
            }
            if (Settings.Instance.UseFittingManager)
            {
                Cache.Instance.FactionName = "Default";
                FactionFitting factionFitting = Settings.Instance.FactionFitting.FirstOrDefault(m => m.Faction.ToLower() == "default");
                if (factionFitting != null)
                {
                    Cache.Instance.FactionFit = factionFitting.Fitting;
                    Logging.Log("AgentInteraction: Faction fitting: " + factionFitting.Faction);
                }
                else
                {
                    Logging.Log("AgentInteraction: Faction fitting: No fittings defined for [ " + Cache.Instance.FactionName + " ]");
                }
                //Cache.Instance.Fitting = Cache.Instance.factionFit;
            }
            return false;
        }

        public void CloseConversation()
        {
            DirectAgentWindow agentWindow = Agent.Window;
            if (agentWindow == null)
            {
                Logging.Log(Cache.Instance.CourierMission ? "AgentInteraction: Courier Done" : "AgentInteraction: Done");
                State = AgentInteractionState.Done;
                return;
            }

            agentWindow.Close();
        }

        public void ProcessState()
        {
            // Wait a bit before doing "things"
            if (DateTime.Now < _nextAgentAction)
                return;

            switch (State)
            {
                case AgentInteractionState.Idle:
                    break;
                case AgentInteractionState.Done:
                    break;

                case AgentInteractionState.ChangeAgent:
                    Logging.Log("AgentInteraction: Change Agent");
                    break;

                case AgentInteractionState.StartConversation:
                    Agent.InteractWith();

                    Logging.Log("AgentInteraction: Waiting for conversation");
                    State = AgentInteractionState.WaitForConversation;
                    break;

                case AgentInteractionState.WaitForConversation:
                    WaitForConversation();
                    break;

                case AgentInteractionState.ReplyToAgent:
                    ReplyToAgent();
                    break;

                case AgentInteractionState.WaitForMission:
                    WaitForMission();
                    break;

                case AgentInteractionState.AcceptMission:
                    AcceptMission();
                    break;

                case AgentInteractionState.DeclineMission:
                    DeclineMission();
                    break;

                case AgentInteractionState.CloseConversation:
                    CloseConversation();
                    break;
            }
        }
    }
}