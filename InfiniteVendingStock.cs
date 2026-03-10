using System;
using System.Collections.Generic;
using System.Reflection;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Infinite Vending Stock", "Rustic0, Improvements made by SeesAll", "1.3.1")]
    [Description("Keeps stock high for vanilla NPC vending machines without interfering with CustomVendingSetup-managed machines.")]
    public class InfiniteVendingStock : RustPlugin
    {
        [PluginReference]
        private Plugin CustomVendingSetup;

        private const int TargetSalesPerOrder = 100000;

        private readonly HashSet<ulong> _queuedRefreshes = new HashSet<ulong>();
        private readonly HashSet<ulong> _queuedUiUpdates = new HashSet<ulong>();

        private static readonly FieldInfo SellOrderItemToSellSkinField = typeof(ProtoBuf.VendingMachine.SellOrder).GetField("itemToSellSkin");
        private static readonly FieldInfo SellOrderItemToSellAsBpField = typeof(ProtoBuf.VendingMachine.SellOrder).GetField("itemToSellAsBP");

        private void Unload()
        {
            _queuedRefreshes.Clear();
            _queuedUiUpdates.Clear();
        }

        private void OnServerInitialized()
        {
            foreach (BaseNetworkable entity in BaseNetworkable.serverEntities)
            {
                NPCVendingMachine vendingMachine = entity as NPCVendingMachine;
                if (vendingMachine == null)
                    continue;

                QueueRefresh(vendingMachine);
            }
        }

        private void OnEntitySpawned(NPCVendingMachine vendingMachine)
        {
            QueueRefresh(vendingMachine);
        }

        private void OnVendingTransaction(NPCVendingMachine vendingMachine)
        {
            QueueRefresh(vendingMachine);
        }

        private void OnRefreshVendingStock(NPCVendingMachine vendingMachine, Item item)
        {
            if (!ShouldManage(vendingMachine))
                return;

            NextTick(delegate()
            {
                if (!ShouldManage(vendingMachine))
                    return;

                if (item != null)
                    RaiseItemToTarget(vendingMachine, item);

                QueueRefresh(vendingMachine);
            });
        }

        private void QueueRefresh(NPCVendingMachine vendingMachine)
        {
            if (!ShouldManage(vendingMachine))
                return;

            ulong networkId = GetNetworkId(vendingMachine);
            if (!_queuedRefreshes.Add(networkId))
                return;

            NextTick(delegate()
            {
                _queuedRefreshes.Remove(networkId);

                if (!ShouldManage(vendingMachine))
                    return;

                ApplyInfiniteStock(vendingMachine);
            });
        }

        private void ApplyInfiniteStock(NPCVendingMachine vendingMachine)
        {
            ItemContainer inventory = vendingMachine.inventory;
            if (inventory == null || inventory.itemList == null || vendingMachine.sellOrders == null || vendingMachine.sellOrders.sellOrders == null)
                return;

            List<Item> itemList = inventory.itemList;
            int orderCount = vendingMachine.sellOrders.sellOrders.Count;
            bool changed = false;

            for (int orderIndex = 0; orderIndex < orderCount; orderIndex++)
            {
                ProtoBuf.VendingMachine.SellOrder sellOrder = vendingMachine.sellOrders.sellOrders[orderIndex];
                if (sellOrder == null)
                    continue;

                int itemId = GetSoldItemId(sellOrder);
                int amountPerSale = GetSoldAmount(sellOrder);
                bool isBlueprint = GetSoldIsBlueprint(sellOrder);
                ulong skinId = GetSoldSkin(sellOrder);

                if (itemId == 0 || amountPerSale <= 0)
                    continue;

                int desiredAmount = GetDesiredBackingAmount(amountPerSale);
                if (desiredAmount <= 0)
                    continue;

                Item matchingItem = FindBackingItem(itemList, itemId, skinId, isBlueprint);
                if (matchingItem == null)
                    continue;

                if (matchingItem.amount < desiredAmount)
                {
                    matchingItem.amount = desiredAmount;
                    matchingItem.MarkDirty();
                    changed = true;
                }
            }

            if (!changed)
                return;

            vendingMachine.UpdateEmptyFlag();
            QueueUiUpdate(vendingMachine);
        }

        private void RaiseItemToTarget(NPCVendingMachine vendingMachine, Item item)
        {
            if (item == null || item.info == null || vendingMachine == null || vendingMachine.sellOrders == null || vendingMachine.sellOrders.sellOrders == null)
                return;

            int orderCount = vendingMachine.sellOrders.sellOrders.Count;
            for (int orderIndex = 0; orderIndex < orderCount; orderIndex++)
            {
                ProtoBuf.VendingMachine.SellOrder sellOrder = vendingMachine.sellOrders.sellOrders[orderIndex];
                if (sellOrder == null)
                    continue;

                int soldItemId = GetSoldItemId(sellOrder);
                if (soldItemId == 0)
                    continue;

                bool isBlueprint = GetSoldIsBlueprint(sellOrder);
                ulong skinId = GetSoldSkin(sellOrder);

                if (!ItemMatches(item, soldItemId, skinId, isBlueprint))
                    continue;

                int desiredAmount = GetDesiredBackingAmount(GetSoldAmount(sellOrder));
                if (desiredAmount > 0 && item.amount < desiredAmount)
                {
                    item.amount = desiredAmount;
                    item.MarkDirty();
                }

                return;
            }
        }

        private void QueueUiUpdate(NPCVendingMachine vendingMachine)
        {
            if (!ShouldManage(vendingMachine))
                return;

            ulong networkId = GetNetworkId(vendingMachine);
            if (!_queuedUiUpdates.Add(networkId))
                return;

            NextTick(delegate()
            {
                _queuedUiUpdates.Remove(networkId);

                if (!ShouldManage(vendingMachine))
                    return;

                vendingMachine.SendNetworkUpdateImmediate();
            });
        }

        private bool ShouldManage(NPCVendingMachine vendingMachine)
        {
            if (vendingMachine == null || vendingMachine.IsDestroyed)
                return false;

            if (CustomVendingSetup == null)
                return true;

            object isCustomized = CustomVendingSetup.Call("API_IsCustomized", vendingMachine);
            return !(isCustomized is bool) || !(bool)isCustomized;
        }

        private static ulong GetNetworkId(NPCVendingMachine vendingMachine)
        {
            if (vendingMachine == null || vendingMachine.net == null)
                return 0UL;

            try
            {
                return Convert.ToUInt64(vendingMachine.net.ID.Value);
            }
            catch
            {
                try
                {
                    return Convert.ToUInt64(vendingMachine.net.ID);
                }
                catch
                {
                    return 0UL;
                }
            }
        }

        private static int GetDesiredBackingAmount(int amountPerSale)
        {
            if (amountPerSale <= 0)
                return 0;

            long desired = (long)amountPerSale * TargetSalesPerOrder;
            return desired > int.MaxValue ? int.MaxValue : (int)desired;
        }

        private static Item FindBackingItem(List<Item> itemList, int soldItemId, ulong soldSkinId, bool soldAsBlueprint)
        {
            int backingItemId = soldAsBlueprint && ItemManager.blueprintBaseDef != null
                ? ItemManager.blueprintBaseDef.itemid
                : soldItemId;

            int blueprintTarget = soldAsBlueprint ? soldItemId : 0;

            for (int i = 0; i < itemList.Count; i++)
            {
                Item item = itemList[i];
                if (item == null || item.info == null)
                    continue;

                if (item.info.itemid != backingItemId)
                    continue;

                if (item.blueprintTarget != blueprintTarget)
                    continue;

                if (soldSkinId != 0UL && item.skin != soldSkinId)
                    continue;

                return item;
            }

            return null;
        }

        private static bool ItemMatches(Item item, int soldItemId, ulong soldSkinId, bool soldAsBlueprint)
        {
            if (item == null || item.info == null)
                return false;

            int backingItemId = soldAsBlueprint && ItemManager.blueprintBaseDef != null
                ? ItemManager.blueprintBaseDef.itemid
                : soldItemId;

            int blueprintTarget = soldAsBlueprint ? soldItemId : 0;

            if (item.info.itemid != backingItemId)
                return false;

            if (item.blueprintTarget != blueprintTarget)
                return false;

            if (soldSkinId != 0UL && item.skin != soldSkinId)
                return false;

            return true;
        }

        private static int GetSoldItemId(ProtoBuf.VendingMachine.SellOrder sellOrder)
        {
            return sellOrder == null ? 0 : sellOrder.itemToSellID;
        }

        private static int GetSoldAmount(ProtoBuf.VendingMachine.SellOrder sellOrder)
        {
            return sellOrder == null ? 0 : sellOrder.itemToSellAmount;
        }

        private static ulong GetSoldSkin(ProtoBuf.VendingMachine.SellOrder sellOrder)
        {
            if (sellOrder == null || SellOrderItemToSellSkinField == null)
                return 0UL;

            object value = SellOrderItemToSellSkinField.GetValue(sellOrder);
            if (value == null)
                return 0UL;

            try
            {
                return Convert.ToUInt64(value);
            }
            catch
            {
                return 0UL;
            }
        }

        private static bool GetSoldIsBlueprint(ProtoBuf.VendingMachine.SellOrder sellOrder)
        {
            if (sellOrder == null)
                return false;

            if (sellOrder.itemToSellIsBP)
                return true;

            if (SellOrderItemToSellAsBpField == null)
                return false;

            object value = SellOrderItemToSellAsBpField.GetValue(sellOrder);
            return value is bool && (bool)value;
        }
    }
}
