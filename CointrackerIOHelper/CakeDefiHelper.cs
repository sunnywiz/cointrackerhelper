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
    public class CakeDefiHelper : IFormatHelper<CakeDefiHelper.Row>
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
            var orderedData = Data.OrderBy(x => x.DateDate).ToList();
            var ret = new List<CtImportRow>();

            for (int orderIndex = 0; orderIndex < orderedData.Count; orderIndex++)
            {
                Row cakeTrade = orderedData[orderIndex];
                CtImportRow result1 = null;
                if (cakeTrade.Operation == "Deposit")
                {
                    result1 = new CtImportRow
                    {
                        Date = cakeTrade.DateDate,
                        ReceivedCurrency = cakeTrade.CoinOrAsset,
                        ReceivedQuantity = cakeTrade.Amount
                    };
                }
                else if (cakeTrade.Operation == "Signup bonus"
                    || cakeTrade.Operation == "Freezer staking bonus"
                    || cakeTrade.Operation == "Staking reward"
                    || cakeTrade.Operation.StartsWith("Liquidity mining reward "))
                {
                    result1 = new CtImportRow
                    {
                        Date = cakeTrade.DateDate,
                        ReceivedCurrency = cakeTrade.CoinOrAsset,
                        ReceivedQuantity = cakeTrade.Amount,
                        Tag = "payment"
                    };
                }
                else if (cakeTrade.Operation == "Swapped in")
                {
                    // these are dealt with during Swapped out
                }
                else if (cakeTrade.Operation == "Swapped out" && cakeTrade.Amount < 0)
                {
                    var swapIn = orderedData.FirstOrDefault(
                        x => x.RelatedReferenceID == cakeTrade.RelatedReferenceID
                        && x.Operation == "Swapped in"
                        );
                    if (swapIn == null) throw new NotSupportedException("COuld not find SwapIn:" + cakeTrade.RelatedReferenceID);
                    result1 = new CtImportRow
                    {
                        Date = cakeTrade.DateDate,
                        ReceivedCurrency = swapIn.CoinOrAsset,
                        ReceivedQuantity = swapIn.Amount,
                        SentCurrency = cakeTrade.CoinOrAsset,
                        SentQuantity = -cakeTrade.Amount
                        // no fees
                    };
                    // mark the swap as well
                    swapIn.ConvertInfo = result1.ToString();
                }
                else if (cakeTrade.Operation.StartsWith("Add liquidity "))
                {
                    // after analyzing the official cointracking.info (rival) importer
                    // we expense going to BTCDFI and we get paid coming back
                    // which are both taxable
                    result1 = new CtImportRow
                    {
                        Date = cakeTrade.DateDate,
                        SentCurrency = cakeTrade.CoinOrAsset,
                        SentQuantity = -cakeTrade.Amount
                    };
                    var t2 = orderedData.FirstOrDefault(x => x.Reference == cakeTrade.RelatedReferenceID);
                    t2.ConvertInfo = t2.ConvertInfo ?? "";
                    t2.ConvertInfo += $"{-cakeTrade.Amount} {cakeTrade.CoinOrAsset} ";
                }
                else if (cakeTrade.Operation == "Added liquidity")
                {
                    // these are handled by "Added Liquidity DFI" as a taxable transaction
                    // we don't track the BTC-DFI things directly
                }

                // Gather and mark result
                if (result1 != null)
                {
                    cakeTrade.ConvertInfo = result1.ToString();
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
            // Date,Operation,Amount,Coin/Asset,FIAT value,FIAT currency,Transaction ID,Withdrawal address,Reference,Related reference ID
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

            [Ignore]
            public string ConvertInfo { get; set; }
        }

    }

}