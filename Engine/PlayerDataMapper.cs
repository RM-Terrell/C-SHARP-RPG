using System;
using System.Data;
using System.Data.SqlClient;

namespace Engine
{
    public static class PlayerDataMapper
    {
        private static readonly string _connectionString = "Data Source=(local);Initial Catalog=SuperAdventure;Integrated Security=True";

        public static Player CreateFromDatabase()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    Player player;

                    using (SqlCommand savedGameCommand = connection.CreateCommand())
                    {
                        savedGameCommand.CommandType = CommandType.Text;
                        savedGameCommand.CommandText = "SELECT TOP 1 * FROM SavedGame";

                        SqlDataReader reader = savedGameCommand.ExecuteReader();

                        if (!reader.HasRows)
                        {
                            return null;
                        }

                        reader.Read();

                        int currentHitPoints = (int)reader["CurrentHitPoints"];
                        int maximumHitPoints = (int)reader["MaximumHitPoints"];
                        int gold = (int)reader["Gold"];
                        int experiencePoints = (int)reader["ExperiencePoints"];
                        int currentLocationID = (int)reader["CurrentLocationID"];

                        player = Player.CreatePlayerFromDatabase(currentHitPoints, maximumHitPoints, gold,
                            experiencePoints, currentLocationID);
                    }

                    using (SqlCommand questCommand = connection.CreateCommand())
                    {
                        questCommand.CommandType = CommandType.Text;
                        questCommand.CommandText = "SELECT * FROM Quest";

                        SqlDataReader reader = questCommand.ExecuteReader();

                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int questID = (int)reader["QuestID"];
                                bool isCompleted = (bool)reader["IsCompleted"];

                                PlayerQuest playerQuest = new PlayerQuest(World.QuestByID(questID));
                                playerQuest.IsCompleted = isCompleted;

                                player.Quests.Add(playerQuest);
                            }
                        }
                    }

                    using (SqlCommand inventoryCommand = connection.CreateCommand())
                    {
                        inventoryCommand.CommandType = CommandType.Text;
                        inventoryCommand.CommandText = "SELECT * FROM Inventory";

                        SqlDataReader reader = inventoryCommand.ExecuteReader();

                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int inventoryItemID = (int)reader["InventoryItemID"];
                                int quantity = (int)reader["Quantity"];

                                player.AddItemToInventory(World.ItemByID(inventoryItemID), quantity);
                            }
                        }
                    }

                    return player;
                }
            }
            catch (Exception ex)
            {
                //TODO add some basic error handling
            }

            return null;
        }

        public static void SaveToDatabase(Player player)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (SqlCommand existingRowCountCommand = connection.CreateCommand())
                    {
                        existingRowCountCommand.CommandType = CommandType.Text;
                        existingRowCountCommand.CommandText = "SELECT count(*) FROM SavedGame";

                        int existingRowCount = (int)existingRowCountCommand.ExecuteScalar();

                        if (existingRowCount == 0)
                        {
                            using (SqlCommand insertSavedGame = connection.CreateCommand())
                            {
                                insertSavedGame.CommandType = CommandType.Text;
                                insertSavedGame.CommandText =
                                    "INSERT INTO SavedGame " +
                                    "(CurrentHitPoints, MaximumHitPoints, Gold, ExperiencePoints, CurrentLocationID) " +
                                    "VALUES " +
                                    "(@CurrentHitPoints, @MaximumHitPoints, @Gold, @ExperiencePoints, @CurrentLocationID)";

                                insertSavedGame.Parameters.Add("@CurrentHitPoints", SqlDbType.Int);
                                insertSavedGame.Parameters["@CurrentHitPoints"].Value = player.CurrentHitPoints;
                                insertSavedGame.Parameters.Add("@MaximumHitPoints", SqlDbType.Int);
                                insertSavedGame.Parameters["@MaximumHitPoints"].Value = player.MaximumHitPoints;
                                insertSavedGame.Parameters.Add("@Gold", SqlDbType.Int);
                                insertSavedGame.Parameters["@Gold"].Value = player.Gold;
                                insertSavedGame.Parameters.Add("@ExperiencePoints", SqlDbType.Int);
                                insertSavedGame.Parameters["@ExperiencePoints"].Value = player.ExperiencePoints;
                                insertSavedGame.Parameters.Add("@CurrentLocationID", SqlDbType.Int);
                                insertSavedGame.Parameters["@CurrentLocationID"].Value = player.CurrentLocation.ID;

                                insertSavedGame.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (SqlCommand updateSavedGame = connection.CreateCommand())
                            {
                                updateSavedGame.CommandType = CommandType.Text;
                                updateSavedGame.CommandText =
                                    "UPDATE SavedGame " +
                                    "SET CurrentHitPoints = @CurrentHitPoints, " +
                                    "MaximumHitPoints = @MaximumHitPoints, " +
                                    "Gold = @Gold, " +
                                    "ExperiencePoints = @ExperiencePoints, " +
                                    "CurrentLocationID = @CurrentLocationID";

                                updateSavedGame.Parameters.Add("@CurrentHitPoints", SqlDbType.Int);
                                updateSavedGame.Parameters["@CurrentHitPoints"].Value = player.CurrentHitPoints;
                                updateSavedGame.Parameters.Add("@MaximumHitPoints", SqlDbType.Int);
                                updateSavedGame.Parameters["@MaximumHitPoints"].Value = player.MaximumHitPoints;
                                updateSavedGame.Parameters.Add("@Gold", SqlDbType.Int);
                                updateSavedGame.Parameters["@Gold"].Value = player.Gold;
                                updateSavedGame.Parameters.Add("@ExperiencePoints", SqlDbType.Int);
                                updateSavedGame.Parameters["@ExperiencePoints"].Value = player.ExperiencePoints;
                                updateSavedGame.Parameters.Add("@CurrentLocationID", SqlDbType.Int);
                                updateSavedGame.Parameters["@CurrentLocationID"].Value = player.CurrentLocation.ID;

                                updateSavedGame.ExecuteNonQuery();
                            }
                        }
                    }

                    using (SqlCommand deleteQuestsCommand = connection.CreateCommand())
                    {
                        deleteQuestsCommand.CommandType = CommandType.Text;
                        deleteQuestsCommand.CommandText = "DELETE FROM Quest";

                        deleteQuestsCommand.ExecuteNonQuery();
                    }

                    foreach (PlayerQuest playerQuest in player.Quests)
                    {
                        using (SqlCommand insertQuestCommand = connection.CreateCommand())
                        {
                            insertQuestCommand.CommandType = CommandType.Text;
                            insertQuestCommand.CommandText = "INSERT INTO Quest (QuestID, IsCompleted) VALUES (@QuestID, @IsCompleted)";

                            insertQuestCommand.Parameters.Add("@QuestID", SqlDbType.Int);
                            insertQuestCommand.Parameters["@QuestID"].Value = playerQuest.Details.ID;
                            insertQuestCommand.Parameters.Add("@IsCompleted", SqlDbType.Bit);
                            insertQuestCommand.Parameters["@IsCompleted"].Value = playerQuest.IsCompleted;

                            insertQuestCommand.ExecuteNonQuery();
                        }
                    }

                    using (SqlCommand deleteInventoryCommand = connection.CreateCommand())
                    {
                        deleteInventoryCommand.CommandType = CommandType.Text;
                        deleteInventoryCommand.CommandText = "DELETE FROM Inventory";

                        deleteInventoryCommand.ExecuteNonQuery();
                    }

                    foreach (InventoryItem inventoryItem in player.Inventory)
                    {
                        using (SqlCommand insertInventoryCommand = connection.CreateCommand())
                        {
                            insertInventoryCommand.CommandType = CommandType.Text;
                            insertInventoryCommand.CommandText = "INSERT INTO Inventory (InventoryItemID, Quantity) VALUES (@InventoryItemID, @Quantity)";

                            insertInventoryCommand.Parameters.Add("@InventoryItemID", SqlDbType.Int);
                            insertInventoryCommand.Parameters["@InventoryItemID"].Value = inventoryItem.Details.ID;
                            insertInventoryCommand.Parameters.Add("@Quantity", SqlDbType.Int);
                            insertInventoryCommand.Parameters["@Quantity"].Value = inventoryItem.Quantity;

                            insertInventoryCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO add some basic error handling
            }
        }
    }
}
