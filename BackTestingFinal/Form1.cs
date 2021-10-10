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

            if (isJoo)
                backTesting = new BackTesting(this, JooDB.path, Commision.Joo);
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
