﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pok
{
    public enum BehaviorDatabase { NEWITEM, CHANGE_QUANTITY_ITEM };
    public struct GameDatabaseInventoryEvent
    {
        public BaseItemGameInstanced item;
        public BehaviorDatabase behavior;

        public GameDatabaseInventoryEvent(BehaviorDatabase pBehavior, BaseItemGameInstanced pItem)
        {
            behavior = pBehavior;
            item = pItem;
        }

    }
    public struct AddCreatureEvent
    {
        public CreatureInstanceSaved creature;
        public string zoneid;
        public bool manualByHand;
        public int change;
        public System.Action<Creature> onCreated;
    }

    [System.Serializable]
    public class TimeModule
    {
        public System.DateTime time;
    }


    [System.Serializable]
    public class CreatureInfoSaved
    {
        public string id;
        private bool isUnLock = false;
        private int level;
        public int[] boughtNumberVariant = new int[] { 0, 0 };
        public Dictionary<string, int[]> boughtNumberVariants = new Dictionary<string, int[]>();
        public int BoughtNumber
        {
            get
            {
                if (!BoughtNumberVariant.ContainsKey(GameManager.Instance.ZoneChoosed))
                {
                    BoughtNumberVariant.Add(GameManager.Instance.ZoneChoosed,new int[] { 0, 0 });
                    ES3.markDirty(SaveDataConstraint.WORLD_INFO);
                }
                return BoughtNumberVariant[GameManager.Instance.ZoneChoosed][0] + BoughtNumberVariant[GameManager.Instance.ZoneChoosed][1]; ;
            }
        }

        public Dictionary<string, int[]> BoughtNumberVariant { get => boughtNumberVariants; }
        public bool IsUnLock { get => isUnLock; set { isUnLock = value; ES3.markDirty(SaveDataConstraint.WORLD_INFO); } }
        public int Level { get => level; set { level = value; ES3.markDirty(SaveDataConstraint.WORLD_INFO); } }
    }
    [System.Serializable]
    public class ZoneInfoSaved
    {
        [SerializeField]
        private string id;
        [SerializeField]
        private bool isUnLock;
        public Dictionary<string, string> leaderSelected = new Dictionary<string, string>();

        public void addLeader(string map, string leader)
        {
            leaderSelected[map] = leader;
            ES3.markDirty(SaveDataConstraint.WORLD_INFO);
        }
        public List<string> creatureAdded = new List<string>();
        public string curentUnlock;

        public string CurentUnlock { get => curentUnlock; set { curentUnlock = value; ES3.markDirty(SaveDataConstraint.WORLD_INFO); } }

        public bool IsUnLock { get => isUnLock; set { isUnLock = value; ES3.markDirty(SaveDataConstraint.WORLD_INFO); } }
        public string Id { get => id; set{ id = value; ES3.markDirty(SaveDataConstraint.WORLD_INFO); } }
    }
    [System.Serializable]
    public class ZoneInstanceSaved
    {
        public string id;
        public List<MapInstanceSaved> maps = new List<MapInstanceSaved>();

        public void onInit()
        {
            maps.Sort((a, b) => { return GameDatabase.Instance.MapCollection.FindIndex(x => x.ItemID == a.id).CompareTo(GameDatabase.Instance.MapCollection.FindIndex(x => x.ItemID == b.id)); });
            for (int i = 0; i < maps.Count; ++i)
            {
                maps[i].zoneParent = this;
                maps[i].onInit();
            }
        }
        public List<CreatureInstanceSaved> GetCreaturesInZone(string creature)
        {
            for (int i = 0; i < maps.Count; ++i)
            {
                var creatures = maps[i].creatures.FindAll(x => x.id == creature);
                if (creatures.Count > 0)
                {
                    return creatures;
                }
            }
            return new List<CreatureInstanceSaved>();
        }
        public int GetCreatureInZone(string creature)
        {
            int total = 0;
            for (int i = 0; i < maps.Count; ++i)
            {
                total += maps[i].creatureCount(creature);
            }
            return total;
        }
    }
    [System.Serializable]
    public class MapInstanceSaved
    {
        public string id;
        [System.NonSerialized]
        public ZoneInstanceSaved zoneParent;
        [System.NonSerialized]
        public bool isUnlocked;
        public List<CreatureInstanceSaved> creatures = new List<CreatureInstanceSaved>();
        public void onInit()
        {

            for (int i = creatures.Count - 1; i >= 0; --i)
            {
                if (!GameDatabase.Instance.CreatureCollection.Exists(x => x.ItemID == creatures[i].id))
                {
                    creatures.RemoveAt(i);
                    continue;
                }
                creatures[i].mapParent = this;
                creatures[i].onInit();
            }
        }
        public int creatureCount(string id)
        {
            return creatures.Count(x => x.id == id);
        }
    }

    [System.Serializable]
    public class CreatureInstanceSaved
    {
        public string instanceID;
        public string id;
        public int level = 1;
        public CreatureInstanceSaved autoFindParent(CreatureItem item)
        {
            mapParent = GameManager.Instance.Database.worldData.zones.Find(x => x.id == GameManager.Instance.ZoneChoosed).maps.Find(x => item.parentMap && x.id == item.parentMap.ItemID);
            return this;
        }
        public CreatureInstanceSaved autoFindParent()
        {
            var item = GameDatabase.Instance.CreatureCollection.Find(x => x.ItemID == id);
            mapParent = GameManager.Instance.Database.worldData.zones.Find(x => x.id == GameManager.Instance.ZoneChoosed).maps.Find(x => item.parentMap && x.id == item.parentMap.ItemID);
            return this;
        }
        public void onInit()
        {
        }
        [System.NonSerialized]
        public MapInstanceSaved mapParent;
    }
    [System.Serializable]
    public class PackageCreatureInstanceSaved : CreatureInstanceSaved
    {
        public string creature;
    }
    [System.Serializable]
    public class WorldDataInstanced
    {
        public List<ZoneInstanceSaved> zones = new List<ZoneInstanceSaved>();
        public void onInit()
        {
            zones.Sort((a, b) => { return GameDatabase.Instance.ZoneCollection.FindIndex(x => x.ItemID == a.id).CompareTo(GameDatabase.Instance.ZoneCollection.FindIndex(x => x.ItemID == b.id)); });
            for (int i = 0; i < zones.Count; ++i)
            {
                zones[i].onInit();

            }
        }

        public MapInstanceSaved addCreature(CreatureInstanceSaved creature, string address)
        {
            ES3.markDirty(SaveDataConstraint.WORLD_DATA);
            var zone = zones.Find(x => x.id == address);
            var zoneInfo = GameManager.Instance.Database.zoneInfos.Find(x => x.Id == address);
            var map = creature.mapParent;
            if (map == null)
            {
                var creatureInside = GameDatabase.Instance.CreatureCollection.Find(x => x.ItemID == creature.id);
                var parentFind = zone.maps.Find(x => creatureInside.parentMap && x.id == creatureInside.parentMap.ItemID);
                creature.mapParent = parentFind;
                map = parentFind;
            }
            if (map == null) return null;
            if (zone != null)
            {
                var listParent = new List<CreatureItem>();
                var creatureItem = GameDatabase.Instance.CreatureCollection.Find(x => x.ItemID == creature.id);
                listParent.Add(creatureItem);
                creatureItem.getParents(listParent);
                foreach (var parent in listParent)
                {
                    if (!zoneInfo.creatureAdded.Contains(parent.ItemID))
                    {
                        zoneInfo.creatureAdded.Add(parent.ItemID);
                    }
                }

                creature.mapParent = map;
                map.creatures.Add(creature);
                return map;
            }
            return null;
        }
    }


    [System.Serializable]
    public class GameDatabaseInstanced
    {

        //instance
        public WorldDataInstanced worldData = new WorldDataInstanced();

        public List<BaseItemGameInstanced> inventory = new List<BaseItemGameInstanced>();
        public List<TimeCounterInfo> timeRestore = new List<TimeCounterInfo>();

        //class
        public List<ZoneInfoSaved> zoneInfos = new List<ZoneInfoSaved>();
        public List<CreatureInfoSaved> creatureInfos = new List<CreatureInfoSaved>();
        

        public void addZoneInfo(ZoneInfoSaved zone)
        {
            zoneInfos.Add(zone);
        }

        public void resolveConflictData()
        {
            List<CreatureItem> allcreatures = new List<CreatureItem>();
            for (int i = 0; i < worldData.zones.Count; ++i)
            {
                var zone = worldData.zones[i];
                var zoneInfo = GameManager.Instance.Database.zoneInfos.Find(x => x.Id == zone.id);

                int rank = 0;
                for (int g = 0; g < zoneInfo.leaderSelected.Count; ++g)
                {
                    List<CreatureItem> creatures = new List<CreatureItem>();
                    GameDatabase.Instance.CreatureCollection.Find(x => x.ItemID == zoneInfo.leaderSelected.ElementAt(g).Value).getChild(creatures, 6);
                    if (g < zoneInfo.leaderSelected.Count - 1)
                    {
                        for (int j = 0; j < creatures.Count; ++j)
                        {
                            allcreatures.Add(creatures[j]);
                            if (!zoneInfo.creatureAdded.Contains(creatures[j].ItemID))
                            {
                                zoneInfo.creatureAdded.Add(creatures[j].ItemID);
                            }
                        }

                        rank = creatures[creatures.Count - 1].RankChild;
                    }
                    else
                    {
                        var startRank = rank;
                        for (int j = 0; j < creatures.Count; ++j)
                        {
                            if (zone.GetCreatureInZone(creatures[j].ItemID) > 0)
                            {
                                if (creatures[j].RankChild > rank)
                                {
                                    rank = creatures[j].RankChild;
                                }

                            }
                        }
                        for (int j = 0; j < rank - startRank; ++j)
                        {
                            allcreatures.Add(creatures[j]);
                            if (!zoneInfo.creatureAdded.Contains(creatures[j].ItemID))
                            {
                                zoneInfo.creatureAdded.Add(creatures[j].ItemID);
                            }
                        }
                    }

                }
                foreach (var creature in allcreatures)
                {
                    var creatureInfo = GameManager.Instance.Database.creatureInfos.Find(x => x.id == creature.ItemID);
                    if (!creatureInfo.IsUnLock)
                    {
                        Debug.Log("find Some thing conflict" + creatureInfo.id);
                        creatureInfo.IsUnLock = true;
                    }
                }
            }
            for(int i = 0; i < creatureInfos.Count; ++i)
            {
                if(creatureInfos[i].boughtNumberVariant != null)
                {
                    if (!creatureInfos[i].BoughtNumberVariant.ContainsKey(GameManager.Instance.ZoneChoosed))
                    {
                        creatureInfos[i].BoughtNumberVariant.Add(GameManager.Instance.ZoneChoosed, creatureInfos[i].boughtNumberVariant);
                    }
                    creatureInfos[i].BoughtNumberVariant[GameManager.Instance.ZoneChoosed] = creatureInfos[i].boughtNumberVariant;
                    creatureInfos[i].boughtNumberVariant = null;
                    ES3.markDirty(SaveDataConstraint.WORLD_INFO);
                }
            }

         
        }

        public void InitDatabase()
        {
            GameManager.Instance.timeCollection.Clear();
            worldData.onInit();
            for (int i = 0; i < timeRestore.Count; ++i)
            {
                timeRestore[i].firstTimeAdd = -timeRestore[i].CounterTime;
                timeRestore[i].counterTime = 0;
                GameManager.Instance.timeCollection.Add(timeRestore[i]);
            }
            for (int i = inventory.Count - 1; i >= 0; --i)
            {
                bool pBool = inventory[i].onInit();
                if (!pBool)
                {
                    inventory.RemoveAt(i);
                }
            }
            for (int i = 0; i < GameDatabase.Instance.ZoneCollection.Count; ++i)
            {
                // if (zoneInfos.Count <= i)
                {
                    var zone = zoneInfos.Find(x => x.Id == GameDatabase.Instance.ZoneCollection[i].ItemID);
                    if (zone == null)
                    {
                        addZoneInfo(zone = new ZoneInfoSaved()
                        {
                            Id = GameDatabase.Instance.ZoneCollection[i].ItemID,
                            IsUnLock = (i == 0),

                        });
                    }
                    if (!zone.leaderSelected.ContainsKey(GameDatabase.Instance.MapCollection[0].ItemID))
                    {
                        zone.addLeader(GameDatabase.Instance.MapCollection[0].ItemID, GameDatabase.Instance.treeCreature.Find(x => x.map == GameDatabase.Instance.MapCollection[0]).creatureLeader.ItemID);
                    }



                    // if (zone.isUnLock)
                    {
                        var zoneInstance = worldData.zones.Find(x => x.id == zone.Id);

                        if (zoneInstance == null)
                        {
                            worldData.zones.Add(zoneInstance = new ZoneInstanceSaved() { id = zone.Id, maps = new List<MapInstanceSaved>() });
                        }
                        if (zoneInstance != null)
                        {
                            var maps = GameDatabase.Instance.getAllMapInZone(zone.Id);
                            for (int j = 0; j < maps.Count; ++j)
                            {
                                var mapExist = zoneInstance.maps.Find(x => x.id == maps[j].ItemID);
                                if (mapExist == null)
                                {
                                    zoneInstance.maps.Add(new MapInstanceSaved()
                                    {
                                        id = maps[j].ItemID,
                                        zoneParent = zoneInstance
                                    });
                                }
                            }
                        }

                    }
                }
            }
            for (int i = 0; i < GameDatabase.Instance.CreatureCollection.Count; ++i)
            {
                if (!creatureInfos.Exists(x => x.id == GameDatabase.Instance.CreatureCollection[i].ItemID))
                {
                    creatureInfos.Add(new CreatureInfoSaved()
                    {
                        id = GameDatabase.Instance.CreatureCollection[i].ItemID,
                        IsUnLock = (i == 0)
                    });
                }


            }
            resolveConflictData();
            for (int i = 0; i < zoneInfos.Count; ++i)
            {
                calculateCurrentUnlock(zoneInfos[i].Id);
            }
            for (int i = inventory.Count - 1; i >= 0; --i)
            {
                inventory[i].onEnable();
            }
            
        }

        public void calculateCurrentUnlock(string zone)
        {
            var zoneInfo = zoneInfos.Find(x => x.Id == zone);
            var creatures = getAllCreatureInfoInZone(zone, true);
            if (creatures.Length > 0 && zoneInfo.CurentUnlock != creatures[creatures.Length - 1].ItemID)
            {
                zoneInfo.CurentUnlock = creatures[creatures.Length - 1].ItemID;
            }
            if (creatures.Length == 0)
            {
                zoneInfo.CurentUnlock = "";
            }
            Debug.Log("check curent unlock" + "-" + zoneInfo.CurentUnlock + "-" + zoneInfo.Id);
        }

        public void checkTimeForAll()
        {
            for (int i = 0; i < GameDatabase.Instance.ItemInventoryCollection.Count; ++i)
            {
                var item = GameDatabase.Instance.ItemInventoryCollection[i];
                if (!item.variantItem || item.ItemID.Contains(GameManager.Instance.ZoneChoosed))
                {
                    checkTimeItem(item.ItemID);
                }
            }
        }
        public CreatureInfoSaved[] getAllInfoCreatureInAddress(string zoneId, string map)
        {
            var zoneinfo = zoneInfos.Find(x => x.Id == zoneId);
            if (zoneinfo.leaderSelected.ContainsKey(map))
            {
                string leader = zoneinfo.leaderSelected[map];
                var tree = GameDatabase.Instance.treeCreature.Find(x => x.creatureLeader.ItemID == leader);
                var listCreature = new List<CreatureItem>();
                tree.creatureLeader.getChild(listCreature, tree.amountCreature);
                var listCreatureResult = new List<CreatureInfoSaved>();
                for (int i = 0; i < listCreature.Count; ++i)
                {
                    listCreatureResult.Add(GameManager.Instance.Database.creatureInfos.Find(x => x != null && x.id == listCreature[i].ItemID));
                }
                return listCreatureResult.ToArray();
            }
            return new CreatureInfoSaved[0];
        }

        public CreatureItem[] getAllCreatureInfoInZone(string zoneID, bool unlockOnly = false)
        {
            var zone = zoneInfos.Find(x => x.Id == zoneID);
            List<CreatureItem> creatures = new List<CreatureItem>();
            bool endFind = false;
            for (int i = 0; i < zone.leaderSelected.Count; ++i)
            {
                var leader = zone.leaderSelected.Values.ElementAt(i);
                var tree = GameDatabase.Instance.treeCreature.Find(x => x.creatureLeader.ItemID == leader);
                var subList = new List<CreatureItem>();
                tree.creatureLeader.getChild(subList, tree.amountCreature);
                foreach (var element in subList)
                {
                    var creatureInfo = creatureInfos.Find(x => x.id == element.ItemID);
                    if (!creatureInfo.IsUnLock || !zone.creatureAdded.Contains(creatureInfo.id))
                    {
                        endFind = true;
                        break;
                    }
                    if (!creatures.Exists(x => x.ItemID == element.ItemID))
                    {
                        creatures.Add(element);
                    }

                }
                if (endFind)
                {
                    break;
                }
            }
            return creatures.ToArray();
        }

        public List<CreatureInstanceSaved> getAllCreatureInstanceInZone(string zoneID)
        {
            var maps = GameManager.Instance.Database.worldData.zones.Find(x => x.id == zoneID).maps;
            var listCreatures = new List<CreatureInstanceSaved>();
            foreach (var map in maps)
            {
                listCreatures.addFromList(map.creatures.ToArray());
            }
            return listCreatures;
        }
        public List<CreatureInstanceSaved> getAllCreatureInstanceInAdress(string zoneID, string map)
        {
            var mapFind = GameManager.Instance.Database.worldData.zones.Find(x => x.id == zoneID).maps.Find(x => x.id == map);
            var listCreatures = new List<CreatureInstanceSaved>();
            if (mapFind != null)
            {
                listCreatures.addFromList(mapFind.creatures.ToArray());
            }
            return listCreatures;
        }
        public List<MapInstanceSaved> getAllMapInZone(string zoneID)
        {
            return GameManager.Instance.Database.worldData.zones.Find(x => x.id == zoneID).maps;
        }
        public List<MapInstanceSaved> getAllMapActiveInZone(string zoneID)
        {
            var zone = GameManager.Instance.Database.worldData.zones.Find(x => x.id == zoneID);
            var zoneInfos = GameManager.Instance.Database.zoneInfos.Find(x => x.Id == zoneID);
            List<MapInstanceSaved> maps = new List<MapInstanceSaved>();
            for (int i = 0; i < zone.maps.Count; ++i)
            {
                if (zoneInfos.leaderSelected.ContainsKey(zone.maps[i].id))
                {
                    maps.Add(zone.maps[i]);
                }
            }
            return maps;
        }

        public void checkTimeItem(string itemID)
        {
            var item = GameDatabase.Instance.getItemInventory(itemID);
            if (item == null)
            {
                Debug.Log(itemID + "null");
            }
            if (item.categoryItem == CategoryItem.COMMON)
            {
                var itemExist = getItem(item.ItemID);
                if (item.variantItem && !item.ItemID.Contains(GameManager.Instance.ZoneChoosed))
                {
                    var time = timeRestore.Find(x => x.id.Contains(item.ItemID) && x.id.Contains("Restore"));
                    if (time != null)
                    {
                        time.pauseTime(true);
                    }
                    return;
                }
                executeTime(itemExist);
            }
            else if (item.categoryItem == CategoryItem.CREATURE || item.categoryItem == CategoryItem.PACKAGE_CREATURE)
            {
                var itemExist = getCreatureItem(item.ItemID, GameManager.Instance.ZoneChoosed);
                if (itemExist != null) {
                    executeTime(itemExist, GameManager.Instance.ZoneChoosed);
                }
             

            }
        }
        public void executeTime(BaseItemGameInstanced itemExist, string adress = "")
        {
            var item = itemExist.item;
            if (itemExist != null)
            {
                if ((item.attribute & AttributeItem.RestoreAble) == AttributeItem.RestoreAble)
                {
                    bool restore = true;
                    if ((item.attribute & AttributeItem.Limit) == AttributeItem.Limit)
                    {
                        if (itemExist.QuantityBig >= item.limitInInventory.getUnit(itemExist.CurrentLevel))
                        {
                            restore = false;
                        }
                    }
                    if (restore)
                    {
                        var existTime = GameManager.Instance.Database.timeRestore.Exists(x => x.id == "[Restore]" + adress + (string.IsNullOrEmpty(adress) ? "" : "/") + item.ItemID);
                        bool continueRestore = true;
                        bool isCreature = item.categoryItem == CategoryItem.CREATURE || item.categoryItem == CategoryItem.PACKAGE_CREATURE;
                        var limitSlot = isCreature ? ((CreatureItem)item).parentMap.limitSlot : item.limitInInventory.getCurrentUnit();
                        var quantitySource = isCreature ? getAllCreatureInstanceInAdress(adress, ((CreatureItem)item).parentMap.ItemID).Count : itemExist.QuantityBig;
                        if (item.isLimit && itemExist.QuantityBig < item.limitInInventory.getUnit(itemExist.CurrentLevel))
                        {
                            if (limitSlot <= quantitySource)
                            {
                                continueRestore = false;
                            }
                        }
                        float factor = 1;
                        if (item.ItemID.Contains("Egg"))
                        {
                            factor = 1 - GameManager.Instance.getPercentReduceTimeEgg().x;
                        }
                        if (!existTime && continueRestore)
                        {
                            var timer = new TimeCounterInfo() { firstTimeAdd = TimeCounter.CounterValue, counterTime = 0, id = "[Restore]" + adress + (string.IsNullOrEmpty(adress) ? "" : "/") + item.ItemID, destinyIfHave = (double)itemExist.item.timeToRestore.getCurrentUnit() * factor };
                            addTime(timer);
                        }
                        else if (existTime)
                        {
                            var time = GameManager.Instance.Database.timeRestore.Find(x => x.id == "[Restore]" + adress + (string.IsNullOrEmpty(adress) ? "" : "/") + item.ItemID);
                            time.pauseTime(false);
                            if (time != null)
                            {

                                time.destinyIfHave = (double)itemExist.item.timeToRestore.getCurrentUnit() * factor;
                            }
                            bool _removeTime = false;
                            if (continueRestore)
                            {
                                System.Numerics.BigInteger quantity = System.Numerics.BigInteger.Parse(((long)time.CounterTime).ToString()) / System.Numerics.BigInteger.Parse(((long)item.timeToRestore.getCurrentUnit() * ((int)(100 * factor)) / 100).ToString());
                                if (quantity > 0)
                                {
                                    time.firstTimeAdd = TimeCounter.CounterValue;
                                    if (quantitySource + quantity < limitSlot)
                                    {
                                        itemExist.addQuantity(quantity.ToString());
                                    }
                                    else
                                    {
                                        quantity -= quantitySource + quantity - limitSlot;
                                        itemExist.addQuantity(quantity.ToString());
                                        _removeTime = true;
                                    }
                                }

                            }
                            else
                            {

                                _removeTime = true;
                            }
                            if (_removeTime)
                            {
                                removeTime(time);
                            }
                        }
                    }
                }
            }
        }

        public void removeTime(TimeCounterInfo time)
        {
            GameManager.Instance.Database.timeRestore.Remove(time);
            GameManager.Instance.timeCollection.Remove(time);
            ES3.markDirty(SaveDataConstraint.TIME_DATA);
            EazyEngine.Tools.EzEventManager.TriggerEvent<RemoveTimeEvent>(new RemoveTimeEvent() { timeInfo = time });
        }
        public void addTime(TimeCounterInfo time)
        {
            GameManager.Instance.Database.timeRestore.Add(time);
            GameManager.Instance.timeCollection.Add(time);
            ES3.markDirty(SaveDataConstraint.TIME_DATA);
        }
        public List<CreatureInstanceSaved> getCreatureExist(string idCreature, string zoneID)
        {
            for (int i = 0; i < worldData.zones.Count; ++i)
            {
                if (worldData.zones[i].id == zoneID)
                {
                    return worldData.zones[i].GetCreaturesInZone(idCreature);
                }

            }
            return new List<CreatureInstanceSaved>();
        }
        public int getQuantityCreatureExist(string idCreature, string zoneID)
        {
            for (int i = 0; i < worldData.zones.Count; ++i)
            {
                if (worldData.zones[i].id == zoneID)
                {
                    return worldData.zones[i].GetCreatureInZone(idCreature);
                }

            }
            return 0;
        }

        public bool addItem(string itemID, string address)
        {
            if (string.IsNullOrEmpty(address)) return false;
            var pItemOriginal = GameDatabase.Instance.getItemInventory(itemID);
            if (pItemOriginal)
            {
                CreatureInstanceSaved creatureNew = null;// new CreatureInstanceSaved() { instanceID = GameManager.Instance.GenerateID.ToString(), id = itemID };
                if (pItemOriginal.categoryItem == CategoryItem.PACKAGE_CREATURE)
                {
                    int levelCreature = creatureInfos.Find(x => x.id == itemID).Level;
                    var package = (PackageCreatureObject)pItemOriginal;
                    var map = package.parentMap;
                    var creatures = GameManager.Instance.Database.getAllInfoCreatureInAddress(address, map.ItemID);
                    if (creatures.Length >= map.limitSlot)
                    {
                        return false;
                    }
                    if (package.creatureExtra.Length > 0)
                    {
                        var creatureFind = package.creatureExtra[levelCreature];
                        if (creatureFind != null)
                        {
                            creatureNew = new PackageCreatureInstanceSaved() { creature = creatureFind.ItemID, instanceID = GameManager.Instance.GenerateID.ToString(), id = itemID };
                            GameManager.Instance.GenerateID++;
                        }

                    }
                }
                if (creatureNew != null)
                {
                    worldData.addCreature(creatureNew, address);
                    EazyEngine.Tools.EzEventManager.TriggerEvent(new AddCreatureEvent() { change = 1, creature = creatureNew, zoneid = address, manualByHand = false });
                    return true;
                }

            }
            return false;
        }

        public BaseItemGameInstanced getCreatureItem(string itemID, string zone)
        {
            var pItemOriginal = (BaseItemGame)GameDatabase.Instance.CreatureCollection.Find(x => x.ItemID == itemID);
            pItemOriginal = pItemOriginal != null ? pItemOriginal : GameDatabase.Instance.getItemInventory(itemID);
            if (pItemOriginal && (!pItemOriginal.variantItem || pItemOriginal.ItemID.Contains(zone)))
            {
                if (pItemOriginal.categoryItem == CategoryItem.CREATURE || pItemOriginal.categoryItem == CategoryItem.PACKAGE_CREATURE)
                {
                    var itemnew = new BaseItemGameInstanced() { quantity = getQuantityCreatureExist(pItemOriginal.ItemID, zone).ToString(), address = $"{zone}", itemID = pItemOriginal.ItemID, item = pItemOriginal };
                    return itemnew;
                }
            }
            return null;
        }
        public BaseItemGameInstanced getItem(string itemID)
        {
            var pItemOriginal = GameDatabase.Instance.getItemInventory(itemID);
            if (pItemOriginal)
            {
                if (pItemOriginal.categoryItem == CategoryItem.COMMON)
                {
                    var itemExist = inventory.Find(x => x.item != null && x.item.ItemID == itemID);
                    if (itemExist == null)
                    {
                        var item = new BaseItemGameInstanced() { item = pItemOriginal, quantity = "0", itemID = pItemOriginal.itemID };
                        inventory.Add(item);
                        return item;
                    }
                    else if (itemExist.QuantityBig > 0)
                    {
                        itemExist.EmptySlot = false;
                    }
                    return itemExist;
                }
            }
            return null;
        }
    }
}
