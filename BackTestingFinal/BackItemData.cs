using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingLibrary;
using TradingLibrary.Base;

namespace BackTestingFinal
{
    class BackItemData : BaseItemData
    {
        public decimal hoDiff = decimal.MaxValue;

        public bool BaseReady;
        public TimeSpan ShortestBeforeGap;
        public string ShortestBeforeGapText;
        public int Count;
        public int Win;
        public double WinRate = -1;
        public bool ExitException;

        public DateTime BeforeExitTime;
        public (DateTime firstMin, DateTime lastMin) firstLastMin;

        public BackResultData[] resultDataForMetric = new BackResultData[2];

        public BackItemData(string c, int n) : base (c, n) {}

        public void Reset()
        {
            positionData = new PositionData[] { new PositionData(Position.Long), new PositionData(Position.Short) };
            positionData2 = new PositionData[] { new PositionData(Position.Long), new PositionData(Position.Short) };
            BaseReady = false;
            ShortestBeforeGap = TimeSpan.MaxValue;
            ShortestBeforeGapText = "";
            Count = 0;
            Win = 0;
            ExitException = false;
        }
    }
}
