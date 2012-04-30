// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System.Globalization;

namespace BuyLPI
{
    using System;
    using System.Linq;
    using System.Threading;
    using DirectEve;
    using Questor.Modules.Logging;

    internal class BuyLPI
    {
        private const int WaitMillis = 3500;
        private static long _lastLoyaltyPoints;
        private static DateTime _nextAction;
        private static DateTime _loyaltyPointTimeout;
        private static string _type;
        private static int? _quantity;
        private static int? _totalquantityoforders;
        private static bool _done;
        private static DirectEve _directEve;

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Logging.Log("Syntax:");
                Logging.Log("DotNet BuyLPI BuyLPI <TypeName or TypeId> [Quantity]");
                Logging.Log("(Quantity is optional)");
                Logging.Log("");
                Logging.Log("Example:");
                Logging.Log("DotNet BuyLPI BuyLPI \"Caldari Navy Mjolnir Torpedo\" 10");
                Logging.Log("*OR*");
                Logging.Log("DotNet BuyLPI BuyLPI 27339 10");
                return;
            }

            if (args.Length >= 1)
            {
                _type = args[0];
            }

            if (args.Length >= 2)
            {
                int dummy;
                if (!int.TryParse(args[1], out dummy))
                {
                    Logging.Log("BuyLPI: Quantity must be an integer, 0 - " + int.MaxValue);
                    return;
                }

                if (dummy < 0)
                {
                    Logging.Log("BuyLPI: Quantity must be a positive number");
                    return;
                }

                _quantity = dummy;
                _totalquantityoforders = dummy;
            }

            Logging.Log("BuyLPI: Starting BuyLPI...");
            _directEve = new DirectEve();
            _directEve.OnFrame += OnFrame;

            // Sleep until we're done
            while (!_done)
                Thread.Sleep(50);

            _directEve.Dispose();
            Logging.Log("BuyLPI: BuyLPI finished.");
        }

        private static void OnFrame(object sender, EventArgs eventArgs)
        {
            if (_done)
                return;

            // Wait for the next action
            if (_nextAction >= DateTime.Now)
            {
                return;
            }

            DirectContainer hangar = _directEve.GetItemHangar();
            if (!hangar.IsReady)
            {
                _nextAction = DateTime.Now.AddMilliseconds(WaitMillis);
                _directEve.ExecuteCommand(DirectCmd.OpenHangarFloor);

                Logging.Log("BuyLPI: Opening item hangar");
                return;
            }

            DirectLoyaltyPointStoreWindow lpstore = _directEve.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
            if (lpstore == null)
            {
                _nextAction = DateTime.Now.AddMilliseconds(WaitMillis);
                _directEve.ExecuteCommand(DirectCmd.OpenLpstore);

                Logging.Log("BuyLPI: Opening loyalty point store");
                return;
            }

            // Wait for the amount of LP to change
            if (_lastLoyaltyPoints == lpstore.LoyaltyPoints)
                return;

            // Do not expect it to be 0 (probably means its reloading)
            if (lpstore.LoyaltyPoints == 0)
            {
                if (_loyaltyPointTimeout < DateTime.Now)
                {
                    Logging.Log("BuyLPI: It seems we have no loyalty points left");

                    _done = true;
                    return;
                }
                return;
            }

            _lastLoyaltyPoints = lpstore.LoyaltyPoints;

            // Find the offer
            DirectLoyaltyPointOffer offer = lpstore.Offers.FirstOrDefault(o => o.TypeId.ToString(CultureInfo.InvariantCulture) == _type || String.Compare(o.TypeName, _type, StringComparison.OrdinalIgnoreCase) == 0);
            if (offer == null)
            {
                Logging.Log("BuyLPI: Can't find offer with type name/id: {0}!", _type);

                _done = true;
                return;
            }

            // Check LP
            if (_lastLoyaltyPoints < offer.LoyaltyPointCost)
            {
                Logging.Log("BuyLPI: Not enough loyalty points left");

                _done = true;
                return;
            }

            // Check ISK
            if (_directEve.Me.Wealth < offer.IskCost)
            {
                Logging.Log("BuyLPI: Not enough ISK left");

                _done = true;
                return;
            }

            // Check items
            foreach (DirectLoyaltyPointOfferRequiredItem requiredItem in offer.RequiredItems)
            {
                DirectItem item = hangar.Items.FirstOrDefault(i => i.TypeId == requiredItem.TypeId);
                if (item == null || item.Quantity < requiredItem.Quantity)
                {
                    Logging.Log("BuyLPI: Missing [" + requiredItem.Quantity + "] x [" +
                                                    requiredItem.TypeName + "]");
                    _done = true;
                    return;
                }
            }

            // All passed, accept offer
            Logging.Log("BuyLPI: Accepting " + offer.TypeName + " [ " + _quantity.Value + " ] of [ " + _totalquantityoforders.Value + " ] orders and will cost another [" + Math.Round(((offer.IskCost * _quantity.Value)/(double)1000000),2) + "mil isk]"); 
            offer.AcceptOffer();

            // Set next action + loyalty point timeout
            _nextAction = DateTime.Now.AddMilliseconds(WaitMillis);
            _loyaltyPointTimeout = DateTime.Now.AddSeconds(10);

            if (_quantity.HasValue)
            {
                _quantity = _quantity.Value - 1;
                if (_quantity.Value <= 0)
                {
                    Logging.Log("Quantity limit reached");

                    _done = true;
                    return;
                }
            }
        }
    }
}