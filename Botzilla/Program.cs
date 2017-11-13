using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bittrex.Net;
using Bittrex.Net.Logging;
using Botzilla.Helpers;
using Botzilla.Models;

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
        private const string Key = "c72dc5afff514f6e9b6a09a835aa7eed";
        private const string Secret = "6b2aa822edb2429db35a83aa0ca805ab";
        private const string FileName = "bittrex.txt";

        public List<ReportLine> Report;
        public ReportLine CurrentLine;
        public ReportLine PreviousLine;

        public List<string> ReportLines
            => Report?.Select(x => x.ToString()).ToList();

        public string Difference
            => GetDifference(CurrentLine, PreviousLine);

        public void Process()
        {
            Init();
            Watch();

            Console.ReadLine();
        }

        private void Init()
        {
            BittrexDefaults.SetDefaultApiCredentials(Key, Secret);
            BittrexDefaults.SetDefaultLogOutput(Console.Out);
            BittrexDefaults.SetDefaultLogVerbosity(LogVerbosity.Warning);
            LoadReport();
        }

        private void Watch()
        {
            var timer = new Timer(CheckStatus, new AutoResetEvent(false), 0, 1000);
        }

        private void CheckStatus(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            var now = DateTime.Now;
            int totalSeconds = now.Minute * 60 + now.Second;

            if (totalSeconds % 300 == 0)
            {
                var balanceBTC = BittrexHelper.GetBalanceBTC();
                var balanceUSD = BittrexHelper.GetBalanceUSD(balanceBTC);

                PreviousLine = CurrentLine;
                CurrentLine = new ReportLine(now, balanceBTC, balanceUSD);

                UpdateReport();
                Thread.Sleep(TimeSpan.FromMinutes(5));
            }
        }

        private void LoadReport()
        {
            if (!File.Exists(FileName))
                File.Create(FileName).Close();

            Report = File
                .ReadAllLines(FileName)
                .Select(x => new ReportLine(x))
                .ToList();

            PreviousLine = null;
            foreach (ReportLine line in Report)
            {
                CurrentLine = line;
                Console.WriteLine($"{CurrentLine}  {Difference}");
                PreviousLine = CurrentLine;
            }
        }

        private string GetDifference(ReportLine current, ReportLine previous)
        {
            var sb = new StringBuilder();

            if (previous != null)
            {
                decimal currentBTC = current.BalanceBTC.GetBtc();
                decimal previousBTC = previous.BalanceBTC.GetBtc();
                decimal currentUSD = current.BalanceUSD;
                decimal previousUSD = previous.BalanceUSD;

                sb.Append('(');

                if (currentBTC > previousBTC)
                    sb.Append('+');
                else if (currentBTC < previousBTC)
                    sb.Append('-');
                else
                    sb.Append('=');

                sb.Append('/');

                if (currentUSD > previousUSD)
                    sb.Append('+');
                else if (currentUSD < previousUSD)
                    sb.Append('-');
                else
                    sb.Append('=');

                sb.Append(')');
            }

            return sb.ToString();
        }

        private void UpdateReport()
        {
            Report.Add(CurrentLine);
            File.AppendAllLines(FileName, new List<string> {CurrentLine.ToString()});
            Console.WriteLine($"{CurrentLine}  {Difference}");
        }
    }
}