using BL3Tools.GameData.Items;
using BL3Tools.GVAS;
using System.Linq;
using OakSave;
using System.Collections.Generic;

namespace BL3Tools {

    /// <summary>
    /// A simple underlying class that's used to store both <see cref="BL3Save"/> and <see cref="BL3Profile"/>
    /// </summary>
    public class UE3Save {
        /// <summary>
        /// The file path associated with this path; does not need to be set
        /// </summary>
        public string filePath { get; set; } = null;

        public GVASSave GVASData { get; set; }
    }


    /// <summary>
    /// A class that represents a Borderlands 3 save.
    /// <para>This stores all of the data about it; Including the underlying protobuf data as well as all of the loaded/serializable items from the save.</para>
    /// </summary>
    public class BL3Save : UE3Save {
        public BL3Save(GVASSave saveData, Character character) {
            GVASData = saveData;
            Character = character;
            
            InventoryItems = Character.InventoryItems.Select(x => Borderlands3Serial.DecryptSerial(x.ItemSerialNumber)).ToList();
            
            for(int i = 0; i < InventoryItems.Count; i++) {
                InventoryItems[i].OriginalData = Character.InventoryItems[i];
            }
        }

        // Unlike the profiles, we can't just remove all of the data from the save's inventory and then readd it
        // Saves store other data in the items as well so we can't do that

        /// <summary>
        /// Deletes the given item from the save
        /// </summary>
        /// <param name="serialToDelete">A <see cref="Borderlands3Serial"/> object representing the item to delete</param>
        public void DeleteItem(Borderlands3Serial serialToDelete) {

            InventoryItems.Remove(serialToDelete);
            if (serialToDelete.OriginalData != null) {
                Character.InventoryItems.RemoveAll(x => ReferenceEquals(x, serialToDelete.OriginalData));
            }
        }

        /// <summary>
        /// Adds the given item to the save
        /// </summary>
        /// <param name="serialToAdd">A <see cref="Borderlands3Serial"/> object representing the item to add</param>
        public void AddItem(Borderlands3Serial serialToAdd) {
            InventoryItems.Add(serialToAdd);
            var oakItem = new OakInventoryItemSaveGameData() {
                DevelopmentSaveData = null,
                Flags = 0x01, // "NEW" Flag
                PickupOrderIndex = 8008,
                WeaponSkinPath = "", // No skin obviously
                ItemSerialNumber = serialToAdd.EncryptSerialToBytes()
            };
            // Properly add in the item onto the save
            Character.InventoryItems.Add(oakItem);
            serialToAdd.OriginalData = oakItem;
        }

        /// <summary>
        /// The underlying protobuf data representing this save
        /// </summary>
        public Character Character { get; set; } = null;

        /// <summary>
        ///  The respective platform for the given save
        ///  <para>Used for encryption/decryption of the save files</para>
        /// </summary>
        public Platform Platform { get; set; } = Platform.PC;

        /// <summary>
        /// A list containing all of the inventory items of this file
        /// <para>If you want to add items to the save, please use <see cref="AddItem(Borderlands3Serial)"/></para>
        /// <para>If you want to delete items from the save, please use <see cref="DeleteItem(Borderlands3Serial)"/></para>
        /// </summary>
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

    /// <summary>
    /// A simple class that represents the Borderlands 3 profile structure.
    /// </summary>
    public class BL3Profile : UE3Save {
        public BL3Profile(GVASSave gvasSave, Profile profile) {
            this.GVASData = gvasSave;
            Profile = profile;

            BankItems = Profile.BankInventoryLists.Select(x => Borderlands3Serial.DecryptSerial(x)).ToList();
            LostLootItems = Profile.LostLootInventoryLists.Select(x => Borderlands3Serial.DecryptSerial(x)).ToList();

            for(int i = 0; i < BankItems.Count - 1; i++) {
                Borderlands3Serial item = BankItems[i];
                item.OriginalData = new OakInventoryItemSaveGameData() {
                    DevelopmentSaveData = null,
                    Flags = 0x00,
                    ItemSerialNumber = Profile.BankInventoryLists[i],
                    PickupOrderIndex = -1,
                    WeaponSkinPath = ""
                };
            }

            for (int i = 0; i < LostLootItems.Count - 1; i++) {
                Borderlands3Serial item = LostLootItems[i];
                item.OriginalData = new OakInventoryItemSaveGameData() {
                    DevelopmentSaveData = null,
                    Flags = 0x00,
                    ItemSerialNumber = Profile.LostLootInventoryLists[i],
                    PickupOrderIndex = -1,
                    WeaponSkinPath = ""
                };
            }
        }

        /// <summary>
        /// The underlying protobuf data representing this profile
        /// </summary>
        public Profile Profile { get; set; }


        /// <summary>
        /// A list representing all of the items stored in the current profile's bank.
        /// </summary>
        public List<Borderlands3Serial> BankItems { get; set; } = null;

        /// <summary>
        /// A list representing all of the items stored in the current profile's lost loot.
        /// </summary>
        public List<Borderlands3Serial> LostLootItems { get; set; } = null;


        /// <summary>
        ///  The respective platform for the profile
        ///  <para>Used for encryption/decryption</para>
        /// </summary>
        public Platform Platform { get; set; } = Platform.PC;
    }

}
