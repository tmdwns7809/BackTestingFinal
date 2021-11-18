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

            BaseFunctions.isJoo = MessageBox.Show("isJoo?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
            var futures = false;
            if (!BaseFunctions.isJoo)
                futures = MessageBox.Show("futures?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
            var update = MessageBox.Show("update?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
            var check = MessageBox.Show("check duplicate?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;

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
