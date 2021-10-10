using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using TradingLibrary.Base;
using TradingLibrary;
using TradingLibrary.Trading;

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

        public BinanceDB(Form form)
        {
            this.form = form;

            client = new BinanceClient(new BinanceClientOptions { ApiCredentials = new ApiCredentials(BinanceBase.future2_API_Key, BinanceBase.future2_Secret_Key) });
        }

        void SetItemDataList()
        {
            var result = client.FuturesUsdt.System.GetExchangeInfoAsync().Result;
            if (!result.Success)
                Trading.ShowError();

            var c = 0;
            foreach (var s in result.Data.Symbols)
            {
                if (s.Name == "BTCSTUSDT")
                    continue;

                var itemData = new TradeItemData(s.Name);
                ItemDataList.Add(itemData.Name, itemData);

                c++;
            }
        }
    }
}
