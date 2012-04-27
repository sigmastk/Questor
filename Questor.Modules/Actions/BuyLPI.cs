
namespace Questor.Modules.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DirectEve;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.States;
    
    public class BuyLPI
    {
        public StateBuyLPI State { get; set; }

        public int Item { get; set; }
        public int Unit { get; set; }

        //private MainForm _form;

        private static DateTime _lastAction;
        private static DateTime _loyaltyPointTimeout;
        private static long _lastLoyaltyPoints;
        private int _requiredUnit;
        private int _requiredItemId;

        //public BuyLPI(MainForm form1)
        //{
        //    _form = form1;
        //}

        public void ProcessState()
        {

            DirectContainer hangar = Cache.Instance.DirectEve.GetItemHangar();
            DirectContainer shiphangar = Cache.Instance.DirectEve.GetShipHangar();
            DirectLoyaltyPointStoreWindow lpstore = Cache.Instance.DirectEve.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
            DirectMarketWindow marketWindow = Cache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

            if(DateTime.Now.Subtract(_lastAction).TotalSeconds < 1)
                return;
            _lastAction = DateTime.Now;

            switch(State)
            {
                case StateBuyLPI.Idle:
                case StateBuyLPI.Done:
                    break;


                case StateBuyLPI.Begin:

                    /*
                    if(marketWindow != null)
                        marketWindow.Close();

                    if(lpstore != null)
                        lpstore.Close();*/

                    State = StateBuyLPI.OpenItemHangar;

                    break;

                case StateBuyLPI.OpenItemHangar:

                    if(!hangar.IsReady)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                        Logging.Log("BuyLPI: Opening item hangar");
                    }
                    State = StateBuyLPI.OpenLpStore;

                    break;

                case StateBuyLPI.OpenLpStore:

                    if(lpstore == null)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenLpstore);
                        Logging.Log("BuyLPI: Opening loyalty point store");
                    }
                    State = StateBuyLPI.FindOffer;

                    break;

                case StateBuyLPI.FindOffer:

                    if (lpstore != null)
                    {
                        DirectLoyaltyPointOffer offer = lpstore.Offers.FirstOrDefault(o => o.TypeId == Item);

                        // Wait for the amount of LP to change
                        if(_lastLoyaltyPoints == lpstore.LoyaltyPoints)
                            break;

                        // Do not expect it to be 0 (probably means its reloading)
                        if(lpstore.LoyaltyPoints == 0)
                        {
                            if(_loyaltyPointTimeout < DateTime.Now)
                            {
                                Logging.Log("BuyLPI: It seems we have no loyalty points left");

                                State = StateBuyLPI.Done;
                            }
                            break;
                        }

                        _lastLoyaltyPoints = lpstore.LoyaltyPoints;

                        // Find the offer
                        if(offer == null)
                        {
                            Logging.Log("BuyLPI: Can't find offer with type name/id: " + Item + "!");

                            State = StateBuyLPI.Done;
                            break;
                        }
                    }

                    State = StateBuyLPI.CheckPetition;


                    break;

                case StateBuyLPI.CheckPetition:

                    if (lpstore != null)
                    {
                        DirectLoyaltyPointOffer offer1 = lpstore.Offers.FirstOrDefault(o => o.TypeId == Item);

                        // Check LP
                        if(offer1 != null && _lastLoyaltyPoints < offer1.LoyaltyPointCost)
                        {
                            Logging.Log("BuyLPI: Not enough loyalty points left");

                            State = StateBuyLPI.Done;
                            break;
                        }

                        // Check ISK
                        if (offer1 != null && Cache.Instance.DirectEve.Me.Wealth < offer1.IskCost)
                        {
                            Logging.Log("BuyLPI: Not enough ISK left");

                            State = StateBuyLPI.Done;
                            break;
                        }

                        // Check items
                        if (offer1 != null)
                            foreach(DirectLoyaltyPointOfferRequiredItem requiredItem in offer1.RequiredItems)
                            {

                                DirectItem ship = shiphangar.Items.FirstOrDefault(i => i.TypeId == requiredItem.TypeId);
                                DirectItem item = hangar.Items.FirstOrDefault(i => i.TypeId == requiredItem.TypeId);
                                if(item == null || item.Quantity < requiredItem.Quantity)
                                {
                                    if(ship == null || ship.Quantity < requiredItem.Quantity)
                                    {
                                        Logging.Log("BuyLPI: Missing [" + requiredItem.Quantity + "] x [" + requiredItem.TypeName + "]");

                                        //if(!_form.chkBuyItems.Checked)
                                        //{
                                        //    Logging.Log("BuyLPI: Done, do not buy item");
                                        //    State = StateBuyLPI.Done;
                                        //    break;
                                        //}

                                        Logging.Log("BuyLPI: Are buying the item [" + requiredItem.TypeName + "]");
                                        _requiredUnit = Convert.ToInt32(requiredItem.Quantity);
                                        _requiredItemId = requiredItem.TypeId;
                                        State = StateBuyLPI.OpenMarket;
                                        return;
                                    }
                                }
                            }
                    }

                    State = StateBuyLPI.AcceptOffer;

                    break;


                case StateBuyLPI.OpenMarket:

                    if(marketWindow == null)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                        break;
                    }

                    if(!marketWindow.IsReady)
                        break;

                    State = StateBuyLPI.BuyItems;

                    break;

                case StateBuyLPI.BuyItems:

                    Logging.Log("BuyLPI: Opening Market");

                    if(marketWindow != null && marketWindow.DetailTypeId != _requiredItemId)
                    {
                        marketWindow.LoadTypeId(_requiredItemId);
                        break;
                    }

                    if (marketWindow != null)
                    {
                        IEnumerable<DirectOrder> orders = marketWindow.SellOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId);

                        DirectOrder order = orders.OrderBy(o => o.Price).FirstOrDefault();

                        if(order == null)
                        {
                            Logging.Log("BuyLPI: No orders");
                            State = StateBuyLPI.Done;
                            break;
                        }

                        order.Buy(_requiredUnit, DirectOrderRange.Station);
                    }

                    Logging.Log("BuyLPI: Buy Item");


                    State = StateBuyLPI.CheckPetition;

                    break;

                case StateBuyLPI.AcceptOffer:

                    if (lpstore != null)
                    {
                        DirectLoyaltyPointOffer offer2 = lpstore.Offers.FirstOrDefault(o => o.TypeId == Item);

                        if (offer2 != null)
                        {
                            Logging.Log("BuyLPI: Accepting [" + offer2.TypeName + "]");
                            offer2.AcceptOffer();
                        }
                    }
                    State = StateBuyLPI.Quatity;

                    break;

                case StateBuyLPI.Quatity:

                    _loyaltyPointTimeout = DateTime.Now.AddSeconds(1);

                    Unit = Unit - 1;
                    if(Unit <= 0)
                    {
                        Logging.Log("BuyLPI: Quantity limit reached");

                        State = StateBuyLPI.Done;
                        break;
                    }

                    State = StateBuyLPI.Begin;

                    break;
            }
        }

    }
}
