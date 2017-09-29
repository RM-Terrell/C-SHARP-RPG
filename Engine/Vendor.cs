using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Engine
{
    public class Vendor : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public BindingList<InventoryItem> Inventory { get; private set; }

        public Vendor(string name)
        {
            Name = name;
            Inventory = new BindingList<InventoryItem>();
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

            OnPropertyChanged("Inventory");
        }

        public void RemoveItemFromInventory(Item itemToRemove, int quantity = 1)
        {
            InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == itemToRemove.ID);

            if (item == null)
            {
                // The item is not in the player's inventory, so ignore it.
                //TODO We might want to raise an error for this situation
            }
            else
            {
                item.Quantity -= quantity;

                // Don't allow negative quantities.
                //TODO We might want to raise an error for this situation
                if (item.Quantity < 0)
                {
                    item.Quantity = 0;
                }

                if (item.Quantity == 0)
                {
                    Inventory.Remove(item);
                }

                OnPropertyChanged("Inventory");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}