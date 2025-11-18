using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using log4net;

namespace Beasts.Services
{
    /// <summary>
    /// Service for fetching and caching item prices from poe.ninja API
    /// </summary>
    public class PoeNinjaService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeNinjaService));
        private static readonly HttpClient _httpClient = new HttpClient();

        // Price cache
        private static Dictionary<string, PoeNinjaItemPrice> _priceCache = new Dictionary<string, PoeNinjaItemPrice>();
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(1);

        // API configuration
        private const string POENINJA_API_BASE = "https://poe.ninja/api/data";
        
        // League (configurable)
        public static string CurrentLeague { get; set; } = "Standard";

        /// <summary>
        /// Get the chaos value of an item by name
        /// Returns null if item not found or API unavailable
        /// </summary>
        public static double? GetItemValue(string itemName, string itemClass = null)
        {
            if (string.IsNullOrEmpty(itemName))
                return null;

            // Ensure cache is fresh
            if (NeedsCacheRefresh())
            {
                Log.Info("[PoeNinja] Cache expired, will refresh on background thread");
                // Don't block - use stale cache if available
                Task.Run(() => RefreshCache());
            }

            // Try exact match first
            if (_priceCache.TryGetValue(itemName, out var price))
            {
                return price.ChaosValue;
            }

            // Try case-insensitive match
            var caseInsensitiveMatch = _priceCache.FirstOrDefault(kvp => 
                string.Equals(kvp.Key, itemName, StringComparison.OrdinalIgnoreCase));
            
            if (caseInsensitiveMatch.Value != null)
            {
                return caseInsensitiveMatch.Value.ChaosValue;
            }

            // Try partial match (useful for unique items with varying rolls)
            var partialMatch = _priceCache.FirstOrDefault(kvp => 
                kvp.Key.Contains(itemName) || itemName.Contains(kvp.Key));
            
            if (partialMatch.Value != null)
            {
                Log.DebugFormat("[PoeNinja] Partial match: {0} -> {1}", itemName, partialMatch.Key);
                return partialMatch.Value.ChaosValue;
            }

            // Item not found in cache
            return null;
        }

        /// <summary>
        /// Check if an item is valuable based on chaos threshold
        /// </summary>
        public static bool IsItemValuable(string itemName, double minChaosValue, string itemClass = null)
        {
            var value = GetItemValue(itemName, itemClass);
            return value.HasValue && value.Value >= minChaosValue;
        }

        /// <summary>
        /// Refresh the price cache from poe.ninja API
        /// </summary>
        private static async Task RefreshCache()
        {
            try
            {
                Log.Info($"[PoeNinja] Refreshing price cache for league: {CurrentLeague}");

                var newCache = new Dictionary<string, PoeNinjaItemPrice>();

                // Fetch different item categories
                await FetchCurrencyPrices(newCache);
                await FetchFragmentPrices(newCache);
                await FetchDivinationCardPrices(newCache);
                await FetchUniquePrices(newCache);
                await FetchMapPrices(newCache);

                // Update cache atomically
                _priceCache = newCache;
                _lastCacheUpdate = DateTime.UtcNow;

                Log.InfoFormat("[PoeNinja] Cache refreshed successfully - {0} items loaded", _priceCache.Count);
            }
            catch (Exception ex)
            {
                Log.Error("[PoeNinja] Failed to refresh price cache", ex);
            }
        }

        private static bool NeedsCacheRefresh()
        {
            return (DateTime.UtcNow - _lastCacheUpdate) > CACHE_DURATION;
        }

        private static async Task FetchCurrencyPrices(Dictionary<string, PoeNinjaItemPrice> cache)
        {
            try
            {
                var url = $"{POENINJA_API_BASE}/currencyoverview?league={CurrentLeague}&type=Currency";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                foreach (var item in data["lines"])
                {
                    var name = item["currencyTypeName"]?.ToString();
                    var chaosValue = item["chaosEquivalent"]?.ToObject<double>() ?? 0;

                    if (!string.IsNullOrEmpty(name))
                    {
                        cache[name] = new PoeNinjaItemPrice
                        {
                            Name = name,
                            ChaosValue = chaosValue,
                            ItemClass = "Currency"
                        };
                    }
                }

                Log.DebugFormat("[PoeNinja] Loaded {0} currency prices", cache.Count);
            }
            catch (Exception ex)
            {
                Log.Warn("[PoeNinja] Failed to fetch currency prices", ex);
            }
        }

        private static async Task FetchFragmentPrices(Dictionary<string, PoeNinjaItemPrice> cache)
        {
            try
            {
                var url = $"{POENINJA_API_BASE}/currencyoverview?league={CurrentLeague}&type=Fragment";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                foreach (var item in data["lines"])
                {
                    var name = item["currencyTypeName"]?.ToString();
                    var chaosValue = item["chaosEquivalent"]?.ToObject<double>() ?? 0;

                    if (!string.IsNullOrEmpty(name))
                    {
                        cache[name] = new PoeNinjaItemPrice
                        {
                            Name = name,
                            ChaosValue = chaosValue,
                            ItemClass = "Fragment"
                        };
                    }
                }

                Log.DebugFormat("[PoeNinja] Loaded {0} total items (including fragments)", cache.Count);
            }
            catch (Exception ex)
            {
                Log.Warn("[PoeNinja] Failed to fetch fragment prices", ex);
            }
        }

        private static async Task FetchDivinationCardPrices(Dictionary<string, PoeNinjaItemPrice> cache)
        {
            try
            {
                var url = $"{POENINJA_API_BASE}/itemoverview?league={CurrentLeague}&type=DivinationCard";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                foreach (var item in data["lines"])
                {
                    var name = item["name"]?.ToString();
                    var chaosValue = item["chaosValue"]?.ToObject<double>() ?? 0;

                    if (!string.IsNullOrEmpty(name))
                    {
                        cache[name] = new PoeNinjaItemPrice
                        {
                            Name = name,
                            ChaosValue = chaosValue,
                            ItemClass = "DivinationCard"
                        };
                    }
                }

                Log.DebugFormat("[PoeNinja] Loaded {0} total items (including divination cards)", cache.Count);
            }
            catch (Exception ex)
            {
                Log.Warn("[PoeNinja] Failed to fetch divination card prices", ex);
            }
        }

        private static async Task FetchUniquePrices(Dictionary<string, PoeNinjaItemPrice> cache)
        {
            try
            {
                // Fetch multiple unique categories
                var categories = new[] { "UniqueWeapon", "UniqueArmour", "UniqueAccessory", "UniqueFlask", "UniqueJewel" };

                foreach (var category in categories)
                {
                    var url = $"{POENINJA_API_BASE}/itemoverview?league={CurrentLeague}&type={category}";
                    var response = await _httpClient.GetStringAsync(url);
                    var data = JObject.Parse(response);

                    foreach (var item in data["lines"])
                    {
                        var name = item["name"]?.ToString();
                        var chaosValue = item["chaosValue"]?.ToObject<double>() ?? 0;

                        if (!string.IsNullOrEmpty(name))
                        {
                            cache[name] = new PoeNinjaItemPrice
                            {
                                Name = name,
                                ChaosValue = chaosValue,
                                ItemClass = "Unique"
                            };
                        }
                    }
                }

                Log.DebugFormat("[PoeNinja] Loaded {0} total items (including uniques)", cache.Count);
            }
            catch (Exception ex)
            {
                Log.Warn("[PoeNinja] Failed to fetch unique item prices", ex);
            }
        }

        private static async Task FetchMapPrices(Dictionary<string, PoeNinjaItemPrice> cache)
        {
            try
            {
                var url = $"{POENINJA_API_BASE}/itemoverview?league={CurrentLeague}&type=Map";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                foreach (var item in data["lines"])
                {
                    var name = item["name"]?.ToString();
                    var chaosValue = item["chaosValue"]?.ToObject<double>() ?? 0;

                    if (!string.IsNullOrEmpty(name))
                    {
                        cache[name] = new PoeNinjaItemPrice
                        {
                            Name = name,
                            ChaosValue = chaosValue,
                            ItemClass = "Map"
                        };
                    }
                }

                Log.DebugFormat("[PoeNinja] Loaded {0} total items (including maps)", cache.Count);
            }
            catch (Exception ex)
            {
                Log.Warn("[PoeNinja] Failed to fetch map prices", ex);
            }
        }

        /// <summary>
        /// Force an immediate cache refresh (blocking)
        /// </summary>
        public static void ForceRefresh()
        {
            Log.Info("[PoeNinja] Force refreshing cache...");
            RefreshCache().Wait();
        }

        /// <summary>
        /// Initialize the service and load initial price data
        /// </summary>
        public static void Initialize(string league = null)
        {
            if (!string.IsNullOrEmpty(league))
            {
                CurrentLeague = league;
            }

            Log.InfoFormat("[PoeNinja] Initializing service for league: {0}", CurrentLeague);
            
            // Start async cache refresh
            Task.Run(() => RefreshCache());
        }
    }

    /// <summary>
    /// Represents an item price from poe.ninja
    /// </summary>
    public class PoeNinjaItemPrice
    {
        public string Name { get; set; }
        public double ChaosValue { get; set; }
        public string ItemClass { get; set; }
    }
}

