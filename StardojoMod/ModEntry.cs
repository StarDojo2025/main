using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System;
using System.Text;
using StardewValley.Tools;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using testUtils;
using ActionSpace.actions;
using StardewValley.Menus;
using HarmonyLib;
using System.Net;
using System.Net.Sockets;
using ActionSpace.common;
using Microsoft.Xna.Framework.Graphics;
using AHelper = ActionSpace.actions.Helper;
using System.Reflection;
using MRectangle = Microsoft.Xna.Framework.Rectangle;
using InitTask;
using Microsoft.Xna.Framework.Input;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace observeSpaceTest
{
    public class ModEntry : Mod
    {
        private string? outputFilePath;
        private string? logFilePath;

        private Vector2 targetTile;

        private IReflectedMethod? drawMethod;
        private static int port = 10783;

        private IModHelper? _helper;

        private static string mmapFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"shared_memory_{port}.bin");

        public override void Entry(IModHelper helper)
        {
            Game1.options.pauseWhenOutOfFocus = false;
            outputFilePath = Path.Combine(helper.DirectoryPath, "game_data.json");
            Monitor.Log($"mmapFilePath: {mmapFilePath}", LogLevel.Info);

            logFilePath = Path.Combine(helper.DirectoryPath, "MyModLog.txt");

            Global.mainMod = this;
            _helper = helper;

            var harmony = new Harmony("com.xspadex.actionspace");
            harmony.PatchAll();

            drawMethod = helper.Reflection.GetMethod(Game1.game1, "Draw", false);

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--port-id" && i + 1 < args.Length)
                {
                    string portId = args[i + 1]; 
                    Monitor.Log($"Port ID: {portId}", LogLevel.Info);
                    port = int.Parse(portId);
                    break;
                }
                if (args[i] == "--sample-rate" && i + 1 < args.Length)
                {
                    string sampleRateS = args[i + 1]; 
                    Monitor.Log($"sample rate: {sampleRateS}", LogLevel.Info);
                    var sampleRate = int.Parse(sampleRateS);
                    Actions.initSampleRate(sampleRate);
                    break;
                }
            }

            mmapFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"shared_memory_{port}.bin");

            Task.Run(() => StartServer(port));

            InitMemoryMappedFile();

            // Hook into events
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            //helper.Events.Player.Warped += OnPlayerWarped;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.ConsoleCommands.Add("give_tool_1", "give the player a tool", giveTool);
            helper.ConsoleCommands.Add("tp_1", "teleport the player", tpPlayer);
            helper.ConsoleCommands.Add("fish_1", "start fishing", fish);
            helper.ConsoleCommands.Add("give_1", "give items", giveItems);
            helper.ConsoleCommands.Add("give_id_1", "give items by id", giveItemsById);
            helper.ConsoleCommands.Add("eat_1", "give items by id", testEat);
            helper.ConsoleCommands.Add("place_1", "place current items by tile", placeItem);
            helper.ConsoleCommands.Add("use_1", "use current item by tile", useItem);
            helper.ConsoleCommands.Add("drop_in_1", "drop item to current item", dropIn);
            helper.ConsoleCommands.Add("attach_1", "attach item to current tool", attach);
            helper.ConsoleCommands.Add("detach_1", "detach item from current tool", detach);
            helper.ConsoleCommands.Add("fire_1", "fire slingshot", fireSlingshot);
            helper.ConsoleCommands.Add("talk_1", "talk to nearest npc", talkNearest);
            helper.ConsoleCommands.Add("noon_1", "set time to 12:00", noon);
            helper.ConsoleCommands.Add("open_shop_1", "open shop", openShop);
            helper.ConsoleCommands.Add("close_shop_1", "close shop", closeShop);
            helper.ConsoleCommands.Add("money_1", "give_money", giveMoney);
            helper.ConsoleCommands.Add("buy_1", "buy from active shop", buyFromShop);
            helper.ConsoleCommands.Add("sell_1", "sell item to shop", sellToShop);
            helper.ConsoleCommands.Add("open_special_1", "open special shop", openSpecialShop);
            helper.ConsoleCommands.Add("buy_animal_1", "buy from animal shop", buyFromAnimalShop);
            helper.ConsoleCommands.Add("check_1", "check from animal shop", check);
            helper.ConsoleCommands.Add("action_1", "perform action in shop", action);
            helper.ConsoleCommands.Add("choose_1", "choose in a dialogue", chooseDialogue);
            helper.ConsoleCommands.Add("upgrade_1", "choose in a dialogue", upgrade);
            helper.ConsoleCommands.Add("backpack_1", "choose in a dialogue", backpackUpgrade);
            helper.ConsoleCommands.Add("answer_1", "choose in a dialogue", answerDialogue);
            helper.ConsoleCommands.Add("construct_1", "choose in a dialogue", construct);
            helper.ConsoleCommands.Add("check_door_1", "choose in a dialogue", checkDoor);
            helper.ConsoleCommands.Add("pause_1", "choose in a dialogue", pause);
            helper.ConsoleCommands.Add("resume_1", "choose in a dialogue", resume);
            helper.ConsoleCommands.Add("energy_1", "choose in a dialogue", energy);


            Actions.initPixelData();
        }

        private void energy(string command, string[] args)
        {
            var s = float.Parse(args[0]);
            Game1.player.stamina = s;
        }

        private void resume(string command, string[] args)
        {
            Game1.paused = false;
        }

        private void pause(string command, string[] args)
        {
            Game1.paused = true;
        }

        private async Task StartServer(int port)
        {
            TcpListener? server = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);
                server.Start();
                while (true)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    LogToFile("message received from client");
                    _ = HandleClientAsync(client);
                }
            }
            catch (SocketException e)
            {
                Monitor.Log($"SocketException: {e}", LogLevel.Error);
            }
            finally
            {
                server?.Stop();
            }
        }

        static string mmapFileName = "SharedMemoryFile"; // Windows/Linux memory
        //static string mmapFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"shared_memory.bin"); // Mac memory
        static int mmapSize = 8 * 1024 * 1024; // 8MB
        private static MemoryMappedFile mmf;
        private static MemoryMappedViewAccessor accessor;

        public static void InitMemoryMappedFile()
        {
            var fs = new FileStream(mmapFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            fs.SetLength(mmapSize);
            mmf = MemoryMappedFile.CreateFromFile(fs, null, mmapSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            accessor = mmf.CreateViewAccessor();
            
        }

        static async Task WriteToMemoryMappedFile(byte[] bytes)
        {
            accessor.Write(0, 0); 
            accessor.Write(4, bytes.Length); 
            accessor.WriteArray(8, bytes, 0, bytes.Length);
            accessor.Write(0, 1); 
        }

        private async Task HandleClientAsync(TcpClient client)
        {

            using (client)
            {
                NetworkStream stream = client.GetStream();
                byte[] bytes = new byte[256];
                int i;
                while ((i = await stream.ReadAsync(bytes, 0, bytes.Length)) != 0)
                {
                    LogToFile("start reading from stream");
                    string data = Encoding.ASCII.GetString(bytes, 0, i);
                    Monitor.Log($"Received: {data}", LogLevel.Debug);
                    LogToFile($"Received: {data}");

                    var usingTool = Game1.player.UsingTool;
                    var isEating = Game1.player.isEating;
                    var paused = Game1.paused;
                    var animating = Game1.player.FarmerSprite.IsPlayingBasicAnimation(Game1.player.facingDirection.Value, true) ||
                        Game1.player.FarmerSprite.IsPlayingBasicAnimation(Game1.player.facingDirection.Value, false);
                    var usingWeapon = Game1.player.FarmerSprite.isUsingWeapon();
                    var toolAnimation = Game1.player.FarmerSprite.isOnToolAnimation();
                    var passingOut = Game1.player.FarmerSprite.isPassingOut();
                    var activeClickableMenu = Game1.activeClickableMenu;
                    Monitor.Log($"waiting resaon before executing: usingTool '{usingTool}'; isEating {isEating}; paused {paused}; animating {animating}; usingWeapon {usingWeapon}; toolAnimation {toolAnimation}; passingOut {passingOut}; activeClickableMenu {activeClickableMenu} ", LogLevel.Debug);
                    LogToFile($"waiting resaon before executing: usingTool '{usingTool}'; isEating {isEating}; paused {paused}; animating {animating}; usingWeapon {usingWeapon}; toolAnimation {toolAnimation}; passingOut {passingOut}; activeClickableMenu {activeClickableMenu} ");
                    object? returnValue = await HandleMessage(data);
                    if (returnValue is Byte[] returnedBytes)
                    {
                        Monitor.Log($"Processed From Main: {data}", LogLevel.Debug);
                        LogToFile($"Processed From Main: {data}");
                        Monitor.Log($"return length：{returnedBytes.Length} bytes", LogLevel.Debug);
                        LogToFile($"return length：{returnedBytes.Length} bytes");
                        //byte[] sentSignal = Encoding.ASCII.GetBytes("sent");
                        //byte[] eofBytes = Encoding.ASCII.GetBytes("<EOF>");
                        Console.WriteLine("time_point_13: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                        await WriteToMemoryMappedFile(returnedBytes);
                        //await stream.WriteAsync(sentSignal, 0, sentSignal.Length);
                        //await stream.WriteAsync(eofBytes, 0, eofBytes.Length);
                        LogToFile($"written length：{returnedBytes.Length} bytes");
                        Console.WriteLine("time_point_14: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                    }
                    else
                    {
                        Monitor.Log($"Processed From Main: {data}", LogLevel.Debug);
                        LogToFile($"Processed From Main: {data}");
                        string? returnValueS = returnValue?.ToString();
                        byte[] msg = Encoding.ASCII.GetBytes((returnValueS ?? "Message received") + "<EOF>");
                        Monitor.Log($"return length：{msg.Length} bytes", LogLevel.Debug);
                        LogToFile($"return length：{msg.Length} bytes");
                        await stream.WriteAsync(msg, 0, msg.Length);
                        LogToFile($"written length：{msg.Length} bytes");
                    }
                }
            }
        }

        private async Task waitForReady(string methodName)
        {
            if (methodName == "resume" || methodName == "pause" || methodName == "observe" || methodName == "get_surroundings" || methodName == "load_game_record" || methodName == "observe_v2")
            {
                return;
            }

            Monitor.Log($"now wait for ready!", LogLevel.Debug);
            while (true)
            {
                var usingTool = Game1.player.UsingTool;
                var isEating = Game1.player.isEating;
                var paused = Game1.paused;
                var usingWeapon = Game1.player.FarmerSprite.isUsingWeapon();
                var toolAnimation = Game1.player.FarmerSprite.isOnToolAnimation();
                var passingOut = Game1.player.FarmerSprite.isPassingOut();
                var activeClickableMenu = Game1.activeClickableMenu;
                var fading = Game1.fadeToBlack;
                if (activeClickableMenu is StardewValley.Menus.SaveGameMenu)
                {
                    Monitor.Log($"waiting reason: activeClickableMenu {activeClickableMenu}", LogLevel.Debug);
                    await Task.Delay(100);
                    continue;
                }


//                var canExit = !usingTool && !isEating && !paused && !usingWeapon && !toolAnimation && !passingOut;
//                var canExit = !usingTool && !isEating && !paused && !animating && !usingWeapon && !toolAnimation && !passingOut;

                var canExit = !usingTool && !paused && !usingWeapon && !toolAnimation && !passingOut && !fading;
//                var canExit = !paused && !animating && !passingOut;
                if (canExit)
                {
                    break;
                }
                else
                {
                    Monitor.Log($"{methodName} waiting resaon: usingTool '{usingTool}'; isEating {isEating}; paused {paused}; usingWeapon {usingWeapon}; toolAnimation {toolAnimation}; passingOut {passingOut}; fading {fading}", LogLevel.Debug);
                    await Task.Delay(100);
                }
            }
            

        }

        private async Task<object?> HandleMessage(string message)
        {
            var tcs = new TaskCompletionSource<object?>();
            EventHandler<StardewModdingAPI.Events.UpdateTickedEventArgs>? updateTickedHandler = null;
            object? res = null;
            LogToFile($"begin handling message：{message}");
            updateTickedHandler = async (sender, e) =>
            {
                this.Helper.Events.GameLoop.UpdateTicked -= updateTickedHandler;
                LogToFile($"awaiting message in main：{message}");
                res = await HandleMessageInMain(message);
                LogToFile($"received message from main：message: {message}");
                tcs.SetResult(res);
            };
            this.Helper.Events.GameLoop.UpdateTicked += updateTickedHandler;
            return await tcs.Task;
        }
        private async Task<object?> HandleMessageInMain(string message)
        {
            Monitor.Log("Doing something on the main thread...", LogLevel.Info);
            LogToFile($"Doing something on the main thread... message：{message}");
            object? res = null;
            var parts = message.Split('%');
            // TODO CheckValid
            string methodName = parts[0]; 
            string[]? args = null;
            if (parts.Length > 1)
            {
                args = parts.Skip(1).ToArray();
            }
            var method = typeof(ActionsAPI).GetMethod(methodName);
            if (method == null)
            {
                method = typeof(InitTaskAPI).GetMethod(methodName);
            }
            LogToFile($"waiting for ready, method name：{methodName}");

            await waitForReady(methodName);
            LogToFile($"method is ready, method name：{methodName}");

            if (method != null)
            {
                try
                {
                    if (args != null)
                    {
                        object[] parameters = new object[args.Length + 1];
                        Array.Copy(args, parameters, args.Length);
                        parameters[args.Length] = this;
                        LogToFile($"invoking method with args, method name：{methodName}");

                        res = method.Invoke(null, parameters);
                        LogToFile($"invoked method with args, method name：{methodName}");

                    }
                    else
                    {
                        LogToFile($"invoking method without args, method name：{methodName}");

                        res = method.Invoke(null, new object[] { this });
                        LogToFile($"invoked method without args, method name：{methodName}");

                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error invoking method '{methodName}': {ex.Message}", LogLevel.Error);
                    LogToFile($"Error invoking method '{methodName}': {ex.Message}");

                }
            }
            else
            {
                Monitor.Log($"No such method '{methodName}' found in ActionsAPI or InitTaskAPI", LogLevel.Error);
                LogToFile($"No such method '{methodName}' found in ActionsAPI or InitTaskAPI");
            }

//            if (methodName == "resume" || methodName == "observe" || methodName == "get_surroundings" || methodName == "pause")
//            {
//                return res;
//            }

//            if (methodName == "observe" )
//            {
//                return res;
//            }

            if (res is Task task && res.GetType().IsGenericType)
            {
                Monitor.Log($"{methodName} waiting resaon: await new task {task}", LogLevel.Debug);
                var asyncValue = await (dynamic)task;
                return asyncValue;
            }
            return res;
            
        }

        private void getMousePos(string command, string[] args)
        {
            TestUtils.print_mouse_pos(this);
        }

        private void checkDoor(string command, string[] args)
        {
            var res = ActionSpace.actions.Helper.checkDoor();
            this.Monitor.Log($"checkDoor: {res}");
        }

        private void construct(string command, string[] args)
        {
            if (Game1.activeClickableMenu is CarpenterMenu && args.Length>=2)
            {
                int x = int.Parse(args[0]);
                int y = int.Parse(args[1]);
                Actions.build_current_building(x, y);
            }
            else
            {
                Actions.open_carpenter_construct();
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to write log: {ex.Message}", LogLevel.Error);
            }
        }

        private void answerDialogue(string command, string[] args)
        {
            if (args.Length < 2)
            {
                return;
            }
            string answerKey = args[0];
            string[] questionKey = ArgUtility.SplitBySpace(args[1]);
            Actions.answer_question(answerKey, questionKey);
        }


        private void backpackUpgrade(string command, string[] args)
        {
            Actions.upgrade_backpack();
        }

        private void upgrade(string command, string[] args)
        {
            Actions.upgrade_house();
        }

        private void chooseDialogue(string command, string[] args)
        {
            var index = 0;
            if (args.Length > 0)
            {
                index = int.Parse(args[0]);
            }
            Actions.select_dialogue(index, this);
        }

        private void action(string command, string[] args)
        {
            string actionId = "BuyBackpack";
            if (args.Length > 0)
            {
                actionId = args[0];
            }
            Actions.location_perform_action(actionId);
        }

        private void check(string command, string[] args)
        {
            var player = Game1.player;
            int player_x_pos = (int)player.TilePoint.X;
            int player_y_pos = (int)player.TilePoint.Y;
            var gameLocation = player.currentLocation;
            var location = new Location(player_x_pos, player_y_pos);
            var res = gameLocation.checkAction(location, Game1.viewport, player);
            Monitor.Log($"the res of checkAction is {res.ToString()}");
        }

        private void openSpecialShop(string command, string[] args)
        {
            if (args.Length <= 0)
            {
                return;
            }
            var shopId = args[0];
            switch (shopId)
            {
                case "AnimalShop":
                    Actions.open_animal_shop();
                    break;
                default:
                    break;
            }
        }

        private void giveMoney(string command, string[] args)
        {
            var amount = int.Parse(args[0]);
            TestUtils.give_money(amount);
        }

        private void sellToShop(string command, string[] args)
        {
            Actions.sell_to_shop(this);
        }

        private void buyFromAnimalShop(string command, string[] args)
        {
            Actions.buy_from_animals_shop(0, 4, "corn", this);
        }

        private void buyFromShop(string command, string[] args)
        {
            var menu = Game1.activeClickableMenu;
            var index = 0;
            var count = 1;
            if (args.Length >= 2)
            {
                index = int.Parse(args[0]);
                count = int.Parse(args[1]);
            }
            
            if (menu is ShopMenu shopMenu)
            {
                Actions.buy_from_shop(index, count, this);
            }
        }


        private void closeShop(string command, string[] args)
        {
            Actions.close_shop("AnimalShop", "Marnie", this);
        }


        private void openShop(string command, string[] args)
        {
            var shopId = "AnimalShop";
            var ownerId = "Marnie";
            if (args.Length >= 2)
            {
                shopId = args[0];
                ownerId = args[1];
            }
            Actions.open_shop(shopId, ownerId, this);
        }

        private void noon(string command, string[] args)
        {
            TestUtils.set_time(this);
        }

        private void talkNearest(string command, string[] args)
        {
            Actions.talk(this);
        }

        private void fireSlingshot(string command, string[] args)
        {
            var player = Game1.player;
            int player_x_pos = (int)player.TilePoint.X;
            int player_y_pos = (int)player.TilePoint.Y;
            Actions.FireSlingshot(player_x_pos+4, player_y_pos, this);
        }

        private void detach(string command, string[] args)
        {
            Actions.detach(this);
        }

        private void attach(string command, string[] args)
        {
            try
            {
                var player = Game1.player;
                string itemIndexString = args[0];
                int itemIdex = int.Parse(itemIndexString);
                var playerInventory = Game1.player.Items;
                var item = playerInventory[itemIdex];
                if (item is StardewValley.Object obj)
                {
                    Actions.attach(obj, this);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"error {ex.Data}");
            }
        }

        private void dropIn(string command, string[] args)
        {
            var player = Game1.player;
            string itemIndexString = args[0];
            int itemIdex = int.Parse(itemIndexString);
            var playerInventory = Game1.player.Items;
            var item = playerInventory[itemIdex];

            Actions.drop_in(item, this);
        }

        private void useItem(string command, string[] args)
        {
            var player = Game1.player;
            int player_x_pos = (int)player.TilePoint.X;
            int player_y_pos = (int)player.TilePoint.Y;
            Actions.use_item(player_x_pos + 1, player_y_pos, this);
        }

        private void placeItem(string command, string[] args)
        {
            var player = Game1.player;
            int player_x_pos = (int)player.TilePoint.X;
            int player_y_pos = (int)player.TilePoint.Y;
            Actions.place_item(player_x_pos+1,player_y_pos,this);
        }

        private void testEat(string command, string[] args)
        {
            Actions.eat_food(this);
        }

        private void giveItems(string command, string[] args)
        {
            int itemId = TestUtils.ItemIdMap[args[0]];
            int count = 1;
            if (args.Length > 1)
            {
                count = int.Parse(args[1]);
            }
            TestUtils.give_items(itemId.ToString(), count);
        }

        private void giveItemsById(string command, string[] args)
        {
            string itemId = args[0];
            int count = 1;
            if (args.Length > 1)
            {
                count = int.Parse(args[1]);
            }
            TestUtils.give_items(itemId, count);
        }

        private void fish(string command, string[] args)
        {
            fish();
        }

        private void tpPlayer(string command, string[] args)
        {
            TestUtils.tp_player(args[0], this);
        }

        private void giveTool(string command, string[] args)
        {
            TestUtils.give_tool(args[0], this);
        }

        private void fish()
        {
            var player = Game1.player;
            var gameLocation = player.currentLocation;
            int player_x_pos = (int)player.TilePoint.X;
            int player_y_pos = (int)player.TilePoint.Y;
            Tool currentTool = Game1.player.CurrentTool;
            if (currentTool is FishingRod)
            {
                currentTool.DoFunction(gameLocation, player_x_pos, player_y_pos, 10, player);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

         
            if (e.Button == SButton.K)
            {
                //Game1.player.BeginUsingTool();
                var kb = Game1.GetKeyboardState();
                var mb = Game1.input.GetMouseState();
                var cb = Game1.input.GetGamePadState();
                Game1.pressActionButton(kb, mb, cb);
            }

            if (e.Button == SButton.P)
            {
                var player = Game1.player;
                int player_x_pos = (int)player.TilePoint.X;
                int player_y_pos = (int)player.TilePoint.Y;
                TestUtils.add_chest(player_x_pos, player_y_pos, Color.Blue, this);
            }

    
            if (e.Button == SButton.O)
            {
                TestUtils.print_mouse_pos(this);
            }

        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            Actions.recordDayStart();
        }

        private void StartAutoPathing(Vector2 targetTile)
        {
            this.Monitor.Log("Attempting to auto-path player.", LogLevel.Info);

            // Calculate the path
            var player = Game1.player;
            var pathFinder = new PathFindController(player, player.currentLocation, targetTile.ToPoint(), -1);

            if (pathFinder.pathToEndPoint != null && pathFinder.pathToEndPoint.Count > 0)
            {
                player.controller = pathFinder;
                this.Monitor.Log($"Auto-pathing started to {targetTile}.", LogLevel.Info);
                // Immediately start the clearing task after pathing completes
                pathFinder.endBehaviorFunction = (farmer, location) =>
                {
                    this.Monitor.Log($"Player has reached target {targetTile}. Clearing area now.", LogLevel.Info);
                    ClearArea();
                };
            }
            else
            {
                this.Monitor.Log($"No valid path to {targetTile}.", LogLevel.Warn);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            Actions.updatePixelData(this);
            if (Game1.activeClickableMenu is ShippingMenu shippingMenu)
            {
                this.Monitor.Log("Shipping menu is on");
                shippingMenu.exitThisMenu();
                this.Monitor.Log("Shipping menu exited");
            }
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            this.Monitor.Log("Menu Changed");
        }

        private void ClearArea()
        {
            int minX = 19;
            int maxX = 23;
            int minY = 36;
            int maxY = 40;

            var player = Game1.player;
            var location = player.currentLocation;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var tile = new Vector2(x, y);
                    var tileObject = location.getObjectAtTile(x, y);
                    var terrainFeature = location.terrainFeatures.ContainsKey(tile) ? location.terrainFeatures[tile] : null;

                    if (tileObject != null)
                    {
                        if (tileObject.Name.Contains("Weeds") || tileObject.Name.Contains("Twig"))
                        {
                            UseToolOnTile(player.getToolFromName("Scythe"), tile);
                        }
                        else if (tileObject.Name.Contains("Stone"))
                        {
                            UseToolOnTile(player.getToolFromName("Pickaxe") ?? new Pickaxe(), tile);
                        }
                        else if (tileObject.Name.Contains("Tree"))
                        {
                            UseToolOnTile(player.getToolFromName("Axe") ?? new Axe(), tile);
                        }
                    }
                    else if (terrainFeature is HoeDirt)
                    {
                        UseToolOnTile(player.getToolFromName("Hoe") ?? new Hoe(), tile);
                    }
                    else if (terrainFeature is Tree || terrainFeature is FruitTree)
                    {
                        UseToolOnTile(player.getToolFromName("Axe") ?? new Axe(), tile);
                    }
                }
            }
        }

        private void UseToolOnTile(Tool tool, Vector2 tile)
        {
            var player = Game1.player;

            if (tool == null)
            {
                this.Monitor.Log($"Error: Tool is null. Cannot use tool on tile {tile}.", LogLevel.Error);
                return;
            }

            player.CurrentTool = tool;
            tool.DoFunction(player.currentLocation, (int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, 0, player);
            this.Monitor.Log($"Used {tool.Name} on tile {tile}.", LogLevel.Info);
        }


        private void ExportGameData()
        {
            // Gather game data
            var gameData = GatherGameData();

            // Serialize the data to JSON
            string json = JsonConvert.SerializeObject(gameData, Formatting.Indented);

            // Write the JSON to a file
            if (outputFilePath != null)
            {
                File.WriteAllText(outputFilePath, json);
                this.Monitor.Log($"Game data exported to {outputFilePath}", LogLevel.Info);
            }

        }

        private object GatherGameData()
        {
            var playerData = GetPlayerData();
            var npcData = GetNPCData();
            var locationData = GetLocationData(Game1.getLocationFromName("Farm"));
            var gameStateData = GetGameStateData();

            return new
            {
                Player = playerData,
                NPCs = npcData,
                Location = locationData,
                GameState = gameStateData
            };
        }

        private object GetPlayerData()
        {
            return new
            {
                Name = Game1.player.Name,
                Health = Game1.player.health,
                Stamina = Game1.player.Stamina,
                Money = Game1.player.Money,
                Location = Game1.player.currentLocation.Name,
                Inventory = Game1.player.Items.Select(item => item?.Name).ToList(),
                Skills = new
                {
                    Farming = Game1.player.farmingLevel.Value,
                    Mining = Game1.player.miningLevel.Value,
                    Combat = Game1.player.combatLevel.Value,
                    Fishing = Game1.player.fishingLevel.Value,
                    Foraging = Game1.player.foragingLevel.Value
                }
            };
        }

        private List<NPCData> GetNPCData()
        {
            return Utility.getAllCharacters().Select(npc => new NPCData
            {
                Name = npc.Name,
                Location = npc.currentLocation.Name,
                Friendship = Game1.player.getFriendshipLevelForNPC(npc.Name)
            }).ToList();
        }

        private class NPCData
        {
            public string? Name { get; set; }
            public string? Location { get; set; }
            public int Friendship { get; set; }
        }

        private object GetLocationData(GameLocation location)
        {
            var tilesData = new List<object>();

            for (int x = 0; x < location.map.Layers[0].LayerWidth; x++)
            {
                for (int y = 0; y < location.map.Layers[0].LayerHeight; y++)
                {
                    var tile = new Vector2(x, y);
                    tilesData.Add(new
                    {
                        X = x,
                        Y = y,
                        IsPassable = location.isTilePassable(new Location(x, y), Game1.viewport),
                        TerrainFeature = location.terrainFeatures.ContainsKey(tile) ? location.terrainFeatures[tile].GetType().Name : null,
                        Object = location.getObjectAtTile(x, y)?.Name
                    });
                }
            }

            var buildings = location.buildings?.Select(b => new
            {
                Name = b.buildingType.Value,
                Position = b.tileX.Value,
                Owner = b.owner?.Name ?? "None"
            }).ToList();

            return new
            {
                LocationName = location.Name,
                Tiles = tilesData,
                Buildings = buildings
            };
        }

        private object GetGameStateData()
        {
            return new
            {
                DayOfMonth = Game1.dayOfMonth,
                Season = Game1.currentSeason,
                Year = Game1.year,
                TimeOfDay = Game1.timeOfDay,
                Weather = Game1.isRaining ? "Raining" : Game1.isSnowing ? "Snowing" : "Clear",
                IsWeddingDay = Game1.weddingToday
            };
        }
    }
}