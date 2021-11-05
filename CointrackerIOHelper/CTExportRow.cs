using System;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

namespace CointrackerIOHelper
{
    public class CtExportRow
    {
        [Name("Date")]
        // Format: "01/31/2015 23:01:31"   intended to be UTC
        public string DateString { get; set; }

        [Ignore]
        public DateTime? Date
        {
            get
            {
                if (DateTime.TryParseExact(this.DateString, "MM/dd/yyyy HH:mm:ss", null, DateTimeStyles.AssumeUniversal,
                    out DateTime d))
                {
                    return d;
                }

                return null; 
            }
        }

        public string Type { get; set; }
        [Name("Transaction ID")]
        public string TransactionID { get; set; }
        [Name("Received Quantity")]
        public decimal? ReceivedQuantity { get; set; }
        [Name("Received Currency")]
        public string ReceivedCurrency { get; set; }
        [Name("Received Wallet")]
        public string ReceivedWallet { get; set; }
        [Name("Sent Quantity")]
        public decimal? SentQuantity { get; set; }
        [Name("Sent Currency")]
        public string SentCurrency { get; set; }
        [Name("Sent Wallet")]
        public string SentWallet { get; set; }
    }
}