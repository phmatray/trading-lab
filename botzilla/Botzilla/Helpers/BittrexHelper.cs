using System;
using System.Linq;
using Bittrex.Net;
using Info.Blockchain.API.ExchangeRates;
using Info.Blockchain.API.Models;

namespace Botzilla.Helpers
{
    public class BittrexHelper
    {
        public static decimal GetBalanceUSD(BitcoinValue balanceBTC)
        {
            var exchangeRateExplorer = new ExchangeRateExplorer();
            double balanceUSD = exchangeRateExplorer
                .FromBtcAsync(balanceBTC, "USD")
                .GetAwaiter()
                .GetResult();

            return Convert.ToDecimal(balanceUSD);
        }

        public static BitcoinValue GetBalanceBTC()
        {
            using (var client = new BittrexClient())
            {
                var myBalances = client.GetBalances()
                    .Result
                    .Where(x => x.Balance > 0)
                    .ToList();

                decimal result = 0;
                foreach (var b in myBalances)
                {
                    if (b.Currency == "BTC")
                        result += (decimal) b.Balance;
                    else
                    {
                        var ticker = client.GetTicker($"BTC-{b.Currency}");
                        result += (decimal) (ticker.Result.Bid * b.Balance);
                    }
                }

                return new BitcoinValue(result);
            }
        }
    }
}