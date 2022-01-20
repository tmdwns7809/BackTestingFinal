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

        public string DBTimeColumnName = "time";

        public string DBOpenColumnName = "open";
        public string DBHighColumnName = "high";
        public string DBLowColumnName = "low";
        public string DBCloseColumnName = "close";
        
        public string DBBaseVolumeColumnName = "baseVolume";
        public string DBTakerBuyBaseVolumeColumnName = "takerBuyBaseVolume";
        public string DBQuoteVolumeColumnName = "quoteVolume";
        public string DBTakerBuyQuoteVolumeColumnName = "takerBuyQuoteVolume";

        public string DBTradeCountColumnName = "tradeCount";

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

                //ChangeColumns();
                //DeleteIndex();
                //CreateIndex();
            }));};

            //ChangeNames();
        }

        protected abstract void ConnectAndGetCodeList();
        protected abstract void SaveAndUpdateDB();
        protected abstract int GetSticksCountBetween(string code, DateTime start, DateTime end, ChartValues chartValue);

        void CheckDB()
        {
            BaseFunctions.LoadingSettingFirst(form);

            var doneTime = 0;
            foreach (var pair in BaseFunctions.ChartValuesDic)
            {
                SQLiteConnection conn = new SQLiteConnection("Data Source =" + path + BaseName + pair.Value.Text + ".db");
                conn.Open();

                var codeList = new List<string>();
                var reader = new SQLiteCommand("Select name From sqlite_master where type='table'", conn).ExecuteReader();
                while (reader.Read())
                    codeList.Add(reader["name"].ToString());

                var doneCode = 0;
                foreach (var code in codeList)
                {
                    BaseFunctions.AddOrChangeLoadingText("Checking (" + doneTime + "/" + BaseFunctions.ChartValuesDic.Count + ")DB duplicate...(" + doneCode + "/" + codeList.Count + ")", doneCode == 0 && doneTime == 0);

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
                        var time = DateTime.ParseExact(reader[DBTimeColumnName].ToString(), BaseFunctions.DBTimeFormat, null);

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

            BaseFunctions.HideLoading();
            BaseFunctions.AlertStart("done");
        }

        void ChangeColumns()
        {
            if (!form.InvokeRequired)
                BaseFunctions.ShowError(form);

            var beforeFormText = form.Text;

            var doneTime = 0;
            foreach (var pair in BaseFunctions.ChartValuesDic)
            {
                var oldPath = path + BaseName + pair.Value.Text + ".db";
                var newPath = path + BaseName + pair.Value.Text + "_new.db";

                SQLiteConnection conn = new SQLiteConnection("Data Source =" + oldPath);
                conn.Open();
                SQLiteConnection conn2 = new SQLiteConnection("Data Source =" + newPath);
                conn2.Open();

                var codeList = new List<string>();
                var reader = new SQLiteCommand("Select name From sqlite_master where type='table'", conn).ExecuteReader();
                while (reader.Read())
                    codeList.Add(reader["name"].ToString());

                var firstText = "Changing (" + doneTime + "/" + BaseFunctions.ChartValuesDic.Count + ")DB ...(";

                var doneCode = 0;
                foreach (var code in codeList)
                {
                    form.Invoke(new Action(() => { form.Text = firstText + doneCode + "/" + codeList.Count + ")"; }));

                    new SQLiteCommand("Begin", conn2).ExecuteNonQuery();

                    new SQLiteCommand("CREATE TABLE IF NOT EXISTS '" + code + "' " +
                        "('" + DBTimeColumnName + "' INTEGER, '" + DBOpenColumnName + "' REAL, '" + DBHighColumnName + "' REAL, '" + 
                        DBLowColumnName + "' REAL, '" + DBCloseColumnName + "' REAL, '" + DBBaseVolumeColumnName + "' REAL, '" + DBTakerBuyBaseVolumeColumnName + "' REAL, '" + 
                        DBQuoteVolumeColumnName + "' REAL, '" + DBTakerBuyQuoteVolumeColumnName + "' REAL, '" + DBTradeCountColumnName + "' INTEGER)", conn2).ExecuteNonQuery();

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
                    while (reader.Read())
                    {
                        new SQLiteCommand("INSERT INTO '" + code + "' ('" + DBTimeColumnName + "', '" + DBOpenColumnName + "', '" + DBHighColumnName + "', '" +
                        DBLowColumnName + "', '" + DBCloseColumnName + "', '" + DBBaseVolumeColumnName + "', '" + DBTakerBuyBaseVolumeColumnName + "', '" +
                        DBQuoteVolumeColumnName + "', '" + DBTakerBuyQuoteVolumeColumnName + "', '" + DBTradeCountColumnName + "') values " +
                            "('" + reader["date"].ToString() + reader["time"].ToString() + "', '" + reader["open"].ToString() + "', '" + reader["high"].ToString() + "', '" + reader["low"].ToString() + "', '" + reader["close"].ToString()
                             + "', '" + reader["baseVolume"].ToString() + "', '" + reader["takerBuyBaseVolume"].ToString() + "', '" + reader["quoteVolume"].ToString() + "', '" + reader["takerBuyQuoteVolume"].ToString() + "', '" + reader["tradeCount"].ToString() + "')", conn2).ExecuteNonQuery();

                        lastRowIDAll = reader["rowid"].ToString();

                        if (first)
                        {
                            first = false;

                            if (firstRowID != lastRowIDAll)
                                BaseFunctions.ShowError(form);
                        }
                    }
                    if (lastRowID != lastRowIDAll)
                        BaseFunctions.ShowError(form);

                    new SQLiteCommand("Commit", conn2).ExecuteNonQuery();

                    doneCode++;
                }

                conn.Close();
                conn2.Close();

                //System.IO.File.Move(path + BaseName + pair.Value.Text + "_new.db", path + BaseName + pair.Value.Text + "_new.db");

                doneTime++;
            }

            form.Invoke(new Action(() => { form.Text = beforeFormText; }));
        }

        void CreateIndex()
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
                var reader = new SQLiteCommand("Select name From sqlite_master where type='table'", conn).ExecuteReader();
                while (reader.Read())
                    codeList.Add(reader["name"].ToString());

                var firstText = "Creating (" + doneTime + "/" + BaseFunctions.ChartValuesDic.Count + ")DB index ...(";

                var doneCode = 0;
                foreach (var code in codeList)
                {
                    form.Invoke(new Action(() => { form.Text = firstText + doneCode + "/" + codeList.Count + ")"; }));

                    new SQLiteCommand("Begin", conn).ExecuteNonQuery();

                    new SQLiteCommand("CREATE UNIQUE INDEX IF NOT EXISTS '" + code + "_index' ON '" + code + "' ('" + DBTimeColumnName + "')", conn).ExecuteNonQuery();

                    new SQLiteCommand("Commit", conn).ExecuteNonQuery();

                    doneCode++;
                }

                conn.Close();

                //System.IO.File.Move(path + BaseName + pair.Value.Text + "_new.db", path + BaseName + pair.Value.Text + "_new.db");

                doneTime++;
            }

            form.Invoke(new Action(() => { form.Text = beforeFormText; }));
        }

        void DeleteIndex()
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
                var reader = new SQLiteCommand("Select name From sqlite_master where type='table'", conn).ExecuteReader();
                while (reader.Read())
                    codeList.Add(reader["name"].ToString());

                var firstText = "Deleting (" + doneTime + "/" + BaseFunctions.ChartValuesDic.Count + ")DB index ...(";

                var doneCode = 0;
                foreach (var code in codeList)
                {
                    form.Invoke(new Action(() => { form.Text = firstText + doneCode + "/" + codeList.Count + ")"; }));

                    new SQLiteCommand("Begin", conn).ExecuteNonQuery();

                    new SQLiteCommand("DROP INDEX '" + code + "_index'", conn).ExecuteNonQuery();

                    new SQLiteCommand("Commit", conn).ExecuteNonQuery();

                    doneCode++;
                }

                conn.Close();

                //System.IO.File.Move(path + BaseName + pair.Value.Text + "_new.db", path + BaseName + pair.Value.Text + "_new.db");

                doneTime++;
            }

            form.Invoke(new Action(() => { form.Text = beforeFormText; }));
        }

        void ChangeNames()
        {
            foreach (var pair in BaseFunctions.ChartValuesDic)
            {
                try
                {
                    System.IO.File.Move(path + BaseName + pair.Value.Text + ".db", path + BaseName + pair.Value.Text + "_old.db");
                    System.IO.File.Move(path + BaseName + pair.Value.Text + "_new.db", path + BaseName + pair.Value.Text + ".db");
                }
                catch (Exception e)
                {

                    throw;
                }
            }
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
