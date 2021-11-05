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
        public CtData CTData { get; set; }
        public List<VoyagerRow> VoyagerData { get; set; }
        public List<CtImportRow> CTProposed { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            
            CTData = new CtData();
            VoyagerData = new List<VoyagerRow>();
            CTProposed = new List<CtImportRow>();

            UpdateDependencyCTData();
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
                CTData.Rows.Clear();
                CTData.Rows.AddRange(csv.GetRecords<CtExportRow>());
                CTExisting.ItemsSource = CTData.Rows;
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
                ImportVoyagerTrades.IsEnabled = false;
                MatchedWallets.Items.Clear(); 
            }
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

                CTProposed.Clear(); 
                CTProposed.AddRange(VoyagerRow.ConvertToCTImport(VoyagerData));

                ProposedGrid.ItemsSource = CTProposed;
                CTNewTab.IsSelected = true; 
            }
        }


        private void MatchWalletName_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (CTData?.Rows?.Count != null && !String.IsNullOrEmpty(MatchWalletName.Text))
            {
                UpdateDependencyCTData();
            }
            else
            {
                MatchedWallets?.Items?.Clear(); 
            }
        }

    }
}
