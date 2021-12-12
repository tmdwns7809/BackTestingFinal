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

        public bool BaseReady;
        public bool Enter;
        public TimeSpan ShortestBeforeGap;
        public string ShortestBeforeGapText;
        public int Count;
        public int Win;
        public string WinRate;
        public bool ExitException;

        public decimal EnterPrice;
        public DateTime EnterTime;
        public DateTime BeforeExitTime;
        public List<(DateTime foundTime, ChartValues chartValues)> EnterFoundList;

        public SortedList<ChartValues, ChartListDate> listDic;

        public BackItemData(string c, int n, SortedList<ChartValues, ChartListDate> lD)
        {
            Code = c;
            number = n;
            listDic = lD;
        }

        public void Reset()
        {
            Enter = false;
            BaseReady = false;
            ShortestBeforeGap = TimeSpan.MaxValue;
            ShortestBeforeGapText = "";
            Count = 0;
            Win = 0;
            WinRate = "";
            ExitException = false;
        }
    }

    class ChartListDate
    {
        public List<TradeStick> list = new List<TradeStick>();
        public bool endLoaded = false;
        public bool found = false;
        public int startIndex = 0;
        public int currentIndex = 0;
        public BackTradeStick lastStick;

        public void Reset()
        {
            list.Clear();
            endLoaded = false;
            found = false;
            startIndex = 0;
            currentIndex = 0;
        }
    }
}
