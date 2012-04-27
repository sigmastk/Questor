using Questor.Modules.Caching;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.BackgroundTasks
{
    using System;
    //using System.Linq;

    public class LocalWatch
    {
        public LocalWatchState State { get; set; }
        private DateTime _lastAction;

        public void ProcessState()
        {
            switch(State)
            {
                case LocalWatchState.Idle:
                    //checking local every 5 second
                    if(DateTime.Now.Subtract(_lastAction).TotalSeconds < (int)Time.CheckLocalDelay_seconds)
                        break;

                    State = LocalWatchState.CheckLocal;
                    break;

                case LocalWatchState.CheckLocal:
                    //
                    // this ought to cache the name of the system, and the number of ppl in local (or similar)
                    // and only query everyone in local for standings changes if something has changed...
                    //
                    Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate,Settings.Instance.LocalBadStandingLevelToConsiderBad);

                    _lastAction = DateTime.Now;
                    State = LocalWatchState.Idle;
                    break;

                default:
                    // Next state
                    State = LocalWatchState.Idle;
                    break;
            }
        }
    }
}
