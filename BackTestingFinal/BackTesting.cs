using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System.Drawing;
using System.Data.SQLite;
using BrightIdeasSoftware;
using System.IO;
using TradingLibrary;
using TradingLibrary.Trading;
using TradingLibrary.Base;

namespace BackTestingFinal
{
    public sealed class BackTesting : BaseFunctions
    {
        public static BackTesting instance;

        BackItemData showingItemData;

        SortedList<DateTime, DayData> simulDays = new SortedList<DateTime, DayData>();
        SortedList<DateTime, DayData> marketDays = new SortedList<DateTime, DayData>();

        int st = 0;
        static int RSINumber = 7;
        int searchDay = RSINumber + 1;

        Action<DayData> clickResultAction;
        FastObjectListView clickResultListView = new FastObjectListView();

        int start = 0;
        int end = 99999999;

        public Chart totalChart = new Chart();
        public Button totalButton = new Button();
        public Button captureButton = new Button();
        public Button beforeButton = new Button();
        public Button afterButton = new Button();
        public TextBox fromTextBox = new TextBox();
        public TextBox midTextBox = new TextBox();
        public TextBox toTextBox = new TextBox();
        public Button runButton = new Button();
        public Button yearBeforeButton = new Button();
        public Button yearAfterButton = new Button();
        public Button firstButton = new Button();
        public Button lastButton = new Button();
        public FastObjectListView metricListView = new FastObjectListView();
        public FastObjectListView allListView = new FastObjectListView();
        public FastObjectListView codeListView = new FastObjectListView();
        public FastObjectListView singleResultListView = new FastObjectListView();


        int ready_start_date = int.MaxValue;
        int ready_end_date = int.MinValue;

        string resultImagePath;

        Dictionary<string, BackItemData> itemDataDic = new Dictionary<string, BackItemData>();
        Dictionary<string, MetricData> metricDic = new Dictionary<string, MetricData>();

        string KOSPI = "U001";
        string KOSDAQ = "U201";

        SortedList<int, int> openDaysPerYear = new SortedList<int, int>();

        string MetricCR = "CR";
        string MetricCAGR = "CAGR";
        string MetricMDD = "MDD";
        string MetricMDDStart = "MDDStart";
        string MetricMDDEnd = "MDDEnd";
        string MetricLDDD = "LDD Days";
        string MetricLDDDStart = "LDDDStart";
        string MetricLDDDEnd = "LDDDEnd";
        string MetricStart = "    Start";
        string MetricEnd = "    End";
        string MetricWinRate = "Win Rate";
        string MetricWinRateYearStandartDeviation = "    WRYSD";
        string MetricAllCount = "    Count";
        string MetricAvgProfitRate = "    APR";
        string MetricWinAvgProfitRate = "    WAPR";
        string MetricLoseAvgProfitRate = "    LAPR";
        string MetricMaxHas = "Max Has";
        string MetricMaxHasDate = "    Date";
        string MetricLongestHasDays = "LH Days";
        string MetricLongestHasDaysCode = "    Code";
        string MetricAverageHasDays = "AVGH Days";

        string sticksDBpath;
        string sticksDBbaseName;

        public BackTesting(Form form, bool isJoo) : base(form, isJoo)
        {
            sticksDBpath = BaseSticksDB.path;
            sticksDBbaseName = BaseSticksDB.BaseName;

            var start = sticksDBpath.LastIndexOf('\\');
            var image_folder = sticksDBpath.Substring(0, start) + "image";
            var dir = new DirectoryInfo(image_folder);
            if (dir.Exists == false)
                dir.Create();
            resultImagePath = image_folder + @"\";

            LoadCodeListAndMetric();

            SetAdditionalMainView();
        }
        void SetAdditionalMainView()
        {
            //mainChart.Size = new Size(mainChart.Width, Screen.GetWorkingArea(form).Size.Height - 30);

            clickResultAction = new Action<DayData>((date) =>
            {
                clickResultListView.ClearObjects();

                foreach (var data in date.resultDatas)
                    if (data.EnterTime.Date >= simulDays.Keys[start] && data.ExitTime.Date <= simulDays.Keys[end])
                        clickResultListView.AddObject(data);

                clickResultListView.Sort(clickResultListView.GetColumn("Profit"), SortOrder.Ascending);
                var n = 0;
                foreach (BackResultData ob in clickResultListView.Objects)
                    ob.NumberForClick = ++n;
                clickResultListView.Sort(clickResultListView.GetColumn("No."), SortOrder.Descending);
            });

            SetChart(totalChart, new Size(mainChart.Width, mainChart.Height), new Point(mainChart.Location.X, mainChart.Location.Y));
            totalChart.Hide();
            totalChart.Click += (sender, e) =>
            {
                var e2 = e as MouseEventArgs;
                if (e2.Button == MouseButtons.Left)
                {
                    var index = (int)(totalChart.ChartAreas[0].AxisX.PixelPositionToValue(e2.X) - 0.5);
                    var index2 = (int)(totalChart.ChartAreas[0].AxisY2.PixelPositionToValue(e2.Y) - 0.5);

                    if (index2 < totalChart.ChartAreas[0].AxisY2.ScaleView.ViewMinimum || index < 0 || index >= totalChart.Series[0].Points.Count)
                        return;

                    form.Text = totalChart.Series[0].Points[index].AxisLabel + "    " + totalChart.Series[0].Points[index].YValues[0] + "    " + totalChart.Series[1].Points[index].YValues[0]
                        + "    " + totalChart.Series[2].Points[index].YValues[0];

                    clickResultAction(simulDays[DateTime.Parse(totalChart.Series[0].Points[index].AxisLabel)]);
                }
            };

            var chartArea6 = totalChart.ChartAreas.Add("ChartAreaResult");
            chartArea6.Position = new ElementPosition(0, 0, 100, 100);
            chartArea6.BackColor = ColorSet.ControlBack;
            chartArea6.BorderColor = ColorSet.FormText;

            chartArea6.AxisX.MajorGrid.LineColor = ColorSet.ChartGrid;
            chartArea6.AxisX.MajorGrid.Interval = 250;
            chartArea6.AxisX.MajorTickMark.Enabled = false;
            chartArea6.AxisX.LabelStyle.Interval = 250;
            chartArea6.AxisX.LabelStyle.Format = "yyyyMMdd";
            chartArea6.AxisX.LabelStyle.ForeColor = ColorSet.FormText;
            chartArea6.AxisX.ScrollBar.Enabled = false;
            chartArea6.AxisX.LineColor = ColorSet.Border;
            chartArea6.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;

            chartArea6.AxisY.Enabled = AxisEnabled.True;
            chartArea6.AxisY.MajorGrid.LineColor = Color.Transparent;
            chartArea6.AxisY.ScrollBar.Enabled = false;
            chartArea6.AxisY.MajorTickMark.Enabled = false;
            chartArea6.AxisY.LabelStyle.ForeColor = ColorSet.FormText;
            chartArea6.AxisY.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartArea6.AxisY.LineColor = ColorSet.Border;
            chartArea6.AxisY2.Enabled = AxisEnabled.True;
            chartArea6.AxisY2.MajorGrid.LineColor = ColorSet.ChartGrid;
            chartArea6.AxisY2.ScrollBar.Enabled = false;
            chartArea6.AxisY2.MajorTickMark.Enabled = false;
            chartArea6.AxisY2.IsStartedFromZero = false;
            chartArea6.AxisY2.LabelStyle.Format = "{0:0.00}%";
            chartArea6.AxisY2.LabelStyle.ForeColor = ColorSet.FormText;
            chartArea6.AxisY2.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartArea6.AxisY2.LineColor = ColorSet.Border;
            chartArea6.AxisY2.StripLines.Add(new StripLine() { IntervalOffset = 0, BackColor = ColorSet.Border, StripWidth = 0.5 });

            var series0 = totalChart.Series.Add("Market");
            series0.ChartType = SeriesChartType.Line;
            series0.XValueType = ChartValueType.Time;
            series0.Color = ColorSet.ChartGrid;
            series0.YAxisType = AxisType.Secondary;
            series0.ChartArea = chartArea6.Name;

            var series1 = totalChart.Series.Add("Simulation");
            series1.ChartType = SeriesChartType.Line;
            series1.XValueType = ChartValueType.Time;
            series1.Color = Color.SkyBlue;
            series1.YAxisType = AxisType.Secondary;
            series1.ChartArea = chartArea6.Name;

            var series2 = totalChart.Series.Add("Has");
            series2.ChartType = SeriesChartType.Line;
            series2.XValueType = ChartValueType.Time;
            series2.Color = Color.FromArgb(20, Color.SkyBlue);
            series2.YAxisType = AxisType.Primary;
            series2.ChartArea = chartArea6.Name;

            var lastButton = buttonDic.Values.Last();

            SetButton(totalButton, "T", (sender, e) =>
            {
                if (!totalChart.Visible)
                {
                    mainChart.Visible = false;
                    totalChart.Visible = true;

                    foreach (var b in buttonDic.Values)
                        b.BackColor = ColorSet.Button;
                    totalButton.BackColor = ColorSet.ButtonSelected;
                }
            });
            totalButton.Size = lastButton.Size;
            totalButton.Location = new Point(lastButton.Location.X, lastButton.Location.Y + lastButton.Height + 5);

            SetButton(captureButton, "", (sender, e) =>
            {
                //if (!marketReady)
                //    return;

                //dayChart.Size = new Size(300, 800);

                //chartArea0.Position = new ElementPosition(0, 0, 100, 100 / 6);
                //chartArea1.Position = new ElementPosition(0, chartArea0.Position.Y + chartArea0.Position.Height, 100, 100 / 6);
                //chartArea2.Position = new ElementPosition(0, chartArea1.Position.Y + chartArea1.Position.Height, 100, 100 / 6);
                //chartArea3.Position = new ElementPosition(0, chartArea2.Position.Y + chartArea2.Position.Height, 100, 100 / 6);
                //chartArea4.Position = new ElementPosition(0, chartArea3.Position.Y + chartArea3.Position.Height, 100, 100 / 6);
                //chartArea5.Position = new ElementPosition(0, chartArea4.Position.Y + chartArea4.Position.Height, 100, 100 / 6);
                //chartArea0.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);
                //chartArea1.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);
                //chartArea2.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);
                //chartArea3.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);
                //chartArea4.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);
                //chartArea5.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);
                //chartArea0.AxisX.ScrollBar.Enabled = false;
                //chartArea1.AxisX.ScrollBar.Enabled = false;
                //chartArea2.AxisX.ScrollBar.Enabled = false;
                //chartArea3.AxisX.ScrollBar.Enabled = false;
                //chartArea4.AxisX.ScrollBar.Enabled = false;
                //chartArea5.AxisX.ScrollBar.Enabled = false;
                //chartArea0.AxisX.MajorGrid.LineColor = Color.Transparent;
                //chartArea1.AxisX.MajorGrid.LineColor = Color.Transparent;
                //chartArea2.AxisX.MajorGrid.LineColor = Color.Transparent;
                //chartArea3.AxisX.MajorGrid.LineColor = Color.Transparent;
                //chartArea4.AxisX.MajorGrid.LineColor = Color.Transparent;
                //chartArea5.AxisX.MajorGrid.LineColor = Color.Transparent;
                //chartArea0.AxisX.MajorTickMark.Enabled = false;
                //chartArea1.AxisX.MajorTickMark.Enabled = false;
                //chartArea2.AxisX.MajorTickMark.Enabled = false;
                //chartArea3.AxisX.MajorTickMark.Enabled = false;
                //chartArea4.AxisX.MajorTickMark.Enabled = false;
                //chartArea5.AxisX.MajorTickMark.Enabled = false;
                //chartArea0.AxisX.LabelStyle.Enabled = false;
                //chartArea1.AxisX.LabelStyle.Enabled = false;
                //chartArea2.AxisX.LabelStyle.Enabled = false;
                //chartArea3.AxisX.LabelStyle.Enabled = false;
                //chartArea4.AxisX.LabelStyle.Enabled = false;
                //chartArea5.AxisX.LabelStyle.Enabled = false;
                //chartArea0.AxisY.LabelStyle.Enabled = false;
                //chartArea1.AxisY.LabelStyle.Enabled = false;
                //chartArea2.AxisY.LabelStyle.Enabled = false;
                //chartArea3.AxisY.LabelStyle.Enabled = false;
                //chartArea4.AxisY.LabelStyle.Enabled = false;
                //chartArea5.AxisY.LabelStyle.Enabled = false;
                //chartArea0.AxisY2.LabelStyle.Enabled = false;
                //chartArea1.AxisY2.LabelStyle.Enabled = false;
                //chartArea2.AxisY2.LabelStyle.Enabled = false;
                //chartArea3.AxisY2.LabelStyle.Enabled = false;
                //chartArea4.AxisY2.LabelStyle.Enabled = false;
                //chartArea5.AxisY2.LabelStyle.Enabled = false;

                //chartArea0.AxisX.ScaleView.ZoomReset();
                //dayChart.SaveImage(resultImagePath + "chart.png", ChartImageFormat.Png);

                //var count = 0;
                //foreach (var day in simulDays.Values)
                //    foreach (var data in day.resultDatas)
                //        if (data.EnterDate == day.Date)
                //        {
                //            count++;

                //            ShowChart(itemDataDic[data.Code], 30, day.Date - 1, false, false);
                //            chartArea0.AxisX.ScaleView.ZoomReset();
                //            dayChart.SaveImage(resultImagePath + (data.ProfitRate > 1 ? @"win\" : @"lose\") + Math.Round(data.ProfitRate, 4) + "_" + data.EnterDate + "_" + data.Code + "_" + count + ".png", ChartImageFormat.Png);

                //        }
            });
            captureButton.Size = new Size(totalButton.Width / 2, totalButton.Height / 2);
            captureButton.Location = new Point(totalButton.Location.X, totalButton.Location.Y + totalButton.Height + 5);
        }
        protected override void SetRestView()
        {
            #region From_To_Run
            SetTextBox(fromTextBox, NowTime().AddDays(-30).ToString(DateTimeFormat));
            fromTextBox.ReadOnly = false;
            fromTextBox.BorderStyle = BorderStyle.Fixed3D;
            fromTextBox.Size = new Size((Screen.GetWorkingArea(form).Size.Width - mainChart.Location.X - mainChart.Width) / 10 * 4 - 10, 30);
            fromTextBox.Location = new Point(mainChart.Location.X + mainChart.Width + 5, mainChart.Location.Y + 5);

            SetTextBox(midTextBox, "~");
            midTextBox.Size = new Size((Screen.GetWorkingArea(form).Size.Width - mainChart.Location.X - mainChart.Width) / 10 - 10, fromTextBox.Height);
            midTextBox.Location = new Point(fromTextBox.Location.X + fromTextBox.Width + 10, fromTextBox.Location.Y);

            SetTextBox(toTextBox, NowTime().ToString(DateTimeFormat));
            toTextBox.ReadOnly = false;
            toTextBox.BorderStyle = BorderStyle.Fixed3D;
            toTextBox.Size = new Size(fromTextBox.Width, fromTextBox.Height);
            toTextBox.Location = new Point(midTextBox.Location.X + midTextBox.Width + 10, midTextBox.Location.Y);

            SetButton(runButton, "Run",
                (sender, e) =>
                {
                    start = int.TryParse(fromTextBox.Text, out int fromText) ? fromText : int.MinValue;
                    end = int.TryParse(toTextBox.Text, out int toText) ? toText : int.MaxValue;
                    if (start > end)
                         ShowError(form);
                    RunTotal(start, end);
                });
            runButton.Size = new Size(midTextBox.Width, toTextBox.Height);
            runButton.Location = new Point(toTextBox.Location.X + toTextBox.Width + 10, toTextBox.Location.Y);
            #endregion

            #region Metric
            SetListView(metricListView, new (string, string, int)[]
                {
                    ("Metric", "MetricName", 4),
                    ("Market", "Market", 4),
                    ("Strategy", "Strategy", 4)
                });
            metricListView.Size = new Size(Screen.GetWorkingArea(form).Size.Width - mainChart.Location.X - mainChart.Width - 5,
                (Screen.GetWorkingArea(form).Height - fromTextBox.Location.Y - fromTextBox.Height - 30) / 2 - 10);
            metricListView.Location = new Point(mainChart.Location.X + mainChart.Size.Width, fromTextBox.Location.Y + fromTextBox.Height + 5);
            #endregion

            #region Period_Total_Result
            SetListView(allListView, new (string, string, int)[]
                {
                    ("No.", "Number", 3),
                    ("Date", "Date", 4),
                    ("Count", "Count", 3),
                    ("Win Rate", "WinRate", 4),
                    ("PRA", "ProfitRateAvg", 4),
                    ("WPRA", "WinProfitRateAvg", 4),
                    ("LPRA", "LoseProfitRateAvg", 4),
                });
            allListView.Size = new Size(metricListView.Width, metricListView.Height / 9 * 2);
            allListView.Location = new Point(metricListView.Location.X, metricListView.Location.Y + metricListView.Height + 10);
            allListView.SelectionChanged += (sender, e) =>
            {
                if (allListView.SelectedIndices.Count == 1)
                {
                    var data = allListView.SelectedObject as DayData;
                    clickResultAction(data);
                }
            };
            #endregion

            #region Code_List_Result
            SetListView(codeListView, new (string, string, int)[]
                {
                    ("Code", "Code", 4),
                    ("No.", "number", 2),
                    ("Count", "Count", 4),
                    ("Win Rate", "WinRate", 4),
                    ("SBG", "ShortestBeforeGap", 4)
                });
            codeListView.Size = new Size(metricListView.Width, metricListView.Height / 9 * 2 - 5);
            codeListView.Location = new Point(metricListView.Location.X, allListView.Location.Y + allListView.Height + 10);
            codeListView.SelectionChanged += (sender, e) =>
            {
                if (codeListView.SelectedIndices.Count != 1)
                    return;

                var itemData = codeListView.SelectedObject as BackItemData;
                ShowChart(itemData, int.MinValue, (DateTime.MaxValue, true));

                if (ready_start_date > ready_end_date)
                    return;

                singleResultListView.ClearObjects();
                var n = 0;
                for (int i = start; i <= end; i++)
                    foreach (var resultData in simulDays.Values[i].resultDatas)
                        if (itemData.Code == resultData.Code && resultData.ExitTime.Date == simulDays.Keys[i] && resultData.EnterTime.Date >= simulDays.Keys[start])
                        {
                            resultData.NumberForSingle = ++n;
                            singleResultListView.AddObject(resultData);
                        }
            };
            #endregion

            #region Controller
            SetButton(beforeButton, "<", (sender, e) =>
            {
            });
            beforeButton.Size = new Size(codeListView.Width / 4 - 10, codeListView.Height / 4 - 5);
            beforeButton.Location = new Point(codeListView.Location.X + 5, codeListView.Location.Y + codeListView.Height + 5);

            SetButton(afterButton, ">", (sender, e) =>
            {
            });
            afterButton.Size = new Size(beforeButton.Width, beforeButton.Height);
            afterButton.Location = new Point(beforeButton.Location.X + beforeButton.Width + 10, beforeButton.Location.Y);

            SetButton(yearBeforeButton, "<Y",
                (sender, e) =>
                {
                    // if (showingItemData != default)
                    //   ShowChart(showingItemData, baseChartViewSticksSize, (showingItemData.ShowingTime / 10000) * 10000 - 1, false);
                });
            yearBeforeButton.Size = new Size(afterButton.Width, afterButton.Height);
            yearBeforeButton.Location = new Point(afterButton.Location.X + afterButton.Width + 10, afterButton.Location.Y);

            SetButton(yearAfterButton, "Y>", (sender, e) =>
            {
                //if (showingItemData != default)
                //  ShowChart(showingItemData, baseChartViewSticksSize, (showingItemData.ShowingTime / 10000 + 1) * 10000);
            });
            yearAfterButton.Size = new Size(yearBeforeButton.Width, yearBeforeButton.Height);
            yearAfterButton.Location = new Point(yearBeforeButton.Location.X + yearBeforeButton.Width + 10, yearBeforeButton.Location.Y);

            SetButton(firstButton, "First",
                (sender, e) =>
                {
                    //if (showingItemData != default)
                    //  ShowChart(showingItemData, baseChartViewSticksSize);
                });
            firstButton.Size = new Size(codeListView.Width / 2 - 10, beforeButton.Height);
            firstButton.Location = new Point(beforeButton.Location.X, beforeButton.Location.Y + beforeButton.Height + 10);

            SetButton(lastButton, "Last",
                (sender, e) =>
                {
                    //if (showingItemData != default)
                    //  ShowChart(showingItemData, baseChartViewSticksSize, 100000000, false);
                });
            lastButton.Size = new Size(firstButton.Width, firstButton.Height);
            lastButton.Location = new Point(firstButton.Location.X + firstButton.Width + 10, firstButton.Location.Y);
            #endregion

            #region Results
            var action = new Action<FastObjectListView>((sender) =>
            {
                if (sender.SelectedIndices.Count != 1)
                    return;

                var data = sender.SelectedObject as BackResultData;
                var itemData = itemDataDic[data.Code];
                //ShowChart2(itemData, data.EnterTime);
            });

            SetListView(singleResultListView, new (string, string, int)[]
                {
                    ("No.", "NumberForSingle", 2),
                    ("Enter", "EnterDate", 4),
                    ("Exit", "ExitDate", 4),
                    ("Profit", "ProfitRate", 2),
                    ("Days", "Days", 2),
                    ("BG", "BeforeGap", 2)
                });
            singleResultListView.Size = new Size(metricListView.Width, codeListView.Height);
            singleResultListView.Location = new Point(codeListView.Location.X, firstButton.Location.Y + firstButton.Height + 10);
            singleResultListView.SelectionChanged += (sender, e) => { action(singleResultListView); };

            SetListView(clickResultListView, new (string, string, int)[]
                {
                    ("No.", "NumberForClick", 2),
                    ("Code", "Code", 4),
                    ("Enter", "EnterDate", 4),
                    ("Exit", "ExitDate", 4),
                    ("Profit", "ProfitRate", 2),
                    ("Days", "Days", 2)
                });
            clickResultListView.Size = new Size(metricListView.Width, codeListView.Height);
            clickResultListView.Location = new Point(codeListView.Location.X, singleResultListView.Location.Y + singleResultListView.Height + 10);
            clickResultListView.SelectionChanged += (sender, e) => {
                action(clickResultListView);

                singleResultListView.ClearObjects();
                var n = 0;
                for (int i = start; i <= end; i++)
                    foreach (var resultData in simulDays.Values[i].resultDatas)
                        if (showingItemData.Code == resultData.Code && resultData.ExitTime.Date == simulDays.Values[i].Date && resultData.EnterTime.Date >= simulDays.Values[start].Date)
                        {
                            resultData.NumberForSingle = ++n;
                            singleResultListView.AddObject(resultData);
                        }
            };
            #endregion
        }

        void LoadCodeListAndMetric()
        {
            var conn = new SQLiteConnection("Data Source =" + sticksDBpath + sticksDBbaseName + BaseChartTimeSet.OneMinute.Text + ".db");
            conn.Open();

            var reader = new SQLiteCommand("Select name From sqlite_master where type='table'", conn).ExecuteReader();

            var number = 0;
            while (reader.Read())
            {
                var itemData = new BackItemData(reader["name"].ToString(), number);
                codeListView.AddObject(itemData);
                itemDataDic.Add(itemData.Code, itemData);
                number++;
            }

            conn.Close();

            metricDic.Add(MetricCR, new MetricData() { MetricName = MetricCR });
            metricListView.AddObject(metricDic[MetricCR]);
            metricDic.Add(MetricCAGR, new MetricData() { MetricName = MetricCAGR });
            metricListView.AddObject(metricDic[MetricCAGR]);
            metricListView.AddObject(new MetricData());

            metricDic.Add(MetricWinRate, new MetricData() { MetricName = MetricWinRate });
            metricListView.AddObject(metricDic[MetricWinRate]);
            metricDic.Add(MetricWinRateYearStandartDeviation, new MetricData() { MetricName = MetricWinRateYearStandartDeviation });
            metricListView.AddObject(metricDic[MetricWinRateYearStandartDeviation]);
            metricDic.Add(MetricAllCount, new MetricData() { MetricName = MetricAllCount });
            metricListView.AddObject(metricDic[MetricAllCount]);
            metricDic.Add(MetricAvgProfitRate, new MetricData() { MetricName = MetricAvgProfitRate });
            metricListView.AddObject(metricDic[MetricAvgProfitRate]);
            metricDic.Add(MetricWinAvgProfitRate, new MetricData() { MetricName = MetricWinAvgProfitRate });
            metricListView.AddObject(metricDic[MetricWinAvgProfitRate]);
            metricDic.Add(MetricLoseAvgProfitRate, new MetricData() { MetricName = MetricLoseAvgProfitRate });
            metricListView.AddObject(metricDic[MetricLoseAvgProfitRate]);
            metricListView.AddObject(new MetricData());

            metricDic.Add(MetricMDD, new MetricData() { MetricName = MetricMDD });
            metricListView.AddObject(metricDic[MetricMDD]);
            metricDic.Add(MetricMDDStart, new MetricData() { MetricName = MetricStart });
            metricListView.AddObject(metricDic[MetricMDDStart]);
            metricDic.Add(MetricMDDEnd, new MetricData() { MetricName = MetricEnd });
            metricListView.AddObject(metricDic[MetricMDDEnd]);
            metricDic.Add(MetricLDDD, new MetricData() { MetricName = MetricLDDD });
            metricListView.AddObject(metricDic[MetricLDDD]);
            metricDic.Add(MetricLDDDStart, new MetricData() { MetricName = MetricStart });
            metricListView.AddObject(metricDic[MetricLDDDStart]);
            metricDic.Add(MetricLDDDEnd, new MetricData() { MetricName = MetricEnd });
            metricListView.AddObject(metricDic[MetricLDDDEnd]);
            metricListView.AddObject(new MetricData());

            metricDic.Add(MetricMaxHas, new MetricData() { MetricName = MetricMaxHas });
            metricListView.AddObject(metricDic[MetricMaxHas]);
            metricDic.Add(MetricMaxHasDate, new MetricData() { MetricName = MetricMaxHasDate });
            metricListView.AddObject(metricDic[MetricMaxHasDate]);
            metricDic.Add(MetricLongestHasDays, new MetricData() { MetricName = MetricLongestHasDays });
            metricListView.AddObject(metricDic[MetricLongestHasDays]);
            metricDic.Add(MetricLongestHasDaysCode, new MetricData() { MetricName = MetricLongestHasDaysCode });
            metricListView.AddObject(metricDic[MetricLongestHasDaysCode]);
            metricDic.Add(MetricAverageHasDays, new MetricData() { MetricName = MetricAverageHasDays });
            metricListView.AddObject(metricDic[MetricAverageHasDays]);
        }

        void RunTotal(int start, int end)
        {
            ClearChart(totalChart);

            foreach (var itemData in itemDataDic.Values)
            {
                itemData.Count = 0;
                itemData.Win = 0;
            }

            Run(start, end);

            if (!marketDays.ContainsKey(DateTime.Parse(start.ToString()).Date))
            {
                var first_date = int.Parse(marketDays.Keys[0].ToString("yyyyMMss"));
                if (start < first_date)
                    start = first_date;
                else if (start > int.Parse(marketDays.Keys[marketDays.Count - 1].ToString("yyyyMMss")))
                {
                    MessageBox.Show("start date input wrong");
                    return;
                }
                else
                    while (!marketDays.ContainsKey(DateTime.Parse(start.ToString()).Date))
                        start++;
            }
            if (!marketDays.ContainsKey(DateTime.Parse(end.ToString()).Date))
            {
                var last_date = int.Parse(marketDays.Keys[marketDays.Count - 1].ToString("yyyyMMss"));
                if (end > last_date)
                    end = last_date;
                else if (end < int.Parse(marketDays.Keys[0].ToString("yyyyMMss")))
                {
                    MessageBox.Show("end date input wrong");
                    return;
                }
                else
                    while (!marketDays.ContainsKey(DateTime.Parse(end.ToString()).Date))
                        end--;
            }

            CalculateMetric(start, end);

            foreach (var itemData in itemDataDic.Values)
                itemData.WinRate = Math.Round((double)itemData.Win / itemData.Count * 100, 2) + "%";

            totalButton.PerformClick();
        }
        void Run(int start, int end)
        {
            if (start >= ready_start_date && end <= ready_end_date)
                return;

            marketDays.Clear();
            simulDays.Clear();
            openDaysPerYear.Clear();

            foreach (var itemData in itemDataDic.Values)
            {
                itemData.ShortestBeforeGap = int.MaxValue;

                var conn = new SQLiteConnection("Data Source =" + sticksDBpath + sticksDBbaseName + BaseChartTimeSet.OneMinute.Text + ".db");
                conn.Open();
                var reader = new SQLiteCommand("Select * From '" + itemData.Code + "' where date>='" + start + "' and date<='" + end + "' order by date, time", conn).ExecuteReader();
                var list = new List<TradeStick>();
                while (reader.Read())
                {
                    var stick = GetStickFromSQL(reader);

                    if (!marketDays.ContainsKey(stick.Time.Date))
                    {
                        marketDays.Add(stick.Time.Date, new DayData() { Date = stick.Time.Date });
                        simulDays.Add(stick.Time.Date, new DayData() { Date = stick.Time.Date });

                        var year = stick.Time.Year;
                        if (!openDaysPerYear.ContainsKey(year))
                            openDaysPerYear.Add(year, 0);
                        openDaysPerYear[year]++;
                    }

                    list.Add(stick);

                    if (list.Count > 1 && list[list.Count - 1].Time.Date != list[list.Count - 2].Time.Date)
                        marketDays[list[list.Count - 2].Time.Date].day_sticks_for_market.Add(Calculate_Day_Stick(list, list.Count - 2));

                    if (list.Count == 1 || (!itemData.BaseReady && BaseCondition(itemData, list, list.Count - 2, st)))
                        itemData.BaseReady = true;

                    if (itemData.BaseReady && !itemData.Enter && EnterCondition(itemData, list, list.Count - 2, st))
                    {
                        itemData.Enter = true;
                        itemData.EnterTime = stick.Time;
                        itemData.EnterPrice = stick.Price[2];
                        itemData.EnterIndex = list.Count - 1;
                    }
                    else if (itemData.Enter && ExitCondition(itemData, list, list.Count - 2, st))
                    {
                        itemData.Enter = false;
                        itemData.BaseReady = false;

                        if (!itemData.ExitException)
                        {
                            var resultData = new BackResultData()
                            {
                                Code = itemData.Code,
                                EnterTime = itemData.EnterTime,
                                ExitTime = stick.Time,
                                ProfitRate = (stick.Price[2] / itemData.EnterPrice - 1) * 100,
                                Days = marketDays.IndexOfKey(stick.Time.Date) - marketDays.IndexOfKey(itemData.EnterTime.Date),
                                BeforeGap = itemData.BeforeExitIndex == default ? int.MaxValue : (itemData.EnterIndex - itemData.BeforeExitIndex)
                            };


                            for (int i = simulDays.IndexOfKey(resultData.ExitTime.Date); i >= 0; i--)
                            {
                                if (i < simulDays.IndexOfKey(resultData.EnterTime.Date))
                                    break;

                                simulDays.Values[i].resultDatas.Add(resultData);
                            }

                            if (resultData.BeforeGap < itemData.ShortestBeforeGap)
                                itemData.ShortestBeforeGap = resultData.BeforeGap;
                        }

                        itemData.BeforeExitIndex = list.Count - 1;
                    }
                }
                conn.Close();
            }

            if (openDaysPerYear.Values.Count > 2)
            {
                openDaysPerYear.Values[0] = openDaysPerYear.Values[1];
                openDaysPerYear.Values[openDaysPerYear.Values.Count - 1] = openDaysPerYear.Values[openDaysPerYear.Values.Count - 2];
            }

            ready_start_date = start;
            ready_end_date = end;
        }

        TradeStick Calculate_Day_Stick(List<TradeStick> list, int last_index)
        {
            var day_stick = new TradeStick() { Time = list[last_index].Time.Date };
            day_stick.Price[0] = list[last_index].Price[0];
            day_stick.Price[1] = list[last_index].Price[1];
            day_stick.Price[3] = list[last_index].Price[3];

            for (var index = last_index; index >= 0; index--)
            {
                if (list[index].Time.Date != day_stick.Time.Date)
                    break;

                if (list[index].Price[0] > day_stick.Price[0])
                    day_stick.Price[0] = list[index].Price[0];

                if (list[index].Price[1] < day_stick.Price[1])
                    day_stick.Price[1] = list[index].Price[1];

                day_stick.Price[2] = list[index].Price[2];

                day_stick.Ms += list[index].Ms;
                day_stick.Md += list[index].Md;
            }

            return day_stick;
        }
        void CalculateMetric(int start, int end)
        {
            start = marketDays.IndexOfKey(DateTime.Parse(start.ToString()).Date);
            end = marketDays.IndexOfKey(DateTime.Parse(end.ToString()).Date);

            var maxBuyItemsAtOnce = int.MinValue;
            DateTime maxBuyItemsDate;
            var longestHasDays = double.MinValue;
            var longestHasCode = "";
            var beforeBuyItemsAtOnce = 0;
            var goingUp = false;
            allListView.ClearObjects();

            var fullCount = 0;
            var fullWin = 0;
            decimal fullProfitRateSum = 0;
            decimal fullProfitWinRateSum = 0;

            for (int i = start; i <= end; i++)
            {
                var buyItemsAtOnce = 0;
                simulDays.Values[i].Count = 0;
                simulDays.Values[i].Win = 0;
                simulDays.Values[i].WinProfitRateSum = 0;
                simulDays.Values[i].ProfitRateSum = 0;
                foreach (var resultData in simulDays.Values[i].resultDatas)
                {
                    simulDays.Values[i].Count++;
                    simulDays.Values[i].ProfitRateSum += resultData.ProfitRate;
                    if (resultData.ProfitRate > commisionRate)
                    {
                        simulDays.Values[i].Win++;
                        simulDays.Values[i].WinProfitRateSum += resultData.ProfitRate;
                    }

                    buyItemsAtOnce++;

                    if (resultData.ExitTime.Subtract(resultData.EnterTime).TotalDays > longestHasDays)
                    {
                        longestHasDays = Math.Round(resultData.ExitTime.Subtract(resultData.EnterTime).TotalDays, 2);
                        longestHasCode = resultData.Code;
                    }

                    if (resultData.ExitTime.Date == simulDays.Keys[i] && resultData.EnterTime.Date >= simulDays.Keys[start])
                    {
                        var itemData = itemDataDic[resultData.Code];
                        fullCount++;
                        itemData.Count++;
                        fullProfitRateSum += resultData.ProfitRate;
                        if (resultData.ProfitRate > commisionRate)
                        {
                            fullWin++;
                            itemData.Win++;
                            fullProfitWinRateSum += resultData.ProfitRate;
                        }
                    }
                }

                if (buyItemsAtOnce > maxBuyItemsAtOnce)
                {
                    maxBuyItemsAtOnce = buyItemsAtOnce;
                    maxBuyItemsDate = simulDays.Keys[i];
                }

                simulDays.Values[i - 1].WinRate = Math.Round((decimal)simulDays.Values[i - 1].Win / simulDays.Values[i - 1].Count * 100, 2);
                simulDays.Values[i - 1].ProfitRateAvg = Math.Round((simulDays.Values[i - 1].ProfitRateSum / simulDays.Values[i - 1].Count - 1) * 100, 2);
                simulDays.Values[i - 1].WinProfitRateAvg = (simulDays.Values[i - 1].Win == 0 ? 0m : Math.Round((simulDays.Values[i - 1].WinProfitRateSum / simulDays.Values[i - 1].Win - 1) * 100, 2));
                simulDays.Values[i - 1].LoseProfitRateAvg = (simulDays.Values[i - 1].Count == simulDays.Values[i - 1].Win ? 0m : Math.Round(((simulDays.Values[i - 1].ProfitRateSum - simulDays.Values[i - 1].WinProfitRateSum) / (simulDays.Values[i - 1].Count - simulDays.Values[i - 1].Win) - 1) * 100, 2));

                if (buyItemsAtOnce > beforeBuyItemsAtOnce)
                    goingUp = true;
                else if (buyItemsAtOnce < beforeBuyItemsAtOnce)
                {
                    if (goingUp && beforeBuyItemsAtOnce > 10)
                        allListView.AddObject(simulDays.Values[i - 1]);
                    goingUp = false;
                }

                beforeBuyItemsAtOnce = buyItemsAtOnce;

                totalChart.Series[1].Points.AddXY(simulDays.Keys[i].ToString("yyyyMMss"), simulDays.Values[i].ProfitRateAvg);
                totalChart.Series[2].Points.AddXY(simulDays.Keys[i].ToString("yyyyMMss"), buyItemsAtOnce);
            }

            allListView.Sort(allListView.GetColumn("PRA"), SortOrder.Ascending);
            var n = 0;
            foreach (DayData ob in allListView.Objects)
                ob.Number = ++n;
            allListView.Sort(allListView.GetColumn("No."), SortOrder.Descending);

            var WinRate = Math.Round((double)fullWin / fullCount * 100, 2) + "%";
            metricDic[MetricWinRate].Strategy = WinRate;
            metricDic[MetricAllCount].Strategy = fullCount.ToString();
            metricDic[MetricAvgProfitRate].Strategy = fullCount == 0 ? "0%" : (Math.Round((fullProfitRateSum / fullCount - 1) * 100, 2) + "%");
            metricDic[MetricWinAvgProfitRate].Strategy = fullWin == 0 ? "0%" : (Math.Round((fullProfitWinRateSum / fullWin - 1) * 100, 2) + "%");
            metricDic[MetricLoseAvgProfitRate].Strategy = fullCount == fullWin ? "0%" : (Math.Round(((fullProfitRateSum - fullProfitWinRateSum) / (fullCount - fullWin) - 1) * 100, 2) + "%");
            metricDic[MetricMaxHas].Strategy = maxBuyItemsAtOnce.ToString();
            metricDic[MetricLongestHasDays].Strategy = longestHasDays + "days";
            metricDic[MetricLongestHasDaysCode].Strategy = longestHasCode;
        }
        (int has, double kelly) CalculateHasAndKelly(int index, int days)
        {
            if (index < 0)
                return (1, 0.5);

            double beforeKelly = 1.01D;
            double beforeGeoMean = double.MinValue;
            double beforeKelly2 = 1.02D;
            double beforeGeoMean2 = double.MinValue;
            var kelly = 1D;
            var geoMean = 1D;
            var minHas = int.MaxValue;
            while (true)
            {
                var count = 0;
                var win = 0;
                for (int i = index; i > index - days; i--)
                {
                    if (i < 0)
                        break;

                    foreach (var data in simulDays.Values[i].resultDatas)
                    {
                        if (simulDays.Values[i].resultDatas.Count < minHas)
                            minHas = simulDays.Values[i].resultDatas.Count;

                        if (data.ExitTime.Date == simulDays.Keys[i])
                        {
                            count++;
                            geoMean *= 1 + ((double)data.ProfitRate - 1) * kelly;
                            if (data.ProfitRate > 1)
                                win++;
                        }
                    }
                }

                if (count <= 1 || count == win)
                    return (minHas == int.MaxValue ? 1 : minHas, 0.5);

                geoMean = Math.Pow((double)geoMean, 1D / count);

                if (beforeKelly > 2 || beforeKelly < 0.1)
                    return (minHas, beforeKelly > 2 ? 1 : 0.1);

                if (geoMean > beforeGeoMean)
                {
                    beforeKelly2 = beforeKelly;
                    beforeGeoMean2 = beforeGeoMean;
                    beforeKelly = kelly;
                    beforeGeoMean = geoMean;
                    kelly = beforeKelly - (beforeKelly2 - beforeKelly);
                    geoMean = 1D;
                }
                else
                {
                    if (beforeGeoMean2 == double.MinValue)
                    {
                        beforeKelly2 = kelly;
                        beforeGeoMean2 = geoMean;
                        kelly = beforeKelly + (beforeKelly - beforeKelly2);
                        geoMean = 1D;
                    }
                    else
                        return (minHas == int.MaxValue ? 1 : minHas, beforeKelly / 2);
                }
            }
        }

        void FindSimulAndShow(bool toPast)
        {
            DateTime from = toPast ? DateTime.MaxValue : DateTime.MinValue;

            var centerViewIndex = (int)mainChart.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 2 - (baseChartViewSticksSize + 1) / 2;
            if (mainChart.Series[0].Points.Count - 1 >= centerViewIndex && centerViewIndex >= 0)
                from = DateTime.Parse(mainChart.Series[0].Points[centerViewIndex].AxisLabel);

            #region save
            //if (mainChart.Series[0].Points.Count != 0)
            //{
            //    var viewStart = (int)mainChart.ChartAreas[0].AxisX.ScaleView.ViewMinimum + 1;
            //    var viewEnd = (int)mainChart.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 1;
            //    if (mainChart.ChartAreas[0].AxisX.StripLines.Count == 0 || mainChart.ChartAreas[0].AxisX.StripLines.All(strip => (strip.IntervalOffset <= viewStart && strip.IntervalOffset >= viewEnd)))
            //    {
            //        if (toPast)
            //        {
            //            if (viewStart >= 0)
            //                from = DateTime.Parse(mainChart.Series[0].Points[viewStart].AxisLabel);
            //            else
            //            {
            //                var chartValue = mainChart.Tag as ChartValues;
            //                from = DateTime.Parse(mainChart.Series[0].Points[0].AxisLabel);
            //                from = (chartValue != BaseChartTimeSet.OneMonth) ? from.AddSeconds(-chartValue.seconds) : from.AddMonths(-1);
            //            }
            //        }
            //        else
            //        {
            //            if (viewEnd <= mainChart.Series[0].Points.Count - 1)
            //                from = DateTime.Parse(mainChart.Series[0].Points[viewEnd].AxisLabel);
            //            else
            //            {
            //                var chartValue = mainChart.Tag as ChartValues;
            //                from = DateTime.Parse(mainChart.Series[0].Points.Last().AxisLabel);
            //                from = (chartValue != BaseChartTimeSet.OneMonth) ? from.AddSeconds(chartValue.seconds) : from.AddMonths(1);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        var stripsInView = mainChart.ChartAreas[0].AxisX.StripLines.Where(strip => (strip.IntervalOffset > viewStart && strip.IntervalOffset < viewEnd)).ToList();
            //        from = toPast ? DateTime.Parse(mainChart.Series[0].Points[(int)stripsInView[0].IntervalOffset].AxisLabel) : DateTime.Parse(mainChart.Series[0].Points[(int)stripsInView.Last().IntervalOffset].AxisLabel);
            //    }
            //}
            #endregion

            BackItemData itemData = default;
            int foundIndex = int.MinValue;

            if (codeListView.SelectedIndices.Count == 1)
            {
                itemData = showingItemData;
                foundIndex = FindSimulAndReturnIndex(itemData, toPast, mainChart.Tag as ChartValues, from);
            }
            else
            {
                var m = toPast ? -1 : 1;

                foreach (var itemData2 in itemDataDic.Values)
                {
                    var foundIndex2 = FindSimulAndReturnIndex(itemData2, toPast, mainChart.Tag as ChartValues, from);
                    if (itemData == default || foundIndex == int.MaxValue)
                    {
                        itemData = itemData2;
                        foundIndex = foundIndex2;
                    }
                    else if (foundIndex2 != int.MaxValue)
                    {
                        var numberNow = showingItemData != default ? showingItemData.number : (toPast ? itemDataDic.Count : -1);
                        var distant = m * itemData.list[foundIndex].Time.Subtract(from).TotalSeconds;
                        var distant2 = m * itemData2.list[foundIndex2].Time.Subtract(from).TotalSeconds;
                        if (itemData2.list[foundIndex2].Time != from 
                            ? (distant2 < distant || (toPast && distant2 == distant)) 
                            : (m * (itemData2.number - numberNow) > 0 && (toPast || itemData.list[foundIndex].Time != from)))
                        {
                            itemData = itemData2;
                            foundIndex = foundIndex2;
                        }
                    }
                }
            }

            if (foundIndex != int.MaxValue)
                ShowChart(itemData, foundIndex, default, mainChart.Tag as ChartValues);
            else
            {
                MessageBox.Show("none", "Alert", MessageBoxButtons.OK);
                LoadAndCheckSticks(showingItemData, true, true, mainChart.Tag as ChartValues, 
                    DateTime.Parse(mainChart.Series[0].Points.Last().AxisLabel), mainChart.Series[0].Points.Count);
            }
        }
        int FindSimulAndReturnIndex(BackItemData itemData, bool toPast, ChartValues chartValues, DateTime from)
        {
            var foundIndex = int.MinValue;

            foundIndex = LoadAndCheckSticks(itemData, true, toPast, chartValues, from, 1000);
            if (foundIndex == int.MaxValue)
                ShowError(form);

            while (foundIndex == int.MinValue)
                foundIndex = LoadAndCheckSticks(itemData, false, toPast, chartValues, default, 1000);

            if (foundIndex != int.MaxValue)
                LoadAndCheckSticks(itemData, false, !toPast, chartValues, default, 100);

            return foundIndex;
        }

        public override void SetChartNowOrLoad(ChartValues chartValues)
        {
            if (showingItemData == default || mainChart.Tag as ChartValues == chartValues)
                return;

            DateTime from = mainChart.Series[0].Points.Count != 0 ? DateTime.Parse(mainChart.Series[0].Points[(int)mainChart.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 2].AxisLabel) : DateTime.MaxValue;

            ShowChart(showingItemData, int.MinValue, (from, true), chartValues);
        }
        void ShowChart(BackItemData itemData, int center = int.MinValue, (DateTime from, bool toPast) load = default, ChartValues chartValues = default)
        {
            if (!(center == int.MinValue ^ load == default))
                ShowError(form);

            if (chartValues == default)
                chartValues = mainChart.Tag as ChartValues;

            showingItemData = itemData;

            form.Text = itemData.Code;

            ClearMainChartAndSet(chartValues);

            if (load != default)
                if (LoadAndCheckSticks(itemData, true, load.toPast, chartValues, load.from) == int.MaxValue)
                {
                    ShowError(form);
                    return;
                }

            foreach (var stick in itemData.list)
                AddFullChartPoint(mainChart, stick);

            foreach (var enterIndex in itemData.enterIndexList)
                foreach (var ca in mainChart.ChartAreas)
                    ca.AxisX.StripLines.Add(new StripLine()
                    {
                        BackColor = ColorSet.Losing,
                        IntervalOffset = enterIndex + 1,
                        StripWidth = 1
                    });

            var zoomStart = 0D;
            var zoomEnd = zoomStart + baseChartViewSticksSize;
            if (load != default && load.toPast)
            {
                zoomEnd = mainChart.Series[0].Points.Count;
                zoomStart = zoomEnd - baseChartViewSticksSize;
            }
            else if (center != int.MinValue)
            {
                zoomStart = center - (baseChartViewSticksSize + 1) / 2;
                zoomEnd = zoomStart + baseChartViewSticksSize;
            }

            ZoomX(mainChart, (int)zoomStart, (int)zoomEnd);
        }
        public override void AdjustChart(Chart chart)
        {
            AdjustChartBasic(chart, showingItemData.hoDiff);
        }
        protected override void LoadMore(Chart chart, ScrollType scrollType, bool loadNew)
        {
            if (showingItemData == default)
                return;

            var zoomStart = chart.ChartAreas[0].AxisX.ScaleView.ViewMinimum + 1;
            var zoomEnd = chart.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 1;

            var chartValue = chart.Tag as ChartValues;
            var toPast = scrollType == ScrollType.SmallDecrement;
            var countLast = showingItemData.list.Count;
            LoadAndCheckSticks(showingItemData, false, toPast, chartValue, default);
            var addedCount = showingItemData.list.Count - countLast;

            if (toPast)
            {
                foreach (var ca in chart.ChartAreas)
                    foreach (var strip in ca.AxisX.StripLines)
                        strip.IntervalOffset += addedCount;

                for (int i = addedCount - 1; i >= 0; i--)
                    InsertFullChartPoint(chart, showingItemData.list[i]);

                zoomStart += addedCount;
                zoomEnd += addedCount;

                if (!double.IsNaN(chart.ChartAreas[0].CursorX.Position))
                    chart.ChartAreas[0].CursorX.Position += addedCount;
            }
            else
            {
                for (int i = countLast; i < countLast + addedCount; i++)
                    AddFullChartPoint(chart, showingItemData.list[i]);
            }

            foreach (var enterIndex in showingItemData.enterIndexList)
                if (toPast ? enterIndex < addedCount : enterIndex >= countLast)
                    foreach (var ca in mainChart.ChartAreas)
                        ca.AxisX.StripLines.Add(new StripLine()
                        {
                            BackColor = ColorSet.Losing,
                            IntervalOffset = enterIndex + 1,
                            StripWidth = 1
                        });

            ZoomX(mainChart, (int)zoomStart, (int)zoomEnd);
        }
        int LoadAndCheckSticks(BackItemData itemData, bool newLoad, bool toPast, ChartValues chartValues, DateTime from = default, int size = default)
        {
            var conn = new SQLiteConnection("Data Source =" + sticksDBpath + sticksDBbaseName + chartValues.Text + ".db");
            conn.Open();

            var reader0 = new SQLiteCommand("Select * From sqlite_master where type='table' and name='" + itemData.Code + "'", conn).ExecuteReader();
            if (!reader0.HasRows)
            {
                ShowError(form);
                return default;
            }

            var list = new List<TradeStick>();

            var multiplier = toPast ? -1 : 1;
            if (!newLoad)
            {
                var lastTime = toPast ? itemData.list[0].Time : itemData.list.Last().Time;
                from = chartValues != BaseChartTimeSet.OneMonth ? lastTime.AddSeconds(multiplier * chartValues.seconds) : lastTime.AddMonths(multiplier);
            }

            if (size == default)
                size = baseLoadSticksSize;

            var date = from.ToString("yyyyMMdd");
            var time = from.ToString("HHmmss");
            var reader = new SQLiteCommand("Select *, rowid From " +
                "(Select *, rowid From '" + itemData.Code + "' " +
                "where " + (toPast ? ("date<'" + date + "' or date='" + date + "' and time<='" + time + "' order by rowid desc") :
                    ("date>'" + date + "' or date='" + date + "' and time>='" + time + "' order by date")) + " limit " + size + ") order by rowid", conn).ExecuteReader();
            var smallestDiff = decimal.MaxValue;
            while (reader.Read())
            {
                var stick = GetStickFromSQL(reader);
                
                if (stick.Price[0] - stick.Price[2] != 0 && stick.Price[0] - stick.Price[2] < smallestDiff)
                    smallestDiff = stick.Price[0] - stick.Price[2];
                if (stick.Price[0] - stick.Price[3] != 0 && stick.Price[0] - stick.Price[3] < smallestDiff)
                    smallestDiff = stick.Price[0] - stick.Price[3];
                if (stick.Price[2] - stick.Price[1] != 0 && stick.Price[2] - stick.Price[1] < smallestDiff)
                    smallestDiff = stick.Price[2] - stick.Price[1];
                if (stick.Price[3] - stick.Price[1] != 0 && stick.Price[3] - stick.Price[1] < smallestDiff)
                    smallestDiff = stick.Price[3] - stick.Price[1];

                list.Add(stick);
            }

            if (list.Count == 0)
                return int.MaxValue;

            var addedCount = 0;
            date = list[0].Time.ToString("yyyyMMdd");
            time = list[0].Time.ToString("HHmmss");
            reader = new SQLiteCommand("Select *, rowid From '" + itemData.Code + "' " +
                "where date<'" + date + "' or date='" + date + "' and time<'" + time + "' order by rowid desc limit " + (suddenNeedDays - 1), conn).ExecuteReader();
            while (reader.Read())
            {
                addedCount++;
                var stick = GetStickFromSQL(reader);

                if (stick.Price[0] - stick.Price[2] != 0 && stick.Price[0] - stick.Price[2] < smallestDiff)
                    smallestDiff = stick.Price[0] - stick.Price[2];
                if (stick.Price[0] - stick.Price[3] != 0 && stick.Price[0] - stick.Price[3] < smallestDiff)
                    smallestDiff = stick.Price[0] - stick.Price[3];
                if (stick.Price[2] - stick.Price[1] != 0 && stick.Price[2] - stick.Price[1] < smallestDiff)
                    smallestDiff = stick.Price[2] - stick.Price[1];
                if (stick.Price[3] - stick.Price[1] != 0 && stick.Price[3] - stick.Price[1] < smallestDiff)
                    smallestDiff = stick.Price[3] - stick.Price[1];

                list.Insert(0, stick);
            }
            conn.Close();

            int firstSimul = int.MinValue;
            var enterIndexList = new List<int>();
            for (int i = 0; i < list.Count; i++)
            {
                if (CheckSuddenBurst(isJoo, list, list[i], chartValues, i - 1))
                {
                    if (!newLoad && (toPast || enterIndexList.Count == 0))
                        firstSimul = i - addedCount;
                    enterIndexList.Add(i - addedCount);
                }
            }

            list.RemoveRange(0, addedCount);

            if (!newLoad && !toPast)
            {
                enterIndexList.ForEach(enterIndex => { enterIndex += itemData.list.Count; });
                itemData.list.AddRange(list);
                itemData.enterIndexList.AddRange(enterIndexList);
            }
            else
            {
                if (!newLoad)
                {
                    itemData.enterIndexList.ForEach(enterIndex => { enterIndex += list.Count; });
                    list.AddRange(itemData.list);
                    enterIndexList.AddRange(itemData.enterIndexList);
                }

                itemData.list.Clear();
                itemData.enterIndexList.Clear();
                itemData.list = list;
                itemData.enterIndexList = enterIndexList;
            }

            if (smallestDiff < itemData.hoDiff)
                itemData.hoDiff = smallestDiff;

            return firstSimul;
        }
        TradeStick GetStickFromSQL(SQLiteDataReader reader)
        {
            return new TradeStick()
            {
                Time = DateTime.Parse(reader["date"].ToString().Insert(4, "-").Insert(7, "-") + " " + reader["time"].ToString().Insert(2, ":").Insert(5, ":")),
                Price = new decimal[]
                        {
                                decimal.Parse(reader["high"].ToString()),
                                decimal.Parse(reader["low"].ToString()),
                                decimal.Parse(reader["open"].ToString()),
                                decimal.Parse(reader["close"].ToString())
                        },
                Ms = decimal.Parse(reader["takerBuyBaseVolume"].ToString()),
                Md = decimal.Parse(reader["baseVolume"].ToString()) - decimal.Parse(reader["takerBuyBaseVolume"].ToString())
            };
        }

        bool BaseCondition(BackItemData itemData, List<TradeStick> list, int index, int ST)
        {
            if (index < 0)
                return false;

            switch (ST)
            {
                default:
                    return true;
            }
        }
        bool EnterCondition(BackItemData itemData, List<TradeStick> list, int index, int ST)
        {
            switch (ST)
            {
                case 0:
                    if (index < 0)
                        return false;

                    if (list[index].RSI < 10 &&
                        EnterConditionLast(list, index, ST))
                        return true;
                    else
                        return false;

                case 1:
                    if (index - 3 < 0)
                        return false;

                    if (list[index].RSI < 20 &&
                        list[index].RSI - list[index - 2].RSI > 8 && list[index - 1].RSI - list[index - 3].RSI > -3 &&
                        Math.Abs(2 * list[index - 1].RSI - list[index - 2].RSI - list[index].RSI) / Math.Sqrt(4 + Math.Pow(list[index].RSI - list[index - 2].RSI, 2)) < 2 &&
                        Math.Abs(2 * list[index - 2].RSI - list[index - 3].RSI - list[index - 1].RSI) / Math.Sqrt(4 + Math.Pow(list[index - 1].RSI - list[index - 3].RSI, 2)) < 2 &&
                        EnterConditionLast(list, index, ST))
                        return true;
                    else
                        return false;

                case 2:
                    if (index - 3 < 0)
                        return false;

                    if (list[index].RSI < 20 &&
                        list[index].RSI - list[index - 2].RSI > 8 && list[index - 1].RSI - list[index - 3].RSI > -3 &&
                        Math.Abs(2 * list[index - 1].RSI - list[index - 2].RSI - list[index].RSI) / Math.Sqrt(4 + Math.Pow(list[index].RSI - list[index - 2].RSI, 2)) < 2 &&
                        Math.Abs(2 * list[index - 2].RSI - list[index - 3].RSI - list[index - 1].RSI) / Math.Sqrt(4 + Math.Pow(list[index - 1].RSI - list[index - 3].RSI, 2)) < 2 &&
                        EnterConditionLast(list, index, ST))
                        return true;
                    else
                        return false;

                default:
                    return false;
            }
        }
        bool EnterConditionLast(List<TradeStick> list, int index, int ST)
        {
            switch (ST)
            {
                case 0:
                case 1:
                case 2:
                    for (int i = index; i > index - searchDay; i--)
                    {
                        if (i < 0)
                            break;

                        if (list[i].Ms + list[i].Md < 50000)
                            return false;
                    }

                    return true;

                default:
                    return false;
            }
        }
        bool ExitCondition(BackItemData itemData, List<TradeStick> list, int index, int ST)
        {
            if (ExitConditionException(list, index, ST))
            {
                itemData.ExitException = true;
                return true;
            }

            switch (ST)
            {
                case 0:
                    if (list[index].RSI > 30 && list[index].RSI - list[index - 1].RSI < list[index - 1].RSI - list[index - 2].RSI)
                    {
                        itemData.ExitException = false;
                        return true;
                    }
                    else
                        return false;

                case 1:
                    if (list[index].RSI - list[index - 2].RSI < -5 ||
                        Math.Abs(2 * list[index - 1].RSI - list[index - 2].RSI - list[index].RSI) / Math.Sqrt(4 + Math.Pow(list[index].RSI - list[index - 2].RSI, 2)) > 3)
                    {
                        itemData.ExitException = false;
                        return true;
                    }
                    else
                        return false;

                case 2:
                    if (list[index].RSI > 30 && list[index].RSI - list[index - 1].RSI < list[index - 1].RSI - list[index - 2].RSI)
                    {
                        itemData.ExitException = false;
                        return true;
                    }
                    else
                        return false;

                default:
                    return false;
            }
        }
        bool ExitConditionException(List<TradeStick> list, int index, int ST)
        {
            switch (ST)
            {
                case 0:
                case 1:
                case 2:
                    if (list[index].Price[0] == list[index].Price[1])
                        return true;
                    else
                        return false;

                default:
                    return false;
            }
        }

        void CalRSI(List<DayStick> list, int index)
        {
            if (index < searchDay - 1)
            {
                list[index].RSI = double.NaN;
                list[index].RSI2 = double.NaN;
            }
            else
            {
                var plusSum = 0m;
                var totalSum = 0m;
                var plusSum2 = 0m;
                var totalSum2 = 0m;
                for (int i = index; i > index - RSINumber; i--)
                {
                    if (list[i].Price[3] > list[i - 1].Price[3])
                    {
                        plusSum += list[i].Price[3] - list[i - 1].Price[3];
                        totalSum += list[i].Price[3] - list[i - 1].Price[3];
                    }
                    else
                        totalSum += list[i - 1].Price[3] - list[i].Price[3];

                    if (list[i].Price[3] > list[i].Price[2])
                    {
                        plusSum2 += list[i].Price[3] - list[i].Price[2];
                        totalSum2 += list[i].Price[3] - list[i].Price[2];
                    }
                    else
                        totalSum2 += list[i].Price[2] - list[i].Price[3];

                    if (list[i].Price[0] == list[i].Price[1])
                    {
                        totalSum = 0;
                        break;
                    }
                }

                if (list[index - RSINumber].Price[0] == list[index - RSINumber].Price[1])
                    totalSum = 0;

                list[index].RSI = totalSum > 0 ? (double)(plusSum / totalSum * 100) : double.NaN;
                list[index].RSI2 = totalSum2 > 0 ? (double)(plusSum2 / totalSum2 * 100) : double.NaN;
            }
        }
    }
}
