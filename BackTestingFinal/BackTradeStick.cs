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
            suddenBurst2 = stick.suddenBurst2;
        }

        public bool suddenBurst = false;
        public bool suddenBurst2 = false;

        public bool isEqual(BackTradeStick stick)
        {
            return 
                Price[0] == stick.Price[0] &&
                Price[1] == stick.Price[1] &&
                Price[2] == stick.Price[2] &&
                Price[3] == stick.Price[3] &&
                Ms == stick.Ms &&
                Md == stick.Md &&
                TCount == stick.TCount &&
                Time == stick.Time &&
                suddenBurst == stick.suddenBurst &&
                suddenBurst2 == stick.suddenBurst2;
        }
    }
}
