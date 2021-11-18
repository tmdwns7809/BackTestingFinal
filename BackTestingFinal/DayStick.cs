using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingLibrary.Base;

namespace BackTestingFinal
{
    public class DayStick : Stick
    {
        public decimal Volume;

        public double RSI;
        public double RSI2;

        public int Date;
    }
}
