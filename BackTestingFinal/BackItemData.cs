using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingLibrary;

namespace BackTestingFinal
{
    class BackItemData
    {
        public string Code;
        public int number;
        public decimal hoDiff = decimal.MaxValue;
        public DateTime ShowingTime;
        public bool isMarket;

        public bool BaseReady = false;
        public bool Enter = false;
        public decimal EnterPrice;
        public DateTime EnterTime;
        public int EnterIndex;
        public int ExitIndex;
        public decimal SupportPrice;
        public int BeforeExitIndex;
        public int ShortestBeforeGap = int.MaxValue;
        public bool ExitException;

        public List<TradeStick> list = new List<TradeStick>();
        public List<int> enterIndexList = new List<int>();

        public int Count;
        public int Win;
        public string WinRate;

        public BackItemData(string c, int n)
        {
            Code = c;
            number = n;
        }
    }
}
