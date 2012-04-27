using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Questor.Modules.States
{
    public enum StateBuy
    {
        Idle,
        Done,
        Begin,
        OpenMarket,
        LoadItem,
        BuyItem,
        WaitForItems,
    }
}
