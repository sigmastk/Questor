﻿
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
        public int Item { get; set; }

        public int Unit { get; set; }

        public string Hangar { get; set; }

        private double _freeCargoCapacity;

        private DateTime _lastAction;

        public void ProcessState()
        {
            if (!Cache.Instance.InStation)
                return;

            if (Cache.Instance.InSpace)
                return;

            if (DateTime.Now < Cache.Instance.LastInSpace.AddSeconds(20)) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return;

            DirectContainer _hangar = null;

            if (!Cache.Instance.OpenItemsHangar("Grab")) return;
            if (!Cache.Instance.OpenShipsHangar("Grab")) return;

            if ("Local Hangar" == Hangar)
                _hangar = Cache.Instance.ItemHangar;
            else if ("Ship Hangar" == Hangar)
                _hangar = Cache.Instance.ShipHangar;
            //else
            //_hangar = Cache.Instance.DirectEve.GetCorporationHangar(Hangar); //this needs to be fixed

            switch (_States.CurrentGrabState)
            {
                case GrabState.Idle:
                case GrabState.Done:
                    break;

                case GrabState.Begin:
                    _States.CurrentGrabState = GrabState.OpenItemHangar;
                    break;

                case GrabState.OpenItemHangar:

                    if (DateTime.Now.Subtract(_lastAction).TotalSeconds < 2)
                        break;

                    if ("Local Hangar" == Hangar)
                    {
                        if (!Cache.Instance.OpenItemsHangar("Drop")) return;
                    }
                    else if ("Ship Hangar" == Hangar)
                    {
                        if (!Cache.Instance.OpenShipsHangar("Drop")) return;

                        if (_hangar != null && (_hangar.Window == null || !_hangar.Window.IsReady))
                            break;
                    }
                    else if (Hangar != null)
                    {
                        if (_hangar != null && _hangar.Window == null)
                        {
                            // No, command it to open
                            //Cache.Instance.DirectEve.OpenCorporationHangar();
                            break;
                        }

                        if (_hangar != null && !_hangar.Window.IsReady)
                            break;
                    }

                    Logging.Log("Grab", "Opening Hangar", Logging.white);

                    _States.CurrentGrabState = GrabState.OpenCargo;

                    break;

                case GrabState.OpenCargo:

                    if (!Cache.Instance.OpenCargoHold("Grab")) break;

                    Logging.Log("Grab", "Opening Cargo Hold", Logging.white);

                    _freeCargoCapacity = Cache.Instance.CargoHold.Capacity - Cache.Instance.CargoHold.UsedCapacity;

                    _States.CurrentGrabState = Item == 00 ? GrabState.AllItems : GrabState.MoveItems;

                    break;

                case GrabState.MoveItems:

                    if (DateTime.Now.Subtract(_lastAction).TotalSeconds < 2)
                        break;
                    if (Unit == 00)
                    {
                        if (_hangar != null)
                        {
                            DirectItem grabItem = _hangar.Items.FirstOrDefault(i => (i.TypeId == Item));
                            if (grabItem != null)
                            {
                                double totalVolum = grabItem.Quantity * grabItem.Volume;
                                if (_freeCargoCapacity >= totalVolum)
                                {
                                    Cache.Instance.CargoHold.Add(grabItem, grabItem.Quantity);
                                    _freeCargoCapacity -= totalVolum;
                                    Logging.Log("Grab", "Moving all the items", Logging.white);
                                    _lastAction = DateTime.Now;
                                    _States.CurrentGrabState = GrabState.WaitForItems;
                                }
                                else
                                {
                                    _States.CurrentGrabState = GrabState.Done;
                                    Logging.Log("Grab", "No load capacity", Logging.white);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_hangar != null)
                        {
                            DirectItem grabItem = _hangar.Items.FirstOrDefault(i => (i.TypeId == Item));
                            if (grabItem != null)
                            {
                                double totalVolum = Unit * grabItem.Volume;
                                if (_freeCargoCapacity >= totalVolum)
                                {
                                    Cache.Instance.CargoHold.Add(grabItem, Unit);
                                    _freeCargoCapacity -= totalVolum;
                                    Logging.Log("Grab", "Moving item", Logging.white);
                                    _lastAction = DateTime.Now;
                                    _States.CurrentGrabState = GrabState.WaitForItems;
                                }
                                else
                                {
                                    _States.CurrentGrabState = GrabState.Done;
                                    Logging.Log("Grab", "No load capacity", Logging.white);
                                }
                            }
                        }
                    }

                    break;

                case GrabState.AllItems:

                    if (DateTime.Now.Subtract(_lastAction).TotalSeconds < 2)
                        break;

                    if (_hangar != null)
                    {
                        List<DirectItem> allItem = _hangar.Items;
                        if (allItem != null)
                        {
                            foreach (DirectItem item in allItem)
                            {
                                if (Cache.Instance.DirectEve.ActiveShip.ItemId == item.ItemId)
                                {
                                    allItem.Remove(item);
                                    continue;
                                }

                                double totalVolum = item.Quantity * item.Volume;
                                
                                if (_freeCargoCapacity >= totalVolum)
                                {
                                    Cache.Instance.CargoHold.Add(item);
                                    _freeCargoCapacity -= totalVolum;
                                }
                                else
                                {
                                    // we are out of room, should we do a partial item move?
                                    double quantitywecanfit = _freeCargoCapacity / item.Volume;
                                    Cache.Instance.CargoHold.Add(item, (int)quantitywecanfit);
                                    //we are now "full" and should go "home" or "market" (how do we decide where to go ffs?)
                                }
                            }
                            Logging.Log("Grab", "Moving items", Logging.white);
                            _lastAction = DateTime.Now;
                            _States.CurrentGrabState = GrabState.WaitForItems;
                        }
                    }

                    break;

                case GrabState.WaitForItems:
                    // Wait 5 seconds after moving
                    if (DateTime.Now.Subtract(_lastAction).TotalSeconds < 5)
                        break;

                    if (Cache.Instance.DirectEve.GetLockedItems().Count == 0)
                    {
                        Logging.Log("Grab", "Done", Logging.white);
                        _States.CurrentGrabState = GrabState.Done;
                        break;
                    }

                    break;
            }
        }
    }
}