
using System.Diagnostics;

namespace Questor.Modules.BackgroundTasks
{
   using System;
    //using System.Linq;
    using DirectEve;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.States;

   public class Cleanup
   {
      public CleanupState State { get; set; }
      private DateTime _lastCleanupAction;
      private DateTime _lastChatWindowAction;
      private bool _newprivateconvowindowhandled;
      //private Audio _localalarm;
      private string _localalarmmp3 = "test.mp3";
      //private Audio _convoalarm;
      private string _convoalarmmp3 = "test.mp3";
      //private Audio _miscalarm;
      private string _miscalarmmp3 = "test.mp3";
      //private  Audio _music;


      #region Play WAV
      /// <summary>
      /// Plays a .wav File with the Option to Repeat.
      /// </summary>
      /// <param name="location">The Location to the Sound File.</param>
      /// <param name="repeat">True to Repeat, False to Play Once.</param>
      public  void PlayWAV(String location, Boolean repeat)
      {
          //Declare player as a new SoundPlayer with SoundLocation as the sound location
          System.Media.SoundPlayer player = new System.Media.SoundPlayer(location);
          //If the user has Repeat equal to true
          if (repeat == true)
          {
              //Play the sound continuously
              player.PlayLooping();
          }
          else
          {
              //Play the sound once
              player.Play();
              System.Media.SystemSound sound = System.Media.SystemSounds.Beep;
              sound.Play();
          }
      }
      #endregion
      #region Play MP3
      /// <summary>
      /// Plays a .mp3 File.
      /// </summary>
      /// <param name="location">The Location to the Sound File.</param>
      public  void PlayMP3(String location)
      {
          //_music = new Microsoft.DirectX.AudioVideoPlayback.Audio(location);
          //_music.Play();
      }
      #endregion

      private  void BeginClosingQuestor()
      {
          Cache.Instance.EnteredCloseQuestor_DateTime = DateTime.Now;
          Cache.Instance.SessionState = "Quitting";
      }

      public  void CheckEVEStatus()
      {
          // get the current process
          Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

          // get the physical mem usage (this only runs between missions)
          Cache.Instance.TotalMegaBytesOfMemoryUsed = ((currentProcess.WorkingSet64 / 1024) / 1024);
          Logging.Log("Questor: EVE instance: totalMegaBytesOfMemoryUsed - " +
                      Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");

          // If Questor window not visible, schedule a restart of questor in the uplink so that the GUI will start normally
          
          /*
           * 
           if (!m_Parent.Visible)
          //GUI isn't visible and CloseQuestorflag is true, so that his code block only runs once
          {
              //m_Parent.Visible = true; //this does not work for some reason - innerspace issue?
              Cache.Instance.ReasonToStopQuestor =
                  "The Questor GUI is not visible: did EVE get restarted due to a crash or lag?";
              Logging.Log(Cache.Instance.ReasonToStopQuestor);
              Cache.Instance.CloseQuestorCMDLogoff = false;
              Cache.Instance.CloseQuestorCMDExitGame = true;
              Cache.Instance.SessionState = "Exiting";
              BeginClosingQuestor();
          }
          else 
          
           */

          if (Cache.Instance.TotalMegaBytesOfMemoryUsed > (Settings.Instance.EVEProcessMemoryCeiling - 50) &&
                      Settings.Instance.EVEProcessMemoryCeilingLogofforExit != "")
          {
              Logging.Log(
                  "Questor: Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " +
                  Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");
              Cache.Instance.ReasonToStopQuestor =
                  "Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " +
                  Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB";
              if (Settings.Instance.EVEProcessMemoryCeilingLogofforExit == "logoff")
              {
                  Cache.Instance.CloseQuestorCMDLogoff = true;
                  Cache.Instance.CloseQuestorCMDExitGame = false;
                  Cache.Instance.SessionState = "LoggingOff";
                  BeginClosingQuestor();
                  return;
              }
              if (Settings.Instance.EVEProcessMemoryCeilingLogofforExit == "exit")
              {
                  Cache.Instance.CloseQuestorCMDLogoff = false;
                  Cache.Instance.CloseQuestorCMDExitGame = true;
                  Cache.Instance.SessionState = "Exiting";
                  BeginClosingQuestor();
                  return;
              }
              Logging.Log(
                  "Questor: EVEProcessMemoryCeilingLogofforExit was not set to exit or logoff - doing nothing ");
          }
          else
          {
              Cache.Instance.SessionState = "Running";
          }
      }

      public void ProcessState()
      {
         //Cleanup State should only run every 10 seconds
         if (DateTime.Now.Subtract(_lastCleanupAction).TotalSeconds < 10)
              return;

         _lastCleanupAction = DateTime.Now;
                        
         switch (State)
         {
            case CleanupState.Idle:
               State = CleanupState.CheckModalWindows;
               break;

            case CleanupState.CheckModalWindows:
               //
               // go through *every* window
               //
               foreach (DirectWindow window in Cache.Instance.Windows)
               {
                  // Telecom messages are generally mission info messages: close them
                  if (window.Name == "telecom")
                  {
                     Logging.Log("Cleanup: Closing telecom message...");
                     Logging.Log("Cleanup: Content of telecom window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") + "]");
                     window.Close();
                  }

                  // Modal windows must be closed
                  // But lets only close known modal windows
                  if (window.Name == "modal")
                  {
                     bool close = false;
                     bool restart = false;
                     bool gotobasenow = false;
                     bool sayyes = false;
                     //bool sayno = false;
                     if (!string.IsNullOrEmpty(window.Html))
                     {
                        // Server going down /unscheduled/ potentially very soon!
                        // CCP does not reboot in the middle of the day because the server is behaving
                        // dock now to avoid problems
                        gotobasenow |= window.Html.Contains("for a short unscheduled reboot");

                        // Server going down
                        close |= window.Html.Contains("Please make sure your characters are out of harm");
                        close |= window.Html.Contains("the servers are down for 30 minutes each day for maintenance and updates");
                        if (window.Html.Contains("The socket was closed"))
                        {
                           Logging.Log("Cleanup: This window indicates we are disconnected: Content of modal window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") + "]");
                           //Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdLogOff); //this causes the questor window to not re-appear
                           Cache.Instance.CloseQuestorCMDLogoff = false;
                           Cache.Instance.CloseQuestorCMDExitGame = true;
                           Cache.Instance.ReasonToStopQuestor = "The socket was closed";
                           Cache.Instance.SessionState = "Quitting";
                           break;
                        }

                        // In space "shit"
                        close |= window.Html.Contains("Item cannot be moved back to a loot container.");
                        close |= window.Html.Contains("you do not have the cargo space");
                        close |= window.Html.Contains("cargo units would be required to complete this operation.");
                        close |= window.Html.Contains("You are too far away from the acceleration gate to activate it!");
                        close |= window.Html.Contains("maximum distance is 2500 meters");
                        // Stupid warning, lets see if we can find it
                        close |= window.Html.Contains("Do you wish to proceed with this dangerous action?");
                        // Yes we know the mission is not complete, Questor will just redo the mission
                        close |= window.Html.Contains("Please check your mission journal for further information.");
                        close |= window.Html.Contains("weapons in that group are already full");
                        close |= window.Html.Contains("You have to be at the drop off location to deliver the items in person");
                        // Lag :/
                        close |= window.Html.Contains("This gate is locked!");
                        close |= window.Html.Contains("The Zbikoki's Hacker Card");
                        close |= window.Html.Contains(" units free.");
                        close |= window.Html.Contains("already full");
                        //
                        // restart the client if these are encountered
                        //
                        restart |= window.Html.Contains("Local cache is corrupt");
                        restart |= window.Html.Contains("Local session information is corrupt");
                        restart |= window.Html.Contains("The connection to the server was closed"); 										//CONNECTION LOST
                        restart |= window.Html.Contains("server was closed");  																//CONNECTION LOST
                        restart |= window.Html.Contains("The socket was closed"); 															//CONNECTION LOST
                        restart |= window.Html.Contains("The connection was closed"); 														//CONNECTION LOST
                        restart |= window.Html.Contains("Connection to server lost"); 														//INFORMATION
                        restart |= window.Html.Contains("The user connection has been usurped on the proxy"); 								//CONNECTION LOST
                        restart |= window.Html.Contains("The transport has not yet been connected, or authentication was not successful"); 	//CONNECTION LOST
                        restart |= window.Html.Contains("Your client has waited"); //SOUL-CRUSHING LAG - Your client has waited x minutes for a remote call to complete.
                        restart |= window.Html.Contains("This could mean the server is very loaded"); //SOUL-CRUSHING LAG - Your client has waited x minutes for a remote call to complete.
                        //
                        // Modal Dialogs the need "yes" pressed
                        //
                        sayyes |= window.Html.Contains("objectives requiring a total capacity");
                        sayyes |= window.Html.Contains("your ship only has space for");
                        //
                        // Modal Dialogs the need "no" pressed
                        //
                        //sayno |= window.Html.Contains("Do you wish to proceed with this dangerous action
                     }
                     if (sayyes)
                     {
                        Logging.Log("Cleanup: Found a window that needs 'yes' chosen...");
                        Logging.Log("Cleanup: Content of modal window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") + "]");
                        window.AnswerModal("Yes");
                        continue;
                     }
                     if (close)
                     {
                        Logging.Log("Cleanup: Closing modal window...");
                        Logging.Log("Cleanup: Content of modal window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") + "]");
                        window.Close();
                        continue;
                     }

                     if (restart)
                     {
                        Logging.Log("Cleanup: Restarting eve...");
                        Logging.Log("Cleanup: Content of modal window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") + "]");
                        Cache.Instance.CloseQuestorCMDLogoff = false;
                        Cache.Instance.CloseQuestorCMDExitGame = true;
                        Cache.Instance.ReasonToStopQuestor = "A message from ccp indicated we were disconnected";
                        Cache.Instance.SessionState = "Quitting";
                        Settings.Instance.SecondstoWaitAfterExteringCloseQuestorBeforeExitingEVE = 30;
                        window.Close();
                        continue;
                     }
                     if (gotobasenow)
                     {
                        Logging.Log("Cleanup: Evidentially the cluster is dieing... and CCP is restarting the server");
                        Logging.Log("Cleanup: Content of modal window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") + "]");
                        Cache.Instance.GotoBaseNow = true;
                        Settings.Instance.AutoStart = false;
                        //
                        // do not close eve, let the shutdown of the server do that
                        //
                        //Cache.Instance.CloseQuestorCMDLogoff = false;
                        //Cache.Instance.CloseQuestorCMDExitGame = true;
                        //Cache.Instance.ReasonToStopQuestor = "A message from ccp indicated we were disconnected";
                        //Cache.Instance.SessionState = "Quitting";
                        window.Close();
                        continue;
                     }
                  }
               }
               State = CleanupState.CheckWindowsThatDontBelongInSpace;
               break;
            
             case CleanupState.CheckWindowsThatDontBelongInSpace:
               if (Cache.Instance.InSpace)
               {
                   if (Settings.Instance.UseDrones && (Cache.Instance.DirectEve.ActiveShip.GroupId != 31 && Cache.Instance.DirectEve.ActiveShip.GroupId != 28 && Cache.Instance.DirectEve.ActiveShip.GroupId != 380))
                   {
                       _lastCleanupAction = DateTime.Now;
                       // Close the drone bay, its not required in space.
                       if (Cache.Instance.DroneBay != null && Cache.Instance.DroneBay.IsReady)
                       {
                           Logging.Log("Cleanup: Closing Drone Bay Window as it is not useful in space.");
                           Cache.Instance.DroneBay.Window.Close();
                       }
                   }
               }
               State = CleanupState.Done;
               break;
            
             case CleanupState.Done:
               _lastCleanupAction = DateTime.Now;
               State = CleanupState.Idle;
               break;

            default:
               // Next state
               State = CleanupState.Idle;
               break;
         }
      }
   }
}