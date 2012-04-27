// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using Questor.Modules.Caching;
using Questor.Modules.Lookup;

namespace Questor.Modules.Logging
{
    using System;
    using InnerSpaceAPI;
    using System.IO;

    public static class Logging
    {
        //list of colors
        public const string green = "\ag";    //traveler mission control
        public const string yellow = "\ay";
        public const string blue = "\ab";
        public const string red = "\ar";      //error panic
        public const string orange = "\ao";   //error can fix
        public const string purple = "\ap";   //combat
        public const string magenta = "\am";  //drones
        public const string teal = "\at";     //log debug
        public const string white = "\aw";    //questor 

        //public  void Log(string format, params object[] parms)
        //{
        //    string line = string.Format(format, parms);
        //    Log(line);
        //}

        //public  void Log(string line)
        public static void Log(string line, string color = Logging.white)
        {
            //InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now, color + line));
            InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now, line));
            Cache.Instance.ExtConsole += string.Format("{0:HH:mm:ss} {1}", DateTime.Now, line + "\r\n");
            Cache.Instance.ConsoleLog += string.Format("{0:HH:mm:ss} {1}", DateTime.Now, line + "\r\n");
            if (Settings.Instance.SaveConsoleLog)
            {
                if (!Cache.Instance.ConsoleLogOpened)
                {
                    if (Settings.Instance.ConsoleLogPath != null && Settings.Instance.ConsoleLogFile != null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Settings.Instance.ConsoleLogFile));
                        if (Directory.Exists(Path.GetDirectoryName(Settings.Instance.ConsoleLogFile)))
                        {
                            line = "Questor: Writing to Daily Console Log ";
                            InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now, line));

                            Cache.Instance.ExtConsole += string.Format("{0:HH:mm:ss} {1}", DateTime.Now, line + "\r\n");
                            Cache.Instance.ConsoleLog += string.Format("{0:HH:mm:ss} {1}", DateTime.Now, line + "\r\n");
                            Cache.Instance.ConsoleLogOpened = true;
                            line = "";
                        }
                        else
        {
                            InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now, "Logging: Unable to find (or create): " + Settings.Instance.ConsoleLogPath));
                        }
                    }
                }
                if (Cache.Instance.ConsoleLogOpened)
                {
                    File.AppendAllText(Settings.Instance.ConsoleLogFile, Cache.Instance.ConsoleLog);
                    Cache.Instance.ConsoleLog = null;
                }

        }
    }
    }
}