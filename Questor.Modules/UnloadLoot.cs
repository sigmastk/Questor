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
            DirectContainer cargo = Cache.Instance.DirectEve.GetShipsCargo();
            DirectContainer itemshangar = Cache.Instance.DirectEve.GetItemHangar();

            DirectContainer corpAmmoHangar = null;
            if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))
                corpAmmoHangar = Cache.Instance.DirectEve.GetCorporationHangar(Settings.Instance.AmmoHangar);

            DirectContainer corpLootHangar = null;
            if (!string.IsNullOrEmpty(Settings.Instance.LootHangar))
                corpLootHangar = Cache.Instance.DirectEve.GetCorporationHangar(Settings.Instance.LootHangar);

            DirectContainer lootContainer = null;
            if(!string.IsNullOrEmpty(Settings.Instance.LootContainer))
            {
               var firstlootcontainer = itemshangar.Items.FirstOrDefault(i => i.GivenName != null && i.GivenName.ToLower() == Settings.Instance.LootContainer.ToLower());
               if (firstlootcontainer != null)
               {
                  long lootContainerID = firstlootcontainer.ItemId;
                  lootContainer = Cache.Instance.DirectEve.GetContainer(lootContainerID);
               }
               else
               {
                  Logging.Log("UnloadLoot: unable to find LootContainer named [ " + Settings.Instance.LootContainer.ToLower() + " ]");
                  var firstothercontainer = itemshangar.Items.FirstOrDefault(i => i.GivenName != null);
                  if (firstothercontainer != null)
                  {
                     Logging.Log("UnloadLoot: we did however find a container named [ " + firstothercontainer.GivenName + " ]");
                  }
               }
            }

           DirectContainer corpBookmarkHangar = null;
            if (!string.IsNullOrEmpty(Settings.Instance.BookmarkHangar))
                corpBookmarkHangar = Cache.Instance.DirectEve.GetCorporationHangar(Settings.Instance.BookmarkHangar);

            switch (State)
            {
                case UnloadLootState.Idle:
                case UnloadLootState.Done:
                    break;

                case UnloadLootState.Begin:
                    if (cargo.Items.Count == 0 && cargo.IsValid)
                        State = UnloadLootState.Done;
                    else
                        State = UnloadLootState.OpenItemHangar;
                    break;

                case UnloadLootState.OpenItemHangar:
                    // Is the hangar open?
                    if (itemshangar.Window == null)
                    {
                        // No, command it to open
                        Logging.Log("UnloadLoot: Opening station hangar");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                        break;
                    }

                    // Not ready yet
                    if (!itemshangar.IsReady)
                        break;

                    State = UnloadLootState.OpenShipsCargo;
                    break;

                case UnloadLootState.OpenShipsCargo:
                    // Is cargo open?
                    if (cargo.Window == null)
                    {
                        // No, command it to open
                        Logging.Log("UnloadLoot: Opening ship's cargo hold");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
                        break;
                    }

                    if (!cargo.IsReady)
                        break;

                    if (corpAmmoHangar != null || corpLootHangar != null)
                    {
                        //Logging.Log("UnloadLoot: Opening corporation hangar");
                        State = UnloadLootState.OpenCorpHangar;
                    }
                    else
                    {
                        //Logging.Log("UnloadLoot: CommonMissionCompletionitems");
                        State = UnloadLootState.MoveCommonMissionCompletionitems;
                    }
                    break;

                case UnloadLootState.OpenCorpHangar:
                    // Is cargo open?
                    DirectContainer corpHangar = corpAmmoHangar ?? corpLootHangar;
                    if (corpHangar != null)
                    {
                        if (corpHangar.Window == null)
                        {
                            // No, command it to open
                            Logging.Log("UnloadLoot: Opening corporation hangar");
                            Cache.Instance.DirectEve.OpenCorporationHangar();
                            break;
                        }

                        if (!corpHangar.IsReady)
                            break;
                    }
                    Logging.Log("UnloadLoot: Moving CommonMissionCompletionitems");
                    State = UnloadLootState.MoveCommonMissionCompletionitems;
                    break;

                case UnloadLootState.MoveCommonMissionCompletionitems:
                    DirectContainer commonMissionCompletionItemHangar = itemshangar;
                    //
                    // how do we get IsMissionItem to work for us here? (see ItemCache)
                    // Zbikoki's Hacker Card 28260, Reports 3814, Gate Key 2076, Militants 25373, Marines 3810, i.groupid == 314 (Misc Mission Items, mainly for storylines) and i.GroupId == 283 (Misc Mission Items, mainly for storylines)
                    //
                    IEnumerable<DirectItem> itemsToMove = cargo.Items.Where(i => i.TypeId == 17192 || i.TypeId == 2076 || i.TypeId == 3814 || i.TypeId == 17206 || i.TypeId == 28260 || i.GroupId == 283 || i.GroupId == 314);
                    
                    Logging.Log("UnloadLoot: Moving Common Mission Completion items");
                    commonMissionCompletionItemHangar.Add(itemsToMove);
                    //_nextUnloadAction = DateTime.Now.AddSeconds((int)Settings.Instance.random_number3_5());
                    State = UnloadLootState.MoveLoot;
                    break;

                case UnloadLootState.MoveLoot:
                    DirectContainer lootHangar = corpLootHangar ?? lootContainer ?? itemshangar;
                    IEnumerable<DirectItem> lootToMove = cargo.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() != Cache.Instance.BringMissionItem && !Settings.Instance.Ammo.Any(a => a.TypeId == i.TypeId));
                    foreach (DirectItem item in lootToMove)
                    {
                        if (!Cache.Instance.InvTypesById.ContainsKey(item.TypeId))
                            continue;

                        InvType invType = Cache.Instance.InvTypesById[item.TypeId];
                        Statistics.Instance.LootValue += (int)(invType.MedianBuy ?? 0)*Math.Max(item.Quantity, 1);
                    }

                    // Move loot to the loot hangar
                    lootHangar.Add(lootToMove);
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
                                    itemshangar.AddBookmarks(salvageBMs);
                                    salvageBMs.Clear();
                                }
                            }
                            if (salvageBMs.Count > 0)
                            {
                                itemshangar.AddBookmarks(salvageBMs);
                                salvageBMs.Clear();
                            }
                        }
                    }
                    State = UnloadLootState.MoveAmmo;
                    break;

                case UnloadLootState.MoveAmmo:
                    DirectContainer ammoHangar = corpAmmoHangar ?? itemshangar;

                    // Move the mission item & ammo to the ammo hangar
                    Logging.Log("UnloadLoot: Moving ammo");
                    ammoHangar.Add(cargo.Items.Where(i => ((i.TypeName ?? string.Empty).ToLower() == Cache.Instance.BringMissionItem || Settings.Instance.Ammo.Any(a => a.TypeId == i.TypeId))));
                    Logging.Log("UnloadLoot: Waiting for items to move");
                    State = UnloadLootState.WaitForMove;
                    _nextUnloadAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To5());
                    _lastUnloadAction = DateTime.Now;
                    break;

                case UnloadLootState.WaitForMove:
                    if (cargo.Items.Count != 0)
                    {
                        _nextUnloadAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To5());
                        _lastUnloadAction = DateTime.Now;
                        break;
                    }

                    // Wait x seconds after moving
                    if (DateTime.Now < _nextUnloadAction)
                        break;

                    if (Cache.Instance.DirectEve.GetLockedItems().Count == 0)
                    {
                        if (corpBookmarkHangar != null && Settings.Instance.CreateSalvageBookmarks)
                        {
                            Logging.Log("UnloadLoot: Moving salvage bookmarks to corp hangar");
                            corpBookmarkHangar.Add(itemshangar.Items.Where(i => i.TypeId == 51));
                        }
                        _nextUnloadAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber5To7());
                        Logging.Log("UnloadLoot: Stacking items: waiting until [ " + _nextUnloadAction.ToString("HH:mm:ss") + " ] to continue");
                        State = UnloadLootState.StackItemsHangar;
                        break;
                    }

                    if (DateTime.Now.Subtract(_lastUnloadAction).TotalSeconds > 120)
                    {
                        Logging.Log("UnloadLoot: Moving items timed out, clearing item locks");
                        Cache.Instance.DirectEve.UnlockItems();

                        Logging.Log("UnloadLoot: Stacking items");
                        State = UnloadLootState.StackItemsHangar;
                        break;
                    }
                    break;

                case UnloadLootState.StackItemsHangar:
                    // Don't stack until 5 seconds after the cargo has cleared
                    if (DateTime.Now < _nextUnloadAction)
                        break;

                    // Stack everything
                    if(corpAmmoHangar == null || corpLootHangar == null || lootContainer == null) // Only stack if we moved something
                    {
                        itemshangar.StackAll();
                        _nextUnloadAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To5());
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
                        if (corpAmmoHangar != null)
                        {
                            corpAmmoHangar.StackAll();
                            _nextUnloadAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To5());
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
                        if (corpLootHangar != null)
                        {
                            corpLootHangar.StackAll();
                            _nextUnloadAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To5());
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
                        if (lootContainer != null)
                        {
                            lootContainer.StackAll();
                            _nextUnloadAction = DateTime.Now.AddSeconds(Settings.Instance.RandomNumber3To5());
                            _lastUnloadAction = DateTime.Now;
                            Logging.Log("UnloadLoot: Stacking items: waiting until [ " + _nextUnloadAction.ToString("HH:mm:ss") + " ] to continue");
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