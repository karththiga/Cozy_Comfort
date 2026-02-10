using System;
using System.Collections.Generic;
using System.Linq;
using SOC_CozyComfort_API.Models;

namespace SOC_CozyComfort_API.Services
{
    public static class InventoryRepository
    {
        private static readonly object SyncLock = new object();
        private static int _nextId = 1;

        private static readonly Dictionary<string, List<InventoryItemDto>> Data = new Dictionary<string, List<InventoryItemDto>>
        {
            { "Manufacturer", new List<InventoryItemDto>
                {
                    new InventoryItemDto { Id = NextId(), Sku = "CC-WOOL-QUEEN", Name = "Wool Queen Blanket", Quantity = 5420, Location = "Factory A", LastUpdated = DateTime.Now },
                    new InventoryItemDto { Id = NextId(), Sku = "CC-COTTON-KING", Name = "Cotton King Blanket", Quantity = 2210, Location = "Factory B", LastUpdated = DateTime.Now }
                }
            },
            { "Distributor", new List<InventoryItemDto>
                {
                    new InventoryItemDto { Id = NextId(), Sku = "CC-WOOL-QUEEN", Name = "Wool Queen Blanket", Quantity = 640, Location = "Central Warehouse", LastUpdated = DateTime.Now },
                    new InventoryItemDto { Id = NextId(), Sku = "CC-FLEECE-SINGLE", Name = "Fleece Single Blanket", Quantity = 190, Location = "North Hub", LastUpdated = DateTime.Now }
                }
            },
            { "Seller", new List<InventoryItemDto>
                {
                    new InventoryItemDto { Id = NextId(), Sku = "CC-COTTON-KING", Name = "Cotton King Blanket", Quantity = 24, Location = "Store A-12", LastUpdated = DateTime.Now },
                    new InventoryItemDto { Id = NextId(), Sku = "CC-FLEECE-SINGLE", Name = "Fleece Single Blanket", Quantity = 16, Location = "Store A-12", LastUpdated = DateTime.Now }
                }
            }
        };

        public static bool IsValidRole(string role)
        {
            return Data.ContainsKey(role);
        }

        public static List<InventoryItemDto> GetByRole(string role)
        {
            lock (SyncLock)
            {
                if (!Data.ContainsKey(role)) return new List<InventoryItemDto>();
                return Data[role].OrderBy(x => x.Sku).Select(Clone).ToList();
            }
        }

        public static InventoryItemDto GetById(string role, int id)
        {
            lock (SyncLock)
            {
                if (!Data.ContainsKey(role)) return null;
                var item = Data[role].FirstOrDefault(x => x.Id == id);
                return item == null ? null : Clone(item);
            }
        }

        public static InventoryItemDto Add(string role, InventoryItemDto item)
        {
            lock (SyncLock)
            {
                item.Id = NextId();
                item.LastUpdated = DateTime.Now;
                var created = Clone(item);
                Data[role].Add(created);
                return Clone(created);
            }
        }

        public static bool Update(string role, int id, InventoryItemDto item)
        {
            lock (SyncLock)
            {
                if (!Data.ContainsKey(role)) return false;
                var existing = Data[role].FirstOrDefault(x => x.Id == id);
                if (existing == null) return false;

                existing.Sku = item.Sku;
                existing.Name = item.Name;
                existing.Quantity = item.Quantity;
                existing.Location = item.Location;
                existing.LastUpdated = DateTime.Now;
                return true;
            }
        }

        public static bool Delete(string role, int id)
        {
            lock (SyncLock)
            {
                if (!Data.ContainsKey(role)) return false;
                var existing = Data[role].FirstOrDefault(x => x.Id == id);
                if (existing == null) return false;
                Data[role].Remove(existing);
                return true;
            }
        }

        private static int NextId()
        {
            return _nextId++;
        }

        private static InventoryItemDto Clone(InventoryItemDto item)
        {
            return new InventoryItemDto
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
