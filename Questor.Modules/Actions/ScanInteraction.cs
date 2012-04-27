//NOT FINISHED DON'T USE

namespace Questor.Modules.Actions
{
    using System.Linq;
    using DirectEve;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.States;
    using global::Questor.Modules.Logging;

    public class ScanInteraction
    {
        //private DateTime _lastExecute;

        public ScanInteractionState State { get; set; }

        //public List<DirectScanResult> Result;

        public void ProcessState()
        {
            DirectScannerWindow scannerWindow = Cache.Instance.Windows.OfType<DirectScannerWindow>().FirstOrDefault();

            switch(State)
            {
                case ScanInteractionState.Idle:
                    //_lastExecute = DateTime.Now;
                    break;
                case ScanInteractionState.Done:

                    Logging.Log("ScanInteraction: Closing Scan Window");
                    if (scannerWindow != null) scannerWindow.Close();

                    State = ScanInteractionState.Idle;

                    break;
                case ScanInteractionState.Scan:

                    if(scannerWindow == null)
                    {
                        Logging.Log("ScanInteraction: Open Scan Window");

                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenScanner);
                        break;
                    }
                    if(!scannerWindow.IsReady)
                        return;

                    //Not Finish don't use
                    //ScannerWindow.SelectByIdx(0);
                    //Result = ScannerWindow.ScanResults;

                    //State = ScanInteractionState.Done;

                    break;

                default:
                    State = ScanInteractionState.Idle;
                    break;
            }
        }

    }
}
