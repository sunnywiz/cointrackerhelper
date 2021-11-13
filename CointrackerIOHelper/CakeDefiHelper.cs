using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CointrackerIOHelper
{
    public class CakeDefiHelper:IFormatHelper<CakeDefiHelper.Row>
    {
        public CakeDefiHelper()
        {
            Data = new List<Row>();
        }
        public List<Row> Data { get; set; }

        public bool ChooseAndReadFile()
        {
            var a = new OpenFileDialog()
            {
                CheckFileExists = true,
                Title = "CakeDefi transactions",
                DefaultExt = ".csv",
                Filter = "Cake transactions|*.csv"
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
            throw new NotImplementedException();
        }

        public void ReadFile(string fileName)
        {
            using var reader = new StreamReader(fileName);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null
            });
            Data.AddRange(csv.GetRecords<Row>());
        }

        public class Row
        {
            // Date,Operation,Amount,Coin/Asset,FIAT value,FIAT currency,Transaction ID,Withdrawal address,Reference,Related reference ID
            [Name("Date")]
            public string DateString { get; set; }
            public string Operation { get; set; }
            public decimal Amount { get; set; }
 
            [Name("Coin/Asset")]
            public string CoinOrAsset { get; set; }
            
            [Name("FIAT value")]
            public decimal FiatValue { get; set; }

            [Name("FIAT currency")]
            public string FiatCurrency { get; set; }

            [Name("Transaction ID")]
            public string TransactionID { get; set; }

            [Name("Withdrawal address")]
            public string WithdrawalAddress { get; set; }

            public string Reference { get; set; }
            [Name("Related reference ID")]
            public string RelatedReferenceID { get; set; }
        }

    }

}