using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using TradingLibrary.Base;

namespace BackTestingFinal
{
    sealed class JooSticksDB : BaseSticksDB
    {
        CPSYSDIBLib.StockChart StockChart = new CPSYSDIBLib.StockChart();
        CPUTILLib.CpCodeMgr CpCodeMgr = new CPUTILLib.CpCodeMgr();

        public static string JooBaseName = "Sticks_";
        public static string JooPath = @"C:\Users\tmdwn\source\repos\BigFilesCantSaveGit\SticksDB\Joo\";

        public JooSticksDB(Form f, bool update, bool check) : base (f, JooPath, JooBaseName, update, check) { }

        protected override void ConnectAndGetCodeList()
        {
            CPUTILLib.CpCybos cpCybos = new CPUTILLib.CpCybos();
            if (cpCybos.IsConnect == 0)
            {
                var sErrMsg = "PLUS 연결 필요합니다";
                form.Invoke(new Action(() => { MessageBox.Show(sErrMsg, "실행확인", MessageBoxButtons.OK, MessageBoxIcon.Error); }));
            }

            //codeList.Add("U001");   //kospi
            //codeList.Add("U201");   //kosdaq
            var kospi = CpCodeMgr.GetStockListByMarket(CPUTILLib.CPE_MARKET_KIND.CPC_MARKET_KOSPI);
            var kosdaq = CpCodeMgr.GetStockListByMarket(CPUTILLib.CPE_MARKET_KIND.CPC_MARKET_KOSDAQ);
            foreach (var code in kospi)
                codeList.Add(code);
            foreach (var code in kosdaq)
                codeList.Add(code);
        }

        protected override void SaveAndUpdateDB()
        {
            var conn = new SQLiteConnection("Data Source =" + path + BaseName + ".db");
            conn.Open();

            var start = 10000000;
            var list = new List<JooDBStick>();
            StockChart.Received += new CPSYSDIBLib._ISysDibEvents_ReceivedEventHandler(() =>
            {
                var code = StockChart.GetHeaderValue(0);
                var count = (long)StockChart.GetHeaderValue(3);

                for (int i = 0; i < count; i++)
                {
                    var stick = new JooDBStick()
                    {
                        date = (int)StockChart.GetDataValue(0, i),
                        open = (decimal)StockChart.GetDataValue(1, i),
                        high = (decimal)StockChart.GetDataValue(2, i),
                        low = (decimal)StockChart.GetDataValue(3, i),
                        close = (decimal)StockChart.GetDataValue(4, i),
                        volume = (decimal)StockChart.GetDataValue(5, i)
                    };
                    list.Add(stick);

                    if (i == count - 1 && stick.date - 1 >= start)
                    {
                        StockChart.SetInputValue(0, code);
                        StockChart.SetInputValue(1, '1');
                        StockChart.SetInputValue(2, stick.date - 1);
                        StockChart.SetInputValue(3, start);
                        StockChart.SetInputValue(5, new object[] { 0, 2, 3, 4, 5, 8 });
                        StockChart.SetInputValue(6, 'D');
                        StockChart.SetInputValue(9, '1');

                        Thread.Sleep(300);
                        StockChart.BlockRequest();
                    }
                }
            });

            var count2 = 0;
            foreach (var code in codeList)
            {
                count2++;

                start = 10000000;
                list = new List<JooDBStick>();

                new SQLiteCommand("Begin", conn).ExecuteNonQuery();

                new SQLiteCommand("CREATE TABLE IF NOT EXISTS '" + code + "' " +
                    "('date' INTEGER, 'open' REAL, 'high' REAL, 'low' REAL, 'close' REAL, 'volume' REAL)", conn).ExecuteNonQuery();

                var reader = new SQLiteCommand("Select * From '" + code + "' " +
                    "where date<'" + DateTime.Now.ToString("yyyyMMdd") + "' order by date desc limit 1", conn).ExecuteReader();
                while (reader.Read())
                    start = int.Parse(reader["date"].ToString()) + 1;

                var today = int.Parse(DateTime.Now.ToString("yyyyMMdd")) - 1;
                StockChart.SetInputValue(0, code);
                StockChart.SetInputValue(1, '1');
                StockChart.SetInputValue(2, today);
                StockChart.SetInputValue(3, start);
                StockChart.SetInputValue(5, new object[] { 0, 2, 3, 4, 5, 8 });
                StockChart.SetInputValue(6, 'D');
                StockChart.SetInputValue(9, '1');

                Thread.Sleep(300);
                StockChart.BlockRequest();

                list.Reverse();
                for (int i = 0; i < list.Count; i++)
                    new SQLiteCommand("INSERT INTO '" + code + "' ('date', 'open', 'high', 'low', 'close', 'volume') values " +
                        "('" + list[i].date + "', '" + list[i].open + "', '" + list[i].high + "', '" + list[i].low + "', '" + list[i].close + "', '" + list[i].volume + "')", conn).ExecuteNonQuery();

                new SQLiteCommand("Commit", conn).ExecuteNonQuery();
            }

            conn.Close();
        }

        protected override int GetSticksCountBetween(string code, DateTime start, DateTime end, ChartValues chartValue)
        {
            return 0;
        }
    }

    public class JooDBStick
    {
        public int date;
        public decimal open;
        public decimal high;
        public decimal low;
        public decimal close;
        public decimal volume;
    }
}
