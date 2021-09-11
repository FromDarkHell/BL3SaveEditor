using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using BL3Tools.GameData.Items;

namespace BL3SaveEditor.Controls {


    /// <summary>
    /// A simple UI which allows the user to change an item balance based off of the type / balance
    /// </summary>
    public partial class ItemBalanceChanger {

        public ListCollectionView ItemTypes {
            get {
                var value = InventoryKeyDB.ItemTypeToKey.Keys.ToList();
                return new ListCollectionView(value);
            }
        }

        public ListCollectionView Balances {
            get {
                if (SelectedItemType == null || !InventoryKeyDB.ItemTypeToKey.ContainsKey(SelectedItemType)) return null;
                var itemKeys = InventoryKeyDB.ItemTypeToKey[SelectedItemType];
                var longNames = InventoryKeyDB.KeyDictionary.Where(x => itemKeys.Contains(x.Value)).Select(x => x.Key).ToList();
                var shortNames = longNames.Select(x => InventorySerialDatabase.GetShortNameFromBalance(x)).Where(x => x != null).ToList();

                return new ListCollectionView(shortNames);
            }
        }

        private string _SelectedItemType = null;
        public string SelectedItemType {
            get { 
                return _SelectedItemType; 
            }
            set {
                _SelectedItemType = value;
                if (!IsStarted) return;

                this.DataContext = null;
                this.DataContext = this;
            }
        }

        public string SelectedBalance { get; set; }
        public string SelectedInventoryData { get; private set; }
        
        public bool IsStarted = false;

        public ItemBalanceChanger() {
            InitializeComponent();

            IsStarted = true;
        }

        public ItemBalanceChanger(string itemType, string balance) {
            InitializeComponent();

            this.SelectedItemType = itemType;
            this.SelectedBalance = balance;

            this.DataContext = null;
            this.DataContext = this;

            this.IsStarted = true;
        }

        private void ExitBtn_Click(object sender, System.Windows.RoutedEventArgs e) {
            SelectedItemType = null;
            SelectedBalance = null;
            SelectedInventoryData = null;
            this.Close();
        }

        private void SaveBtn_Click(object sender, System.Windows.RoutedEventArgs e) {
            string longName = InventorySerialDatabase.GetBalanceFromShortName(SelectedBalance);
            SelectedInventoryData = InventorySerialDatabase.GetInventoryDataByBalance(longName);

            this.Close();
        }

        private void BalanceBox_Selected(object sender, SelectionChangedEventArgs e) {
            if (!IsStarted) return;

            var box = (ComboBox)sender;
            if (box.SelectedItem == null && Balances != null) {
                box.SelectedIndex = 0;
                SelectedBalance = Balances.SourceCollection.OfType<string>().FirstOrDefault();
            }
        }

    }
}
