
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.ComponentModel;

namespace Engine
{
    public class Player : LivingCreature
    {        
        private int _gold; 
        private int _experiencePoints; 
        private Monster _currentMonster;
        private Location _currentLocation;

        public Weapon CurrentWeapon { get; set; }
        public BindingList<InventoryItem> Inventory { get; set; }
        public BindingList<PlayerQuest> Quests { get; set; }

        public List<Weapon> Weapons
        {
            get { return Inventory.Where(x => x.Details is Weapon).Select(x => x.Details as Weapon).ToList(); }
        }

        public List<HealingPotion> Potions
        {
            get { return Inventory.Where(x => x.Details is HealingPotion).Select(x => x.Details as HealingPotion).ToList(); }
        }

        public int Level//
        {
            get { return ((ExperiencePoints / 100) + 1); }
        }

        public event EventHandler<MessageEventArgs> OnMessage;

        private void RaiseMessage(string message, bool addExtraNewLine = false)
        {
            if (OnMessage != null)
            {
                OnMessage(this, new MessageEventArgs(message, addExtraNewLine));
            }
        }

        public int Gold
        {
            get { return _gold; }
            set
            {
                _gold = value;
                OnPropertyChanged("Gold");
            }
        }

        public int ExperiencePoints
        {
            get { return _experiencePoints; }
            private set
            {
                _experiencePoints = value;
                OnPropertyChanged("ExperiencePoints");
                OnPropertyChanged("Level"); 
            }
        }       

        public Location CurrentLocation
        {
            get { return _currentLocation; }
            set
            {
                _currentLocation = value;
                OnPropertyChanged("CurrentLocation");
            }
        }           
       
        private void RaiseInventoryChangedEvent(Item item)
        {
            if (item is Weapon)
            {
                OnPropertyChanged("Weapons");
            }

            if (item is HealingPotion)
            {
                OnPropertyChanged("Potions");
            }
        }

        public void RemoveItemFromInventory(Item itemToRemove, int quantity = 1)
        {
            InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == itemToRemove.ID);

            if (item == null)
            {
                // The item is not in the player's inventory, so ignore it.
                // We might want to raise an error for this situation
            }
            else
            {                
                item.Quantity -= quantity;

                // We might want to raise an error for this situation
                if (item.Quantity < 0)
                {
                    item.Quantity = 0;
                }
                                
                if (item.Quantity == 0)
                {
                    Inventory.Remove(item);
                }
                
                RaiseInventoryChangedEvent(itemToRemove);
            }
        }

        private Player(
            int currentHitPoints, int maximumHitPoints, 
            int gold, int experiencePoints) : base(currentHitPoints, maximumHitPoints)
        {
            Gold = gold;
            ExperiencePoints = experiencePoints;

            Inventory = new BindingList<InventoryItem>();
            Quests = new BindingList<PlayerQuest>();
        }

        public static Player CreateDefaultPlayer()
        {
            Player player = new Player(10, 10, 20, 0);
            player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_RUSTY_SWORD), 1));
            player.CurrentLocation = World.LocationByID(World.LOCATION_ID_HOME);

            return player;
        }

        public static Player CreatePlayerFromXmlString(string xmlPlayerData)
        {
            try
            {
                XmlDocument playerData = new XmlDocument();

                playerData.LoadXml(xmlPlayerData);

                int currentHitPoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentHitPoints").InnerText);
                int maximumHitPoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/MaximumHitPoints").InnerText);
                int gold = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/Gold").InnerText);
                int experiencePoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/ExperiencePoints").InnerText);

                Player player = new Player(currentHitPoints, maximumHitPoints, gold, experiencePoints);

                int currentLocationID = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentLocation").InnerText);
                player.CurrentLocation = World.LocationByID(currentLocationID);

                if (playerData.SelectSingleNode("/Player/Stats/CurrentWeapon") != null) 
                {
                    int currentWeaponID = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentWeapon").InnerText);
                    player.CurrentWeapon = (Weapon)World.ItemByID(currentWeaponID);
                }

                foreach (XmlNode node in playerData.SelectNodes("/Player/InventoryItems/InventoryItem"))
                {
                    int id = Convert.ToInt32(node.Attributes["ID"].Value);
                    int quantity = Convert.ToInt32(node.Attributes["Quantity"].Value);

                    for (int i = 0; i < quantity; i++)
                    {
                        player.AddItemToInventory(World.ItemByID(id));
                    }
                }

                foreach (XmlNode node in playerData.SelectNodes("/Player/PlayerQuests/PlayerQuest"))
                {
                    int id = Convert.ToInt32(node.Attributes["ID"].Value);
                    bool isCompleted = Convert.ToBoolean(node.Attributes["IsCompleted"].Value);

                    PlayerQuest playerQuest = new PlayerQuest(World.QuestByID(id));
                    playerQuest.IsCompleted = isCompleted;

                    player.Quests.Add(playerQuest);
                }

                return player;
            }
            catch
            {
                return Player.CreateDefaultPlayer();
            }
        }  
                       
        public bool HasRequiredItemToEnterThisLocation(Location location)
        {
            if (location.ItemRequiredToEnter == null)
            {                
                return true;
            }            
            return Inventory.Any(ii => ii.Details.ID == location.ItemRequiredToEnter.ID);
        }

        public bool HasThisQuest(Quest quest)
        {
            return Quests.Any(pq => pq.Details.ID == quest.ID);
        }

        public bool CompletedThisQuest(Quest quest)
        {
            foreach (PlayerQuest playerQuest in Quests)
            {
                if (playerQuest.Details.ID == quest.ID)
                {
                    return playerQuest.IsCompleted;
                }
            }

            return false;
        }

        public bool HasAllQuestCompletionItems(Quest quest)
        {            
            foreach (QuestCompletionItem qci in quest.QuestCompletionItems)
            {                
                if (!Inventory.Any(ii => ii.Details.ID == qci.Details.ID && ii.Quantity >= qci.Quantity))
                {
                    return false;
                }
            }            
            return true;
        }

        public void AddExperiencePoints(int experiencePointsToAdd)
        {
            ExperiencePoints += experiencePointsToAdd;
            MaximumHitPoints = (Level * 10);
        }

        public void RemoveQuestCompletionItems(Quest quest)
        {
            foreach (QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                
                InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == qci.Details.ID);

                if (item != null)
                {
                    RemoveItemFromInventory(item.Details, qci.Quantity);
                }
            }
        }

        public void AddItemToInventory(Item itemToAdd, int quantity = 1)
        {
            InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == itemToAdd.ID);

            if (item == null)
            {
                Inventory.Add(new InventoryItem(itemToAdd, quantity));
            }
            else
            {
                item.Quantity += quantity;
            }

            RaiseInventoryChangedEvent(itemToAdd);
        }

        public void MarkQuestCompleted(Quest quest)
        {
            PlayerQuest playerQuest = Quests.SingleOrDefault(pq => pq.Details.ID == quest.ID);

            if (playerQuest != null)
            {
                playerQuest.IsCompleted = true;
            }
        }

        public void UseWeapon(Weapon weapon)
        {
            int damageToMonster = RandomNumberGenerator.NumberBetween(weapon.MinimumDamage, weapon.MaximumDamage);

            _currentMonster.CurrentHitPoints -= damageToMonster;

            RaiseMessage("You hit the " + _currentMonster.Name + " for " + damageToMonster + " points.");

            if (_currentMonster.CurrentHitPoints <= 0)
            {
                RaiseMessage("");
                RaiseMessage("You defeated the " + _currentMonster.Name);

                AddExperiencePoints(_currentMonster.RewardExperiencePoints);
                RaiseMessage("You receive " + _currentMonster.RewardExperiencePoints + " experience points");

                Gold += _currentMonster.RewardGold;
                RaiseMessage("You receive " + _currentMonster.RewardGold + " gold");

                List<InventoryItem> lootedItems = new List<InventoryItem>();

                foreach (LootItem lootItem in _currentMonster.LootTable)
                {
                    if (RandomNumberGenerator.NumberBetween(1, 100) <= lootItem.DropPercentage)
                    {
                        lootedItems.Add(new InventoryItem(lootItem.Details, 1));
                    }
                }

                if (lootedItems.Count == 0)
                {
                    foreach (LootItem lootItem in _currentMonster.LootTable)
                    {
                        if (lootItem.IsDefaultItem)
                        {
                            lootedItems.Add(new InventoryItem(lootItem.Details, 1));
                        }
                    }
                }

                foreach (InventoryItem inventoryItem in lootedItems)
                {
                    AddItemToInventory(inventoryItem.Details);

                    if (inventoryItem.Quantity == 1)
                    {
                        RaiseMessage("You loot " + inventoryItem.Quantity + " " + inventoryItem.Details.Name);
                    }
                    else
                    {
                        RaiseMessage("You loot " + inventoryItem.Quantity + " " + inventoryItem.Details.NamePlural);
                    }
                }

                RaiseMessage("");

                MoveTo(CurrentLocation);
            }
            else
            {
                int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);

                RaiseMessage("The " + _currentMonster.Name + " did " + damageToPlayer + " points of damage.");

                CurrentHitPoints -= damageToPlayer;

                if (CurrentHitPoints <= 0)
                {
                    RaiseMessage("The " + _currentMonster.Name + " killed you.");

                    MoveHome();
                }
            }
        }

        public void UsePotion(HealingPotion potion)
        {
            CurrentHitPoints = (CurrentHitPoints + potion.AmountToHeal);

            if (CurrentHitPoints > MaximumHitPoints)
            {
                CurrentHitPoints = MaximumHitPoints;
            }

            RemoveItemFromInventory(potion, 1);

            RaiseMessage("You drink a " + potion.Name);

            int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);

            RaiseMessage("The " + _currentMonster.Name + " did " + damageToPlayer + " points of damage.");

            CurrentHitPoints -= damageToPlayer;

            if (CurrentHitPoints <= 0)
            {
                RaiseMessage("The " + _currentMonster.Name + " killed you.");

                MoveHome();
            }
        }


        public void MoveHome()
        {
            MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
        }

        public void MoveNorth()
        {
            if (CurrentLocation.LocationToNorth != null)
            {
                MoveTo(CurrentLocation.LocationToNorth);
            }
        }

        public void MoveEast()
        {
            if (CurrentLocation.LocationToEast != null)
            {
                MoveTo(CurrentLocation.LocationToEast);
            }
        }

        public void MoveSouth()
        {
            if (CurrentLocation.LocationToSouth != null)
            {
                MoveTo(CurrentLocation.LocationToSouth);
            }
        }

        public void MoveWest()
        {
            if (CurrentLocation.LocationToWest != null)
            {
                MoveTo(CurrentLocation.LocationToWest);
            }
        }

        public void MoveTo(Location newLocation)
        {
            if (!HasRequiredItemToEnterThisLocation(newLocation))
            {
                RaiseMessage("You must have a " + newLocation.ItemRequiredToEnter.Name + " to enter this location.");
                return;
            }
                        
            CurrentLocation = newLocation;
                        
            CurrentHitPoints = MaximumHitPoints;

            if (newLocation.QuestAvailableHere != null)
            {
                bool playerAlreadyHasQuest = HasThisQuest(newLocation.QuestAvailableHere);
                bool playerAlreadyCompletedQuest = CompletedThisQuest(newLocation.QuestAvailableHere);

                if (playerAlreadyHasQuest)
                {
                    if (!playerAlreadyCompletedQuest)
                    {
                        bool playerHasAllItemsToCompleteQuest = HasAllQuestCompletionItems(newLocation.QuestAvailableHere);

                        if (playerHasAllItemsToCompleteQuest)
                        {
                            RaiseMessage("");
                            RaiseMessage("You complete the '" + newLocation.QuestAvailableHere.Name + "' quest.");

                            RemoveQuestCompletionItems(newLocation.QuestAvailableHere);

                            RaiseMessage("You receive: ");
                            RaiseMessage(newLocation.QuestAvailableHere.RewardExperiencePoints + " experience points");
                            RaiseMessage(newLocation.QuestAvailableHere.RewardGold + " gold");
                            RaiseMessage(newLocation.QuestAvailableHere.RewardItem.Name, true);

                            AddExperiencePoints(newLocation.QuestAvailableHere.RewardExperiencePoints);
                            Gold += newLocation.QuestAvailableHere.RewardGold;

                            AddItemToInventory(newLocation.QuestAvailableHere.RewardItem);

                            MarkQuestCompleted(newLocation.QuestAvailableHere);
                        }
                    }
                }
                else
                {
                    RaiseMessage("You receive the " + newLocation.QuestAvailableHere.Name + " quest.");
                    RaiseMessage(newLocation.QuestAvailableHere.Description);
                    RaiseMessage("To complete it, return with:");
                    foreach (QuestCompletionItem qci in newLocation.QuestAvailableHere.QuestCompletionItems)
                    {
                        if (qci.Quantity == 1)
                        {
                            RaiseMessage(qci.Quantity + " " + qci.Details.Name);
                        }
                        else
                        {
                            RaiseMessage(qci.Quantity + " " + qci.Details.NamePlural);
                        }
                    }
                    RaiseMessage("");

                    Quests.Add(new PlayerQuest(newLocation.QuestAvailableHere));
                }
            }

            if (newLocation.MonsterLivingHere != null)
            {
                RaiseMessage("You see a " + newLocation.MonsterLivingHere.Name);

                Monster standardMonster = World.MonsterByID(newLocation.MonsterLivingHere.ID);

                _currentMonster = new Monster(standardMonster.ID, standardMonster.Name, standardMonster.MaximumDamage,
                    standardMonster.RewardExperiencePoints, standardMonster.RewardGold, standardMonster.CurrentHitPoints, standardMonster.MaximumHitPoints);

                foreach (LootItem lootItem in standardMonster.LootTable)
                {
                    _currentMonster.LootTable.Add(lootItem);
                }
            }
            else
            {
                _currentMonster = null;
            }
        }

        //-------------------------------XML Handling----------------------------------------------//
        public string ToXmlString()
        {
            XmlDocument playerData = new XmlDocument();

            XmlNode player = playerData.CreateElement("Player");
            playerData.AppendChild(player);

            XmlNode stats = playerData.CreateElement("Stats");
            player.AppendChild(stats);

            XmlNode currentHitPoints = playerData.CreateElement("CurrentHitPoints");
            currentHitPoints.AppendChild(playerData.CreateTextNode(this.CurrentHitPoints.ToString()));
            stats.AppendChild(currentHitPoints);

            XmlNode maximumHitPoints = playerData.CreateElement("MaximumHitPoints");
            maximumHitPoints.AppendChild(playerData.CreateTextNode(this.MaximumHitPoints.ToString()));
            stats.AppendChild(maximumHitPoints);

            XmlNode gold = playerData.CreateElement("Gold");
            gold.AppendChild(playerData.CreateTextNode(this.Gold.ToString()));
            stats.AppendChild(gold);

            XmlNode experiencePoints = playerData.CreateElement("ExperiencePoints");
            experiencePoints.AppendChild(playerData.CreateTextNode(this.ExperiencePoints.ToString()));
            stats.AppendChild(experiencePoints);

            XmlNode currentLocation = playerData.CreateElement("CurrentLocation");
            currentLocation.AppendChild(playerData.CreateTextNode(this.CurrentLocation.ID.ToString()));
            stats.AppendChild(currentLocation);

            if (CurrentWeapon != null)
            {
                XmlNode currentWeapon = playerData.CreateElement("CurrentWeapon");
                currentWeapon.AppendChild(playerData.CreateTextNode(this.CurrentWeapon.ID.ToString()));
                stats.AppendChild(currentWeapon);
            }

            XmlNode inventoryItems = playerData.CreateElement("InventoryItems");
            player.AppendChild(inventoryItems);

            foreach (InventoryItem item in this.Inventory)
            {
                XmlNode inventoryItem = playerData.CreateElement("InventoryItem");

                XmlAttribute idAttribute = playerData.CreateAttribute("ID");
                idAttribute.Value = item.Details.ID.ToString();
                inventoryItem.Attributes.Append(idAttribute);

                XmlAttribute quantityAttribute = playerData.CreateAttribute("Quantity");
                quantityAttribute.Value = item.Quantity.ToString();
                inventoryItem.Attributes.Append(quantityAttribute);

                inventoryItems.AppendChild(inventoryItem);
            }

            XmlNode playerQuests = playerData.CreateElement("PlayerQuests");
            player.AppendChild(playerQuests);

            foreach (PlayerQuest quest in this.Quests)
            {
                XmlNode playerQuest = playerData.CreateElement("PlayerQuest");

                XmlAttribute idAttribute = playerData.CreateAttribute("ID");
                idAttribute.Value = quest.Details.ID.ToString();
                playerQuest.Attributes.Append(idAttribute);

                XmlAttribute isCompletedAttribute = playerData.CreateAttribute("IsCompleted");
                isCompletedAttribute.Value = quest.IsCompleted.ToString();
                playerQuest.Attributes.Append(isCompletedAttribute);

                playerQuests.AppendChild(playerQuest);
            }

            return playerData.InnerXml;
        }

    }
}
