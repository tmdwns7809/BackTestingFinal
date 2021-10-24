using System.Windows.Forms;
using TradingLibrary.Base;
using TradingLibrary;

namespace BackTestingFinal
{
    public partial class Form1 : Form
    {
        BackTesting backTesting;
        bool update = false;
        bool isJoo = false;

        public Form1()
        {
            InitializeComponent();

            if (update)
                if (isJoo)
                    new JooDB(this, update);
                else
                    new BinanceDB(this, BinanceDB.path + BinanceDB.futures1mName);

            if (isJoo)
                backTesting = new BackTesting(this, JooDB.path, Commision.Joo, isJoo);
            else
                backTesting = new BackTesting(this, BinanceDB.path + BinanceDB.futures1mName, Commision.Binance, isJoo);
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
