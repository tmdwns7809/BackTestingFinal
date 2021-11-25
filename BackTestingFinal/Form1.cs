using System.Windows.Forms;
using TradingLibrary.Base;
using System;

namespace BackTestingFinal
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            var settingsList = BaseFunctions.LoadSettings("BackTestingFinal");

            var futures = false;
            var update = false;
            var check = false;
            if (settingsList.Count == 0 || MessageBox.Show("like before?", "", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                BaseFunctions.isJoo = MessageBox.Show("isJoo?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
                if (!BaseFunctions.isJoo)
                    futures = MessageBox.Show("futures?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
                update = MessageBox.Show("update?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
                check = MessageBox.Show("check duplicate?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;

                settingsList.Clear();
                settingsList.Add(("1", "초기세팅", "isJoo:" + BaseFunctions.isJoo.ToString() + "," + "futures:" + futures.ToString() + "," + "update:" + update.ToString() + "," + "check:" + check.ToString() + ","));
                BaseFunctions.UpdateSettings("BackTestingFinal", settingsList);
            }
            else
            {
                if (settingsList.Count != 1)
                    BaseFunctions.ShowError(this);

                BaseFunctions.isJoo = bool.Parse(settingsList[0].data.Split(new string[] { "isJoo:" }, StringSplitOptions.None)[1].Split(',')[0]);
                futures = bool.Parse(settingsList[0].data.Split(new string[] { "futures:" }, StringSplitOptions.None)[1].Split(',')[0]);
                update = bool.Parse(settingsList[0].data.Split(new string[] { "update:" }, StringSplitOptions.None)[1].Split(',')[0]);
                check = bool.Parse(settingsList[0].data.Split(new string[] { "check:" }, StringSplitOptions.None)[1].Split(',')[0]);
            }


            if (BaseFunctions.isJoo)
                new JooSticksDB(this, update, check);
            else
                new BinanceSticksDB(this, update, check, futures);

            BackTesting.instance = new BackTesting(this, BaseFunctions.isJoo);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                    BackTesting.instance.beforeButton.PerformClick();
                    return true;

                case Keys.Right:
                    BackTesting.instance.afterButton.PerformClick();
                    return true;

                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}
