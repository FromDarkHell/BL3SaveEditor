using System;
using System.IO;
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

        private static readonly string embeddedDatabasePath = "BL3Tools.GameData.Items.SerialDB.Inventory Serial Number Database.json";

        static InventorySerialDatabase() {
            Console.WriteLine("Initializing InventorySerialDatabase...");

            Assembly me = typeof(BL3Tools).Assembly;

            using (Stream stream = me.GetManifestResourceStream(embeddedDatabasePath))
            using (StreamReader reader = new StreamReader(stream)) {
                string result = reader.ReadToEnd();

                LoadInventorySerialDatabase(result);
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

            return assets[index - 1].Value<string>();
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

                return true;
            }
            catch(Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                InventoryDatabase = originalDatabase;
            }

            return false;
        }
    }
}
