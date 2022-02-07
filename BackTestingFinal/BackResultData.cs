using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingFinal
{
    public class BackResultData
    {
        public int NumberForSingle;
        public int NumberForClick;
        public string Code;
        public DateTime EnterTime;
        public DateTime ExitTime;
        public double ProfitRate = 0;
        public int Count = 0;
        public double CumulativeReturnWhenEnter;
        public string Duration;
        public string BeforeGap;
        public int LorS;    // 0 L 1 S
    }
}
