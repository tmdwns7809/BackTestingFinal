using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingFinal
{
    public class MetricVar
    {
        public bool isLong;

        public double CR = 1D;
        public double Kelly = 1D;
        public List<double> ProfitRates = new List<double>();

        public DrawDownData DD = default;
        public DrawDownData MDD = default;
        public DrawDownData LDD = default;

        public int highestHasItemsAtADay = int.MinValue;
        public DateTime highestHasItemsDate = default;
        public TimeSpan longestHasTime = TimeSpan.MinValue;
        public DateTime longestHasTimeStart = default;
        public string longestHasCode = "";
        public int SingleCount = 0;
        public int SingleWin = 0;
        public double SingleProfitRateSum = 0;
        public double SingleProfitWinRateSum = 0;
        public int AverageCount = 0;
        public int AverageWin = 0;
        public double AverageProfitRateSum = 0;
        public double AverageProfitRateMul = 1;
        public double AverageProfitWinRateSum = 0;

        public int disappearCount;
        public int lastDisappearCount;
        public double minKelly = 0.3;
        public double maxKelly = 0.5;
        public double lowestKelly = double.MaxValue;
        public double highestKelly = double.MinValue;
        public double beforeCR;

    }
}
