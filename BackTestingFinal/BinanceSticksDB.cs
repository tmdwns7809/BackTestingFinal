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
using Binance.Net.Interfaces;
using CryptoExchange.Net.Authentication;
using TradingLibrary.Base;
using TradingLibrary;
using TradingLibrary.Trading;
using System.Data.SQLite;

namespace BackTestingFinal
{
    sealed class BinanceSticksDB : BaseSticksDB
    {
        public static string futureBaseName = "KlinesFuturesUsdt_";
        public static string spotBaseName = "KlinesSpot_";
        public static string BinancePath = @"C:\Users\tmdwn\source\repos\BigFilesCantSaveGit\SticksDB\Binance\";

        BinanceClient client;

        public BinanceSticksDB(Form f, bool update, bool check, bool isFuture) : base(f, BinancePath, isFuture ? futureBaseName : spotBaseName, update, check) { }

        protected override void ConnectAndGetCodeList()
        {
            BaseFunctions.SetChartValuesDic(false);

            client = new BinanceClient(new BinanceClientOptions { ApiCredentials = new ApiCredentials(BinanceBase.future2_API_Key, BinanceBase.future2_Secret_Key) });

            var result = client.FuturesUsdt.System.GetExchangeInfoAsync().Result;
            if (!result.Success)
                BaseFunctions.ShowError(form);

            var c = 0;
            foreach (var s in result.Data.Symbols)
            {
                if (s.Status != SymbolStatus.Trading)
                    continue;

                codeList.Add(s.Name);

                c++;
            }

            BaseFunctions.BinanceUpdateWeightLimit(result.Data.RateLimits, form);
        }

        protected override void SaveAndUpdateDB()
        {
            BaseFunctions.LoadingSettingFirst(form);

            var doneTime = 0;
            foreach (var pair in BaseFunctions.ChartValuesDic)
            {
                SQLiteConnection conn = new SQLiteConnection("Data Source =" + path + BaseName + pair.Value.Text + ".db");
                conn.Open();

                var doneCode = 0;
                foreach (var code in codeList)
                {
                    BaseFunctions.AddOrChangeLoadingText("Saving (" + doneTime + "/" + BaseFunctions.ChartValuesDic.Count + ")DB...(" + doneCode + "/" + codeList.Count + ")", doneCode == 0);

                    new SQLiteCommand("Begin", conn).ExecuteNonQuery();

                    new SQLiteCommand("CREATE TABLE IF NOT EXISTS '" + code + "' " +
                        "('date' TEXT, 'time' TEXT, 'open' REAL, 'high' REAL, 'low' REAL, 'close' REAL, 'baseVolume' REAL, 'takerBuyBaseVolume' REAL, 'quoteVolume' REAL, 'takerBuyQuoteVolume' REAL, 'tradeCount' INTEGER)", conn).ExecuteNonQuery();

                    var startTime = DateTime.Parse("2000-01-01");

                    var reader = new SQLiteCommand("SELECT *, rowid FROM '" + code + "' ORDER BY rowid DESC LIMIT 1", conn).ExecuteReader();

                    if (reader.HasRows)
                    {
                        reader.Read();
                        startTime = DateTime.Parse(reader["date"].ToString().Insert(4, "-").Insert(7, "-") + " " + reader["time"].ToString().Insert(2, ":").Insert(5, ":"));
                        var rowid = reader["rowid"].ToString();

                        new SQLiteCommand("DELETE FROM '" + code + "' WHERE rowid='" + rowid + "'", conn).ExecuteNonQuery();
                    }

                    var first = true;
                    while (true)
                    {
                        try
                        {
                            var klines = client.FuturesUsdt.Market.GetKlinesAsync(code, pair.Key, startTime, null, 1000).Result;
                            if (!klines.Success || klines.Data == null)
                                continue;

                            BaseFunctions.BinanceUpdateWeightNow(klines.ResponseHeaders);
                            if (BaseFunctions.binance_weight_now + 50 > BaseFunctions.binance_weight_limit)
                                Thread.Sleep(60000);

                            var first2 = true;
                            IBinanceKline last = default;
                            var count = 0;
                            foreach (var kline in klines.Data)
                            {
                                last = kline;
                                count++;

                                if (first)
                                {
                                    first = false;
                                    first2 = false;
                                }
                                else if (first2)
                                {
                                    first2 = false;
                                    continue;
                                }

                                new SQLiteCommand("INSERT INTO '" + code + "' " +
                                    "('date', 'time', 'open', 'high', 'low', 'close', 'baseVolume', 'takerBuyBaseVolume', 'quoteVolume', 'takerBuyQuoteVolume', 'tradeCount') values " +
                                    "('" + kline.OpenTime.ToString("yyyyMMdd") + "', '" + kline.OpenTime.ToString("HHmmss") + "', '" + kline.Open + "', '" + kline.High + "', '" + kline.Low + "', '" + kline.Close
                                     + "', '" + kline.BaseVolume + "', '" + kline.TakerBuyBaseVolume + "', '" + kline.QuoteVolume + "', '" + kline.TakerBuyQuoteVolume + "', '" + kline.TradeCount + "')", conn).ExecuteNonQuery();
                            }

                            if (count == 0)
                                BaseFunctions.ShowError(form);

                            if (count != 1000)
                                break;
                            else
                                startTime = last.OpenTime;
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                        }
                    }

                    new SQLiteCommand("Commit", conn).ExecuteNonQuery();

                    doneCode++;
                }

                conn.Close();
                doneTime++;
            }

            BaseFunctions.HideLoading();
        }

        protected override int GetSticksCountBetween(string code, DateTime start, DateTime end, ChartValues chartValue)
        {
            var result = client.FuturesUsdt.Market.GetKlinesAsync(code, (KlineInterval)chartValue.original, start, end).Result;
            if (!result.Success || result.Data == null)
                BaseFunctions.ShowError(form);

            return result.Data.Count();
        }
    }
}
