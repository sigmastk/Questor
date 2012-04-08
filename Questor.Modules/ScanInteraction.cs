//NOT FINISH DON'T USE
namespace Questor.Modules
{
    //using System;
    //using System.Collections.Generic;
    //using System.IO;
    using System.Linq;
    //using System.Reflection;
    //using System.Text.RegularExpressions;
    //using System.Xml.Linq;
    //using System.Xml.XPath;
    using DirectEve;

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
