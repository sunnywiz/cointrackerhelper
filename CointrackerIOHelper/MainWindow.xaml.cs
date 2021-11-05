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
using CsvHelper.Configuration.Attributes;
using Microsoft.Win32;

namespace CointrackerIOHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CTData CTData { get; set; }
        public List<VoyagerRow> VoyagerData { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            CTData = new CTData();
            VoyagerData = new List<VoyagerRow>();
            UpdateDependencyCTData();
        }

        private void ImportCointrackerHistory_OnClick(object sender, RoutedEventArgs e)
        {
            var a = new OpenFileDialog()
            {
                CheckFileExists = true,
                Title = "Cointracker.IO exported transactions",
                DefaultExt = "csv", 
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
                CTData.Rows.Clear();
                CTData.Rows.AddRange(csv.GetRecords<CTExportRow>());
                CTGrid.ItemsSource = CTData.Rows;
                CTTab.IsSelected = true; 
                UpdateDependencyCTData();
            }
        }

        public void UpdateDependencyCTData()
        {
            if (CTData.Rows.Any())
            {
                ImportVoyagerTrades.IsEnabled = true;
                Dictionary<string, bool> a = new Dictionary<string, bool>();
                foreach (var row in CTData.Rows)
                {
                    if (!String.IsNullOrEmpty(row.ReceivedWallet) &&
                        row.ReceivedWallet.Contains(VoyagerName.Text))
                    {
                        a[row.ReceivedWallet] = true; 
                    }

                    if (!String.IsNullOrEmpty(row.SentWallet) &&
                        row.SentWallet.Contains(VoyagerName.Text))
                    {
                        a[row.SentWallet] = true; 
                    }
                }

                VoyagerWallets.Items.Clear(); 
                foreach (var b in a.Keys.OrderBy(x => x))
                {
                    VoyagerWallets.Items.Add(b); 
                }

                UpdateDependencyVoyagerData();
            }
            else
            {
                ImportVoyagerTrades.IsEnabled = false;
                MatchVoyager.IsEnabled = false;
                VoyagerWallets.Items.Clear(); 
            }
        }

        private void ImportVoyagerTrades_OnClick(object sender, RoutedEventArgs e)
        {
            var a = new OpenFileDialog()
            {
                CheckFileExists = true,
                Title = "Voyager transactions",
                DefaultExt = "csv", 
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
                VoyagerInTab.IsSelected = true; 
                VGINGrid.ItemsSource = VoyagerData;
                UpdateDependencyVoyagerData(); 
            }
        }

        public void UpdateDependencyVoyagerData()
        {
            MatchVoyager.IsEnabled = VoyagerData.Count > 0 
                                     && CTData.Rows.Count > 0 
                                     && VoyagerWallets.Items.Count > 0;
        }

        public class CTImportRow
        {
            [Name("Date")]
            public string DateString { get; set; }
            
            [Ignore]
            public DateTime Date { get; set; }

            [Name("Received Quantity")]
            public decimal? ReceivedQuantity { get; set; }
            [Name("Received Currency")]
            public string ReceivedCurrency { get; set; }
            
            [Name("Sent Quantity")]
            public decimal? SentQuantity { get; set; }
            [Name("Sent Currency")]
            public string SentCurrency { get; set; }
            
            [Name("Fee Amount")]
            public decimal? FeeAmount { get; set; }
            [Name("Fee Currency")]
            public string FeeCurrency { get; set; }
            
            [Name("Tag")]
            public string Tag { get; set; }
        }

        private void VoyagerName_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (CTData?.Rows?.Count != null && !String.IsNullOrEmpty(VoyagerName.Text))
            {
                UpdateDependencyCTData();
            }
            else
            {
                VoyagerWallets?.Items?.Clear(); 
            }
        }

        private void MatchVoyager_OnClick(object sender, RoutedEventArgs e)
        {
            var low = new Dictionary<string,bool>();
            foreach (var a in VoyagerWallets.Items) low[a.ToString()]=true; 

            foreach (var v in VoyagerData)
            {
                if (v.transaction_direction == "deposit")
                {
                    var m1 = CTData.Rows
                        .Where(x => low.ContainsKey(x.ReceivedWallet) &&
                                    x.ReceivedCurrency == v.base_asset)
                        .ToList();
                    var m2 = m1.Where(x=>
                                    x.ReceivedQuantity == v.quantity &&
                                    x.Date.HasValue && 
                                    v.TransactionDate.HasValue && 
                                    Math.Abs((x.Date.Value-v.TransactionDate.Value).TotalMinutes)<30)
                        .ToList();
                    if (m2.Count == 1)
                    {
                        Debug.WriteLine("Match");
                    }
                    else
                    {
                        Debug.WriteLine("No Match");
                    }
                }            
            }
        }
    }
}
