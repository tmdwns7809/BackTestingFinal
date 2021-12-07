using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingLibrary;
using TradingLibrary.Base;

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

        public SortedList<ChartValues, ChartListDate> listDic;

        public int Count;
        public int Win;
        public string WinRate;

        public BackItemData(string c, int n, SortedList<ChartValues, ChartListDate> lD)
        {
            Code = c;
            number = n;
            listDic = lD;
        }
    }

    class ChartListDate
    {
        public List<TradeStick> list = new List<TradeStick>();
        public bool endLoaded = false;
        public int startIndex = 0;
        public int currentIndex = 0;
        public BackTradeStick lastStick;

        public void Reset()
        {
            list.Clear();
            endLoaded = false;
            startIndex = 0;
            currentIndex = 0;
        }
    }
}
