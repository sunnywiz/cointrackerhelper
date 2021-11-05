using System;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

namespace CointrackerIOHelper
{
    public class VoyagerRow
    {
        [Name("transaction_date")]
        public string transaction_date_string { get; set; }

        [Ignore]
        public DateTime? TransactionDate {
            get
            {
                var x = transaction_date_string;
                if (x.Length > 19) x = x.Substring(0, 19);
                if (DateTime.TryParseExact(x,"yyyy-MM-dd HH:mm:ss", null,
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