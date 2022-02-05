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
        public double WinRate;
        public double WinProfitRateSum;
        public double ProfitRateSum;
        public double ProfitRateAvg;
        public double WinProfitRateAvg;
        public double LoseProfitRateAvg;

        public List<BackResultData> ResultDatasForMetric = new List<BackResultData>();

    }
}
