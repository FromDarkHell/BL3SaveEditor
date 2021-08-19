using BL3Tools.GameData.Items;
using BL3Tools.GVAS;
using System.Linq;
using OakSave;
using System.Collections.Generic;

namespace BL3Tools {

    public class UE3Save {
        /// <summary>
        /// The file path associated with this path; does not need to be set
        /// </summary>
        public string filePath { get; set; } = null;

        public GVASSave GVASData { get; set; }
    }

    public class BL3Save : UE3Save {
        public BL3Save(GVASSave saveData, Character character) {
            GVASData = saveData;
            Character = character;
            
            InventoryItems = Character.InventoryItems.Select(x => Borderlands3Serial.DecryptSerial(x.ItemSerialNumber)).ToList();
        }

        public Character Character { get; set; } = null;

        public List<Borderlands3Serial> InventoryItems { get; set; } = null;

        public static Dictionary<string, PlayerClassSaveGameData> ValidClasses = new Dictionary<string, PlayerClassSaveGameData>() {
            { "FL4K", new PlayerClassSaveGameData() {
                DlcPackageId = 0,
                PlayerClassPath="/Game/PlayerCharacters/Beastmaster/PlayerClassId_Beastmaster.PlayerClassId_Beastmaster" } },
            { "Moze", new PlayerClassSaveGameData() {
                DlcPackageId = 0,
                PlayerClassPath="/Game/PlayerCharacters/Gunner/PlayerClassId_Gunner.PlayerClassId_Gunner" } },
            { "Zane", new PlayerClassSaveGameData() {
                DlcPackageId = 0,
                PlayerClassPath="/Game/PlayerCharacters/Operative/PlayerClassId_Operative.PlayerClassId_Operative" } },
            { "Amara", new PlayerClassSaveGameData() {
                DlcPackageId = 0, PlayerClassPath=
                "/Game/PlayerCharacters/SirenBrawler/PlayerClassId_Siren.PlayerClassId_Siren" } },
        };

        public static Dictionary<string, string> CharacterToClassPair = new Dictionary<string, string>() {
            { "FL4K", "Beastmaster" },
            { "Moze", "Gunner" },
            { "Zane", "Operative" },
            { "Amara", "Siren" }
        };
    }

    public class BL3Profile : UE3Save {
        public BL3Profile(GVASSave gvasSave, Profile profile) {
            this.GVASData = gvasSave;
            Profile = profile;

            BankItems = Profile.BankInventoryLists.Select(x => Borderlands3Serial.DecryptSerial(x)).ToList();
            LostLootItems = Profile.LostLootInventoryLists.Select(x => Borderlands3Serial.DecryptSerial(x)).ToList();
        }

        public Profile Profile { get; set; }

        public List<Borderlands3Serial> BankItems { get; set; } = null;

        public List<Borderlands3Serial> LostLootItems { get; set; } = null;
    }

}
