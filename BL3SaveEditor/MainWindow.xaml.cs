using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using BL3Tools;
using BL3Tools.GameData;
using AdonisUI;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Generic;
using BL3SaveEditor.Helpers;
using System.Collections.ObjectModel;
using BL3Tools.GameData.Items;

namespace BL3SaveEditor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        
        #region Databinding Data
        public int maximumXP { get; } = PlayerXP._XPMaximumLevel;
        public int minimumXP { get; } = PlayerXP._XPMinimumLevel;
        public int maximumMayhemLevel { get; } = MayhemLevel.MaximumLevel;
        public bool bSaveLoaded { get; set; } = false;
        public bool showDebugMaps { get; set; } = false;

        public ListCollectionView ValidPlayerClasses { 
            get {
                return new ListCollectionView(BL3Save.ValidClasses.Keys.ToList());
            }
        }
        public ListCollectionView ValidPlayerHeads {
            get {
                // Hasn't loaded a save yet
                if (saveGame == null) return new ListCollectionView(new List<string>() { "" });
                
                string characterClassPath = saveGame.Character.PlayerClassData.PlayerClassPath;
                var kvp = BL3Save.ValidClasses.Where(x => x.Value.PlayerClassPath == characterClassPath);
                
                // Unknown character?
                if(!kvp.Any()) return new ListCollectionView(new List<string>() { "" });
                string characterName = kvp.First().Key;

                var headAssetPaths = DataPathTranslations.HeadNamesDictionary[characterName];
                List<string> headNames = new List<string>();
                foreach(string assetPath in headAssetPaths) {
                    string headName = DataPathTranslations.headAssetPaths[assetPath];
                    headNames.Add(headName);
                }

                return new ListCollectionView(headNames);
            }
        }
        public ListCollectionView ValidPlayerSkins {
            get {
                // Hasn't loaded a save yet
                if (saveGame == null) return new ListCollectionView(new List<string>() { "" });

                string characterClassPath = saveGame.Character.PlayerClassData.PlayerClassPath;
                var kvp = BL3Save.ValidClasses.Where(x => x.Value.PlayerClassPath == characterClassPath);

                // Unknown character?
                if (!kvp.Any()) return new ListCollectionView(new List<string>() { "" });
                string characterName = kvp.First().Key;

                var skinAssetPaths = DataPathTranslations.SkinNamesDictionary[characterName];
                List<string> skinNames = new List<string>();
                foreach (string assetPath in skinAssetPaths) {
                    string headName = DataPathTranslations.skinAssetPaths[assetPath];
                    skinNames.Add(headName);
                }

                return new ListCollectionView(skinNames);
            }
        }

        public ListCollectionView SlotItems {
            get {
                // Hasn't loaded a save/profile yet
                if (saveGame == null && profile == null) return null;
                ObservableCollection<StringSerialPair> px = new ObservableCollection<StringSerialPair>();
                List<int> usedIndexes = new List<int>();
                List<Borderlands3Serial> itemsToSearch = null;

                if (saveGame != null) {
                    var equippedItems = saveGame.Character.EquippedInventoryLists;
                    foreach (var item in equippedItems) {
                        if (!item.Enabled || item.InventoryListIndex < 0 || item.InventoryListIndex > saveGame.InventoryItems.Count - 1) continue;
                        usedIndexes.Add(item.InventoryListIndex);
                        px.Add(new StringSerialPair("Equipped", saveGame.InventoryItems[item.InventoryListIndex]));
                    }
                    itemsToSearch = saveGame.InventoryItems;
                }
                else {
                    itemsToSearch = profile.BankItems;
                }

                for (int i = 0; i < itemsToSearch.Count; i++) {
                    // Ignore already used (equipped) indexes
                    if (usedIndexes.Contains(i)) continue;
                    var serial = itemsToSearch[i];

                    // Split the items out into groups, assume weapons because they're the most numerous and different
                    string itemType = "Weapon";
                    if (serial.InventoryKey.Contains("_ClassMod")) itemType = "Class Mods";
                    else if (serial.InventoryKey.Contains("_Artifact")) itemType = "Artifacts";
                    else if (serial.InventoryKey.Contains("_Shield")) itemType = "Shields";

                    px.Add(new StringSerialPair(itemType, serial));
                }

                ListCollectionView vx = new ListCollectionView(px);
                // Group them by the "type"
                vx.GroupDescriptions.Add(new PropertyGroupDescription("Val1"));
                return vx;
            }
        }

        public Borderlands3Serial SelectedSerial { get; set; }

        public int MaximumBankSDUs { get { return SDU.MaximumBankSDUs;  } }
        public int MaximumLostLootSDUs { get { return SDU.MaximumLostLoot;  } }
        #endregion

        private static Debug.DebugConsole dbgConsole;
        private bool bLaunched = false;

        /// <summary>
        /// The current profile object; will be null if we haven't loaded a profile
        /// </summary>
        public BL3Profile profile { get; set; } = null;

        /// <summary>
        /// The current save game object; will be null if we loaded a profile instead of a save game
        /// </summary>
        public BL3Save saveGame { get; set; } = null;

        public MainWindow() {
            this.profile = null;
            this.saveGame = null;

            InitializeComponent();
            DataContext = this;

            // Restore the dark mode state from last run
            bLaunched = true;
            CheckBox darkBox = (CheckBox)FindName("DarkModeBox");
            darkBox.IsChecked = Properties.Settings.Default.bDarkModeEnabled;
            DarkModeBox_Checked(darkBox, null);

            dbgConsole = new Debug.DebugConsole();

            ((TabControl)FindName("TabCntrl")).SelectedIndex = ((TabControl)FindName("TabCntrl")).Items.Count-1;

            var test = Borderlands3Serial.DecryptSerial("BL3(AwAAAAD7u4A86PMBE4wakIcjwVhsgKJLUOHyoFJxhxAAAAAAAGCMAQ==)");
            var bar = test.EncryptSerial();
        }

        #region Toolbar Interaction
        private void NewSaveBtn_Click(object sender, RoutedEventArgs e) {
            Console.WriteLine("Made a new save!");
        }

        private void OpenSaveBtn_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog fileDialog = new OpenFileDialog {
                Title = "Select BL3 Save/Profile",
                Filter = "BL3 Save/Profile|*.sav",
                InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Borderlands 3", "Saved", "SaveGames")
            };

            if (fileDialog.ShowDialog() == true) {
                object saveObj = BL3Tools.BL3Tools.LoadFileFromDisk(fileDialog.FileName);

                Console.WriteLine($"Reading a save of type: {saveObj.GetType()}");

                if (saveObj.GetType() == typeof(BL3Profile)) {
                    profile = (BL3Profile)saveObj;
                    saveGame = null;
                    bSaveLoaded = false;
                }
                else {
                    saveGame = (BL3Save)saveObj;
                    profile = null;
                    bSaveLoaded = true;
                }
            }

            ((TabItem)FindName("RawTabItem")).IsEnabled = true;
            ((TabItem)FindName("InventoryTabItem")).IsEnabled = true;

            ((Button)FindName("SaveSaveBtn")).IsEnabled = true;
            ((Button)FindName("SaveAsSaveBtn")).IsEnabled = true;

            // Refresh the bindings on the GUI
            DataContext = null;
            DataContext = this;
        }

        private void SaveSaveBtn_Click(object sender, RoutedEventArgs e) {
            Console.WriteLine("Saving save...");

            if (saveGame != null) BL3Tools.BL3Tools.WriteFileToDisk(saveGame);
            else if (profile != null) BL3Tools.BL3Tools.WriteFileToDisk(profile);

            DataContext = null;
            DataContext = this;
        }

        private void SaveAsSaveBtn_Click(object sender, RoutedEventArgs e) {
            Console.WriteLine("Saving save as...");
            SaveFileDialog saveFileDialog = new SaveFileDialog() {
                Title = "Save BL3 Save/Profile",
                Filter = "BL3 Save/Profile|*.sav",
                InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Borderlands 3", "Saved", "SaveGames")
            };

            // Update the file like this so that way once you do a save as, it still changes the saved-as file instead of the originally opened file.
            if(saveFileDialog.ShowDialog() == true) {
                if (saveGame != null) saveGame.filePath = saveFileDialog.FileName;
                else if (profile != null) profile.filePath = saveFileDialog.FileName;
            }

            if (saveGame != null) BL3Tools.BL3Tools.WriteFileToDisk(saveGame);
            else if (profile != null) BL3Tools.BL3Tools.WriteFileToDisk(profile);

            // Refresh data context for safety
            DataContext = null;
            DataContext = this;
        }

        private void DbgBtn_Click(object sender, RoutedEventArgs e) {
            dbgConsole.Show();
        }

        private void Info_Click(object sender, RoutedEventArgs e) {

        }


        #endregion

        private void AdonisWindow_Closed(object sender, EventArgs e) {
            Console.WriteLine("Closing program...");

            // Release the console writer on close to avoid memory issues
            dbgConsole.consoleRedirectWriter.Release();

            // Need to set this boolean in order to actually close the program
            dbgConsole.bClose = true;
            dbgConsole.Close();
        }

        #region Theme Toggling
        private void DarkModeBox_Checked(object sender, RoutedEventArgs e) {
            if(bLaunched) {
                bool bChecked = (bool)((CheckBox)sender).IsChecked;
                ResourceLocator.SetColorScheme(Application.Current.Resources, bChecked ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);

                // Update the settings now
                Properties.Settings.Default.bDarkModeEnabled = bChecked;
                Properties.Settings.Default.Save();

            }
        }
        #endregion

        #region Interactions
        private void RandomizeGUIDBtn_Click(object sender, RoutedEventArgs e) {
            Guid newGUID = Guid.NewGuid();
            GUIDTextBox.Text = newGUID.ToString().Replace("-","").ToUpper();
        }

        #region Fast Travel
        private void DbgMapBox_StateChange(object sender, RoutedEventArgs e) {
            VisitedTeleportersGrpBox.DataContext = null;
            VisitedTeleportersGrpBox.DataContext = this;
        }

        private void FastTravelChkBx_StateChanged(object sender, RoutedEventArgs e) {
            if (sender == null || saveGame == null) return;
            CheckBox senderBx = (CheckBox)sender;
            if (senderBx.Content.GetType() != typeof(TextBlock)) return;

            bool bFastTravelEnabled = senderBx.IsChecked == true;
            string fastTravelToChange = ((senderBx.Content as TextBlock).Text);
            string assetPath = DataPathTranslations.FastTravelTranslations.FirstOrDefault(x => x.Value == fastTravelToChange).Key;
            
            Console.WriteLine("Changed state of {0} ({2}) to {1}", fastTravelToChange, bFastTravelEnabled, assetPath);
            int amtOfPlaythroughs = saveGame.Character.ActiveTravelStationsForPlaythroughs.Count - 1;
            int playthroughIndex = SelectedPlaythroughBox.SelectedIndex;

            if (amtOfPlaythroughs < SelectedPlaythroughBox.SelectedIndex) {
                saveGame.Character.ActiveTravelStationsForPlaythroughs.Add(new OakSave.PlaythroughActiveFastTravelSaveData());
            }

            var travelStations = saveGame.Character.ActiveTravelStationsForPlaythroughs[playthroughIndex].ActiveTravelStations;
            if(bFastTravelEnabled) {
                travelStations.Add(new OakSave.ActiveFastTravelSaveData() {
                    ActiveTravelStationName = assetPath,
                    Blacklisted = false
                });
            }
            else {
                travelStations.RemoveAll(x => x.ActiveTravelStationName == assetPath);
            }

            return;
        }

        private void EnableAllTeleportersBtn_Click(object sender, RoutedEventArgs e) {
            foreach (BoolStringPair bsp in TeleportersItmCntrl.Items) {
                ContentPresenter presenter = (ContentPresenter)TeleportersItmCntrl.ItemContainerGenerator.ContainerFromItem(bsp);
                presenter.ApplyTemplate();
                CheckBox chkBox = presenter.ContentTemplate.FindName("FastTravelChkBx", presenter) as CheckBox;
                chkBox.IsChecked = true;
            }
        }

        private void DisableAllTeleportersBtn_Click(object sender, RoutedEventArgs e) {
            foreach (BoolStringPair bsp in TeleportersItmCntrl.Items) {
                ContentPresenter presenter = (ContentPresenter)TeleportersItmCntrl.ItemContainerGenerator.ContainerFromItem(bsp);
                presenter.ApplyTemplate();
                CheckBox chkBox = presenter.ContentTemplate.FindName("FastTravelChkBx", presenter) as CheckBox;
                chkBox.IsChecked = false;
            }
        }

        #endregion

        #region Backpack / Bank

        private void BackpackListView_Selected(object sender, RoutedEventArgs e) {
            if (BackpackListView.Items.Count <= 1 || BackpackListView.SelectedValue == null) return;
            StringSerialPair svp = (StringSerialPair)(((ListView)sender).SelectedValue);
            SelectedSerial = svp.Val2;

            (sender as ListView).ScrollIntoView(svp);
        }

        private void CopyItem_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) {
            StringSerialPair svp = (StringSerialPair)BackpackListView.SelectedValue;
            SelectedSerial = svp.Val2;
            // Be nice and copy the code with a 0 seed (:
            string serialString = SelectedSerial.EncryptSerial(0);
            Console.WriteLine("Copying selected item code: {0}", serialString);
            
            // Copy it to the clipboard
            Clipboard.SetText(serialString);
        }

        #endregion

        #endregion


    }
}
