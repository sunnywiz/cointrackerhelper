using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;

namespace CointrackerIOHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<CtExportRow> CtExistingData { get; set; }
        public List<CtExportRow> CtExistingFilteredData { get; set; }
        public List<CtImportRow> CtProposedData { get; set; }
        public VoyagerHelper VoyagerHelper {  get; set; }
        public CakeDefiHelper CakeDefiHelper { get; set; }

        public CardanoYoroiHelper CardanoYoroiHelper { get; set; }

        public HNTCointrackingInfoHelper HNTCointrackingInfoHelper { get; set; }

        public CointrackingInfoHelper CointrackingInfoHelper { get; set; }

        public List<CointrackingInfoHelper.Row> CointrackingInfoFilteredData { get; set; }

        public List<CtExportRow> MergeFilteredData { get; set; }
        public List<CtImportRow> MergeProposedData { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            
            CtExistingData = new List<CtExportRow>();
            CtExistingFilteredData = new List<CtExportRow>();
            CtProposedData = new List<CtImportRow>();
            MergeFilteredData = new List<CtExportRow>();
            MergeProposedData = new List<CtImportRow>();
            VoyagerHelper = new VoyagerHelper();
            CakeDefiHelper = new CakeDefiHelper();
            HNTCointrackingInfoHelper= new HNTCointrackingInfoHelper();
            CardanoYoroiHelper = new CardanoYoroiHelper();
            CointrackingInfoHelper = new CointrackingInfoHelper();
            CointrackingInfoFilteredData = new List<CointrackingInfoHelper.Row>();

            MergeStartDate.SelectedDate = DateTime.Now.AddYears(-1);
            MergeEndDate.SelectedDate = DateTime.Now;

            UpdateDependencies();
        }

        private void ImportCointrackerHistory_OnClick(object sender, RoutedEventArgs e)
        {
            var a = new OpenFileDialog()
            {
                CheckFileExists = true,
                Title = "Cointracker.IO exported transactions",
                DefaultExt = ".csv", 
                Filter = "Cointracker Files|*.csv"
            };
            if (a.ShowDialog()??false)
            {
                using var reader = new StreamReader(a.FileName);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true, 
                    MissingFieldFound = null
                });
                CtExistingData.Clear();
                CtExistingData.AddRange(csv.GetRecords<CtExportRow>());
                CtExistingTab.IsSelected = true; 
                UpdateDependencies();
                _mergeTransactionsTabInitialized = false; 
            }
        }

        public void UpdateDependencies()
        {
            ImportVoyagerTrades.IsEnabled = true;  // can import at any time

            // MatchedWallets depends on CTData having data and the MatchWalletName
            UpdateCtExistingDataWithMatches();

            UpdateCointrackingInfoDataWithMatches(); 

            // MatchButton is dependent on Proposed Trades being there
            MatchButton.IsEnabled = CtProposedData?.Count > 0;

            ExportButton.IsEnabled = CtProposedData?.Any(x => String.IsNullOrEmpty(x.MatchInfo)) ?? false;
        }

        private void UpdateCointrackingInfoDataWithMatches()
        {
            if (CointrackingInfoHelper.Data.Any())
            {
                CointrackingInfoFilteredData.Clear(); 
                if (String.IsNullOrEmpty(CointrackingInfoFilterExchangesText.Text))
                {
                    CointrackingInfoFilteredData.AddRange(CointrackingInfoHelper.Data);
                    CointrackingInfoMatchedExchanges.Items.Clear(); 
                } else
                {
                    Dictionary<string, bool> a = new Dictionary<string, bool>();
                    foreach (var row in CointrackingInfoHelper.Data)
                    {
                        bool match = false;
                        if (!String.IsNullOrEmpty(row.Exchange) &&
                            row.Exchange.Contains(CointrackingInfoFilterExchangesText.Text))
                        {
                            a[row.Exchange] = true;
                            match = true;
                        }

                        if (match) CointrackingInfoFilteredData.Add(row);
                    }

                    CointrackingInfoMatchedExchanges.Items.Clear();
                    foreach (var b in a.Keys.OrderBy(x => x))
                    {
                        CointrackingInfoMatchedExchanges.Items.Add(b);
                    }

                }
            } else
            {
                CointrackingInfoMatchedExchanges.Items.Clear();
                CointrackingInfoFilteredData.Clear(); 
            }
            CointrackingInfoDataGrid.ItemsSource = null;
            CointrackingInfoDataGrid.ItemsSource = CointrackingInfoFilteredData; 
        }

        private void UpdateCtExistingDataWithMatches()
        {
            if (CtExistingData.Any())
            {
                CtExistingFilteredData.Clear();
                if (String.IsNullOrEmpty(MatchWalletName.Text))
                {
                    CtExistingFilteredData.AddRange(CtExistingData);
                    MatchedWallets.Items.Clear();
                }
                else
                {
                    Dictionary<string, bool> a = new Dictionary<string, bool>();
                    foreach (var row in CtExistingData)
                    {
                        bool match = false;
                        if (!String.IsNullOrEmpty(row.ReceivedWallet) &&
                            row.ReceivedWallet.Contains(MatchWalletName.Text))
                        {
                            a[row.ReceivedWallet] = true;
                            match = true;
                        }

                        if (!String.IsNullOrEmpty(row.SentWallet) &&
                            row.SentWallet.Contains(MatchWalletName.Text))
                        {
                            a[row.SentWallet] = true;
                            match = true;
                        }

                        if (match) CtExistingFilteredData.Add(row);
                    }

                    MatchedWallets.Items.Clear();
                    foreach (var b in a.Keys.OrderBy(x => x))
                    {
                        MatchedWallets.Items.Add(b);
                    }
                }
            }
            else
            {
                MatchedWallets.Items.Clear();
                CtExistingFilteredData.Clear();
            }
            CtExistingGrid.ItemsSource = null;
            CtExistingGrid.ItemsSource = CtExistingFilteredData;
        }

        private void ImportVoyagerTrades_OnClick(object sender, RoutedEventArgs e)
        {
            if (VoyagerHelper.ChooseAndReadFile()) {             

                VoyagerTab.IsSelected = true; 
                VoyagerDataGrid.ItemsSource = VoyagerHelper.Data;

                CtProposedData.Clear(); 
                CtProposedData.AddRange(VoyagerHelper.ConvertToCTImport());

                CtProposedGrid.ItemsSource = CtProposedData;
                CtNewTab.IsSelected = true;

                UpdateDependencies(); 
            }
        }

        private void ImportCakeDefiTrades_OnClick(object sender, RoutedEventArgs e)
        {
            if (CakeDefiHelper.ChooseAndReadFile())
            {
                CakeDefiTab.IsSelected = true;
                CakeDefiDataGrid.ItemsSource = CakeDefiHelper.Data;

                CtProposedData.Clear();
                CtProposedData.AddRange(CakeDefiHelper.ConvertToCTImport());

                CtProposedGrid.ItemsSource = CtProposedData;
                CtNewTab.IsSelected = true;

                UpdateDependencies(); 
            }
        }

        private void MatchWalletName_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (CtExistingData?.Count != null && !String.IsNullOrEmpty(MatchWalletName.Text))
            {
                UpdateDependencies();
            }
            else
            {
                MatchedWallets?.Items?.Clear(); 
            }
        }

        private void MatchButton_OnClick(object sender, RoutedEventArgs e)
        {
            var listOfWallets = new List<string>();
            foreach (var item in MatchedWallets.Items)
            {
                listOfWallets.Add(item.ToString());
            }

            CtExistingData.ForEach(x => x.MatchInfo = null);
            CtProposedData.ForEach(x => x.MatchInfo = null);

            var source = CtExistingFilteredData.ToList(); 

            double minutes = 5;
            Double.TryParse(MatchMinutes.Text, out minutes);

            int decimals = 4;
            int.TryParse(MatchDecimals.Text, out decimals);

            foreach (var trade in CtProposedData)
            {
                var m1 = source.Where(x => x.Date.HasValue &&
                                           Math.Abs((x.Date.Value - trade.Date).TotalMinutes) <= minutes).ToList();
                if (m1.Count > 0)
                {

                    var m2 = new List<CtExportRow>();
                    foreach (var m in m1)
                    {
                        int matchLevel = 0;
                        var minuteLevel = Math.Abs((m.Date.Value - trade.Date).TotalMinutes);

                        if (trade.ReceivedCurrency != null &&
                            m.ReceivedCurrency == trade.ReceivedCurrency &&
                            trade.ReceivedQuantity.HasValue &&
                            m.ReceivedQuantity.HasValue &&
                            Decimal.Compare(Math.Round(m.ReceivedQuantity.Value, decimals),
                                Math.Round(trade.ReceivedQuantity.Value, decimals)) == 0)
                        {
                            matchLevel++;
                        }

                        if (trade.SentCurrency != null &&
                            m.SentCurrency == trade.SentCurrency &&
                            trade.SentQuantity.HasValue &&
                            m.SentQuantity.HasValue &&
                            Decimal.Compare(Math.Round(m.SentQuantity.Value, decimals),
                                Math.Round(trade.SentQuantity.Value, decimals)) == 0)
                        {
                            matchLevel++; 
                        }

                        if (trade.FeeCurrency != null &&
                            m.FeeCurrency == trade.SentCurrency &&
                            trade.FeeAmount.HasValue &&
                            m.FeeAmount.HasValue &&
                            Decimal.Compare(Math.Round(m.FeeAmount.Value, decimals),
                                Math.Round(trade.FeeAmount.Value, decimals)) == 0)
                        {
                            matchLevel++; 
                        }

                        if (matchLevel > 0)
                        {
                            // This is a MATCH
                            trade.MatchInfo = $"{minuteLevel:N1}min match=" + matchLevel;
                            m.MatchInfo = trade.ToString(); 
                        }
                    }
                }
            }

            CtProposedGrid.ItemsSource = null;
            CtProposedGrid.ItemsSource = CtProposedData; 

            UpdateDependencies();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                Filter = "Cointracker CSV|*.csv",
                FileName = "upload_to_cointracker.csv"
            };
            if (sfd.ShowDialog() ?? false)
            {
                using (var sw = new StreamWriter(sfd.FileName))
                {
                    using (var csv = new CsvWriter(sw,new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord=true
                    }))
                    {
                        csv.WriteRecords<CtImportRow>(CtProposedData.Where(x => String.IsNullOrEmpty(x.MatchInfo)));
                    }
                }
            }
        }

        private void ImportCointrackingTrades_Click(object sender, RoutedEventArgs e)
        {
            if (HNTCointrackingInfoHelper.ChooseAndReadFile())
            {
                
                HNTCointrackingInfoTab.IsSelected = true;
                HNTCointrackingInfoDataGrid.ItemsSource = HNTCointrackingInfoHelper.Data;

                CtProposedData.Clear();
                CtProposedData.AddRange(HNTCointrackingInfoHelper.ConvertToCTImport());

                CtProposedGrid.ItemsSource = CtProposedData;
                CtNewTab.IsSelected = true;

                UpdateDependencies();
            }

        }

        private void ImportCardanoYoroiTrades_Click(object sender, RoutedEventArgs e)
        {
            if (CardanoYoroiHelper.ChooseAndReadFile())
            {
                CardanoiYoroiTab.IsSelected = true;
                CardanoYoroiDataGrid.ItemsSource = CardanoYoroiHelper.Data;

                CtProposedData.Clear();
                CtProposedData.AddRange(CardanoYoroiHelper.ConvertToCTImport());

                CtProposedGrid.ItemsSource = CtProposedData;
                CtNewTab.IsSelected = true;

                UpdateDependencies();
            }
        }

        private void ImportCointrackingInfo_Click(object sender, RoutedEventArgs e)
        {
            if (CointrackingInfoHelper.ChooseAndReadFile())
            {
                CointrackingInfoTab.IsSelected = true;

                // this applies the exchange name match
                UpdateDependencies();
            }
        }

        private void CointrackingInfoFilterExchangesText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CointrackingInfoHelper?.Data?.Count != null && !String.IsNullOrEmpty(CointrackingInfoFilterExchangesText.Text))
            {
                UpdateDependencies();
            }
            else
            {
                CointrackingInfoMatchedExchanges?.Items?.Clear();
            }

        }

        private void CointrackerInfoGenerateButton_Click(object sender, RoutedEventArgs e)
        {
            CtProposedData.Clear();
            CtProposedData.AddRange(CointrackingInfoHelper.ConvertToCTImport(CointrackingInfoFilteredData));

            CtProposedGrid.ItemsSource = CtProposedData;
            CtNewTab.IsSelected = true;

            UpdateDependencies();

        }

        private bool _mergeTransactionsTabInitialized = false;

        private void MergeTransactionsTab_Selected(object sender, RoutedEventArgs e)
        {
            if (!_mergeTransactionsTabInitialized)
            {
                InitializeMergeTransactionsTab();
                _mergeTransactionsTabInitialized = true;
            }
        }

        private void InitializeMergeTransactionsTab()
        {
            if (CtExistingData == null || CtExistingData.Count == 0)
            {
                return;
            }

            // Initialize date range to min/max of existing data
            var minDate = CtExistingData.Where(d => d.Date.HasValue).Min(d => d.Date);
            var maxDate = CtExistingData.Where(d => d.Date.HasValue).Max(d => d.Date);

            MergeStartDate.SelectedDate = minDate;
            MergeEndDate.SelectedDate = maxDate;

            // Populate wallet dropdown
            MergeWalletCombo.Items.Clear();
            MergeWalletCombo.Items.Add(string.Empty); // Add empty option

            var wallets = new HashSet<string>();
            foreach (var row in CtExistingData)
            {
                if (!string.IsNullOrEmpty(row.ReceivedWallet))
                    wallets.Add(row.ReceivedWallet);
                if (!string.IsNullOrEmpty(row.SentWallet))
                    wallets.Add(row.SentWallet);
            }

            foreach (var wallet in wallets.OrderBy(w => w))
            {
                MergeWalletCombo.Items.Add(wallet);
            }

            // Populate currency dropdown
            MergeCurrencyCombo.Items.Clear();
            MergeCurrencyCombo.Items.Add(string.Empty); // Add empty option

            var currencies = new HashSet<string>();
            foreach (var row in CtExistingData)
            {
                if (!string.IsNullOrEmpty(row.ReceivedCurrency))
                    currencies.Add(row.ReceivedCurrency);
                if (!string.IsNullOrEmpty(row.SentCurrency))
                    currencies.Add(row.SentCurrency);
            }

            foreach (var currency in currencies.OrderBy(c => c))
            {
                MergeCurrencyCombo.Items.Add(currency);
            }

            // Populate transaction type dropdown
            // Since CtExportRow doesn't have a direct transaction type field,
            // we'll infer types from the data patterns
            MergeTransactionTypeCombo.Items.Clear();
            MergeTransactionTypeCombo.Items.Add(string.Empty); // Add empty option

            // Common transaction types
            var transactionTypes = new List<string>
    {
        "Staking reward",
        "Mining",
        "Deposit",
        "Withdrawal",
        "Trade",
        "Transfer"
    };

            foreach (var type in transactionTypes)
            {
                MergeTransactionTypeCombo.Items.Add(type);
            }

            // Do initial filtering with blank criteria
            UpdateMergeTransactionsDataGrid();
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateMergeTransactionsDataGrid();
        }

        private void MergeTransactionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (CtExistingData.Count == 0)
            {
                MessageBox.Show("Please import Cointracker history first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Make sure we have filtered data
            if (MergeFilteredData.Count == 0)
            {
                MessageBox.Show("No transactions match the current filter criteria.", "No Matching Transactions", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create proposed data from filtered transactions
            MergeProposedData = new List<CtImportRow>();

            // For now, just convert the filtered transactions to proposed format
            foreach (var transaction in MergeFilteredData)
            {
                MergeProposedData.Add(new CtImportRow
                {
                    Date = transaction.Date ?? DateTime.Now,
                    ReceivedCurrency = transaction.ReceivedCurrency,
                    ReceivedQuantity = transaction.ReceivedQuantity,
                    SentCurrency = transaction.SentCurrency,
                    SentQuantity = transaction.SentQuantity,
                    FeeAmount = transaction.FeeAmount,
                    FeeCurrency = transaction.FeeCurrency,
                    // Comment = $"Merged from {MergeFilteredData.Count} transactions"
                });
            }

            // Display in the proposed trades grid
            CtProposedData.Clear();
            CtProposedData.AddRange(MergeProposedData);
            CtProposedGrid.ItemsSource = null;
            CtProposedGrid.ItemsSource = CtProposedData;
            CtNewTab.IsSelected = true;

            UpdateDependencies();
        }

        private void UpdateMergeTransactionsDataGrid()
        {
            if (CtExistingData.Count == 0)
            {
                MergeFilteredData.Clear();
                MergeTransactionsDataGrid.ItemsSource = null;
                return;
            }

            string wallet = MergeWalletCombo.Text?.Trim() ?? "";
            string currency = MergeCurrencyCombo.Text?.Trim() ?? "";
            string transactionType = MergeTransactionTypeCombo.Text?.Trim() ?? "";
            DateTime startDate = MergeStartDate.SelectedDate ?? DateTime.MinValue;
            DateTime endDate = MergeEndDate.SelectedDate ?? DateTime.MaxValue;

            // Filter transactions based on criteria
            MergeFilteredData = CtExistingData.Where(t =>
                (string.IsNullOrEmpty(wallet) ||
                 (t.ReceivedWallet?.Contains(wallet) == true || t.SentWallet?.Contains(wallet) == true)) &&
                (string.IsNullOrEmpty(currency) ||
                 (t.ReceivedCurrency?.Equals(currency, StringComparison.OrdinalIgnoreCase) == true ||
                  t.SentCurrency?.Equals(currency, StringComparison.OrdinalIgnoreCase) == true)) &&
                // For transaction type, we'll need to infer it from the data
                // This is a simplified approach - you may need to refine it based on your data
                (string.IsNullOrEmpty(transactionType) ||
                 (transactionType == "Staking reward" && t.ReceivedQuantity.HasValue && t.ReceivedQuantity > 0 && !t.SentQuantity.HasValue) ||
                 (transactionType == "Mining" && t.ReceivedQuantity.HasValue && t.ReceivedQuantity > 0 && !t.SentQuantity.HasValue) ||
                 (transactionType == "Deposit" && t.ReceivedQuantity.HasValue && t.ReceivedQuantity > 0 && !t.SentQuantity.HasValue) ||
                 (transactionType == "Withdrawal" && t.SentQuantity.HasValue && t.SentQuantity > 0 && !t.ReceivedQuantity.HasValue) ||
                 (transactionType == "Trade" && t.ReceivedQuantity.HasValue && t.SentQuantity.HasValue) ||
                 (transactionType == "Transfer" && ((t.ReceivedQuantity.HasValue && !t.SentQuantity.HasValue) || (!t.ReceivedQuantity.HasValue && t.SentQuantity.HasValue)))) &&
                t.Date.HasValue &&
                t.Date.Value >= startDate &&
                t.Date.Value <= endDate
            ).ToList();

            // Update the data grid
            MergeTransactionsDataGrid.ItemsSource = null;
            MergeTransactionsDataGrid.ItemsSource = MergeFilteredData;
        }



    }
}
