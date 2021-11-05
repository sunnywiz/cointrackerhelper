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
        public List<VoyagerRow> VoyagerData { get; set; }
        public List<CtImportRow> CtProposedData { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            
            CtExistingData = new List<CtExportRow>();
            VoyagerData = new List<VoyagerRow>();
            CtProposedData = new List<CtImportRow>();

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
                CtExistingGrid.ItemsSource = CtExistingData;
                CtExistingTab.IsSelected = true; 
                UpdateDependencies();
            }
        }

        public void UpdateDependencies()
        {
            ImportVoyagerTrades.IsEnabled = true;  // can import at any time
            
            // MatchedWallets depends on CTData having data and the MatchWalletName
            if (CtExistingData.Any())
            {
                Dictionary<string, bool> a = new Dictionary<string, bool>();
                foreach (var row in CtExistingData)
                {
                    if (!String.IsNullOrEmpty(row.ReceivedWallet) &&
                        row.ReceivedWallet.Contains(MatchWalletName.Text))
                    {
                        a[row.ReceivedWallet] = true; 
                    }

                    if (!String.IsNullOrEmpty(row.SentWallet) &&
                        row.SentWallet.Contains(MatchWalletName.Text))
                    {
                        a[row.SentWallet] = true; 
                    }
                }
                MatchedWallets.Items.Clear(); 
                foreach (var b in a.Keys.OrderBy(x => x))
                {
                    MatchedWallets.Items.Add(b); 
                }
            }
            else
            {
               MatchedWallets.Items.Clear(); 
            }

            // MatchButton is dependent on Proposed Trades being there
            MatchButton.IsEnabled = CtProposedData?.Count > 0; 
        }

        private void ImportVoyagerTrades_OnClick(object sender, RoutedEventArgs e)
        {
            var a = new OpenFileDialog()
            {
                CheckFileExists = true,
                Title = "Voyager transactions",
                DefaultExt = ".csv", 
                Filter = "Voyager transactions|*.csv"
            };
            if (a.ShowDialog() ?? false)
            {
                using var reader = new StreamReader(a.FileName);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null
                });
                VoyagerData.Clear();
                VoyagerData.AddRange(csv.GetRecords<VoyagerRow>());

                VoyagerTab.IsSelected = true; 
                VoyagerDataGrid.ItemsSource = VoyagerData;

                CtProposedData.Clear(); 
                CtProposedData.AddRange(VoyagerRow.ConvertToCTImport(VoyagerData));

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

            var source = CtExistingData
                .Where(x => listOfWallets.Contains(x.ReceivedWallet)
                            || listOfWallets.Contains(x.SentWallet))
                .ToList();

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
                            trade.MatchInfo = "matched level " + matchLevel;
                            m.MatchInfo = trade.ToString(); 
                        }
                    }
                }
            }

            CtExistingGrid.ItemsSource = null;
            CtExistingGrid.ItemsSource = CtExistingData;

            CtProposedGrid.ItemsSource = null;
            CtProposedGrid.ItemsSource = CtProposedData; 
        }
    }
}
