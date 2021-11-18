using System;
using System.Windows;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Linq;
using BL3Tools;
using BL3Tools.GameData;
using OakSave;
using System.Windows.Media;
using System.Collections.ObjectModel;
using BL3Tools.GameData.Items;
using Newtonsoft.Json.Linq;

namespace BL3SaveEditor.Helpers {

    /// <summary>
    /// A WPF value converter which converts to a boolean based off of whether or not the value is not null
    /// </summary>
    public class NullToBooleanConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (value == null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value;
        }
    }

    /// <summary>
    /// A WPF value converter which converts a UInt32 amount of seconds to a TimeSpan (and back and forth)
    /// </summary>
    public class IntegerSecondsToTimeSpanConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            TimeSpan timeSpan = TimeSpan.FromSeconds(System.Convert.ToDouble(value));
            return timeSpan;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return System.Convert.ToUInt32(((TimeSpan)value).TotalSeconds);
        }
    }

    /// <summary>
    /// A WPF multi-value converter which will only return true if all of the variables passed can be converted ot a bool; and resolve as True.
    /// </summary>
    public class AndConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Any(v => ReferenceEquals(v, DependencyProperty.UnsetValue)))
                return DependencyProperty.UnsetValue;
            values = values.Select(x => {
                if (x is string && x != null) return true;
                else if (x is string && x == null) return false;
                return x;
            }).ToArray();

            return values.All(System.Convert.ToBoolean);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// A WPF value converter which implements a simple limiting function on the passed parameter (as an integer)
    /// </summary>
    public class IntegerLimiterConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var strParam = (parameter as string);
            string limitingType = "max";
            if (strParam != null && strParam.Contains("|")) limitingType = strParam.Split('|')[1];
            
            string integer = strParam.Split('|')[0];
            int limit = System.Convert.ToInt32(integer);
            int? val = value as int?;

            if (val == null) return false;
            
            return limitingType == "max" ? val < limit : val > limit;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    #region Specialized Converters
    /// <summary>
    /// A simple WPF converter that converts the EXP points of a player to the specified level
    /// </summary>
    public class EXPointToLevelConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return PlayerXP.GetLevelForPoints(System.Convert.ToInt32(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return PlayerXP.GetPointsForXPLevel(System.Convert.ToInt32(value));
        }
    }

    /// <summary>
    /// A simple WPF converter that converts the XP level of a player to the specified XP points
    /// <para></para>
    /// See also: <seealso cref="EXPointToLevelConverter"/>
    /// </summary>
    public class LevelToEXPointConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return PlayerXP.GetPointsForXPLevel(System.Convert.ToInt32(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return PlayerXP.GetLevelForPoints(System.Convert.ToInt32(value));
        }
    }
    /// <summary>
    /// A WPF value converter that converts the player class (<see cref="PlayerClassSaveGameData"/>) to a string based off of the path
    /// </summary>
    public class StringToCharacterClassConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                Dictionary<string, PlayerClassSaveGameData> validClasses = BL3Save.ValidClasses;
                PlayerClassSaveGameData val = (PlayerClassSaveGameData)value;

                string result = validClasses.Where(x => x.Value.PlayerClassPath.Equals(val.PlayerClassPath)).First().Key;
                return result;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                PlayerClassSaveGameData data = BL3Save.ValidClasses.Where(x => x.Key.Equals((string)value)).First().Value;
                return data;
            }
            return null;
        }
    }

    /// <summary>
    /// A simple WPF value converter which converts the <see cref="CustomPlayerColorSaveGameData"/> struct to a native C# <see cref="Color"/> struct.
    /// </summary>
    public class CustomPlayerColorToColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

            if (value != null) {
                CustomPlayerColorSaveGameData colorData = (CustomPlayerColorSaveGameData)value;
                if (colorData.UseDefaultColor || colorData.UseDefaultSplitColor) 
                    return Color.FromArgb(0, 0, 0, 0);
                byte r = System.Convert.ToByte(colorData.AppliedColor.X * 255);
                byte g = System.Convert.ToByte(colorData.AppliedColor.Y * 255);
                byte b = System.Convert.ToByte(colorData.AppliedColor.Z * 255);
                Color clr = Color.FromArgb(255, r, g, b);
                return clr;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                Color clr = (Color)value;

                Vec3 vecClr = new Vec3() {
                    X = clr.R / 255f,
                    Y = clr.G / 255f,
                    Z = clr.B / 255f
                };

                CustomPlayerColorSaveGameData colorData = new CustomPlayerColorSaveGameData() {
                    ColorParameter = System.Convert.ToString(parameter),
                    AppliedColor = vecClr,
                    SplitColor = vecClr,
                    UseDefaultColor = clr.A < 255,
                    UseDefaultSplitColor = clr.A < 255
                };

                return colorData;
            }
            return null;
        }
    }
    
    /// <summary>
    /// A simple WPF value converter which converts the passed in customization path to a "human safe" name
    /// </summary>
    public class CustomizationToStringConverter : IValueConverter {
        private Character chx = null;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

            if (value != null && parameter != null) {
                string param = (string)parameter;
                chx = (Character)value;
                List<string> val = chx.SelectedCustomizations;
                string characterName = chx.GetCharacterString();

                string customizationName = "";
                if (param == "heads") {
                    foreach (string customization in val) {
                        // Check if the string is a head
                        if (DataPathTranslations.headAssetPaths.ContainsKey(customization)) {
                            customizationName = DataPathTranslations.headAssetPaths[customization];
                            break;
                        }
                    }

                }
                else if (param == "skins") {
                    foreach (string customization in val) {
                        // Check if the string is a skin
                        if (DataPathTranslations.skinAssetPaths.ContainsKey(customization)) {
                            customizationName = DataPathTranslations.skinAssetPaths[customization];
                            break;
                        }
                    }
                }

                if (customizationName == "") return null;
                return customizationName;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null && chx != null) {
                string param = (string)parameter;
                string selectedCustomization = (string)value;

                List<string> customizations = chx.SelectedCustomizations;
                string characterName = chx.GetCharacterString();
                if (param == "heads") {
                    string headAssetPath = DataPathTranslations.HeadNamesDictionary[characterName].Where(x => DataPathTranslations.headAssetPaths[x] == selectedCustomization).FirstOrDefault();
                    for (int i = 0; i < customizations.Count; i++) {
                        if (DataPathTranslations.headAssetPaths.ContainsKey(customizations[i])) {
                            customizations[i] = headAssetPath;
                            break;
                        }
                    }
                    if (customizations.Count <= 0) customizations.Add(headAssetPath);
                }
                else if (param == "skins") {
                    string skinAssetPath = DataPathTranslations.SkinNamesDictionary[characterName].Where(x => DataPathTranslations.skinAssetPaths[x] == selectedCustomization).FirstOrDefault();
                    for (int i = 0; i < customizations.Count; i++) {
                        if (DataPathTranslations.skinAssetPaths.ContainsKey(customizations[i])) {
                            customizations[i] = skinAssetPath;
                            break;
                        }
                    }
                    if (customizations.Count <= 0) customizations.Add(skinAssetPath);
                }


                return chx;
            }

            if (value == null && chx != null) return chx;
            return null;
        }
    }
    
    /// <summary>
    /// A WPF value converter which converts the given <c>parameter</c> ("money"/"eridium") to the integer value based off of the stored character in <c>value</c>
    /// </summary>
    public class CurrencyToIntegerConverter : IValueConverter {
        private Character chx = null;
        private uint currencyHash = 0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null && parameter != null) {
                string param = (string)parameter;
                chx = (Character)value;
                switch (param) {
                    case "money":
                        currencyHash = DataPathTranslations.MoneyHash;
                        break;
                    case "eridium":
                        currencyHash = DataPathTranslations.EridiumHash;
                        break;
                    default:
                        break;
                }

                if (currencyHash == 0) return 0;

                int? quantity = chx.InventoryCategoryLists.Where(x => x.BaseCategoryDefinitionHash == currencyHash)?.Select(x => x.Quantity).FirstOrDefault();
                if (quantity == null) quantity = 0;

                return (int)quantity;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null && chx != null && currencyHash != 0) {
                if (!chx.InventoryCategoryLists.Where(x => x.BaseCategoryDefinitionHash == currencyHash).Any()) {
                    chx.InventoryCategoryLists.Add(new InventoryCategorySaveData() {
                        BaseCategoryDefinitionHash = currencyHash,
                        Quantity = System.Convert.ToInt32(value)
                    });
                }
                else {
                    chx.InventoryCategoryLists.FirstOrDefault(x => x.BaseCategoryDefinitionHash == currencyHash).Quantity = System.Convert.ToInt32(value);
                }
            }
            return chx;
        }
    }

    public class TravelStationConverter : IMultiValueConverter {
        private Character chx = null;
        private bool bShowDbgMaps = false;
        private int playthroughToShow = 0;

        public static Dictionary<string, string> dbgMapsToShow = new Dictionary<string, string>();
        public static Dictionary<string, string> MapsToShow = new Dictionary<string, string>();

        static TravelStationConverter() {
            foreach (KeyValuePair<string, string> kvp in DataPathTranslations.FastTravelTranslations) {
                bool bDbgMap = DataPathTranslations.UnobtainableFastTravels.Contains(kvp.Key);
                dbgMapsToShow.Add(kvp.Key, bDbgMap ? kvp.Key : kvp.Value);

                if (!bDbgMap) MapsToShow.Add(kvp.Key, kvp.Value);
            }
        }

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            // Check for invalid usages            
            if (value[0].GetType() != typeof(Character) || value[1].GetType() != typeof(bool) || value[2].GetType() != typeof(int)) return null;

            bool bUpdateDictionaries = (chx != (Character)value[0]);

            chx = (Character)value[0];
            bShowDbgMaps = (bool)value[1];
            playthroughToShow = (int)value[2];

            if (playthroughToShow < 0) return null;

            var playthroughData = chx.ActiveTravelStationsForPlaythroughs[Math.Min(playthroughToShow, chx.ActiveTravelStationsForPlaythroughs.Count - 1)].ActiveTravelStations.Select(x => x.ActiveTravelStationName);

            Dictionary<string, string> mapsToShow = bShowDbgMaps ? dbgMapsToShow : MapsToShow;
            List<BoolStringPair> result = new List<BoolStringPair>();
            foreach (KeyValuePair<string, string> kvp in mapsToShow) {
                result.Add(new BoolStringPair(playthroughData.Contains(kvp.Key), kvp.Value));
            }

            result = result.OrderBy(x => x.Value).ToList();

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            return null;
        }
    }
    
    /// <summary>
    /// Converts the most active playthrough of the <see cref="Character"/> stored in <c>value</c> to a string (NVHM/TVHM)
    /// </summary>
    public class PlaythroughToStringConverter : IValueConverter {
        private static readonly string[] indexToString = new string[] {
            "NVHM",
            "TVHM"
        };
        private Character chx = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                chx = (Character)value;
                string param = (string)parameter;
                if (param == "list") return indexToString.ToList();

                return indexToString[chx.LastPlayThroughIndex];
            }

            return indexToString[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return chx;
        }
    }

    /// <summary>
    /// Converts guardian rank data to a valid data grid type (<see cref="ObservableCollection{T}"/>
    /// </summary>
    public class GuardianRankToDataGridConverter : IValueConverter {
        private Profile prf = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            ObservableCollection<StringIntPair> pairs = new ObservableCollection<StringIntPair>();
            if (value != null) {
                prf = (Profile)value;

                foreach (string humanName in DataPathTranslations.GuardianRankRewards.Values) {
                    bool bUpdatedRankData = false;
                    foreach (GuardianRankRewardSaveGameData rankData in prf.GuardianRank.RankRewards) {
                        string human = DataPathTranslations.GetHumanRewardString(rankData.RewardDataPath);
                        if (human == humanName) {
                            Console.WriteLine("Rank Data ({0}): {1}", humanName, rankData.NumTokens);
                            pairs.Add(new StringIntPair(humanName, rankData.NumTokens));
                            bUpdatedRankData = true;
                            break;
                        }
                    }

                    if (!bUpdatedRankData) pairs.Add(new StringIntPair(humanName, 0));
                }
            }

            return pairs;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return prf;
        }
    }
    
    public class KeyToIntegerConverter : IValueConverter {
        private Profile prf = null;

        public static Dictionary<string, uint> stringToHash = new Dictionary<string, uint>() {
            { "GoldenKeys", DataPathTranslations.GoldenKeyHash },
            { "DiamondKeys", DataPathTranslations.DiamondKeyHash },
            { "VaultCard1Keys", DataPathTranslations.VaultCard1Hash },
            { "VaultCard2Keys", DataPathTranslations.VaultCard2Hash }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || parameter == null) return 0;
            
            prf = (Profile)value;
            uint hash = stringToHash[(string)parameter];
            return prf.BankInventoryCategoryLists.Where(x => x.BaseCategoryDefinitionHash == hash).Select(x => x.Quantity).FirstOrDefault();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || parameter == null) return prf;
            uint hash = stringToHash[(string)parameter];

            var prop = prf.BankInventoryCategoryLists.Where(x => x.BaseCategoryDefinitionHash == hash).FirstOrDefault();
            if(prop != default) {
                prop.Quantity = (int)value;
            }
            else {
                prf.BankInventoryCategoryLists.Add(new InventoryCategorySaveData() {
                    BaseCategoryDefinitionHash = hash,
                    Quantity = (int)value
                });
            }

            return prf;
        }
    }
    public class ProfileSDUToIntegerConverter : IValueConverter {
        private Profile prf = null;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            int amountOfSDUs = 0;
            if (value != null && parameter != null) {
                prf = (Profile)value;
                string assetPath = ((string)parameter) == "LostLoot" ? DataPathTranslations.LLSDUAssetPath : DataPathTranslations.BankSDUAssetPath;

                return prf.ProfileSduLists.Where(x => x.SduDataPath == assetPath).Select(x => x.SduLevel).FirstOrDefault();
            }

            return amountOfSDUs;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null && parameter != null) {
                string assetPath = ((string)parameter) == "LostLoot" ? DataPathTranslations.LLSDUAssetPath : DataPathTranslations.BankSDUAssetPath;

                // I'm fairly certain this logic here isn't ACTUALLY needed but I'm adding it just for safety
                var validSDUs = prf.ProfileSduLists.Where(x => x.SduDataPath == assetPath);
                if (validSDUs.Any()) {
                    // Set the value, bit wonky and costly because of LINQ; doesn't matter too much though
                    prf.ProfileSduLists.FirstOrDefault(x => x.SduDataPath == assetPath).SduLevel = (int)value;
                }
                else {
                    // Add the SDU to the list since it clearly wasn't there before.
                    prf.ProfileSduLists.Add(new OakSDUSaveGameData() {
                        SduDataPath = assetPath,
                        SduLevel = (int)value
                    });
                }
            }
            return prf;
        }
    }
    
    /// <summary>
    /// A WPF converter that returns the first object that is not set to null in the multiple values passed in to the multi-value converter
    /// </summary>
    public class MultiElementObjectBinder : IMultiValueConverter {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            foreach (object obj in value) {
                if (obj != null && obj != DependencyProperty.UnsetValue) {
                    return obj;
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            return null;
        }
    }

    public class IntegerToMayhemLevelConverter : IValueConverter {
        private Borderlands3Serial serial = null;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                serial = (Borderlands3Serial)value;
                return serial.GenericParts?.Where(x => x.Contains("Mayhem")).Select(x => System.Convert.ToInt32(x.Split(new string[] { "WeaponMayhemLevel_" }, StringSplitOptions.RemoveEmptyEntries).Last())).LastOrDefault();
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            int newValue = (int)value;

            // Remove all mayhem level based parts when since the converter should do that stuff instead
            serial.GenericParts?.RemoveAll(x => x.Contains("WeaponMayhemLevel_"));

            if (newValue != 0) {
                var assets = ((JArray)InventorySerialDatabase.InventoryDatabase["InventoryGenericPartData"]["assets"]).Children().Select(x => x.Value<string>()).ToList();

                var mayhemParts = assets.Where(x => x.Contains("WeaponMayhemLevel"));

                var mayhemPartToAdd = mayhemParts.Where(x => x.Contains(newValue.ToString("D2"))).FirstOrDefault();

                // Maximum limit for parts is 63 because math
                if (serial?.GenericParts.Count > 62) return serial;

                serial?.GenericParts.Add(mayhemPartToAdd);
            }

            return serial;
        }
    }
    public class ChallengeToBooleanConverter : IValueConverter {
        private Character chx = null;

        public static Dictionary<string, List<string>> stringToChallenges = new Dictionary<string, List<string>>() {
            { "MayhemMode", new List<string>() { "/Game/GameData/Challenges/Account/Challenge_VaultReward_Mayhem.Challenge_VaultReward_Mayhem_C" } },
            { "Analyzer", new List<string>() { "/Game/GameData/Challenges/Account/Challenge_VaultReward_Analyzer.Challenge_VaultReward_Analyzer_C" }  },
            { "Resonator", new List<string>() { "/Game/GameData/Challenges/Account/Challenge_VaultReward_Resonator.Challenge_VaultReward_Resonator_C" } },

            { "Artifacts", new List<string>() { "/Game/GameData/Challenges/Account/Challenge_VaultReward_ArtifactSlot.Challenge_VaultReward_ArtifactSlot_C" } },
            { "ClassMods", new List<string>() { "/Game/GameData/Challenges/Character/Beastmaster/BP_Challenge_Beastmaster_ClassMod.BP_Challenge_Beastmaster_ClassMod_C", "/Game/GameData/Challenges/Character/Gunner/BP_Challenge_Gunner_ClassMod.BP_Challenge_Gunner_ClassMod_C", "/Game/GameData/Challenges/Character/Operative/BP_Challenge_Operative_ClassMod.BP_Challenge_Operative_ClassMod_C", "/Game/GameData/Challenges/Character/Siren/BP_Challenge_Siren_ClassMod.BP_Challenge_Siren_ClassMod_C" } },
        };

        private static List<string> GetChallengePathForChallenge(string challenge) {
            if (stringToChallenges.ContainsKey(challenge))
                return stringToChallenges[challenge];
            else if (stringToChallenges.Any(x => x.Value.Contains(challenge)))
                return stringToChallenges.Where(x => x.Value.Contains(challenge)).First().Value;

            return null;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || parameter == null) return false;
            chx = (Character)value;
            string mode = (string)parameter;

            var challenges = GetChallengePathForChallenge(mode);
            if (challenges == null) return false;

            bool anyCompleted = false;
            foreach (string challengePath in challenges) {
                // Try to find the challenge
                var challenge = chx.ChallengeDatas.Where(x => x.ChallengeClassPath.Equals(challengePath)).FirstOrDefault();

                // Return false if the challenge somehow wasn't able to be found (???)
                if (challenge == default(ChallengeSaveGameData)) continue;

                anyCompleted |= challenge.CurrentlyCompleted;
            }

            return anyCompleted;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || parameter == null) return chx;
            string mode = (string)parameter;
            var challenges = GetChallengePathForChallenge(mode);
            if (challenges == null) return chx;

            // Iterate over all of the possible challenges that map to the key.
            foreach (string challengePath in challenges) {
                bool bVal = (bool)value;

                // Try to find the challenge
                var challenge = chx.ChallengeDatas.Where(x => x.ChallengeClassPath.Equals(challengePath)).FirstOrDefault();
                if (challenge == default(ChallengeSaveGameData)) {
                    chx.ChallengeDatas.Add(new ChallengeSaveGameData() {
                        IsActive = !bVal,
                        ChallengeRewardInfoes = new List<OakChallengeRewardSaveGameData>(),
                        ChallengeClassPath = challengePath,
                        CompletedCount = System.Convert.ToInt32(bVal),
                        CompletedProgressLevel = 0,
                        CurrentlyCompleted = bVal,
                        ProgressCounter = 0,
                        StatInstanceStates = new List<ChallengeStatSaveGameData>()
                    });
                }
                else {
                    challenge.CurrentlyCompleted = bVal;
                    challenge.IsActive = !bVal;
                    challenge.CompletedCount = System.Convert.ToInt32(bVal);
                    challenge.ProgressCounter = 0;
                    challenge.CompletedProgressLevel = 0;
                }
            }

            if(mode == "ClassMods") {
                ItemSlotConverter.GetEquippedSlotForName("ClassMod").Enabled = (bool)value;
                if (!((bool)value)) 
                    ItemSlotConverter.GetEquippedSlotForName("ClassMod").InventoryListIndex = -1;
            }
            else if(mode == "Artifacts") {
                ItemSlotConverter.GetEquippedSlotForName("Artifact").Enabled = (bool)value;
                if (!((bool)value))
                    ItemSlotConverter.GetEquippedSlotForName("Artifact").InventoryListIndex = -1;
            }

            return chx;
        }
    }
    public class ItemSlotConverter : IValueConverter {
        public static Character chx = null;

        public static EquippedInventorySaveGameData GetEquippedSlotForName(string slot) {
            if (!DataPathTranslations.SlotToPathDictionary.ContainsKey(slot)) return null;
            string slotDataPath = DataPathTranslations.SlotToPathDictionary[slot];
            var slotToEdit = chx.EquippedInventoryLists.Where(x => x.SlotDataPath == slotDataPath).FirstOrDefault();
            // Somehow unable to find the slot...?
            if (slotToEdit == default(EquippedInventorySaveGameData)) return null;

            return slotToEdit;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || parameter == null) return false;
            chx = (Character)value;
            string slot = (string)parameter;
            var slotToEdit = GetEquippedSlotForName(slot);

            return slotToEdit.Enabled;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || parameter == null) return chx;
            string slot = (string)parameter;
            var slotToEdit = GetEquippedSlotForName(slot);

            if (slotToEdit == null) {
                chx.EquippedInventoryLists.Add(new EquippedInventorySaveGameData() {
                    Enabled = true,
                    InventoryListIndex = -1,
                    SlotDataPath = DataPathTranslations.SlotToPathDictionary[slot],
                    TrinketDataPath = ""
                });
            }
            else {
                bool bValue = (bool)value;
                slotToEdit.Enabled = bValue;
                // When disabling the slot, probably best to remove the equipped item as well.
                if (!bValue) {
                    slotToEdit.InventoryListIndex = -1;
                    slotToEdit.TrinketDataPath = "";
                }
            }

            return chx;
        }
    }
    public class CalcConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            double val = (double)value;
            string expressionString = parameter as string;
            string type = expressionString.Split(';').First();
            string offset = expressionString.Split(';').Last();

            switch(type) {
                case "Subtract":
                case "Minus":
                    return val - System.Convert.ToDouble(offset);
                default:
                    throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    public class SerialPartConverter : IValueConverter {
        private Borderlands3Serial serial;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || parameter == null) return new List<string>();
            serial = (Borderlands3Serial)value;
            List<string> fullNameParts = (List<string>)serial.GetType().GetProperty((string)parameter).GetValue(serial, null);

            var parts = fullNameParts.Select(x => x.Split('.').Last()).ToList();

            return parts;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    #endregion

    public class StringIntPair {
        public string str { get; set; } = "";
        public double Value { get; set; } = 0;
        public StringIntPair(string name, int val) {
            str = name;
            Value = val;
        }
    }
    public class BoolStringPair {
        public bool booleanVar { get; set; } = false;
        public string Value { get; set; } = "";
        public BoolStringPair(bool var, string val) {
            booleanVar = var;
            Value = val;
        }
    }
    public class StringSerialPair {
        public string Val1 { get; set; } = "";
        public Borderlands3Serial Val2 { get; set; } = null;

        public StringSerialPair(string val1, Borderlands3Serial val2) {
            Val1 = val1;
            Val2 = val2;
        }

        public override string ToString() {
            return Val2.UserFriendlyName;
        }

        public static implicit operator Borderlands3Serial(StringSerialPair x) {
            return x.Val2;
        }
    }

    public static class Helpers {
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var queue = new Queue<DependencyObject>(new[] { parent });

            while (queue.Any()) {
                var reference = queue.Dequeue();
                var count = VisualTreeHelper.GetChildrenCount(reference);

                for (var i = 0; i < count; i++) {
                    var child = VisualTreeHelper.GetChild(reference, i);
                    if (child is T children)
                        yield return children;

                    queue.Enqueue(child);
                }
            }
        }

        /// <summary>
        /// Recursively finds the specified named parent in a control hierarchy
        /// </summary>
        /// <typeparam name="T">The type of the targeted Find</typeparam>
        /// <param name="child">The child control to start with</param>
        /// <returns></returns>
        public static T FindParent<T>(this DependencyObject child) where T : DependencyObject {
            if (child == null) return null;

            T foundParent = null;
            var currentParent = VisualTreeHelper.GetParent(child);

            do {
                var frameworkElement = currentParent as FrameworkElement;
                if (frameworkElement is T) {
                    foundParent = (T)currentParent;
                    break;
                }

                currentParent = VisualTreeHelper.GetParent(currentParent);

            } while (currentParent != null);

            return foundParent;
        }
    }
}