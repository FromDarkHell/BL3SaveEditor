using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace BL3Tools.GameData.Items {
    public static class InventoryKeyDB {
        /// <summary>
        /// A <c>JObject</c> representing the InventoryKey DB as loaded from JSON
        /// </summary>
        private static JObject KeyDatabase { get; set; } = null;

        /// <summary>
        /// An easy to use dictionary mapping the balances to a SerialDB key as loaded from the DB.
        /// </summary>
        public static Dictionary<string, string> KeyDictionary { get; private set; } = null;

        public static Dictionary<string, List<string>> ItemTypeToKey { get; private set; } = null;

        private static readonly string embeddedDatabasePath = "BL3Tools.GameData.Items.Mappings.balance_to_inv_key.json";

        static InventoryKeyDB() {
            Console.WriteLine("Initializing InventoryKeyDB...");

            Assembly me = typeof(BL3Tools).Assembly;

            using (Stream stream = me.GetManifestResourceStream(embeddedDatabasePath))
            using (StreamReader reader = new StreamReader(stream)) {
                string result = reader.ReadToEnd();

                LoadInventoryKeyDatabase(result);
            }
        }

        /// <summary>
        /// Replace the inventory serial database info with the one specified in this specific string
        /// </summary>
        /// <param name="JSONString">A JSON string representing the InventorySerialDB</param>
        /// <returns>Whether or not the loading succeeded</returns>
        public static bool LoadInventoryKeyDatabase(string JSONString) {
            var lastDB = KeyDatabase;
            try {
                JObject db = JObject.FromObject(JsonConvert.DeserializeObject(JSONString));
                KeyDictionary = db.ToObject<Dictionary<string, string>>();
                KeyDatabase = db;

                var invKeys = KeyDictionary.Values.Distinct();

                ItemTypeToKey = new Dictionary<string, List<string>>() {
                    { "Grenades", invKeys.Where(x => x.Contains("_GrenadeMod_")).ToList() },
                    { "Shields", invKeys.Where(x => x.Contains("_Shield_")).ToList() },
                    { "Class Mods", invKeys.Where(x => x.Contains("_ClassMod_")).ToList() },
                    { "Artifacts", invKeys.Where(x => x.Contains("_Artifact_")).ToList() },
                    { "Assault Rifles", invKeys.Where(x => x.Contains("_AR_")).ToList() },
                    { "Pistols", invKeys.Where(x => x.Contains("_Pistol_") || x.Contains("_PS_")).ToList() },
                    { "SMGs", invKeys.Where(x => x.Contains("_SM_") || x.Contains("_SMG")).ToList() },
                    { "Heavy Weapons", invKeys.Where(x => x.Contains("_HW_")).ToList() },
                    { "Shotguns", invKeys.Where(x => x.Contains("_SG_") || x.Contains("_Shotgun_")).ToList() },
                    { "Sniper Rifles", invKeys.Where(x => x.Contains("_SR_")).ToList() },
                    { "Eridian Fabricator", invKeys.Where(x => x.Contains("EridianFabricator")).ToList() },
                    { "Customizations", invKeys.Where(x => x.Contains("Customization")).ToList() }
                };

                return true;
            }
            catch (Exception) {
                KeyDatabase = lastDB;
            }

            return false;
        }


        public static string GetKeyForBalance(string balance) {

            // Check if the name exists by default
            if (!balance.Contains(".")) balance = $"{balance}.{balance.Split('/').LastOrDefault()}";

            if (KeyDictionary.ContainsKey(balance))
                return KeyDictionary[balance];
            else if (KeyDictionary.ContainsKey(balance.ToLower())) 
                return KeyDictionary[balance.ToLower()];
            
            return null;
        }
    }
}
