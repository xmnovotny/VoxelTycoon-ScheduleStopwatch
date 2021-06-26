using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VoxelTycoon;
using VoxelTycoon.Buildings;
using VoxelTycoon.Recipes;
using VoxelTycoon.Researches;
using static ScheduleStopwatch.VehicleScheduleCapacity;
using XMNUtils;

namespace ScheduleStopwatch
{
    class RecipeHelper : LazyManager<RecipeHelper>
    {
        protected override void OnInitialize()
        {
            base.OnInitialize();
            LazyManager<ResearchManager>.Current.ResearchCompleted += OnResearchCompleted;
        }

        private void OnResearchCompleted(Research research)
        {
            _minedItemsPerMonth = null;
        }

        private (List<RecipeItem> rawItems, Dictionary<Item, float> subItems, Dictionary<Recipe, float> recipes) FindIngredients(Item item, float? coeficient = null)
        {
            List<RecipeItem> cacheResultList;
            Dictionary<Item, float> cacheResultSubItemList;
            Dictionary<Recipe, float> cacheResultRecipes;
            if (!this._itemToIngredients.TryGetValue(item, out cacheResultList))
            {
                Dictionary<Item, RecipeItem> dictItems = new Dictionary<Item, RecipeItem>();
                cacheResultList = (this._itemToIngredients[item] = new List<RecipeItem>());
                cacheResultSubItemList = this._itemToSubitems[item] = new Dictionary<Item, float>();
                cacheResultRecipes = this._itemToSubRecipes[item] = new Dictionary<Recipe, float>();
                (Recipe recipe, float outputCount) = GetRecipe(item);
                if (recipe != null)
                {
                    float localCoeficient = 1 / outputCount;
                    AddRecipeToDict(cacheResultRecipes, recipe, localCoeficient);
                    foreach (RecipeItem recipeItem in recipe.InputItems)
                    {
                        (List<RecipeItem> ingredients, Dictionary<Item, float> subItems, Dictionary<Recipe, float> subRecipes) = this.FindIngredients(recipeItem.Item, localCoeficient * recipeItem.Count);
                        if (ingredients.Count > 0)
                        {
                            AddRecipeItemToDict(cacheResultSubItemList, recipeItem.Item, localCoeficient * recipeItem.Count);
                            foreach (RecipeItem ingredient in ingredients)
                            {
                                AddRecipeItemToDict(dictItems, ingredient.Item, ingredient.Count);
                            }
                            foreach (KeyValuePair<Item, float> subItem in subItems)
                            {
                                AddRecipeItemToDict(cacheResultSubItemList, subItem.Key, subItem.Value);
                            }
                            foreach (KeyValuePair<Recipe, float> pair in subRecipes)
                            {
                                AddRecipeToDict(cacheResultRecipes, pair.Key, pair.Value);
                            }
                        } else
                        {
                            //add item as ingredient
                            AddRecipeItemToDict(dictItems, recipeItem.Item, localCoeficient * recipeItem.Count);
                        }
                    }
                    cacheResultList.AddRange(dictItems.Values);
                }
            } else
            {
                cacheResultSubItemList = _itemToSubitems[item];
                cacheResultRecipes = _itemToSubRecipes[item];
            }
            if (coeficient != null && coeficient.Value != 1)
            {
                List<RecipeItem> resultList = new List<RecipeItem>();
                Dictionary<Item, float> resultSubItemList = new Dictionary<Item, float>();
                Dictionary<Recipe, float> resultRecipes = new Dictionary<Recipe, float>();
                foreach (RecipeItem rawItem in cacheResultList)
                {
                    RecipeItem newItem = new RecipeItem();
                    newItem.Count = rawItem.Count * coeficient.Value;
                    newItem.Item = rawItem.Item;
                    resultList.Add(newItem);
                }
                foreach (KeyValuePair<Item, float> subItem in cacheResultSubItemList) {
                    AddRecipeItemToDict(resultSubItemList, subItem.Key, subItem.Value * coeficient.Value);
                }
                foreach (KeyValuePair<Recipe, float> recipe in cacheResultRecipes)
                {
                    AddRecipeToDict(resultRecipes, recipe.Key, recipe.Value * coeficient.Value);
                }
                return (resultList, resultSubItemList, resultRecipes);
            }
            return (cacheResultList, cacheResultSubItemList, cacheResultRecipes);
        }

        public static void AddItem(Dictionary<Item, int> items, Item item, int count)
        {
            if (items.TryGetValue(item, out int oldCount))
            {
                items[item] = oldCount + count;
            }
            else
            {
                items.Add(item, count);
            }
        }
        public static void AddItems(Dictionary<Item, int> items, IReadOnlyDictionary<Item, TransferData> itemsToAdd, TransferDirection direction)
        {
            foreach (KeyValuePair<Item, TransferData> pair in itemsToAdd)
            {
                int count = pair.Value.Get(direction);
                if (count != 0)
                {
                    AddItem(items, pair.Key, count);
                }
            }
        }
        public static void AddItems(Dictionary<Item, int> items, IReadOnlyDictionary<Item, int> itemsToAdd)
        {
            foreach (KeyValuePair<Item, int> pair in itemsToAdd)
            {
                AddItem(items, pair.Key, pair.Value);
            }
        }

        public ImmutableList<RecipeItem> GetIngredients(Item item)
        {
            (List<RecipeItem> ingredients, _, _) = FindIngredients(item);
            return ingredients.ToImmutableList<RecipeItem>();
        }

        public List<RecipeItem> GetIngredients(Item item, List<Item> finalItems, float? multiplier = null, Dictionary<Recipe, float> recipesNeeded = null)
        {
            (List<RecipeItem> ingredients, Dictionary<Item, float> subItems, Dictionary<Recipe, float> recipes) = FindIngredients(item);
            Dictionary<Item, float> tmpDict = new Dictionary<Item, float>();
            foreach (RecipeItem ingrItem in ingredients)
            {
                tmpDict.Add(ingrItem.Item, ingrItem.Count);
            }
            Dictionary<Item, float> itemsToProcess = new Dictionary<Item, float>();
            foreach (Item finalItem in finalItems)
            {
                if (subItems.TryGetValue(finalItem, out float count)) {
                    itemsToProcess.Add(finalItem, count);
                }
            }
            Dictionary<Recipe, float> tmpRecipes = null;
            if (recipesNeeded != null)
            {
                tmpRecipes = new Dictionary<Recipe, float>();
                foreach (KeyValuePair<Recipe, float> recipe in recipes)
                {
                    tmpRecipes[recipe.Key] = recipe.Value;
                }
            }

            //subtract ingredients of finalItems
            bool finished = itemsToProcess.Count == 0;
            while (!finished)
            {
                finished = true;
                foreach (KeyValuePair<Item, float> pair in itemsToProcess.ToArray<KeyValuePair<Item, float>>())
                {
                    float value = itemsToProcess[pair.Key];
                    if (value != 0)
                    {
                        //subtract all ingredients of final item and add final item to result ingredients
                        tmpDict.AddFloatToDict(pair.Key, value);
                        itemsToProcess[pair.Key] = 0;
                        (List<RecipeItem> subIngredients, Dictionary<Item, float> subItems2, Dictionary<Recipe, float> subRecipes) = FindIngredients(pair.Key);
                        foreach (RecipeItem ingrItem in subIngredients)
                        {
                            tmpDict.AddFloatToDict(ingrItem.Item, -ingrItem.Count * value);
                        }
                        if (tmpRecipes != null)
                        {
                            foreach (KeyValuePair<Recipe, float> subRecipe in subRecipes)
                            {
                                tmpRecipes.AddFloatToDict(subRecipe.Key, -subRecipe.Value * value);
                            }
                        }
                        if (subItems2.Count>0)
                        {
                            //subtract subitems of final item from final items count
                            foreach (Item item2 in itemsToProcess.Keys.ToArray())
                            {
                                finished = false;
                                if (subItems2.TryGetValue(item2, out float subItemCount))
                                {
                                    itemsToProcess[item2] -= subItemCount;
                                }
                            }
                        }
                    }
                }
                if (finished)
                {
                    //look for negative items count (caused when there some item in finalItems is a subitem or ingredient of another in finalItems
                    foreach (KeyValuePair<Item, float> pair in tmpDict)
                    {
                        if (pair.Value < 0)
                        {
                            if (!itemsToProcess.TryGetValue(pair.Key, out float value))
                            {
                                value = 0;
                            }
                            itemsToProcess[pair.Key] = value - pair.Value;
                            finished = false;
                        }
                    }
                }
            }

            List<RecipeItem> result = new List<RecipeItem>();
            if (multiplier == null)
            {
                multiplier = 1;
            }
            foreach (KeyValuePair<Item, float> pair in tmpDict)
            {
                RecipeItem newItem = new RecipeItem();
                newItem.Item = pair.Key;
                newItem.Count = pair.Value * multiplier.Value;
                result.Add(newItem);
//                FileLog.Log(pair.Key.DisplayName + ": " + pair.Value.ToString("N2"));
            }
            if (recipesNeeded != null)
            {
                foreach(KeyValuePair<Recipe, float> recipe in tmpRecipes)
                {
                    if (recipe.Value != 0)
                    {
                        AddRecipeToDict(recipesNeeded, recipe.Key, recipe.Value * multiplier.Value);
                    }
                }
            }
            return result;
        }

        public (int? count, Mine mine) GetMinedItemsPerMineAndMonth(Item item)
        {
            if (_minedItemsPerMonth == null)
            {
                CalculateMinedItemsPerMonth();
            }
            if (_minedItemsPerMonth.TryGetValue(item, out (int count, Mine mine) data)) {
                return data;
            }
            return (null, null);
        }

        public Device GetDevice(Recipe recipe)
        {
            if (_recipeToDevice == null)
            {
                CreateRecipeToDevice();
            }
            _recipeToDevice.TryGetValue(recipe, out Device result);
            return result;
        }

        public Dictionary<(Device device, Recipe recipe), float> GetNeededDevicesPerMonth(Dictionary<Recipe, float> recipesAmounts)
        {
            Dictionary<(Device device, Recipe recipe), float> result = new();
            FileLog.Log("Devices: ");
            foreach (KeyValuePair<Recipe, float> recipeAmount in recipesAmounts)
            {
                Device device = GetDevice(recipeAmount.Key);
                if (device)
                {
                    float devicesCount = (recipeAmount.Key.Duration * recipeAmount.Value * TimeManager.GameMonthsPerSecond);
                    result.AddFloatToDict((device, recipeAmount.Key), devicesCount);
                    FileLog.Log($"{device.DisplayName}, {recipeAmount.Key.DisplayName}: " + devicesCount.ToString("N2"));
                }
            }
            return result;
        }

        private void AddRecipeItemToDict(Dictionary<Item, RecipeItem> dictionary, Item item, float count)
        {
            if (!dictionary.TryGetValue(item, out RecipeItem recipeItem))
            {
                recipeItem = dictionary[item] = new RecipeItem();
                recipeItem.Item = item;
                recipeItem.Count = count;
            } else
            {
                recipeItem.Count += count;
            }
        }
        private void AddRecipeItemToDict(Dictionary<Item, float> dictionary, Item item, float count)
        {
            if (!dictionary.TryGetValue(item, out float dictCount))
            {
                dictionary.Add(item, count);
            }
            else
            {
                dictionary[item] = dictCount+count;
            }
        }

        private void AddRecipeToDict(Dictionary<Recipe, float> dictionary, Recipe recipe, float count)
        {
            if (!dictionary.TryGetValue(recipe, out float dictCount))
            {
                dictionary.Add(recipe, count);
            }
            else
            {
                dictionary[recipe] = dictCount + count;
            }
        }

        private (Recipe recipe, float outputCount) GetRecipe(Item item)
        {
            if (!_itemToRecipe.TryGetValue(item, out (Recipe recipe, float outputCount) result))
            {
                result = (null, 0);
                ImmutableList<Recipe> recipes = Recipes;
                for (int i = 0; i < recipes.Count; i++)
                {
                    RecipeItem[] outputItems = recipes[i].OutputItems;
                    for (int j = 0; j < outputItems.Length; j++)
                    {
                        _itemToRecipe[item] = (recipes[i], outputItems[j].Count);
                        if (outputItems[j].Item == item)
                        {
                            result = _itemToRecipe[item];
                        }
                    }
                    if (result.recipe != null)
                    {
                        return result;
                    }
                }
            }
            return result;
        }

        private void CalculateMinedItemsPerMonth()
        {
            List<Mine> mines = Manager<AssetLibrary>.Current.GetAll<Mine>();
            BuildingRecipeManager buildMan = LazyManager<BuildingRecipeManager>.Current;
            _minedItemsPerMonth = new Dictionary<Item, ValueTuple<int, Mine>>();
            foreach (Mine mine in mines)
            {
                BuildingRecipe recipe = buildMan.Get(mine.AssetId);
                if (recipe.Hidden == false && recipe.IsUnlocked == true)
                {
                    int itemsPerMonth = (int)Math.Round(1 / mine.SharedData.OutputInterval / TimeManager.GameMonthsPerSecond);
                    var storages = LazyManager<StorageManager>.Current.GetStorages(mine.AssetId);
                    if (storages != null)
                    {
                        foreach (Storage storage in storages.ToList())
                        {
                            bool noChange;
                            if (noChange = _minedItemsPerMonth.TryGetValue(storage.Item, out (int count , Mine itemMine) itemData))
                            {
                                if (itemData.count < itemsPerMonth)
                                {
                                    noChange = false;
                                }
                            }
                            if (!noChange)
                            {
                                _minedItemsPerMonth[storage.Item] = (itemsPerMonth, mine);
                            }
                        }
                    }
                }
            }
        }

        private ImmutableList<Recipe> Recipes
        {
            get
            {
                if (_recipes == null)
                {
                    _recipes = Manager<RecipeManager>.Current.GetRecipes();
                }
                return _recipes.Value;
            }
        }

        private void CreateRecipeToDevice()
        {
            _recipeToDevice = new Dictionary<Recipe, Device>();
            List<Device> devices = Manager<AssetLibrary>.Current.GetAll<Device>();
            RecipeManager recipeMan = Manager<RecipeManager>.Current;
            foreach (Device device in devices)
            {
                ImmutableList<Recipe> recipes = recipeMan.GetRecipes(device.SharedData.RecipeTarget);
                foreach (Recipe recipe in recipes.ToArray())
                {
                    _recipeToDevice.Add(recipe, device);
                }
            }
        }

        private readonly Dictionary<Item, (Recipe recipe, float outputCount)> _itemToRecipe = new Dictionary<Item, (Recipe recipe, float outputCount)>();
        private ImmutableList<Recipe>? _recipes;
        private readonly Dictionary<Item, List<RecipeItem>> _itemToIngredients = new Dictionary<Item, List<RecipeItem>>();
        private readonly Dictionary<Item, Dictionary<Item, float>> _itemToSubitems = new Dictionary<Item, Dictionary<Item, float>>();
        private readonly Dictionary<Item, Dictionary<Recipe, float>> _itemToSubRecipes = new Dictionary<Item, Dictionary<Recipe, float>>();
        private Dictionary<Item, ValueTuple<int, Mine>> _minedItemsPerMonth = null;
        private Dictionary<Recipe, Device> _recipeToDevice = null;

    }
}
