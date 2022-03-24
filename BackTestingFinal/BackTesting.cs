using BrightIdeasSoftware;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TradingLibrary;
using TradingLibrary.Base;
using System.Runtime.InteropServices;

namespace BackTestingFinal
{
    public sealed class BackTesting : BaseFunctions
    {
        public static BackTesting instance;

        BackItemData showingItemData;

        SortedList<DateTime, DayData>[] simulDays = new SortedList<DateTime, DayData>[] { new SortedList<DateTime, DayData>(), new SortedList<DateTime, DayData>() }; // 0 long 1 short
        SortedList<DateTime, DayData> marketDays = new SortedList<DateTime, DayData>();
        public List<BackResultData>[] ResultDatasForMetric1 = new List<BackResultData>[] { new List<BackResultData>(), new List<BackResultData>() };
        public List<BackResultData>[] ResultDatasForMetric2 = new List<BackResultData>[] { new List<BackResultData>(), new List<BackResultData>() };
        List<(double cr, double profit, double profitWithKelly)>[] resultList1 = new List<(double cr, double profit, double profitWithKelly)>[] { new List<(double cr, double profit, double profitWithKelly)>(), new List<(double cr, double profit, double profitWithKelly)>() };
        List<(double cr, double profit, double profitWithKelly)>[] resultList2 = new List<(double cr, double profit, double profitWithKelly)>[] { new List<(double cr, double profit, double profitWithKelly)>(), new List<(double cr, double profit, double profitWithKelly)>() };

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
        string MetricCompoundAnnualGrowthRate = "CAGR";
        string MetricMDD = "MDD";
        string MetricMDDStart = "MDDStart";
        string MetricMDDLow = "MDDLow";
        string MetricMDDEnd = "MDDEnd";
        string MetricMDDDays = "MDDDays";
        string MetricDays = "    Days";
        string MetricStart = "    Start";
        string MetricLow = "    Low";
        string MetricEnd = "    End";
        string MetricWinRate = "Win Rate";
        string MetricWinRateYearStandardDeviation = "    WRYSD";
        string MetricAllCount = "    Count";
        string MetricAvgProfitRate = "    APR";
        string MetricWinAvgProfitRate = "    WAPR";
        string MetricLoseAvgProfitRate = "    LAPR";
        string MetricDayMaxHas = "Day Max Has";
        string MetricDayMaxHasDay = "    Date";
        string MetricLongestHasTime = "LH Days";
        string MetricLongestHasTimeCode = "    Code";
        string MetricLongestHasTimeStart = "    Start";
        string MetricAverageHasDays = "AVGH Days";

        string sticksDBpath;
        string sticksDBbaseName;

        string TimeSpanFormat = @"d\.hh\:mm\:ss";

        private object dayLocker = new object();
        private object foundLocker = new object();

        Stopwatch sw = new Stopwatch();

        Dictionary<ChartValues, SQLiteConnection> DBDic = new Dictionary<ChartValues, SQLiteConnection>();

        bool TestAll = MessageBox.Show("TestAll?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
        bool AlertOn = MessageBox.Show("AlertOn?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
        bool isItemLimitAverage = MessageBox.Show("isItemLimitAverage?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
        int threadN;

        static string STResultDBPath = @"C:\Users\tmdwn\source\repos\BackTestingFinal\전략결과\";
        SQLiteConnection STResultDB = new SQLiteConnection(@"Data Source=" + STResultDBPath + "strategy_result.db");

        public BackTesting(Form form, bool isJoo) : base(form, isJoo, 10.1m)
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
            toTextBox.Text = "2022-01-22";
        }
        void SetAdditionalMainView()
        {
            ClickAction += (i) =>
            {
                var chartValues = mainChart.Tag as ChartValues;
                var list = showingItemData.listDic[chartValues].list;
                var Ind3 = CalIndicator(list, list[i], i - 1, 3);
                var Ind20 = CalIndicator(list, list[i - 3], i - 4, 20);

                //var text = "   ind20-3(priceAmp:" + Math.Round(Ind20.priceAmp, 3) + ", pricePR:" + Math.Round(Ind20.pricePR, 2) + ", priceA:" + Math.Round(Ind20.priceA, 2) +
                //    ") ind3(priceAmp:" + Math.Round(Ind3.priceAmp, 3) + ", priceA:" + Math.Round(Ind3.priceA, 2) + ")";

                form.Text = showingItemData.Code + "     H:" + list[i].Price[0] + "  L:" + list[i].Price[1] + "  O:" + list[i].Price[2] + "  C:" + list[i].Price[3] + "  Ms:" + list[i].Ms + "  Md:" + list[i].Md;
            };

            clickResultAction = new Action<DayData>((date) =>
            {
                dayResultListView.ClearObjects();

                var n = 0;
                    foreach (var data in date.resultDatas)
                        if (data.EnterTime.Date == date.Date && data.ExitTime.Date <= simulDays[0].Keys[simulDays[0].Count - 1])
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


                    clickResultAction(simulDays[0][DateTime.Parse(totalChart.Series[(totalChart.Series[0].Points.Count == totalChart.Series[3].Points.Count || totalChart.Series[3].Points.Count == 0) ? 0 : 1].Points[result.Xindex].AxisLabel)]);
                }
            };

            var mainChartArea = mainChart.ChartAreas[0];
            var chartAreaCR = totalChart.ChartAreas.Add("ChartAreaCumulativeReturn");
            SetChartAreaFirst(chartAreaCR);
            chartAreaCR.Position = new ElementPosition(mainChartArea.Position.X, mainChartArea.Position.Y, mainChartArea.Position.Width, 
                (100 - mainChartArea.Position.Y - (int)mainChartArea.Tag) / 3f);
            chartAreaCR.InnerPlotPosition = new ElementPosition(mainChartArea.InnerPlotPosition.X, mainChartArea.InnerPlotPosition.Y, mainChartArea.InnerPlotPosition.Width, mainChartArea.InnerPlotPosition.Height - 7);
            chartAreaCR.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartAreaCR.AxisY.Enabled = AxisEnabled.False;
            chartAreaCR.AxisY2.LabelStyle.Format = "{0:0.00}%";
            chartAreaCR.AxisY2.IntervalAutoMode = IntervalAutoMode.FixedCount;
            //chartArea6.AxisY2.StripLines.Add(new StripLine() { IntervalOffset = 0, BackColor = ColorSet.Border, StripWidth = 0.5 });
            chartAreaCR.AxisY.Maximum = double.NaN;
            chartAreaCR.AxisY.Minimum = double.NaN;

            var chartAreaPR = totalChart.ChartAreas.Add("ChartAreaProfitRate");
            SetChartAreaFirst(chartAreaPR);
            chartAreaPR.Position = new ElementPosition(chartAreaCR.Position.X, chartAreaCR.Position.Y + chartAreaCR.Position.Height, chartAreaCR.Position.Width, chartAreaCR.Position.Height);
            chartAreaPR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaCR.InnerPlotPosition.Height);
            chartAreaPR.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartAreaPR.AxisY.Enabled = AxisEnabled.False;
            chartAreaPR.AxisY2.LabelStyle.Format = "{0:0.00}%";
            chartAreaPR.AxisY2.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartAreaPR.AxisY.Maximum = double.NaN;
            chartAreaPR.AxisY.Minimum = double.NaN;
            chartAreaPR.AlignWithChartArea = chartAreaCR.Name;

            var chartAreaHas = totalChart.ChartAreas.Add("ChartAreaHas");
            SetChartAreaLast(chartAreaHas);
            chartAreaHas.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaPR.Position.Y + chartAreaPR.Position.Height, chartAreaPR.Position.Width, 100 - (chartAreaPR.Position.Y + chartAreaPR.Position.Height));
            chartAreaHas.InnerPlotPosition = new ElementPosition(chartAreaPR.InnerPlotPosition.X, chartAreaPR.InnerPlotPosition.Y, chartAreaPR.InnerPlotPosition.Width, 
                100 - chartAreaPR.InnerPlotPosition.Y - (int)mainChartArea.Tag / chartAreaHas.Position.Height * 100f - 7);
            chartAreaHas.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartAreaHas.AxisY.Enabled = AxisEnabled.False;
            chartAreaHas.AxisY2.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartAreaHas.AxisY.Maximum = double.NaN;
            chartAreaHas.AxisY.Minimum = double.NaN;
            chartAreaHas.AlignWithChartArea = chartAreaCR.Name;

            var seriesLCR = totalChart.Series.Add("LongCR");
            seriesLCR.ChartType = SeriesChartType.Line;
            seriesLCR.XValueType = ChartValueType.Time;
            seriesLCR.Color = Color.FromArgb(200, ColorSet.PlusPrice);
            seriesLCR.YAxisType = AxisType.Secondary;
            seriesLCR.ChartArea = chartAreaCR.Name;

            var seriesLPR = totalChart.Series.Add("LongProfitRate");
            seriesLPR.ChartType = SeriesChartType.Line;
            seriesLPR.XValueType = seriesLCR.XValueType;
            seriesLPR.Color = seriesLCR.Color;
            seriesLPR.YAxisType = seriesLCR.YAxisType;
            seriesLPR.ChartArea = chartAreaPR.Name;

            var seriesLH = totalChart.Series.Add("LongHas");
            seriesLH.ChartType = SeriesChartType.Line;
            seriesLH.XValueType = seriesLCR.XValueType;
            seriesLH.Color = seriesLCR.Color;
            seriesLH.YAxisType = seriesLCR.YAxisType;
            seriesLH.ChartArea = chartAreaHas.Name;

            var seriesSCR = totalChart.Series.Add("ShortCR");
            seriesSCR.ChartType = seriesLCR.ChartType;
            seriesSCR.XValueType = seriesLCR.XValueType;
            seriesSCR.Color = Color.FromArgb(200, ColorSet.MinusPrice);
            seriesSCR.YAxisType = seriesLCR.YAxisType;
            seriesSCR.ChartArea = seriesLCR.ChartArea;

            var seriesSPR = totalChart.Series.Add("ShortProfitRate");
            seriesSPR.ChartType = seriesLPR.ChartType;
            seriesSPR.XValueType = seriesLCR.XValueType;
            seriesSPR.Color = seriesSCR.Color;
            seriesSPR.YAxisType = seriesLCR.YAxisType;
            seriesSPR.ChartArea = seriesLPR.ChartArea;

            var seriesSH = totalChart.Series.Add("ShortHas");
            seriesSH.ChartType = seriesLH.ChartType;
            seriesSH.XValueType = seriesLCR.XValueType;
            seriesSH.Color = seriesSCR.Color;
            seriesSH.YAxisType = seriesLCR.YAxisType;
            seriesSH.ChartArea = seriesLH.ChartArea;


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
            var runAction = new Action<Position>((isALS) =>
            {
                if (DateTime.TryParseExact(fromTextBox.Text, DateTimeFormat, null, System.Globalization.DateTimeStyles.None, out DateTime from) &&
                    DateTime.TryParseExact(toTextBox.Text, DateTimeFormat, null, System.Globalization.DateTimeStyles.None, out DateTime to) && from <= to)
                    Task.Run(new Action(() =>
                    {
                        var first = GetFirstOrLastTime(true, default, BaseChartTimeSet.OneDay).time;
                        if (from < first)
                        {
                            from = first;
                            form.BeginInvoke(new Action(() => { fromTextBox.Text = from.ToString(DateTimeFormat); }));
                        }
                        var last = GetFirstOrLastTime(false, default, BaseChartTimeSet.OneDay).time;
                        if (to > last)
                        {
                            to = last;
                            form.BeginInvoke(new Action(() => { toTextBox.Text = to.ToString(DateTimeFormat); }));
                        }
                        if (from > to)
                        {
                            ShowError(form, "input error");
                            return;
                        }
                        RunMain(from, to, isALS);
                    }));
                else
                    ShowError(form, "input error");
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

            SetButton(runButton, "Run", (sender, e) => { runAction(Position.All); });
            runButton.Size = new Size((GetFormWidth(form) - toTextBox.Location.X - toTextBox.Width - 10) / 2 - 2, toTextBox.Height);
            runButton.Location = new Point(toTextBox.Location.X + toTextBox.Width + 5, toTextBox.Location.Y);

            SetButton(runLongButton, "L", (sender, e) => { runAction(Position.Long); });
            runLongButton.Size = new Size(runButton.Width / 2 - 2, toTextBox.Height);
            runLongButton.Location = new Point(runButton.Location.X + runButton.Width + 4, toTextBox.Location.Y);
            //runLongButton.Font = new Font(runLongButton.Font.FontFamily, 5);
            runLongButton.BackColor = ColorSet.PlusPrice;

            SetButton(runShortButton, "S", (sender, e) => { runAction(Position.Short); });
            runShortButton.Size = new Size(runLongButton.Width, toTextBox.Height);
            runShortButton.Location = new Point(runLongButton.Location.X + runLongButton.Width + 4, toTextBox.Location.Y);
            //runShortButton.Font = new Font(runShortButton.Font.FontFamily, runShortButton.Font.Size);
            runShortButton.BackColor = ColorSet.MinusPrice;
            #endregion

            #region Metric
            SetListView(metricListView, new (string, string, int)[]
                {
                    ("Metric", "MetricName", 4),
                    ("Long", "Long", 4),
                    ("Short", "Short", 4)
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

                if (simulDays[0].Count == 0)
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
                var result = LoadAndCheckSticks(itemData, true, false, default, data.OutEnterTime == default ? data.EnterTime : data.OutEnterTime, default, false);
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
                    ("L", "isL", 2),
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
            dayResultListView.SelectionChanged += (sender, e) =>
            {
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
            foreach (var sd in simulDays)
                foreach (var day in sd)
                    foreach (var resultData in day.Value.resultDatas)
                        if (itemData.Code == resultData.Code && resultData.ExitTime.Date == day.Key && resultData.EnterTime.Date >= sd.Values[0].Date)
                        {
                            resultData.NumberForSingle = ++n;
                            codeResultListView.AddObject(resultData);
                        }
        }

        void SetDB()
        {
            foreach (var conn in DBDic.Values)
                conn.Close();
            DBDic.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();

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
            metricListView.AddObject(new MetricData());

            metricDic.Add(MetricWinRate, new MetricData() { MetricName = MetricWinRate });
            metricListView.AddObject(metricDic[MetricWinRate]);
            metricDic.Add(MetricWinRateYearStandardDeviation, new MetricData() { MetricName = MetricWinRateYearStandardDeviation });
            metricListView.AddObject(metricDic[MetricWinRateYearStandardDeviation]);
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
            metricDic.Add(MetricMDDDays, new MetricData() { MetricName = MetricDays });
            metricListView.AddObject(metricDic[MetricMDDDays]);
            metricDic.Add(MetricMDDStart, new MetricData() { MetricName = MetricStart });
            metricListView.AddObject(metricDic[MetricMDDStart]);
            metricDic.Add(MetricMDDLow, new MetricData() { MetricName = MetricLow });
            metricListView.AddObject(metricDic[MetricMDDLow]);
            metricDic.Add(MetricMDDEnd, new MetricData() { MetricName = MetricEnd });
            metricListView.AddObject(metricDic[MetricMDDEnd]);
            metricListView.AddObject(new MetricData());

            metricDic.Add(MetricDayMaxHas, new MetricData() { MetricName = MetricDayMaxHas });
            metricListView.AddObject(metricDic[MetricDayMaxHas]);
            metricDic.Add(MetricDayMaxHasDay, new MetricData() { MetricName = MetricDayMaxHasDay });
            metricListView.AddObject(metricDic[MetricDayMaxHasDay]);
            metricListView.AddObject(new MetricData());

            metricDic.Add(MetricLongestHasTime, new MetricData() { MetricName = MetricLongestHasTime });
            metricListView.AddObject(metricDic[MetricLongestHasTime]);
            metricDic.Add(MetricLongestHasTimeCode, new MetricData() { MetricName = MetricLongestHasTimeCode });
            metricListView.AddObject(metricDic[MetricLongestHasTimeCode]);
            metricDic.Add(MetricLongestHasTimeStart, new MetricData() { MetricName = MetricLongestHasTimeStart });
            metricListView.AddObject(metricDic[MetricLongestHasTimeStart]);
        }

        void RunMain(DateTime start, DateTime end, Position isAllLongShort)
        {
            form.BeginInvoke(new Action(() => { ClearChart(totalChart, true); }));

            if (simulDays[0].Count == 0 || start < simulDays[0].Keys[0] || end > simulDays[0].Keys[simulDays[0].Keys.Count - 1])
            {
                SetDB();

                LoadingSettingFirst(form);
                sw.Reset();
                sw.Start();

                ClearBeforeRun();

                try
                {
                    Run(start, end);
                }
                catch (Exception e)
                {
                    throw;
                }

                if (openDaysPerYear.Values.Count > 2)
                {
                    var firstKey = openDaysPerYear.Keys[0];
                    var lastKey = openDaysPerYear.Keys[openDaysPerYear.Values.Count - 1];
                    openDaysPerYear[firstKey] = openDaysPerYear.Values[1];
                    openDaysPerYear[lastKey] = openDaysPerYear.Values[openDaysPerYear.Values.Count - 2];
                }

                sw.Stop();
                HideLoading();
                if (AlertOn)
                    AlertStart("done : " + sw.Elapsed.ToString(TimeSpanFormat));
            }

            form.BeginInvoke(new Action(() =>
            {
                form.Text += ST + " done : " + sw.Elapsed.ToString(TimeSpanFormat);
                CalculateMetric(start, end, isAllLongShort);
                totalButton.PerformClick();
            }));
        }
        void ClearBeforeRun()
        {
            marketDays.Clear();
            foreach (var sd in simulDays)
                sd.Clear();
            openDaysPerYear.Clear();
        }

        void CalculateMetric(DateTime start, DateTime end, Position isAllLongShort)
        {
            #region First Setting
            var startIndex = marketDays.IndexOfKey(start);
            var endIndex = marketDays.IndexOfKey(end);

            metricResultListView.ClearObjects();

            var MetricVars = new MetricVar[] { new MetricVar() { isLong = true }, new MetricVar() { isLong = false } };

            var n = 0;
            #endregion

            foreach (var itemData in itemDataDic.Values)
                itemData.Reset();

            var beforeCR = new double[2]; 

            for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                if (isAllLongShort == Position.All || isAllLongShort == (Position)j)
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        MetricVars[j].HasItemsAtADay = 0;

                        simulDays[j].Values[i].Count = 0;
                        simulDays[j].Values[i].Win = 0;
                        simulDays[j].Values[i].WinProfitRateSum = 0;
                        simulDays[j].Values[i].ProfitRateSum = 0;

                        foreach (var resultData in simulDays[j].Values[i].resultDatas)
                            if (resultData.EnterTime.Date == simulDays[j].Keys[i] && resultData.ExitTime.Date <= end)
                            {
                                var hasTime = resultData.ExitTime.Subtract(resultData.EnterTime);
                                if (hasTime > MetricVars[(int)resultData.LorS].longestHasTime)
                                {
                                    MetricVars[(int)resultData.LorS].longestHasTime = hasTime;
                                    MetricVars[(int)resultData.LorS].longestHasCode = resultData.Code;
                                    MetricVars[(int)resultData.LorS].longestHasTimeStart = resultData.EnterTime;
                                }

                                var itemData = itemDataDic[resultData.Code];

                                simulDays[j].Values[i].Count++;
                                itemData.Count++;
                                simulDays[j].Values[i].ProfitRateSum += resultData.ProfitRate;
                                MetricVars[(int)resultData.LorS].HasItemsAtADay++;
                                MetricVars[(int)resultData.LorS].Count++;
                                MetricVars[(int)resultData.LorS].ProfitRateSum += resultData.ProfitRate;
                                if (resultData.ProfitRate > commisionRate)
                                {
                                    simulDays[j].Values[i].Win++;
                                    itemData.Win++;
                                    simulDays[j].Values[i].WinProfitRateSum += resultData.ProfitRate;
                                    MetricVars[(int)resultData.LorS].Win++;
                                    MetricVars[(int)resultData.LorS].ProfitWinRateSum += resultData.ProfitRate;
                                }
                            }

                        if (MetricVars[j].HasItemsAtADay > MetricVars[j].highestHasItemsAtADay)
                        {
                            MetricVars[j].highestHasItemsAtADay = MetricVars[j].HasItemsAtADay;
                            MetricVars[j].highestHasItemsDate = simulDays[j].Keys[i];
                        }

                        var Kelly = CalculateKelly(MetricVars[j].ProfitRates);
                        foreach (var resultData in simulDays[j].Values[i].ResultDatasForMetric)
                            if (resultData.EnterTime.Date == simulDays[j].Keys[i] && resultData.ExitTime.Date <= end)
                            {
                                if (beforeCR[j] != default && beforeCR[j] != MetricVars[j].CR)
                                    ShowError(form, "CR Error");

                                var avgPR = resultData.ProfitRate / resultData.Count - commisionRate;
                                MetricVars[j].CR *= 1 + avgPR * Kelly / 100;

                                beforeCR[j] = MetricVars[j].CR;

                                MetricVars[j].ProfitRates.Add(avgPR);
                                resultList1[j].Add((MetricVars[j].CR, avgPR, Kelly));
                            }

                        if (MetricVars[j].DD == default)
                        {
                            MetricVars[j].DD = new DrawDownData()
                            {
                                HighestCumulativeReturn = MetricVars[j].CR,
                                HighestCumulativeReturnIndex = i,
                                LowestCumulativeReturn = MetricVars[j].CR,
                                LowestCumulativeReturnIndex = i,
                                LastCumulativeReturnIndex = i,
                            };
                            MetricVars[j].MDD = MetricVars[j].DD;
                        }
                        else
                        {
                            if (MetricVars[j].CR <= MetricVars[j].DD.LowestCumulativeReturn && i != endIndex)
                            {
                                MetricVars[j].DD.LowestCumulativeReturn = MetricVars[j].CR;
                                MetricVars[j].DD.LowestCumulativeReturnIndex = i;
                            }
                            else if (MetricVars[j].CR > MetricVars[j].DD.HighestCumulativeReturn || i == endIndex)
                            {
                                MetricVars[j].DD.LastCumulativeReturnIndex = i;

                                if (MetricVars[j].MDD == default || MetricVars[j].DD.LowestCumulativeReturn / MetricVars[j].DD.HighestCumulativeReturn < MetricVars[j].MDD.LowestCumulativeReturn / MetricVars[j].MDD.HighestCumulativeReturn
                                    || (MetricVars[j].DD.LowestCumulativeReturn / MetricVars[j].DD.HighestCumulativeReturn == MetricVars[j].MDD.LowestCumulativeReturn / MetricVars[j].MDD.HighestCumulativeReturn &&
                                        MetricVars[j].DD.LastCumulativeReturnIndex - MetricVars[j].DD.HighestCumulativeReturnIndex > MetricVars[j].MDD.LastCumulativeReturnIndex - MetricVars[j].MDD.HighestCumulativeReturnIndex))
                                    MetricVars[j].MDD = MetricVars[j].DD;

                                MetricVars[j].DD = new DrawDownData()
                                {
                                    HighestCumulativeReturn = MetricVars[j].CR,
                                    HighestCumulativeReturnIndex = i,
                                    LowestCumulativeReturn = MetricVars[j].CR,
                                    LowestCumulativeReturnIndex = i,
                                    LastCumulativeReturnIndex = i,
                                };
                            }
                        }

                        if (simulDays[j].Values[i].Count != 0)
                        {
                            simulDays[j].Values[i].WinRate = Math.Round((double)simulDays[j].Values[i].Win / simulDays[j].Values[i].Count * 100, 2);
                            simulDays[j].Values[i].ProfitRateAvg = Math.Round(simulDays[j].Values[i].ProfitRateSum / simulDays[j].Values[i].Count, 2);
                            simulDays[j].Values[i].WinProfitRateAvg = simulDays[j].Values[i].Win == 0 ? 0 : Math.Round(simulDays[j].Values[i].WinProfitRateSum / simulDays[j].Values[i].Win, 2);
                            simulDays[j].Values[i].LoseProfitRateAvg = simulDays[j].Values[i].Count == simulDays[j].Values[i].Win ? 0 :
                                Math.Round((simulDays[j].Values[i].ProfitRateSum - simulDays[j].Values[i].WinProfitRateSum) / (simulDays[j].Values[i].Count - simulDays[j].Values[i].Win), 2);
                        }
                        else
                        {
                            simulDays[j].Values[i].WinRate = 0;
                            simulDays[j].Values[i].ProfitRateAvg = 0;
                            simulDays[j].Values[i].WinProfitRateAvg = 0;
                            simulDays[j].Values[i].LoseProfitRateAvg = 0;
                        }

                        if (simulDays[j].Values[i].Count != 0)
                        {
                            simulDays[j].Values[i].Number = ++n;
                            metricResultListView.AddObject(simulDays[j].Values[i]);
                        }

                        var axisLabel = simulDays[j].Keys[i].ToString(DateTimeFormat);
                        totalChart.Series[3 * j].Points.AddXY(axisLabel, Math.Round((MetricVars[j].CR - 1) * 100, 0));
                        totalChart.Series[3 * j + 1].Points.AddXY(axisLabel, simulDays[j].Values[i].ProfitRateAvg);
                        totalChart.Series[3 * j + 2].Points.AddXY(axisLabel, MetricVars[j].HasItemsAtADay);
                    }

            resultList2 = resultList1;
            resultList1 = new List<(double cr, double profit, double profitWithKelly)>[] { new List<(double cr, double profit, double profitWithKelly)>(), new List<(double cr, double profit, double profitWithKelly)>() };
            ResultDatasForMetric2 = ResultDatasForMetric1;
            ResultDatasForMetric1 = new List<BackResultData>[] { new List<BackResultData>(), new List<BackResultData>() };

            foreach (var itemData in itemDataDic.Values)
                itemData.WinRate = itemData.Count == 0 ? -1 : Math.Round((double)itemData.Win / itemData.Count * 100, 2);

            //metricResultListView.Sort(metricResultListView.GetColumn("PRA"), SortOrder.Ascending);
            //var n = 0;
            //foreach (DayData ob in metricResultListView.Objects)
            //    ob.Number = ++n;
            //metricResultListView.Sort(metricResultListView.GetColumn("No."), SortOrder.Descending);

            #region Print Result
            var que = new Queue<Task>();

            for (int i = (int)Position.Long; i <= (int)Position.Short; i++)
            {
                var isLong = Enum.GetName(typeof(Position), i);
                var CR = "";
                var WinRate = "";
                var AllCount = "";
                var AvgProfitRate = "";
                var WinAvgProfitRate = "";
                var LoseAvgProfitRate = "";
                var MDD = "";
                var MDDDays = "";
                var MDDStart = "";
                var MDDLow = "";
                var MDDEnd = "";
                var DayMaxHas = "";
                var DayMaxHasDay = "";
                var LongestHasTime = "";
                var LongestHasTimeCode = "";
                var LongestHasTimeStart = "";
                if (MetricVars[i].Count != 0)
                {
                    CR = Math.Round((MetricVars[i].CR - 1) * 100, 0) + "%";

                    WinRate = Math.Round((double)MetricVars[i].Win / MetricVars[i].Count * 100, 1) + "%";
                    AllCount = MetricVars[i].Count.ToString();
                    AvgProfitRate = Math.Round(MetricVars[i].ProfitRateSum / MetricVars[i].Count, 2) + "%";
                    WinAvgProfitRate = MetricVars[i].Win == 0 ? "0%" : (Math.Round(MetricVars[i].ProfitWinRateSum / MetricVars[i].Win, 2) + "%");
                    LoseAvgProfitRate = MetricVars[i].Count == MetricVars[i].Win ? "0%" : (Math.Round((MetricVars[i].ProfitRateSum - MetricVars[i].ProfitWinRateSum) / (MetricVars[i].Count - MetricVars[i].Win), 2) + "%");

                    MDD = Math.Round((MetricVars[i].MDD.LowestCumulativeReturn / MetricVars[i].MDD.HighestCumulativeReturn - 1) * 100, 0) + "%";
                    MDDDays = MetricVars[i].MDD.LastCumulativeReturnIndex - MetricVars[i].MDD.HighestCumulativeReturnIndex + " days";
                    MDDStart = simulDays[i].Keys[MetricVars[i].MDD.HighestCumulativeReturnIndex].ToString(DateTimeFormat);
                    MDDLow = simulDays[i].Keys[MetricVars[i].MDD.LastCumulativeReturnIndex].ToString(DateTimeFormat);
                    MDDEnd = simulDays[i].Keys[MetricVars[i].MDD.LowestCumulativeReturnIndex].ToString(DateTimeFormat);

                    DayMaxHas = MetricVars[i].highestHasItemsAtADay.ToString();
                    DayMaxHasDay = MetricVars[i].highestHasItemsDate.ToString(DateTimeFormat);

                    LongestHasTime = MetricVars[i].longestHasTime.ToString(TimeSpanFormat);
                    LongestHasTimeCode = MetricVars[i].longestHasCode;
                    LongestHasTimeStart = MetricVars[i].longestHasTimeStart.ToString(TimeFormat);
                }

                if (isAllLongShort == Position.All)
                    que.Enqueue(new Task(() =>
                    {
                        try
                        {
                            var columnDic = new Dictionary<string, string>();
                            var isJooName = "isJoo";
                            var isFuturesName = "isFutures";
                            var strategyName = "strategy";
                            var isItemLimitAverageName = "isItemLimitAverage";
                            var isLongName = "isLong";
                            var start_dayName = "start_day";
                            var end_dayName = "end_day";
                            var daysName = "days";
                            var Cumulative_ReturnName = "Cumulative_Return";
                            var Win_RateName = "Win_Rate";
                            var CountName = "Count";
                            var Average_Profit_RateName = "Average_Profit_Rate";
                            var Win_APRName = "Win_APR";
                            var Lose_APRName = "Lose_APR";
                            var Max_Draw_DownName = "Max_Draw_Down";
                            var MDD_DaysName = "MDD_Days";
                            var MDD_Start_DayName = "MDD_Start_Day";
                            var MDD_Low_DayName = "MDD_Low_Day";
                            var MDD_End_DayName = "MDD_End_Day";
                            var Day_Max_HasName = "Day_Max_Has";
                            var DMH_DayName = "DMH_Day";
                            var Longest_Has_TimeName = "Longest_Has_Time";
                            var LHT_CodeName = "LHT_Code";
                            var LHT_StartName = "LHT_Start";
                            var ImageName = "Image";
                            var test_timeName = "test_time";
                            var threadName = "thread";
                            var test_spend_timeName = "test_spend_time";

                            columnDic.Add(isJooName, "TEXT");
                            columnDic.Add(isFuturesName, "TEXT");
                            columnDic.Add(strategyName, "INTEGER");
                            columnDic.Add(isItemLimitAverageName, "TEXT");
                            columnDic.Add(isLongName, "TEXT");
                            columnDic.Add(start_dayName, "TEXT");
                            columnDic.Add(end_dayName, "TEXT");
                            columnDic.Add(daysName, "TEXT");
                            columnDic.Add(Cumulative_ReturnName, "TEXT");
                            columnDic.Add(Win_RateName, "TEXT");
                            columnDic.Add(CountName, "INTEGER");
                            columnDic.Add(Average_Profit_RateName, "TEXT");
                            columnDic.Add(Win_APRName, "TEXT");
                            columnDic.Add(Lose_APRName, "TEXT");
                            columnDic.Add(Max_Draw_DownName, "TEXT");
                            columnDic.Add(MDD_DaysName, "TEXT");
                            columnDic.Add(MDD_Start_DayName, "TEXT");
                            columnDic.Add(MDD_Low_DayName, "TEXT");
                            columnDic.Add(MDD_End_DayName, "TEXT");
                            columnDic.Add(Day_Max_HasName, "INTEGER");
                            columnDic.Add(DMH_DayName, "TEXT");
                            columnDic.Add(Longest_Has_TimeName, "TEXT");
                            columnDic.Add(LHT_CodeName, "TEXT");
                            columnDic.Add(LHT_StartName, "TEXT");
                            columnDic.Add(ImageName, "BLOB");
                            columnDic.Add(test_timeName, "TEXT");
                            columnDic.Add(threadName, "INTEGER");
                            columnDic.Add(test_spend_timeName, "TEXT");

                            var statement = "";

                            var count = 0;
                            foreach (var column in columnDic)
                            {
                                if (count == 0)
                                    statement = "CREATE TABLE IF NOT EXISTS 'result' (";
                                else
                                    statement += ", ";

                                statement += "'" + column.Key + "' " + column.Value;

                                if (count == columnDic.Count - 1)
                                    statement += ")";

                                count++;
                            }

                            new SQLiteCommand(statement, STResultDB).ExecuteNonQuery();

                            columnDic[isJooName] = "'" + isJoo.ToString() + "'";
                            columnDic[isFuturesName] = "'" + isFutures.ToString() + "'";
                            columnDic[strategyName] = "'" + ST.ToString() + "'";
                            columnDic[isItemLimitAverageName] = "'" + isItemLimitAverage.ToString() + "'";
                            columnDic[isLongName] = "'" + isLong.ToString() + "'";
                            columnDic[start_dayName] = "'" + start.ToString(DateTimeFormat) + "'";
                            columnDic[end_dayName] = "'" + end.ToString(DateTimeFormat) + "'";
                            columnDic[daysName] = "'" + Math.Round(end.Subtract(start).TotalDays, 0) + " days'";
                            columnDic[Cumulative_ReturnName] = "'" + CR + "'";
                            columnDic[Win_RateName] = "'" + WinRate + "'";
                            columnDic[CountName] = "'" + AllCount + "'";
                            columnDic[Average_Profit_RateName] = "'" + AvgProfitRate + "'";
                            columnDic[Win_APRName] = "'" + WinAvgProfitRate + "'";
                            columnDic[Lose_APRName] = "'" + LoseAvgProfitRate + "'";
                            columnDic[Max_Draw_DownName] = "'" + MDD + "'";
                            columnDic[MDD_DaysName] = "'" + MDDDays + "'";
                            columnDic[MDD_Start_DayName] = "'" + MDDStart + "'";
                            columnDic[MDD_Low_DayName] = "'" + MDDLow + "'";
                            columnDic[MDD_End_DayName] = "'" + MDDEnd + "'";
                            columnDic[Day_Max_HasName] = "'" + DayMaxHas + "'";
                            columnDic[DMH_DayName] = "'" + DayMaxHasDay + "'";
                            columnDic[Longest_Has_TimeName] = "'" + LongestHasTime + "'";
                            columnDic[LHT_CodeName] = "'" + LongestHasTimeCode + "'";
                            columnDic[LHT_StartName] = "'" + LongestHasTimeStart + "'";
                            columnDic[ImageName] = "@image";
                            columnDic[test_timeName] = "'" + DateTime.Now.ToString(TimeFormat) + "'";
                            columnDic[threadName] = "'" + threadN.ToString() + "'";
                            columnDic[test_spend_timeName] = "'" + sw.Elapsed.ToString(TimeSpanFormat) + "'";

                            ////Total Screen Capture
                            var image = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                                Screen.PrimaryScreen.Bounds.Height);
                            using (Graphics g = Graphics.FromImage(image))
                            {
                                g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                                    Screen.PrimaryScreen.Bounds.Y,
                                                    0, 0,
                                                    image.Size,
                                                    CopyPixelOperation.SourceCopy);
                            }
                            var bytes = (byte[])new ImageConverter().ConvertTo(image, typeof(byte[]));

                            var columnDic2 = new Dictionary<string, string>();
                            columnDic2.Add(isJooName, columnDic[isJooName]);
                            columnDic2.Add(isFuturesName, columnDic[isFuturesName]);
                            columnDic2.Add(strategyName, columnDic[strategyName]);
                            columnDic2.Add(isItemLimitAverageName, columnDic[isItemLimitAverageName]);
                            columnDic2.Add(isLongName, columnDic[isLongName]);
                            columnDic2.Add(start_dayName, columnDic[start_dayName]);
                            columnDic2.Add(end_dayName, columnDic[end_dayName]);
                            columnDic2.Add(threadName, columnDic[threadName]);

                            count = 0;
                            foreach (var column in columnDic2)
                            {
                                if (count == 0)
                                    statement = "SELECT *, rowid FROM 'result' where ";
                                else
                                    statement += " and ";

                                statement += column.Key + "=" + column.Value;

                                count++;
                            }

                            var reader = new SQLiteCommand(statement, STResultDB).ExecuteReader();
                            if (reader.Read())
                            {
                                count = 0;
                                foreach (var column in columnDic)
                                {
                                    if (count == 0)
                                        statement = "update 'result' set ";
                                    else
                                        statement += ", ";

                                    statement += "'" + column.Key + "'=" + column.Value;

                                    if (count == columnDic.Count - 1)
                                        statement += " where rowid='" + reader["rowid"] + "'";

                                    count++;
                                }
                            }
                            else
                            {
                                count = 0;
                                foreach (var column in columnDic)
                                {
                                    if (count == 0)
                                        statement = "INSERT INTO 'result' (";
                                    else
                                        statement += ", ";

                                    statement += "'" + column.Key + "'";

                                    count++;
                                }
                                count = 0;
                                foreach (var column in columnDic)
                                {
                                    if (count == 0)
                                        statement += ") values (";
                                    else
                                        statement += ", ";

                                    statement += column.Value;

                                    if (count == columnDic.Count - 1)
                                        statement += ")";

                                    count++;
                                }
                            }

                            var command = new SQLiteCommand(statement, STResultDB);
                            command.Parameters.AddWithValue("@image", bytes);
                            command.ExecuteNonQuery();

                            count = 0;
                            foreach (var column in columnDic2)
                            {
                                if (count == 0)
                                    statement = "";
                                else
                                    statement += ", ";

                                statement += column.Key + "=" + column.Value;

                                count++;
                            }

                            image.Save(STResultDBPath + statement + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                        catch (Exception e)
                        {
                            throw;
                        }
                    }));

                metricDic[MetricCR].SetText(i, CR);

                metricDic[MetricWinRate].SetText(i, WinRate);
                metricDic[MetricAllCount].SetText(i, AllCount);
                metricDic[MetricAvgProfitRate].SetText(i, AvgProfitRate);
                metricDic[MetricWinAvgProfitRate].SetText(i, WinAvgProfitRate);
                metricDic[MetricLoseAvgProfitRate].SetText(i, LoseAvgProfitRate);

                metricDic[MetricMDD].SetText(i, MDD);
                metricDic[MetricMDDDays].SetText(i, MDDDays);
                metricDic[MetricMDDStart].SetText(i, MDDStart);
                metricDic[MetricMDDLow].SetText(i, MDDLow);
                metricDic[MetricMDDEnd].SetText(i, MDDEnd);

                metricDic[MetricDayMaxHas].SetText(i, DayMaxHas);
                metricDic[MetricDayMaxHasDay].SetText(i, DayMaxHasDay);

                metricDic[MetricLongestHasTime].SetText(i, LongestHasTime);
                metricDic[MetricLongestHasTimeCode].SetText(i, LongestHasTimeCode);
                metricDic[MetricLongestHasTimeStart].SetText(i, LongestHasTimeStart);
            }

            Task.Run(new Action(() => {
                //form.Cursor = new System.Windows.Forms.Cursor(System.Windows.Forms.Cursor.Current.Handle);
                //var x = System.Windows.Forms.Cursor.Position.X;
                //var y = System.Windows.Forms.Cursor.Position.Y;
                //System.Windows.Forms.Cursor.Position = new Point(GetFormWidth(form) / 2, GetFormHeight(form));
                ////System.Windows.Forms.Cursor.Clip = new Rectangle(this.Location, this.Size);
                var active = ApplicationIsActivated();

                var proc = Process.GetCurrentProcess();
                var current = GetPlacement(proc.MainWindowHandle);
                if (!active || current.showCmd == ShowWindowCommands.Minimized)
                {
                    SetForegroundWindow(proc.MainWindowHandle);
                    ShowWindow(proc.MainWindowHandle, SW_MAXIMIZE);
                }

                Thread.Sleep(1000);

                STResultDB.Open();
                new SQLiteCommand("Begin", STResultDB).ExecuteNonQuery();

                while (que.Count != 0)
                    que.Dequeue().RunSynchronously();

                new SQLiteCommand("Commit", STResultDB).ExecuteNonQuery();
                STResultDB.Close();

                if (!active || current.showCmd == ShowWindowCommands.Minimized)
                    ShowWindow(proc.MainWindowHandle, SW_MINIMIZE);
                //System.Windows.Forms.Cursor.Position = new Point(x, y);

                //if (forTesting)
                //{
                //    forTesting = false;
                //    SetST(15);
                //    ClearBeforeRun();
                //    try
                //    {
                //        RunMain(start, end, Position.All);
                //    }
                //    catch (Exception)
                //    {
                //        throw;
                //    }
                //}
                //else
                {
                    if (TestAll && ST < STList.Keys[STList.Count - 1])
                    {
                        Thread.Sleep(180000);

                        while (!STList.ContainsKey(ST + 1))
                            ST++;
                        SetST(ST + 1);
                        ClearBeforeRun();
                        RunMain(start, end, Position.All);
                    }
                    else
                        form.BeginInvoke(new Action(() => { form.Text += "done"; }));
                }
            }));
            #endregion
        }
        double CalculateKelly(List<double> list)
        {
            if (list.Count <= 1)
                return 0.5;

            var beforeKelly = 1.01D;
            var beforeGeoMean = double.MinValue;
            var beforeKelly2 = 1.02D;
            var beforeGeoMean2 = double.MinValue;
            var kelly = 1D;
            var geoMean = 1D;
            while (true)
            {
                for (int i = 0; i < list.Count; i++)
                    geoMean *= 1 + list[i] * kelly / 100;

                if (geoMean > beforeGeoMean)
                {
                    if (kelly <= 0.5)
                        return 0.5;

                    beforeKelly2 = beforeKelly;
                    beforeGeoMean2 = beforeGeoMean;
                    beforeKelly = kelly;
                    beforeGeoMean = geoMean;
                    kelly = beforeKelly - (beforeKelly2 - beforeKelly);
                    geoMean = 1D;
                }
                else
                    return beforeKelly / 2;
            }
        }

        /// <summary>Returns true if the current application has focus, false otherwise</summary>
        public static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        private static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(
            IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
        }

        internal enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);
        
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;

        [DllImport("user32.dll")]
        private static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

        void FindSimulAndShow(bool toPast, bool oneChart = true)
        {
            Task.Run(new Action(() =>
            {
                LoadingSettingFirst(form);
                sw.Reset();
                sw.Start();

                var m = toPast ? -1 : 1;

                BackItemData itemData = default;
                bool all = true;
                form.Invoke(new Action(() =>
                {
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

                    form.BeginInvoke(new Action(() =>
                    {
                        ClearMainChartAndSet(result.chartValues, itemData);
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

            ClearMainChartAndSet(chartValues, showingItemData);

            if (showingItemData == default)
                return;

            var firstTime = GetFirstOrLastTime(true, showingItemData).time;
            if (from < firstTime)
                from = firstTime;

            var list = showingItemData.listDic[mainChart.Tag as ChartValues].list;
            ShowChart(showingItemData, (from, (int)position, true), list.Count != 0 && from >= list[0].Time && from <= list[list.Count - 1].Time);
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

                    resultData = (v.list[i] as BackTradeStick).resultData2;
                    if (resultData != default)
                        exitIndex = i;
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
            ClearMainChartAndSet(chartValues, itemData);

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
                        BackColor = ColorSet.SimulLosing,
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
                        BackColor = ColorSet.SimulWinning,
                        ForeColor = ColorSet.FormText,
                        IntervalOffset = index - width + 1,
                        StripWidth = width,
                        TextLineAlignment = StringAlignment.Near,
                        TextAlignment = StringAlignment.Center,
                        TextOrientation = TextOrientation.Horizontal,
                        Text = ca.Name == PriceChartAreaName ? "\n2" : ""
                    });
            }
            if ((v.list[index] as BackTradeStick).resultData2 != default)
            {
                var resultData = (v.list[index] as BackTradeStick).resultData2;
                var EnterTime = resultData.EnterTime.AddSeconds(-(int)(resultData.EnterTime.TimeOfDay.TotalSeconds % vc.seconds));
                var ExitTime = resultData.ExitTime.AddSeconds(-(int)(resultData.ExitTime.TimeOfDay.TotalSeconds % vc.seconds));
                var width = (int)(ExitTime.Subtract(EnterTime).TotalSeconds / vc.seconds);
                if (v.list[index].Time != ExitTime || v.list[index - width].Time != EnterTime)
                    ShowError(form);

                foreach (var ca in mainChart.ChartAreas)
                    ca.AxisX.StripLines.Add(new StripLine()
                    {
                        BackColor = ColorSet.SimulInsideWinning,
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
                    if (SuddenBurst(list[i]).found)
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
                    {
                        p.Value.found = false;
                        p.Value.found2 = false;
                    }
            }
            else
            {
                if (newLoad)
                {
                    itemData.Reset();
                    foreach (var l in itemData.listDic.Values)
                        l.Reset();
                }

                var checkStartTime = from;
                from = toPast ? from.Date.AddDays(1).AddMinutes(-1) : from.Date;

                var minituesInADay = BaseChartTimeSet.OneDay.seconds / BaseChartTimeSet.OneMinute.seconds;

                if (size == default || size % minituesInADay != 0)
                    size = (size / minituesInADay + 1) * minituesInADay;

                var toM = from.AddSeconds(multiplier * BaseChartTimeSet.OneMinute.seconds * (size - 1));

                if (toPast ? from < itemData.firstLastMin.firstMin || toM > itemData.firstLastMin.lastMin : toM < itemData.firstLastMin.firstMin || from > itemData.firstLastMin.lastMin)
                    return result;

                var vm = itemData.listDic[BaseChartTimeSet.OneMinute];
                var vmc = BaseChartTimeSet.OneMinute;
                for (int i = minCV.index; i <= maxCV.index; i++)
                {
                    var v = itemData.listDic.Values[i];
                    var vc = itemData.listDic.Keys[i];
                    var from2 = toPast ? from : from.AddSeconds(-vc.seconds * (TotalNeedDays - 1));
                    if (i == minCV.index)
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
                            var size2 = minituesInADay * BaseChartTimeSet.OneMinute.seconds / vc.seconds * (i - minCV.index + 1) + (TotalNeedDays - 1);
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
                List<(DateTime foundTime, ChartValues chartValues)> fixedFoundList2 = default;
                for (vm.currentIndex = vm.startIndex; vm.currentIndex < vm.list.Count; vm.currentIndex++)
                {
                    vm.lastStick = vm.list[vm.currentIndex] as BackTradeStick;
                    for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                    {
                        itemData.positionData[j].foundList = new List<(DateTime foundTime, ChartValues chartValues)>();
                        itemData.positionData[j].found = false;
                    }

                    for (int j = minCV.index; j <= maxCV.index; j++)
                    {
                        var v = itemData.listDic.Values[j];
                        var vc = itemData.listDic.Keys[j];
                        if (j != minCV.index)
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

                        if (!calOnlyFullStick)
                            SetRSIAandDiff(v.list, v.lastStick, v.currentIndex - 1);

                        if (toPast ? vm.lastStick.Time <= checkStartTime : vm.lastStick.Time >= checkStartTime)
                            OneChartFindConditionAndAdd(itemData, vc, v.currentIndex - 1);
                    }

                    for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                    {
                        var positionData = itemData.positionData[j];
                        if (!itemData.positionData[(int)Position.Long].Enter && !itemData.positionData[(int)Position.Short].Enter)
                        {
                            if (positionData.found && (toPast || result.foundTime == DateTime.MinValue))
                            {
                                EnterSetting(positionData, vm.lastStick);

                                if (inside)
                                    InsideFirstSetting(itemData, (Position)j);
                            }
                        }
                        else if (positionData.Enter && ExitConditionFinal(itemData, (Position)j))
                        {
                            positionData.Enter = false;

                            if (!inside)
                                result = positionData.EnterFoundList[1];

                            fixedFoundList = positionData.EnterFoundList;

                            var resultData = new BackResultData()
                            {
                                Code = itemData.Code,
                                EnterTime = positionData.EnterTime,
                                ExitTime = vm.lastStick.Time,
                                ProfitRate = Math.Round((double)(((Position)j == Position.Long ? vm.lastStick.Price[3] / positionData.EnterPrice : positionData.EnterPrice / vm.lastStick.Price[3]) - 1) * 100, 2),
                                Duration = vm.lastStick.Time.Subtract(positionData.EnterTime).ToString(TimeSpanFormat),
                                LorS = (Position)j
                            };

                            foreach (var cl in itemData.listDic)
                            {
                                if (cl.Key.index > positionData.EnterFoundList.Last().chartValues.index)
                                    break;

                                var v2 = itemData.listDic[cl.Key];
                                (v2.list[v2.currentIndex] as BackTradeStick).resultData = resultData;
                            }
                        }

                        if (inside)
                        {
                            var positionData2 = itemData.positionData2[j];
                            if (!positionData2.Enter)
                            {
                                if (positionData.Enter && InsideEnterCondition(itemData, (Position)j))
                                {
                                    EnterSetting(positionData2, vm.lastStick);
                                    positionData2.OutEnterTime = positionData.EnterTime;
                                }
                            }
                            else if (InsideExitCondition(itemData, (Position)j))
                            {
                                positionData2.Enter = false;

                                var resultData = new BackResultData()
                                {
                                    Code = itemData.Code,
                                    OutEnterTime = positionData2.OutEnterTime,
                                    EnterTime = positionData2.EnterTime,
                                    ExitTime = vm.lastStick.Time,
                                    ProfitRate = Math.Round((double)(((Position)j == Position.Long ? vm.lastStick.Price[3] / positionData2.EnterPrice : positionData2.EnterPrice / vm.lastStick.Price[3]) - 1) * 100, 2),
                                    Duration = vm.lastStick.Time.Subtract(positionData2.EnterTime).ToString(TimeSpanFormat),
                                    LorS = (Position)j
                                };

                                if (toPast || result.foundTime == DateTime.MinValue)
                                {
                                    result = positionData2.EnterFoundForExit;
                                    fixedFoundList2 = new List<(DateTime foundTime, ChartValues chartValues)>() { positionData2.EnterFoundForExit };
                                }

                                foreach (var cl in itemData.listDic)
                                {
                                    if (cl.Key.index > positionData2.EnterFoundForExit.chartValues.index)
                                        break;

                                    var v2 = itemData.listDic[cl.Key];
                                    (v2.list[v2.currentIndex] as BackTradeStick).resultData2 = resultData;
                                }
                            }
                        }
                    }

                    if (vm.currentIndex == vm.list.Count - 1 && 
                        (itemData.positionData[(int)Position.Long].Enter || itemData.positionData[(int)Position.Short].Enter || itemData.positionData2[(int)Position.Long].Enter || itemData.positionData2[(int)Position.Short].Enter) && 
                        vm.lastStick.Time.AddMinutes(1) == vm.lastStick.Time.Date.AddDays(1))
                    {
                        var to = vm.lastStick.Time.Date.AddDays(2);
                        for (int i = minCV.index; i <= maxCV.index; i++)
                        {
                            var v = itemData.listDic.Values[i];
                            var vc = itemData.listDic.Keys[i];
                            var from2 = v.list[v.list.Count - 1].Time.AddSeconds(vc.seconds);
                            if (from2 < to)
                                v.list.AddRange(LoadSticks(itemData, vc, from2, minituesInADay / (vc.seconds / vmc.seconds), false));
                        }
                    }
                }

                if (result.foundTime != DateTime.MinValue &&
                    !itemData.positionData[(int)Position.Long].Enter && !itemData.positionData[(int)Position.Short].Enter && 
                    !itemData.positionData2[(int)Position.Long].Enter && !itemData.positionData2[(int)Position.Short].Enter)
                {
                    for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                        itemData.positionData[(int)Position.Long].Enter = false;

                    for (int i = minCV.index; i <= maxCV.index; i++)
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

                        if (inside)
                            foreach (var fixedFound in fixedFoundList2)
                                if (fixedFound.chartValues == vc)
                                    v.found2 = true;
                                else
                                    v.found2 = false;

                        for (int j = 0; j < v.list.Count; j++)
                        {
                            SetRSIAandDiff(v.list, v.list[j], j - 1);
                            if (SuddenBurst(v.list[j]).found)
                                (v.list[j] as BackTradeStick).suddenBurst = true;
                        }

                        v.list.RemoveRange(0, v.startIndex);
                        v.startIndex = 0;
                    }
                }
            }

            return result;
        }
        void Run(DateTime start, DateTime end)
        {
            if (TestAll)
                threadN = 3;
            else
                threadN = 6;

            var from = start;
            var size = (int)end.AddDays(1).Subtract(start).TotalSeconds / BaseChartTimeSet.OneMinute.seconds;

            AddOrChangeLoadingText("Simulating ST" + ST + " ...(" + from.ToString(DateTimeFormat) + ")", true);

            foreach (var itemData in itemDataDic.Values)
            {
                itemData.Reset();
                itemData.BeforeExitTime = from;

                foreach (var l in itemData.listDic.Values)
                    l.Reset();
            }

            var minituesInADay = BaseChartTimeSet.OneDay.seconds / BaseChartTimeSet.OneMinute.seconds;

            var action = new Action<BackItemData, int, DateTime>((itemData, i, from2) =>
            {
                if (itemData.firstLastMin.lastMin < from2)
                    return;

                var vm = itemData.listDic[BaseChartTimeSet.OneMinute];
                var vmc = BaseChartTimeSet.OneMinute;
                for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                {
                    itemData.positionData[j].foundList = new List<(DateTime foundTime, ChartValues chartValues)>();
                    itemData.positionData[j].found = false;
                }

                for (int j = minCV.index; j <= maxCV.index; j++)
                {
                    try
                    {
                        var v = itemData.listDic.Values[j];
                        var vc = itemData.listDic.Keys[j];

                        if (i % minituesInADay == 0)
                        {
                            if (v.list.Count == 0)
                            {
                                if (itemData.firstLastMin.firstMin < from2.AddMinutes(minituesInADay))
                                {
                                    v.list = LoadSticks(itemData, vc, from2.AddSeconds(-vc.seconds * (TotalNeedDays - 1)),
                                        minituesInADay * BaseChartTimeSet.OneMinute.seconds / vc.seconds * (j - minCV.index + 1) + (TotalNeedDays - 1), false);
                                    v.currentIndex = GetStartIndex(v.list, from2);
                                    v.startIndex = v.currentIndex;
                                    v.lastStick = new BackTradeStick() { Time = v.list[v.currentIndex].Time };


                                }
                            }
                            else if (v.currentIndex == v.list.Count - 1)
                            {
                                if (itemData.firstLastMin.lastMin >= from2)
                                {
                                    if (v.list.Count - (TotalNeedDays - 1) > 0)
                                        v.list.RemoveRange(0, v.list.Count - (TotalNeedDays - 1));
                                    v.currentIndex = v.list.Count - 1;
                                    v.list.AddRange(LoadSticks(itemData, vc, from2, minituesInADay * BaseChartTimeSet.OneMinute.seconds / vc.seconds * (j - minCV.index + 1), false));
                                }
                            }
                        }

                        if (itemData.firstLastMin.firstMin > from2)
                            continue;

                        if (j != minCV.index)
                        {
                            var timeDiff = from2.Subtract(v.lastStick.Time).TotalSeconds;
                            if (timeDiff == vc.seconds)
                            {
                                if (!BackTradeStick.isEqual(v.lastStick as BackTradeStick, v.list[v.currentIndex] as BackTradeStick)
                                    && (itemData.Code != "BTCUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2019-09-24")
                                    && (itemData.Code != "ETHUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2019-12-11")
                                    && (itemData.Code != "XRPUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2020-01-16")
                                    && (itemData.Code != "XRPUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2020-01-17")
                                    && (itemData.Code != "EOSUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2020-01-19")
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

                        if (!calOnlyFullStick)
                            SetRSIAandDiff(v.list, v.lastStick, v.currentIndex - 1);

                        OneChartFindConditionAndAdd(itemData, vc, v.currentIndex - 1);
                    }
                    catch (Exception e)
                    {
                        ShowError(form, e.Message);
                        throw;
                    }
                }

                for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                {
                    var positionData = itemData.positionData[j];
                    //if (!itemData.positionData[j].Enter && positionData.found)
                    //if (positionData.found)
                    //    lock (foundLocker)
                    //        foundItemList[j].Add((itemData, positionData.foundList));
                    if (!itemData.positionData[(int)Position.Long].Enter && !itemData.positionData[(int)Position.Short].Enter && positionData.found)
                        lock (foundLocker)
                            foundItemList[j].Add((itemData, positionData.foundList));
                    else
                    if (positionData.Enter && ExitConditionFinal(itemData, (Position)j))
                    {
                        positionData.Enter = false;

                        if (!inside)
                        {
                            var resultData = new BackResultData()
                            {
                                Code = itemData.Code,
                                EnterTime = positionData.EnterTime,
                                ExitTime = vm.lastStick.Time,
                                ProfitRate = Math.Round((double)(((Position)j == Position.Long ? vm.lastStick.Price[3] / positionData.EnterPrice : positionData.EnterPrice / vm.lastStick.Price[3]) - 1) * 100, 2),
                                Duration = vm.lastStick.Time.Subtract(positionData.EnterTime).ToString(TimeSpanFormat),
                                BeforeGap = positionData.EnterTime.Subtract(itemData.BeforeExitTime).ToString(TimeSpanFormat),
                                LorS = (Position)j
                            };

                            if (itemData.resultDataForMetric[j] != null)
                            {
                                if (itemData.resultDataForMetric[j].Code == itemData.Code)
                                    itemData.resultDataForMetric[j].ExitTime = resultData.ExitTime;
                                lock (itemData.resultDataForMetric[j].locker)
                                {
                                    itemData.resultDataForMetric[j].ProfitRate += resultData.ProfitRate;
                                    itemData.resultDataForMetric[j].listForAvg.Add(resultData);
                                }
                                itemData.resultDataForMetric[j] = null;
                            }

                            itemData.BeforeExitTime = resultData.ExitTime;

                            var enterIndex = simulDays[0].IndexOfKey(resultData.EnterTime.Date);
                            for (int k = simulDays[0].IndexOfKey(resultData.ExitTime.Date); k >= 0; k--)
                            {
                                if (k < enterIndex)
                                    break;

                                lock (dayLocker)
                                {
                                    simulDays[j].Values[k].resultDatas.Add(resultData);
                                }
                            }

                            var BeforeGap = TimeSpan.ParseExact(resultData.BeforeGap, TimeSpanFormat, null);
                            if (BeforeGap < itemData.ShortestBeforeGap)
                            {
                                itemData.ShortestBeforeGap = BeforeGap;
                                itemData.ShortestBeforeGapText = BeforeGap.ToString();
                            }
                        }
                    }

                    if (inside)
                    {
                        var positionData2 = itemData.positionData2[j];
                        if (!positionData2.Enter)
                        {
                            if (positionData.Enter && InsideEnterCondition(itemData, (Position)j))
                            {
                                EnterSetting(positionData2, vm.lastStick);
                                positionData2.OutEnterTime = positionData.EnterTime;
                            }
                        }
                        else if (InsideExitCondition(itemData, (Position)j))
                        {
                            positionData2.Enter = false;

                            var resultData = new BackResultData()
                            {
                                Code = itemData.Code,
                                OutEnterTime = positionData2.OutEnterTime,
                                EnterTime = positionData2.EnterTime,
                                ExitTime = vm.lastStick.Time,
                                ProfitRate = Math.Round((double)(((Position)j == Position.Long ? vm.lastStick.Price[3] / positionData2.EnterPrice : positionData2.EnterPrice / vm.lastStick.Price[3]) - 1) * 100, 2),
                                Duration = vm.lastStick.Time.Subtract(positionData2.EnterTime).ToString(TimeSpanFormat),
                                BeforeGap = positionData2.EnterTime.Subtract(itemData.BeforeExitTime).ToString(TimeSpanFormat),
                                LorS = (Position)j
                            };

                            if (itemData.resultDataForMetric[j] != null)
                            {
                                if (itemData.resultDataForMetric[j].Code == itemData.Code)
                                    itemData.resultDataForMetric[j].ExitTime = resultData.ExitTime;
                                lock (itemData.resultDataForMetric[j].locker)
                                {
                                    itemData.resultDataForMetric[j].ProfitRate += resultData.ProfitRate;
                                    itemData.resultDataForMetric[j].listForAvg.Add(resultData);
                                }
                                itemData.resultDataForMetric[j] = null;
                            }

                            itemData.BeforeExitTime = resultData.ExitTime;

                            var enterIndex = simulDays[0].IndexOfKey(resultData.EnterTime.Date);
                            for (int k = simulDays[0].IndexOfKey(resultData.ExitTime.Date); k >= 0; k--)
                            {
                                if (k < enterIndex)
                                    break;

                                lock (dayLocker)
                                {
                                    simulDays[j].Values[k].resultDatas.Add(resultData);
                                }
                            }

                            var BeforeGap = TimeSpan.ParseExact(resultData.BeforeGap, TimeSpanFormat, null);
                            if (BeforeGap < itemData.ShortestBeforeGap)
                            {
                                itemData.ShortestBeforeGap = BeforeGap;
                                itemData.ShortestBeforeGapText = BeforeGap.ToString();
                            }
                        }
                    }
                }
            });

            for (int i = 0; i < size; i++)
            {
                var from2 = from.AddMinutes(i);
                if (!simulDays[0].ContainsKey(from2.Date))
                {
                    for (int j = 0; j < 2; j++)
                        simulDays[j].Add(from2.Date, new DayData() { Date = from2.Date, isL = j });
                    marketDays.Add(from2.Date, new DayData() { Date = from2.Date });

                    var year = from2.Year;
                    if (!openDaysPerYear.ContainsKey(year))
                        openDaysPerYear.Add(year, 0);
                    openDaysPerYear[year]++;

                    AddOrChangeLoadingText("Simulating ST" + ST + " ...(" + from2.ToString(DateTimeFormat) + ")   " + sw.Elapsed.ToString(TimeSpanFormat), false);
                }

                var block = new ActionBlock<BackItemData>(iD => { action(iD, i, from2); }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = threadN });

                foundItemList = new List<(BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)>)>[] 
                    { new List<(BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)>)>(), 
                        new List<(BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)>)>() };

                foreach (var iD in itemDataDic.Values)
                    block.Post(iD);
                try
                {
                    block.Complete();
                    block.Completion.Wait();
                }
                catch (Exception)
                {

                    throw;
                }

                var conditionResult = AllItemFindCondition();
                if (conditionResult.found)
                    for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                    {
                        var simulDay = simulDays[j][from2.Date];
                        if (conditionResult.position[j])
                            foreach (var foundItem in foundItemList[j])
                            //if (!foundItem.itemData.positionData[(int)Position.Long].Enter && !foundItem.itemData.positionData[(int)Position.Short].Enter)
                            //if (!foundItem.itemData.positionData[j].Enter)
                            {
                                var minV = foundItem.itemData.listDic.Values[minCV.index];
                                EnterSetting(foundItem.itemData.positionData[j], minV.lastStick);

                                if (inside)
                                {
                                    InsideFirstSetting(foundItem.itemData, (Position)j);
                                    if (InsideEnterCondition(foundItem.itemData, (Position)j))
                                    {
                                        EnterSetting(foundItem.itemData.positionData2[j], minV.lastStick);
                                        foundItem.itemData.positionData2[j].OutEnterTime = foundItem.itemData.positionData[j].EnterTime;
                                    }
                                }

                                if (simulDay.ResultDatasForMetric.Count == 0 || simulDay.ResultDatasForMetric[simulDay.ResultDatasForMetric.Count - 1].ExitTime != default)
                                {
                                    var backResultData = new BackResultData() { EnterTime = from2, Code = foundItem.itemData.Code };
                                    simulDay.ResultDatasForMetric.Add(backResultData);
                                    ResultDatasForMetric1[j].Add(backResultData);
                                }

                                var resultData = simulDay.ResultDatasForMetric[simulDay.ResultDatasForMetric.Count - 1];
                                if (isItemLimitAverage || resultData.Count < ItemLimit)
                                {
                                    resultData.Count++;
                                    (foundItem.itemData as BackItemData).resultDataForMetric[j] = resultData;
                                }
                            }
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
            try
            {
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
            }
            catch (Exception e)
            {
                ShowError(form, e.Message);
                throw;
            }

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
    }
}