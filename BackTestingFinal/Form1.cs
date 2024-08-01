using System.Windows.Forms;
using TradingLibrary.Base;
using TradingLibrary.Base.Enum;
using TradingLibrary.Base.DB;
using System;
using ScrollType = System.Windows.Forms.DataVisualization.Charting.ScrollType;

namespace BackTestingFinal
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            //BackTesting.instance = new BackTesting(this, Settings.ProgramBackTesting, 8.4103m);     // 속도 버그 있음 개선 필요, 디비쪽 문제일듯?
            //BackTesting.instance = new BackTesting(this, Settings.ProgramBackTesting, 8.41031m);
            BackTesting.instance = new BackTesting(this, Settings.ProgramBackTesting, 100);

            FormClosed += Form1_FormClosed;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SticksDBManager.CloseAllDB();
        }

        protected override void WndProc(ref Message m)
        {
            BaseFunctions.VerticalWheel(m);
            base.WndProc(ref m);
        }

    }
}
