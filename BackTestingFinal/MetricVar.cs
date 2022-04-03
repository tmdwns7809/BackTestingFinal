﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingFinal
{
    public class MetricVar
    {
        public bool isLong;

        public decimal CR = 1m;
        public double Kelly = 1D;
        public List<decimal> ProfitRates = new List<decimal>();

        public DrawDownData DD = default;
        public DrawDownData MDD = default;

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
    }
}
