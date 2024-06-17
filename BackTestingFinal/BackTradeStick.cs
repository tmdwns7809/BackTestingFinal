using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingLibrary.Trading;
using TradingLibrary;
using TradingLibrary.Base;
using TradingLibrary.Base.Values.Chart;

namespace BackTestingFinal
{
    class BackTradeStick : TradeStick
    {
        public bool suddenBurst = false;
        public BackResultData resultData = default;
        public BackResultData resultData2 = default;

        public BackTradeStick(ChartValues cv, decimal firstPrice = 0, DateTime time = default) : base(cv, firstPrice, time)
        {
        }

        public static bool isEqual(BackTradeStick stick0, BackTradeStick stick1)
        {
            var limit = 0.001m;
            return 
                (stick1.Price[0] == stick1.Price[2] || stick0.Price[0] == stick0.Price[2] || stick0.Price[0] == stick1.Price[0] || Math.Abs(1 - stick0.Price[0] / stick1.Price[0]) < limit) &&
                (stick1.Price[1] == stick1.Price[2] || stick0.Price[1] == stick0.Price[2] || stick0.Price[1] == stick1.Price[1] || Math.Abs(1 - stick0.Price[1] / stick1.Price[1]) < limit) &&
                //(Price[2] == stick.Price[2] || Math.Abs(1 - Price[2] / stick.Price[2]) < limit) &&    open이 안맞는 경우가 많음
                (stick0.Price[3] == stick1.Price[3] || Math.Abs(1 - stick0.Price[3] / stick1.Price[3]) < limit) &&
                (stick0.Ms == 0 || stick1.Ms == 0 || stick0.Ms == stick1.Ms || Math.Abs(1 - stick0.Ms / stick1.Ms) < limit) &&
                (stick0.Md == 0 || stick1.Md == 0 || stick0.Md == stick1.Md || Math.Abs(1 - stick0.Md / stick1.Md) < limit) &&
                (stick0.TCount == stick1.TCount || (stick0.TCount != 0 && stick1.TCount != 0 && Math.Abs(1 - stick0.TCount / stick1.TCount) < limit)) &&
                stick0.Time == stick1.Time &&
                stick0.suddenBurst == stick1.suddenBurst;
        }
    }
}
