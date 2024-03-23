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

            DBManager.Manage(Settings.ProgramBackTesting);

            BackTesting.instance = new BackTesting(this, Settings.values[Settings.ProgramName].market[Settings.MarketsName] == Markets.KRX);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        //protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        //{
        //    switch (keyData)
        //    {
        //        case Keys.Left:
        //            BackTesting.instance.beforeButton.PerformClick();
        //            return true;

        //        case Keys.Right:
        //            BackTesting.instance.afterButton.PerformClick();
        //            return true;

        //        default:
        //            return base.ProcessCmdKey(ref msg, keyData);
        //    }
        //}
    }
}
