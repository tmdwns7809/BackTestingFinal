using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using TradingLibrary.Base;
using System.Threading;

namespace BackTestingFinal
{
    abstract class BaseSticksDB
    {
        protected Form form;

        protected List<string> codeList = new List<string>();

        public static string path;
        public static string BaseName;

        protected BaseSticksDB(Form f, string p, string b, bool update, bool check)
        {
            form = f;
            path = p;
            BaseName = b;

            ConnectAndGetCodeList();

            form.Shown += (sender, e) => { Task.Run(new Action(() => 
            { 
                if (update)
                    SaveAndUpdateDB();
                
                if (check)
                    CheckDB();
            })); };
        }

        protected abstract void ConnectAndGetCodeList();
        protected abstract void SaveAndUpdateDB();
        protected abstract int GetSticksCountBetween(string code, DateTime start, DateTime end, ChartValues chartValue);

        void CheckDB()
        {
            if (!form.InvokeRequired)
                BaseFunctions.ShowError(form);

            var beforeFormText = form.Text;

            var doneTime = 0;
            foreach (var pair in BaseFunctions.ChartValuesDic)
            {
                SQLiteConnection conn = new SQLiteConnection("Data Source =" + path + BaseName + pair.Value.Text + ".db");
                conn.Open();

                var codeList = new List<string>();
                var reader = new SQLiteCommand("Select name From sqlite_master where type='table' order by name", conn).ExecuteReader();
                while (reader.Read())
                    codeList.Add(reader["name"].ToString());

                var firstText = "Checking (" + doneTime + "/" + BaseFunctions.ChartValuesDic.Count + ")DB duplicate...(";

                var doneCode = 0;
                foreach (var code in codeList)
                {
                    form.Invoke(new Action(() => { form.Text = firstText + doneCode + "/" + codeList.Count + ")"; }));

                    var firstRowID = "";
                    var lastRowID = "";
                    reader = new SQLiteCommand("Select *, rowid From '" + code + "' order by rowid limit 1", conn).ExecuteReader();
                    if (reader.Read())
                        firstRowID = reader["rowid"].ToString();
                    else
                        BaseFunctions.ShowError(form);
                    reader = new SQLiteCommand("Select *, rowid From '" + code + "' order by rowid desc limit 1", conn).ExecuteReader();
                    if (reader.Read())
                        lastRowID = reader["rowid"].ToString();
                    else
                        BaseFunctions.ShowError(form);

                    reader = new SQLiteCommand("Select *, rowid From '" + code + "' ", conn).ExecuteReader();
                    var first = true;
                    var lastRowIDAll = "";
                    DateTime lastTime = default;
                    while (reader.Read())
                    {
                        var time = DateTime.Parse(reader["date"].ToString().Insert(4, "-").Insert(7, "-") + " " + reader["time"].ToString().Insert(2, ":").Insert(5, ":"));

                        if (lastTime != default 
                            && (pair.Value != BaseChartTimeSet.OneMonth ? lastTime.AddSeconds(pair.Value.seconds) : lastTime.AddMonths(1) ) != time
                            && GetSticksCountBetween(code, lastTime, time, pair.Value) != 2)
                            BaseFunctions.ShowError(form);

                        lastRowIDAll = reader["rowid"].ToString();
                        lastTime = time;

                        if (first)
                        {
                            first = false;

                            if (firstRowID != lastRowIDAll)
                                BaseFunctions.ShowError(form);
                        }
                    }
                    if (lastRowID != lastRowIDAll)
                        BaseFunctions.ShowError(form);

                    doneCode++;
                }

                conn.Close();
                doneTime++;
            }

            form.Invoke(new Action(() => { form.Text = beforeFormText; }));
        }

        void DeleteAllLast()    // 일봉DB에 똑같은 날짜가 두개씩 저장된 적이 있었음 증권사쪽 오류로 추정, 그 두개 들어온 데이터중 하나를 지우기 위한 함수
        {
            foreach (var pair in BaseFunctions.ChartValuesDic)
            {
                SQLiteConnection conn = new SQLiteConnection("Data Source =" + path + BaseName + pair.Value.Text + ".db");
                conn.Open();

                foreach (var code in codeList)
                {
                    new SQLiteCommand("Begin", conn).ExecuteNonQuery();

                    new SQLiteCommand("CREATE TABLE IF NOT EXISTS '" + code + "' " +
                        "('date' INTEGER, 'open' REAL, 'high' REAL, 'low' REAL, 'close' REAL, 'volume' REAL)", conn).ExecuteNonQuery();

                    var reader = new SQLiteCommand("Select *, rowid From '" + code + "' " +
                        "where date='20211008'", conn).ExecuteReader();
                    var count = 0;
                    var rowidList = new List<string>();
                    while (reader.Read())
                    {
                        count++;
                        rowidList.Add(reader["rowid"].ToString());
                    }

                    if (count != 2)
                        form.Invoke(new Action(() => { MessageBox.Show("오류"); }));

                    new SQLiteCommand("DELETE FROM '" + code + "' WHERE date='20211008' and rowid='" + rowidList[1] + "'", conn).ExecuteNonQuery();

                    new SQLiteCommand("Commit", conn).ExecuteNonQuery();
                }

                conn.Close();
            }
        }
    }
}
