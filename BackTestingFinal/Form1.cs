using System.Windows.Forms;
using TradingLibrary.Base;
using TradingLibrary.Base.Enum;
using TradingLibrary.Base.Settings;
using TradingLibrary.Base.SticksDB;
using System;

namespace BackTestingFinal
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Settings.Load(Settings.ProgramBackTesting);
            var setting = Settings.settings[Settings.ProgramName];

            if (setting.market[Settings.MarketsName] == Markets.KRX)
                new JooSticksDB(this, setting.others[Settings.DoUpdate], setting.others[Settings.DoCheckError]);
            else
                new BinanceSticksDB(this, setting.others[Settings.DoUpdate], setting.others[Settings.DoCheckError],
                    setting.market[Settings.MarketsName] == Markets.BinancenFuturesUSDT);

            BackTesting.instance = new BackTesting(this, setting.market[Settings.MarketsName] == Markets.KRX);
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
