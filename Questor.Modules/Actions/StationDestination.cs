// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace Questor.Modules.Actions
{
    using System;
    using DirectEve;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Caching;
    
    public class StationDestination2 : TravelerDestination
    {
        private DateTime _nextAction;

        public StationDestination2(long stationId)
        {
            DirectLocation station = Cache.Instance.DirectEve.Navigation.GetLocation(stationId);
            if (station == null || !station.ItemId.HasValue || !station.SolarSystemId.HasValue)
            {
                Logging.Log("QuestorManager.StationDestination: Invalid station id [" + stationId + "]");

                SolarSystemId = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                StationId = -1;
                StationName = "";
                return;
            }

            Logging.Log("QuestorManager.StationDestination: Destination set to [" + station.Name + "]");

            StationId = stationId;
            StationName = station.Name;
            SolarSystemId = station.SolarSystemId.Value;
            Logging.Log(station.SolarSystemId.Value + " " + stationId + " " + station.Name);
        }

        public StationDestination2(long solarSystemId, long stationId, string stationName)
        {
            Logging.Log("QuestorManager.StationDestination: Destination set to [" + stationName + "]");
            Logging.Log(solarSystemId + " " + stationId + " " + stationName);

            SolarSystemId = solarSystemId;
            StationId = stationId;
            StationName = stationName;
        }

        public long StationId { get; set; }
        public string StationName { get; set; }

        public override bool PerformFinalDestinationTask()
        {
            return PerformFinalDestinationTask(StationId, StationName, ref _nextAction);
        }

        internal static bool PerformFinalDestinationTask(long stationId, string stationName, ref DateTime nextAction)
        {
            if (Cache.Instance.DirectEve.Session.IsInStation && Cache.Instance.DirectEve.Session.StationId == stationId)
            {
                Logging.Log("QuestorManager.StationDestination: Arrived in station");
                return true;
            }

            if (Cache.Instance.DirectEve.Session.IsInStation)
            {
                // We are in a station, but not the correct station!
                if (nextAction < DateTime.Now)
                {
                    Logging.Log("QuestorManager.StationDestination: We're docked in the wrong station, undocking");

                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                    nextAction = DateTime.Now.AddSeconds(30);
                }

                // We are not there yet
                return false;
            }

            if (!Cache.Instance.DirectEve.Session.IsInSpace)
            {
                // We are not in station and not in space?  Wait for a bit
                return false;
            }

            if (nextAction > DateTime.Now)
                return false;

            EntityCache entity = Cache.Instance.EntityByName(stationName);
            if (entity == null)
            {
                // We are there but no station? Wait a bit
                return false;
            }

            if (entity.Distance < 2500)
            {
                Logging.Log("QuestorManager.StationDestination: Dock at [" + entity.Name + "]");
                entity.Dock();
            }
            else if (entity.Distance < 150000)
            {
                Logging.Log("QuestorManager.StationDestination: Approach [" + entity.Name + "]");
                entity.Approach();
            }
            else
            {
                Logging.Log("QuestorManager.StationDestination: Warp to and dock at [" + entity.Name + "]");
                entity.WarpToAndDock();
            }

            nextAction = DateTime.Now.AddSeconds(30);
            return false;
        }
    }
}