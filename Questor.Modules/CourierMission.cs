namespace Questor.Modules
{
    using System;
    using System.Linq;
    using DirectEve;

    public class CourierMission
    {
        private DateTime _nextCourierAction;
        private readonly Traveler _traveler;
        public CourierMissionState State { get; set; }

        /// <summary>
        ///   Arm does nothing but get into a (assembled) shuttle
        /// </summary>
        /// <returns></returns>
        ///
        public CourierMission()
        {
            _traveler = new Traveler();
        }

        private bool GotoMissionBookmark(long agentId, string title)
        {
            var destination = _traveler.Destination as MissionBookmarkDestination;
            if (destination == null || destination.AgentId != agentId || !destination.Title.StartsWith(title))
                _traveler.Destination = new MissionBookmarkDestination(Cache.Instance.GetMissionBookmark(agentId, title));

            _traveler.ProcessState();

            if (_traveler.State == TravelerState.AtDestination)
            {
                if (destination != null)
                    Logging.Log("CourierMission: Arrived at Mission Bookmark Destination [ " + destination.Title + " ]");
                else
                {
                    Logging.Log("CourierMission: destination is null"); //how would this occur exactly?
                }
                _traveler.Destination = null;
                return true;
            }

            return false;
        }

        private bool MoveItem(bool pickup)
        {
            DirectEve directEve = Cache.Instance.DirectEve;

            // Open the item hangar (should still be open)
            if (!Cache.OpenItemsHangar("CourierMission")) return false;

            if (!Cache.OpenCargoHold("CourierMission")) return false;

            const string missionItem = "Encoded Data Chip";
            Logging.Log("CourierMission: mission item is: " + missionItem);
            DirectContainer from = pickup ? Cache.Instance.ItemHangar : Cache.Instance.CargoHold;
            DirectContainer to = pickup ? Cache.Instance.CargoHold : Cache.Instance.ItemHangar;

            // We moved the item
            if (to.Items.Any(i => i.TypeName == missionItem))
                return true;

            if (directEve.GetLockedItems().Count != 0)
                return false;

            // Move items
            foreach (DirectItem item in from.Items.Where(i => i.TypeName == missionItem))
            {
                Logging.Log("CourierMissionState: Moving [" + item.TypeName + "][" + item.ItemId + "] to " + (pickup ? "cargo" : "hangar"));
                to.Add(item);
            }
            _nextCourierAction = DateTime.Now.AddSeconds(8);
            return false;
        }

        /// <summary>
        ///   Goto the pickup location
        ///   Pickup the item
        ///   Goto drop off location
        ///   Drop the item
        ///   Goto Agent
        ///   Complete mission
        /// </summary>
        /// <returns></returns>
        public void ProcessState()
        {
            switch (State)
            {
                case CourierMissionState.Idle:
                    break;

                case CourierMissionState.GotoPickupLocation:
                    //cache.instance.agentid cannot be used for storyline missions! you must pass the correct agentID to this module if you wish to extend it to do storyline missions
                    if (GotoMissionBookmark(Cache.Instance.AgentId, "Objective (Pick Up)"))
                        State = CourierMissionState.PickupItem;
                    break;

                case CourierMissionState.PickupItem:
                    if (MoveItem(true))
                        State = CourierMissionState.GotoDropOffLocation;
                    break;

                case CourierMissionState.GotoDropOffLocation:
                    //cache.instance.agentid cannot be used for storyline missions! you must pass the correct agentID to this module if you wish to extend it to do storyline missions
                    if (GotoMissionBookmark(Cache.Instance.AgentId, "Objective (Drop Off)"))
                        State = CourierMissionState.DropOffItem;
                    break;

                case CourierMissionState.DropOffItem:
                    if (MoveItem(false))
                        State = CourierMissionState.Done;
                    break;

                case CourierMissionState.Done:
                    Logging.Log("CourierMissionState: Done");
                    break;
            }

        }
    }
}
