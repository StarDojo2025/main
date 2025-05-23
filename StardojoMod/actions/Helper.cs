using System;
using Netcode;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using xTile.Layers;
using StardewValley.Objects;

namespace ActionSpace.actions
{
	public static class Helper
	{
        public static bool myTryToBuild(CarpenterMenu carpenterMenu, int x_bias, int y_bias)
        {
            NetString skinId = carpenterMenu.currentBuilding.skinId;
            Vector2 tileLocation = new Vector2((Game1.viewport.X + x_bias) / 64, (Game1.viewport.Y + y_bias) / 64);
            if (carpenterMenu.TargetLocation.buildStructure(carpenterMenu.currentBuilding.buildingType.Value, tileLocation, Game1.player, out var constructed, carpenterMenu.Blueprint.MagicalConstruction))
            {
                constructed.skinId.Value = skinId.ToString();
                if (constructed.isUnderConstruction())
                {
                    Game1.netWorldState.Value.MarkUnderConstruction(carpenterMenu.Builder, constructed);
                }
                return true;
            }
            return false;
        }

        public static bool checkDoor()
        {
            var playerFacingP = getFacingPoint();
            foreach (var building in Game1.currentLocation.buildings)
            {
                var doorP = building.getPointForHumanDoor();
                if (playerFacingP.X == doorP.X && playerFacingP.Y == doorP.Y)
                {
                    return true;
                }
            }
            return false;
        }

        public static Point getFacingPoint()
        {
            var player = Game1.player;
            var facingDirection = player.getFacingDirection();
            var curP = new Point(player.TilePoint.X, player.TilePoint.Y);
            switch (facingDirection)
            {
                case 0:
                    curP.X += 1;
                    break;
                case 1:
                    curP.Y += 1;
                    break;
                case 2:
                    curP.X -= 1;
                    break;
                case 3:
                    curP.Y -= 1;
                    break;
                default:
                    break;
            }
            return curP;
        }
        
        public static byte[,,] ConvertTo3ChannelMatrix(Color[,] colorData)
        {
            int width = colorData.GetLength(0);
            int height = colorData.GetLength(1);

    
            byte[,,] rgbMatrix = new byte[width, height, 3];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color color = colorData[x, y];
                    rgbMatrix[x, y, 0] = color.R; 
                    rgbMatrix[x, y, 1] = color.G; 
                    rgbMatrix[x, y, 2] = color.B; 
                }
            }

            return rgbMatrix;
        }

        public static class ChestHelper
        {
            /// <summary>
            /// 从给定 Chest 的指定物品索引中，取出指定数量并尽量放进玩家背包。
            /// 如果背包放不下全部，剩余的退回 Chest。
            /// </summary>
            /// <param name="chest">箱子对象</param>
            /// <param name="itemIndex">箱子里物品的索引（Chest.items[itemIndex]）</param>
            /// <param name="amount">想要获取的数量</param>
            public static void TakeXItemsFromChest(Chest chest, int itemIndex, int amount)
            {

                if (chest == null || itemIndex < 0 || itemIndex >= chest.Items.Count || amount <= 0)
                    return;

                Item chestItem = chest.Items[itemIndex];
                if (chestItem == null || chestItem.Stack <= 0)
                    return;


                int canTake = Math.Min(chestItem.Stack, amount);


                Item itemToTake = chestItem.getOne();
                itemToTake.Stack = canTake;


                chestItem.Stack -= canTake;


                if (chestItem.Stack <= 0)
                {
                    chest.Items[itemIndex] = null; // chest.items.RemoveAt(itemIndex);
                    chest.clearNulls(); // null
                }


                Item leftover = Game1.player.addItemToInventory(itemToTake);


                if (leftover != null)
                {

                    ReturnLeftoverToChest(chest, leftover);
                }
            }

            public static void PutXItemsIntoChest(Chest chest, int playerInventoryIndex, int amount)
            {

                if (chest == null
                    || playerInventoryIndex < 0
                    || playerInventoryIndex >= Game1.player.Items.Count
                    || amount <= 0)
                    return;

                Item inventoryItem = Game1.player.Items[playerInventoryIndex];
                if (inventoryItem == null || inventoryItem.Stack <= 0)
                    return;


                int canPut = Math.Min(inventoryItem.Stack, amount);


                Item itemToPut = inventoryItem.getOne();
                itemToPut.Stack = canPut;


                inventoryItem.Stack -= canPut;
                if (inventoryItem.Stack <= 0)
                {
                    Game1.player.Items[playerInventoryIndex] = null;
                }


                Item leftover = chest.addItem(itemToPut);


                if (leftover != null)
                {
                    leftover = Game1.player.addItemToInventory(leftover);

                }


                chest.clearNulls();
            }
            /// <summary>
            /// 将剩余物品尝试放回到 Chest（可简单地加到第一个空位，或与相同类型堆叠）
            /// </summary>
            /// <param name="chest"></param>
            /// <param name="leftover"></param>
            private static void ReturnLeftoverToChest(Chest chest, Item leftover)
            {

                chest.Items.Add(leftover);
                chest.clearNulls();
            }
        }

        public static byte[,,] ConvertRenderTargetTo3ChannelMatrix(GraphicsDevice graphicsDevice, RenderTarget2D renderTarget)
        {
            
            int width = renderTarget.Width;
            int height = renderTarget.Height;

            
            Color[] colorArray = new Color[width * height];
            renderTarget.GetData(colorArray);

           
            byte[,,] rgbMatrix = new byte[width, height, 3];

            for (int i = 0; i < colorArray.Length; i++)
            {
                int x = i % width;
                int y = i / width;

                Color color = colorArray[i];
                rgbMatrix[x, y, 0] = color.R; // Red
                rgbMatrix[x, y, 1] = color.G; // Green
                rgbMatrix[x, y, 2] = color.B; // Blue
            }

            return rgbMatrix;
        }

    }

    public static class ChestHelper
    {
        /// <summary>
        /// 从给定 Chest 的指定物品索引中，取出指定数量并尽量放进玩家背包。
        /// 如果背包放不下全部，剩余的退回 Chest。
        /// </summary>
        /// <param name="chest">箱子对象</param>
        /// <param name="itemIndex">箱子里物品的索引（Chest.items[itemIndex]）</param>
        /// <param name="amount">想要获取的数量</param>
        public static void TakeXItemsFromChest(Chest chest, int itemIndex, int amount)
        {

            if (chest == null || itemIndex < 0 || itemIndex >= chest.Items.Count || amount <= 0)
                return;

            Item chestItem = chest.Items[itemIndex];
            if (chestItem == null || chestItem.Stack <= 0)
                return;


            int canTake = Math.Min(chestItem.Stack, amount);


            Item itemToTake = chestItem.getOne();
            itemToTake.Stack = canTake;

  
            chestItem.Stack -= canTake;

           
            if (chestItem.Stack <= 0)
            {
                chest.Items[itemIndex] = null; // chest.items.RemoveAt(itemIndex);
                chest.clearNulls(); // null
            }

            
            Item leftover = Game1.player.addItemToInventory(itemToTake);

           
            if (leftover != null)
            {
               
                ReturnLeftoverToChest(chest, leftover);
            }
        }

        public static void PutXItemsIntoChest(Chest chest, int playerInventoryIndex, int amount)
        {
           
            if (chest == null
                || playerInventoryIndex < 0
                || playerInventoryIndex >= Game1.player.Items.Count
                || amount <= 0)
                return;

            Item inventoryItem = Game1.player.Items[playerInventoryIndex];
            if (inventoryItem == null || inventoryItem.Stack <= 0)
                return; 

            
            int canPut = Math.Min(inventoryItem.Stack, amount);

        
            Item itemToPut = inventoryItem.getOne();
            itemToPut.Stack = canPut;

        
            inventoryItem.Stack -= canPut;
            if (inventoryItem.Stack <= 0)
            {
                Game1.player.Items[playerInventoryIndex] = null; 
            }

            
            Item leftover = chest.addItem(itemToPut);

         
            if (leftover != null)
            {
                leftover = Game1.player.addItemToInventory(leftover);
               
            }

           
            chest.clearNulls();
        }
        /// <summary>
        /// 将剩余物品尝试放回到 Chest（可简单地加到第一个空位，或与相同类型堆叠）
        /// </summary>
        /// <param name="chest"></param>
        /// <param name="leftover"></param>
        private static void ReturnLeftoverToChest(Chest chest, Item leftover)
        {
            
            chest.Items.Add(leftover);
            chest.clearNulls();
        }
    }
}

