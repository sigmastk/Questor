using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Questor.Modules.States
{
        public enum StateBuyLPI
        {
            Idle,
            Begin,
            OpenItemHangar,
            OpenLpStore,
            FindOffer,
            CheckPetition,
            OpenMarket,
            BuyItems,
            AcceptOffer,
            Quatity,
            Done,
        }
}
