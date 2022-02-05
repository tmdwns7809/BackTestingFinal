using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingLibrary.Base;

namespace BackTestingFinal
{
    public class MetricData
    {
        public string MetricName;
        public string Market;
        public string Long;
        public string Short;
        
        public void SetText(int isLong, string text)
        {
            if (isLong == 0)
                Long = text;
            else
                Short = text;
        }
    }
}
