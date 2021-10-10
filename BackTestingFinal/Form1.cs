using System.Windows.Forms;
using TradingLibrary;

namespace BackTestingFinal
{
    public partial class Form1 : Form
    {
        BackTesting backTesting;

        public Form1()
        {
            InitializeComponent();

            backTesting = new BackTesting(this, @"C:\Users\tmdwn\source\repos\CybosPlus\DaySticks.db", 0.3m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                    backTesting.beforeButton.PerformClick();
                    return true;

                case Keys.Right:
                    backTesting.afterButton.PerformClick();
                    return true;

                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}
