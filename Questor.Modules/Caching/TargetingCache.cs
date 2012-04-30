using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Questor.Modules.Caching
{
    public class TargetingCache
    {
        public static EntityCache CurrentDronesTarget { get; set; }
        public static EntityCache CurrentWeaponsTarget { get; set; }        
        public TargetingCache()
        {

        }
    }
}
