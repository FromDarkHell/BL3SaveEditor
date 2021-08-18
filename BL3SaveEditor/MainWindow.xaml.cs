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
using MessageBox = AdonisUI.Controls.MessageBox;
using System.Windows.Input;

namespace BL3SaveEditor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {

        #region Databinding Data
        public static RoutedCommand DuplicateCommand { get; } = new RoutedCommand();

        public int maximumXP { get; } = PlayerXP._XPMaximumLevel;
        public int minimumXP { get; } = PlayerXP._XPMinimumLevel;
        public int maximumMayhemLevel { get; } = MayhemLevel.MaximumLevel;
        public bool bSaveLoaded { get; set; } = false;
        public bool showDebugMaps { get; set; } = false;
        public bool ForceLegitParts { get; set; } = true;

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

                    if (serial.InventoryKey == null) continue;
                    else if (serial.InventoryKey.Contains("_ClassMod")) itemType = "Class Mods";
                    else if (serial.InventoryKey.Contains("_Artifact")) itemType = "Artifacts";
                    else if (serial.InventoryKey.Contains("_Shield")) itemType = "Shields";
                    else if (serial.InventoryKey.Contains("_Customization")) itemType = "Customizations";
                    else if (serial.InventoryKey.Contains("_GrenadeMod_")) itemType = "Grenades";

                    px.Add(new StringSerialPair(itemType, serial));
                }

                ListCollectionView vx = new ListCollectionView(px);
                // Group them by the "type"
                vx.GroupDescriptions.Add(new PropertyGroupDescription("Val1"));
                return vx;
            }
        }
        public ListCollectionView ValidBalances {
            get {
                if (SelectedSerial == null) return null;

                string inventoryKey = SelectedSerial.InventoryKey;
                var balances = InventoryKeyDB.KeyDictionary.Where(x => x.Value.Equals(inventoryKey) && !x.Key.Contains("partset")).Select(x => InventorySerialDatabase.GetShortNameFromBalance(x.Key)).Where(x => !string.IsNullOrEmpty(x)).ToList();

                return new ListCollectionView(balances);
            }
        }
        public string SelectedBalance {
            get {
                if (SelectedSerial == null) return null;                
                return InventorySerialDatabase.GetShortNameFromBalance(SelectedSerial.Balance);
            }
            set {
                if (SelectedSerial == null) return;
                SelectedSerial.Balance = InventorySerialDatabase.GetBalanceFromShortName(value);
            }
        }
        public ListCollectionView ValidManufacturers {
            get {
                return new ListCollectionView(InventorySerialDatabase.GetManufacturers());
            }
        }
        public string SelectedManufacturer {
            get {
                if (SelectedSerial == null) return null;
                string Manufacturer = SelectedSerial.Manufacturer;
                
                List<string> shortNames = InventorySerialDatabase.GetManufacturers();
                List<string> longNames = InventorySerialDatabase.GetManufacturers(false);

                return shortNames[longNames.IndexOf(Manufacturer)];
            }
            set {
                if (SelectedSerial == null) return;
                
                List<string> shortNames = InventorySerialDatabase.GetManufacturers();
                List<string> longNames = InventorySerialDatabase.GetManufacturers(false);

                SelectedSerial.Manufacturer = longNames[shortNames.IndexOf(value)];
            }
        }
        public ListCollectionView InventoryDatas {
            get {
                return new ListCollectionView(InventorySerialDatabase.GetInventoryDatas());
            }
        }
        public string SelectedInventoryData {
            get {
                return SelectedSerial?.InventoryData.Split('.').LastOrDefault();
            }
            set {
                if (SelectedSerial == null) return;

                List<string> shortNames = InventorySerialDatabase.GetInventoryDatas();
                List<string> longNames = InventorySerialDatabase.GetInventoryDatas(false);
                SelectedSerial.InventoryData = longNames[shortNames.IndexOf(value)];
            }
        }
        public Borderlands3Serial SelectedSerial { get; set; }

        public ListCollectionView ValidParts {
            get {
                if (SelectedSerial == null) return null;

                List<string> validParts = InventorySerialDatabase.GetPartsForInvKey(SelectedSerial.InventoryKey);
                return new ListCollectionView(validParts.Select(x => x.Split('.').Last()).ToList());
            }
        }

        public ListCollectionView ValidGenerics {
            get {
                if (SelectedSerial == null) return null;
                List<string> validParts = InventorySerialDatabase.GetPartsForInvKey("InventoryGenericPartData");
                return new ListCollectionView(validParts.Select(x => x.Split('.').Last()).ToList());
            }
        }

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
        private void RefreshBackpackView() {
            // Need to change the data context real quick to make the GUI update
            var grid = ((Grid)FindName("SerialContentsGrid"));
            grid.DataContext = null;
            grid.DataContext = this;
        }

        private void BackpackListView_Selected(object sender, EventArgs e) {
            if (BackpackListView.Items.Count <= 1 || BackpackListView.SelectedValue == null) return;
            ListView listView = (sender as ListView);
            StringSerialPair svp = (StringSerialPair)listView.SelectedValue;
            SelectedSerial = svp.Val2;
            
            // Scroll to the selected item (in case of duplication / etc)
            listView.ScrollIntoView(listView.SelectedItem);

            RefreshBackpackView();

        }
        private void BackpackListView_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (e.Handled) return;

            // This janky bit of logic allows us to scroll on hover over the items of the ListView as well :/
            var listview = (sender as ListView);
            var scrollViewer = listview.FindVisualChildren<ScrollViewer>().First();
            // Multiply the value by 0.7 because just the delta value can be a bit much tbh
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (e.Delta * 0.7) );

            // Make sure no other elements can handle the events
            e.Handled = true;
        }

        private void NewItemBtn_Click(object sender, RoutedEventArgs e) {
            Controls.ItemBalanceChanger changer = new Controls.ItemBalanceChanger() { Owner = this };
            changer.ShowDialog();

            // The user actually hit the save button and we have data about the item
            if (changer.SelectedInventoryData != null) {
                var serial = Borderlands3Serial.CreateSerialFromBalanceData(changer.SelectedBalance);
                if (serial == null) return;

                serial.InventoryData = changer.SelectedInventoryData;
                // Set a manufacturer so that way the bindings don't lose their mind
                serial.Manufacturer = InventorySerialDatabase.GetManufacturers().FirstOrDefault();

                if (profile == null) saveGame.InventoryItems.Add(serial);
                else profile.BankItems.Add(serial);

                BackpackListView.ItemsSource = null;
                BackpackListView.ItemsSource = SlotItems;
                RefreshBackpackView();
            }


        }
        private void PasteCodeBtn_Click(object sender, RoutedEventArgs e) {
            string serialCode = Clipboard.GetText();

            try {
                Borderlands3Serial item = Borderlands3Serial.DecryptSerial(serialCode);

                if (profile == null) saveGame.InventoryItems.Add(item);
                else profile.BankItems.Add(item);

                BackpackListView.Items.Refresh();

                var selectedValue = BackpackListView.Items.Cast<StringSerialPair>().Where(x => ReferenceEquals(x.Val2, item)).LastOrDefault();
                BackpackListView.SelectedValue = selectedValue;
            }
            catch(BL3Tools.BL3Tools.BL3Exceptions.SerialParseException ex) {
                string message = ex.Message;
                Console.WriteLine($"Exception ({message}) parsing serial: {ex.ToString()}");
                if (ex.knowCause)
                    MessageBox.Show($"Error parsing serial: {ex.Message}", "Serial Parse Exception", AdonisUI.Controls.MessageBoxButton.OK, AdonisUI.Controls.MessageBoxImage.Error);
            }
        }
        private void SyncEquippedBtn_Click(object sender, RoutedEventArgs e) {
            if (saveGame == null) return;
            int levelToSync = PlayerXP.GetLevelForPoints(saveGame.Character.ExperiencePoints);
            foreach(var equipData in saveGame.Character.EquippedInventoryLists) {
                if (!equipData.Enabled || equipData.InventoryListIndex < 0 || equipData.InventoryListIndex > saveGame.InventoryItems.Count - 1) continue;
                
                // Sync the level onto the item
                saveGame.InventoryItems[equipData.InventoryListIndex].Level = levelToSync;
            }
            RefreshBackpackView();
        }
        private void SyncAllBtn_Click(object sender, RoutedEventArgs e) {
            int levelToSync = -1;
            if (profile != null) {
                var msgBox = new Controls.IntegerMessageBox("Please enter a level to sync your items for syncing", "Level: ", 0, maximumXP, maximumXP);
                msgBox.Owner = this;
                msgBox.ShowDialog();
                if (!msgBox.Succeeded) return;

                levelToSync = msgBox.Result;
            }
            else 
                levelToSync = PlayerXP.GetLevelForPoints(saveGame.Character.ExperiencePoints);
            
            foreach (Borderlands3Serial item in (profile == null ? saveGame.InventoryItems : profile.BankItems)) {
                Console.WriteLine($"Syncing level for item ({item.UserFriendlyName}) from {item.Level} to {levelToSync}");
                item.Level = levelToSync;
            }
            RefreshBackpackView();
        }
        private void CopyItem_Executed(object sender, ExecutedRoutedEventArgs e) {
            StringSerialPair svp = (StringSerialPair)BackpackListView.SelectedValue;
            SelectedSerial = svp.Val2;

            // Be nice and copy the code with a 0 seed (:
            string serialString = SelectedSerial.EncryptSerial(0);
            Console.WriteLine("Copying selected item code: {0}", serialString);

            // Copy it to the clipboard
            Clipboard.SetDataObject(serialString);
        }
        private void PasteItem_Executed(object sender, ExecutedRoutedEventArgs e) {
            PasteCodeBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        private void DuplicateItem_Executed(object sender, ExecutedRoutedEventArgs e) {
            // This basically just clicks both the copy and paste button
            CopyItem_Executed(null, e);
            PasteCodeBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        private void ChangeTypeBtn_Click(object sender, RoutedEventArgs e) {
            var itemKey = InventoryKeyDB.GetKeyForBalance(InventorySerialDatabase.GetBalanceFromShortName(SelectedBalance));
            var itemType = InventoryKeyDB.ItemTypeToKey.Where(x => x.Value.Contains(itemKey)).Select(x => x.Key).FirstOrDefault();

            Controls.ItemBalanceChanger changer = new Controls.ItemBalanceChanger(itemType, SelectedBalance) { Owner = this };

            changer.ShowDialog();

            // The user actually hit the save button and we have data about the item
            if (changer.SelectedInventoryData != null) {
                SelectedInventoryData = changer.SelectedInventoryData;
                SelectedBalance = changer.SelectedBalance;

                RefreshBackpackView();
            }
        }
        private void AddItemPartBtn_Click(object sender, RoutedEventArgs e) {
            if (SelectedSerial == null) return;
            // Update the valid parts
            ValidParts.Refresh();

            SelectedSerial.Parts.Add(ValidParts.SourceCollection.Cast<string>().FirstOrDefault());
            ListView obj = ((ListView)FindName("PartsListView"));
            obj.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();
        }
        private void DeleteItemPartBtn_Click(object sender, RoutedEventArgs e) {
            ListView obj = ((ListView)FindName("PartsListView"));

            if (obj.SelectedIndex != -1) 
                SelectedSerial.Parts.RemoveAt(obj.SelectedIndex);
            
            // Update the valid parts
            ValidParts.Refresh();
            obj.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();
        }

        // This bit of logic is here so that way the ListView's selected value stays up to date with the combobox's selected value :/
        private void ComboBox_DropDownChanged(object sender, EventArgs e) {
            ComboBox box = ((ComboBox)sender);
            ListView parent = box.FindParent<ListView>();

            parent.SelectedValue = box.SelectedValue;
        }

        private string GetSelectedPart(string type, object sender, SelectionChangedEventArgs e) {
            if (e.Handled || e.RemovedItems.Count < 1) return null;
            ComboBox box = ((ComboBox)sender);

            var newPart = e.AddedItems.Cast<string>().FirstOrDefault();
            var oldPart = e.RemovedItems.Cast<string>().FirstOrDefault();
            if (newPart == default || oldPart == default) return null;

            Console.WriteLine($"Changed \"{oldPart}\" to \"{newPart}\"");
            ListView parent = box.FindParent<ListView>();
            if (parent.SelectedIndex == -1) return null;

            string assetCat = (type == "Parts" ? SelectedSerial.InventoryKey : "InventoryGenericPartData");
            string fullName = InventorySerialDatabase.GetPartFromShortName(assetCat, newPart);
            if (fullName == default) fullName = newPart;

            return fullName;
        }

        private void ItemPart_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ListView parent = ((ComboBox)sender).FindParent<ListView>();
            string propertyName = parent.Name.Split(new string[] { "ListView" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (propertyName == default) return;

            string fullName = GetSelectedPart(propertyName, sender, e);
            if (fullName == null) return;

            List<string>  parts = (List<string>)SelectedSerial.GetType().GetProperty((string)propertyName).GetValue(SelectedSerial, null);
            parts[parent.SelectedIndex] = fullName;
        }

        #endregion

        #endregion


    }
}
