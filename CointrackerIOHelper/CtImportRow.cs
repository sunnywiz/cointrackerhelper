using System;
using CsvHelper.Configuration.Attributes;

namespace CointrackerIOHelper
{
    public class CtImportRow
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
}