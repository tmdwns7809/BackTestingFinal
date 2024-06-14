using System.Windows.Forms;
using TradingLibrary.Base;
using TradingLibrary.Base.Enum;
using TradingLibrary.Base.DB;
using System;

namespace BackTestingFinal
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            BackTesting.instance = new BackTesting(this, Settings.ProgramBackTesting, 8.4103m);
            //BackTesting.instance = new BackTesting(this, Settings.ProgramBackTesting, 8.412m);
        }
    }
}
