using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Characters;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Logging;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System.ComponentModel.Design;
using System.Reflection;
using System.Threading;
using xTile.Dimensions;
using xTile.Tiles;
using ActionSpace.actions;
using Netcode;
using StardewValley.Network;
using System.Xml.Linq;


namespace InitTask
{
    public static class InitTaskAPI
    {
        public static void set_base_stamina(string amount, Mod mod)
        {
            int amountI = int.Parse(amount);
            Game1.player.maxStamina.Value = amountI;
            mod.Monitor.Log($"OK, you now have {amount} base stamina.");
        }
        
        public static void set_stamina(string amount, Mod mod)
        {
            int amountI;
            if (amount == "None")
            {
                amountI = Game1.player.maxStamina.Value;
            }
            else
            {
                amountI = int.Parse(amount);
            }
            Game1.player.Stamina = amountI;
            mod.Monitor.Log($"OK, you now have {amountI} stamina.");
        }

        public static void set_base_health(string amount, Mod mod)
        {
            int amountI = int.Parse(amount);
            Game1.player.maxHealth = amountI;
            mod.Monitor.Log($"OK, you now have {amount} base health.");
        }

        public static void set_health(string amount, Mod mod)
        {
            int amountI;
            if (amount == "None")
            {
                amountI = Game1.player.maxHealth;
            }
            else
            {
                amountI = int.Parse(amount);
            }
            Game1.player.health = amountI;
            mod.Monitor.Log($"OK, you now have {amountI} health.");
        }

        public static void set_backpack_size(string size, Mod mod)
        {
            int sizeI = int.Parse(size);
            Game1.player.MaxItems = sizeI;
            mod.Monitor.Log($"OK, you now have {sizeI} backpack size.");
        }

        public static void clear_backpack(Mod mod)
        {
            Game1.player.clearBackpack();
            mod.Monitor.Log($"OK, you now have an empty backpack.");
        }

        public static void set_money(string amount, Mod mod)
        {
            int amountI = int.Parse(amount);
            Game1.player.Money = amountI;
            mod.Monitor.Log($"OK, you now have {Game1.player.Money} gold.");
        }

        public static void add_item_by_id(string id, string count, string quality, Mod mod)
        {
            int countI = int.Parse(count);
            int qualityI = int.Parse(quality);
            Item item = ItemRegistry.Create(id, countI, qualityI);
            Game1.playSound("coin");
            Game1.player.addItemByMenuIfNecessary(item);
            mod.Monitor.Log($"Added {item.DisplayName} ({item.QualifiedItemId})");
        }

        public static void add_item_by_name(string name,  string count, string quality, Mod mod)
        {
            int countI = int.Parse(count);
            int qualityI = int.Parse(quality);
            Item item = Utility.fuzzyItemSearch(name, countI);
            if (item == null)
            {
                mod.Monitor.Log("No item found with name '" + name + "'");
                return;
            }
            item.quality.Value = qualityI;
            MeleeWeapon.attemptAddRandomInnateEnchantment(item, null);
            Game1.player.addItemToInventory(item);
            Game1.playSound("coin");
            mod.Monitor.Log($"Added {item.DisplayName} ({item.QualifiedItemId})");
        }

        private static int remove_objects(GameLocation location, Func<StardewValley.Object, bool> shouldRemove)
        {
            int removed = 0;
            foreach ((Vector2 tile, StardewValley.Object? obj) in location.Objects.Pairs.ToArray())
            {
                if (shouldRemove(obj))
                {
                    location.Objects.Remove(tile);
                    removed++;
                }
            }
            return removed;
        }

        private static int remove_terrain_features(GameLocation location, Func<TerrainFeature, bool> shouldRemove)
        {
            int removed = 0;
            foreach ((Vector2 tile, TerrainFeature? feature) in location.terrainFeatures.Pairs.ToArray())
            {
                if (shouldRemove(feature))
                {
                    location.terrainFeatures.Remove(tile);
                    removed++;
                }
            }
            return removed;
        }

        private static int remove_large_terrain_features(GameLocation location, Func<LargeTerrainFeature, bool> shouldRemove)
        {
            int removed = 0;
            foreach (LargeTerrainFeature feature in location.largeTerrainFeatures.ToArray())
            {
                if (shouldRemove(feature))
                {
                    location.largeTerrainFeatures.Remove(feature);
                    removed++;
                }
            }
            return removed;
        }

        private static int remove_resource_clumps(GameLocation location, Func<ResourceClump, bool> shouldRemove)
        {
            int removed = 0;
            foreach (ResourceClump clump in location.resourceClumps.Where(shouldRemove).ToArray())
            {
                location.resourceClumps.Remove(clump);
                removed++;
            }
            return removed;
        }

        private static int remove_furniture(GameLocation location, Func<Furniture, bool> shouldRemove)
        {
            int removed = 0;
            foreach (Furniture furniture in location.furniture.ToArray())
            {
                if (shouldRemove(furniture))
                {
                    location.furniture.Remove(furniture);
                    removed++;
                }
            }
            return removed;
        }

        public static void world_clear(string entity, string location_name, Mod mod)
        {
            int[] DebrisClumps = { ResourceClump.stumpIndex, ResourceClump.hollowLogIndex, ResourceClump.meteoriteIndex, ResourceClump.boulderIndex };
            string[] ValidTypes = { "crops", "debris", "fruit-trees", "furniture", "grass", "trees", "removable", "everything" };
            GameLocation? location = Game1.locations.FirstOrDefault(p => p.Name != null && p.Name.Equals(location_name, StringComparison.OrdinalIgnoreCase));
            if (location == null && location_name == "current")
                location = Game1.currentLocation;
            if (location == null)
            {
                string[] location_names = (from loc in Game1.locations where !string.IsNullOrWhiteSpace(loc.Name) orderby loc.Name select loc.Name).ToArray();
                mod.Monitor.Log($"Could not find a location with that name. Must be one of [{string.Join(", ", location_names)}].");
                return;
            }

            switch (entity)
            {
                case "crops":
                    {
                        int removed =
                            remove_terrain_features(location, p => p is HoeDirt)
                            + remove_resource_clumps(location, p => p is GiantCrop);
                        mod.Monitor.Log($"Done! Removed {removed} entities from {location.Name}.");
                        break;
                    }
                case "debris":
                    {
                        int removed = 0;
                        foreach (var pair in location.terrainFeatures.Pairs.ToArray())
                        {
                            TerrainFeature feature = pair.Value;
                            if (feature is HoeDirt dirt && dirt.crop?.dead.Value is true)
                            {
                                dirt.crop = null;
                                removed++;
                            }
                        }
                        removed +=
                            remove_objects(location, obj =>
                                obj is not Chest
                                && (
                                    obj.Name is "Weeds" or "Stone"
                                    || obj.ParentSheetIndex is 294 or 295
                                )
                            )
                            + remove_resource_clumps(location, clump => DebrisClumps.Contains(clump.parentSheetIndex.Value));
                        mod.Monitor.Log($"Done! Removed {removed} entities from {location.Name}.");
                        break;
                    }
                case "fruit-trees":
                    {
                        int removed = remove_terrain_features(location, feature => feature is FruitTree);
                        mod.Monitor.Log($"Done! Removed {removed} entities from {location.Name}.");
                        break;
                    }
                case "furniture":
                    {
                        int removed = remove_furniture(location, _ => true);
                        mod.Monitor.Log($"Done! Removed {removed} entities from {location.Name}.");
                        break;
                    }
                case "grass":
                    {
                        int removed = remove_terrain_features(location, feature => feature is Grass);
                        mod.Monitor.Log($"Done! Removed {removed} entities from {location.Name}.");
                        break;
                    }
                case "trees":
                    {
                        int removed = remove_terrain_features(location, feature => feature is Tree);
                        mod.Monitor.Log($"Done! Removed {removed} entities from {location.Name}.");
                        break;
                    }
                case "removable":
                case "everything":
                    {
                        bool everything = entity == "everything";
                        int removed =
                            remove_furniture(location, _ => true)
                            + remove_objects(location, _ => true)
                            + remove_terrain_features(location, _ => true)
                            + remove_large_terrain_features(location, p => everything || p is not Bush bush || bush.isDestroyable())
                            + remove_resource_clumps(location, _ => true);
                        mod.Monitor.Log($"Done! Removed {removed} entities from {location.Name}.");
                        break;
                    }
                default:
                    mod.Monitor.Log($"Unknown type '{entity}'. Must be one [{string.Join(", ", ValidTypes)}].", LogLevel.Error);
                    break;
            }
        }

        public static void place_item(string item, string type, string x, string y, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int xI, yI;
            if(x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            if (location.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt { crop: null })
            {
                location.terrainFeatures.Remove(tile);
            }
            if (location.isTilePassable(tile) && !location.IsTileOccupiedBy(tile, ~(CollisionMask.Characters | CollisionMask.Farmers | CollisionMask.TerrainFeatures)))
            {
                StardewValley.Object i = ItemRegistry.Create<StardewValley.Object>(item);
                if(type == "forage")
                {
                    i.IsSpawnedObject = true;
                }
                location.objects.Add(tile, i);
                mod.Monitor.Log($"Spawned {i.DisplayName} on the tile ({xI}, {yI})");
            }
            else
            {
                mod.Monitor.Log($"Sorry, the tile ({xI}, {yI}) is occupied.");
                return;
            }
        }

        public static void set_terrain(string terrain, string id, string x, string y, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            if (location.doesTileHaveProperty(xI, yI, "Diggable", "Back") != null && location.CanItemBePlacedHere(tile, itemIsPassable: true, CollisionMask.All, CollisionMask.None))
            {
                if (terrain == "dirt")
                {
                    location.terrainFeatures.Add(tile, new HoeDirt());
                }
                else if(terrain == "grass")
                {
                    location.terrainFeatures.Add(tile, new Grass(int.Parse(id), 4));
                }
                else if(terrain == "tree")
                {
                    location.terrainFeatures.Add(tile, new Tree(id));
                }
                else if(terrain == "fruittree")
                {
                    location.terrainFeatures.Add(tile, new FruitTree(id));
                }
                mod.Monitor.Log($"Spawned {terrain} on the tile ({xI}, {yI}).");
            }
            else
            {
                mod.Monitor.Log($"Sorry, the tile ({xI}, {yI}) is not diggable, or it is occupied.");
            }
        }

        public static void place_crop(string crop, string x, string y, Mod mod)
        {
            set_terrain("dirt", "", x, y, mod);
            GameLocation location = Game1.player.currentLocation;
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            if (location.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt dirt && dirt.crop == null)
            {
                dirt.crop = new Crop(crop, xI, yI, dirt.Location);
                mod.Monitor.Log($"Spawned the crop on the tile ({xI}, {yI}).");
            }
        }

        public static void grow_crop(string day, string x, string y, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int dayI = int.Parse(day);
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            if (location.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt dirt && dirt.crop != null)
            {
                for (int i = 0; i < dayI; i++)
                    dirt.crop.newDay(HoeDirt.watered);
                if (dirt.crop.currentPhase.Value == dirt.crop.phaseDays.Count - 1)
                {
                    mod.Monitor.Log($"Crop on the tile ({xI}, {yI}) is fully grown.");
                }
                else
                {
                    mod.Monitor.Log($"Crop on the tile ({xI}, {yI}) is grown for {dayI} day(s).");
                }
            }
            else
            {
                mod.Monitor.Log($"Sorry, there is no crop on the tile ({xI}, {yI}).");
            }
        }

        public static void grow_tree(string day, string x, string y, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int dayI = int.Parse(day);
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            if (location.terrainFeatures.TryGetValue(tile, out var feature))
            {
                if (feature is Tree tree)
                {
                    for (int i = 0; i < dayI; i++)
                        tree.dayUpdate();
                    mod.Monitor.Log("Done!");
                }
                else if(feature is FruitTree fruittree)
                {
                    for (int i = 0; i < dayI; i++)
                        fruittree.dayUpdate();
                    mod.Monitor.Log("Done!");
                }
                else
                {
                    mod.Monitor.Log($"Sorry, there is no tree on the tile ({xI}, {yI}).");
                }
            }
            else
            {
                mod.Monitor.Log($"Sorry, there is no tree on the tile ({xI}, {yI}).");
            }
        }

        public static void build(string type, string force, string x, string y, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            bool forceBuild = (force == "True" ? true : false);
            if (!Game1.buildingData.ContainsKey(type))
            {
                type = Game1.buildingData.Keys.FirstOrDefault((string key) => type.EqualsIgnoreCase(key)) ?? type;
            }
            Building constructed;
            if (!Game1.buildingData.ContainsKey(type))
            {
                string[] matches = Utility.fuzzySearchAll(type, Game1.buildingData.Keys, sortByScore: false).ToArray();
                mod.Monitor.Log((matches.Length == 0) ? ("There's no building with type '" + type + "'.") : ("There's no building with type '" + type + "'. Did you mean one of these?\n- " + string.Join("\n- ", matches)));
            }
            else if (!Game1.currentLocation.buildStructure(type, tile, Game1.player, out constructed, magicalConstruction: false, forceBuild))
            {
                mod.Monitor.Log($"Couldn't place a '{type}' building at position ({xI}, {yI}).");
            }
            else
            {
                constructed.daysOfConstructionLeft.Value = 0;
                mod.Monitor.Log($"Placed '{type}' at position ({xI}, {yI}).");
            }
        }

        public static void move_building(string x_source, string y_source, string x_dest, string y_dest, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int xI_source, yI_source, xI_dest, yI_dest;
            if (x_source == "None" && y_source == "None")
            {
                xI_source = Game1.player.TilePoint.X + 1;
                yI_source = Game1.player.TilePoint.Y;
            }
            else
            {
                xI_source = int.Parse(x_source);
                yI_source = int.Parse(y_source);
            }
            if (x_dest == "None" && y_dest == "None")
            {
                xI_dest = Game1.player.TilePoint.X + 1;
                yI_dest = Game1.player.TilePoint.Y;
            }
            else
            {
                xI_dest = int.Parse(x_dest);
                yI_dest = int.Parse(y_dest);
            }
            Vector2 tile = new(xI_source, yI_source);
            Building building = location.getBuildingAt(tile);
            if (building != null)
            {
                building.tileX.Value = xI_dest;
                building.tileY.Value = yI_dest;
                mod.Monitor.Log($"Building on the tile ({xI_source}, {yI_source}) is moved to ({xI_dest}, {yI_dest}).");
            }
            else
            {
                mod.Monitor.Log($"Sorry, there is no building on the tile ({xI_source}, {yI_source}).");
            }
        }

        public static void remove_building(string x, string y, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            Building building = location.getBuildingAt(tile);
            if (building != null)
            {
                Game1.currentLocation.buildings.Remove(building);
                mod.Monitor.Log($"Building on the tile ({xI}, {yI}) is removed.");
            }
            else
            {
                mod.Monitor.Log($"Sorry, there is no building on the tile ({xI}, {yI}).");
            }
        }

        public static void spawn_pet(string type, string breed, string name, string x, string y, Mod mod)
        {

            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Pet pet = (type == "cat" ? new Pet(xI, yI, breed, "Cat") : new Pet(xI, yI, breed, "Dog"));
            Game1.currentLocation.characters.Add(pet);
            if (name != "None")
            {
                pet.Name = name;
                mod.Monitor.Log($"Spawned a {type} called {name} on the tile ({xI}, {yI}).");
            }
            else
            {
                mod.Monitor.Log($"Spawned a {type} on the tile ({xI}, {yI}).");
            }
        }

        public static void build_stable(string x, string y, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            Stable stable = new Stable(tile);
            Game1.currentLocation.buildings.Add(stable);
            stable.dayUpdate(0);
            mod.Monitor.Log($"Spawned a stable on the tile ({xI}, {yI}).");
        }

        public static void instant_build(string x, string y, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            Building building = location.getBuildingAt(tile);
            if (building != null)
            {
                if (building.daysOfConstructionLeft.Value > 0)
                {
                    building.dayUpdate(0);
                    mod.Monitor.Log($"The construction on the tile ({xI}, {yI}) is completed.");
                    return;
                }
                if (building.daysUntilUpgrade.Value > 0)
                {
                    building.dayUpdate(0);
                    mod.Monitor.Log($"The construction on the tile ({xI}, {yI}) is completed.");
                    return;
                }
            }
            mod.Monitor.Log($"Sorry, there is no building under construction on the tile ({xI}, {yI}).");
        }

        public static void spawn_animal(string type, string name, Mod mod)
        {

            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }

            var multiplayerField = typeof(Game1).GetField("multiplayer", BindingFlags.NonPublic | BindingFlags.Static);
            var multiplayerInstance = multiplayerField.GetValue(null);
            var newID = (long)multiplayerInstance.GetType().GetMethod("getNewID").Invoke(multiplayerInstance, null);

            FarmAnimal animal = new FarmAnimal(type.Trim(), newID, Game1.player.UniqueMultiplayerID);
            Utility.addAnimalToFarm(animal);
            if (name != "None")
            {
                animal.Name = name;
                mod.Monitor.Log($"Spawned a {type} called {name}.");
            }
            else
            {
                mod.Monitor.Log($"Spawned a {type}.");
            }
        }

        public static void grow_animal(string name, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            if (name != "None")
            {
                FarmAnimal animal = Utility.fuzzyAnimalSearch(name);
                if (animal == null)
                {
                    mod.Monitor.Log("Couldn't find character named " + name);
                    return;
                }
                animal.growFully();
                mod.Monitor.Log($"Grow {name}.");
            }
            else
            {
                int count = 0;
                foreach (FarmAnimal value in Game1.currentLocation.animals.Values)
                {
                    value.growFully();
                    count++;
                }
                mod.Monitor.Log($"Grow {count} animals.");
            }
        }

        public static void animal_friendship(string name, string friendship, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int friendshipI;
            if(friendship != "None")
            {
                friendshipI = int.Parse(friendship);
            }
            else
            {
                friendshipI = 1000;
            }
            if(name != "None")
            {
                FarmAnimal animal = Utility.fuzzyAnimalSearch(name);
                if (animal == null)
                {
                    mod.Monitor.Log("Couldn't find character named " + name);
                    return;
                }
                animal.friendshipTowardFarmer.Value = friendshipI;
            }
            else
            {
                foreach (FarmAnimal value in Game1.currentLocation.animals.Values)
                {
                    value.friendshipTowardFarmer.Value = friendshipI;
                }

            }
            mod.Monitor.Log($"Done!");
        }

        public static void warp(string location_name, string x, string y, Mod mod)
        {
            GameLocation location = Utility.fuzzyLocationSearch(location_name);
            if (location == null)
            {
                mod.Monitor.Log("No location with name " + location_name);
                return;
            }
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = 0;
                yI = 0;
                Utility.getDefaultWarpLocation(location.Name, ref xI, ref yI);
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Game1.warpFarmer(new LocationRequest(location.NameOrUniqueName, location.uniqueName.Value != null, location), xI, yI, 2);
            mod.Monitor.Log($"Warping Game1.player to {location.NameOrUniqueName} at ({xI}, {yI}).");
        }

        public static void warp_mine(string level, Mod mod)
        {
            int levelI = int.Parse(level);
            Game1.enterMine(levelI, null);
            if(levelI == 77377)
                mod.Monitor.Log($"Warping Game1.player to quarry mine.");
            else if (levelI > 120)
                mod.Monitor.Log($"Warping Game1.player to skull cavern, level {levelI - 120}.");
            else
                mod.Monitor.Log($"Warping Game1.player to mine, level {levelI}.");
        }

        public static void warp_volcano (string level, Mod mod)
        {
            int levelI = int.Parse(level);
            Game1.warpFarmer(VolcanoDungeon.GetLevelName(levelI), 0, 1, 2);
            mod.Monitor.Log($"Warping Game1.player to volcano dungeon, level {levelI}.");
        }

        public static void warp_home (Mod mod)
        {
            Game1.warpHome();
            mod.Monitor.Log($"Warping Game1.player back home.");
        }

        public static void warp_shop(string npc, Mod mod)
        {
            switch (npc.ToLower())
            {
                case "pierre":
                    Game1.game1.parseDebugInput("Warp SeedShop 4 19");
                    Game1.game1.parseDebugInput("WarpCharacterTo Pierre SeedShop 4 17");
                    break;
                case "robin":
                    Game1.game1.parseDebugInput("Warp ScienceHouse 8 20");
                    Game1.game1.parseDebugInput("WarpCharacterTo Robin ScienceHouse 8 18");
                    break;
                case "krobus":
                    Game1.game1.parseDebugInput("Warp Sewer 31 19");
                    break;
                case "sandy":
                    Game1.game1.parseDebugInput("Warp SandyHouse 2 7");
                    Game1.game1.parseDebugInput("WarpCharacterTo Sandy SandyHouse 2 5");
                    break;
                case "marnie":
                    Game1.game1.parseDebugInput("Warp AnimalShop 12 16");
                    Game1.game1.parseDebugInput("WarpCharacterTo Marnie AnimalShop 12 14");
                    break;
                case "clint":
                    Game1.game1.parseDebugInput("Warp Blacksmith 3 15");
                    Game1.game1.parseDebugInput("WarpCharacterTo Clint Blacksmith 3 13");
                    break;
                case "gus":
                    Game1.game1.parseDebugInput("Warp Saloon 10 20");
                    Game1.game1.parseDebugInput("WarpCharacterTo Gus Saloon 10 18");
                    break;
                case "willy":
                    Game1.game1.parseDebugInput("Warp FishShop 6 6");
                    Game1.game1.parseDebugInput("WarpCharacterTo Willy FishShop 6 4");
                    break;
                case "pam":
                    Game1.game1.parseDebugInput("Warp BusStop 7 12");
                    Game1.game1.parseDebugInput("WarpCharacterTo Pam BusStop 11 10");
                    break;
                case "dwarf":
                    Game1.game1.parseDebugInput("Warp Mine 43 7");
                    break;
                case "wizard":
                    Game1.player.eventsSeen.Add("418172");
                    Game1.player.hasMagicInk = true;
                    Game1.game1.parseDebugInput("Warp WizardHouse 2 14");
                    break;
                default:
                    mod.Monitor.Log("That npc doesn't have a shop or it isn't handled by this command");
                    break;
            }
        }

        public static void warp_character(string npc, string location_name, string x, string y, Mod mod)
        {
            NPC n = Utility.fuzzyCharacterSearch(npc);
            if (n == null)
            {
                mod.Monitor.Log("Could not find valid character " + npc);
                return;
            }

            GameLocation location = Game1.currentLocation;
            if (location_name != "None")
            {
                location = Utility.fuzzyLocationSearch(location_name);
                if (location == null)
                {
                    mod.Monitor.Log("No location with name " + location_name);
                    return;
                }
            }

            int xI, yI;
            if (x == "None" && y == "None")
            {
                if (location_name == "None")
                {
                    mod.Monitor.Log("Warping " + n.displayName);
                    Game1.warpCharacter(n, Game1.currentLocation.Name, new Vector2(Game1.player.TilePoint.X+1, Game1.player.TilePoint.Y));
                    n.controller = null;
                    n.Halt();
                    return;
                }
                else
                {
                    xI = 0;
                    yI = 0;
                    Utility.getDefaultWarpLocation(location.Name, ref xI, ref yI);
                }
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }

            Vector2 tile = new(xI, yI);
            Game1.warpCharacter(n, location.Name, tile);
            n.controller = null;
            n.Halt();
        }

        public static void remove_item(string x, string y, Mod mod)
        {
            GameLocation location = Game1.player.currentLocation;
            if (location == null)
            {
                mod.Monitor.Log($"You must be in a location to use this command.");
                return;
            }
            int xI, yI;
            if (x == "None" && y == "None")
            {
                xI = Game1.player.TilePoint.X + 1;
                yI = Game1.player.TilePoint.Y;
            }
            else
            {
                xI = int.Parse(x);
                yI = int.Parse(y);
            }
            Vector2 tile = new(xI, yI);
            location.objects.Remove(tile);
            mod.Monitor.Log($"Remove the item on the tile ({xI}, {yI}).");
        }

        public static void set_date(string year, string season, string day, Mod mod)
        {
            if (year != "None")
            {
                Game1.year = int.Parse(year);
            }
            switch (season)
            {
                case "spring":
                    Game1.season = Season.Spring;
                    Game1.setGraphicsForSeason();
                    break;
                case "summer":
                    Game1.season = Season.Summer;
                    Game1.setGraphicsForSeason();
                    break;
                case "fall" or "autumn":
                    Game1.season = Season.Fall;
                    Game1.setGraphicsForSeason();
                    break;
                case "winter":
                    Game1.season = Season.Winter;
                    Game1.setGraphicsForSeason();
                    break;
            }
            if(day != "None")
            {
                int dayI = int.Parse(day);
                Game1.stats.DaysPlayed = (uint)(Game1.seasonIndex * 28 + dayI + (Game1.year - 1) * 4 * 28);
                Game1.dayOfMonth = dayI;
            }
            mod.Monitor.Log("Done!");
        }

        public static void set_time(string time, Mod mod)
        {
            if (time != "None")
            {
                Game1.timeOfDay = int.Parse(time);
                Game1.outdoorLight = Color.White;
            }
        }

        public static void character_tile(Mod mod)
        {
            mod.Monitor.Log($"Player tile position is {Game1.player.Tile} (World position: {Game1.player.Position})");
        }

        public static void lookup(string name, Mod mod)
        {
            Item item = Utility.fuzzyItemSearch(name);
            if (item != null)
            {
                mod.Monitor.Log(item.DisplayName + "'s qualified ID is " + item.QualifiedItemId);
            }
            else
            {
                mod.Monitor.Log("No item found with name " + name);
            }
        }

        public static void set_deepest_mine_level(string level, Mod mod)
        {
            int levelI = int.Parse(level);
            MineShaft.lowestLevelReached = levelI;
            Game1.player.deepestMineLevel = levelI;
            mod.Monitor.Log($"The deepest mine level that the player reached is changed to {levelI}.");
        }

        public static void set_monster_stat(string monster, string kills, Mod mod)
        {
            int killsI = int.Parse(kills);
            Game1.stats.specificMonstersKilled[monster] = killsI;
            mod.Monitor.Log(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3159", monster, killsI));
        }

        public static int get_monster_kills(string monster,Mod mod)
        {
            if (Game1.stats.specificMonstersKilled.Keys.Contains(monster)){
                int killsI = Game1.stats.specificMonstersKilled[monster];
                return killsI;
            }
            else
            {
                return 0;
            }
        }

        public static void complete_quest(string id, Mod mod)
        {
            Game1.player.completeQuest(id);
            mod.Monitor.Log("Done!");
        }

        public static void add_recipe(string type, string recipe, Mod mod)
        {
            if (type == "crafting")
            {
                if (recipe == "None")
                {
                    foreach (string s in CraftingRecipe.craftingRecipes.Keys)
                    {
                        Game1.player.craftingRecipes.TryAdd(s, 0);
                    }
                    mod.Monitor.Log("Add all crafting recipes to the player.");
                }
                else
                {
                    Game1.player.craftingRecipes.TryAdd(recipe.Trim(), 0);
                    mod.Monitor.Log($"Add the crafting recipe {recipe.Trim()} to the player.");
                }
            }
            else
            {
                if(recipe == "None")
                {
                    foreach (string s in CraftingRecipe.cookingRecipes.Keys)
                    {
                        Game1.player.cookingRecipes.TryAdd(s, 0);
                    }
                    mod.Monitor.Log("Add all cooking recipes to the player.");
                }
                else
                {
                    Game1.player.cookingRecipes.Add(recipe.Trim(), 0);
                    mod.Monitor.Log($"Add the cooking recipe {recipe.Trim()} to the player.");
                }
            }
        }

        public static void upgrade_house(string level, Mod mod)
        {
            int levelI = int.Parse(level);
            Utility.getHomeOfFarmer(Game1.player).moveObjectsForHouseUpgrade(levelI);
            Utility.getHomeOfFarmer(Game1.player).setMapForUpgradeLevel(levelI);
            Game1.player.HouseUpgradeLevel = levelI;
            Game1.addNewFarmBuildingMaps();
            Utility.getHomeOfFarmer(Game1.player).ReadWallpaperAndFloorTileData();
            Utility.getHomeOfFarmer(Game1.player).RefreshFloorObjectNeighbors();
            mod.Monitor.Log($"Upgrade the farmhouse to level {levelI}.");
        }

        public static void spawn_junimo_note (string id, Mod mod)
        {
            CommunityCenter ccc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
            if (id == "None")
            {
                for (int i = 0; i < ccc.areasComplete.Count; i++)
                {
                    Game1.RequireLocation<CommunityCenter>("CommunityCenter").addJunimoNote(i);
                }
                mod.Monitor.Log($"Spawn junimo notes in all rooms of community center.");
            }
            else
            {
                var rooms = new Dictionary<int, string>
                {
                    { 0, "Pantry" },
                    { 1, "Crafts Room" },
                    { 2, "Fish Tank" },
                    { 3, "Boiler Room" },
                    { 4, "Vault" },
                    { 5, "Bulletin Board" },
                };
                Game1.RequireLocation<CommunityCenter>("CommunityCenter").addJunimoNote(int.Parse(id));
                mod.Monitor.Log($"Spawn a junimo note in {rooms[int.Parse(id)]}.");
            }
        }
        public static void complete_room_bundle(string id, Mod mod)
        {
            CommunityCenter ccc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
            var rooms = new Dictionary<int, string>
            {
                { 0, "ccPantry" },
                { 1, "ccCraftsRoom" },
                { 2, "ccFishTank" },
                { 3, "ccBoilerRoom" },
                { 4, "ccVault" },
                { 5, "ccBulletin" },
            };
            if (id == "None")
            {
                for (int i = 0; i < ccc.areasComplete.Count; i++)
                {
                    Game1.player.mailReceived.Add(rooms[i]);
                    ccc.markAreaAsComplete(i);
                    ccc.areasComplete[i] = true;
                }
                mod.Monitor.Log($"Complete all the collection bundles in community center.");
            }
            else
            {
                int idI = int.Parse(id);
                Game1.player.mailReceived.Add(rooms[idI]);
                ccc.markAreaAsComplete(idI);
                ccc.areasComplete[idI] = true;
                mod.Monitor.Log($"Complete the collection bundles of {rooms[idI]}.");
            }
        }

        public static void joja_membership(Mod mod)
        {
            Game1.player.mailReceived.Add("JojaMember");
            mod.Monitor.Log($"Purchase the JojaMart membership.");
        }

        public static void community_development(string id, Mod mod)
        {
            CommunityCenter ccc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
            var projects = new Dictionary<int, string>
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
            if (id == "None")
            {
                for (int i = 0; i < 5; i++)
                {
                    Game1.player.mailReceived.Add(projects[i]);
                    Game1.player.mailReceived.Add(projects[i + 5]);
                }
                mod.Monitor.Log($"Complete all the community development projects.");
            }
            else
            {
                int idI = int.Parse(id);
                Game1.player.mailReceived.Add(projects[idI]);
                Game1.player.mailReceived.Add(projects[idI + 5]);
                mod.Monitor.Log($"Purchase the community development project of {projects[idI]}.");
            }
        }

        public static void set_max_luck(Mod mod)
        {
            Game1.player.luckLevel.Set(10);
            Game1.player.hasSpecialCharm = true;
            Game1.player.team.sharedDailyLuck.Value = 1d;
            mod.Monitor.Log("Done!");
        }

        public static void print_luck(Mod mod)
        {
            mod.Monitor.Log($"luck_level: {Game1.player.luckLevel}");
            mod.Monitor.Log($"daily_luck: {Game1.player.DailyLuck}");
        }

        public static void receive_mail(string mail, Mod mod)
        {
            Game1.addMail(mail, noLetter: false, sendToEveryone: true);
            mod.Monitor.Log("Done!");
        }

        public static void trigger_event(string id, Mod mod)
        {
            Game1.player.eventsSeen.Remove(id);
            Game1.eventsSeenSinceLastLocationChange.Remove(id);
            if (Game1.PlayEvent(id, checkPreconditions: false, checkSeen: false))
            {
                mod.Monitor.Log("Starting event " + id);
            }
            else
            {
                mod.Monitor.Log("Event '" + id + "' not found.");
            }
        }

        public static void seen_event(string id, string see_or_forget, Mod mod)
        {
            bool seen = true;
            if (see_or_forget == "False")
                seen = false;
            Game1.player.eventsSeen.Toggle(id, seen);
            if (!seen)
            {
                Game1.eventsSeenSinceLastLocationChange.Remove(id);
            }
        }

        public static void mark_bundle(string id, Mod mod)
        {
            int idI = int.Parse(id);
            foreach (KeyValuePair<int, NetArray<bool, NetBool>> b in Game1.RequireLocation<CommunityCenter>("CommunityCenter").bundles.FieldDict)
            {
                if (b.Key == idI)
                {
                    for (int j = 0; j < b.Value.Count; j++)
                    {
                        b.Value[j] = true;
                    }
                }
            }
            Game1.playSound("crystal", 0);
        }

        public static void start_quest(string id, Mod mod)
        {
            Game1.player.addQuest(id);
            mod.Monitor.Log("Done!");
        }

        public static void npc_friendship(string npc, string value, Mod mod)
        {
            NPC n = Utility.fuzzyCharacterSearch(npc);
            if (n == null)
            {
                mod.Monitor.Log("No character found matching '" + npc + "'.");
                return;
            }
            if (!Game1.player.friendshipData.TryGetValue(n.Name, out var friendship))
            {
                friendship = (Game1.player.friendshipData[n.Name] = new Friendship());
            }
            friendship.Points = int.Parse(value);
            mod.Monitor.Log("Done");
        }

        public static void all_npc_friendship(string value, Mod mod)
        {
            if (Game1.year == 1)
            {
                Game1.AddCharacterIfNecessary("Kent", bypassConditions: true);
                Game1.AddCharacterIfNecessary("Leo", bypassConditions: true);
            }
            Utility.ForEachVillager(delegate (NPC n)
            {
                if (!n.CanSocialize && n.Name != "Sandy" && n.Name == "Krobus")
                {
                    return true;
                }
                if (n.Name == "Marlon")
                {
                    return true;
                }
                if (!Game1.player.friendshipData.ContainsKey(n.Name))
                {
                    Game1.player.friendshipData.Add(n.Name, new Friendship());
                }
                Game1.player.changeFriendship(int.Parse(value), n);
                return true;
            });
        }

        public static void dating(string npc, Mod mod)
        {
            NPC n = Utility.fuzzyCharacterSearch(npc);
            if (n == null)
            {
                mod.Monitor.Log("No character found matching '" + npc + "'.");
                return;
            }
            if (!Game1.player.friendshipData.TryGetValue(n.Name, out var friendship))
            {
                friendship = (Game1.player.friendshipData[n.Name] = new Friendship());
            }
            friendship.Status = FriendshipStatus.Dating;
            mod.Monitor.Log("Done");
        }

        public static void rain(Mod mod)
        {
            string contextId = Game1.player.currentLocation.GetLocationContextId();
            LocationWeather weather = Game1.netWorldState.Value.GetWeatherForLocation(contextId);
            weather.IsRaining = !weather.IsRaining;
            weather.IsDebrisWeather = false;
            if (contextId == "Default")
            {
                Game1.isRaining = weather.IsRaining;
                Game1.isDebrisWeather = false;
            }
        }

        public static void start_help_quest(string type, Mod mod)
        {
            if (type == "collect")
                Game1.player.questLog.Add(new ResourceCollectionQuest());
            else if (type == "delivery")
                Game1.player.questLog.Add(new ItemDeliveryQuest());
            else if (type == "slay")
            {
                Game1.player.questLog.Add(new SlayMonsterQuest
                {
                    ignoreFarmMonsters = { true }
                });
            }
        }
    }
}
