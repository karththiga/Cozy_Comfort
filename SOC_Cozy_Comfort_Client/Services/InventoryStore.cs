using System;
using System.Collections.Generic;
using System.Linq;
using SOC_Cozy_Comfort_Client.Models;

namespace SOC_Cozy_Comfort_Client.Services
{
    public static class InventoryStore
    {
        private static readonly object SyncLock = new object();
        private static int _nextId = 1;

        private static readonly Dictionary<string, List<InventoryItem>> Data = new Dictionary<string, List<InventoryItem>>
        {
            { "Manufacturer", new List<InventoryItem>
                {
                    new InventoryItem { Id = NextId(), Sku = "CC-WOOL-QUEEN", Name = "Wool Queen Blanket", Quantity = 5420, Location = "Factory A", LastUpdated = DateTime.Now },
                    new InventoryItem { Id = NextId(), Sku = "CC-COTTON-KING", Name = "Cotton King Blanket", Quantity = 2210, Location = "Factory B", LastUpdated = DateTime.Now }
                }
            },
            { "Distributor", new List<InventoryItem>
                {
                    new InventoryItem { Id = NextId(), Sku = "CC-WOOL-QUEEN", Name = "Wool Queen Blanket", Quantity = 640, Location = "Central Warehouse", LastUpdated = DateTime.Now },
                    new InventoryItem { Id = NextId(), Sku = "CC-FLEECE-SINGLE", Name = "Fleece Single Blanket", Quantity = 190, Location = "North Hub", LastUpdated = DateTime.Now }
                }
            },
            { "Seller", new List<InventoryItem>
                {
                    new InventoryItem { Id = NextId(), Sku = "CC-COTTON-KING", Name = "Cotton King Blanket", Quantity = 24, Location = "Store A-12", LastUpdated = DateTime.Now },
                    new InventoryItem { Id = NextId(), Sku = "CC-FLEECE-SINGLE", Name = "Fleece Single Blanket", Quantity = 16, Location = "Store A-12", LastUpdated = DateTime.Now }
                }
            }
        };

        public static List<InventoryItem> GetByRole(string role)
        {
            lock (SyncLock)
            {
                return Data.ContainsKey(role)
                    ? Data[role].OrderBy(x => x.Sku).Select(Clone).ToList()
                    : new List<InventoryItem>();
            }
        }

        public static InventoryItem Find(string role, int id)
        {
            lock (SyncLock)
            {
                if (!Data.ContainsKey(role)) return null;
                var item = Data[role].FirstOrDefault(x => x.Id == id);
                return item == null ? null : Clone(item);
            }
        }

        public static void Add(string role, InventoryItem item)
        {
            lock (SyncLock)
            {
                if (!Data.ContainsKey(role)) return;
                item.Id = NextId();
                item.LastUpdated = DateTime.Now;
                Data[role].Add(Clone(item));
            }
        }

        public static void Update(string role, InventoryItem item)
        {
            lock (SyncLock)
            {
                if (!Data.ContainsKey(role)) return;
                var existing = Data[role].FirstOrDefault(x => x.Id == item.Id);
                if (existing == null) return;

                existing.Sku = item.Sku;
                existing.Name = item.Name;
                existing.Quantity = item.Quantity;
                existing.Location = item.Location;
                existing.LastUpdated = DateTime.Now;
            }
        }

        public static void Delete(string role, int id)
        {
            lock (SyncLock)
            {
                if (!Data.ContainsKey(role)) return;
                var existing = Data[role].FirstOrDefault(x => x.Id == id);
                if (existing != null)
                {
                    Data[role].Remove(existing);
                }
            }
        }

        private static int NextId()
        {
            return _nextId++;
        }

        private static InventoryItem Clone(InventoryItem item)
        {
            return new InventoryItem
            {
                Id = item.Id,
                Sku = item.Sku,
                Name = item.Name,
                Quantity = item.Quantity,
                Location = item.Location,
                LastUpdated = item.LastUpdated
            };
        }
    }
}
