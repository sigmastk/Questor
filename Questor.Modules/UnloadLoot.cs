// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------
namespace Questor.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DirectEve;

    public class UnloadLoot
    {
        public const int StationContainer = 17366;

        private DateTime _nextUnloadAction = DateTime.MinValue;
        private DateTime _lastUnloadAction = DateTime.MinValue;

        public UnloadLootState State { get; set; }
        //public double LootValue { get; set; }

        public void ProcessState()
        {
            switch (State)
            {
                case UnloadLootState.Idle:
                case UnloadLootState.Done:
                    break;

                case UnloadLootState.Begin:
                    if (!Cache.OpenCargoHold("UnloadLoot")) break;
                    if (Cache.Instance.CargoHold.Items.Count == 0 && Cache.Instance.CargoHold.IsValid)
                        State = UnloadLootState.Done;
                    else
                        State = UnloadLootState.OpenLootHangar;
                    break;

                case UnloadLootState.OpenLootHangar:
                    // Is the hangar open?
                    //Logging.Log("UnloadLoot: Opening Loot Hangar");
                    if(!Cache.OpenLootHangar("UnloadLoot")) break;

                    State = UnloadLootState.OpenShipsCargo;
                    break;

                case UnloadLootState.OpenShipsCargo:
                    // Is cargo open?
                    //Logging.Log("UnloadLoot: Opening Ships Hangar");
                    if (!Cache.OpenCargoHold("UnloadLoot")) break;

                    if (Cache.Instance.CorpAmmoHangar != null)
                    {
                        //Logging.Log("UnloadLoot: Opening corporation hangar");
                        State = UnloadLootState.OpenAmmoHangar;
                    }
                    else if (Settings.Instance.MoveCommonMissionCompletionItemsToAmmoHangar == true)
                    {
                        //Logging.Log("UnloadLoot: Moving CommonMissionItems to AmmoHangar");
                        State = UnloadLootState.MoveCommonMissionCompletionItemsToAmmoHangar;
                    }
                    else if (Settings.Instance.MoveCommonMissionCompletionItemsToAmmoHangar == false)
                    {
                        //Logging.Log("UnloadLoot: CommonMissionCompletionitems");
                        State = UnloadLootState.MoveCommonMissionCompletionitems;
                    }
                    break;

                case UnloadLootState.OpenAmmoHangar:
                    // Is cargo open?
                    if (!Cache.OpenAmmoHangar("UnloadLoot")) break;

                    if (Settings.Instance.MoveCommonMissionCompletionItemsToAmmoHangar == true)
                    {
                        Logging.Log("UnloadLoot: Moving Common Mission Completion items to Corporate Ammo Hangar");
                        State = UnloadLootState.MoveCommonMissionCompletionItemsToAmmoHangar;
                    }
                    else if (Settings.Instance.MoveCommonMissionCompletionItemsToAmmoHangar == false)
                    {
                        Logging.Log("UnloadLoot: Moving Common Mission Completion items to to Local Hangar");
                    State = UnloadLootState.MoveCommonMissionCompletionitems;
                    }
                    break;

                case UnloadLootState.MoveCommonMissionCompletionItemsToAmmoHangar:
                    //
                    // how do we get IsMissionItem to work for us here? (see ItemCache)
                    // Zbikoki's Hacker Card 28260, Reports 3814, Gate Key 2076, Militants 25373, Marines 3810, i.groupid == 314 (Misc Mission Items, mainly for storylines) and i.GroupId == 283 (Misc Mission Items, mainly for storylines)
                    //
                    IEnumerable<DirectItem> itemsToMove = Cache.Instance.CargoHold.Items.Where(i => i.TypeId == 17192 || i.TypeId == 2076 || i.TypeId == 3814 || i.TypeId == 17206 || i.TypeId == 28260 || i.GroupId == 283 || i.GroupId == 314);
                    
                    if (Cache.Instance.AmmoHangar != null) Cache.Instance.AmmoHangar.Add(itemsToMove);
                    //_nextUnloadAction = DateTime.Now.AddSeconds((int)Settings.Instance.random_number3_5());
                    State = UnloadLootState.MoveLoot;
                    break;

                case UnloadLootState.MoveCommonMissionCompletionitems:
                    if (!Cache.OpenCargoHold("UnloadLoot")) return;
                    if (!Cache.OpenItemsHangar("UnloadLoot")) return;
                    //
                    // how do we get IsMissionItem to work for us here? (see ItemCache)
                    // Zbikoki's Hacker Card 28260, Reports 3814, Gate Key 2076, Militants 25373, Marines 3810, i.groupid == 314 (Misc Mission Items, mainly for storylines) and i.GroupId == 283 (Misc Mission Items, mainly for storylines)
                    //
                    IEnumerable<DirectItem> itemsToMove2 = Cache.Instance.CargoHold.Items.Where(i => i.TypeId == 17192 || i.TypeId == 2076 || i.TypeId == 3814 || i.TypeId == 17206 || i.TypeId == 28260 || i.GroupId == 283 || i.GroupId == 314);
                    
                    Cache.Instance.ItemHangar.Add(itemsToMove2);
                    //_nextUnloadAction = DateTime.Now.AddSeconds((int)Settings.Instance.random_number3_5());
                    State = UnloadLootState.MoveLoot;
                    break;

                case UnloadLootState.MoveLoot:
                    if (!Cache.OpenCargoHold("UnloadLoot")) return;
                    if (!Cache.OpenLootHangar("UnloadLoot")) return;
                    if (!Cache.OpenLootContainer("UnloadLoot")) return;

                    IEnumerable<DirectItem> lootToMove = Cache.Instance.CargoHold.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() != Cache.Instance.BringMissionItem && !Settings.Instance.Ammo.Any(a => a.TypeId == i.TypeId));
                    foreach (DirectItem item in lootToMove)
                    {
                        if (!Cache.Instance.InvTypesById.ContainsKey(item.TypeId))
                            continue;

                        InvType invType = Cache.Instance.InvTypesById[item.TypeId];
                        Statistics.Instance.LootValue += (int)(invType.MedianBuy ?? 0)*Math.Max(item.Quantity, 1);
                    }

                    // Move loot to the loot hangar
                    Cache.Instance.LootHangar.Add(lootToMove);
                    Logging.Log("UnloadLoot: Loot was worth an estimated [" + Statistics.Instance.LootValue.ToString("#,##0") + "] isk in buy-orders");

                    //Move bookmarks to the bookmarks hangar
                    if (!string.IsNullOrEmpty(Settings.Instance.BookmarkHangar) && Settings.Instance.CreateSalvageBookmarks)
                    {
                        Logging.Log("UnloadLoot: Creating salvage bookmarks in hangar");
                        List<DirectBookmark> bookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                        var salvageBMs = new List<long>();
                        if (bookmarks.Any())
                        {
                            foreach (DirectBookmark bookmark in bookmarks)
                            {
                                if (bookmark.BookmarkId != null) salvageBMs.Add((long) bookmark.BookmarkId);
                                if (salvageBMs.Count == 5)
                                {
                                    Cache.Instance.ItemHangar.AddBookmarks(salvageBMs);
                                    salvageBMs.Clear();
                                }
                            }
                            if (salvageBMs.Count > 0)
                            {
                                Cache.Instance.ItemHangar.AddBookmarks(salvageBMs);
                                salvageBMs.Clear();
                            }
                        }
                    }
                    State = UnloadLootState.MoveAmmo;
                    break;

                case UnloadLootState.MoveAmmo:
                    if (!Cache.OpenAmmoHangar("UnloadLoot")) return;

                    Logging.Log("UnloadLoot: Moving Ammo to AmmoHangar [" + Cache.Instance.AmmoHangar.Window.Name + "]");
                    // Move the mission item & ammo to the ammo hangar
                    Cache.Instance.AmmoHangar.Add(Cache.Instance.CargoHold.Items.Where(i => ((i.TypeName ?? string.Empty).ToLower() == Cache.Instance.BringMissionItem || Settings.Instance.Ammo.Any(a => a.TypeId == i.TypeId))));
                    Logging.Log("UnloadLoot: Waiting for items to move");
                    State = UnloadLootState.WaitForMove;
                    _nextUnloadAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                    _lastUnloadAction = DateTime.Now;
                    break;

                case UnloadLootState.WaitForMove:
                    if (!Cache.OpenCargoHold("UnloadLoot")) return;
                    
                    if (Cache.Instance.CargoHold.Items.Count != 0)
                    {
                        //Logging.Log("Unloadloot: WaitForMove: 1");
                        _nextUnloadAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                        _lastUnloadAction = DateTime.Now;
                        break;
                    }

                    // Wait x seconds after moving
                    if (DateTime.Now < _nextUnloadAction)
                        break;

                    if (Cache.Instance.DirectEve.GetLockedItems().Count == 0)
                    {
                        if (Cache.Instance.CorpBookmarkHangar != null && Settings.Instance.CreateSalvageBookmarks)
                        {
                            Logging.Log("UnloadLoot: Moving salvage bookmarks to corporate hangar");
                            Cache.Instance.CorpBookmarkHangar.Add(Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == 51));
                        }
                        _nextUnloadAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                        Logging.Log("UnloadLoot: Stacking items in Items Hangar: resuming in [ " + Math.Round(_nextUnloadAction.Subtract(DateTime.Now).TotalSeconds,0) + " sec ]");
                        State = UnloadLootState.StackItemsHangar;
                        break;
                    }

                    if (DateTime.Now.Subtract(_lastUnloadAction).TotalSeconds > 120)
                    {
                        Logging.Log("UnloadLoot: Moving items timed out, clearing item locks");
                        Cache.Instance.DirectEve.UnlockItems();

                        Logging.Log("UnloadLoot: Stacking items in Items Hangar");
                        State = UnloadLootState.StackItemsHangar;
                        break;
                    }
                    break;

                case UnloadLootState.StackItemsHangar:
                    // Don't stack until 5 seconds after the cargo has cleared
                    if (DateTime.Now < _nextUnloadAction)
                        break;

                    // Stack everything
                    if (Cache.Instance.CorpAmmoHangar == null || Cache.Instance.CorpLootHangar == null || Cache.Instance.LootContainer == null) // Only stack if we moved something
                    {
                        Cache.Instance.ItemHangar.StackAll();
                        _nextUnloadAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                    }

                    State = UnloadLootState.StackItemsCorpAmmo;
                    break;

                case UnloadLootState.StackItemsCorpAmmo:
                    if (Settings.Instance.AmmoHangar != string.Empty)
                    {
                        // Don't stack until 5 seconds after the cargo has cleared
                        if (DateTime.Now < _nextUnloadAction)
                            break;

                        // Stack everything
                        if (((Cache.Instance.CorpAmmoHangar != null) && Cache.Instance.CorpAmmoHangar.IsValid) && Cache.Instance.CorpAmmoHangar.IsReady)
                        {
                            Cache.Instance.CorpAmmoHangar.StackAll();
                            _nextUnloadAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                        }
                    }
                    State = UnloadLootState.StackItemsCorpLoot;
                    break;

                case UnloadLootState.StackItemsCorpLoot:
                    if (Settings.Instance.LootHangar != string.Empty)
                    {
                        // Don't stack until 5 seconds after the cargo has cleared
                        if (DateTime.Now < _nextUnloadAction)
                            break;

                        // Stack everything
                        if (((Cache.Instance.CorpLootHangar != null) && Cache.Instance.CorpLootHangar.IsValid) && Cache.Instance.CorpLootHangar.IsReady)
                        {
                            Cache.Instance.CorpLootHangar.StackAll();
                            _nextUnloadAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                        }
                    }
                    State = UnloadLootState.StackItemsLootContainer;
                    break;

                case UnloadLootState.StackItemsLootContainer:
                    if (Settings.Instance.LootContainer != string.Empty)
                    {
                        // Don't stack until 5 seconds after the cargo has cleared
                        if (DateTime.Now < _nextUnloadAction)
                            break;

                        // Stack everything
                        if (((Cache.Instance.LootContainer != null) && Cache.Instance.LootContainer.IsValid) && Cache.Instance.LootContainer.IsReady)
                        {
                            Cache.Instance.LootContainer.StackAll();
                            _nextUnloadAction = DateTime.Now.AddSeconds(Cache.Instance.RandomNumber(3, 5));
                            _lastUnloadAction = DateTime.Now;
                            Logging.Log("UnloadLoot: Stacking items in loot container: resuming in [ " + DateTime.Now.Subtract(_nextUnloadAction).TotalSeconds + " sec ]");
                        }
                    }
                    State = UnloadLootState.WaitForStacking;
                    break;

                case UnloadLootState.WaitForStacking:
                    // Wait 5 seconds after stacking
                    if (DateTime.Now < _nextUnloadAction)
                        break;

                    if (Cache.Instance.DirectEve.GetLockedItems().Count == 0)
                    {
                        Logging.Log("UnloadLoot: Done");
                        State = UnloadLootState.Done;
                        break;
                    }

                    if (DateTime.Now.Subtract(_lastUnloadAction).TotalSeconds > 120)
                    {
                        Logging.Log("UnloadLoot: Stacking items timed out, clearing item locks");
                        Cache.Instance.DirectEve.UnlockItems();

                        Logging.Log("UnloadLoot: Done");
                        State = UnloadLootState.Done;
                        break;
                    }
                    break;
            }
        }
    }
}