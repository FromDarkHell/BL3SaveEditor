using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace BL3Tools.GameData.Items {
    public static class InventoryNameDatabase {
        /// <summary>
        /// A <c>JObject</c> representing the Name DB as loaded from JSON
        /// </summary>
        private static JObject NameDatabase { get; set; } = null;

        /// <summary>
        /// An easy to use dictionary mapping a part to a ""human safe"" name as loaded from the DB.
        /// </summary>
        public static Dictionary<string, string> NameDictionary { get; private set; } = null;

        /// <summary>
        /// A <c>JObject</c> representing the prefix DB as loaded from the JSON
        /// </summary>
        private static JObject PrefixDatabase { get; set; } = null;

        /// <summary>
        /// An easy to use dictionary that maps a part to a given prefix
        /// </summary>
        public static Dictionary<string, string> PrefixDictionary { get; private set; } = null;

        private static readonly string embeddedNameDatabase = "BL3Tools.GameData.Items.Mappings.part_name_mapping.json";
        private static readonly string embeddedPrefixDatabase = "BL3Tools.GameData.Items.Mappings.prefix_name_mapping.json";

        static InventoryNameDatabase() {
            Console.WriteLine("Initializing InventoryNameDatabase...");

            Assembly me = typeof(BL3Tools).Assembly;

            using (Stream stream = me.GetManifestResourceStream(embeddedNameDatabase))
            using (StreamReader reader = new StreamReader(stream)) {
                string result = reader.ReadToEnd();

                LoadInventoryNameDatabase(result);
            }

            using (Stream stream = me.GetManifestResourceStream(embeddedPrefixDatabase))
            using (StreamReader reader = new StreamReader(stream)) {
                string result = reader.ReadToEnd();

                LoadInventoryPrefixDatabase(result);
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

        public static bool LoadInventoryPrefixDatabase(string JSONString) {
            var lastDB = PrefixDatabase;
            try {
                JObject db = JObject.FromObject(JsonConvert.DeserializeObject(JSONString));
                PrefixDictionary = db.ToObject<Dictionary<string, string>>();
                PrefixDatabase = db;

                return true;
            }
            catch (Exception) {
                PrefixDatabase = lastDB;
            }

            return false;
        }

        /// <summary>
        /// Gets the associated ""human safe"" (non code) name for the given set of parts; 
        /// You <b>CAN</b> include a customization balance in the parts and you will get a customization part instead of calling <see cref="GetCustomizationNameForBalance"/>.
        /// </summary>
        /// <param name="parts">A list of parts</param>
        /// <returns>Whether or not to include prefixes in the result</returns>
        public static string GetNameForParts(List<string> parts, bool bPrefixes = true) {
            string titleName = NameDictionary.Where(x => parts.Contains(x.Key)).Select(x => x.Value).LastOrDefault();
            string prefix = !bPrefixes ? null : PrefixDictionary.Where(x => parts.Contains(x.Key)).Select(x => x.Value).LastOrDefault();
            if (string.IsNullOrEmpty(titleName)) return null;
            else if (!string.IsNullOrEmpty(prefix)) 
                return (prefix + " " + titleName);

            return titleName;
        }

        /// <summary>
        /// Gets the associated ""human safe"" (non-code) name for a customization balance
        /// Since customizations don't have parts, this is the function you want to use in the event you need the name of a customization.
        /// </summary>
        /// <param name="balance">The balance for the customization</param>
        /// <returns>The human safe name of the balance</returns>
        public static string GetCustomizationNameForBalance(string balance) {
            string name = NameDictionary.Where(x => x.Key.Equals(balance)).Select(x => x.Value).LastOrDefault();
            if (string.IsNullOrEmpty(name)) return null;
            
            return name;
        }
    }
}
