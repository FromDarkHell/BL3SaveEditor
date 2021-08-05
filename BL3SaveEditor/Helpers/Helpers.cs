using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Linq;
using BL3Tools;
using BL3Tools.GameData;
using OakSave;
using System.Windows.Media;

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

    #region Specialized Converters
    public class EXPointToLevelConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return PlayerXP.GetLevelForPoints(System.Convert.ToInt32(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return PlayerXP.GetPointsForXPLevel(System.Convert.ToInt32(value));
        }
    }
    public class LevelToEXPointConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return PlayerXP.GetPointsForXPLevel(System.Convert.ToInt32(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return PlayerXP.GetLevelForPoints(System.Convert.ToInt32(value));
        }
    }
    public class StringToCharacterClassConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value != null) {
                Dictionary<string, PlayerClassSaveGameData> validClasses = BL3Save.ValidClasses;
                PlayerClassSaveGameData val = (PlayerClassSaveGameData)value;

                string result = validClasses.Where(x => x.Value.PlayerClassPath.Equals(val.PlayerClassPath)).First().Key;
                return result;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value != null) {
                PlayerClassSaveGameData data = BL3Save.ValidClasses.Where(x => x.Key.Equals((string)value)).First().Value;
                return data;
            }
            return null;
        }


    }
    public class CustomPlayerColorToColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            
            if(value != null) {
                CustomPlayerColorSaveGameData colorData = (CustomPlayerColorSaveGameData)value;
                byte r = System.Convert.ToByte(colorData.AppliedColor.X * 255);
                byte g = System.Convert.ToByte(colorData.AppliedColor.Y * 255);
                byte b = System.Convert.ToByte(colorData.AppliedColor.Z * 255);
                Color clr = Color.FromArgb(255, r, g, b);
                return clr;
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value != null) {
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
                    UseDefaultColor = false,
                    UseDefaultSplitColor = false
                };

                return colorData;
            }
            return null;
        }
    }
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
                    foreach(string customization in val) {
                        // Check if the string is a head
                        if(DataPathTranslations.headAssetPaths.ContainsKey(customization)) {
                            customizationName = DataPathTranslations.headAssetPaths[customization];
                            break;
                        }
                    }

                }
                else if(param == "skins") {
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
                if(param == "heads") {
                    string headAssetPath = DataPathTranslations.HeadNamesDictionary[characterName].Where(x => DataPathTranslations.headAssetPaths[x] == selectedCustomization).FirstOrDefault();
                    for (int i = 0; i < customizations.Count; i++) {
                        if(DataPathTranslations.headAssetPaths.ContainsKey(customizations[i])) {
                            customizations[i] = headAssetPath;
                            break;
                        }
                    }
                    if(customizations.Count <= 0) customizations.Add(headAssetPath);
                }
                else if(param == "skins") {
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
    public class CurrencyToIntegerConverter : IValueConverter {
        private Character chx = null;
        private uint currencyHash = 0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null && parameter != null) {
                string param = (string)parameter;
                chx = (Character)value;
                switch (param) {
                    case "golden":
                        currencyHash = DataPathTranslations.GoldenKeyHash;
                        break;
                    case "money":
                        currencyHash = DataPathTranslations.MoneyHash;
                        break;
                    case "eridium":
                        currencyHash = DataPathTranslations.EridiumHash;
                        break;
                    case "diamond":
                        currencyHash = DataPathTranslations.DiamondKeyHash;
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
                if(!chx.InventoryCategoryLists.Where(x => x.BaseCategoryDefinitionHash == currencyHash).Any()) {
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

            var playthroughData = chx.ActiveTravelStationsForPlaythroughs[Math.Min(playthroughToShow, chx.ActiveTravelStationsForPlaythroughs.Count-1)].ActiveTravelStations.Select(x => x.ActiveTravelStationName);

            Dictionary<string, string> mapsToShow = bShowDbgMaps ? dbgMapsToShow : MapsToShow;
            List<BoolStringPair> result = new List<BoolStringPair>();
            foreach(KeyValuePair<string, string> kvp in mapsToShow) {
                result.Add(new BoolStringPair(playthroughData.Contains(kvp.Key), kvp.Value));
            }

            result = result.OrderBy(x => x.Value).ToList();

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            return null;
        }
    }

    public class PlaythroughToStringConverter : IValueConverter {
        private static readonly string[] indexToString = new string[] {
            "NVHM",
            "TVHM"
        };
        private Character chx = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value != null) {
                chx = (Character)value;
                string param = (string)parameter;
                if(param == "list") return indexToString.ToList();

                return indexToString[chx.LastPlayThroughIndex];
            }

            return indexToString[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return chx;
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

}