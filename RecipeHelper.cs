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

        private (List<RecipeItem> rawItems, Dictionary<Item, float> subItems) FindIngredients(Item item, float? coeficient = null)
        {
            List<RecipeItem> cacheResultList;
            Dictionary<Item, float> cacheResultSubItemList;
            if (!this._itemToIngredients.TryGetValue(item, out cacheResultList))
            {
                Dictionary<Item, RecipeItem> dictItems = new Dictionary<Item, RecipeItem>();
                cacheResultList = (this._itemToIngredients[item] = new List<RecipeItem>());
                cacheResultSubItemList = this._itemToSubitems[item] = new Dictionary<Item, float>();
                (Recipe recipe, float outputCount) = GetRecipe(item);
                if (recipe != null)
                {
                    float localCoeficient = 1 / outputCount;
                    foreach (RecipeItem recipeItem in recipe.InputItems)
                    {
                        (List<RecipeItem> ingredients, Dictionary<Item, float> subItems) = this.FindIngredients(recipeItem.Item, localCoeficient * recipeItem.Count);
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
            }
            if (coeficient != null && coeficient.Value != 1)
            {
                List<RecipeItem> resultList = new List<RecipeItem>();
                Dictionary<Item, float> resultSubItemList = new Dictionary<Item, float>();
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
                return (resultList, resultSubItemList);
            }
            return (cacheResultList, cacheResultSubItemList);
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

        public ImmutableList<RecipeItem> GetIngredients(Item item)
        {
            (List<RecipeItem> ingredients, Dictionary<Item, float> subItems) = FindIngredients(item);
            return ingredients.ToImmutableList<RecipeItem>();
        }

        public List<RecipeItem> GetIngredients(Item item, List<Item> finalItems, float? multiplier = null)
        {
            (List<RecipeItem> ingredients, Dictionary<Item, float> subItems) = FindIngredients(item);
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
                        AddRecipeItemToDict(tmpDict, pair.Key, value);
                        itemsToProcess[pair.Key] = 0;
                        (List<RecipeItem> subIngredients, Dictionary<Item, float> subItems2) = FindIngredients(pair.Key);
                        foreach (RecipeItem ingrItem in subIngredients)
                        {
                            AddRecipeItemToDict(tmpDict, ingrItem.Item, -ingrItem.Count*value);
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

        private (Recipe recipe, float outputCount) GetRecipe(Item item)
        {
            if (!_itemToRecipe.TryGetValue(item, out (Recipe recipe, float outputCount) result))
            {
                result = (null, 0);
                if (_recipes == default)
                {
                    _recipes = Manager<RecipeManager>.Current.GetRecipes();
                }
                for (int i = 0; i < _recipes.Count; i++)
                {
                    RecipeItem[] outputItems = _recipes[i].OutputItems;
                    for (int j = 0; j < outputItems.Length; j++)
                    {
                        _itemToRecipe[item] = (_recipes[i], outputItems[j].Count);
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

        private readonly Dictionary<Item, (Recipe recipe, float outputCount)> _itemToRecipe = new Dictionary<Item, (Recipe recipe, float outputCount)>();
        private ImmutableList<Recipe> _recipes = default;
        private readonly Dictionary<Item, List<RecipeItem>> _itemToIngredients = new Dictionary<Item, List<RecipeItem>>();
        private readonly Dictionary<Item, Dictionary<Item, float>> _itemToSubitems = new Dictionary<Item, Dictionary<Item, float>>();
        private Dictionary<Item, ValueTuple<int, Mine>> _minedItemsPerMonth = null;

    }
}
