﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using Engine;



namespace SuperAdventure
{
    public partial class SuperAdventure : Form
    {
        private Player _player;
        private Monster _currentMonster;
        private const string PLAYER_DATA_FILE_NAME = "PlayerData.xml";

        public SuperAdventure() 
        {
            InitializeComponent();

            if (File.Exists(PLAYER_DATA_FILE_NAME))
            {
                _player = Player.CreatePlayerFromXmlString(File.ReadAllText(PLAYER_DATA_FILE_NAME));
            }
            else
            {
                _player = Player.CreateDefaultPlayer();
            }

            lblHitPoints.DataBindings.Add("Text", _player, "CurrentHitPoints"); // labels added after object creation. MUST be done in this order
            lblGold.DataBindings.Add("Text", _player, "Gold");
            lblExperience.DataBindings.Add("Text", _player, "ExperiencePoints");
            lblLevel.DataBindings.Add("Text", _player, "Level");

            //----------Inventory Data Binding----------------------//

            dgvInventory.RowHeadersVisible = false;
            dgvInventory.AutoGenerateColumns = false; 

            dgvInventory.DataSource = _player.Inventory;

            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Width = 197,
                DataPropertyName = "Description"
            });

            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Quantity",
                DataPropertyName = "Quantity"
            });

            //-----------Quest Data Binding--------------------//

            dgvQuests.RowHeadersVisible = false;
            dgvQuests.AutoGenerateColumns = false;

            dgvQuests.DataSource = _player.Quests;

            dgvQuests.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Width = 197,
                DataPropertyName = "Name"
            });

            dgvQuests.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Done?",
                DataPropertyName = "IsCompleted"
            });

            MoveTo(_player.CurrentLocation);
        }

        
        private void cboWeapons_SelectedIndexChanged(object sender, EventArgs e)
        {
            _player.CurrentWeapon = (Weapon)cboWeapons.SelectedItem; 
        }


        private void btnNorth_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToNorth);
        }

        private void btnEast_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToEast);
        }

        private void btnSouth_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToSouth);
        }

        private void btnWest_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToWest);

        }
        public void MoveTo(Location newLocation)
        {
            //Does the location have any required items
            if (!_player.HasRequiredItemToEnterThisLocation(newLocation))
            {
                rtbMessages.Text += "You must have a " + newLocation.ItemRequiredToEnter.Name + " to enter this location." + Environment.NewLine;
                return;
            }

            // Update the player's current location
            _player.CurrentLocation = newLocation;

            // Show/hide available movement buttons
            btnNorth.Visible = (newLocation.LocationToNorth != null);
            btnEast.Visible = (newLocation.LocationToEast != null);
            btnSouth.Visible = (newLocation.LocationToSouth != null);
            btnWest.Visible = (newLocation.LocationToWest != null);

            // Display current location name and description
            rtbLocation.Text = newLocation.Name + Environment.NewLine;
            rtbLocation.Text += newLocation.Description + Environment.NewLine;

            // Completely heal the player
            _player.CurrentHitPoints = _player.MaximumHitPoints;

            // Update Hit Points in UI
            

            // Does the location have a quest?
            if (newLocation.QuestAvailableHere != null)
            {
                // See if the player already has the quest, and if they've completed it
                bool playerAlreadyHasQuest = _player.HasThisQuest(newLocation.QuestAvailableHere);
                bool playerAlreadyCompletedQuest = _player.CompletedThisQuest(newLocation.QuestAvailableHere);

                // See if the player already has the quest
                if (playerAlreadyHasQuest)
                {
                    // If the player has not completed the quest yet
                    if (!playerAlreadyCompletedQuest)
                    {
                        // See if the player has all the items needed to complete the quest
                        bool playerHasAllItemsToCompleteQuest = _player.HasAllQuestCompletionItems(newLocation.QuestAvailableHere);

                        // The player has all items required to complete the quest
                        if (playerHasAllItemsToCompleteQuest)
                        {
                            // Display message
                            rtbMessages.Text += Environment.NewLine;
                            rtbMessages.Text += "You complete the '" + newLocation.QuestAvailableHere.Name + "' quest." + Environment.NewLine;

                            // Remove quest items from inventory
                            _player.RemoveQuestCompletionItems(newLocation.QuestAvailableHere);

                            // Give quest rewards
                            rtbMessages.Text += "You receive: " + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardExperiencePoints.ToString() + " experience points" + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardGold.ToString() + " gold" + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardItem.Name + Environment.NewLine;
                            rtbMessages.Text += Environment.NewLine;


                            _player.AddExperiencePoints(newLocation.QuestAvailableHere.RewardExperiencePoints);
                            _player.Gold += newLocation.QuestAvailableHere.RewardGold;

                            // Add the reward item to the player's inventory
                            _player.AddItemToInventory(newLocation.QuestAvailableHere.RewardItem);

                            // Mark the quest as completed
                            _player.MarkQuestCompleted(newLocation.QuestAvailableHere);
                        }
                    }
                }
                else
                {
                    // The player does not already have the quest

                    // Display the messages
                    rtbMessages.Text += "You receive the " + newLocation.QuestAvailableHere.Name + " quest." + Environment.NewLine;
                    rtbMessages.Text += newLocation.QuestAvailableHere.Description + Environment.NewLine;
                    rtbMessages.Text += "To complete it, return with:" + Environment.NewLine;
                    foreach (QuestCompletionItem qci in newLocation.QuestAvailableHere.QuestCompletionItems)
                    {
                        if (qci.Quantity == 1)
                        {
                            rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.Name + Environment.NewLine;
                        }
                        else
                        {
                            rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.NamePlural + Environment.NewLine;
                        }
                    }
                    rtbMessages.Text += Environment.NewLine;

                    // Add the quest to the player's quest list
                    _player.Quests.Add(new PlayerQuest(newLocation.QuestAvailableHere));
                }
            }

            // Does the location have a monster?
            if (newLocation.MonsterLivingHere != null)
            {
                rtbMessages.Text += "You see a " + newLocation.MonsterLivingHere.Name + Environment.NewLine;

                // Make a new monster, using the values from the standard monster in the World.Monster list
                Monster standardMonster = World.MonsterByID(newLocation.MonsterLivingHere.ID);

                _currentMonster = new Monster(standardMonster.ID, standardMonster.Name, standardMonster.MaximumDamage,
                    standardMonster.RewardExperiencePoints, standardMonster.RewardGold, standardMonster.CurrentHitPoints, standardMonster.MaximumHitPoints);

                foreach (LootItem lootItem in standardMonster.LootTable)
                {
                    _currentMonster.LootTable.Add(lootItem);
                }

                cboWeapons.Visible = true;
                cboPotions.Visible = true;
                btnUseWeapon.Visible = true;
                btnUsePotion.Visible = true;
            }
            else
            {
                _currentMonster = null;

                cboWeapons.Visible = false;
                cboPotions.Visible = false;
                btnUseWeapon.Visible = false;
                btnUsePotion.Visible = false;
            }
            

            // Refresh player's inventory list
            

            // Refresh player's quest list
            

            // Refresh player's weapons combobox
            UpdateWeaponListInUI();

            // Refresh player's potions combobox
            UpdatePotionListInUI();
        }

        

        
        private void UpdateWeaponListInUI()
        {
            List<Weapon> weapons = new List<Weapon>();

            foreach (InventoryItem inventoryItem in _player.Inventory)
            {
                if (inventoryItem.Details is Weapon)
                {
                    if (inventoryItem.Quantity > 0)
                    {
                        weapons.Add((Weapon)inventoryItem.Details);
                    }
                }
            }

            if (weapons.Count == 0)
            {                
                cboWeapons.Visible = false;
                btnUseWeapon.Visible = false;
            }
            else
            {
                cboWeapons.SelectedIndexChanged -= cboWeapons_SelectedIndexChanged;
                cboWeapons.DataSource = weapons;
                cboWeapons.SelectedIndexChanged += cboWeapons_SelectedIndexChanged;
                cboWeapons.DisplayMember = "Name";
                cboWeapons.ValueMember = "ID";

                if (_player.CurrentWeapon != null)
                {
                    cboWeapons.SelectedItem = _player.CurrentWeapon;
                }
                else
                {
                    cboWeapons.SelectedIndex = 0;
                }
            }
        }

        private void UpdatePotionListInUI()
        {
            List<HealingPotion> healingPotions = new List<HealingPotion>();

            foreach (InventoryItem inventoryItem in _player.Inventory)
            {
                if (inventoryItem.Details is HealingPotion)
                {
                    if (inventoryItem.Quantity > 0)
                    {
                        healingPotions.Add((HealingPotion)inventoryItem.Details);
                    }
                }
            }

            if (healingPotions.Count == 0)
            {
                // The player doesn't have any potions, so hide the potion combobox and "Use" button
                cboPotions.Visible = false;
                btnUsePotion.Visible = false;
            }
            else
            {
                cboPotions.DataSource = healingPotions;
                cboPotions.DisplayMember = "Name";
                cboPotions.ValueMember = "ID";

                cboPotions.SelectedIndex = 0;
            }
        }
        private void SuperAdventure_Load(object sender, EventArgs e)
        {

        }

        private void btnUseWeapon_Click_1(object sender, EventArgs e)
        { // Get the currently selected weapon from the cboWeapons ComboBox
            Weapon currentWeapon = (Weapon)cboWeapons.SelectedItem;

            // Determine the amount of damage to do to the monster
            int damageToMonster = RandomNumberGenerator.NumberBetween(currentWeapon.MinimumDamage, currentWeapon.MaximumDamage);

            // Apply the damage to the monster's CurrentHitPoints
            _currentMonster.CurrentHitPoints -= damageToMonster;

            // Display message
            rtbMessages.Text += "You hit the " + _currentMonster.Name + " for " + damageToMonster.ToString() + " points." + Environment.NewLine;

            // Check if the monster is dead
            if (_currentMonster.CurrentHitPoints <= 0)
            {
                // Monster is dead
                rtbMessages.Text += Environment.NewLine;
                rtbMessages.Text += "You defeated the " + _currentMonster.Name + Environment.NewLine;

                // Give player experience points for killing the monster
                _player.AddExperiencePoints(_currentMonster.RewardExperiencePoints);
                rtbMessages.Text += "You receive " + _currentMonster.RewardExperiencePoints.ToString() + " experience points" + Environment.NewLine;

                // Give player gold for killing the monster 
                _player.Gold += _currentMonster.RewardGold;
                rtbMessages.Text += "You receive " + _currentMonster.RewardGold.ToString() + " gold" + Environment.NewLine;

                // Get random loot items from the monster
                List<InventoryItem> lootedItems = new List<InventoryItem>();

                // Add items to the lootedItems list, comparing a random number to the drop percentage
                foreach (LootItem lootItem in _currentMonster.LootTable)
                {
                    if (RandomNumberGenerator.NumberBetween(1, 100) <= lootItem.DropPercentage)
                    {
                        lootedItems.Add(new InventoryItem(lootItem.Details, 1));
                    }
                }

                // If no items were randomly selected, then add the default loot item(s).
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

                // Add the looted items to the player's inventory
                foreach (InventoryItem inventoryItem in lootedItems)
                {
                    _player.AddItemToInventory(inventoryItem.Details);

                    if (inventoryItem.Quantity == 1)
                    {
                        rtbMessages.Text += "You loot " + inventoryItem.Quantity.ToString() + " " + inventoryItem.Details.Name + Environment.NewLine;
                    }
                    else
                    {
                        rtbMessages.Text += "You loot " + inventoryItem.Quantity.ToString() + " " + inventoryItem.Details.NamePlural + Environment.NewLine;
                    }
                }

                // Refresh player information and inventory controls

                
                
                UpdateWeaponListInUI();
                UpdatePotionListInUI();

                // Add a blank line to the messages box, just for appearance.
                rtbMessages.Text += Environment.NewLine;

                // Move player to current location (to heal player and create a new monster to fight)
                MoveTo(_player.CurrentLocation);
            }
            else
            {
                // Monster is still alive

                // Determine the amount of damage the monster does to the player
                int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);

                // Display message
                rtbMessages.Text += "The " + _currentMonster.Name + " did " + damageToPlayer.ToString() + " points of damage." + Environment.NewLine;

                // Subtract damage from player
                _player.CurrentHitPoints -= damageToPlayer;

                

                if (_player.CurrentHitPoints <= 0)
                {
                    // Display message
                    rtbMessages.Text += "The " + _currentMonster.Name + " killed you." + Environment.NewLine;

                    // Move player to "Home"
                    MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
                }
            }

        }

        private void btnUsePotion_Click_1(object sender, EventArgs e)
        {// Get the currently selected potion from the combobox
            HealingPotion potion = (HealingPotion)cboPotions.SelectedItem;

            // Add healing amount to the player's current hit points
            _player.CurrentHitPoints = (_player.CurrentHitPoints + potion.AmountToHeal);

            // CurrentHitPoints cannot exceed player's MaximumHitPoints
            if (_player.CurrentHitPoints > _player.MaximumHitPoints)
            {
                _player.CurrentHitPoints = _player.MaximumHitPoints;
            }

            // Remove the potion from the player's inventory
            foreach (InventoryItem ii in _player.Inventory)
            {
                if (ii.Details.ID == potion.ID)
                {
                    ii.Quantity--;
                    break;
                }
            }

            // Display message
            rtbMessages.Text += "You drink a " + potion.Name + Environment.NewLine;

            // Monster gets their turn to attack

            // Determine the amount of damage the monster does to the player
            int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);

            // Display message
            rtbMessages.Text += "The " + _currentMonster.Name + " did " + damageToPlayer.ToString() + " points of damage." + Environment.NewLine;

            // Subtract damage from player
            _player.CurrentHitPoints -= damageToPlayer;

            if (_player.CurrentHitPoints <= 0)
            {
                // Display message
                rtbMessages.Text += "The " + _currentMonster.Name + " killed you." + Environment.NewLine;

                // Move player to "Home"
                MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
            }

            // Refresh player data in UI
            
           
            UpdatePotionListInUI();

        }

        //Function created by double clciking the rtb that scrolls the rtb to the bottom everytime a new line is added. Default behavior
        //causes the box to start at the top everytime and the player has to scroll. Very annoying.
        private void rtbMessages_TextChanged(object sender, EventArgs e)
        {
            rtbMessages.SelectionStart = rtbMessages.Text.Length;
            rtbMessages.ScrollToCaret();
        }
        //double clicking in the desing view is pretty slick
        private void SuperAdventure_FormClosing(object sender, FormClosingEventArgs e)
        {

            File.WriteAllText(PLAYER_DATA_FILE_NAME, _player.ToXmlString());
        }
    }


}
