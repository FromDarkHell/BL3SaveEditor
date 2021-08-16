using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace BL3Tools.GameData.Items {
    public static class InventorySerialDatabase {
        /// <summary>
        /// The maximum version acceptable for the inventory DB
        /// </summary>
        public static long MaximumVersion { get; private set; } = long.MinValue;
        
        /// <summary>
        /// A <c>JObject</c> representing the InventoryDB as loaded from JSON
        /// </summary>
        public static JObject InventoryDatabase { get; private set; } = null;

        /// <summary>
        /// A list containing all of the valid InventoryDatas for <b>ANY</b> item
        /// </summary>
        public static List<string> InventoryDatas { get; private set; } = null;

        /// <summary>
        /// A list containing all of the valid Manufacturers (<c>ManufacturerData</c>) for <b>ANY</b> item
        /// </summary>
        public static List<string> Manufacturers { get; private set; } = null;

        /// <summary>
        /// A <c>JObject</c> representing the balance to inventory data mapping as loaded from JSON.
        /// </summary>
        public static JObject InventoryDataDatabase { get; private set; } = null;

        /// <summary>
        /// A dictionary mapping a balance to an inventory data string
        /// Do note that this isn't an enforced requirement for items; They can use any InventoryData that they'd like to.
        /// </summary>
        public static Dictionary<string, string> BalanceToData { get; private set; } = null;

        private static readonly string embeddedDatabasePath = "BL3Tools.GameData.Items.SerialDB.Inventory Serial Number Database.json";
        private static readonly string embeddedInvDataDBPath = "BL3Tools.GameData.Items.Mappings.balance_to_inv_data.json";

        static InventorySerialDatabase() {
            Console.WriteLine("Initializing InventorySerialDatabase...");

            Assembly me = typeof(BL3Tools).Assembly;

            using (Stream stream = me.GetManifestResourceStream(embeddedDatabasePath))
            using (StreamReader reader = new StreamReader(stream)) {
                string result = reader.ReadToEnd();

                LoadInventorySerialDatabase(result);
            }

            using (Stream stream = me.GetManifestResourceStream(embeddedInvDataDBPath))
            using (StreamReader reader = new StreamReader(stream)) {
                string result = reader.ReadToEnd();

                LoadInventoryDataDatabase(result);
            }
        }

        /// <summary>
        /// Returns the number of bits used for the specific <paramref name="category"/> and a version of <paramref name="version"/>
        /// </summary>
        /// <param name="category">A category specified in the InventorySerialDatabase for which to eat the bits of</param>
        /// <param name="version">Version of the item that you want to eat the bits of</param>
        /// <returns></returns>
        public static int GetBitsToEat(string category, long version) {
            JArray versionArray = ((JArray)InventoryDatabase[category]["versions"]);
            var minimumBits = versionArray.First["bits"].Value<int>();

            foreach(JObject versionData in versionArray.Children()) {
                int arrVer = versionData["version"].Value<int>();
                if (arrVer > version) 
                    return minimumBits;
                else if (version >= arrVer) {
                    minimumBits = versionData["bits"].Value<int>();
                }
            }

            return minimumBits;
        }

        /// <summary>
        /// Given <paramref name="index"/>, return the associated part name for the <paramref name="category"/>
        /// </summary>
        /// <param name="category">A category specified in the InventorySerialDatabase for which to eat the bits of</param>
        /// <param name="index">Index gathered by eating the bits for the item in the DB</param>
        /// <returns></returns>
        public static string GetPartByIndex(string category, int index) {
            if (index < 0) return null;
            JArray assets = ((JArray)InventoryDatabase[category]["assets"]);
            if (index > assets.Count) return null;

            return assets[index - 1].Value<string>();
        }

        /// <summary>
        /// Given <paramref name="part"/>, return the index for the part in the <paramref name="category"/>
        /// </summary>
        /// <param name="category">A category specified in the InventorySerialDatabase for which to get the index of</param>
        /// <param name="part">A SerialDB based part-name</param>
        /// <returns>The index of the given part in the category</returns>
        public static int GetIndexByPart(string category, string part) {
            // This logic here is a little bit janky, allows us to actually do an index of call without casting to a JToken
            // It basically just gets every item of the category as a string.
            var assets = ((JArray)InventoryDatabase[category]["assets"]).Children().Select(x => x.Value<string>()).ToList();
            int index = assets.IndexOf(part);
            if (index == -1) index = assets.IndexOf(part.ToLowerInvariant());
            
            return index == -1 ? -1 : index + 1;
        }

        /// <summary>
        /// Given <paramref name="balance"/>, get its properly capitalized "short-name" (last end of the balance after '.')
        /// </summary>
        /// <param name="balance">A balance for which to get the short name of</param>
        /// <returns>Shortname of the balance; null if not found</returns>
        public static string GetShortNameFromBalance(string balance) {
            string serializedName = "";
            var balances = ((JArray)InventoryDatabase["InventoryBalanceData"]["assets"]).Children().Select(x => x.Value<string>());
            serializedName = balances.FirstOrDefault(x => x.Contains(balance) || x.ToLowerInvariant().Contains(balance));
            if (!string.IsNullOrEmpty(serializedName)) {
                serializedName = serializedName.Split('.').Last();
                return serializedName;
            }
            return null;
        }

        /// <summary>
        /// Given <paramref name="shortName"/>, get the balance of the short name
        /// </summary>
        /// <param name="shortName">the ""short name"" of the balance (last end after '.')</param>
        /// <returns>The fully qualified balance of the name</returns>
        public static string GetBalanceFromShortName(string shortName) {
            var balances = ((JArray)InventoryDatabase["InventoryBalanceData"]["assets"]).Children().Select(x => x.Value<string>());
            string balance = balances.FirstOrDefault(x => x.EndsWith(shortName));

            return string.IsNullOrEmpty(balance) ? null : balance;
        }

        /// <summary>
        /// Returns a list of all of the valid manufacturers
        /// </summary>
        /// <param name="bShortName">Whether or not to get the ""short name"" of the manufacturer</param>
        /// <returns>List of all valid manufacturers</returns>
        public static List<string> GetManufacturers(bool bShortName = true) {
            if (bShortName) return Manufacturers.Select(x => x.Split('.').Last()).ToList();
            return Manufacturers;
        }

        /// <summary>
        /// Returns a list of all of the valid <c>InventoryDatas</c>
        /// </summary>
        /// <param name="bShortName">Whether or not to get the ""short name"" of the inventory data</param>
        /// <returns>List of all valid <c>InventoryDatas</c></returns>
        public static List<string> GetInventoryDatas(bool bShortName = true) {
            if (bShortName) return InventoryDatas.Select(x => x.Split('.').Last()).ToList();
            return InventoryDatas;
        }

        /// <summary>
        /// Returns a inventory data for the given balance
        /// Do note that this doesn't make an item that specific type; that's determined by the balance.
        /// </summary>
        /// <param name="balance">A balance to get the inventory data of</param>
        /// <param name="bShortName">Whether or not to get the ""short name"" of the inventory data</param>
        /// <param name="bSelfCorrect">If true, the function will try to get the closest available inventory data if it's not in the database; If false, returns null</param>
        /// <returns>The string representing the InventoryData (short equivalent if <paramref name="bShortName"/> is true)</returns>
        public static string GetInventoryDataByBalance(string balance, bool bShortName = true, bool bSelfCorrect = true) {
            string longName = GetBalanceFromShortName(balance);
            if (longName == null) longName = balance;
            if (BalanceToData.ContainsKey(longName)) {
                string data = BalanceToData[longName];
                if (bShortName) data = data.Split('.').Last();

                return data;
            }
            if (!bSelfCorrect) return null;

            // If the previous check didn't succeed (outdated DB?), try and get the closest thing to it...
            string shortName = balance.Split('.').Last();

            // This is a ton of weird logic that's kinda *ehhh*
            string str = "WT_" + shortName.Substring(shortName.IndexOf('_')+1);
            str = str.Substring(0, str.LastIndexOf('_'));
            List<int> distances = new List<int>();
            foreach(string data in GetInventoryDatas(true)) {
                distances.Add(LevenshteinDistance.Compute(str, data.Split('.').Last()));
            }
            var possibleData = GetInventoryDatas(bShortName)[distances.IndexOf(distances.Min())];

            return possibleData;
        }

        /// <summary>
        /// Returns a list of valid parts for the inventory key
        /// </summary>
        /// <param name="invKey">A valid inventory key for the Serial DB</param>
        /// <returns>A list of valid parts for the inventory key</returns>
        public static List<string> GetPartsForInvKey(string invKey) {
            var parts = ((JArray)InventoryDatabase[invKey]["assets"]).Children().Select(x => x.Value<string>()).ToList();
            return parts;
        }

        /// <summary>
        /// Replace the inventory serial database info with the one specified in this specific string
        /// </summary>
        /// <param name="JSONString">A JSON string representing the InventorySerialDB</param>
        /// <returns>Whether or not the loading succeeded</returns>
        public static bool LoadInventorySerialDatabase(string JSONString) {
            var originalDatabase = InventoryDatabase;
            try {
                InventoryDatabase = JObject.FromObject(JsonConvert.DeserializeObject(JSONString));
                foreach (JProperty token in InventoryDatabase.Children<JProperty>()) {
                    var versions = token.Value.First;
                    var assets = token.Value.Last;
                    if (versions == null || assets == null || (versions == assets)) throw new Exception("Invalid JSON for SerialDB...");
                    
                    foreach(JObject versionData in versions.First.Children()) {
                        long bitAmt = (long)((JValue)versionData["bits"]).Value;
                        long versionNum = (long)((JValue)versionData["version"]).Value;

                        if (versionNum > MaximumVersion) MaximumVersion = versionNum;
                    }
                }

                InventoryDatas = ((JArray)InventoryDatabase["InventoryData"]["assets"]).Children().Select(x => x.Value<string>()).ToList();
                Manufacturers = ((JArray)InventoryDatabase["ManufacturerData"]["assets"]).Children().Select(x => x.Value<string>()).ToList();

                return true;
            }
            catch(Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                InventoryDatabase = originalDatabase;
            }

            return false;
        }

        /// <summary>
        /// Replace the loaded inventory data database info with the one specified in the JSON string
        /// </summary>
        /// <param name="JSONString">A JSON string representing the InventoryData DB</param>
        /// <returns>Whether or not the loading succeeded</returns>
        public static bool LoadInventoryDataDatabase(string JSONString) {

            var originalDB = InventoryDataDatabase;
            try {
                InventoryDataDatabase = JObject.FromObject(JsonConvert.DeserializeObject(JSONString));
                BalanceToData = InventoryDataDatabase.ToObject<Dictionary<string, string>>();

                return true;
            }
            catch(Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                InventoryDataDatabase = originalDB;
            }

            return false;
        }
    }
}
