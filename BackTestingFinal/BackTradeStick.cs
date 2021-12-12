using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingLibrary.Trading;
using TradingLibrary;

namespace BackTestingFinal
{
    class BackTradeStick : TradeStick
    {
        public BackTradeStick(BackTradeStick stick = default) : base(stick)
        {
            if (stick == default)
                return;

            suddenBurst = stick.suddenBurst;
            resultData = stick.resultData;
        }

        public bool suddenBurst = false;
        public BackResultData resultData = default;

        public bool isEqual(BackTradeStick stick)
        {
            var limit = 0.001m;
            return 
                (Price[0] == stick.Price[0] || Math.Abs(1 - Price[0] / stick.Price[0]) < limit) &&
                (Price[1] == stick.Price[1] || Math.Abs(1 - Price[1] / stick.Price[1]) < limit) &&
                (Price[2] == stick.Price[2] || Math.Abs(1 - Price[2] / stick.Price[2]) < limit) &&
                (Price[3] == stick.Price[3] || Math.Abs(1 - Price[3] / stick.Price[3]) < limit) &&
                (Ms == stick.Ms || Math.Abs(1 - Ms / stick.Ms) < limit) &&
                (Md == stick.Md || Math.Abs(1 - Md / stick.Md) < limit) &&
                (TCount == stick.TCount || (TCount != 0 && stick.TCount != 0 && Math.Abs(1 - TCount / stick.TCount) < limit)) &&
                Time == stick.Time &&
                suddenBurst == stick.suddenBurst;
        }
    }
}
