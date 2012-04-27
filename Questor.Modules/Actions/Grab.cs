
namespace Questor.Modules.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DirectEve;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.States;
    
    public class Grab
    {
        public StateGrab State { get; set; }

        public int Item { get; set; }
        public int Unit { get; set; }
        public string Hangar { get; set; }
        private double freeCargoCapacity;

        private DateTime _lastAction;


        public void ProcessState()
        {

            DirectContainer _hangar = null;

            DirectContainer cargo = Cache.Instance.DirectEve.GetShipsCargo();
            

            if ("Local Hangar" == Hangar)
                _hangar = Cache.Instance.DirectEve.GetItemHangar();
            else if ("Ship Hangar" == Hangar)
                _hangar = Cache.Instance.DirectEve.GetShipHangar();
            else
                _hangar = Cache.Instance.DirectEve.GetCorporationHangar(Hangar);

            
            switch (State)
            {
                case StateGrab.Idle:
                case StateGrab.Done:
                    break;

                case StateGrab.Begin:
                    State = StateGrab.OpenItemHangar;
                    break;

                case StateGrab.OpenItemHangar:

                    if (DateTime.Now.Subtract(_lastAction).TotalSeconds < 2)
                        break;

                    if ("Local Hangar" == Hangar)
                    {
                        // Is the hangar open?
                        if (_hangar.Window == null)
                        {
                            // No, command it to open
                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                            
                        }
                        if (!_hangar.IsReady)
                            break;

                    }
                    else if ("Ship Hangar" == Hangar)
                    {
                        if (_hangar.Window == null)
                        {
                            // No, command it to open
                            Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenShipHangar);
                            
                        }
                        if (!_hangar.IsReady)
                            break;
                    }
                    else if (Hangar != null)
                    {
                        if (_hangar.Window == null)
                        {
                            // No, command it to open
                            Cache.Instance.DirectEve.OpenCorporationHangar();
                            
                        }

                        if (!_hangar.IsReady)
                            break;
                    }

                        Logging.Log("Grab: Opening Hangar");

                        State = StateGrab.OpenCargo;
     
                    break;

                case StateGrab.OpenCargo:

                    if (DateTime.Now.Subtract(_lastAction).TotalSeconds < 2)
                        break;
                    // Is cargo open?
                    if (cargo.Window == null)
                    {
                        // No, command it to open
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
                        break;
                    }

                    if (!cargo.IsReady)
                        break;


                    Logging.Log("Grab: Opening Cargo Hold");

                    freeCargoCapacity = cargo.Capacity - cargo.UsedCapacity;

                    if (Item == 00)
                        State = StateGrab.AllItems;
                    else
                        State = StateGrab.MoveItems;

                    
                    break;

                case StateGrab.MoveItems:

                    if (DateTime.Now.Subtract(_lastAction).TotalSeconds < 2)
                        break;
                    if (Unit == 00)
                    {
                        DirectItem GrabItem = _hangar.Items.FirstOrDefault(i => (i.TypeId == Item));
                        if (GrabItem != null)
                        {
                            double totalVolum = GrabItem.Quantity*GrabItem.Volume;
                            if (freeCargoCapacity >= totalVolum)
                            {
                                cargo.Add(GrabItem, GrabItem.Quantity);
                                freeCargoCapacity -= totalVolum;
                                Logging.Log("Grab: Moving all the items");
                                _lastAction = DateTime.Now;
                                State = StateGrab.WaitForItems;
                            }
                            else
                            {
                                State = StateGrab.Done;
                                Logging.Log("Grab: No load capacity");
                            }
                        }
                    }
                    else
                    {
                        DirectItem GrabItem = _hangar.Items.FirstOrDefault(i => (i.TypeId == Item));
                        if (GrabItem != null)
                        {
                            double totalVolum = Unit*GrabItem.Volume;
                            if (freeCargoCapacity >= totalVolum)
                            {
                                cargo.Add(GrabItem, Unit);
                                freeCargoCapacity -= totalVolum;
                                Logging.Log("Grab: Moving item");
                                _lastAction = DateTime.Now;
                                State = StateGrab.WaitForItems;
                            }
                            else
                            {
                                State = StateGrab.Done;
                                Logging.Log("Grab: No load capacity");
                            }
                        }
                    } 

                    
                     break;


                case StateGrab.AllItems:

                     if (DateTime.Now.Subtract(_lastAction).TotalSeconds < 2)
                         break;

                     List<DirectItem> AllItem = _hangar.Items;
                     if (AllItem != null)
                     {
                         foreach (DirectItem item in AllItem)
                         {
                             double totalVolum = item.Quantity*item.Volume;

                             if (freeCargoCapacity >= totalVolum)
                             {
                                 cargo.Add(item);
                                 freeCargoCapacity -= totalVolum;
                             }

                         }
                         Logging.Log("Grab: Moving items");
                         _lastAction = DateTime.Now;
                         State = StateGrab.WaitForItems;
                     }


                     break;

                case StateGrab.WaitForItems:
                     // Wait 5 seconds after moving
                     if (DateTime.Now.Subtract(_lastAction).TotalSeconds < 5)
                         break;


                     if (Cache.Instance.DirectEve.GetLockedItems().Count == 0)
                     {

                         Logging.Log("Grab: Done");
                         State = StateGrab.Done;
                         break;
                     }


                     break;

            }


        }



    }
}
