using System;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
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
            var sDateTime = list[0].Trim();
            var sBalanceBTC = list[1].TrimEnd(' ', 'B', 'T', 'C');
            var sBalanceUSD = list[2].TrimEnd(' ', 'U', 'S', 'D');

            DateTime = DateTime.Parse(sDateTime);
            BalanceBTC = new BitcoinValue(Convert.ToDecimal(sBalanceBTC));
            BalanceUSD = Convert.ToDecimal(sBalanceUSD);
        }

        public override string ToString()
        {
            return $"{DateTime:G} | {BalanceBTC} BTC | {BalanceUSD:N2} USD";
        }
    }
}