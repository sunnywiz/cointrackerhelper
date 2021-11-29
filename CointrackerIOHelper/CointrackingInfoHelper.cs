using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CointrackerIOHelper
{
    public class CointrackingInfoHelper : IFormatHelper<CointrackingInfoHelper.Row>
    {
        public List<Row> Data { get; set; }

        public CointrackingInfoHelper()
        {
            Data = new List<Row>();
        }

        public bool ChooseAndReadFile()
        {
            var a = new OpenFileDialog()
            {
                CheckFileExists = true,
                Title = "Cointracking.INFO transactions",
                DefaultExt = ".csv",
                Filter = "CoinTrackingInfo transactions|*.csv"
            };
            if (a.ShowDialog() ?? false)
            {
                Data.Clear();
                ReadFile(a.FileName);
                return true;
            }
            return false;
        }

        public List<CtImportRow> ConvertToCTImport()
        {
            // not used
            return null;
        }

        public List<CtImportRow> ConvertToCTImport(List<Row> filteredData)
        {
            var od = filteredData.OrderBy(x => x.DateDate).ToList();
            var ret = new List<CtImportRow>();

            for (int i = 0; i < od.Count; i++)
            {
                var row = od[i];
                CtImportRow result1 = null;

                if (row.Type == "Deposit")
                {
                    result1 = new CtImportRow()
                    {
                        Date = row.DateDate,
                        ReceivedCurrency = row.BuyCurrency,
                        ReceivedQuantity = row.Buy,
                    };
                }
                else if (row.Type == "Trade")
                {
                    result1 = new CtImportRow()
                    {
                        Date = row.DateDate,
                        ReceivedCurrency = row.BuyCurrency,
                        ReceivedQuantity = row.Buy,
                        SentCurrency = row.SellCurrency,
                        SentQuantity = row.Sell,
                        FeeAmount = row.Fee,
                        FeeCurrency = row.FeeCurrency
                    };
                }
                else if (row.Type == "Withdrawal")
                {
                    if (row.FeeCurrency == row.SellCurrency)
                    {
                        result1 = new CtImportRow()
                        {
                            Date = row.DateDate,
                            SentCurrency = row.SellCurrency,
                            SentQuantity = row.Sell + row.Fee,
                            FeeAmount = row.Fee,
                            FeeCurrency = row.FeeCurrency
                        };
                    }
                    else throw new NotSupportedException("Special withdrawal case");
                }
                else if (row.Type == "Interest Income" || row.Type == "Reward / Bonus")
                {
                    string tag = "payment";
                    if (row.Type == "Reward / Bonus" && row.Comment.Contains("Airdrop")) { tag = "airdrop"; }
                    result1 = new CtImportRow()
                    {
                        Date = row.DateDate,
                        ReceivedCurrency = row.BuyCurrency,
                        ReceivedQuantity = row.Buy,
                        Tag = tag
                    };
                }

                if (result1 != null)
                {
                    row.ConvertInfo = result1.ToString();
                    ret.Add(result1);
                }
            }

            return ret;
        }

        public static string ConvertCurrency(string a)
        {
            return a;
        }

        public void ReadFile(string fileName)
        {
            using var reader = new StreamReader(fileName);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null
            });
            Data.AddRange(csv.GetRecords<Row>().OrderBy(x => x.DateDate));
        }

        public class Row
        {
            // "Type","Buy","Cur.","Sell","Cur.","Fee","Cur.","Exchange","Group","Comment","Date"
            // "Staking","0.20003005","DFI","","","","","Cake Defi","","Staking reward","11/01/2021 09:17:37"

            [Index(0)]
            public string Type { get; set; }

            [Index(1)]
            public decimal? Buy { get; set; }

            [Index(2)]
            public string BuyCurrency { get; set; }

            [Index(3)]
            public decimal? Sell { get; set; }

            [Index(4)]
            public string SellCurrency { get; set; }

            [Index(5)]
            public decimal? Fee { get; set; }

            [Index(6)]
            public string FeeCurrency { get; set; }

            [Index(7)]
            public string Exchange { get; set; }

            [Index(8)]
            public string Group { get; set; }

            [Index(9)]
            public string Comment { get; set; }

            [Index(10)]
            public string DateString { get; set; }

            [Ignore]
            public DateTime DateDate
            {
                get
                {
                    if (DateTime.TryParse(DateString, null, DateTimeStyles.AssumeUniversal, out DateTime d))
                    {
                        return d;
                    }
                    throw new NotSupportedException(DateString);
                }
            }

            [Ignore]
            public string ConvertInfo { get; set; }

        }
    }

}