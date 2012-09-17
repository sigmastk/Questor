﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System.Drawing;
using System.Windows.Forms;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;

namespace Questor.Modules.Logging
{
    using System;
    using InnerSpaceAPI;
    using System.IO;
    using LavishScriptAPI;

    public static class Logging
    {
        //list of colors
        public const string green = "\ag";    //traveler mission control
        public const string yellow = "\ay";
        public const string blue = "\ab";     //DO NOT USE - blends into default lavish GUIs background.
        public const string red = "\ar";      //error panic
        public const string orange = "\ao";   //error can fix
        public const string purple = "\ap";   //combat
        public const string magenta = "\am";  //drones
        public const string teal = "\at";     //log debug
        public const string white = "\aw";    //questor

        //public  void Log(string line)
        //public static void Log(string module, string line, string color = Logging.white)
        public static void Log(string module, string line, string color)
        {
            string colorLogLine = line;
            //colorLogLine contains color and is for the InnerSpace console
            InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now, Logging.orange + "[" + Logging.yellow + module + Logging.orange + "] " + color + colorLogLine));                            //Innerspace Console Log

            string plainLogLine = FilterColorsFromLogs(line);
            //plainLogLine contains plain text and is for the log file and the GUI console (why cant the GUI be made to use color too?)
            Cache.Instance.ExtConsole += string.Format("{0:HH:mm:ss} {1}", DateTime.Now, "[" + module + "] " + plainLogLine + "\r\n");               //Questor GUI Console Log
            Cache.Instance.ConsoleLog += string.Format("{0:HH:mm:ss} {1}", DateTime.Now, "[" + module + "] " + plainLogLine + "\r\n");               //In memory Console Log
            Cache.Instance.ConsoleLogRedacted += string.Format("{0:HH:mm:ss} {1}", DateTime.Now, "[" + module + "] " + FilterSensitiveInfo(plainLogLine) + "\r\n");  //In memory Console Log with sensitive info redacted
            if (Settings.Instance.SaveConsoleLog)
            {
                if (!Cache.Instance.ConsoleLogOpened)
                {
                    if (Settings.Instance.ConsoleLogPath != null && Settings.Instance.ConsoleLogFile != null)
                    {
                        module = "Logging";
                        line = "Writing to Daily Console Log ";
                        if (Settings.Instance.InnerspaceGeneratedConsoleLog) 
                        {
                            InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now, "log " + Settings.Instance.ConsoleLogFile + "-innerspace-generated.log"));
                            LavishScript.ExecuteCommand("log " + Settings.Instance.ConsoleLogFile + "-innerspace-generated.log");
                        }
                        InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now, Logging.orange + "[" + Logging.yellow + module + Logging.orange + "] " + color + colorLogLine));                            //Innerspace Console Log
                        Cache.Instance.ExtConsole += string.Format("{0:HH:mm:ss} {1}", DateTime.Now, plainLogLine + "\r\n");

                        if (!string.IsNullOrEmpty(Settings.Instance.ConsoleLogFile))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(Settings.Instance.ConsoleLogFile));
                            if (Directory.Exists(Path.GetDirectoryName(Settings.Instance.ConsoleLogFile)))
                            {
                                Cache.Instance.ConsoleLog += string.Format("{0:HH:mm:ss} {1}", DateTime.Now,
                                                                           "[" + module + "]" + plainLogLine + "\r\n");
                                Cache.Instance.ConsoleLogOpened = true;
                            }
                            else
                            {
                                InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now,
                                                              "Logging: Unable to find (or create): " +
                                                              Settings.Instance.ConsoleLogPath));
                            }
                            line = "";
                        }
                        else
                        {
                            line = "Logging: Unable to write log to file yet as: ConsoleLogFile is not yet defined";
                            InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTime.Now, colorLogLine));
                            Cache.Instance.ExtConsole += string.Format("{0:HH:mm:ss} {1}", DateTime.Now, "[" + module + "] " + plainLogLine + "\r\n");
                        }
                    }
                }
                if (Cache.Instance.ConsoleLogOpened)
                {
                    if (Settings.Instance.ConsoleLogFile != null)
                        File.AppendAllText(Settings.Instance.ConsoleLogFile, Cache.Instance.ConsoleLog);               //Write In Memory Console log to File
                    Cache.Instance.ConsoleLog = null;

                    if (Settings.Instance.ConsoleLogFileRedacted != null)
                        File.AppendAllText(Settings.Instance.ConsoleLogFileRedacted, Cache.Instance.ConsoleLogRedacted);               //Write In Memory Console log to File
                    Cache.Instance.ConsoleLogRedacted = null;
                }
            }
        }

        //path = path.Replace(Environment.CommandLine, "");
        //path = path.Replace(Environment.GetCommandLineArgs(), "");

        public static string FilterSensitiveInfo(string line)
        {
            if (line == null)
                return string.Empty;
            if (!string.IsNullOrEmpty(Settings.Instance.CharacterName))
            {
                line = line.Replace(Settings.Instance.CharacterName, "_MyEVECharacterNameRedacted_");
                line = line.Replace("/" + Settings.Instance.CharacterName, "/_MyEVECharacterNameRedacted_");
                line = line.Replace("\\" + Settings.Instance.CharacterName, "\\_MyEVECharacterNameRedacted_");
                line = line.Replace("[" + Settings.Instance.CharacterName + "]", "[_MyEVECharacterNameRedacted_]");
                line = line.Replace(Settings.Instance.CharacterName + ".xml", "_MyEVECharacterNameRedacted_.xml");
            }
            //if (!string.IsNullOrEmpty(Cache.Instance.CurrentAgent))
            //{
            //    if (Settings.Instance.DebugLogging) InnerSpace.Echo("Logging.Log: FilterSensitiveInfo: CurrentAgent exists [" + Cache.Instance.CurrentAgent + "]");
            //    line = line.Replace(" " + Cache.Instance.CurrentAgent + " ", " _MyCurrentAgentRedacted_ ");
            //    line = line.Replace("[" + Cache.Instance.CurrentAgent + "]", "[_MyCurrentAgentRedacted_]");
            //}
            //if (Cache.Instance.AgentId != -1)
            //{
            //    if(Settings.Instance.DebugLogging) InnerSpace.Echo("Logging.Log: FilterSensitiveInfo: AgentId is not -1");
            //    line = line.Replace(" " + Cache.Instance.AgentId + " ", " _MyAgentIdRedacted_ ");
            //    line = line.Replace("[" + Cache.Instance.AgentId + "]", "[_MyAgentIdRedacted_]");
            //}
            if (!string.IsNullOrEmpty(Settings.Instance.LoginCharacter))
            {
                if (Settings.Instance.DebugLogging) InnerSpace.Echo("Logging.Log: FilterSensitiveInfo: LoginCharacter is [" + Settings.Instance.LoginCharacter + "]");
                line = line.Replace(Settings.Instance.LoginCharacter, "_MyEVECharacterNameRedacted_");
            }
            if (!string.IsNullOrEmpty(Settings.Instance.LoginUsername))
            {
                if (Settings.Instance.DebugLogging) InnerSpace.Echo("Logging.Log: FilterSensitiveInfo: LoginUsername is [" + Settings.Instance.LoginUsername + "]");
                line = line.Replace(Settings.Instance.LoginUsername, "_MyLoginUserNameRedacted_");
            }
            if (!string.IsNullOrEmpty(Environment.UserName))
            {
                if (Settings.Instance.DebugLogging) InnerSpace.Echo("Logging.Log: FilterSensitiveInfo: Environment.Username is [" + Environment.UserName + "]");
                line = line.Replace("\\" + Environment.UserName + "\\", "\\_MyWindowsLoginNameRedacted_\\");
                line = line.Replace("/" + Environment.UserName + "/", "/_MyWindowsLoginNameRedacted_/");
            }
            if (!string.IsNullOrEmpty(Environment.UserDomainName))
            {
                if (Settings.Instance.DebugLogging) InnerSpace.Echo("Logging.Log: FilterSensitiveInfo: Environment.UserDomainName is [" + Environment.UserDomainName + "]");
                line = line.Replace(Environment.UserDomainName, "_MyWindowsDomainNameRedacted_");
            }
            return line;
        }

        public static class RichTextBoxExtensions
        {
            public static void AppendText(RichTextBox box, string text, Color color)
            {
                box.SelectionStart = box.TextLength;
                box.SelectionLength = 0;

                box.SelectionColor = color;
                box.AppendText(text);
                box.SelectionColor = box.ForeColor;
            }
        }

        public static string FilterColorsFromLogs(string line)
        {
            if (line == null)
                return string.Empty;

            line = line.Replace("\ag", "");
            line = line.Replace("\ay", "");
            line = line.Replace("\ab", "");
            line = line.Replace("\ar", "");
            line = line.Replace("\ao", "");
            line = line.Replace("\ap", "");
            line = line.Replace("\am", "");
            line = line.Replace("\at", "");
            line = line.Replace("\aw", "");
            while (line.IndexOf("  ", System.StringComparison.Ordinal) >= 0)
                line = line.Replace("  ", " ");
            return line.Trim();
        }
    }
}