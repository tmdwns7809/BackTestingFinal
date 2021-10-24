using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Binance.Net;
using Binance.Net.Objects;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using TradingLibrary.Base;
using TradingLibrary;
using TradingLibrary.Trading;
using System.Data.SQLite;

namespace BackTestingFinal
{
    class BinanceDB
    {
        public static string futures1mName = "1mKlinesFuturesUsdt.db";
        public static string spot1mName = "1mKlinesSpot.db";
        public static string futures1hName = "1hKlinesFuturesUsdt.db";
        public static string path = @"C:\Users\tmdwn\source\repos\BigFilesCantSaveGit\SticksDB\Binance\";

        Form form;

        BinanceClient client;

        Dictionary<string, TradeItemData> ItemDataList = new Dictionary<string, TradeItemData>();

        int weight_limit;

        public BinanceDB(Form form, string path)
        {
            this.form = form;

            client = new BinanceClient(new BinanceClientOptions { ApiCredentials = new ApiCredentials(BinanceBase.future2_API_Key, BinanceBase.future2_Secret_Key) });

            SetItemDataList();

            SaveUntilNow(path);
        }

        void SetItemDataList()
        {
            var result = client.FuturesUsdt.System.GetExchangeInfoAsync().Result;
            if (!result.Success)
                Trading.ShowError(form);

            var c = 0;
            foreach (var s in result.Data.Symbols)
            {
                if (s.Name == "BTCSTUSDT")
                    continue;

                var itemData = new TradeItemData(s.Name);
                ItemDataList.Add(itemData.Code, itemData);

                c++;
            }

            foreach (var l in result.Data.RateLimits)
            {
                switch (l.Type)
                {
                    case RateLimitType.RequestWeight:
                        if (l.Interval != RateLimitInterval.Minute || l.IntervalNumber != 1)
                            Trading.ShowError(form);

                        weight_limit = l.Limit;
                        break;

                    default:
                        break;
                }
            }
        }

        void SaveUntilNow(string path)
        {
            SQLiteConnection conn = new SQLiteConnection("Data Source =" + path);
            conn.Open();

            var done = 0;
            foreach (var s in ItemDataList)
            {
                new SQLiteCommand("Begin", conn).ExecuteNonQuery();

                new SQLiteCommand("CREATE TABLE IF NOT EXISTS '" + s.Key + "' " +
                    "('date' TEXT, 'time' TEXT, 'open' REAL, 'high' REAL, 'low' REAL, 'close' REAL, 'baseVolume' REAL, 'takerBuyBaseVolume' REAL, 'quoteVolume' REAL, 'takerBuyQuoteVolume' REAL, 'tradeCount' INTEGER)", conn).ExecuteNonQuery();

                var startTime = DateTime.Parse("2000-01-01");

                var reader = new SQLiteCommand("SELECT *, rowid FROM '" + s.Key + "' ORDER BY rowid DESC LIMIT 2", conn).ExecuteReader();

                KlineInterval klineInterval = KlineInterval.OneMinute;

                if (reader.HasRows)
                {
                    reader.Read();
                    startTime = DateTime.Parse(reader["date"].ToString().Insert(4, "-").Insert(7, "-") + " " + reader["time"].ToString().Insert(2, ":").Insert(5, ":"));
                    var rowid = reader["rowid"].ToString();

                    if (!reader.Read())
                        Trading.ShowError(form);
                    var time0 = DateTime.Parse(reader["date"].ToString().Insert(4, "-").Insert(7, "-") + " " + reader["time"].ToString().Insert(2, ":").Insert(5, ":"));

                    var intervalMin = startTime.Subtract(time0).TotalMinutes;
                    if (intervalMin == 60)
                        klineInterval = KlineInterval.OneHour;

                    new SQLiteCommand("DELETE FROM '" + s.Key + "' WHERE rowid='" + rowid + "'", conn).ExecuteNonQuery();
                }
                else
                {
                    if (path.Contains("1h"))
                        klineInterval = KlineInterval.OneHour;
                }

                var first = true;
                var weight_now = 0;
                while (true)
                {
                    try
                    {
                        var klines = client.FuturesUsdt.Market.GetKlinesAsync(s.Key, klineInterval, startTime, null, 1000).Result;
                        if (!klines.Success || klines.Data == null)
                            continue;

                        var weights = klines.ResponseHeaders.Where(p => p.Key == "x-mbx-used-weight" || p.Key == "x-mbx-used-weight-1m" || p.Key == "X-MBX-USED-WEIGHT-1M").ToList();
                        weight_now = int.Parse(weights[0].Value.ElementAt(0).ToString());
                        if (weight_now + 50 > weight_limit)
                            Thread.Sleep(60000);

                        var first2 = true;
                        foreach (var kline in klines.Data)
                        {
                            if (!first && first2)
                            {
                                first2 = false;
                                continue;
                            }
                            else if (first)
                            {
                                first = false;
                                first2 = false;
                            }

                            new SQLiteCommand("INSERT INTO '" + s.Key + "' " +
                                "('date', 'time', 'open', 'high', 'low', 'close', 'baseVolume', 'takerBuyBaseVolume', 'quoteVolume', 'takerBuyQuoteVolume', 'tradeCount') values " +
                                "('" + kline.OpenTime.ToString("yyyyMMdd") + "', '" + kline.OpenTime.ToString("HHmmss") + "', '" + kline.Open + "', '" + kline.High + "', '" + kline.Low + "', '" + kline.Close
                                 + "', '" + kline.BaseVolume + "', '" + kline.TakerBuyBaseVolume + "', '" + kline.QuoteVolume + "', '" + kline.TakerBuyQuoteVolume + "', '" + kline.TradeCount + "')", conn).ExecuteNonQuery();
                        }

                        if (klines.Data.Count() != 1000)
                            break;
                        else
                            startTime = klines.Data.Last().OpenTime;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                }

                done++;

                new SQLiteCommand("Commit", conn).ExecuteNonQuery();
            }

            conn.Close();
        }
    }
}
