using System;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI.Events;
using StardewValley.Buildings;
using Newtonsoft.Json;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using StardewValley.Objects;
using static ActionSpace.actions.Actions;
using System.IO;
using StardewValley.Objects;

namespace ActionSpace.actions
{
	public static class ActionsAPI
	{
        private static void LogToFile(string message, Mod mod)
        {
            try
            {
                string logFilePath = Path.Combine(mod.Helper.DirectoryPath, "MyModLog.txt");
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                mod.Monitor.Log($"Failed to write log: {ex.Message}", LogLevel.Error);
            }
        }

        public static async Task<bool> move(string x, string y, Mod mod)
        {
            if (Game1.activeClickableMenu is not null){
                return false;
            }

            var taskCompletionSource = new TaskCompletionSource<bool>();
            int xI = int.Parse(x);
            int yI = int.Parse(y);

            LogToFile($"starting moving to x:{xI}, y:{yI}", mod);
            void OnWarped(object? sender, WarpedEventArgs e)
            {
                LogToFile($"moving terminated by warp x:{xI}, y:{yI}", mod);
                taskCompletionSource.TrySetResult(true);
                mod.Helper.Events.Player.Warped -= OnWarped;
            }
            mod.Helper.Events.Player.Warped += OnWarped;
            Action<bool> onComplete = (success) => {
                LogToFile($"moving terminated by complete x:{xI}, y:{yI}", mod);
                mod.Helper.Events.Player.Warped -= OnWarped;
                taskCompletionSource.TrySetResult(success);
            };
            Actions.StartAutoPathing(new Vector2(xI, yI), onComplete, mod);
            LogToFile($"awaiting moving x:{xI}, y:{yI}", mod);

            bool success = await taskCompletionSource.Task;
            LogToFile($"moving completed, status = {success}, x:{xI}, y:{yI}", mod);

            return success;
        }

        public static async Task<bool> move_relative(string x, string y, Mod mod)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            int xRelative = int.Parse(x);
            int yRelative = int.Parse(y);
            int xOrigin = Game1.player.TilePoint.X;
            int yOrigin = Game1.player.TilePoint.Y;
            int xI = xOrigin + xRelative;
            int yI = yOrigin + yRelative;
            void OnWarped(object? sender, WarpedEventArgs e)
            {
                if (!taskCompletionSource.Task.IsCompleted)
                {
                    taskCompletionSource.SetResult(true);
                }
                mod.Helper.Events.Player.Warped -= OnWarped;
            }
            mod.Helper.Events.Player.Warped += OnWarped;
            Action<bool> onComplete = (success) => {
                mod.Helper.Events.Player.Warped -= OnWarped;
                if (!taskCompletionSource.Task.IsCompleted)
                {
                    taskCompletionSource.SetResult(success);
                }
            };
            Actions.StartAutoPathing(new Vector2(xI, yI), onComplete, mod);

            bool success = await taskCompletionSource.Task;
            return success;
        }

        public static async Task<bool> move_step(string direction, Mod mod)
        {
            if (Game1.activeClickableMenu is not null)
            {
                return false;
            }
            var status = await Actions.move(direction, mod);
            return status;
        }

        public static void use(Mod mod)
        {
            //Actions.use(mod);
            if (Game1.activeClickableMenu is not null)
            {
                return;
            }
            Actions.useWithAnim(mod);
        }

        public static void turn(string direction, Mod mod)
        {
            if (Game1.activeClickableMenu is not null)
            {
                return;
            }
            Actions.turn(int.Parse(direction), mod);
        }

        public static void interact(Mod mod)
        {
            if (Game1.activeClickableMenu is not null)
            {
                return;
            }
            Actions.interact(mod);
        }

        public static void craft(string item, Mod mod)
        {
            if (Game1.activeClickableMenu is not null)
            {
                return;
            }
            Actions.craft(item, mod);
        }

        public static void choose_option(string index, string quality, string direction, Mod mod)
        {
            int indexI = int.Parse(index);
            int qualityI = int.Parse(quality);
            int directionI = int.Parse(direction);
            if (indexI <= 0)
            {
                Actions.exit_menu();
            }
            else
            {
                indexI = indexI - 1;
            }
            if (Game1.activeClickableMenu is ShopMenu)
            {
                if (directionI == 0)
                {
                    Actions.buy_from_shop(indexI, qualityI, mod);
                }
                else
                {
                    Actions.sell_to_shop_by_index(indexI, mod);
                }
            }
            else if (Game1.activeClickableMenu is DialogueBox)
            {
                Actions.select_dialogue(indexI, mod);
            }
            else if (Game1.activeClickableMenu is ItemGrabMenu)
            {
                if (directionI == 0)
                {
                    take_from_chest(index, quality, mod);
                }
                else
                {
                    put_to_chest(index, quality, mod);
                }
            }
            else
            {
                Actions.exit_menu();
            }
        }

        public static void take_from_chest(string index, string quantity, Mod mod)
        {
            var menu = Game1.activeClickableMenu;
            if (menu is ItemGrabMenu itemGrabMenu)
            {
                if (itemGrabMenu.context is Chest chest)
                {
                    var indexI = int.Parse(index);
                    var quantityI = int.Parse(quantity);
                    Helper.ChestHelper.TakeXItemsFromChest(chest, indexI, quantityI);
                }
            }
        }

        public static void put_to_chest(string index, string quantity, Mod mod)
        {
            var menu = Game1.activeClickableMenu;
            if (menu is ItemGrabMenu itemGrabMenu)
            {
                if (itemGrabMenu.context is Chest chest)
                {
                    var indexI = int.Parse(index);
                    var quantityI = int.Parse(quantity);
                    Helper.ChestHelper.PutXItemsIntoChest(chest, indexI, quantityI);
                }
            }
        }

        public static void sell_current_item(Mod mod)
        {
            Actions.sell_to_shop(mod);
        }

        public static void choose_item(string index, Mod mod)
        {
            int indexI = int.Parse(index);
            if (indexI >= 0 && indexI < Game1.player.MaxItems)
            {
                var item = Game1.player.Items[indexI];
                Game1.player.CurrentToolIndex = indexI;
            }
        }

        public static void attach(string index, Mod mod)
        {
            int indexI = int.Parse(index);
            if (indexI >= 0 && indexI < Game1.player.MaxItems)
            {
                var item = Game1.player.Items[indexI];
                if (item is StardewValley.Object obj)
                {
                    Actions.attach(obj, mod);
                }
            }
        }

        public static void detach(Mod mod)
        {
            Actions.detach(mod);
        }

        public static string observe_v2(string sizeS, Mod mod)
        {
            var size = int.Parse(sizeS);
            var data = Actions.ExportGameData_v2(size, mod);
            mod.Monitor.Log("data received");
            return data;
        }

        public static byte[]? observe(string sizeS, Mod mod)
        {
            var size = int.Parse(sizeS);
            var data = Actions.ExportGameData(size, mod);
            mod.Monitor.Log("data received");
            return data;
        }

        public static void exit_menu(Mod mod)
        {
            Actions.exit_menu();
        }

        public async static Task<bool> wait_game_start(Mod mod)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Action onComplete = () => taskCompletionSource.TrySetResult(true);
            EventHandler<DayStartedEventArgs>? dayStarted = null;
            EventHandler<UpdateTickedEventArgs>? counterUpdate = null;
            var count = 0;
            counterUpdate = (object? sender, UpdateTickedEventArgs e) =>
            {
                if (count < 100)
                {
                    count += 1;
                    return;
                }
                else
                {
                    mod.Helper.Events.GameLoop.UpdateTicked -= counterUpdate;
                    onComplete();
                }
            };
            dayStarted = (object? sender, DayStartedEventArgs e) =>
            {
                mod.Helper.Events.GameLoop.DayStarted -= dayStarted;
                mod.Helper.Events.GameLoop.UpdateTicked += counterUpdate;
            };
            mod.Helper.Events.GameLoop.DayStarted += dayStarted;
            await taskCompletionSource.Task;
            return true;
        }

        public async static Task<bool> enter_load_menu(Mod mod)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Action onComplete = () => taskCompletionSource.SetResult(true);
            testUtils.TestUtils.enterLoadGameMenu(mod, onComplete);
            await taskCompletionSource.Task;
            return true;
        }

        public async static Task<bool> load_game_record(string record_name, Mod mod)
        {
            mod.Monitor.Log($"Try loading game: {record_name}");
            Actions.clearDayStartRecords();
            try
            {
                SaveGame.Load(record_name);
                IClickableMenu activeClickableMenu = Game1.activeClickableMenu;
                TitleMenu val;
                if ((val = (TitleMenu)(object)((activeClickableMenu is TitleMenu) ? activeClickableMenu : null)) != null)
                {
                    ((IClickableMenu)val).exitThisMenu(false);
                }
            }
            catch (Exception ex)
            {
                mod.Monitor.Log(ex.Message);
            }
            mod.Monitor.Log($"Successfully loaded game: {record_name}");
            return true;
        }


        public async static Task<bool> load_game(string which, Mod mod)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Action onComplete = () => taskCompletionSource.SetResult(true);
            mod.Monitor.Log($"Try loading game: {which}");
            LogToFile($"loading game: {which}", mod);
            try
            {
                testUtils.TestUtils.loadGame(which, mod, onComplete);
            }
            catch (Exception ex)
            {
                mod.Monitor.Log(ex.Message);
            }
            mod.Monitor.Log($"Successfully loaded game: {which}");
            LogToFile($"Successfully loaded game: {which}", mod);

            await taskCompletionSource.Task;
            return true;
        }

        public static void exit_title(Mod mod)
        {
            testUtils.TestUtils.exitGameToTitle();
        }

        public static void open_map(Mod mod)
        {
            Game1.activeClickableMenu = new GameMenu();
            if (Game1.activeClickableMenu is GameMenu newMenu)
            {
                newMenu.currentTab = 3;
            }
        }

        public static async Task<bool> navigate(string name, Mod mod)
        {
            foreach (Warp warp in Game1.currentLocation.warps.ToList())
            {
                if (name == warp.TargetName)
                {
                    var res = await move(warp.X.ToString(), warp.Y.ToString(), mod);
                    if (res)
                    {
                        return true;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            return false;
        }

        public static void pause(Mod mod)
        {
            Game1.paused = true;
        }

        public static void resume(Mod mod)
        {
            Game1.paused = false;
        }

        public static string get_surroundings(string sizeS, Mod mod)
        {
            int size = int.Parse(sizeS);
            var playPoint = Game1.player.TilePoint;
            int xI = playPoint.X;
            int yI = playPoint.Y;

            var layer = Game1.player.currentLocation.Map.GetLayer("Back");

            int mapWidth = layer.LayerWidth;
            int mapHeight = layer.LayerHeight;
            int minX = Math.Max(0, xI - size);
            int maxX = Math.Min(mapWidth - 1, xI + size);
            int minY = Math.Max(0, yI - size);
            int maxY = Math.Min(mapHeight - 1, yI + size);

            var tileInfoList = new List<string>();

           
            for (int tileX = minX; tileX <= maxX; tileX++)
            {
                for (int tileY = minY; tileY <= maxY; tileY++)
                {
                    
                    string infoJson = get_tile_info(tileX.ToString(), tileY.ToString(), mod);
                    tileInfoList.Add(infoJson);
                }
            }
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };
            var j_info = JsonConvert.SerializeObject(tileInfoList, settings);
            return j_info;
        }

        public static string get_tile_info(string x, string y, Mod mod)
        {
            int xI = int.Parse(x);
            int yI = int.Parse(y);
            var key = new Vector2(xI, yI);
            object object_info = "";
            object terrain_info = "";
            object builing_info = "";
            object crop_info = "";
            object? debris_info = "";
            object? furniture_info = "";
            if (Game1.currentLocation.objects.ContainsKey(key))
            {
                object_info = Game1.currentLocation.objects[key].BaseName;
            }
            if (Game1.currentLocation.terrainFeatures.ContainsKey(key)){
                terrain_info = Game1.currentLocation.terrainFeatures[key].GetType().ToString();
            }
            if (Game1.currentLocation.buildings is not null)
            {
                foreach (Building building in Game1.currentLocation.buildings)
                {
                    var box = building.GetBoundingBox();
                    var tileSize = Game1.tileSize;
                    var xP = xI * tileSize;
                    var yP = yI * tileSize;
                    if (box.Contains(new Point(xP, yP)))
                    {
                        builing_info = building.buildingType.Value;
                    }
                }
            }
            foreach (Debris debris in Game1.currentLocation.debris.ToList())
            {
                foreach (Chunk chunk in debris.Chunks.ToList())
                {
                    int chunkTileX = (int)(chunk.position.X / Game1.tileSize);
                    int chunkTileY = (int)(chunk.position.Y / Game1.tileSize);
                    
                    if (chunkTileX == xI && chunkTileY == yI)
                    {
                        debris_info = debris?.item?.BaseName;
                        if (debris_info is null)
                        {
                            debris_info = debris?.itemId?.Value;
                        }
                    }
                }
            }
            if (Game1.currentLocation is Farm farm)
            {
                if (farm.GetMainMailboxPosition().X == xI && farm.GetMainMailboxPosition().Y == yI)
                {
                    builing_info = "mailbox";
                }
            }
            if (Game1.currentLocation.terrainFeatures.TryGetValue(key, out var value) && value is HoeDirt hoeDirt && hoeDirt.crop != null)
            {
                var crop = hoeDirt.crop;
                crop_info = new
                {
                    seed_id = crop.netSeedIndex.Value,
                    is_dead = crop.dead.Value,
                    forage_crop = crop.forageCrop.Value,
                    fully_grown = crop.fullyGrown.Value,
                    current_phase = crop.currentPhase.Value,
                    index_harvest = crop.indexOfHarvest.Value
                };
            }
            var position = new List<int>();
            position.Add(xI);
            position.Add(yI);

            if (Game1.currentLocation.furniture is not null)
            {
                var furnitureList = Game1.currentLocation.furniture.ToList();
                foreach (var furnitureItem in furnitureList)
                {
                    if (furnitureItem.GetBoundingBox().Contains(new Point(position[0] * Game1.tileSize, position[1] * Game1.tileSize)))
                    {
                        furniture_info = furnitureItem.BaseName;
                    }
                }
            }

            var tile_info = new
            {
                position = position,
                object_at_tile = object_info,
                terrain_at_tile = terrain_info,
                building_info = builing_info,
                crop_at_tile = crop_info,
                debris_at_tile = debris_info,
                furniture_at_tile = furniture_info,
                placeable = Game1.currentLocation.isTilePlaceable(key)
            };
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };
            var j_info = JsonConvert.SerializeObject(tile_info, settings);
            return j_info;
        }

    }
}

