using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BL3Tools.GameData.Items {
    public static class InventoryNameDatabase {
        /// <summary>
        /// A <c>JObject</c> representing the Name DB as loaded from JSON
        /// </summary>
        private static JObject NameDatabase { get; set; } = null;

        /// <summary>
        /// An easy to use dictionary mapping the balances to a ""human safe"" name as loaded from the DB.
        /// </summary>
        public static Dictionary<string, string> NameDictionary { get; private set; } = null;

        private static readonly string embeddedDatabasePath = "BL3Tools.GameData.Items.Mappings.balance_name_mapping.json";

        static InventoryNameDatabase() {
            Console.WriteLine("Initializing InventoryNameDatabase...");

            Assembly me = typeof(BL3Tools).Assembly;

            using (Stream stream = me.GetManifestResourceStream(embeddedDatabasePath))
            using (StreamReader reader = new StreamReader(stream)) {
                string result = reader.ReadToEnd();

                LoadInventoryNameDatabase(result);
            }
        }

        /// <summary>
        /// Replace the inventory serial database info with the one specified in this specific string
        /// </summary>
        /// <param name="JSONString">A JSON string representing the InventorySerialDB</param>
        /// <returns>Whether or not the loading succeeded</returns>
        public static bool LoadInventoryNameDatabase(string JSONString) {
            var lastDB = NameDatabase;
            try {
                JObject db = JObject.FromObject(JsonConvert.DeserializeObject(JSONString));
                NameDictionary = db.ToObject<Dictionary<string, string>>();
                NameDatabase = db;

                return true;
            }
            catch (Exception) {
                NameDatabase = lastDB;
            }

            return false;
        }

        /// <summary>
        /// Gets the associated ""human safe"" name for the given code path to a balance, will return the balance if it's not available in the DB
        /// </summary>
        /// <param name="balance">Asset path to an item balance</param>
        /// <returns>The human-safe name if it's available, otherwise the original balance string</returns>
        public static string GetNameForBalance(string balance) {
            // Check if the name exists by default
            if (NameDictionary.ContainsKey(balance))
                return NameDictionary[balance];
            else if (NameDictionary.ContainsKey(balance.ToLowerInvariant()))
                return NameDictionary[balance.ToLowerInvariant()];
            
            // Check if the balance has "." in it; It's used mostly in the inventory db
            int idx = balance.LastIndexOf('.');
            if(idx >= 0) {
                // Remove everything past the last "."
                string fixedBalance = balance.Substring(0, idx);
                return GetNameForBalance(fixedBalance);
            }

            return balance;
        }
    }
}
