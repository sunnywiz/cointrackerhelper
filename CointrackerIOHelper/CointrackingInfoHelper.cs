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
            var ret = new List<CtImportRow>();
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
            // "type","buyAmount","buyCurrency","sellAmount","sellCurrency","feeAmount","feeCurrency","exchange","tradeGroup","comment","date"
            // "Mining",0.00966822,"HNT2",,,,,"Helium Wallet",,,"2021-11-12 08:22:23"
            public string type { get; set; }
            public decimal? buyAmount { get; set; }
            public string buyCurrency { get; set; }
            public decimal? sellAmount { get; set; }
            public string sellCurrency {  get; set; }
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

        }
    }

}