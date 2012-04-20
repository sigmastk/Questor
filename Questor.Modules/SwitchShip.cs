
namespace Questor.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DirectEve;
    //using System.Xml.Linq;
    //using System.IO;

    public class SwitchShip
    {
        private DateTime _lastSwitchShipAction;

        public SwitchShipState State { get; set; }

        public void ProcessState()
        {
            DirectContainer shipHangar = Cache.Instance.DirectEve.GetShipHangar();
            var defaultFitting = Settings.Instance.DefaultFitting.Fitting;

            switch (State)
            {
                case SwitchShipState.Idle:

                    break;
                case SwitchShipState.Done:
                    break;

                case SwitchShipState.Begin:
                    State = SwitchShipState.OpenShipHangar;
                    break;

                case SwitchShipState.OpenShipHangar:
                    // Is the ship hangar open?
                    if (!Cache.OpenShipsHangar("SwitchShip")) break;

                    Logging.Log("SwitchShip: Activating combat ship");

                    State = SwitchShipState.ActivateCombatShip;

                    break;

                case SwitchShipState.ActivateCombatShip:
                    string shipName = Settings.Instance.CombatShipName.ToLower();

                    if ((!string.IsNullOrEmpty(shipName) && Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower() != shipName))
                    {
                        if (DateTime.Now.Subtract(_lastSwitchShipAction).TotalSeconds > (int)Time.SwitchShipsDelay_seconds)
                        {
                            List<DirectItem> ships = Cache.Instance.DirectEve.GetShipHangar().Items;
                            foreach (DirectItem ship in ships.Where(ship => ship.GivenName != null && ship.GivenName.ToLower() == shipName))
                            {
                                Logging.Log("Arm: Making [" + ship.GivenName + "] active");

                                ship.ActivateShip();
                                Logging.Log("SwitchShip: Activated");
                                _lastSwitchShipAction = DateTime.Now;
                                return;
                            }
                        }
                    }
                    if (Settings.Instance.UseFittingManager)
                        State = SwitchShipState.OpenFittingWindow;
                    else
                        State = SwitchShipState.Done;

                    break;


                case SwitchShipState.OpenFittingWindow:
                    //let's check first if we need to change fitting at all
                    Logging.Log("SwitchShip: Fitting: " + defaultFitting + " - currentFit: " + Cache.Instance.CurrentFit);
                    if (defaultFitting.Equals(Cache.Instance.CurrentFit))
                    {
                        Logging.Log("SwitchShip: Current fit is correct - no change necessary");
                        State = SwitchShipState.Done;
                    }
                    else
                    {
                        Cache.Instance.DirectEve.OpenFitingManager();
                        State = SwitchShipState.WaitForFittingWindow;
                    }
                    break;

                case SwitchShipState.WaitForFittingWindow:

                    DirectFittingManagerWindow fittingMgr = Cache.Instance.DirectEve.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault();
                    //open it again ?
                    if (fittingMgr == null)
                    {
                        Logging.Log("SwitchShip: Opening fitting manager");
                        Cache.Instance.DirectEve.OpenFitingManager();
                    }
                    //check if it's ready
                    else if (fittingMgr.IsReady)
                    {
                        State = SwitchShipState.ChoseFitting;
                    }
                    break;

                case SwitchShipState.ChoseFitting:
                    fittingMgr = Cache.Instance.DirectEve.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault();
                    if (fittingMgr != null)
                    {
                        Logging.Log("SwitchShip: Looking for fitting " + defaultFitting);

                        foreach (DirectFitting fitting in fittingMgr.Fittings)
                        {
                            //ok found it
                            DirectActiveShip ship = Cache.Instance.DirectEve.ActiveShip;
                            if (defaultFitting.ToLower().Equals(fitting.Name.ToLower()) &&
                                fitting.ShipTypeId == ship.TypeId)
                            {
                                Logging.Log("SwitchShip: Found fitting " + fitting.Name);
                                //switch to the requested fitting for the current mission
                                fitting.Fit();
                                _lastSwitchShipAction = DateTime.Now;
                                Cache.Instance.CurrentFit = fitting.Name;
                                State = SwitchShipState.WaitForFitting;
                                break;
                            }
                        }
                    }
                    State = SwitchShipState.Done;
                    if (fittingMgr != null) fittingMgr.Close();
                    break;

                case SwitchShipState.WaitForFitting:
                    //let's wait 10 seconds
                    if (DateTime.Now.Subtract(_lastSwitchShipAction).TotalMilliseconds > (int)Time.FittingWindowLoadFittingDelay_seconds &&
                        Cache.Instance.DirectEve.GetLockedItems().Count == 0)
                    {
                        //we should be done fitting, proceed to the next state
                        State = SwitchShipState.Done;
                        fittingMgr = Cache.Instance.DirectEve.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault();
                        if (fittingMgr != null) fittingMgr.Close();
                        Logging.Log("SwitchShip: Done fitting");
                    }
                    else Logging.Log("SwitchShip: Waiting for fitting. time elapsed = " + DateTime.Now.Subtract(_lastSwitchShipAction).TotalMilliseconds + " locked items = " + Cache.Instance.DirectEve.GetLockedItems().Count);
                    break;

                case SwitchShipState.NotEnoughAmmo:
                    Logging.Log("SwitchShip: Out of Ammo, checking a solution ...");
                    break;
            }
        }
    }
}