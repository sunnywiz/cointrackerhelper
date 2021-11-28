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
    public class HNTCointrackingInfoHelper : IFormatHelper<HNTCointrackingInfoHelper.Row>
    {
        public List<Row> Data { get; set; }

        public HNTCointrackingInfoHelper()
        {
            Data = new List<Row>();
        }

        public bool ChooseAndReadFile()
        {
            var a = new OpenFileDialog()
            {
                CheckFileExists = true,
                Title = "HNT Cointracking.INFO transactions",
                DefaultExt = ".csv",
                Filter = "HNT CoinTrackingInfo transactions|*.csv"
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

            for (int i=0; i<od.Count; i++)
            {
                var row = od[i];
                CtImportRow result1 = null; 
                if (row.type=="Mining")
                {
                    result1 = new CtImportRow()
                    {
                        Date = row.DateDate,
                        ReceivedCurrency = ConvertCurrency(row.buyCurrency),
                        ReceivedQuantity = row.buyAmount,
                        Tag= "mined"
                    };
                } else if (row.type=="Withdrawal")
                {
                    result1 = new CtImportRow()
                    {
                        Date = row.DateDate,
                        SentCurrency = ConvertCurrency(row.sellCurrency),
                        SentQuantity = row.sellAmount,
                        FeeCurrency = ConvertCurrency(row.feeCurrency),
                        FeeAmount = row.feeAmount
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
            if (a == "HNT2") return "HNT";
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
            // "type","buyAmount","buyCurrency","sellAmount","sellCurrency","feeAmount","feeCurrency","exchange","tradeGroup","comment","date"
            // "Mining",0.00966822,"HNT2",,,,,"Helium Wallet",,,"2021-11-12 08:22:23"
            public string type { get; set; }
            public decimal? buyAmount { get; set; }
            public string buyCurrency { get; set; }
            public decimal? sellAmount { get; set; }
            public string sellCurrency { get; set; }
            public decimal? feeAmount { get; set; }
            public string feeCurrency { get; set; }
            public string exchange { get; set; }
            public string tradeGroup { get; set; }
            public string comment { get; set; }
            [Name("date")]
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