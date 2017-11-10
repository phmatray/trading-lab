using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicTacTec.TA.Library;
using Bittrex.Net;
using Bittrex.Net.Logging;
using Info.Blockchain.API.ExchangeRates;
using Info.Blockchain.API.Models;

namespace Botzilla
{
    class Program
    {
        static void Main(string[] args)
        {
            var bot = new Bot();
            bot.Process();
        }
    }

    public class Bot
    {
        private List<string> _watchList;

        public void Process()
        {
            Init();
            //Watch();
            //Compute(List<IStrategy> strategies);
            //Serve();

            
        }

        private void Init()
        {
            var key = "c72dc5afff514f6e9b6a09a835aa7eed";
            var secret = "6b2aa822edb2429db35a83aa0ca805ab";

            BittrexDefaults.SetDefaultApiCredentials(key, secret);
            BittrexDefaults.SetDefaultLogOutput(Console.Out);
            BittrexDefaults.SetDefaultLogVerbosity(LogVerbosity.Debug);

            var balanceBTC = GetBalance();

            var exchangeRateExplorer = new ExchangeRateExplorer();
            double result = exchangeRateExplorer
                .FromBtcAsync(new BitcoinValue(balanceBTC), "USD")
                .GetAwaiter()
                .GetResult();


            //_watchList = new List<string> { "BTC-PAY", "BTC-RADS" };
        }

        private static decimal GetBalance()
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

                return result;
            }
        }
    }

    public interface IStrategy
    {
        
    }

    public class DMAC
        : IStrategy
    {
        
    }

}
