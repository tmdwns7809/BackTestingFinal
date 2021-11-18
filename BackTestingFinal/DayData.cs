using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingLibrary;

namespace BackTestingFinal
{
    public class DayData
    {
        public DateTime Date;
        public List<BackResultData> resultDatas = new List<BackResultData>();
        public List<TradeStick> day_sticks_for_market = new List<TradeStick>();

        public int Number;
        public int Count;
        public int Win;
        public decimal WinRate;
        public decimal WinProfitRateSum;
        public decimal ProfitRateSum;
        public decimal ProfitRateAvg;
        public decimal WinProfitRateAvg;
        public decimal LoseProfitRateAvg;

        public DayStick KOSPIstick;
    }
}
