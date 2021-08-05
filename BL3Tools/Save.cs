using BL3Tools.GVAS;
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

        }

        public Character Character { get; set; }

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
            this.GVASSave = gvasSave;
            Profile = profile;
        }

        public GVASSave GVASSave { get; }
        public Profile Profile { get; set; }
    }

}
