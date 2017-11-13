using System;
using System.Linq;
using Info.Blockchain.API.Models;

namespace Botzilla.Models
{
    public class ReportLine
    {
        public DateTime DateTime { get; set; }
        public BitcoinValue BalanceBTC { get; set; }
        public decimal BalanceUSD { get; set; }

        public ReportLine(DateTime dateTime, BitcoinValue balanceBTC, decimal balanceUSD)
        {
            DateTime = dateTime;
            BalanceBTC = balanceBTC;
            BalanceUSD = balanceUSD;
        }

        public ReportLine(string line)
        {
            var list = line.Split('|').ToList();
            DateTime = DateTime.Parse(list[0]);
            BalanceBTC = new BitcoinValue(Convert.ToDecimal(list[1].TrimEnd(' ', 'B', 'T', 'C')));
            BalanceUSD = Convert.ToDecimal(list[2].TrimEnd(' ', 'U', 'S', 'D'));
        }

        public override string ToString()
        {
            return $"{DateTime:G} | {BalanceBTC} BTC | {BalanceUSD:N2} USD";
        }
    }
}