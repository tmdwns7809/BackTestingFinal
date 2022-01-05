using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System.Drawing;
using System.Data.SQLite;
using BrightIdeasSoftware;
using System.IO;
using TradingLibrary;
using TradingLibrary.Trading;
using TradingLibrary.Base;
using System.Diagnostics;

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
        FastObjectListView dayResultListView = new FastObjectListView();

        public Chart totalChart = new Chart();
        public Button totalButton = new Button();
        public Button captureButton = new Button();
        public Button beforeButton = new Button();
        public Button afterButton = new Button();
        public Button beforeAllChartButton = new Button();
        public Button afterAllChartButton = new Button();
        public TextBox fromTextBox = new TextBox();
        public TextBox midTextBox = new TextBox();
        public TextBox toTextBox = new TextBox();
        public Button runButton = new Button();
        public Button runLongButton = new Button();
        public Button runShortButton = new Button();
        public Button yearBeforeButton = new Button();
        public Button yearAfterButton = new Button();
        public Button firstButton = new Button();
        public Button lastButton = new Button();
        public FastObjectListView metricListView = new FastObjectListView();
        public FastObjectListView metricResultListView = new FastObjectListView();
        public FastObjectListView codeListView = new FastObjectListView();
        public FastObjectListView codeResultListView = new FastObjectListView();
        public TabControl resultTabControl = new TabControl();
        public TabControl resultTabControl2 = new TabControl();

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
        string MetricMaxHasTime = "    Date";
        string MetricLongestHasDays = "LH Days";
        string MetricLongestHasDaysCode = "    Code";
        string MetricAverageHasDays = "AVGH Days";

        string sticksDBpath;
        string sticksDBbaseName;

        string TimeSpanFormat = @"d\.hh\:mm\:ss";

        private object dayLocker = new object();
        private object foundLocker = new object();

        Stopwatch sw = new Stopwatch();

        Dictionary<ChartValues, SQLiteConnection> DBDic = new Dictionary<ChartValues, SQLiteConnection>();

        public BackTesting(Form form, bool isJoo) : base(form, isJoo)
        {
            sticksDBpath = BaseSticksDB.path;
            sticksDBbaseName = BaseSticksDB.BaseName;
            SetDB();

            var start = sticksDBpath.LastIndexOf('\\');
            var image_folder = sticksDBpath.Substring(0, start) + "image";
            var dir = new DirectoryInfo(image_folder);
            if (dir.Exists == false)
                dir.Create();
            resultImagePath = image_folder + @"\";

            LoadCodeListAndMetric();

            SetAdditionalMainView();

            fromTextBox.Text = DateTime.MinValue.ToString(DateTimeFormat);
            toTextBox.Text = DateTime.MaxValue.ToString(DateTimeFormat);
        }
        void SetAdditionalMainView()
        {
            ClickAction += (i) =>
            {
                var chartValues = mainChart.Tag as ChartValues;
                var list = showingItemData.listDic[chartValues].list;
                var RSIA2 = CalRSIA(list, list[i], i - 1, 2);
                var RSIA2Diff = RSIA2 - CalRSIA(list, list[i - 1], i - 2, 2);
                var RSIA3 = CalRSIA(list, list[i], i - 1, 3);
                var RSIA3Diff = RSIA3 - CalRSIA(list, list[i - 1], i - 2, 3);
                var RSIA4 = CalRSIA(list, list[i], i - 1, 4);
                var RSIA4Diff = RSIA4 - CalRSIA(list, list[i - 1], i - 2, 4);
                //CheckSuddenBurst(isJoo, list, list[i], chartValues, i - 1);
                form.Text = showingItemData.Code + "     H:" + list[i].Price[0] + "  L:" + list[i].Price[1] + "  O:" + list[i].Price[2] + "  C:" + list[i].Price[3] + "  Ms:" + list[i].Ms + "  Md:" + list[i].Md +
                    "  RSIA2:" + Math.Round(RSIA2, 0) + "  RSIA2Diff:" + Math.Round(RSIA2Diff, 0) + "  RSIA3:" + Math.Round(RSIA3, 0) + "  RSIA3Diff:" + Math.Round(RSIA3Diff, 0) + "  RSIA4:" + Math.Round(RSIA4, 0) + "  RSIA4Diff:" + Math.Round(RSIA4Diff, 0);
            };

            clickResultAction = new Action<DayData>((date) =>
            {
                dayResultListView.ClearObjects();

                var n = 0;
                foreach (var data in date.resultDatas)
                    if (data.showNow && data.EnterTime.Date == date.Date && data.ExitTime.Date <= simulDays.Keys[simulDays.Count - 1])
                    {
                        data.NumberForClick = ++n;
                        dayResultListView.AddObject(data);
                    }

                //dayResultListView.Sort(dayResultListView.GetColumn("Profit"), SortOrder.Ascending);
                //var n = 0;
                //foreach (BackResultData ob in dayResultListView.Objects)
                //    ob.NumberForClick = ++n;
                //dayResultListView.Sort(dayResultListView.GetColumn("No."), SortOrder.Descending);
            });

            SetChart(totalChart, mainChart.Size, new Point(mainChart.Location.X, mainChart.Location.Y));
            totalChart.Hide();
            totalChart.Click += (sender, e) =>
            {
                var e2 = e as MouseEventArgs;
                if (e2.Button == MouseButtons.Left)
                {
                    var result = GetCursorPosition(totalChart, e2);
                    if (!result.isInArea)
                        return;

                    form.Text = totalChart.Series[0].Points[result.Xindex].AxisLabel + "    " + totalChart.Series[0].Points[result.Xindex].YValues[0]
                        + "    " + totalChart.Series[1].Points[result.Xindex].YValues[0];

                    clickResultAction(simulDays[DateTime.Parse(totalChart.Series[0].Points[result.Xindex].AxisLabel)]);
                }
            };

            var chartArea6 = totalChart.ChartAreas.Add("ChartAreaResult");
            chartArea6.Position = new ElementPosition(0, 0, 100, 100);
            chartArea6.InnerPlotPosition = new ElementPosition(7, 2, 86, 90);
            chartArea6.BackColor = ColorSet.ControlBack;
            chartArea6.BorderColor = ColorSet.FormText;

            chartArea6.AxisX.MajorGrid.LineColor = ColorSet.ChartGrid;
            chartArea6.AxisX.MajorTickMark.Enabled = false;
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

            var series0 = totalChart.Series.Add("Market");
            series0.ChartType = SeriesChartType.Line;
            series0.XValueType = ChartValueType.Time;
            series0.Color = ColorSet.ChartGrid;
            series0.YAxisType = AxisType.Secondary;
            series0.ChartArea = chartArea6.Name;

            SetButton(totalButton, "T", (sender, e) =>
            {
                if (!totalChart.Visible)
                {
                    mainChart.Visible = false;
                    totalChart.Visible = true;

                    foreach (var b in buttonDic.Values)
                    {
                        b.BackColor = ColorSet.Button;
                        b.BringToFront();
                    }
                    totalButton.BackColor = ColorSet.ButtonSelected;
                }
            });
            totalButton.Size = buttonDic.Values.Last().Size;
            totalButton.Location = new Point(buttonDic.Values.Last().Location.X, buttonDic.Values.Last().Location.Y + buttonDic.Values.Last().Height + 5);

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

            #region Controller
            var ca = mainChart.ChartAreas[1];
            SetButton(beforeButton, "<", (sender, e) => { FindSimulAndShow(true); });
            beforeButton.Size = new Size((int)((mainChart.Width * (ca.Position.X + ca.Position.Width * ca.InnerPlotPosition.X / 100) / 100 - 15) / 2), totalButton.Height);
            beforeButton.Location = new Point(mainChart.Location.X + 5,
                (int)(mainChart.Location.Y + mainChart.Height * (ca.Position.Y + ca.Position.Height * (ca.InnerPlotPosition.Y + ca.InnerPlotPosition.Height) / 100) / 100 - beforeButton.Height));

            SetButton(afterButton, ">", (sender, e) => { FindSimulAndShow(false); });
            afterButton.Size = beforeButton.Size;
            afterButton.Location = new Point(beforeButton.Location.X + beforeButton.Width + 5, beforeButton.Location.Y);

            SetButton(beforeAllChartButton, "<A", (sender, e) => { FindSimulAndShow(true, false); });
            beforeAllChartButton.Size = beforeButton.Size;
            beforeAllChartButton.Location = new Point(beforeButton.Location.X, beforeButton.Location.Y - 5 - beforeButton.Height);
            beforeAllChartButton.Font = new Font(beforeAllChartButton.Font.FontFamily, 7);

            SetButton(afterAllChartButton, "A>", (sender, e) => { FindSimulAndShow(false, false); });
            afterAllChartButton.Size = beforeButton.Size;
            afterAllChartButton.Location = new Point(afterButton.Location.X, beforeAllChartButton.Location.Y);
            afterAllChartButton.Font = new Font(afterAllChartButton.Font.FontFamily, beforeAllChartButton.Font.Size);

            SetButton(firstButton, "F", (sender, e) =>
            {
                var result = GetFirstOrLastTime(true);
                ShowChart(result.itemData, (result.time, 0, true));
            });
            firstButton.Size = beforeButton.Size;
            firstButton.Location = new Point(beforeAllChartButton.Location.X, beforeAllChartButton.Location.Y - 5 - beforeAllChartButton.Height);

            SetButton(lastButton, "L", (sender, e) =>
            {
                var result = GetFirstOrLastTime(false);
                ShowChart(result.itemData, (result.time, baseChartViewSticksSize, true));
            });
            lastButton.Size = beforeButton.Size;
            lastButton.Location = new Point(afterButton.Location.X, firstButton.Location.Y);
            #endregion
        }
        protected override void SetRestView()
        {
            var runAction = new Action<int>((isALS) =>
            {
                if (DateTime.TryParseExact(fromTextBox.Text, DateTimeFormat, null, System.Globalization.DateTimeStyles.None, out DateTime from) &&
                    DateTime.TryParseExact(toTextBox.Text, DateTimeFormat, null, System.Globalization.DateTimeStyles.None, out DateTime to) && from <= to)
                    Task.Run(new Action(() => {
                        var first = GetFirstOrLastTime(true, default, BaseChartTimeSet.OneDay).time;
                        var last = GetFirstOrLastTime(false, default, BaseChartTimeSet.OneDay).time;
                        RunMain(from < first ? first : from, to > last ? last : to, isALS);
                    }));
                else
                    ShowError(form);
            });

            #region From_To_Run
            SetTextBox(fromTextBox, "");
            fromTextBox.ReadOnly = false;
            fromTextBox.BorderStyle = BorderStyle.Fixed3D;
            fromTextBox.Size = new Size((GetFormWidth(form) - mainChart.Location.X - mainChart.Width) / 4, 30);
            fromTextBox.Location = new Point(mainChart.Location.X + mainChart.Width + 5, mainChart.Location.Y + 5);

            SetTextBox(midTextBox, "~");
            midTextBox.Size = new Size(20, fromTextBox.Height);
            midTextBox.Location = new Point(fromTextBox.Location.X + fromTextBox.Width + 10, fromTextBox.Location.Y);

            SetTextBox(toTextBox, "");
            toTextBox.ReadOnly = false;
            toTextBox.BorderStyle = BorderStyle.Fixed3D;
            toTextBox.Size = new Size(fromTextBox.Width, fromTextBox.Height);
            toTextBox.Location = new Point(midTextBox.Location.X + midTextBox.Width + 10, midTextBox.Location.Y);

            SetButton(runButton, "Run", (sender, e) => { runAction(0); });
            runButton.Size = new Size((GetFormWidth(form) - toTextBox.Location.X - toTextBox.Width - 10) / 2 - 2, toTextBox.Height);
            runButton.Location = new Point(toTextBox.Location.X + toTextBox.Width + 5, toTextBox.Location.Y);

            SetButton(runLongButton, "L", (sender, e) => { runAction(1); });
            runLongButton.Size = new Size(runButton.Width / 2 - 2, toTextBox.Height);
            runLongButton.Location = new Point(runButton.Location.X + runButton.Width + 4, toTextBox.Location.Y);
            //runLongButton.Font = new Font(runLongButton.Font.FontFamily, 5);
            runLongButton.BackColor = ColorSet.PlusPrice;

            SetButton(runShortButton, "S", (sender, e) => { runAction(2); });
            runShortButton.Size = new Size(runLongButton.Width, toTextBox.Height);
            runShortButton.Location = new Point(runLongButton.Location.X + runLongButton.Width + 4, toTextBox.Location.Y);
            //runShortButton.Font = new Font(runShortButton.Font.FontFamily, runShortButton.Font.Size);
            runShortButton.BackColor = ColorSet.MinusPrice;
            #endregion

            #region Metric
            SetListView(metricListView, new (string, string, int)[]
                {
                    ("Metric", "MetricName", 4),
                    ("Strategy", "Strategy", 4),
                    ("Long", "Long", 4),
                    ("Short", "Short", 4),
                    ("Market", "Market", 4)
                });
            metricListView.Size = new Size(GetFormWidth(form) - mainChart.Location.X - mainChart.Width - 5,
                (GetFormHeight(form) - fromTextBox.Location.Y - fromTextBox.Height) / 2 - 10);
            metricListView.Location = new Point(mainChart.Location.X + mainChart.Size.Width, fromTextBox.Location.Y + fromTextBox.Height + 5);
            #endregion

            #region Code_List_Result
            SetListView(codeListView, new (string, string, int)[]
                {
                    ("No.", "number", 2),
                    ("Code", "Code", 7),
                    ("C", "Count", 2),
                    ("WR(%)", "WinRate", 3),
                    ("SBG", "ShortestBeforeGapText", 5)
                });
            codeListView.Size = new Size(metricListView.Width, GetFormHeight(form) - metricListView.Location.Y - metricListView.Height - GetFormUpBarSize(form) - 10);
            codeListView.Location = new Point(metricListView.Location.X, metricListView.Location.Y + metricListView.Height + 5);
            codeListView.SelectionChanged += (sender, e) =>
            {
                if (codeListView.SelectedIndices.Count != 1)
                    return;

                var itemData = codeListView.SelectedObject as BackItemData;
                ShowChart(itemData, (GetFirstOrLastTime(false, itemData).time, baseChartViewSticksSize, false));

                if (simulDays.Count == 0)
                    return;

                ShowCodeResult(itemData);
            };
            #endregion

            #region Results
            var action = new Action<FastObjectListView>((sender) =>
            {
                if (sender.SelectedIndices.Count != 1)
                    return;

                var data = sender.SelectedObject as BackResultData;
                var itemData = itemDataDic[data.Code];
                var result = LoadAndCheckSticks(itemData, true, false, default, data.EnterTime, default, false);
                SetChartNowOrLoad(result.chartValues);
                ShowChart(itemData, (result.foundTime, baseChartViewSticksSize / 2, true), true);
            });

            var tab_page_list = new List<TabPage>() { new TabPage("Metric Result"), new TabPage("Day Result") };

            SetTabControl(resultTabControl, new Size((mainChart.Width - 20) / 2 - 10, GetFormHeight(form) - mainChart.Location.Y - mainChart.Height - GetFormUpBarSize(form) - 10),
                new Point(mainChart.Location.X + 10, mainChart.Location.Y + mainChart.Height + 5), tab_page_list);

            var tabPage = resultTabControl.TabPages[0];
            SetListView(metricResultListView, new (string, string, int)[]
                {
                    ("No.", "Number", 2),
                    ("Date", "Date", 7),
                    ("C", "Count", 2),
                    ("WR(%)", "WinRate", 3),
                    ("PRA(%)", "ProfitRateAvg", 3),
                    ("WPRA(%)", "WinProfitRateAvg", 3),
                    ("LPRA(%)", "LoseProfitRateAvg", 3),
                });
            metricResultListView.Size = new Size(tabPage.Width - 12, tabPage.Height - 6);
            metricResultListView.Location = new Point(6, 6);
            metricResultListView.SelectionChanged += (sender, e) =>
            {
                if (metricResultListView.SelectedIndices.Count == 1)
                {
                    var data = metricResultListView.SelectedObject as DayData;
                    clickResultAction(data);
                    resultTabControl.SelectTab(1);
                }
            };
            tabPage.Controls.Add(metricResultListView);

            tabPage = resultTabControl.TabPages[1];
            SetListView(dayResultListView, new (string, string, int)[]
                {
                    ("No.", "NumberForClick", 2),
                    ("Code", "Code", 7),
                    ("EnterTime", "EnterTime", 7),
                    ("ExitTime", "ExitTime", 7),
                    ("Long", "LorS", 2),
                    ("PR(%)", "ProfitRate", 3),
                    ("Dura", "Duration", 5)
                });
            dayResultListView.Size = new Size(tabPage.Width - 12, tabPage.Height - 6);
            dayResultListView.Location = new Point(6, 6);
            dayResultListView.SelectionChanged += (sender, e) => {
                action(dayResultListView);
                ShowCodeResult(itemDataDic[(dayResultListView.SelectedObject as BackResultData).Code]);
            };
            tabPage.Controls.Add(dayResultListView);

            tab_page_list = new List<TabPage>() { new TabPage("Code Result") };

            SetTabControl(resultTabControl2, new Size(resultTabControl.Width, resultTabControl.Height),
                new Point(resultTabControl.Location.X + resultTabControl.Width + 10, resultTabControl.Location.Y), tab_page_list);

            tabPage = resultTabControl2.TabPages[0];
            SetListView(codeResultListView, new (string, string, int)[]
                {
                    ("No.", "NumberForSingle", 2),
                    ("EnterTime", "EnterTime", 7),
                    ("ExitTime", "ExitTime", 7),
                    ("Long", "LorS", 2),
                    ("PR(%)", "ProfitRate", 3),
                    ("Dura", "Duration", 5),
                    ("BefGap", "BeforeGap", 5)
                });
            codeResultListView.Size = new Size(tabPage.Width - 12, tabPage.Height - 6);
            codeResultListView.Location = new Point(6, 6);
            codeResultListView.SelectionChanged += (sender, e) => { action(codeResultListView); };
            tabPage.Controls.Add(codeResultListView);
            #endregion
        }
        void ShowCodeResult(BackItemData itemData)
        {
            codeResultListView.ClearObjects();
            var n = 0;
            foreach (var day in simulDays)
                foreach (var resultData in day.Value.resultDatas)
                    if (itemData.Code == resultData.Code && resultData.ExitTime.Date == day.Key && resultData.EnterTime.Date >= simulDays.Values[0].Date)
                    {
                        resultData.NumberForSingle = ++n;
                        codeResultListView.AddObject(resultData);
                    }
        }

        void SetDB()
        {
            foreach (var cv in ChartValuesDic.Values)
            {
                var conn = new SQLiteConnection("Data Source =" + sticksDBpath + sticksDBbaseName + cv.Text + ".db");
                DBDic.Add(cv, conn);
                conn.Open();
            }
        }

        void LoadCodeListAndMetric()
        {
            var conn = DBDic[BaseChartTimeSet.OneMinute];

            var reader = new SQLiteCommand("Select name From sqlite_master where type='table'", conn).ExecuteReader();

            var number = 1;
            while (reader.Read())
            {
                var itemData = new BackItemData(reader["name"].ToString(), number++);
                codeListView.AddObject(itemData);
                itemDataDic.Add(itemData.Code, itemData);
            }

            foreach (var itemData in itemDataDic.Values)
                itemData.firstLastMin = (GetFirstOrLastTime(true, itemData, BaseChartTimeSet.OneMinute).time, GetFirstOrLastTime(false, itemData, BaseChartTimeSet.OneMinute).time);


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
            metricDic.Add(MetricMaxHasTime, new MetricData() { MetricName = MetricMaxHasTime });
            metricListView.AddObject(metricDic[MetricMaxHasTime]);
            metricDic.Add(MetricLongestHasDays, new MetricData() { MetricName = MetricLongestHasDays });
            metricListView.AddObject(metricDic[MetricLongestHasDays]);
            metricDic.Add(MetricLongestHasDaysCode, new MetricData() { MetricName = MetricLongestHasDaysCode });
            metricListView.AddObject(metricDic[MetricLongestHasDaysCode]);
            metricDic.Add(MetricAverageHasDays, new MetricData() { MetricName = MetricAverageHasDays });
            metricListView.AddObject(metricDic[MetricAverageHasDays]);
        }

        void RunMain(DateTime start, DateTime end, int isAllLongShort)
        {
            form.Invoke(new Action(() => { ClearChart(totalChart); }));

            if (simulDays.Count == 0 || start < simulDays.Keys[0] || end > simulDays.Keys[simulDays.Keys.Count - 1])
            {
                LoadingSettingFirst(form);
                sw.Reset();
                sw.Start();

                marketDays.Clear();
                simulDays.Clear();
                openDaysPerYear.Clear();

                Run(start, end);

                if (openDaysPerYear.Values.Count > 2)
                {
                    var firstKey = openDaysPerYear.Keys[0];
                    var lastKey = openDaysPerYear.Keys[openDaysPerYear.Values.Count - 1];
                    openDaysPerYear[firstKey] = openDaysPerYear.Values[1];
                    openDaysPerYear[lastKey] = openDaysPerYear.Values[openDaysPerYear.Values.Count - 2];
                }

                sw.Stop();
                HideLoading();
                AlertStart("done : " + sw.Elapsed.ToString(TimeSpanFormat));
            }

            form.Invoke(new Action(() => {
                CalculateMetric(start, end, isAllLongShort);
                totalButton.PerformClick();
            }));
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
        void CalculateMetric(DateTime start, DateTime end, int isAllLongShort)
        {
            #region First Setting
            var startIndex = marketDays.IndexOfKey(start);
            var endIndex = marketDays.IndexOfKey(end);

            metricResultListView.ClearObjects();

            var highestFullHasItemsAtADay = int.MinValue;
            DateTime highestFullHasItemsDate = default;
            var longestFullHasTime = TimeSpan.MinValue;
            var longestFullHasCode = "";
            var beforeFullHasItemsAtADay = 0;
            var fullGoingUp = false;
            var fullCount = 0;
            var fullWin = 0;
            double fullProfitRateSum = 0;
            double fullProfitWinRateSum = 0;

            var highestLongHasItemsAtADay = int.MinValue;
            DateTime highestLongHasItemsDate = default;
            var longestLongHasTime = TimeSpan.MinValue;
            var longestLongHasCode = "";
            var longCount = 0;
            var longWin = 0;
            double longProfitRateSum = 0;
            double longProfitWinRateSum = 0;

            var highestShortHasItemsAtADay = int.MinValue;
            DateTime highestShortHasItemsDate = default;
            var longestShortHasTime = TimeSpan.MinValue;
            var longestShortHasCode = "";
            var shortCount = 0;
            var shortWin = 0;
            double shortProfitRateSum = 0;
            double shortProfitWinRateSum = 0;

            var n = 0;
            #endregion

            foreach (var itemData in itemDataDic.Values)
                itemData.Reset();

            for (int i = startIndex; i <= endIndex; i++)
            {
                var fullHasItemsAtADay = 0;
                var longHasItemsAtADay = 0;
                var shortHasItemsAtADay = 0;

                simulDays.Values[i].Count = 0;
                simulDays.Values[i].Win = 0;
                simulDays.Values[i].WinProfitRateSum = 0;
                simulDays.Values[i].ProfitRateSum = 0;

                //var enterTimesCount = new Dictionary<DateTime, int>();
                //foreach (var resultData in simulDays.Values[i].resultDatas)
                //{
                //    if (!enterTimesCount.ContainsKey(resultData.EnterTime))
                //        enterTimesCount.Add(resultData.EnterTime, 0);

                //    if (resultData.EnterTime.Date == simulDays.Keys[i] && resultData.ExitTime.Date <= end)
                //        enterTimesCount[resultData.EnterTime]++;
                //}

                foreach (var resultData in simulDays.Values[i].resultDatas)
                    if (resultData.EnterTime.Date == simulDays.Keys[i] && resultData.ExitTime.Date <= end)
                    {
                        //if (enterTimesCount[resultData.EnterTime] >= 3)
                        if (isAllLongShort == 0 || (isAllLongShort != 1 ^ resultData.LorS))
                        {
                            resultData.showNow = true;

                            var hasTime = resultData.ExitTime.Subtract(resultData.EnterTime);
                            if (hasTime > longestFullHasTime)
                            {
                                longestFullHasTime = hasTime;
                                longestFullHasCode = resultData.Code;
                            }
                            if (resultData.LorS)
                            {
                                if (hasTime > longestLongHasTime)
                                {
                                    longestLongHasTime = hasTime;
                                    longestLongHasCode = resultData.Code;
                                }
                            }
                            else
                            {
                                if (hasTime > longestShortHasTime)
                                {
                                    longestShortHasTime = hasTime;
                                    longestShortHasCode = resultData.Code;
                                }
                            }

                            var itemData = itemDataDic[resultData.Code];

                            fullHasItemsAtADay++;
                            simulDays.Values[i].Count++;
                            fullCount++;
                            itemData.Count++;
                            simulDays.Values[i].ProfitRateSum += resultData.ProfitRate;
                            fullProfitRateSum += resultData.ProfitRate;
                            if (resultData.LorS)
                            {
                                longHasItemsAtADay++;
                                longCount++;
                                longProfitRateSum += resultData.ProfitRate;
                            }
                            else
                            {
                                shortHasItemsAtADay++;
                                shortCount++;
                                shortProfitRateSum += resultData.ProfitRate;
                            }
                            if (resultData.ProfitRate > commisionRate)
                            {
                                simulDays.Values[i].Win++;
                                fullWin++;
                                itemData.Win++;
                                simulDays.Values[i].WinProfitRateSum += resultData.ProfitRate;
                                fullProfitWinRateSum += resultData.ProfitRate;
                                if (resultData.LorS)
                                {
                                    longWin++;
                                    longProfitWinRateSum += resultData.ProfitRate;
                                }
                                else
                                {
                                    shortWin++;
                                    shortProfitWinRateSum += resultData.ProfitRate;
                                }
                            }
                        }
                        else
                            resultData.showNow = false;
                    }

                if (fullHasItemsAtADay > highestFullHasItemsAtADay)
                {
                    highestFullHasItemsAtADay = fullHasItemsAtADay;
                    highestFullHasItemsDate = simulDays.Keys[i];
                }
                if (longHasItemsAtADay > highestLongHasItemsAtADay)
                {
                    highestLongHasItemsAtADay = longHasItemsAtADay;
                    highestLongHasItemsDate = simulDays.Keys[i];
                }
                if (shortHasItemsAtADay > highestShortHasItemsAtADay)
                {
                    highestShortHasItemsAtADay = shortHasItemsAtADay;
                    highestShortHasItemsDate = simulDays.Keys[i];
                }

                if (simulDays.Values[i].Count != 0)
                {
                    simulDays.Values[i].WinRate = Math.Round((double)simulDays.Values[i].Win / simulDays.Values[i].Count * 100, 2);
                    simulDays.Values[i].ProfitRateAvg = Math.Round(simulDays.Values[i].ProfitRateSum / simulDays.Values[i].Count, 2);
                    simulDays.Values[i].WinProfitRateAvg = simulDays.Values[i].Win == 0 ? 0 : Math.Round(simulDays.Values[i].WinProfitRateSum / simulDays.Values[i].Win, 2);
                    simulDays.Values[i].LoseProfitRateAvg = simulDays.Values[i].Count == simulDays.Values[i].Win ? 0 :
                        Math.Round((simulDays.Values[i].ProfitRateSum - simulDays.Values[i].WinProfitRateSum) / (simulDays.Values[i].Count - simulDays.Values[i].Win), 2);
                }
                else
                {
                    simulDays.Values[i].WinRate = 0;
                    simulDays.Values[i].ProfitRateAvg = 0;
                    simulDays.Values[i].WinProfitRateAvg = 0;
                    simulDays.Values[i].LoseProfitRateAvg = 0;
                }

                if (fullHasItemsAtADay > beforeFullHasItemsAtADay)
                    fullGoingUp = true;
                else if (fullHasItemsAtADay < beforeFullHasItemsAtADay)
                {
                    //if (goingUp && beforeBuyItemsAtOnce > 0)
                    //    allListView.AddObject(simulDays.Values[i - 1]);
                    fullGoingUp = false;
                }
                beforeFullHasItemsAtADay = fullHasItemsAtADay;

                if (simulDays.Values[i].Count != 0)
                {
                    simulDays.Values[i].Number = ++n;
                    metricResultListView.AddObject(simulDays.Values[i]);
                }

                totalChart.Series[0].Points.AddXY(simulDays.Keys[i].ToString(DateTimeFormat), simulDays.Values[i].ProfitRateAvg);
                totalChart.Series[1].Points.AddXY(simulDays.Keys[i].ToString(DateTimeFormat), fullHasItemsAtADay);
            }

            foreach (var itemData in itemDataDic.Values)
                itemData.WinRate = itemData.Count == 0 ? -1 : Math.Round((double)itemData.Win / itemData.Count * 100, 2);

            //metricResultListView.Sort(metricResultListView.GetColumn("PRA"), SortOrder.Ascending);
            //var n = 0;
            //foreach (DayData ob in metricResultListView.Objects)
            //    ob.Number = ++n;
            //metricResultListView.Sort(metricResultListView.GetColumn("No."), SortOrder.Descending);

            #region Print Result
            if (fullCount != 0)
            {
                metricDic[MetricWinRate].Strategy = Math.Round((double)fullWin / fullCount * 100, 2) + "%";
                metricDic[MetricAvgProfitRate].Strategy = Math.Round(fullProfitRateSum / fullCount, 2) + "%";
                metricDic[MetricWinAvgProfitRate].Strategy = fullWin == 0 ? "0%" : (Math.Round(fullProfitWinRateSum / fullWin, 2) + "%");
                metricDic[MetricLoseAvgProfitRate].Strategy = fullCount == fullWin ? "0%" : (Math.Round((fullProfitRateSum - fullProfitWinRateSum) / (fullCount - fullWin), 2) + "%");
                metricDic[MetricMaxHasTime].Strategy = highestFullHasItemsDate.ToString(DateTimeFormat);
                metricDic[MetricLongestHasDays].Strategy = longestFullHasTime.ToString(TimeSpanFormat);
            }
            else
            {
                metricDic[MetricWinRate].Strategy = "0%";
                metricDic[MetricAvgProfitRate].Strategy = "0%";
                metricDic[MetricWinAvgProfitRate].Strategy = "0%";
                metricDic[MetricLoseAvgProfitRate].Strategy = "0%";
                metricDic[MetricMaxHasTime].Strategy = "";
                metricDic[MetricLongestHasDays].Strategy = "";
            }
            metricDic[MetricAllCount].Strategy = fullCount.ToString();
            metricDic[MetricMaxHas].Strategy = highestFullHasItemsAtADay.ToString();
            metricDic[MetricLongestHasDaysCode].Strategy = longestFullHasCode;

            if (longCount != 0)
            {
                metricDic[MetricWinRate].Long = Math.Round((double)longWin / longCount * 100, 2) + "%";
                metricDic[MetricAvgProfitRate].Long = Math.Round(longProfitRateSum / longCount, 2) + "%";
                metricDic[MetricWinAvgProfitRate].Long = longWin == 0 ? "0%" : (Math.Round(longProfitWinRateSum / longWin, 2) + "%");
                metricDic[MetricLoseAvgProfitRate].Long = longCount == longWin ? "0%" : (Math.Round((longProfitRateSum - longProfitWinRateSum) / (longCount - longWin), 2) + "%");
                metricDic[MetricMaxHasTime].Long = highestLongHasItemsDate.ToString(DateTimeFormat);
                metricDic[MetricLongestHasDays].Long = longestLongHasTime.ToString(TimeSpanFormat);
            }
            else
            {
                metricDic[MetricWinRate].Long = "0%";
                metricDic[MetricAvgProfitRate].Long = "0%";
                metricDic[MetricWinAvgProfitRate].Long = "0%";
                metricDic[MetricLoseAvgProfitRate].Long = "0%";
                metricDic[MetricMaxHasTime].Long = "";
                metricDic[MetricLongestHasDays].Long = "";
            }
            metricDic[MetricAllCount].Long = longCount.ToString();
            metricDic[MetricMaxHas].Long = highestLongHasItemsAtADay.ToString();
            metricDic[MetricLongestHasDaysCode].Long = longestLongHasCode;

            if (shortCount != 0)
            {
                metricDic[MetricWinRate].Short = Math.Round((double)shortWin / shortCount * 100, 2) + "%";
                metricDic[MetricAvgProfitRate].Short = Math.Round(shortProfitRateSum / shortCount, 2) + "%";
                metricDic[MetricWinAvgProfitRate].Short = shortWin == 0 ? "0%" : (Math.Round(shortProfitWinRateSum / shortWin, 2) + "%");
                metricDic[MetricLoseAvgProfitRate].Short = shortCount == shortWin ? "0%" : (Math.Round((shortProfitRateSum - shortProfitWinRateSum) / (shortCount - shortWin), 2) + "%");
                metricDic[MetricMaxHasTime].Short = highestShortHasItemsDate.ToString(DateTimeFormat);
                metricDic[MetricLongestHasDays].Short = longestShortHasTime.ToString(TimeSpanFormat);
            }
            else
            {
                metricDic[MetricWinRate].Short = "0%";
                metricDic[MetricAvgProfitRate].Short = "0%";
                metricDic[MetricWinAvgProfitRate].Short = "0%";
                metricDic[MetricLoseAvgProfitRate].Short = "0%";
                metricDic[MetricMaxHasTime].Short = "";
                metricDic[MetricLongestHasDays].Short = "";
            }
            metricDic[MetricAllCount].Short = shortCount.ToString();
            metricDic[MetricMaxHas].Short = highestShortHasItemsAtADay.ToString();
            metricDic[MetricLongestHasDaysCode].Short = longestShortHasCode;
            #endregion
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

        void FindSimulAndShow(bool toPast, bool oneChart = true)
        {
            Task.Run(new Action(() => {
                LoadingSettingFirst(form);
                sw.Reset();
                sw.Start();

                var m = toPast ? -1 : 1;

                BackItemData itemData = default;
                bool all = true;
                form.Invoke(new Action(() => {
                    all = (!codeListView.Focused && !lastClickInChart) || codeListView.SelectedIndices.Count != 1 || (codeListView.SelectedObject as BackItemData).Code != showingItemData.Code;
                }));
                if (!all)
                    itemData = showingItemData;

                var from = GetStandardDate(!toPast, oneChart);
                var firstFrom = from;
                AddOrChangeLoadingText("Finding...(" + from.ToString(TimeFormat) + ")", true);

                var chartValues = oneChart ? mainChart.Tag as ChartValues : BaseChartTimeSet.OneMinute;

                (DateTime foundTime, ChartValues chartValues) result = default;
                var limitTime = GetFirstOrLastTime(toPast, itemData, chartValues).time;

                int baseLoadSizeForSearch = BaseChartTimeSet.OneDay.seconds / BaseChartTimeSet.OneMinute.seconds;
                int firstLoadSizeForSearch = oneChart ? baseLoadSizeForSearch : ((int)from.TimeOfDay.TotalMinutes + 1);
                if (!oneChart && !toPast)
                    firstLoadSizeForSearch = BaseChartTimeSet.OneDay.seconds / BaseChartTimeSet.OneMinute.seconds - firstLoadSizeForSearch + 1;
                var count = 1;
                int continueAskingCount = 10;

                while (result.foundTime == DateTime.MinValue
                    && (toPast ? from >= limitTime : from <= limitTime)
                    && (count % continueAskingCount != 0 || MessageBox.Show("Keep searching?", (count * baseLoadSizeForSearch).ToString(), MessageBoxButtons.YesNo) == DialogResult.Yes))
                {
                    AddOrChangeLoadingText("Finding...(" + from.ToString(TimeFormat) + ")   " + sw.Elapsed.ToString(TimeSpanFormat), false);

                    var size = from == firstFrom ? firstLoadSizeForSearch : baseLoadSizeForSearch;
                    var loadNew = oneChart || from == firstFrom;

                    if (all)
                        foreach (var itemData2 in itemDataDic.Values)
                        {
                            var size2 = size;
                            var from2 = from;
                            if ((oneChart || mainChart.Tag as ChartValues == BaseChartTimeSet.OneMinute) && from == firstFrom && itemData2 == showingItemData)
                            {
                                size2 -= 1;
                                from2 = chartValues != BaseChartTimeSet.OneMonth ? from.AddSeconds(m * chartValues.seconds) : from.AddMonths(m);
                            }

                            var result2 = LoadAndCheckSticks(itemData2, loadNew, toPast, size2, from2, chartValues, oneChart);

                            if (result2.foundTime != DateTime.MinValue)
                            {
                                if (result.foundTime != DateTime.MinValue)
                                {
                                    var numberNow = showingItemData != default ? showingItemData.number : (toPast ? itemDataDic.Count + 1 : 0);
                                    var distant = m * result.foundTime.Subtract(from).TotalSeconds;
                                    var distant2 = m * result2.foundTime.Subtract(from).TotalSeconds;
                                    if (result2.foundTime != from
                                        ? (distant2 < distant || (toPast && distant2 == distant))
                                        : (m * (itemData2.number - numberNow) > 0 && (toPast || result.foundTime != from)))
                                    {
                                        itemData = itemData2;
                                        result = result2;
                                    }
                                }
                                else
                                {
                                    itemData = itemData2;
                                    result = result2;
                                }
                            }
                        }
                    else
                    {
                        var size2 = size;
                        var from2 = from;
                        if ((oneChart || mainChart.Tag as ChartValues == BaseChartTimeSet.OneMinute) && from == firstFrom)
                        {
                            size2 -= 1;
                            from2 = chartValues != BaseChartTimeSet.OneMonth ? from.AddSeconds(m * chartValues.seconds) : from.AddMonths(m);
                        }

                        result = LoadAndCheckSticks(itemData, loadNew, toPast, size2, from2, chartValues, oneChart);
                    }

                    from = chartValues != BaseChartTimeSet.OneMonth ? from.AddSeconds(m * chartValues.seconds * size) : from.AddMonths(m * size);

                    count++;
                }

                if (result.foundTime != DateTime.MinValue)
                {
                    foreach (var i in itemDataDic.Values)
                        if (i != itemData)
                            foreach (var v in i.listDic.Values)
                                v.Reset();

                    form.BeginInvoke(new Action(() => {
                        ClearMainChartAndSet(result.chartValues);
                        ShowChart(itemData, (result.foundTime, baseChartViewSticksSize / 2, true), true);
                    }));
                }
                else
                {
                    MessageBox.Show("none", "Alert", MessageBoxButtons.OK);
                    if (showingItemData != default)
                        LoadAndCheckSticks(showingItemData, true, true, mainChart.Series[0].Points.Count, DateTime.Parse(mainChart.Series[0].Points.Last().AxisLabel));
                }

                sw.Stop();
                HideLoading();
                AlertStart("done : " + sw.Elapsed.ToString(TimeSpanFormat));
            }));
        }

        public override void SetChartNowOrLoad(ChartValues chartValues)
        {
            mainChart.Visible = true;
            totalChart.Visible = false;
            totalButton.BackColor = ColorSet.Button;

            var from = mainChart.Tag != null ? GetStandardDate() : default;
            var position = double.IsNaN(mainChart.ChartAreas[0].CursorX.Position) ? baseChartViewSticksSize / 2
                : mainChart.ChartAreas[0].CursorX.Position - mainChart.ChartAreas[0].AxisX.ScaleView.ViewMinimum - 1;

            ClearMainChartAndSet(chartValues);

            if (showingItemData == default)
                return;

            var firstTime = GetFirstOrLastTime(true, showingItemData).time;
            if (from < firstTime)
                from = firstTime;

            ShowChart(showingItemData, (from, (int)position, true), showingItemData.listDic[mainChart.Tag as ChartValues].list.Count != 0);
        }
        void ShowChart(BackItemData itemData, (DateTime time, int position, bool on) cursor, bool loaded = false)
        {
            var chartValues = mainChart.Tag as ChartValues;
            showingItemData = itemData;
            form.Text = itemData.Code;
            var v = itemData.listDic[chartValues];
            cursor.time = cursor.time.AddSeconds(-(int)cursor.time.TimeOfDay.TotalSeconds % chartValues.seconds);

            if (!loaded)
            {
                var more = baseChartViewSticksSize - cursor.position + baseChartViewSticksSize / 2;
                LoadAndCheckSticks(itemData, true, true, baseChartViewSticksSize * 2,
                    chartValues != BaseChartTimeSet.OneMonth ? cursor.time.AddSeconds(more * chartValues.seconds) : cursor.time.AddMonths(more),
                    chartValues);
            }
            else
            {
                if (cursor.time < v.list[0].Time)
                    LoadAndCheckSticks(itemData, false, true, (int)v.list[0].Time.Subtract(cursor.time).TotalSeconds / chartValues.seconds + baseChartViewSticksSize);
                else if (cursor.time > v.list[v.list.Count - 1].Time)
                    LoadAndCheckSticks(itemData, false, true, (int)cursor.time.Subtract(v.list[v.list.Count - 1].Time).TotalSeconds / chartValues.seconds + baseChartViewSticksSize);

                if (cursor.time.Subtract(v.list[0].Time).TotalSeconds / chartValues.seconds < baseChartViewSticksSize)
                    LoadAndCheckSticks(itemData, false, true);
                if (v.list[v.list.Count - 1].Time.Subtract(cursor.time).TotalSeconds / chartValues.seconds < baseChartViewSticksSize)
                    LoadAndCheckSticks(itemData, false, false);

                var foundIndex = 0;
                var exitIndex = -1;
                var enterIndex = -1;
                for (int i = 0; i < v.list.Count; i++)
                {
                    if (v.list[i].Time == cursor.time)
                        foundIndex = i;
                    var resultData = (v.list[i] as BackTradeStick).resultData;
                    if (resultData != default)
                    {
                        exitIndex = i;
                        var exitOpenTime = resultData.ExitTime.AddSeconds(-(int)resultData.ExitTime.TimeOfDay.TotalSeconds % chartValues.seconds); 
                        var enterOpenTime = resultData.EnterTime.AddSeconds(-(int)resultData.EnterTime.TimeOfDay.TotalSeconds % chartValues.seconds);
                        enterIndex = i - (int)exitOpenTime.Subtract(enterOpenTime).TotalSeconds / chartValues.seconds;
                        if (v.list[enterIndex].Time != enterOpenTime)
                            ShowError(form);
                    }
                }

                int left = default;
                int right = default;
                if (exitIndex == -1)
                {
                    left = foundIndex;
                    right = foundIndex;
                }
                else if (foundIndex > exitIndex)
                {
                    left = enterIndex;
                    right = foundIndex;
                }
                else if (foundIndex < enterIndex)
                {
                    left = foundIndex;
                    right = exitIndex;
                }
                else
                {
                    left = enterIndex;
                    right = exitIndex;
                }

                if (v.list.Count > right + baseChartViewSticksSize)
                    v.list.RemoveRange(right + baseChartViewSticksSize, v.list.Count - (right + baseChartViewSticksSize));
                if (left > baseChartViewSticksSize)
                    v.list.RemoveRange(0, left - baseChartViewSticksSize);
            }

            //if (mainChart.Series[0].Points.Count != 0)
                ClearMainChartAndSet(chartValues);

            var cursorIndex = v.list.Count - 1;
            for (int i = 0; i < v.list.Count; i++)
            {
                AddNewChartPoint(mainChart, showingItemData, i, false);
                if (v.list[i].Time == cursor.time || (i + 1 < v.list.Count && v.list[i].Time < cursor.time && cursor.time < v.list[i + 1].Time))
                    cursorIndex = i;
            }

            var zoomStart = cursorIndex - cursor.position + 1;
            ZoomX(mainChart, zoomStart, zoomStart + baseChartViewSticksSize);

            if (cursor.on)
            {
                mainChart.ChartAreas[0].CursorX.Position = cursorIndex + 1;
                SetCursorText(cursorIndex);
            }
            else
                cursorTimeTextBox.Text = "";

            foreach (var p in itemData.listDic)
                if (p.Value.found)
                    buttonDic[p.Key].BackColor = p.Key == chartValues ? ColorSet.ButtonSelectedFound : ColorSet.ButtonFound;
        }

        void AddNewChartPoint(Chart chart, BackItemData itemData, int index, bool insert)
        {
            var vc = chart.Tag as ChartValues;
            var v = itemData.listDic[vc];
            if (insert)
                InsertFullChartPoint(chart, v.list[index]);
            else
                AddFullChartPoint(chart, v.list[index]);

            if ((v.list[index] as BackTradeStick).suddenBurst)
                foreach (var ca in mainChart.ChartAreas)
                    ca.AxisX.StripLines.Add(new StripLine()
                    {
                        BackColor = ColorSet.Losing,
                        ForeColor = ColorSet.FormText,
                        IntervalOffset = index + 1,
                        StripWidth = 1,
                        TextLineAlignment = StringAlignment.Near,
                        TextAlignment = StringAlignment.Center,
                        TextOrientation = TextOrientation.Horizontal,
                        Text = ca.Name == PriceChartAreaName ? "1" : ""
                    });
            if ((v.list[index] as BackTradeStick).resultData != default)
            {
                var resultData = (v.list[index] as BackTradeStick).resultData;
                var EnterTime = resultData.EnterTime.AddSeconds(-(int)(resultData.EnterTime.TimeOfDay.TotalSeconds % vc.seconds));
                var ExitTime = resultData.ExitTime.AddSeconds(-(int)(resultData.ExitTime.TimeOfDay.TotalSeconds % vc.seconds));
                var width = (int)(ExitTime.Subtract(EnterTime).TotalSeconds / vc.seconds);
                if (v.list[index].Time != ExitTime || v.list[index - width].Time != EnterTime)
                    ShowError(form);
                    
                foreach (var ca in mainChart.ChartAreas)
                    ca.AxisX.StripLines.Add(new StripLine()
                    {
                        BackColor = ColorSet.Winning,
                        ForeColor = ColorSet.FormText,
                        IntervalOffset = index - width + 1,
                        StripWidth = width,
                        TextLineAlignment = StringAlignment.Near,
                        TextAlignment = StringAlignment.Center,
                        TextOrientation = TextOrientation.Horizontal,
                        Text = ca.Name == PriceChartAreaName ? "\n2" : ""
                    });
            }
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
            var v = showingItemData.listDic[chartValue];
            var countLast = v.list.Count;
            LoadAndCheckSticks(showingItemData, false, toPast, default, default, chartValue);
            var addedCount = v.list.Count - countLast;

            if (toPast)
            {
                foreach (var ca in chart.ChartAreas)
                    foreach (var strip in ca.AxisX.StripLines)
                        strip.IntervalOffset += addedCount;

                for (int i = addedCount - 1; i >= 0; i--)
                    AddNewChartPoint(chart, showingItemData, i, true);

                zoomStart += addedCount;
                zoomEnd += addedCount;

                if (!double.IsNaN(chart.ChartAreas[0].CursorX.Position))
                    chart.ChartAreas[0].CursorX.Position += addedCount;
            }
            else
                for (int i = countLast; i < countLast + addedCount; i++)
                    AddNewChartPoint(chart, showingItemData, i, false);

            ZoomX(mainChart, (int)zoomStart, (int)zoomEnd);
        }

        (DateTime foundTime, ChartValues chartValues) LoadAndCheckSticks(BackItemData itemData, bool newLoad, bool toPast, int size = default, DateTime from = default, ChartValues chartValues = default, bool oneChart = true)
        {
            (DateTime foundTime, ChartValues chartValues) result = (DateTime.MinValue, default);

            var multiplier = toPast ? -1 : 1;

            if (oneChart)
            {
                if (chartValues == default)
                    chartValues = mainChart.Tag as ChartValues;
                result.chartValues = chartValues;

                if (size == default)
                    size = baseLoadSticksSize;

                var v = itemData.listDic[chartValues];

                if (!newLoad)
                {
                    var lastTime = toPast ? v.list[0].Time : v.list[v.list.Count - 1].Time;
                    from = chartValues != BaseChartTimeSet.OneMonth ? lastTime.AddSeconds(multiplier * chartValues.seconds) : lastTime.AddMonths(multiplier);
                }

                var list = LoadSticks(itemData, chartValues,
                    toPast ? from : 
                        (chartValues != BaseChartTimeSet.OneMonth ? 
                            from.AddSeconds(-chartValues.seconds * (TotalNeedDays - 1)) : 
                            from.AddMonths(-(TotalNeedDays - 1))),
                    size + (TotalNeedDays - 1), toPast);
                var startIndex = GetStartIndex(list, toPast ? (chartValues != BaseChartTimeSet.OneMonth ? from.AddSeconds(-chartValues.seconds * (size - 1)) : from.AddMonths(-(size - 1))) : from);
                if (startIndex == -1)
                    return result;

                for (int i = startIndex; i < list.Count; i++)
                {
                    SetRSIAandDiff(list, list[i], i - 1);
                    if (OneChartFindCondition(list[i]))
                    {
                        (list[i] as BackTradeStick).suddenBurst = true;
                        if (toPast || result.foundTime == DateTime.MinValue)
                            result.foundTime = list[i].Time;
                    }
                }

                list.RemoveRange(0, startIndex);

                if (!newLoad && !toPast)
                    v.list.AddRange(list);
                else
                {
                    if (!newLoad)
                        list.AddRange(v.list);

                    v.list = list;
                }

                if (newLoad)
                    foreach (var p in itemData.listDic)
                        p.Value.found = false;
            }
            else
            {
                if (newLoad)
                    foreach (var l in itemData.listDic.Values)
                        l.Reset();

                var checkStartTime = from;
                from = toPast ? from.Date.AddDays(1).AddMinutes(-1) : from.Date;

                var minituesInADay = BaseChartTimeSet.OneDay.seconds / BaseChartTimeSet.OneMinute.seconds;

                if (size == default || size % minituesInADay != 0)
                    size = (size / minituesInADay + 1) * minituesInADay;

                var toM = from.AddSeconds(multiplier * BaseChartTimeSet.OneMinute.seconds * (size - 1));

                if (toPast ? from < itemData.firstLastMin.firstMin || toM > itemData.firstLastMin.lastMin : toM < itemData.firstLastMin.firstMin || from > itemData.firstLastMin.lastMin)
                    return result;

                var minIndex = itemData.listDic.IndexOfKey(BaseChartTimeSet.OneMinute);
                var dayIndex = itemData.listDic.IndexOfKey(BaseChartTimeSet.OneDay);

                var vm = itemData.listDic.Values[minIndex];
                var vmc = itemData.listDic.Keys[minIndex];
                for (int i = minIndex; i <= dayIndex; i++)
                {
                    var v = itemData.listDic.Values[i];
                    var vc = itemData.listDic.Keys[i];
                    var from2 = toPast ? from : from.AddSeconds(-vc.seconds * (TotalNeedDays - 1));
                    if (i == minIndex)
                    {
                        v.list = LoadSticks(itemData, vc, from2, size + (TotalNeedDays - 1), toPast);

                        if (v.list.Count == 0)
                            ShowError(form);

                        v.startIndex = GetStartIndex(v.list, toPast ? toM : from);

                        if ((toPast ? v.list[v.startIndex].Time : v.list[v.list.Count - 1].Time) == toM)
                        {
                            if ((toPast ? v.list[v.startIndex].Time.ToString(HourMinSecTimeFormat) : v.list[v.list.Count - 1].Time.AddSeconds(vc.seconds).ToString(HourMinSecTimeFormat)) != "00:00:00")
                                ShowError(form);
                        }
                    }
                    else
                    {
                        if (v.list.Count == 0 || vm.list[vm.startIndex].Time < v.list[v.startIndex].Time || vm.list[vm.list.Count - 1].Time > v.list[v.list.Count - 1].Time.AddSeconds(vc.seconds))
                        {
                            var size2 = minituesInADay * BaseChartTimeSet.OneMinute.seconds / vc.seconds * (i - minIndex + 1) + (TotalNeedDays - 1);
                            v.list = LoadSticks(itemData, vc, from2, size2, toPast);

                            var to = from.AddSeconds(multiplier * vc.seconds * (size2 - 1));
                            v.startIndex = GetStartIndex(v.list, toPast ? to : from);

                            if ((toPast ? v.list[v.startIndex].Time : v.list[v.list.Count - 1].Time) == to)
                            {
                                if ((toPast ? v.list[v.startIndex].Time.ToString(HourMinSecTimeFormat) : v.list[v.list.Count - 1].Time.AddSeconds(vc.seconds).ToString(HourMinSecTimeFormat)) != "00:00:00")
                                    ShowError(form);
                            }
                        }
                        v.currentIndex = (int)(vm.list[vm.startIndex].Time.Subtract(v.list[v.startIndex].Time).TotalSeconds / vc.seconds) + v.startIndex;
                        v.lastStick = new BackTradeStick() { Time = v.list[v.currentIndex].Time };

                        if (vm.list[vm.startIndex].Time.Subtract(v.lastStick.Time).TotalSeconds >= vc.seconds)
                            ShowError(form);
                    }
                }

                List<(DateTime foundTime, ChartValues chartValues)> fixedFoundList = default;
                for (vm.currentIndex = vm.startIndex; vm.currentIndex < vm.list.Count; vm.currentIndex++)
                {
                    vm.lastStick = vm.list[vm.currentIndex] as BackTradeStick;
                    itemData.foundList = (new List<(DateTime foundTime, ChartValues chartValues)>(), new List<(DateTime foundTime, ChartValues chartValues)>());

                    if (itemData.Code == "PEOPLEUSDT" && vm.lastStick.Time.ToString(TimeFormat) == "2022-01-03 03:52:00")
                        itemData.Code = itemData.Code;

                    for (int j = minIndex; j <= dayIndex; j++)
                    {
                        var v = itemData.listDic.Values[j];
                        var vc = itemData.listDic.Keys[j];
                        if (j != minIndex)
                        {
                            var timeDiff = vm.lastStick.Time.Subtract(v.lastStick.Time).TotalSeconds;
                            if (timeDiff == vc.seconds)
                            {
                                if (!BackTradeStick.isEqual(v.lastStick as BackTradeStick, v.list[v.currentIndex] as BackTradeStick))
                                    ShowError(form);

                                SetRSIAandDiff(v.list, v.list[v.currentIndex], v.currentIndex - 1);

                                v.currentIndex++;
                                v.lastStick = new BackTradeStick() { Time = v.list[v.currentIndex].Time };
                            }
                            else if (timeDiff > vc.seconds)
                                ShowError(form);

                            if (v.lastStick.Price[1] == 0)
                            {
                                v.lastStick.Price[1] = vm.lastStick.Price[1];
                                v.lastStick.Price[2] = vm.lastStick.Price[2];
                            }

                            if (vm.lastStick.Price[0] > v.lastStick.Price[0])
                                v.lastStick.Price[0] = vm.lastStick.Price[0];
                            if (vm.lastStick.Price[1] < v.lastStick.Price[1])
                                v.lastStick.Price[1] = vm.lastStick.Price[1];
                            v.lastStick.Price[3] = vm.lastStick.Price[3];

                            v.lastStick.Ms += vm.lastStick.Ms;
                            v.lastStick.Md += vm.lastStick.Md;

                            v.lastStick.TCount += vm.lastStick.TCount;
                        }

                        SetRSIAandDiff(v.list, v.lastStick, v.currentIndex - 1);

                        //// st1
                        //if ((run || (toPast ? vm.lastStick.Time <= checkStartTime : vm.lastStick.Time >= checkStartTime)) &&
                        //    CheckSuddenBurst(isJoo, v.list, v.lastStick, cv, v.currentIndex - 1))
                        //{
                        //    if (v.lastStick.Price[3] > v.lastStick.Price[2])
                        //    {
                        //        if (shortFoundList.Count == 0)
                        //            shortFoundList.Add((vm.lastStick.Time, vmc));
                        //        shortFoundList.Add((v.lastStick.Time, cv));
                        //    }
                        //    else
                        //    {
                        //        if (longFoundList.Count == 0)
                        //            longFoundList.Add((vm.lastStick.Time, vmc));
                        //        longFoundList.Add((v.lastStick.Time, cv));
                        //    }
                        //}

                        // st2
                        if (toPast ? vm.lastStick.Time <= checkStartTime : vm.lastStick.Time >= checkStartTime)
                            OneChartFindConditionAndAdd(itemData, vc);

                        //// st3 안 좋음
                        //if ((run || (toPast ? vm.lastStick.Time <= checkStartTime : vm.lastStick.Time >= checkStartTime)) && v.currentIndex >= 2)
                        //{
                        //    var firstCondition = CheckSuddenBurst(isJoo, v.list, v.list[v.currentIndex - 1], cv, v.currentIndex - 2);
                        //    if (firstCondition)
                        //    {
                        //        firstCondition = (v.lastStick.Price[2] == v.lastStick.Price[3]) || (v.list[v.currentIndex - 1].Price[3] > v.list[v.currentIndex - 1].Price[2] ?
                        //                (v.list[v.currentIndex - 1].Price[3] / v.list[v.currentIndex - 1].Price[2] - 1m) / (v.lastStick.Price[2] / v.lastStick.Price[3] - 1m) > 5m :
                        //                (v.list[v.currentIndex - 1].Price[2] / v.list[v.currentIndex - 1].Price[3] - 1m) / (v.lastStick.Price[3] / v.lastStick.Price[2] - 1m) > 5m);
                        //    }

                        //    if (firstCondition ||
                        //    ((v.lastStick.Price[3] > v.lastStick.Price[2] ? shortFoundList.Count : longFoundList.Count) > 0 && CheckSuddenBurst(isJoo, v.list, v.lastStick, cv, v.currentIndex - 1)))
                        //    {
                        //        if (firstCondition ? v.list[v.currentIndex - 1].Price[3] > v.list[v.currentIndex - 1].Price[2] : v.lastStick.Price[3] > v.lastStick.Price[2])
                        //        {
                        //            if (shortFoundList.Count == 0)
                        //                shortFoundList.Add((vm.lastStick.Time, vmc));
                        //            shortFoundList.Add((v.lastStick.Time, cv));
                        //        }
                        //        else
                        //        {
                        //            if (longFoundList.Count == 0)
                        //                longFoundList.Add((vm.lastStick.Time, vmc));
                        //            longFoundList.Add((v.lastStick.Time, cv));
                        //        }
                        //    }
                        //}
                    }

                    if (!itemData.Enter)
                    {
                        var conditionResult = AllChartFindCondition(itemData.foundList);
                        if (conditionResult.found && (toPast || result.foundTime == DateTime.MinValue))
                            EnterSetting(itemData, vm.lastStick, conditionResult.isLong ? itemData.foundList.Long : itemData.foundList.Short, conditionResult.isLong);
                    }
                    else
                    {
                        //var last2ndFound = itemData.EnterFoundList[itemData.EnterFoundList.Count - 2];
                        //var timeTerm = (int)vm.lastStick.Time.AddSeconds(vmc.seconds).Subtract(last2ndFound.foundTime).TotalSeconds;
                        //if (timeTerm % last2ndFound.chartValues.seconds == 0)
                        //{
                        //    var size2 = timeTerm / last2ndFound.chartValues.seconds - 1;
                        //    if (size2 >= 2)
                        //    {
                        //        var v3 = itemData.listDic[last2ndFound.chartValues];
                        //        var RSIA = CalRSIA(v3.list, v3.lastStick, v3.currentIndex - 1, size2 > 4 ? 4 : size2);
                        //        if (itemData.EnterPositionIsLong ? 
                        //            (RSIA < 50 || RSIA - v3.list[v3.currentIndex - 1].RSIA < -10) : 
                        //            (RSIA > 50 || RSIA - v3.list[v3.currentIndex - 1].RSIA > 10))
                        //    }
                        //}

                        if (ExitConditionFinal(itemData))
                        {
                            itemData.Enter = false;

                            var resultData = new BackResultData()
                            {
                                Code = itemData.Code,
                                EnterTime = itemData.EnterTime,
                                ExitTime = vm.lastStick.Time,
                                ProfitRate = Math.Round((double)((itemData.EnterPositionIsLong ? vm.lastStick.Price[3] / itemData.EnterPrice : itemData.EnterPrice / vm.lastStick.Price[3]) - 1) * 100, 2),
                                Duration = vm.lastStick.Time.Subtract(itemData.EnterTime).ToString(TimeSpanFormat),
                                LorS = itemData.EnterPositionIsLong
                            };

                            result = itemData.EnterFoundList[1];
                            fixedFoundList = itemData.EnterFoundList;
                            foreach (var found in itemData.EnterFoundList)
                            {
                                var v2 = itemData.listDic[found.chartValues];
                                (v2.list[v2.currentIndex] as BackTradeStick).resultData = resultData;
                            }
                        }
                    }

                    if (vm.currentIndex == vm.list.Count - 1 && itemData.Enter && vm.lastStick.Time.AddMinutes(1) == vm.lastStick.Time.Date.AddDays(1))
                    {
                        var to = vm.lastStick.Time.Date.AddDays(2);
                        for (int i = minIndex; i <= dayIndex; i++)
                        {
                            var v = itemData.listDic.Values[i];
                            var vc = itemData.listDic.Keys[i];
                            var from2 = v.list[v.list.Count - 1].Time.AddSeconds(vc.seconds);
                            if (from2 < to)
                                v.list.AddRange(LoadSticks(itemData, vc, from2, minituesInADay / (vc.seconds / vmc.seconds), false));
                        }
                    }
                }

                if (result.foundTime != DateTime.MinValue)
                    for (int i = minIndex; i <= dayIndex; i++)
                    {
                        var v = itemData.listDic.Values[i];
                        var vc = itemData.listDic.Keys[i];

                        foreach (var fixedFound in fixedFoundList)
                            if (fixedFound.chartValues == vc)
                            {
                                v.found = true;
                                break;
                            }
                            else
                                v.found = false;

                        for (int j = 0; j < v.list.Count; j++)
                        {
                            SetRSIAandDiff(v.list, v.list[j], j - 1);
                            if (OneChartFindCondition(v.list[j]))
                                (v.list[j] as BackTradeStick).suddenBurst = true;
                        }

                        v.list.RemoveRange(0, v.startIndex);
                        v.startIndex = 0;
                    }
            }

            return result;
        }
        void Run(DateTime start, DateTime end)
        {
            var from = start;
            var size = (int)end.AddDays(1).Subtract(start).TotalSeconds / BaseChartTimeSet.OneMinute.seconds;

            AddOrChangeLoadingText("Simulating...(" + from.ToString(DateTimeFormat) + ")", true);

            var minIndex = int.MinValue;
            var dayIndex = int.MinValue;
            foreach (var iD in itemDataDic.Values)
            {
                if (minIndex == int.MinValue)
                {
                    minIndex = iD.listDic.IndexOfKey(BaseChartTimeSet.OneMinute);
                    dayIndex = iD.listDic.IndexOfKey(BaseChartTimeSet.OneDay);
                }

                iD.Reset();
                iD.BeforeExitTime = from;

                foreach (var l in iD.listDic.Values)
                    l.Reset();
            }

            var minituesInADay = BaseChartTimeSet.OneDay.seconds / BaseChartTimeSet.OneMinute.seconds;

            var action = new Action<BackItemData, int, DateTime>((iD, i, from2) =>
            {
                if (iD.Code == "PEOPLEUSDT" && from2.ToString(TimeFormat) == "2022-01-03 03:52:00")
                    iD.Code = iD.Code;

                if (iD.firstLastMin.lastMin < from2)
                    return;

                var vm = iD.listDic.Values[minIndex];
                var vmc = iD.listDic.Keys[minIndex];
                iD.foundList = (new List <(DateTime foundTime, ChartValues chartValues)>(), new List<(DateTime foundTime, ChartValues chartValues)>());

                for (int j = minIndex; j <= dayIndex; j++)
                {
                    var v = iD.listDic.Values[j];
                    var vc = iD.listDic.Keys[j];

                    if (i % minituesInADay == 0)
                    {
                        if (v.list.Count == 0)
                        {
                            if (iD.firstLastMin.firstMin < from2.AddMinutes(minituesInADay))
                            {
                                v.list = LoadSticks(iD, vc, from2.AddSeconds(-vc.seconds * (TotalNeedDays - 1)),
                                    minituesInADay * BaseChartTimeSet.OneMinute.seconds / vc.seconds * (j - minIndex + 1) + (TotalNeedDays - 1), false);
                                v.currentIndex = GetStartIndex(v.list, from2);
                                v.startIndex = v.currentIndex;
                                v.lastStick = new BackTradeStick() { Time = v.list[v.currentIndex].Time };
                            }
                        }
                        else if (v.currentIndex == v.list.Count - 1)
                        {
                            if (iD.firstLastMin.lastMin >= from2)
                            {
                                v.list.RemoveRange(0, v.list.Count - (TotalNeedDays - 1));
                                v.currentIndex = v.list.Count - 1;
                                v.list.AddRange(LoadSticks(iD, vc, from2, minituesInADay * BaseChartTimeSet.OneMinute.seconds / vc.seconds * (j - minIndex + 1), false));
                            }
                        }
                    }

                    if (iD.firstLastMin.firstMin > from2)
                        continue;

                    if (j != minIndex)
                    {
                        var timeDiff = from2.Subtract(v.lastStick.Time).TotalSeconds;
                        if (timeDiff == vc.seconds)
                        {
                            if (!BackTradeStick.isEqual(v.lastStick as BackTradeStick, v.list[v.currentIndex] as BackTradeStick)
                                && (iD.Code != "BTCUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2019-09-24")
                                && (iD.Code != "ETHUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2019-12-11")
                                && (iD.Code != "XRPUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2020-01-16")
                                && (iD.Code != "XRPUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2020-01-17")
                                && (iD.Code != "EOSUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2020-01-19")
                                && (v.lastStick.Time.ToString(DateTimeFormat) != "2021-01-12")
                                && (v.lastStick.Time.ToString(DateTimeFormat) != "2021-05-15"))
                            {
                                BackTradeStick.isEqual(v.lastStick as BackTradeStick, v.list[v.currentIndex] as BackTradeStick);
                                ShowError(form);
                            }

                            SetRSIAandDiff(v.list, v.list[v.currentIndex], v.currentIndex - 1);

                            v.currentIndex++;

                            if (v.currentIndex == v.list.Count)
                                continue;

                            v.lastStick = new BackTradeStick() { Time = v.list[v.currentIndex].Time };
                        }
                        else if (timeDiff > vc.seconds)
                            ShowError(form);

                        if (v.lastStick.Price[1] == 0)
                        {
                            v.lastStick.Price[1] = vm.lastStick.Price[1];
                            v.lastStick.Price[2] = vm.lastStick.Price[2];
                        }

                        if (vm.lastStick.Price[0] > v.lastStick.Price[0])
                            v.lastStick.Price[0] = vm.lastStick.Price[0];
                        if (vm.lastStick.Price[1] < v.lastStick.Price[1])
                            v.lastStick.Price[1] = vm.lastStick.Price[1];
                        v.lastStick.Price[3] = vm.lastStick.Price[3];

                        v.lastStick.Ms += vm.lastStick.Ms;
                        v.lastStick.Md += vm.lastStick.Md;

                        v.lastStick.TCount += vm.lastStick.TCount;
                    }
                    else
                    {
                        if (from2 != v.lastStick.Time)
                            v.currentIndex++;
                        v.lastStick = v.list[v.currentIndex] as BackTradeStick;
                    }

                    SetRSIAandDiff(v.list, v.lastStick, v.currentIndex - 1);

                    //// st1
                    //if ((run || (toPast ? vm.lastStick.Time <= checkStartTime : vm.lastStick.Time >= checkStartTime)) &&
                    //    CheckSuddenBurst(isJoo, v.list, v.lastStick, cv, v.currentIndex - 1))
                    //{
                    //    if (v.lastStick.Price[3] > v.lastStick.Price[2])
                    //    {
                    //        if (shortFoundList.Count == 0)
                    //            shortFoundList.Add((vm.lastStick.Time, vmc));
                    //        shortFoundList.Add((v.lastStick.Time, cv));
                    //    }
                    //    else
                    //    {
                    //        if (longFoundList.Count == 0)
                    //            longFoundList.Add((vm.lastStick.Time, vmc));
                    //        longFoundList.Add((v.lastStick.Time, cv));
                    //    }
                    //}

                    // st2
                    OneChartFindConditionAndAdd(iD, vc);

                    //// st3 안 좋음
                    //if ((run || (toPast ? vm.lastStick.Time <= checkStartTime : vm.lastStick.Time >= checkStartTime)) && v.currentIndex >= 2)
                    //{
                    //    var firstCondition = CheckSuddenBurst(isJoo, v.list, v.list[v.currentIndex - 1], cv, v.currentIndex - 2);
                    //    if (firstCondition)
                    //    {
                    //        firstCondition = (v.lastStick.Price[2] == v.lastStick.Price[3]) || (v.list[v.currentIndex - 1].Price[3] > v.list[v.currentIndex - 1].Price[2] ?
                    //                (v.list[v.currentIndex - 1].Price[3] / v.list[v.currentIndex - 1].Price[2] - 1m) / (v.lastStick.Price[2] / v.lastStick.Price[3] - 1m) > 5m :
                    //                (v.list[v.currentIndex - 1].Price[2] / v.list[v.currentIndex - 1].Price[3] - 1m) / (v.lastStick.Price[3] / v.lastStick.Price[2] - 1m) > 5m);
                    //    }

                    //    if (firstCondition ||
                    //    ((v.lastStick.Price[3] > v.lastStick.Price[2] ? shortFoundList.Count : longFoundList.Count) > 0 && CheckSuddenBurst(isJoo, v.list, v.lastStick, cv, v.currentIndex - 1)))
                    //    {
                    //        if (firstCondition ? v.list[v.currentIndex - 1].Price[3] > v.list[v.currentIndex - 1].Price[2] : v.lastStick.Price[3] > v.lastStick.Price[2])
                    //        {
                    //            if (shortFoundList.Count == 0)
                    //                shortFoundList.Add((vm.lastStick.Time, vmc));
                    //            shortFoundList.Add((v.lastStick.Time, cv));
                    //        }
                    //        else
                    //        {
                    //            if (longFoundList.Count == 0)
                    //                longFoundList.Add((vm.lastStick.Time, vmc));
                    //            longFoundList.Add((v.lastStick.Time, cv));
                    //        }
                    //    }
                    //}
                }

                if (!iD.Enter)
                {
                    var conditionResult = AllChartFindCondition(iD.foundList);
                    if (conditionResult.found)
                    {
                        EnterSetting(iD, iD.listDic.Values[minIndex].lastStick, conditionResult.isLong ? iD.foundList.Long : iD.foundList.Short, conditionResult.isLong);
                        //lock (foundLocker)
                        //{
                        //    if (conditionResult.isLong)
                        //        foundItemList.Long.Add((iD, iD.foundList.Long));
                        //    else
                        //        foundItemList.Short.Add((iD, iD.foundList.Short));
                        //}
                    }

                    //if (shortFoundList.Count >= 3 || longFoundList.Count >= 3)
                    //{
                    //    var foundItemList = shortFoundList.Count >= 3 ? shortFoundList : longFoundList;
                    //    var isL = longFoundList.Count >= 3;

                    //    iD.Enter = true;
                    //    iD.EnterTime = iD.listDic.Values[minIndex].lastStick.Time;
                    //    iD.EnterPrice = iD.listDic.Values[minIndex].lastStick.Price[3];
                    //    iD.EnterFoundList = foundItemList;
                    //    iD.EnterPositionIsLong = isL;
                    //}
                }
                else
                {
                    //var last2ndFound = itemData.EnterFoundList[itemData.EnterFoundList.Count - 2];
                    //var timeTerm = (int)vm.lastStick.Time.AddSeconds(vmc.seconds).Subtract(last2ndFound.foundTime).TotalSeconds;
                    //if (timeTerm % last2ndFound.chartValues.seconds == 0)
                    //{
                    //    var size2 = timeTerm / last2ndFound.chartValues.seconds - 1;
                    //    if (size2 >= 2)
                    //    {
                    //        var v3 = itemData.listDic[last2ndFound.chartValues];
                    //        var RSIA = CalRSIA(v3.list, v3.lastStick, v3.currentIndex - 1, size2 > 4 ? 4 : size2);
                    //        if (itemData.EnterPositionIsLong ? 
                    //            (RSIA < 50 || RSIA - v3.list[v3.currentIndex - 1].RSIA < -10) : 
                    //            (RSIA > 50 || RSIA - v3.list[v3.currentIndex - 1].RSIA > 10))
                    //    }
                    //}

                    if (ExitConditionFinal(iD))
                    {
                        iD.Enter = false;
                        var resultData = new BackResultData()
                        {
                            Code = iD.Code,
                            EnterTime = iD.EnterTime,
                            ExitTime = vm.lastStick.Time,
                            ProfitRate = Math.Round((double)((iD.EnterPositionIsLong ? vm.lastStick.Price[3] / iD.EnterPrice : iD.EnterPrice / vm.lastStick.Price[3]) - 1) * 100, 2),
                            Duration = vm.lastStick.Time.Subtract(iD.EnterTime).ToString(TimeSpanFormat),
                            BeforeGap = iD.EnterTime.Subtract(iD.BeforeExitTime).ToString(TimeSpanFormat),
                            LorS = iD.EnterPositionIsLong
                        };

                        iD.BeforeExitTime = resultData.ExitTime;

                        var enterIndex = simulDays.IndexOfKey(resultData.EnterTime.Date);
                        for (int k = simulDays.IndexOfKey(resultData.ExitTime.Date); k >= 0; k--)
                        {
                            if (k < enterIndex)
                                break;

                            lock (dayLocker)
                            {
                                simulDays.Values[k].resultDatas.Add(resultData);
                            }
                        }

                        var BeforeGap = TimeSpan.ParseExact(resultData.BeforeGap, TimeSpanFormat, null);
                        if (BeforeGap < iD.ShortestBeforeGap)
                        {
                            iD.ShortestBeforeGap = BeforeGap;
                            iD.ShortestBeforeGapText = BeforeGap.ToString();
                        }
                    }
                }
            });

            for (int i = 0; i < size; i++)
            {
                var from2 = from.AddMinutes(i);
                if (!simulDays.ContainsKey(from2.Date))
                {
                    simulDays.Add(from2.Date, new DayData() { Date = from2.Date });
                    marketDays.Add(from2.Date, new DayData() { Date = from2.Date });

                    var year = from2.Year;
                    if (!openDaysPerYear.ContainsKey(year))
                        openDaysPerYear.Add(year, 0);
                    openDaysPerYear[year]++;

                    AddOrChangeLoadingText("Simulating...(" + from2.ToString(DateTimeFormat) + ")   " + sw.Elapsed.ToString(TimeSpanFormat), false);
                }

                var block = new ActionBlock<BackItemData>(iD => { action(iD, i, from2); }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 6 });

                foundItemList = (new List<(BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)>)>(), new List<(BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)>)>());

                foreach (var iD in itemDataDic.Values)
                    block.Post(iD);

                block.Complete();
                block.Completion.Wait();

                var conditionResult = AllItemFindCondition();
                if (conditionResult.found)
                {
                    var fixedFoundItemList = conditionResult.isLong ? foundItemList.Long : foundItemList.Short;
                    foreach (var foundItem in fixedFoundItemList)
                        EnterSetting(foundItem.itemData, foundItem.itemData.listDic.Values[minIndex].lastStick, foundItem.foundList, conditionResult.isLong);
                }
            }

            foreach (var iD in itemDataDic.Values)
                foreach (var l in iD.listDic.Values)
                    l.Reset();
        }

        List<TradeStick> LoadSticks(BackItemData itemData, ChartValues chartValues = default, DateTime from = default, int size = default, bool toPast = true)
        {
            if (chartValues == default)
                chartValues = mainChart.Tag as ChartValues;

            if (size == default)
                size = baseLoadSticksSize;

            var multiplier = toPast ? -1 : 1;

            var conn = DBDic[chartValues];

            if (from == default)
                from = GetFirstOrLastTime(!toPast, itemData, chartValues).time;
            var to = chartValues != BaseChartTimeSet.OneMonth ? from.AddSeconds(multiplier * chartValues.seconds * size) : from.AddMonths(multiplier * size);

            var list = new List<TradeStick>();
            var comp1 = toPast ? "<" : ">";
            var comp2 = toPast ? ">" : "<";
            //  order by 순서부터 where 조건을 이용해서 limit만큼 찾을때까지 검색하는 모양임. limiit를 못채워서 끝까지 찾는경우를 조심. *and도 속도에 영향을 주는듯
            //  index 생성후 where 조건을 하나에 몰지 않고 select 두 번으로 검색하면 index가 적용되지 않는것 같음 <- 이거아님 범위설정 잘못 했던거임 <- 이거아님 select 두번하면 index적용 안되는듯
            //var reader = new SQLiteCommand("Select *, rowid From (Select *, rowid From '" + itemData.Code + "' where " +         
            //        "date" + comp1 + "'" + fromDate + "' or date='" + fromDate + "' and time" + comp1 + "='" + fromTime + "' " +
            //        "order by rowid " + (toPast ? "desc" : "") + " limit " + size + ") where " +
            //        "date" + comp2 + "'" + toDate + "' or date='" + toDate + "' and time" + comp2 + "'" + toTime + "' " +
            //        "order by rowid", conn).ExecuteReader();
            var reader = new SQLiteCommand("Select *, rowid From '" + itemData.Code + "' where " +
                            "(time" + comp1 + "='" + from.ToString(DBTimeFormat) + "') and (time" + comp2 + "'" + to.ToString(DBTimeFormat) + "') " +
                            "order by rowid " + (toPast ? "desc" : "") + " limit " + size, conn).ExecuteReader();
            //var reader = new SQLiteCommand("Select *, rowid From (Select *, rowid From '" + itemData.Code + "' where " +
            //                "(time" + comp1 + "='" + from.ToString(DBTimeFormat) + "') " +
            //                "order by rowid " + (toPast ? "desc" : "") + " limit " + size + ") where " +
            //                "(time" + comp2 + "'" + to.ToString(DBTimeFormat) + "') " +
            //                "order by rowid", conn).ExecuteReader();
            //if (!toPast)
            {   //속도테스트
                //var spentList = new List<long>();
                //var spentList2 = new List<long>();
                //for (int i = 0; i < 20; i++)
                //{
                //    var sw = new Stopwatch();
                //    sw.Start();
                //    var reader2 = new SQLiteCommand("Select *, rowid From (Select *, rowid From '" + itemData.Code + "' where " +
                //            "(time" + comp1 + "='" + fromTimeFull + "') and (time" + comp2 + "'" + toTimeFull + "') " +
                //            "order by rowid " + (toPast ? "desc" : "") + " limit " + size + ") " +
                //            "order by rowid", conn).ExecuteReader();
                //    sw.Stop();
                //    spentList.Add(sw.ElapsedMilliseconds);
                //    var sw2 = new Stopwatch();
                //    sw2.Start();
                //    var reader3 = new SQLiteCommand("Select *, rowid From (Select *, rowid From '" + itemData.Code + "' where " +
                //            "(time" + comp1 + "='" + fromTimeFull + "') " +
                //            "order by rowid " + (toPast ? "desc" : "") + " limit " + size + ") where " +
                //            "(time" + comp2 + "'" + toTimeFull + "') " +
                //            "order by rowid", conn).ExecuteReader();
                //    sw2.Stop();
                //    spentList2.Add(sw2.ElapsedMilliseconds);
                //}
            }
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

                if (toPast)
                    list.Insert(0, stick);
                else
                    list.Add(stick);
            }
            if (smallestDiff < itemData.hoDiff)
                itemData.hoDiff = smallestDiff;

            return list;
        }
        BackTradeStick GetStickFromSQL(SQLiteDataReader reader)
        {
            return new BackTradeStick()
            {
                //Time = DateTime.ParseExact(reader["date"].ToString() + reader["time"].ToString(), DBTimeFormat, null),
                Time = DateTime.ParseExact(reader["time"].ToString(), DBTimeFormat, null),
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
        DateTime GetStandardDate(bool first = false, bool oneChart = true)
        {
            DateTime from = GetCursorTime();
            if (from == default)
            {
                var chartValues = oneChart ? default : BaseChartTimeSet.OneMinute;
                var centerViewIndex = (int)mainChart.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 2 - (baseChartViewSticksSize + 1) / 2;
                from = (mainChart.Series[0].Points.Count - 1 >= centerViewIndex && centerViewIndex >= 0)
                        ? DateTime.Parse(mainChart.Series[0].Points[centerViewIndex].AxisLabel)
                        : (mainChart.Series[0].Points.Count == 0
                            ? GetFirstOrLastTime(first, default, chartValues).time
                            : GetFirstOrLastTime(centerViewIndex < 0, showingItemData, chartValues).time);
            }
            if (!oneChart && !first)
                from = from.AddSeconds((mainChart.Tag as ChartValues).seconds - BaseChartTimeSet.OneMinute.seconds);

            return from;
        }
        (DateTime time, BackItemData itemData) GetFirstOrLastTime(bool first, BackItemData itemData = default, ChartValues chartValues = default)
        {
            if (chartValues == default)
                chartValues = mainChart.Tag as ChartValues;

            var conn = DBDic[chartValues];

            var time = first ? DateTime.MaxValue : DateTime.MinValue;
            if (itemData == default)
                foreach (var itemData2 in itemDataDic.Values)
                {
                    var reader = new SQLiteCommand("Select *, rowid From '" + itemData2.Code + "' order by rowid " + (first ? "" : "desc") + " limit 1", conn).ExecuteReader();
                    if (!reader.Read())
                        ShowError(form);
                    var stick = GetStickFromSQL(reader);
                    if (first ? stick.Time < time : stick.Time >= time)
                    {
                        time = stick.Time;
                        itemData = itemData2;
                    }
                }
            else
            {
                var reader = new SQLiteCommand("Select *, rowid From '" + itemData.Code + "' order by rowid " + (first ? "" : "desc") + " limit 1", conn).ExecuteReader();
                if (!reader.Read())
                    ShowError(form);
                var stick = GetStickFromSQL(reader);
                if (first ? stick.Time < time : stick.Time > time)
                    time = stick.Time;
            }

            return (time, itemData);
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
