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
        public int Count = 0;
        public int Win = 0;
        public double ProfitRateSum = 0;
        public double ProfitWinRateSum = 0;

        public int HasItemsAtADay = 0;

        public int disappearCount;
        public int lastDisappearCount;
        public double minKelly = 0.5;
        public double maxKelly = 1;
        public double lowestKelly = double.MaxValue;
        public double highestKelly = double.MinValue;
        public double beforeCR;

    }
}
