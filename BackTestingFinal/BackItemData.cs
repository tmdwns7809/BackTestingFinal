using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingFinal
{
    class BackItemData
    {
        public string Code;
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

        public int Count;
        public int Win;
        public string WinRate;

        public BackItemData(string c)
        {
            Code = c;
        }
    }
}
