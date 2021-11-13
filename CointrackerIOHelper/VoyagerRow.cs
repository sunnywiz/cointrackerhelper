using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Win32;

namespace CointrackerIOHelper
{

    public class VoyagerHelper:IFormatHelper<VoyagerHelper.VoyagerRow>
    {

        public VoyagerHelper()
        {
            Data = new List<VoyagerRow>(); 
        }
        public List<VoyagerRow> Data { get; set; }

        public bool ChooseAndReadFile()
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
                Data.Clear();
                ReadFile(a.FileName);
                return true; 
            }
            return false;
        }

        public void ReadFile(string fileName)
        {
            using var reader = new StreamReader(fileName);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null
            });
            Data.AddRange(csv.GetRecords<VoyagerRow>());
        }

        public List<CtImportRow> ConvertToCTImport()
        {
            var vd = Data
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.base_asset)
                .ThenBy(x => x.transaction_type)
                .ToList();

            var ret = new List<CtImportRow>();

            foreach (var v in vd)
            {
                if (v.TransactionDate == null) continue;

                var r = new CtImportRow()
                {
                    Date = v.TransactionDate.Value
                };
                switch (v.transaction_type)
                {
                    case "TRADE":
                        if (v.transaction_direction == "Buy")
                        {
                            r.ReceivedCurrency = v.base_asset;
                            r.ReceivedQuantity = v.quantity;
                            r.SentCurrency = v.quote_asset;
                            r.SentQuantity = v.net_amount;
                        }
                        else if (v.transaction_direction == "Sell")
                        {
                            r.SentCurrency = v.base_asset;
                            r.SentQuantity = v.quantity;
                            r.ReceivedCurrency = v.quote_asset;
                            r.ReceivedQuantity = v.net_amount;
                        }

                        ret.Add(r);
                        break;
                    case "BLOCKCHAIN":
                        if (v.transaction_direction == "deposit")
                        {
                            r.ReceivedQuantity = v.quantity;
                            r.ReceivedCurrency = v.base_asset;
                        }
                        else if (v.transaction_direction == "withdrawal")
                        {
                            r.SentQuantity = v.quantity;   // have to add Fee to this
                            r.SentCurrency = v.base_asset;
                        }

                        ret.Add(r);
                        break;
                    case "FEE":
                        if (v.transaction_direction == "withdrawal")
                        {
                            // should be the immediate previous
                            var last = ret[ret.Count - 1];
                            if (last.Date == v.TransactionDate && last.SentCurrency == v.base_asset)
                            {
                                // cointracking : inclusive of fee
                                last.SentQuantity += v.quantity;
                                last.FeeAmount = v.quantity;
                                last.FeeCurrency = v.base_asset;
                            }
                            else
                            {
                                throw new NotImplementedException("Don't know how to handle fee");
                            }
                        }

                        break;
                    default:
                        throw new NotImplementedException(v.transaction_type);
                }
            }

            return ret;
        }

        public class VoyagerRow
        {
            [Name("transaction_date")]
            public string transaction_date_string { get; set; }

            [Ignore]
            public DateTime? TransactionDate
            {
                get
                {
                    var x = transaction_date_string;
                    if (x.Length > 19) x = x.Substring(0, 19);
                    if (DateTime.TryParseExact(x, "yyyy-MM-dd HH:mm:ss", null,
                        DateTimeStyles.AssumeUniversal, out DateTime d))
                    {
                        return d;
                    }

                    return null;
                }
            }

            public string transaction_id { get; set; }
            public string transaction_direction { get; set; }
            public string transaction_type { get; set; }
            public string base_asset { get; set; }
            public string quote_asset { get; set; }
            public decimal quantity { get; set; }
            public decimal net_amount { get; set; }
            public decimal price { get; set; }

            public CtImportRow ToCTImportRow()
            {
                if (transaction_type == "deposit")
                {
                    return new CtImportRow()
                    {
                        Date = this.TransactionDate.Value,
                        ReceivedQuantity = this.quantity,
                        ReceivedCurrency = this.base_asset
                    };
                }
                else
                {
                    throw new NotSupportedException(transaction_type);
                }
            }

        }

    }

}