using System;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Objects;
using StardewValley.Tools;
using StardewValley.Projectiles;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Network;

using xTile.Dimensions;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Microsoft.Xna.Framework.Input;
using StardewValley.Extensions;
using StardewValley.GameData.FarmAnimals;
using StardewValley.Minigames;

using StardewValley.Pathfinding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using StardewValley.TerrainFeatures;
using ActionSpace.common;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Mods;
using StardewValley.GameData.Objects;
using StardewValley.Quests;
using System.Reflection.Metadata;
using System.Reflection;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using Netcode;
using StardewValley.Characters;
using System.Net.Mail;
using StardewValley.GameData.Buildings;
using System.Security.Cryptography.X509Certificates;
using StardewValley.Enchantments;
using StardewModdingAPI.Events;
using MessagePack;
using System.Text;
using System.Threading.Tasks;
using PeterO.Cbor;

namespace ActionSpace.actions
{

    public static class ProfessionHelper
    {
        
        public static readonly Dictionary<int, string> ProfessionNames = new Dictionary<int, string>
        {
            [0] = "Rancher",
            [1] = "Tiller",
            [2] = "Fisher",
            [3] = "Trapper",
            [4] = "Forester",
            [5] = "Gatherer",
            [6] = "Miner",
            [7] = "Geologist",
            [8] = "Blacksmith",
            [9] = "Excavator",
            [10] = "Coopmaster",
            [11] = "Shepherd",
            [12] = "Angler",
            [13] = "Pirate",
            [14] = "Lumberjack",
            [15] = "Tapper",
            [16] = "Burrower",
            [17] = "Gemologist",
            [18] = "Acrobat",
            [19] = "Desperado"
           
        };
    }

    public static class Actions
    {
        static byte[]? pixelData;
        static int[] currentViewport = new int[] {Game1.viewport.X, Game1.viewport.Y };
        static int sampleRate = 100; //percentage
        static int dayStartTimes = 0;

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

        public static void recordDayStart()
        {
            dayStartTimes += 1;
        }
        public static void clearDayStartRecords()
        {
            dayStartTimes = 0;
        }

        public static void craft(string item, Mod mod)
        {
            var player = Game1.player;
            if (player.craftingRecipes.ContainsKey(item))
            {
                CraftingRecipe recipe = new CraftingRecipe(item);
                if (recipe.doesFarmerHaveIngredientsInInventory())
                {
                    recipe.consumeIngredients(null);
                    Item craftedItem = recipe.createItem();
                    Game1.player.addItemToInventoryBool(craftedItem);
                }
                mod.Monitor.Log($"created {item}");
            }
        }

        public static void turn(int direction, Mod mod)
        {
            Game1.player.FacingDirection = direction;
        }

        public static void interact(Mod mod)
        {
            var position = Game1.player.position;
            var mousePos = new Vector2(position.X, position.Y);
            var tileP = Game1.player.TilePoint;
            var direction = Game1.player.FacingDirection;
            var tileSize = Game1.tileSize;
            var xOffset = 0;
            var yOffset = 0;
            switch (direction)
            {
                case 0:
                    mousePos.Y -= tileSize * 1.5f;
                    yOffset = -1;
                    break;
                case 1:
                    mousePos.X += tileSize * 1.5f;
                    xOffset = 1;
                    break;
                case 2:
                    mousePos.Y += tileSize * 1.5f;
                    yOffset = 1;
                    break;
                case 3:
                    mousePos.X -= tileSize * 1.5f;
                    xOffset = -1;
                    break;
                default:
                    break;
            }
            Vector2 screenPosition = Game1.GlobalToLocal(new Vector2(((int)mousePos.X), ((int)mousePos.Y)));

            if (Game1.player.currentLocation.Name == "Farm")
            {
                var playerX = Game1.player.TilePoint.X;
                var playerY = Game1.player.TilePoint.Y;
                var binLocationX = Game1.getFarm().GetStarterShippingBinLocation().X;
                var binLocationY = Game1.getFarm().GetStarterShippingBinLocation().Y;
                var difX = Math.Abs(playerX - binLocationX);
                var difY = Math.Abs(playerY - binLocationY);
                if ((difX == 1 && difY == 0) || (difY == 1 && difX == 0) || (difY == 2 && difX == 0) || (difX == 1) && (difY == 1))
                {
                    var item = Game1.player.CurrentItem;
                    if (item.canBeShipped())
                    {
                        Game1.getFarm().getShippingBin(Game1.player).Add(item);
                        Game1.player.removeItemFromInventory(item);
                        return;
                    }
                }

                if (Game1.player.ActiveObject != null && Game1.player.ActiveObject.Category == StardewValley.Object.SeedsCategory)
                {
                    Vector2 facingTile = new Vector2(playerX + xOffset, playerY + yOffset);
                    var location = Game1.currentLocation;

                    
                    if (location.isTileHoeDirt(facingTile) && (location.terrainFeatures.TryGetValue(facingTile, out TerrainFeature feature) && feature is HoeDirt dirt))
                    {
                        var seedId = Game1.player.ActiveObject.ParentSheetIndex.ToString();
                        if (dirt.crop == null && dirt.canPlantThisSeedHere(seedId))
                        {
                            var planted = dirt.plant(seedId, Game1.player, false);
                            if (planted)
                            {
                                Game1.player.reduceActiveItemByOne();
                                return;
                            }
                        }
                    }
                }
            }



            mod.Monitor.Log($"rightClick: x={screenPosition.X}, y={screenPosition.Y}");
            rightClick((screenPosition.X).ToString(), (screenPosition.Y).ToString(), mod);
        }

        public static void rightClick(string x, string y, Mod mod)
        {
            int xI = int.Parse(x);
            int yI = int.Parse(y);
            var _currentMouseStateField = mod.Helper.Reflection.GetField<MouseState>(Game1.input, "_currentMouseState");
            var _currentState = _currentMouseStateField.GetValue();
            var newMouseState = new MouseState(
                xI,
                yI,
                _currentState.ScrollWheelValue,
                _currentState.LeftButton,
                _currentState.MiddleButton,
                _currentState.RightButton,
                _currentState.XButton1,
                _currentState.XButton2
                );
            
            _currentMouseStateField.SetValue(newMouseState);

            var player = Game1.player;
            var currentP = Game1.player.TilePoint;
            int directionInt = player.facingDirection.Value;

            switch (directionInt)
            {
                case 0:
                    currentP.Y -= 1;
                    break;
                case 1:
                    currentP.X += 1;
                    break;
                case 2:
                    currentP.Y += 1;
                    break;
                case 3:
                    currentP.X -= 1;
                    break;
                default:
                    break;
            }
            var v2Pos = new Vector2(currentP.X, currentP.Y);

            if (Game1.currentLocation.furniture is not null)
            {
                var furnitureList = Game1.currentLocation.furniture.ToList();
                foreach (var furnitureItem in furnitureList)
                {
                    if (furnitureItem.GetBoundingBox().Contains(new Point(currentP.X * Game1.tileSize, currentP.Y * Game1.tileSize)))
                    {
                        var checkRes = furnitureItem.checkForAction(Game1.player);
                        if (checkRes)
                        {
                            return;
                        }
                    }
                }
            }

            if (Game1.currentLocation.isTileHoeDirt(v2Pos))
            {
                Game1.currentLocation.GetHoeDirtAtTile(v2Pos).performUseAction(v2Pos);
            }
            else if (Game1.currentLocation.objects.Keys.Contains(v2Pos) && Game1.currentLocation.objects[v2Pos] is Chest chest)
            {
                InteractWithChest(chest, mod);
            }
            else
            {
                //Game1.tryToCheckAt(v2Pos, Game1.player);
                pressActionButtonMirror(Game1.input.GetKeyboardState(), newMouseState, Game1.input.GetGamePadState(), mod);
            }
        }

        public static bool pressActionButtonMirror(KeyboardState currentKBState, MouseState currentMouseState, GamePadState currentPadState, Mod mod)
        {
            var player = Game1.player;
            var eventUp = Game1.eventUp;
            var currentLocation = Game1.currentLocation;

            if (Game1.IsChatting)
            {
                currentKBState = default(KeyboardState);
            }
            if (Game1.dialogueTyping)
            {
                bool flag = true;
                Game1.dialogueTyping = false;
                if (Game1.currentSpeaker != null)
                {
                    Game1.currentDialogueCharacterIndex = Game1.currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue().Length;
                }
                else if (Game1.currentObjectDialogue.Count > 0)
                {
                    Game1.currentDialogueCharacterIndex = Game1.currentObjectDialogue.Peek().Length;
                }
                else
                {
                    flag = false;
                }
                Game1.dialogueTypingInterval = 0;
                Game1.oldKBState = currentKBState;
                Game1.oldMouseState = Game1.input.GetMouseState();
                Game1.oldPadState = currentPadState;
                if (flag)
                {
                    Game1.playSound("dialogueCharacterClose");
                    return false;
                }
            }
            if (Game1.dialogueUp)
            {
                if (Game1.isQuestion)
                {
                    Game1.isQuestion = false;
                    if (Game1.currentSpeaker != null)
                    {
                        if (Game1.currentSpeaker.CurrentDialogue.Peek().chooseResponse(Game1.questionChoices[Game1.currentQuestionChoice]))
                        {
                            Game1.currentDialogueCharacterIndex = 1;
                            Game1.dialogueTyping = true;
                            Game1.oldKBState = currentKBState;
                            Game1.oldMouseState = Game1.input.GetMouseState();
                            Game1.oldPadState = currentPadState;
                            return false;
                        }
                    }
                    else
                    {
                        Game1.dialogueUp = false;
                        if (Game1.eventUp && Game1.currentLocation.afterQuestion == null)
                        {
                            Game1.currentLocation.currentEvent.answerDialogue(Game1.currentLocation.lastQuestionKey, Game1.currentQuestionChoice);
                            Game1.currentQuestionChoice = 0;
                            Game1.oldKBState = currentKBState;
                            Game1.oldMouseState = Game1.input.GetMouseState();
                            Game1.oldPadState = currentPadState;
                        }
                        else if (Game1.currentLocation.answerDialogue(Game1.questionChoices[Game1.currentQuestionChoice]))
                        {
                            Game1.currentQuestionChoice = 0;
                            Game1.oldKBState = currentKBState;
                            Game1.oldMouseState = Game1.input.GetMouseState();
                            Game1.oldPadState = currentPadState;
                            return false;
                        }
                        if (Game1.dialogueUp)
                        {
                            Game1.currentDialogueCharacterIndex = 1;
                            Game1.dialogueTyping = true;
                            Game1.oldKBState = currentKBState;
                            Game1.oldMouseState = Game1.input.GetMouseState();
                            Game1.oldPadState = currentPadState;
                            return false;
                        }
                    }
                    Game1.currentQuestionChoice = 0;
                }
                string? text = null;
                if (Game1.currentSpeaker != null)
                {
                    if (Game1.currentSpeaker.immediateSpeak)
                    {
                        Game1.currentSpeaker.immediateSpeak = false;
                        return false;
                    }
                    text = ((Game1.currentSpeaker.CurrentDialogue.Count > 0) ? Game1.currentSpeaker.CurrentDialogue.Peek().exitCurrentDialogue() : null);
                }
                if (text == null)
                {
                    if (Game1.currentSpeaker != null && Game1.currentSpeaker.CurrentDialogue.Count > 0 && Game1.currentSpeaker.CurrentDialogue.Peek().isOnFinalDialogue() && Game1.currentSpeaker.CurrentDialogue.Count > 0)
                    {
                        Game1.currentSpeaker.CurrentDialogue.Pop();
                    }
                    Game1.dialogueUp = false;
                    if (Game1.messagePause)
                    {
                        Game1.pauseTime = 500f;
                    }
                    if (Game1.currentObjectDialogue.Count > 0)
                    {
                        Game1.currentObjectDialogue.Dequeue();
                    }
                    Game1.currentDialogueCharacterIndex = 0;
                    if (Game1.currentObjectDialogue.Count > 0)
                    {
                        Game1.dialogueUp = true;
                        Game1.questionChoices.Clear();
                        Game1.oldKBState = currentKBState;
                        Game1.oldMouseState = Game1.input.GetMouseState();
                        Game1.oldPadState = currentPadState;
                        Game1.dialogueTyping = true;
                        return false;
                    }
                    var currentSpeaker = Game1.currentSpeaker;
                    if (currentSpeaker != null && !currentSpeaker.Name.Equals("Gunther") && !eventUp && !currentSpeaker.doingEndOfRouteAnimation.Value)
                    {
                        currentSpeaker.doneFacingPlayer(player);
                    }
                    currentSpeaker = null;
                    if (!eventUp)
                    {
                        player.CanMove = true;
                    }
                    else if (currentLocation.currentEvent.CurrentCommand > 0 || currentLocation.currentEvent.specialEventVariable1)
                    {
                        if (!Game1.isFestival() || !currentLocation.currentEvent.canMoveAfterDialogue())
                        {
                            currentLocation.currentEvent.CurrentCommand++;
                        }
                        else
                        {
                            player.CanMove = true;
                        }
                    }
                    Game1.questionChoices.Clear();
                    Game1.playSound("smallSelect");
                }
                else
                {
                    Game1.playSound("smallSelect");
                    Game1.currentDialogueCharacterIndex = 0;
                    Game1.dialogueTyping = true;
                    MethodInfo checkIfDialogueIsQuestionMethod = mod.Helper.Reflection.GetMethod(
                        type: typeof(Game1),
                        name: "checkIfDialogueIsQuestion"
                    ).MethodInfo;
                    if (checkIfDialogueIsQuestionMethod != null)
                    {
                        checkIfDialogueIsQuestionMethod.Invoke(null, new object[] { });
                    }
                }
                Game1.oldKBState = currentKBState;
                Game1.oldMouseState = Game1.input.GetMouseState();
                Game1.oldPadState = currentPadState;
                return false;
            }
            if (!player.UsingTool && (!eventUp || (currentLocation.currentEvent != null && currentLocation.currentEvent.playerControlSequence)) && !Game1.fadeToBlack)
            {
                if (Game1.wasMouseVisibleThisFrame && currentLocation.animals.Length > 0)
                {
                    Vector2 position = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y);
                    if (Utility.withinRadiusOfPlayer((int)position.X, (int)position.Y, 1, player))
                    {
                        if (currentLocation.CheckPetAnimal(position, player))
                        {
                            return true;
                        }
                        if (currentLocation.CheckInspectAnimal(position, player))
                        {
                            return true;
                        }
                    }
                }
                Vector2 vector = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / 64f;
                Vector2 vector2 = vector;
                bool flag2 = false;
                if (!Game1.wasMouseVisibleThisFrame || Game1.mouseCursorTransparency == 0f || !Utility.tileWithinRadiusOfPlayer((int)vector.X, (int)vector.Y, 1, player))
                {
                    vector = player.GetGrabTile();
                    flag2 = true;
                }
                bool flag3 = false;
                if (eventUp && !Game1.isFestival())
                {
                    Game1.CurrentEvent?.receiveActionPress((int)vector.X, (int)vector.Y);
                    Game1.oldKBState = currentKBState;
                    Game1.oldMouseState = Game1.input.GetMouseState();
                    Game1.oldPadState = currentPadState;
                    return false;
                }
                if (Game1.tryToCheckAt(vector, player))
                {
                    return false;
                }
                if (player.isRidingHorse())
                {
                    player.mount.checkAction(player, player.currentLocation);
                    return false;
                }
                if (!player.canMove)
                {
                    return false;
                }
                if (!flag3 && player.currentLocation.isCharacterAtTile(vector) != null)
                {
                    flag3 = true;
                }
                bool flag4 = false;
                if (player.ActiveObject != null && !(player.ActiveObject is Furniture))
                {
                    if (player.ActiveObject.performUseAction(currentLocation))
                    {
                        player.reduceActiveItemByOne();
                        Game1.oldKBState = currentKBState;
                        Game1.oldMouseState = Game1.input.GetMouseState();
                        Game1.oldPadState = currentPadState;
                        return false;
                    }
                    int stack = player.ActiveObject.Stack;
                    Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
                    if (flag2)
                    {
                        Game1.isCheckingNonMousePlacement = true;
                    }
                    if (Game1.isOneOfTheseKeysDown(currentKBState, Game1.options.actionButton))
                    {
                        Game1.isCheckingNonMousePlacement = true;
                    }
                    Vector2 nearbyValidPlacementPosition = Utility.GetNearbyValidPlacementPosition(player, currentLocation, player.ActiveObject, (int)vector.X * 64 + 32, (int)vector.Y * 64 + 32);
                    if (!Game1.isCheckingNonMousePlacement && player.ActiveObject is Wallpaper && Utility.tryToPlaceItem(currentLocation, player.ActiveObject, (int)vector2.X * 64, (int)vector2.Y * 64))
                    {
                        Game1.isCheckingNonMousePlacement = false;
                        return true;
                    }
                    if (Utility.tryToPlaceItem(currentLocation, player.ActiveObject, (int)nearbyValidPlacementPosition.X, (int)nearbyValidPlacementPosition.Y))
                    {
                        Game1.isCheckingNonMousePlacement = false;
                        return true;
                    }
                    if (!eventUp && (player.ActiveObject == null || player.ActiveObject.Stack < stack || player.ActiveObject.isPlaceable()))
                    {
                        flag4 = true;
                    }
                    Game1.isCheckingNonMousePlacement = false;
                }
                if (!flag4 && !flag3)
                {
                    vector.Y += 1f;
                    if (player.FacingDirection >= 0 && player.FacingDirection <= 3)
                    {
                        Vector2 value = vector - player.Tile;
                        if (value.X > 0f || value.Y > 0f)
                        {
                            value.Normalize();
                        }
                        if (Vector2.Dot(Utility.DirectionsTileVectors[player.FacingDirection], value) >= 0f && Game1.tryToCheckAt(vector, player))
                        {
                            return false;
                        }
                    }
                    if (!eventUp && player.ActiveObject is Furniture furniture)
                    {
                        furniture.rotate();
                        Game1.playSound("dwoop");
                        Game1.oldKBState = currentKBState;
                        Game1.oldMouseState = Game1.input.GetMouseState();
                        Game1.oldPadState = currentPadState;
                        return false;
                    }
                    vector.Y -= 2f;
                    if (player.FacingDirection >= 0 && player.FacingDirection <= 3 && !flag3)
                    {
                        Vector2 value2 = vector - player.Tile;
                        if (value2.X > 0f || value2.Y > 0f)
                        {
                            value2.Normalize();
                        }
                        if (Vector2.Dot(Utility.DirectionsTileVectors[player.FacingDirection], value2) >= 0f && Game1.tryToCheckAt(vector, player))
                        {
                            return false;
                        }
                    }
                    if (!eventUp && player.ActiveObject is Furniture furniture2)
                    {
                        furniture2.rotate();
                        Game1.playSound("dwoop");
                        Game1.oldKBState = currentKBState;
                        Game1.oldMouseState = Game1.input.GetMouseState();
                        Game1.oldPadState = currentPadState;
                        return false;
                    }
                    vector = player.Tile;
                    if (Game1.tryToCheckAt(vector, player))
                    {
                        return false;
                    }
                    if (!eventUp && player.ActiveObject is Furniture furniture3)
                    {
                        furniture3.rotate();
                        Game1.playSound("dwoop");
                        Game1.oldKBState = currentKBState;
                        Game1.oldMouseState = Game1.input.GetMouseState();
                        Game1.oldPadState = currentPadState;
                        return false;
                    }
                }
                if (!player.isEating && player.ActiveObject != null && !Game1.dialogueUp && !eventUp && !player.canOnlyWalk && !player.FarmerSprite.PauseForSingleAnimation && !Game1.fadeToBlack && player.ActiveObject.Edibility != -300)
                {
                    if (player.team.SpecialOrderRuleActive("SC_NO_FOOD"))
                    {
                        MineShaft? obj = player.currentLocation as MineShaft;
                        if (obj != null && obj.getMineArea() == 121)
                        {
                            Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"), 3));
                            return false;
                        }
                    }
                    if (player.hasBuff("25") && player.ActiveObject != null && !player.ActiveObject.HasContextTag("ginger_item"))
                    {
                        Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Nauseous_CantEat"), 3));
                        return false;
                    }
                    player.faceDirection(2);
                    player.itemToEat = player.ActiveObject;
                    player.FarmerSprite.setCurrentSingleAnimation(304);
                    if (Game1.objectData.TryGetValue(player.ActiveObject?.ItemId, out var value3))
                    {
                        currentLocation.createQuestionDialogue((value3.IsDrink && player.ActiveObject?.preserve.Value != StardewValley.Object.PreserveType.Pickle) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3159", player.ActiveObject?.DisplayName) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3160", player.ActiveObject?.DisplayName), currentLocation.createYesNoResponses(), "Eat");
                    }
                    Game1.oldKBState = currentKBState;
                    Game1.oldMouseState = Game1.input.GetMouseState();
                    Game1.oldPadState = currentPadState;
                    return false;
                }
            }
            if (player.CurrentTool is MeleeWeapon && player.CanMove && !player.canOnlyWalk && !eventUp && !player.onBridge.Value)
            {
                ((MeleeWeapon)player.CurrentTool).animateSpecialMove(player);
                return false;
            }
            return true;
        }

        public static void leftClickF(Mod mod)
        {
            var position = Game1.player.position;
            var tileP = Game1.player.TilePoint;
            var direction = Game1.player.FacingDirection;
            var tileSize = Game1.tileSize;
            switch (direction)
            {
                case 0:
                    position.Y -= tileSize * 1.5f;
                    break;
                case 1:
                    position.X += tileSize * 1.5f;
                    break;
                case 2:
                    position.Y += tileSize * 1.5f;
                    break;
                case 3:
                    position.X -= tileSize * 1.5f;
                    break;
                default:
                    break;
            }
            Vector2 screenPosition = Game1.GlobalToLocal(new Vector2(((int)position.X), ((int)position.Y)));
            mod.Monitor.Log($"leftClick: x={screenPosition.X}, y={screenPosition.Y}");
            leftClick((screenPosition.X).ToString(), (screenPosition.Y).ToString(), mod);
        }

        public static void useWithAnim(Mod mod)
        {
            if (Game1.player.CurrentItem is MeleeWeapon meleeWeapon)
            {
                useMeleeWeapon(mod);
            }
            else if (Game1.player.ActiveObject != null)
            {
                if (Game1.player.ActiveObject.Edibility >= 0)
                {
                    interact(mod);
                }
                else if (Game1.player.ActiveObject.isPlaceable())
                {
                    interact(mod);
                }
                else
                {
                    useActiveObject(mod);
                }
            }
            else
            {
                Game1.pressUseToolButton();
            }
        }

        public static void useActiveObject(Mod mod)
        {
            var player = Game1.player;
            var options = Game1.options;
            var currentLocation = Game1.currentLocation;
            Vector2 position = player.GetToolLocation();
            var game1 = Game1.game1;
            var didInitiateItemStow = mod.Helper.Reflection.GetField<bool>(game1, "_didInitiateItemStow", true).GetValue();
            var hooks = mod.Helper.Reflection.GetField<ModHooks>(typeof(Game1), "hooks", true).GetValue();
            if (player.ActiveObject != null)
            {
                if (options.allowStowing && Game1.CanPlayerStowItem(Game1.GetPlacementGrabTile()))
                {
                    if (Game1.didPlayerJustLeftClick() || didInitiateItemStow)
                    {
                        mod.Helper.Reflection.GetField<bool>(typeof(Game1), "_didInitiateItemStow", true).SetValue(true);
                        Game1.playSound("stoneStep");
                        player.netItemStowed.Set(newValue: true);
                        return;
                    }
                    return;
                }
                bool checkAction = hooks.OnGameLocation_CheckAction(currentLocation, new Location((int)position.X / 64, (int)position.Y / 64), Game1.viewport, player, () => currentLocation.checkAction(new Location((int)position.X / 64, (int)position.Y / 64), Game1.viewport, player));
                if (Utility.withinRadiusOfPlayer((int)position.X, (int)position.Y, 1, player) && checkAction)
                {
                    return;
                }
                Vector2 placementGrabTile = player.GetGrabTile();
                Vector2 nearbyValidPlacementPosition = Utility.GetNearbyValidPlacementPosition(player, currentLocation, player.ActiveObject, (int)placementGrabTile.X * 64, (int)placementGrabTile.Y * 64);
                if (Utility.tryToPlaceItem(currentLocation, player.ActiveObject, (int)nearbyValidPlacementPosition.X, (int)nearbyValidPlacementPosition.Y))
                {
                    Game1.isCheckingNonMousePlacement = false;
                    return;
                }
                Game1.isCheckingNonMousePlacement = false;
                return;
            }
        }

        public static void useMeleeWeapon(Mod mod)
        {
            var player = Game1.player;
            Vector2 position = player.GetToolLocation();
            int facingDirection = player.FacingDirection;
            Vector2 toolLocation = player.GetToolLocation(position);
            player.lastClick = new Vector2((int)position.X, (int)position.Y);
            player.BeginUsingTool();
            if (!player.usingTool.Value)
            {
                player.FacingDirection = facingDirection;
            }
            else if (player.FarmerSprite.IsPlayingBasicAnimation(facingDirection, carrying: true) || player.FarmerSprite.IsPlayingBasicAnimation(facingDirection, carrying: false))
            {
                player.FarmerSprite.StopAnimation();
            }
        }

        public static void use(Mod mod)
        {
            var player = Game1.player;
            var currentP = Game1.player.TilePoint;
            int directionInt = player.facingDirection.Value;

            switch (directionInt)
            {
                case 0:
                    currentP.Y -= 1;
                    break;
                case 1:
                    currentP.X += 1;
                    break;
                case 2:
                    currentP.Y += 1;
                    break;
                case 3:
                    currentP.X -= 1;
                    break;
                default:
                    break;
            }
            if (player.CurrentTool != null || player.CurrentItem != null)
            {
                use_item(currentP.X, currentP.Y, mod, power: 1);
            }

        }

        public static void leftClick(string x, string y, Mod mod)
        {
            int xI = int.Parse(x);
            int yI = int.Parse(y);
            //Game1.oldMouseState = new MouseState(
            //    xI,
            //    yI,
            //    Game1.oldMouseState.ScrollWheelValue,
            //    ButtonState.Pressed,
            //    Game1.oldMouseState.MiddleButton,
            //    Game1.oldMouseState.RightButton,
            //    Game1.oldMouseState.XButton1,
            //    Game1.oldMouseState.XButton2
            //);
            Game1.pressUseToolButton();

            //Task.Run(async () =>
            //{
            //    await Task.Delay(1000);
            //    EventHandler<StardewModdingAPI.Events.UpdateTickedEventArgs>? releaseListener = null;
            //    releaseListener = (sender, e) =>
            //    {
            //        Game1.oldMouseState = new MouseState(
            //            xI,
            //            yI,
            //            Game1.oldMouseState.ScrollWheelValue,
            //            ButtonState.Released,
            //            Game1.oldMouseState.MiddleButton,
            //            Game1.oldMouseState.RightButton,
            //            Game1.oldMouseState.XButton1,
            //            Game1.oldMouseState.XButton2
            //        );
            //        mod.Helper.Events.GameLoop.UpdateTicked -= releaseListener;
            //    };
            //    mod.Helper.Events.GameLoop.UpdateTicked += releaseListener; 
            //});

        }


        public static async Task<bool> move(string direction, Mod mod)
        {
            if (!Game1.player.canMove)
            {
                return false;
            }

            var currentP = Game1.player.TilePoint;
            int directionInt = int.Parse(direction);

            switch (directionInt)
            {
                case 0:
                    break;
                case 1:
                    currentP.Y -= 1;
                    break;
                case 2:
                    currentP.X += 1;
                    break;
                case 3:
                    currentP.Y += 1;
                    break;
                case 4:
                    currentP.X -= 1;
                    break;
                default:
                    break;
            }
            if (Game1.player.currentLocation.isCollidingPosition(Game1.player.GetBoundingBox(), Game1.viewport, Game1.player)
                || Game1.player.currentLocation.isFarmerCollidingWithAnyCharacter()
                || !Game1.player.currentLocation.isTilePassable(new Vector2(currentP.X, currentP.Y)))
            {
                return false;
            }
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Action<bool> onComplete = (success) => taskCompletionSource.TrySetResult(success);
            StartAutoPathing(new Vector2(currentP.X, currentP.Y), onComplete, mod);
            //Game1.player.setTileLocation(new Vector2(currentP.X, currentP.Y));
            await taskCompletionSource.Task;
            return true;
        }

        // place an item onto another
        public static void drop_in(Item loadItem, Mod mod)
        {
            var player = Game1.player;
            var item = player.CurrentItem;
            if (item is StardewValley.Object obj)
            {
                var success = obj.performObjectDropInAction(loadItem, probe: false, player);
                mod.Monitor.Log($"{loadItem.Name} load to {item.Name}: {success}");
            }
            else
            {
                mod.Monitor.Log($"item is not Object");
            }
        }

        // attach item to current tool
        public static void attach(StardewValley.Object attachItem, Mod mod)
        {
            var player = Game1.player;
            var item = player.CurrentItem;
            if (item is StardewValley.Tool tool)
            {
                tool.attach(attachItem);
                mod.Monitor.Log($"{attachItem.Name} attached to {item.Name}");
                player.removeItemFromInventory(attachItem);
            }
            else
            {
                mod.Monitor.Log($"item is not Tool");
            }
        }

        // detach item
        public static void detach(Mod mod)
        {
            var player = Game1.player;
            var item = player.CurrentItem;
            if (item is StardewValley.Tool tool)
            {
                var attachItem = tool.attachments[0];
                tool.attach(null);
                mod.Monitor.Log($"{item.Name} detached");
                player.addItemToInventory(attachItem);
            }
            else
            {
                mod.Monitor.Log($"item is not Tool");
            }
        }

        public static void eat_food(Mod mod)
        {
            var player = Game1.player;
            Item currentItem = Game1.player.CurrentItem;
            if (currentItem is StardewValley.Object currentObject && ((StardewValley.Object)currentItem).Edibility > 0)
            {
                player.eatObject(currentObject);
                player.removeItemFromInventory(currentItem);
            }
        }

        public static void place_item(int posX, int posY, Mod mod)
        {
            var player = Game1.player;
            Item currentItem = player.CurrentItem;
            var gameLocation = player.currentLocation;
            Microsoft.Xna.Framework.Vector2 targetTile = new Microsoft.Xna.Framework.Vector2(posX, posY);
            if (currentItem is StardewValley.Object obj && obj.canBePlacedHere(player.currentLocation, targetTile))
            {
                if (!gameLocation.Objects.ContainsKey(targetTile))
                {

                    mod.Monitor.Log($"Object is Chest: {obj is Chest}! {obj.GetType()}", LogLevel.Debug);

                    if (obj is Chest chest)
                    {
                        chest.placementAction(gameLocation, posX, posY, player);
                        mod.Monitor.Log($"Chest placed {obj.DisplayName} at {targetTile}!", LogLevel.Debug);

                    }
                    else
                    {
                        obj.placementAction(gameLocation, posX, posY, player);

                        gameLocation.Objects.Add(targetTile, obj);
                    }

                    player.removeItemFromInventory(obj);

                    mod.Monitor.Log($"Player placed {obj.DisplayName} at {targetTile}!", LogLevel.Debug);
                }
                else
                {
                    mod.Monitor.Log("The tile is already occupied!", LogLevel.Debug);
                }
            }
            else
            {
                mod.Monitor.Log("The current item cannot be placed!", LogLevel.Debug);
            }

        }

        public static void FireSlingshot(int posX, int posY, Mod mod)
        {
            var who = Game1.player;
            var location = Game1.currentLocation;
            var currentItem = who.CurrentItem;
            if (currentItem is Slingshot slingshot)
            {
                if (slingshot.attachments[0] != null)
                {
                    int mouseX = posX;
                    int mouseY = posY;
                    Microsoft.Xna.Framework.Vector2 shoot_origin = slingshot.GetShootOrigin(who);
                    Microsoft.Xna.Framework.Vector2 v = Utility.getVelocityTowardPoint(slingshot.GetShootOrigin(who), slingshot.AdjustForHeight(new Vector2(mouseX, mouseY)), (float)(15 + Game1.random.Next(4, 6)) * (1f));
                    slingshot.canPlaySound = false;
                    if (!slingshot.canPlaySound)
                    {
                        StardewValley.Object ammunition = (StardewValley.Object)slingshot.attachments[0].getOne();
                        slingshot.attachments[0].Stack--;
                        if (slingshot.attachments[0].Stack <= 0)
                        {
                            slingshot.attachments[0] = null;
                        }
                        int damage = 1;
                        BasicProjectile.onCollisionBehavior? collisionBehavior = null;
                        string collisionSound = "hammer";
                        float damageMod = 1f;
                        if (slingshot.InitialParentTileIndex == 33)
                        {
                            damageMod = 2f;
                        }
                        else if (slingshot.InitialParentTileIndex == 34)
                        {
                            damageMod = 4f;
                        }
                        switch (ammunition.ParentSheetIndex)
                        {
                            case 388:
                                damage = 2;
                                ammunition.ParentSheetIndex++;
                                break;
                            case 390:
                                damage = 5;
                                ammunition.ParentSheetIndex++;
                                break;
                            case 378:
                                damage = 10;
                                ammunition.ParentSheetIndex++;
                                break;
                            case 380:
                                damage = 20;
                                ammunition.ParentSheetIndex++;
                                break;
                            case 384:
                                damage = 30;
                                ammunition.ParentSheetIndex++;
                                break;
                            case 382:
                                damage = 15;
                                ammunition.ParentSheetIndex++;
                                break;
                            case 386:
                                damage = 50;
                                ammunition.ParentSheetIndex++;
                                break;
                            case 441:
                                damage = 20;
                                collisionBehavior = BasicProjectile.explodeOnImpact;
                                collisionSound = "explosion";
                                break;
                        }
                        int category = ammunition.Category;
                        if (category == -5)
                        {
                            collisionSound = "slimedead";
                        }
                        if (!Game1.options.useLegacySlingshotFiring)
                        {
                            v.X *= -1f;
                            v.Y *= -1f;
                        }
                        location.projectiles.Add(new BasicProjectile((int)(damageMod * (float)(damage + Game1.random.Next(-(damage / 2), damage + 2)) * (1f)), ammunition.ParentSheetIndex, 0, 0, (float)(Math.PI / (double)(64f + (float)Game1.random.Next(-63, 64))), 0f - v.X, 0f - v.Y, shoot_origin - new Vector2(32f, 32f), collisionSound, "", "", explode: false, damagesMonsters: true, location, who, collisionBehavior, ammunition.ParentSheetIndex.ToString())
                        {
                            IgnoreLocationCollision = (Game1.currentLocation.currentEvent != null || Game1.currentMinigame != null)
                        }); ;
                    }
                }
                else
                {
                    Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14254"));
                }
                slingshot.canPlaySound = true;
            }
            
        }

        public static void open_shop(string shopId, string ownerName, Mod mod)
        {
            Utility.TryOpenShopMenu(shopId, ownerName);
            mod.Monitor.Log($"{shopId} is opened");
        }

        public static void close_shop(string shopId, string ownerName, Mod mod)
        {
            Game1.activeClickableMenu.exitThisMenu();
            mod.Monitor.Log("menu closed");
        }

        public static void buy_from_animals_shop(int index, int building_index, string animal_name, Mod mod)
        {
            var menu = Game1.activeClickableMenu;
            if (menu is PurchaseAnimalsMenu animalsMenu)
            {
                ClickableTextureComponent item = animalsMenu.animalsToPurchase[index];
                if (animalsMenu.readOnly || (item.item as StardewValley.Object)?.Type != null)
                {
                    return;
                }
                int num = item.item.salePrice();
                if (Game1.player.Money >= num)
                {
                    animalsMenu.clickedAnimalButton = item.myID;
                    Game1.playSound("smallSelect");
                    string text = item.hoverText;
                    if (Game1.farmAnimalData.TryGetValue(text, out var value) && value.AlternatePurchaseTypes != null)
                    {
                        foreach (AlternatePurchaseAnimals alternatePurchaseType in value.AlternatePurchaseTypes)
                        {
                            if (GameStateQuery.CheckConditions(alternatePurchaseType.Condition))
                            {
                                text = Game1.random.ChooseFrom(alternatePurchaseType.AnimalIds);
                                break;
                            }
                        }
                    }
                    var multiplayer = mod.Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer", true).GetValue();
                    animalsMenu.animalBeingPurchased = new FarmAnimal(text, multiplayer.getNewID(), Game1.player.UniqueMultiplayerID);
                    animalsMenu.priceOfAnimal = num;

                    var buildingAt = Game1.getFarm().buildings.ToList()[building_index];
                    if (buildingAt?.GetIndoors() is AnimalHouse animalHouse && !buildingAt.isUnderConstruction())
                    {
                        if (animalsMenu.animalBeingPurchased.CanLiveIn(buildingAt))
                        {
                            if (animalHouse.isFull())
                            {
                                Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11321"));
                            }
                            else
                            {
                                animalsMenu.newAnimalHome = buildingAt;

                                
                                FarmAnimalData animalData = animalsMenu.animalBeingPurchased.GetAnimalData();
                                if (animalData != null)
                                {
                                    if (animalData.BabySound != null)
                                    {
                                        Game1.playSound(animalData.BabySound, 1200 + Game1.random.Next(-200, 201));
                                    }
                                    else if (animalData.Sound != null)
                                    {
                                        Game1.playSound(animalData.Sound, 1200 + Game1.random.Next(-200, 201));
                                    }
                                }



                                animalsMenu.animalBeingPurchased.Name = animal_name;
                                animalsMenu.animalBeingPurchased.displayName = animal_name;

                                if (Utility.areThereAnyOtherAnimalsWithThisName(animal_name))
                                {
                                    Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11308"));
                                    return;
                                }
                                animalsMenu.animalBeingPurchased.Name = animal_name;
                                animalsMenu.animalBeingPurchased.displayName = animal_name;
                                ((AnimalHouse)animalsMenu.newAnimalHome.GetIndoors()).adoptAnimal(animalsMenu.animalBeingPurchased);
                                animalsMenu.newAnimalHome = null;
                                Game1.player.Money -= animalsMenu.priceOfAnimal;
                            }
                        }
                        else
                        {
                            animalsMenu.setUpForReturnAfterPurchasingAnimal();
                            Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11326", animalsMenu.animalBeingPurchased.displayType));
                        }
                    }
                }
                else
                {
                    Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11325"), 3));
                }
            }
            
        }

        public static void buy_from_shop(int index, int count, Mod mod)
        {
            count = count < 0 ? 1 : count;
            var menu = Game1.activeClickableMenu;
            var player = Game1.player;
            if (menu is ShopMenu shopMenu)
            {
                if (shopMenu.forSale[index] != null)
                {
                    int val = (Math.Min(Math.Min(count, ShopMenu.getPlayerCurrencyAmount(Game1.player, shopMenu.currency) / Math.Max(1, shopMenu.itemPriceAndStock[shopMenu.forSale[index]].Price)), Math.Max(1, shopMenu.itemPriceAndStock[shopMenu.forSale[index]].Stock)));
                    if (count > val)
                    {
                        Game1.playSound("cancel");
                        return;
                    }
                    if (shopMenu.ShopId == "ReturnedDonations")
                    {
                        val = shopMenu.itemPriceAndStock[shopMenu.forSale[index]].Stock;
                    }
                    val = Math.Min(val, shopMenu.forSale[index].maximumStackSize());
                    if (val == -1)
                    {
                        val = 1;
                    }
                    if (shopMenu.canPurchaseCheck != null && !shopMenu.canPurchaseCheck(index))
                    {
                        return;
                    }
                    if (val > 0 && testUtils.TestUtils.callTryToPurchaseItem(shopMenu, shopMenu.forSale[index], null, count))
                    {
                        shopMenu.itemPriceAndStock.Remove(shopMenu.forSale[index]);
                        shopMenu.forSale.RemoveAt(index);
                    }
                    else if (val <= 0)
                    {
                        if (shopMenu.itemPriceAndStock[shopMenu.forSale[index]].Price > 0)
                        {
                            Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
                        }
                        Game1.playSound("cancel");
                    }
                    bool isStorageShop = mod.Helper.Reflection.GetField<bool>(shopMenu, "_isStorageShop").GetValue();
                    if (shopMenu.heldItem != null && player.addItemToInventoryBool(shopMenu.heldItem as Item))
                    {
                        shopMenu.heldItem = null;
                        DelayedAction.playSoundAfterDelay("coin", 100);
                    }
                }
                shopMenu.updateSaleButtonNeighbors();
                return;
            }
            
                
        
        }

        [Obsolete("Aborted, please use buy_from_shop(int index, int count,Mod mod) instead")]
        public static void buy_from_shop2(int index, int count,Mod mod)
        {

            var menu = Game1.activeClickableMenu;
            var player = Game1.player;
            if (menu is ShopMenu shopMenu){ 
                var item = shopMenu.forSale[index];
                if (shopMenu.canPurchaseCheck != null && !shopMenu.canPurchaseCheck(index))
                {
                    mod.Monitor.Log("cannot purchase");
                    return;
                }
                testUtils.TestUtils.callTryToPurchaseItem(shopMenu, item, null, count);
                if (shopMenu.heldItem != null)
                {
                    var salable = shopMenu.forSale[index];
                    var value = shopMenu.itemPriceAndStock[salable];
                    value.Stock -= count;
                    shopMenu.itemPriceAndStock[salable] = value;
                    player.addItemToInventory(shopMenu.heldItem as Item);
                    shopMenu.heldItem = null;
                }
            }
           
        }

        public static void sell_to_shop(Mod mod)
        {

            var menu = Game1.activeClickableMenu;
            var player = Game1.player;
            if (menu is ShopMenu shopMenu)
            {
                if (shopMenu.heldItem == null && !shopMenu.readOnly)
                {
                    Item item = player.CurrentItem;
                    if (item != null && shopMenu.highlightItemToSell(item))
                    {
                        if (shopMenu.onSell != null)
                        {
                            shopMenu.onSell(item);
                        }
                        else
                        {
                            float sellPercentage = mod.Helper.Reflection.GetField<float>(shopMenu, "sellPercentage").GetValue();
                            int num = (int)((float)item.sellToStorePrice(-1L) * sellPercentage);
                            ShopMenu.chargePlayer(Game1.player, shopMenu.currency, -num * item.Stack);
                            int num2 = item.Stack / 8 + 2;
                            ISalable? salable = null;
                            if (shopMenu.CanBuyback())
                            {
                                salable = shopMenu.AddBuybackItem(item, num, item.Stack);
                            }
                            if (item is StardewValley.Object @object && @object.edibility.Value != -300)
                            {
                                Item one = @object.getOne();
                                one.Stack = @object.Stack;
                                if (salable != null && shopMenu.buyBackItemsToResellTomorrow.TryGetValue(salable, out var value))
                                {
                                    value.Stack += @object.Stack;
                                }
                                else if (Game1.currentLocation is ShopLocation shopLocation)
                                {
                                    if (salable != null)
                                    {
                                        shopMenu.buyBackItemsToResellTomorrow[salable] = one;
                                    }
                                    shopLocation.itemsToStartSellingTomorrow.Add(one);
                                }
                            }
                            Game1.playSound("sell");
                            Game1.playSound("purchase");
                        }
                        player.removeItemFromInventory(item);
                        shopMenu.updateSaleButtonNeighbors();
                    }
                }

            }

        }

        public static void sell_to_shop_by_index(int index, Mod mod)
        {

            var menu = Game1.activeClickableMenu;
            var player = Game1.player;
            if (menu is ShopMenu shopMenu)
            {
                if (shopMenu.heldItem == null && !shopMenu.readOnly)
                {
                    Item item = player.Items[index];
                    if (item != null && shopMenu.highlightItemToSell(item))
                    {
                        if (shopMenu.onSell != null)
                        {
                            shopMenu.onSell(item);
                        }
                        else
                        {
                            float sellPercentage = mod.Helper.Reflection.GetField<float>(shopMenu, "sellPercentage").GetValue();
                            int num = (int)((float)item.sellToStorePrice(-1L) * sellPercentage);
                            ShopMenu.chargePlayer(Game1.player, shopMenu.currency, -num * item.Stack);
                            int num2 = item.Stack / 8 + 2;
                            ISalable? salable = null;
                            if (shopMenu.CanBuyback())
                            {
                                salable = shopMenu.AddBuybackItem(item, num, item.Stack);
                            }
                            if (item is StardewValley.Object @object && @object.edibility.Value != -300)
                            {
                                Item one = @object.getOne();
                                one.Stack = @object.Stack;
                                if (salable != null && shopMenu.buyBackItemsToResellTomorrow.TryGetValue(salable, out var value))
                                {
                                    value.Stack += @object.Stack;
                                }
                                else if (Game1.currentLocation is ShopLocation shopLocation)
                                {
                                    if (salable != null)
                                    {
                                        shopMenu.buyBackItemsToResellTomorrow[salable] = one;
                                    }
                                    shopLocation.itemsToStartSellingTomorrow.Add(one);
                                }
                            }
                            Game1.playSound("sell");
                            Game1.playSound("purchase");
                        }
                        player.removeItemFromInventory(item);
                        shopMenu.updateSaleButtonNeighbors();
                    }
                }

            }

        }

        public static void open_carpenter_construct()
        {
            answer_question("carpenter_Construct", new string[] { "carpenter"});
        }

        public static void build_current_building(int x_bias, int y_bias)
        {
            var curMenu = Game1.activeClickableMenu;
            if (curMenu is CarpenterMenu carpenterMenu)
            {
                Game1.player.team.buildLock.RequestLock(delegate
                {
                    if (Game1.locationRequest == null)
                    {
                        if (Helper.myTryToBuild(carpenterMenu, x_bias, y_bias))
                        {
                            carpenterMenu.ConsumeResources();
                            DelayedAction.functionAfterDelay(carpenterMenu.returnToCarpentryMenuAfterSuccessfulBuild, 2000);
                            carpenterMenu.freeze = true;
                        }
                        else
                        {
                            Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\UI:Carpenter_CantBuild"), 3));
                        }
                    }
                    Game1.player.team.buildLock.ReleaseLock();
                });
            }
            curMenu.exitThisMenu();
        }

        public static void switch_building(bool isForward)
        {
            var bias = isForward? 1:-1;
            var curMenu = Game1.activeClickableMenu;
            if (curMenu is CarpenterMenu carpenterMenu)
            {
                carpenterMenu.SetNewActiveBlueprint(carpenterMenu.Blueprint.Index+ bias);
            }
        }

        public static void open_animal_shop()
        {
            Game1.currentLocation.ShowAnimalShopMenu();
        }

        public static void select_dialogue(int index, Mod mod)
        {
            if (Game1.activeClickableMenu is DialogueBox db)
            {
                receiveLeftClickMirror(index, db, mod);
            }
            //var npc = testUtils.TestUtils.GetNearestNPC(Game1.player);
            //var name = npc?.Name;
            //var currentLocation = Game1.currentLocation;
            //if (Game1.activeClickableMenu is DialogueBox db && currentLocation.lastQuestionKey != null)
            //{
            //    var choice = db.responses[index];
            //    var choice_key = choice.responseKey;
            //    var key = currentLocation.lastQuestionKey + "_" + choice_key;
            //    answer_question(key, new string[] { key });
            //}

            //var choices = Game1.activeClickableMenu.allClickableComponents;
            //if (choices == null || choices?.Capacity <= index)
            //{
            //    return;
            //}
            //else
            //{
            //    var center = choices[index].bounds.Center;
            //    Game1.activeClickableMenu.receiveLeftClick(center.X, center.Y);
            //}
        }

        public static void receiveLeftClickMirror(int index, DialogueBox dialogueBox, Mod mod)
        {
            MethodInfo tryOutroInfo = mod.Helper.Reflection.GetMethod(
                        dialogueBox,
                        name: "tryOutro"
                    ).MethodInfo;
            MethodInfo setUpIconsInfo = mod.Helper.Reflection.GetMethod(
                        dialogueBox,
                        name: "setUpIcons"
                    ).MethodInfo;
            MethodInfo checkDialogueInfo = mod.Helper.Reflection.GetMethod(
                        dialogueBox,
                        name: "checkDialogue"
                    ).MethodInfo;
            dialogueBox.selectedResponse = index;
            if (dialogueBox.transitioning)
            {
                return;
            }
            if (dialogueBox.characterIndexInDialogue < dialogueBox.getCurrentString().Length - 1)
            {
                dialogueBox.characterIndexInDialogue = dialogueBox.getCurrentString().Length - 1;
            }
            else
            {
                if (dialogueBox.safetyTimer > 0)
                {
                    return;
                }
                if (dialogueBox.isQuestion)
                {
                    if (dialogueBox.selectedResponse == -1)
                    {
                        return;
                    }
                    dialogueBox.questionFinishPauseTimer = (Game1.eventUp ? 600 : 200);
                    dialogueBox.transitioning = true;
                    dialogueBox.transitionInitialized = false;
                    dialogueBox.transitioningBigger = true;
                    if (dialogueBox.characterDialogue == null)
                    {
                        Game1.dialogueUp = false;
                        if (Game1.eventUp && Game1.currentLocation.afterQuestion == null)
                        {
                            Game1.playSound("smallSelect");
                            Game1.currentLocation.currentEvent.answerDialogue(Game1.currentLocation.lastQuestionKey, dialogueBox.selectedResponse);
                            dialogueBox.selectedResponse = -1;
                            tryOutroInfo.Invoke(dialogueBox, new object[] {});
                            return;
                        }
                        if (Game1.currentLocation.answerDialogue(dialogueBox.responses[dialogueBox.selectedResponse]))
                        {
                            Game1.playSound("smallSelect");
                        }
                        dialogueBox.selectedResponse = -1;
                        tryOutroInfo.Invoke(dialogueBox, new object[] { });
                        return;
                    }
                    dialogueBox.characterDialoguesBrokenUp.Pop();
                    dialogueBox.characterDialogue.chooseResponse(dialogueBox.responses[dialogueBox.selectedResponse]);
                    dialogueBox.characterDialoguesBrokenUp.Push("");
                    Game1.playSound("smallSelect");
                }
                else if (dialogueBox.characterDialogue == null)
                {
                    dialogueBox.dialogues.RemoveAt(0);
                    if (dialogueBox.dialogues.Count == 0)
                    {
                        dialogueBox.closeDialogue();
                    }
                    else
                    {
                        dialogueBox.width = Math.Min(1200, SpriteText.getWidthOfString(dialogueBox.dialogues[0]) + 64);
                        dialogueBox.height = SpriteText.getHeightOfString(dialogueBox.dialogues[0], dialogueBox.width - 16);
                        dialogueBox.x = (int)Utility.getTopLeftPositionForCenteringOnScreen(dialogueBox.width, dialogueBox.height).X;
                        dialogueBox.y = Game1.uiViewport.Height - dialogueBox.height - 64;
                        dialogueBox.xPositionOnScreen = dialogueBox.x;
                        dialogueBox.yPositionOnScreen = dialogueBox.y;
                        setUpIconsInfo.Invoke(dialogueBox, new object[] { });
                    }
                }
                dialogueBox.characterIndexInDialogue = 0;
                if (dialogueBox.characterDialogue != null)
                {
                    int portraitIndex = dialogueBox.characterDialogue.getPortraitIndex();
                    if (dialogueBox.characterDialoguesBrokenUp.Count == 0)
                    {
                        dialogueBox.beginOutro();
                        return;
                    }
                    dialogueBox.characterDialoguesBrokenUp.Pop();
                    if (dialogueBox.characterDialoguesBrokenUp.Count == 0)
                    {
                        if (!dialogueBox.characterDialogue.isCurrentStringContinuedOnNextScreen)
                        {
                            dialogueBox.beginOutro();
                        }
                        dialogueBox.characterDialogue.exitCurrentDialogue();
                    }
                    if (!dialogueBox.characterDialogue.isDialogueFinished() && dialogueBox.characterDialogue.getCurrentDialogue().Length > 0 && dialogueBox.characterDialoguesBrokenUp.Count == 0)
                    {
                        dialogueBox.characterDialogue.prepareCurrentDialogueForDisplay();
                        if (dialogueBox.characterDialogue.isDialogueFinished())
                        {
                            dialogueBox.beginOutro();
                            return;
                        }
                        dialogueBox.characterDialoguesBrokenUp.Push(dialogueBox.characterDialogue.getCurrentDialogue());
                    }
                    checkDialogueInfo.Invoke(dialogueBox, new object[] { dialogueBox.characterDialogue });
                    if (dialogueBox.characterDialogue.getPortraitIndex() != portraitIndex)
                    {
                        dialogueBox.newPortaitShakeTimer = ((dialogueBox.characterDialogue.getPortraitIndex() == 1) ? 250 : 50);
                    }
                }
                if (!dialogueBox.transitioning)
                {
                    Game1.playSound("smallSelect");
                }
                setUpIconsInfo.Invoke(dialogueBox, new object[] { });
                dialogueBox.safetyTimer = 750;
                if (dialogueBox.getCurrentString() != null && dialogueBox.getCurrentString().Length <= 20)
                {
                    dialogueBox.safetyTimer -= 200;
                }
            }
        }

        public static void exit_menu()
        {
            if (Game1.activeClickableMenu is null)
            {
                return;
            }
            Game1.activeClickableMenu.clickAway();
            Game1.exitActiveMenu();
        }

 
        public static void location_perform_action(string actionId)
        {
            var player = Game1.player;
            var gameLocation = Game1.currentLocation;
            int player_x_pos = (int)player.TilePoint.X;
            int player_y_pos = (int)player.TilePoint.Y;
            var location = new Location(player_x_pos, player_y_pos);
            gameLocation.performAction(actionId, player, location);
        }

        public static void upgrade_backpack()
        {
            answer_question("Backpack_Purchase", new string[]{"Backpack"});
        }

        public static void answer_question(string answerKey, string[] questionKeys)
        {
            Game1.currentLocation.answerDialogueAction(answerKey, questionKeys);
            exit_menu();
        }

        public static void upgrade_house()
        {
            answer_question("carpenter_Upgrade", new string[] { "carpenter" });
            answer_question("upgrade_Yes", new string[] { "upgrade" });
        }

        public static void talk(Mod mod)
        {
            Farmer player = Game1.player;

            NPC? nearestNpc = testUtils.TestUtils.GetNearestNPC(player);

            if (nearestNpc != null)
            {
                nearestNpc.checkAction(player, Game1.currentLocation);
            }
            else
            {
                Game1.addHUDMessage(new HUDMessage("No NPC surrounded"));
            }
        }

        [Obsolete("Aborted,  use_item(int posX, int posY, Mod mod, int power = 0) instead")]
        public static void use_tool(int posX, int posY, Mod mod, int power = 0)
        {
            var player = Game1.player;
            var tool = player.CurrentTool;

            if (tool == null)
            {
                mod.Monitor.Log($"Error: Tool is null.", LogLevel.Error);
                return;
            }

            tool.DoFunction(player.currentLocation, posX * Game1.tileSize, posY * Game1.tileSize, power, player);
            mod.Monitor.Log($"Used {tool.Name} on {posX}, {posY}.", LogLevel.Info);
        }

        public static void use_item(int posX, int posY, Mod mod, int power = 1)
        {
            var player = Game1.player;
            var item = player.CurrentItem;
            if (item == null)
                return;

            if (item is Tool tool)
            {
                Farmer.useTool(player);
            }
            else if (item is MeleeWeapon weapon)
            {
                weapon.DoFunction(Game1.currentLocation, posX, posY, power, player);
            }
            else if (item is StardewValley.Object obj)
            {
                Microsoft.Xna.Framework.Vector2 tileLocation = new Microsoft.Xna.Framework.Vector2(posX, posY);

                if (obj.canBePlacedHere(Game1.currentLocation, tileLocation))
                {
                    if (obj.placementAction(Game1.currentLocation, posX * Game1.tileSize, posY * Game1.tileSize, player))
                    {
                        player.reduceActiveItemByOne();
                    }
                }
                else
                {
                    if (obj.performUseAction(Game1.currentLocation))
                    {
                        player.reduceActiveItemByOne();
                    }
                }

            }
            else
            {
                item.actionWhenBeingHeld(player);
            }
        }

        public static void InteractWithObject(Vector2 targetPosition, Mod mod)
        {
            var player = Game1.player;
            var currentLocation = player.currentLocation;

            if (currentLocation.Objects.TryGetValue(targetPosition, out StardewValley.Object obj))
            {
                if (obj is Chest)
                {
                    InteractWithChest((Chest)obj, mod);
                }
                else if (obj is BedFurniture)
                {
                    InteractWithBed(mod);
                }
                else
                {
                    mod.Monitor.Log("This object is not interactable", LogLevel.Info);
                }
            }
            else
            {
                mod.Monitor.Log("No object that is interactable", LogLevel.Warn);
            }
        }

        public static void InteractWithChest(Chest chest, Mod mod)
        {
            // Open the chest menu directly with the chest's items
            Game1.activeClickableMenu = new StardewValley.Menus.ItemGrabMenu(
                chest.GetItemsForPlayer(Game1.player.UniqueMultiplayerID),
                reverseGrab: false,
                showReceivingMenu: true,
                highlightFunction: null,
                behaviorOnItemSelectFunction: chest.grabItemFromInventory,
                message: null,
                behaviorOnItemGrab: chest.grabItemFromChest,
                canBeExitedWithKey: true,
                showOrganizeButton: true,
                context: chest);
            mod.Monitor.Log("Open Box", LogLevel.Info);
        }

        public static void InteractWithBed(Mod mod)
        {
            Game1.player.doEmote(24); 
            // Trigger sleep/pass-out action properly by simulating bed interaction
            Game1.timerUntilMouseFade = 1000;  // Adjust this to suit timing
            Game1.player.Halt();
            Game1.player.freezePause = 1000;
            Game1.NewDay(6f);  // Simulate the new day
            mod.Monitor.Log("Player sleep", LogLevel.Info);
        }

        public static void WatchTV(Mod mod)
        {
            var tv = new TV();
            tv.checkForAction(Game1.player, true);
            mod.Monitor.Log("Player TV", LogLevel.Info);
        }

        public static void PlayArcade(Mod mod)
        {
            Game1.currentMinigame = new AbigailGame();
            mod.Monitor.Log("Player Gamming", LogLevel.Info);
        }

        public static void UseMineElevator(Mod mod)
        {
            if (Game1.mine != null)
            {
                Game1.activeClickableMenu = new MineElevatorMenu();
                mod.Monitor.Log("Player is on elevater", LogLevel.Info);
            }
            else
            {
                mod.Monitor.Log("elevater is not available", LogLevel.Warn);
            }
        }

        public static void UseSlotMachine(Mod mod)
        {
            Game1.currentMinigame = new Slots();
            mod.Monitor.Log("Player using tiger machine", LogLevel.Info);
        }

        public static Point? GetAdjacentAvailablePoint(Point p)
        {
            Point[] adjacentOffsets = new Point[]
            {
                new Point(0, -1),  // up
                new Point(1, 0),   // right
                new Point(0, 1),   // down 
                new Point(-1, 0)   // left
            };
            var player = Game1.player;
            foreach (var offset in adjacentOffsets)
            {
                Point testTile = new Point(p.X + offset.X, p.Y + offset.Y);
                if (Game1.currentLocation.isTilePassable(new Vector2(testTile.X, testTile.Y)))
                {
                    var pathFinder = new PathFindController(player, player.currentLocation, testTile, -1);
                    if (pathFinder.pathToEndPoint != null && pathFinder.pathToEndPoint.Count > 0)
                    {
                        bool collideWithNPC = false;
                        foreach(var point in pathFinder.pathToEndPoint.ToList())
                        {
                            if (point.X == p.X && point.Y == p.Y)
                            {
                                collideWithNPC = true;
                            }
                        }
                        if (collideWithNPC)
                        {
                            continue;
                        }
                        else
                        {
                            return testTile;
                        }
                    }
                }
            }
            return null;
        }

        // Auto-pathing method
        public static void StartAutoPathing(Vector2 targetTile, Action<bool> onComplete, Mod mod)
        {
            mod.Monitor.Log("Attempting to auto-path player.", LogLevel.Info);
            LogToFile("Attempting to auto-path player.", mod);
            // Calculate the path
            var player = Game1.player;
            var pathFinder = new PathFindController(player, player.currentLocation, targetTile.ToPoint(), -1);

            void OnWarped(object? sender, WarpedEventArgs e)
            {
                onComplete(true);
                mod.Helper.Events.Player.Warped -= OnWarped;
                mod.Helper.Events.Display.MenuChanged -= OnMenuChanged;
                mod.Monitor.Log($"Warped while moving to {targetTile}.", LogLevel.Info);
                LogToFile($"Warped while moving to {targetTile}.", mod);
            }

            mod.Helper.Events.Player.Warped += OnWarped;

            void OnMenuChanged(object? sender, MenuChangedEventArgs? e)
            {
                onComplete(true);
                mod.Helper.Events.Player.Warped -= OnWarped;
                mod.Helper.Events.Display.MenuChanged -= OnMenuChanged;
                mod.Monitor.Log($"Menu changed while moving to {targetTile}.", LogLevel.Info);
                LogToFile($"Menu changed while moving to {targetTile}.", mod);

            }

            mod.Helper.Events.Display.MenuChanged += OnMenuChanged;

            if (pathFinder.pathToEndPoint != null && pathFinder.pathToEndPoint.Count > 0)
            {
                var npcs = Game1.currentLocation.characters.ToList();
                foreach(var npc in npcs)
                {
                    if(npc.TilePoint.X == targetTile.X && npc.TilePoint.Y == targetTile.Y)
                    {
                        mod.Monitor.Log($"Auto-pathing to npc, avoiding {targetTile}.", LogLevel.Info);
                        LogToFile($"Auto-pathing to npc, avoiding {targetTile}.", mod);
                        var adjacentAvalablePoint = GetAdjacentAvailablePoint(npc.TilePoint);
                        if(adjacentAvalablePoint != null)
                        {
                            mod.Helper.Events.Player.Warped -= OnWarped;
                            mod.Helper.Events.Display.MenuChanged -= OnMenuChanged;
                            StartAutoPathing(new Vector2(adjacentAvalablePoint?.X ?? 0, adjacentAvalablePoint?.Y ?? 0), onComplete, mod);
                            return;
                        }
                        else
                        {
                            onComplete(false);
                            mod.Helper.Events.Player.Warped -= OnWarped;
                            mod.Helper.Events.Display.MenuChanged -= OnMenuChanged;
                            return;
                        }
                    }
                }


                player.controller = pathFinder;
                mod.Monitor.Log($"Auto-pathing started to {targetTile}.", LogLevel.Info);
                LogToFile($"Auto-pathing started to {targetTile}.", mod);

                // Immediately start the clearing task after pathing completes
                pathFinder.endBehaviorFunction = (farmer, location) =>
                {
                    onComplete(true);
                    mod.Helper.Events.Player.Warped -= OnWarped;
                    mod.Helper.Events.Display.MenuChanged -= OnMenuChanged;
                    mod.Monitor.Log($"Player has reached target {targetTile}.", LogLevel.Info);
                    LogToFile($"Player has reached target {targetTile}.", mod);

                };
            }
            else
            {
                onComplete(false);
                mod.Helper.Events.Player.Warped -= OnWarped;
                mod.Helper.Events.Display.MenuChanged -= OnMenuChanged;
                mod.Monitor.Log($"No valid path to {targetTile}.", LogLevel.Warn);
                LogToFile($"No valid path to {targetTile}.", mod);

            }
        }

        public class PlayerData
        {
            public string Name { get; set; }
            public int Health { get; set; }
            public float Stamina { get; set; }
            public int Money { get; set; }
            public string Location { get; set; }
            public Point Position { get; set; }
            public int FacingDirection { get; set; }
            public List<InventoryItem> Inventory { get; set; }
            public CurrentInventoryData CurrentInventory { get; set; }
            public List<string> Professions { get; set; }
            public SkillsData Skills { get; set; }
            public List<string> DatingPartners { get; set; }
            public List<string> Spouse { get; set; }
        }

        public class InventoryItem
        {
            public string Name { get; set; }
            public int? Quantity { get; set; }
        }

        public class CurrentInventoryData
        {
            public int Index { get; set; }
            public string CurrentItem { get; set; }
        }

        public class SkillsData
        {
            public int Farming { get; set; }
            public int Mining { get; set; }
            public int Combat { get; set; }
            public int Fishing { get; set; }
            public int Foraging { get; set; }
        }

        public class FarmData
        {
            public List<FarmAnimalDataInfo> Animals { get; set; }
            public List<FarmBuildingInfo> Buildings { get; set; }
            public List<PetData> Pets { get; set; }
        }

        public class FarmAnimalDataInfo
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public Point Position { get; set; }
            public int Friendship { get; set; }
            public bool IsAdult { get; set; }
            public string CurrentProduce { get; set; }
            public int Happiness { get; set; }
            public bool isTouched { get; set; }
        }

        public class PetData
        {
            public string Type { get; set; } 
            public string Name { get; set; }
            public Point Position { get; set; }
            public int Friendship { get; set; }
            public bool isTouched { get; set; }
        }

        public class GameData
        {
            public PlayerData Player { get; set; }
            public List<NPCData> NPCs { get; set; }
            public GameStateData GameState { get; set; }
            public FarmData Farm { get; set; }
            // public ProgressionData Progression { get; set; }
            public CurrentMenuData CurrentMenuData { get; set; }
            public byte[]? ScreenShot { get; set; }
            public List<BuildingInfo> Buildings { get; set; }
            public List<CropInfo> Crops { get; set; }
            public List<ExitInfo> Exits { get; set; }
            public List<CounterInfo> ShopCounters { get; set; }
            public GameMetaData MetaData { get; set; }
            public CallBackData CallBackData { get; set; }
            public List<TileInfo> SurroundingsData { get; set; }
            public List<FurnitureInfo> Furnitures { get; set; }
        }

        public class CallBackData
        {
            public int OnDayStarted { get; set; }
        }

        public class GameMetaData
        {
            public int[] ViewportSize { get; set; }
        }


        public class SkillData
        {
            public int Farming { get; set; }
            public int Mining { get; set; }
            public int Combat { get; set; }
            public int Fishing { get; set; }
            public int Foraging { get; set; }
        }

        public class GameStateData
        {
            public int Time { get; set; }
            public int DayOfMonth { get; set; }
            public string Season { get; set; }
            public int Year { get; set; }
            public string Weather { get; set; }
        }

        public class ResponseInfo
        {
            public string responseKey { get; set; }
            public string responseText { get; set; }
        }

        public class CurrentMenuData
        {
            public string type { get; set; }
            public string? message { get; set; }
            public int? currentPrizeTrack { get; set; }
            public List<string>? dialogues { get; set; }
            public List<string>? chats { get; set; }
            public List<SaleItemInfo>? shopMenuData { get; set; }
            public List<SaleItemInfo>? animalsMenuData { get; set; }
            public List<ResponseInfo>? responses { get; set; }
            public string? bluePrints { get; set; }
            public List<ChestItem>? ItemsInChest { get; set; }
        }

        public class ProgressionData
        {
            public bool CommunityCenter { get; set; }
            public int MineLevel { get; set; }
            public int SkullCavernLevel { get; set; }
            public List<int> Achievements { get; set; }
            public List<BundleInfo> Bundles { get; set; }
            public List<RepairInfo> Repairs { get; set; }
            public bool MovieTheater { get; set; }
            public bool JojaMembership { get; set; }
            public List<MuseumPieceInfo> Museum { get; set; }
            public List<QuestInfo> Quests { get; set; }
        }

        // Export game data as JSON and return it as a string
        public static byte[]? ExportGameData(int size, Mod mod)
        {
            var gameData = GatherGameData(size, mod);


            var mapper = new CBORTypeMapper();

            byte[] serializedData = CBORObject.FromObject(gameData, mapper).EncodeToBytes();


            CBORObject deserializedCbor = CBORObject.DecodeFromBytes(serializedData);


            var deserializedGameData = deserializedCbor.ToObject<Dictionary<string, object>>();


            //byte[] serializedData = CBORObject.FromObject(gameData).EncodeToBytes();
            //var options = MessagePackSerializerOptions.Standard.WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            //var serializedData = MessagePackSerializer.Serialize(gameData, options);
            Console.WriteLine("time_point_12: " + DateTime.Now.ToString("HH:mm:ss.fff"));

            return serializedData;
        }

        public static string ExportGameData_v2(int size, Mod mod)
        {
            var gameData = GatherGameData(size, mod);
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };
            var j_info = JsonConvert.SerializeObject(gameData, settings);
            return j_info;
        }

        private static GameData GatherGameData(int size, Mod mod)
        {
            Console.WriteLine("time_point_0: "+ DateTime.Now.ToString("HH:mm:ss.fff"));
            var playerData = GetPlayerData();
            Console.WriteLine("time_point_1: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            var npcData = GetNPCData();
            Console.WriteLine("time_point_2: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            var gameStateData = GetGameStateData();
            Console.WriteLine("time_point_3: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            var farmData = GetFarmData(mod);
            Console.WriteLine("time_point_4: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            var progressionData = GetProgressionData();
            Console.WriteLine("time_point_5: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            var currentMenuData = GetCurrentMenuData();
            Console.WriteLine("time_point_6: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            var gameMetaData = GetGameMetaData();
            Console.WriteLine("time_point_7: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            var surroundingsData = GetSurroundings(size);
            Console.WriteLine("time_point_8: " + DateTime.Now.ToString("HH:mm:ss.fff"));


            // buildings eliminated
            var buildings = Game1.currentLocation.buildings;
            Dictionary<string,(int X, int Y)> doorCoordinates = new Dictionary<string, (int X, int Y)>();
            List<BuildingInfo> buildingsData = new List<BuildingInfo>();
            foreach (var building in buildings)
            {
                BuildingInfo buildingInfo = new BuildingInfo()
                {
                    name = building.buildingType.Value,
                };
                if (building.humanDoor.Value != null)
                {
                    var doorX = building.tileX.Value + building.humanDoor.Value.X;
                    var doorY = building.tileY.Value + building.humanDoor.Value.Y;
                    if (doorCoordinates.Keys.Contains(building.buildingType.Value))
                    {
                        continue;
                    }
                    doorCoordinates.Add(building.buildingType.Value, (doorX, doorY));
                    buildingInfo.doorPosition = new Vector2(doorX, doorY);
                }
                if (buildingInfo.doorPosition is null)
                {
                    foreach (var doorDict in Game1.currentLocation.doors.FieldDict.ToList())
                    {
                        if (doorCoordinates.Keys.Contains(doorDict.Value.Value) || doorDict.Value.Value != buildingInfo.name)
                        {
                            continue;
                        }
                        var doorPoint = doorDict.Key;
                        var doorX = doorPoint.X;
                        var doorY = doorPoint.Y;
                        doorCoordinates.Add(doorDict.Value.Value, (doorX, doorY));
                    }
                }
                buildingsData.Add(buildingInfo);
            }

            foreach(var door in Game1.currentLocation.doors.ToList()[0].ToList())
            {
                BuildingInfo buildingInfo = new BuildingInfo()
                {
                    name = door.Value,
                    doorPosition = new Vector2() {
                        X = door.Key.X,
                        Y = door.Key.Y
                    }
                };
                buildingsData.Add(buildingInfo);
            }


            // crops eliminated
            List<CropInfo> cropCoordinates = new List<CropInfo>();
            foreach (TerrainFeature terrainFeature in Game1.currentLocation.terrainFeatures.Values)
            {
                if (terrainFeature is HoeDirt { crop: not null } hoeDirt && !hoeDirt.crop.dead.Value)
                {
                    string cropId = hoeDirt.crop.netSeedIndex.Value;
                    var crop = hoeDirt.crop;
                    cropCoordinates.Add(new CropInfo
                    {
                        id = cropId,
                        position = new Vector2(hoeDirt.Tile.ToPoint().X, hoeDirt.Tile.ToPoint().Y),
                        isWatered = hoeDirt.isWatered(),
                        isDead = crop.dead.Value,
                        forage_crop = crop.forageCrop.Value,
                        current_phase = crop.currentPhase.Value,
                    }) ;
                }
            }


            // furnitures eliminated
            List<FurnitureInfo> furnitures = new List<FurnitureInfo>();
            foreach (Furniture furniture in Game1.currentLocation.furniture.ToList())
            {
                if (furniture is BedFurniture bedFurniture)
                {
                    var bedOffset = bedFurniture.bedTileOffset;
                    var real_position = new Vector2(furniture.TileLocation.X+bedOffset, furniture.TileLocation.Y+bedOffset);
                    var boundingBox = furniture.GetBoundingBox();
                    var topB = boundingBox.Top/Game1.tileSize;
                    var bottomB = boundingBox.Bottom / Game1.tileSize;
                    var leftB = boundingBox.Left / Game1.tileSize;
                    var rightB = boundingBox.Right / Game1.tileSize;
                    furnitures.Add(new FurnitureInfo
                    {
                        name = furniture.name,
                        position = real_position,
                        boundingBox = new BoundingBoxInfo
                        {
                            top = topB,
                            bottom = bottomB,
                            left = leftB,
                            right = rightB
                        }
                    });
                }
                else
                {
                    var boundingBox = furniture.GetBoundingBox();
                    var topB = boundingBox.Top / Game1.tileSize;
                    var bottomB = boundingBox.Bottom / Game1.tileSize;
                    var leftB = boundingBox.Left / Game1.tileSize;
                    var rightB = boundingBox.Right / Game1.tileSize;
                    furnitures.Add(new FurnitureInfo
                    {
                        name = furniture.name,
                        position = new Vector2(furniture.TileLocation.X, furniture.TileLocation.Y),
                        boundingBox = new BoundingBoxInfo
                        {
                            top = topB,
                            bottom = bottomB,
                            left = leftB,
                            right = rightB
                        }
                    });
                }
            }

            // exits eliminated
            List<ExitInfo> exits = new List<ExitInfo>();
            foreach(Warp warp in Game1.currentLocation.warps.ToList())
            {
                exits.Add(new ExitInfo
                {
                    target = warp.TargetName,
                    position = new Vector2(warp.X, warp.Y)
                });
            }

            // counters eliminated
            List<CounterInfo> counters = new List<CounterInfo>();
            var counter_names = new Dictionary<string, Vector2>
            {
                {"SeedShop Counter", new Vector2(4, 18) },
                {"AnimalShop Counter", new Vector2(12, 15) },
                {"JojaMart Counter", new Vector2(10, 25) },
                {"Blacksmith Counter", new Vector2(3, 14) },
                {"ScienceHouse Counter", new Vector2(8, 19) },
                {"FishShop Counter", new Vector2(5, 5) },
            };
            foreach(var counter in counter_names)
            {
                counters.Add(new CounterInfo
                {
                    name = counter.Key,
                    position = counter.Value,
                });
            }

            Console.WriteLine("time_point_11: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            var res = new GameData()
            {
                Player = playerData,
                NPCs = npcData,
                // Locations = allLocationsData,
                GameState = gameStateData,
                Farm = farmData,
                // Progression = progressionData,
                CurrentMenuData = currentMenuData ?? new CurrentMenuData { type = "No Menu" },
                ScreenShot = pixelData,
                // Doors = doorCoordinates,
                Buildings = buildingsData,
                Crops = cropCoordinates,
                Furnitures = furnitures,
                Exits = exits,
                ShopCounters = counters,
                MetaData = gameMetaData,
                CallBackData = new CallBackData
                {
                    OnDayStarted = dayStartTimes
                },
                SurroundingsData = surroundingsData
            };
            return res;
        }

        public class CropInfoInSurroundings
        {
            public string? seed_id { get; set; }
            public string? index_harvest { get; set; }
        }

        public class CropInfo
        {
            public string? id { get; set; }
            public Vector2? position  { get; set; }
            public bool isWatered { get; set; }
            public bool isDead { get; set; }
            public bool forage_crop { get; set; }
            public int current_phase { get; set; }
        }

        public class BuildingInfo
        {
            public string? name { get; set; }
            public Vector2? doorPosition { get; set; }
        }

        public class BoundingBoxInfo
        {
            public int top { get; set; }
            public int bottom { get; set; }
            public int left { get; set; }
            public int right { get; set; }
        }

        public class FurnitureInfo
        {
            public string? name { get; set; }
            public Vector2? position { get; set; }
            public BoundingBoxInfo boundingBox { get; set; }
        }

        public class CounterInfo
        {
            public string? name { get; set; }
            public Vector2? position { get; set; }
        }

        public class ExitInfo
        {
            public string? target { get; set; }
            public Vector2? position { get; set; }
        }

        public class SaleItemInfo
        {
            public string? name { get; set; }
            public int price { get; set; }
        }

        public static PlayerData GetPlayerData()
        {
            var professionIds = Game1.player.professions; // List<int>

          
            var professionNames = professionIds
                .Select(id => ProfessionHelper.ProfessionNames.TryGetValue(id, out string name)
                              ? name
                              : $"Unknown_{id}")
                .ToList();

            // love
            List<string> datingPartners = new List<string>();
            List<string> spouse = new List<string>();
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (Game1.player.friendshipData.ContainsKey(npc.Name))
                {
                    if (Game1.player.friendshipData[npc.Name].IsDating())
                        datingPartners.Add(npc.Name);
                    if (Game1.player.friendshipData[npc.Name].IsEngaged() || Game1.player.friendshipData[npc.Name].IsMarried())
                        spouse.Add(npc.Name);
                }
            }
            var res = new PlayerData {
                Name = Game1.player.Name,
                Health = Game1.player.health,
                Stamina = Game1.player.Stamina,
                Money = Game1.player.Money,
                Location = Game1.player.currentLocation.Name,
                Position = Game1.player.TilePoint,
                FacingDirection = Game1.player.facingDirection.Value,
                Inventory = Game1.player.Items.Select(item => new InventoryItem {
                    Name = item?.Name,
                    Quantity = item?.stack.Value
                }).ToList(),
                CurrentInventory = new CurrentInventoryData
                {
                    Index = Game1.player.CurrentToolIndex,
                    CurrentItem = Game1.player.CurrentItem?.Name ?? "None"
                },
                Professions = professionNames,
                Skills = new SkillsData
                {
                    Farming = Game1.player.farmingLevel.Value,
                    Mining = Game1.player.miningLevel.Value,
                    Combat = Game1.player.combatLevel.Value,
                    Fishing = Game1.player.fishingLevel.Value,
                    Foraging = Game1.player.foragingLevel.Value
                },
                DatingPartners = datingPartners,
                Spouse = spouse
            };
            return res;
        }

        public static List<NPCData> GetNPCData()
        {
            return Utility.getAllCharacters().Select(npc =>
            {
                Game1.player.friendshipData.TryGetValue(npc.Name, out var friendshipData);
                return new NPCData
                {
                    Name = npc.Name,
                    Location = npc.currentLocation.Name,
                    Friendship = Game1.player.getFriendshipLevelForNPC(npc.Name),
                    Position = new List<int>() { npc.TilePoint.X, npc.TilePoint.Y },
                    isTalked = Game1.player.hasPlayerTalkedToNPC(npc.Name),
                    GiftsToday = friendshipData?.GiftsToday ?? 0,
                };
            }).ToList();

        }

        public class NPCData
        {
            public string? Name { get; set; }
            public string? Location { get; set; }
            public int Friendship { get; set; }
            public List<int>? Position { get; set; }
            public bool isTalked { get; set; }
            public int GiftsToday { get; set; }
        }

        public static GameStateData GetGameStateData()
        {
            return new GameStateData
            {
                Time = Game1.timeOfDay,
                DayOfMonth = Game1.dayOfMonth,
                Season = Game1.currentSeason,
                Year = Game1.year,
                Weather = Game1.isRaining ? "Raining" : Game1.isSnowing ? "Snowing" : "Sunny",
            };
        }

        public class FarmBuildingInfo
        {
            public string? type { get; set; }
            public Vector2? position { get; set; }
            public bool? isAnimalDoorOpen { get; set; }
            public bool? isBowlFull { get; set; }
            public int? hayNumber { get; set; }
            public System.Guid? id {  get; set; }
            public int? upgradeLevel { get; set; }
        }

        public static FarmData GetFarmData(Mod mod)
        {
            var farm = Game1.getFarm();

            // Pets
            List<Pet> pets = new List<Pet>();
            foreach(var pet in farm.characters.OfType<Pet>())
            {
                pets.Add(pet);
            }

            //Buildings
            var buildings = farm.buildings;
            List<FarmBuildingInfo> buildingsData = new List<FarmBuildingInfo>();
            foreach (var building in buildings)
            {
                // isWatered
                bool? isWatered = null;
                var wateredField = mod.Helper.Reflection.GetField<NetBool>(building, "watered", required: false);
                if (wateredField != null)
                {

                    isWatered = wateredField.GetValue().Value;
                }

                // hayNumber
                int hayNumber = 0;
                // silo
                if (building.buildingType?.Value == "Silo")
                {
                    hayNumber = farm.piecesOfHay.Value;
                }
                // coop or barn
                else if (building.indoors?.Value is AnimalHouse animalHouse)
                {
                    for (int x = 0; x < animalHouse.map.Layers[0].LayerWidth; x++)
                    {
                        for (int y = 0; y < animalHouse.map.Layers[0].LayerHeight; y++)
                        {
                            if (animalHouse.doesTileHaveProperty(x, y, "Trough", "Back") == null)
                            {
                                continue;
                            }
                            Vector2 tileLocation = new Vector2(x, y);
                            if (animalHouse.objects.ContainsKey(tileLocation))
                            {
                                StardewValley.Object hay = animalHouse.objects[tileLocation];
                                if (hay.Name == "Hay")
                                {
                                    hayNumber++;
                                }
                            }
                        }
                    }
                }

                //upgradeLevel
                int? upgradeLevel = null;
                if (building.buildingType?.Value == "Farmhouse")
                {
                    FarmHouse? farmHouse = Game1.getLocationFromName("FarmHouse") as FarmHouse;
                    if (farmHouse != null)
                        upgradeLevel = farmHouse.upgradeLevel;
                }

                FarmBuildingInfo buildinginfo = new FarmBuildingInfo()
                {
                    type = building.buildingType?.Value,
                    position = new Vector2(building.tileX.Value, building.tileY.Value),
                    isAnimalDoorOpen = building.animalDoorOpen?.Value,
                    isBowlFull = isWatered,
                    hayNumber = hayNumber,
                    id = building.id?.Value,
                    upgradeLevel = upgradeLevel,
                };
                buildingsData.Add(buildinginfo);
            }
            return new FarmData
            {
                Animals = farm.getAllFarmAnimals().Select(a => new FarmAnimalDataInfo {
                    Type = a.type.Value,
                    Name = a.Name,
                    Position = a.TilePoint,
                    Friendship = a.friendshipTowardFarmer.Value,
                    IsAdult = a.isAdult(),
                    CurrentProduce = a.currentProduce.Value,
                    Happiness = a.happiness.Value,
                    isTouched = a.wasPet.Value
                }).ToList(),
                Buildings = buildingsData.ToList(),
                Pets = pets.Select(p => new PetData
                {
                    Type = p.petType.Value,
                    Name = p.Name,
                    Position = p.TilePoint,
                    Friendship = p.friendshipTowardFarmer.Value,
                    isTouched = p.grantedFriendshipForPet.Value
                }).ToList()
            };
        }

        public class BundleInfo
        {
            public int id { get; set; }
            public string? name { get; set; }
            public bool completed { get; set; }
        }

        public class RepairInfo
        {
            public int id { get; set; }
            public string? project { get; set; }
            public bool completed { set; get; }
        }

        public class MuseumPieceInfo
        {
            public Vector2 position { get; set; }
            public string? itemId { get; set; }
            public string? itemName { get; set; }
        }

        public class QuestInfo
        {
            public string id { get; set; }
            public bool completed { get; set; }
        }

        public static ProgressionData GetProgressionData()
        {
            CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");

            // bundles
            List<BundleInfo> bundlesData = new List<BundleInfo>();
            var bundleNames = new Dictionary<int, string>
            {
                { 0, "Spring Crops Bundle" },
                { 1, "Summer Crops Bundle" },
                { 2, "Fall Crops Bundle" },
                { 3, "Quality Crops Bundle" },
                { 4, "Animal Bundle" },
                { 5, "Artisan Bundle" },
                { 6, "River Fish Bundle" },
                { 7, "Lake Fish Bundle" },
                { 8, "Ocean Fish Bundle" },
                { 9, "Night Fishing Bundle" },
                { 10, "Specialty Fish Bundle" },
                { 11, "Crab Pot Bundle" },
                { 13, "Spring Foraging Bundle" },
                { 14, "Summer Foraging Bundle" },
                { 15, "Fall Foraging Bundle" },
                { 16, "Winter Foraging Bundle" },
                { 17, "Construction Bundle" },
                { 19, "Exotic Foraging Bundle" },
                { 20, "Blacksmith's Bundle" },
                { 21, "Geologist's Bundle" },
                { 22, "Adventurer's Bundle" },
                { 23, "2500 Bundle" },
                { 24, "5000 Bundle" },
                { 25, "10000 Bundle" },
                { 26, "25000 Bundle" },
                { 31, "Chef's Bundle" },
                { 32, "Field Research Bundle" },
                { 33, "Enchanter's Bundle" },
                { 34, "Dye Bundle" },
                { 35, "Fodder Bundle" },
                { 36, "The Missing Bundle" },
            };
            foreach (int id in communityCenter.bundles.Keys)
            {
                bundlesData.Add(new BundleInfo
                {
                    id = id,
                    name = bundleNames[id],
                    completed = communityCenter.isBundleComplete(id),
                });
            }

            // repairs
            List<RepairInfo> repairsData = new List<RepairInfo>();
            var mails = new Dictionary<int, string>
            {
                { 0, "jojaPantry" },
                { 1, "jojaCraftsRoom" },
                { 2, "jojaFishTank" },
                { 3, "jojaBoilerRoom" },
                { 4, "jojaVault" },
                { 5, "ccPantry" },
                { 6, "ccCraftsRoom" },
                { 7, "ccFishTank" },
                { 8, "ccBoilerRoom" },
                { 9, "ccVault" },
            };
            var projects = new Dictionary<int, string>
            {
                { 0, "Greenhouse" },
                { 1, "Bridge Repair" },
                { 2, "Glittering Boulder Removed" },
                { 3, "Minecarts Repaired" },
                { 4, "Bus Repair" },
            };
            for (int i = 0; i < projects.Count; i++)
            {
                repairsData.Add(new RepairInfo
                {
                    id = i,
                    project = projects[i],
                    completed = (Game1.player.hasOrWillReceiveMail(mails[i]) || Game1.player.hasOrWillReceiveMail(mails[i + 5])),
                });
            }

            // museum
            var museumPieces = Game1.RequireLocation<LibraryMuseum>("ArchaeologyHouse").museumPieces.FieldDict;
            List<MuseumPieceInfo> museumPiecesData = new List<MuseumPieceInfo>();
            foreach(var piece in museumPieces)
            {
                Item item = ItemRegistry.Create(piece.Value.Value);
                museumPiecesData.Add(new MuseumPieceInfo
                {
                    position = piece.Key,
                    itemId = piece.Value.Value,
                    itemName = item.Name,
                });
            }

            // quests
            var quests = Game1.player.questLog;
            List<QuestInfo> questsData = new List<QuestInfo>();
            foreach (Quest quest in quests)
            {
                questsData.Add(new QuestInfo
                {
                    id = quest.id.Value,
                    completed = quest.completed.Value,
                });
            }

            return new ProgressionData
            {
                CommunityCenter = Game1.player.hasCompletedCommunityCenter(),
                MineLevel = Game1.player.deepestMineLevel,
                SkullCavernLevel = Math.Max(0, Game1.player.deepestMineLevel - 120), // Approximate
                Achievements = Game1.player.achievements.ToList(),
                Bundles = bundlesData.ToList(),
                Repairs = repairsData.ToList(),
                MovieTheater = (Game1.player.team.theaterBuildDate.Value < 0) ? false : true,
                JojaMembership = Game1.player.hasOrWillReceiveMail("JojaMember"),
                Museum = museumPiecesData,
                Quests = questsData,
            };
        }

        public class TileInfo
        {
            public List<int>? position { get; set; }
            public string? object_at_tile { get; set; }
            public string? terrain_at_tile { get; set; }
            public string? building_info { get; set; }
            public string? tile_properties { get; set; }
            public CropInfoInSurroundings? crop_at_tile { get; set; }
            public string? debris_at_tile { get; set; }
            public string? furniture_at_tile { get; set; }
            public string? exit_info { get; set; }
            public string? npc_info { get; set; }
            public bool? placeable { get; set; }
        }

        public static TileInfo GetTileInfo(string x, string y)
        {
            int xI = int.Parse(x);
            int yI = int.Parse(y);
            var key = new Vector2(xI, yI);
            string? object_info = "";
            string? terrain_info = "";
            string? builing_info = "";
            CropInfoInSurroundings? crop_info = null;
            string? debris_info = "";
            string? furniture_info = "";
            string? exit_info = "";
            string? npc_info = "";
            string? tile_properties_info = "";

            var back_layer_tile = Game1.currentLocation.Map?.GetLayer("Back")?.Tiles[xI, yI];
            if (back_layer_tile != null)
            {
                var tileIndex = back_layer_tile.TileIndex;
                var properties = back_layer_tile.TileSheet.TileIndexProperties[tileIndex];
                if (properties?.Count > 0)
                {
                    foreach(var kv in properties)
                    {
                        var value_p = kv.Value;
                        if (value_p?.ToString() == "T")
                        {
                            value_p = "True";
                        }
                        tile_properties_info = tile_properties_info + kv.Key +": " + value_p + "; ";
                    }
                }
            }
            if (Game1.currentLocation.objects.ContainsKey(key))
            {
                object_info = Game1.currentLocation.objects[key].BaseName;
            }
            if (Game1.currentLocation.terrainFeatures.ContainsKey(key))
            {
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
                crop_info = new CropInfoInSurroundings
                {
                    seed_id = crop.netSeedIndex.Value,
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

            foreach(Warp warp in Game1.currentLocation.warps.ToList())
            {
                if (warp.X == xI && warp.Y == yI){
                    exit_info = warp.TargetName;
                }
            }

            // npc info
            foreach(var npc in Game1.player.currentLocation.characters.ToList()){
                if (npc.TilePoint.X == xI && npc.TilePoint.Y == yI){
                    Game1.player.friendshipData.TryGetValue(npc.Name, out var friendshipData);
                    npc_info = "Name: " + npc.Name + " Friendship: " + Game1.player.getFriendshipLevelForNPC(npc.Name) + " isTalked: " + Game1.player.hasPlayerTalkedToNPC(npc.Name) + " GiftsToday: " + friendshipData?.GiftsToday ?? "None";
                    break;
                }
            }

            var tile_info = new TileInfo
            {
                position = position,
                object_at_tile = object_info,
                terrain_at_tile = terrain_info,
                building_info = builing_info,
                crop_at_tile = crop_info,
                debris_at_tile = debris_info,
                tile_properties = tile_properties_info,
                furniture_at_tile = furniture_info,
                exit_info = exit_info,
                npc_info = npc_info,
                placeable = Game1.currentLocation.isTilePlaceable(key)
            };
            
            return tile_info;
        }

        public static List<TileInfo> GetSurroundings(int size)
        {
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

            var tileInfoList = new List<TileInfo>();


            // for counters info
            // List<CounterInfo> counters = new List<CounterInfo>();
            // var counter_names = new Dictionary<string, Vector2>
            // {
            //     {"SeedShop Counter", new Vector2(4, 18) },
            //     {"AnimalShop Counter", new Vector2(12, 15) },
            //     {"JojaMart Counter", new Vector2(10, 25) },
            //     {"Blacksmith Counter", new Vector2(3, 14) },
            //     {"ScienceHouse Counter", new Vector2(8, 19) },
            //     {"FishShop Counter", new Vector2(5, 5) },
            // };
            
            for (int tileX = minX; tileX <= maxX; tileX++)
            {
                for (int tileY = minY; tileY <= maxY; tileY++)
                {
                    TileInfo tileInfo = GetTileInfo(tileX.ToString(), tileY.ToString());

                    // add counter info
                    if (Game1.currentLocation.Name == "SeedShop"){
                        if (tileX == 4 && tileY == 18){
                            tileInfo.furniture_at_tile = "SeedShop Counter";
                        }
                    }
                    else if (Game1.currentLocation.Name == "AnimalShop"){
                        if (tileX == 12 && tileY == 15){
                            tileInfo.furniture_at_tile = "AnimalShop Counter";
                        }
                    }
                    else if (Game1.currentLocation.Name == "JojaMart"){
                        if (tileX == 10 && tileY == 25){
                            tileInfo.furniture_at_tile = "JojaMart Counter";
                        }
                    }  
                    else if (Game1.currentLocation.Name == "Blacksmith"){
                        if (tileX == 3 && tileY == 14){
                            tileInfo.furniture_at_tile = "Blacksmith Counter";
                        }
                    }
                    else if (Game1.currentLocation.Name == "ScienceHouse"){
                        if (tileX == 8 && tileY == 19){ 
                            tileInfo.furniture_at_tile = "ScienceHouse Counter";
                        }
                    }
                    else if (Game1.currentLocation.Name == "FishShop"){
                        if (tileX == 5 && tileY == 5){
                            tileInfo.furniture_at_tile = "FishShop Counter";
                        }
                    }
                    tileInfoList.Add(tileInfo);
                }
            }
            return tileInfoList;
        }

        private static GameMetaData GetGameMetaData()
        {
            return new GameMetaData
            {
                ViewportSize = new[] { Game1.viewport.Width, Game1.viewport.Height }
            };
        }

        public class ChestItem
        {
            public string? Name { get; set; }
            public int Quantity { get; set; }
        }

        private static CurrentMenuData? GetCurrentMenuData()
        {
            var menu = Game1.activeClickableMenu;
            List<SaleItemInfo> shopMenuData = new List<SaleItemInfo>();
            if (menu is ShopMenu shopMenu)
            {
                foreach (StardewValley.Object obj in shopMenu.forSale.ToList())
                {
                    shopMenuData.Add(new SaleItemInfo()
                    {
                        name = obj.Name,
                        price = obj.salePrice()
                    });
                }

                return new CurrentMenuData
                {
                    type = "ShopMenu",
                    shopMenuData = shopMenuData
                };
            }
            else if (menu is PurchaseAnimalsMenu animalsMenu)
            {
                List<SaleItemInfo> animals = new List<SaleItemInfo>();
                foreach (var component in animalsMenu.animalsToPurchase.ToList())
                {
                    SaleItemInfo info = new SaleItemInfo()
                    {
                        name = component.item.Name,
                        price = component.item.salePrice()
                    };
                    animals.Add(info);
                }
                return new CurrentMenuData
                {
                    type = "PurchaseAnimalsMenu",
                    animalsMenuData = animals
                };
            }
            else if (menu is DialogueBox dialogueBox)
            {
                List<ResponseInfo> responseInfos = new List<ResponseInfo>();
                foreach (var response in dialogueBox.responses)
                {
                    var responseInfo = new ResponseInfo()
                    {
                        responseKey = response.responseKey,
                        responseText = response.responseText
                    };
                    responseInfos.Add(responseInfo);
                }
                return new CurrentMenuData
                {
                    type = "DialogueBox",
                    dialogues = dialogueBox.dialogues,
                    responses = responseInfos,
                    chats = dialogueBox.characterDialoguesBrokenUp.ToList()
                };
            }
            else if (menu is PrizeTicketMenu prizeTicketMenu)
            {
                var currentPrizeTrack = Global.mainMod?.Helper.Reflection.GetField<int>(prizeTicketMenu, "currentPrizeTrack").GetValue();
                return new CurrentMenuData
                {
                    type = "PrizeTicketMenu",
                    currentPrizeTrack = currentPrizeTrack
                };
            }
            else if (menu is CarpenterMenu carpenterMenu)
            {
                return new CurrentMenuData
                {
                    type = "CarpenterMenu",
                    bluePrints = carpenterMenu.Blueprints.ToString()
                };
            }
            else if (menu is ItemGrabMenu itemGrabMenu)
            {
                if (itemGrabMenu.context is Chest chest)
                {
                    var chestItems = chest.Items.ToList();
                    var itemsInfo = new List<ChestItem>();
                    foreach (var item in chestItems)
                    {
                        var name = item.BaseName;
                        var quantity = item.Stack;
                        itemsInfo.Add(new ChestItem
                        {
                            Name = name,
                            Quantity = quantity
                        });
                    }
                    return new CurrentMenuData
                    {
                        type = "Chest",
                        ItemsInChest = itemsInfo
                    };
                }
                else
                {
                    return null;
                }
            }
            else if (menu is LetterViewerMenu letterViewerMenu)
            {
                var message = letterViewerMenu.mailMessage.ToList()[0];
                return new CurrentMenuData
                {
                    type = "Letter",
                    message = message
                };
            }
            else
            {
                return null;
            }
        }

        public static void initPixelData()
        {
            pixelData = new byte[Game1.viewport.Width * Game1.viewport.Height * 4];
        }

        public static void initSampleRate(int rate)
        {
            sampleRate = rate;
        }

        public static void updatePixelData(Mod mod)
        {
            try
            {
                var random = new Random();
                var f = random.Next(0, 100);
                if (pixelData == null)
                {
                    mod.Monitor.Log("pixel data is empty");
                    return;
                }
                if (f >= sampleRate)
                {
                    return;
                }

                int width = Game1.viewport.Width;
                int height = Game1.viewport.Height;
                Game1.graphics.GraphicsDevice.SetRenderTarget(null);
                if (width != currentViewport[0] || height != currentViewport[1])
                {
                    pixelData = new byte[Game1.viewport.Width * Game1.viewport.Height * 4];
                    currentViewport[0] = Game1.viewport.Width;
                    currentViewport[1] = Game1.viewport.Height;
                }
                Game1.graphics.GraphicsDevice.GetBackBufferData(pixelData);
            }
            catch (Exception ex)
            {
                mod.Monitor.Log($"Error capturing pixelData: {ex.Message}", LogLevel.Error);
            }
        }
    }
}
