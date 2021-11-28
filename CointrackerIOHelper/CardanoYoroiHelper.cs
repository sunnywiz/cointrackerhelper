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
    public class CardanoYoroiHelper : IFormatHelper<CardanoYoroiHelper.Row>
    {
        public List<Row> Data { get; set; }

        public CardanoYoroiHelper()
        {
            Data = new List<Row>();
        }

        public bool ChooseAndReadFile()
        {
            var a = new OpenFileDialog()
            {
                CheckFileExists = true,
                Title = "Cardano transactions",
                DefaultExt = ".csv",
                Filter = "Cardano transactions|*.csv"
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
            var od = Data.OrderBy(x => x.DateDate).ToList();
            var ret = new List<CtImportRow>();

            for (int i = 0; i < od.Count; i++)
            {
                var row = od[i];
                CtImportRow result1 = null;

                if (row.Type=="Deposit")
                {
                    result1 = new CtImportRow()
                    {
                        Date = row.DateDate,
                        ReceivedCurrency = row.BuyCurrency,
                        ReceivedQuantity = row.BuyAmount,
                        Tag = (row.Comment ?? "").StartsWith("Staking Reward") ? "mined" : ""
                    };
                } else if (row.Type== "Withdrawal" && row.SellCurrency == row.FeeCurrency)
                {
                    result1 = new CtImportRow()
                    {
                        Date = row.DateDate,
                        SentQuantity = row.SellAmount + row.FeeAmount,
                        SentCurrency = row.SellCurrency,
                        FeeAmount = row.FeeAmount,
                        FeeCurrency = row.FeeCurrency
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
            [Name("Type (Trade, IN or OUT)")]
            public string Type { get; set; }
            [Name("Buy Amount")]
            public decimal? BuyAmount { get; set; }
            [Name("Buy Cur.")]
            public string BuyCurrency { get; set; }
            [Name("Sell Amount")]
            public decimal? SellAmount { get; set; }
            [Name("Sell Cur.")]
            public string SellCurrency { get; set; }
            [Name("Fee Amount (optional)")]
            public decimal? FeeAmount { get; set; }
            [Name("Fee Cur. (optional)")]
            public string FeeCurrency { get; set; }
            [Name("Exchange (optional)")]
            public string Exchange { get; set; }
            [Name("Trade Group (optional)")]
            public string TradeGroup { get; set; }
            [Name("Comment (optional)")]
            public string Comment { get; set; }

            [Name("Date")]
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