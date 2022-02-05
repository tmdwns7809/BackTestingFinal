﻿using BrightIdeasSoftware;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TradingLibrary;
using TradingLibrary.Base;

namespace BackTestingFinal
{
    public sealed class BackTesting : BaseFunctions
    {
        public static BackTesting instance;

        BackItemData showingItemData;

        SortedList<DateTime, DayData>[] simulDays = new SortedList<DateTime, DayData>[] { new SortedList<DateTime, DayData>(), new SortedList<DateTime, DayData>() }; // 0 long 1 short
        SortedList<DateTime, DayData> marketDays = new SortedList<DateTime, DayData>();

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
        string MetricMDDEnd = "MDDEnd";
        string MetricLDDD = "LDD Days";
        string MetricLDDDStart = "LDDDStart";
        string MetricLDDDEnd = "LDDDEnd";
        string MetricStart = "    Start";
        string MetricEnd = "    End";
        string MetricWinRate = "Win Rate";
        string MetricWinRateYearStandardDeviation = "    WRYSD";
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

        public BackTesting(Form form, bool isJoo) : base(form, isJoo, 0)
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
                var RSIA2 = CalIndicator(list, list[i], i - 1, 2).RSIA;
                var RSIA2Diff = RSIA2 - CalIndicator(list, list[i - 1], i - 2, 2).RSIA;
                var RSIA3 = CalIndicator(list, list[i], i - 1, 3).RSIA;
                var RSIA3Diff = RSIA3 - CalIndicator(list, list[i - 1], i - 2, 3).RSIA;
                var RSIA4 = CalIndicator(list, list[i], i - 1, 4).RSIA;
                var RSIA4Diff = RSIA4 - CalIndicator(list, list[i - 1], i - 2, 4).RSIA;
                var RSIA10 = CalIndicator(list, list[i], i - 1, 10).RSIA;
                var RSIA10Diff = RSIA10 - CalIndicator(list, list[i - 1], i - 2, 10).RSIA;
                var RSIA15 = CalIndicator(list, list[i], i - 1, 15).RSIA;
                var RSIA15Diff = RSIA15 - CalIndicator(list, list[i - 1], i - 2, 15).RSIA;
                var RSIA20 = CalIndicator(list, list[i], i - 1, 20).RSIA;
                var RSIA20Diff = RSIA20 - CalIndicator(list, list[i - 1], i - 2, 20).RSIA;
                //CheckSuddenBurst(isJoo, list, list[i], chartValues, i - 1);
                form.Text = showingItemData.Code + "     H:" + list[i].Price[0] + "  L:" + list[i].Price[1] + "  O:" + list[i].Price[2] + "  C:" + list[i].Price[3] + "  Ms:" + list[i].Ms + "  Md:" + list[i].Md +
                    "  RSIA2:" + Math.Round(RSIA2, 0) + "  RSIA2Diff:" + Math.Round(RSIA2Diff, 0) + "  RSIA3:" + Math.Round(RSIA3, 0) + "  RSIA3Diff:" + Math.Round(RSIA3Diff, 0) +
                    "  RSIA4:" + Math.Round(RSIA4, 0) + "  RSIA4Diff:" + Math.Round(RSIA4Diff, 0) + "  RSIA10:" + Math.Round(RSIA10, 0) + "  RSIA10Diff:" + Math.Round(RSIA10Diff, 0) +
                    "  RSIA15:" + Math.Round(RSIA15, 0) + "  RSIA15Diff:" + Math.Round(RSIA15Diff, 0) + "  RSIA20:" + Math.Round(RSIA20, 0) + "  RSIA20Diff:" + Math.Round(RSIA20Diff, 0);
            };

            clickResultAction = new Action<DayData>((date) =>
            {
                dayResultListView.ClearObjects();

                var n = 0;
                foreach (var sd in simulDays)
                    foreach (var data in date.resultDatas)
                        if (data.showNow && data.EnterTime.Date == date.Date && data.ExitTime.Date <= sd.Keys[sd.Count - 1])
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

                    clickResultAction(simulDays[0][DateTime.Parse(totalChart.Series[0].Points[result.Xindex].AxisLabel)]);
                }
            };

            var mainChartArea = mainChart.ChartAreas[0];
            var chartAreaCR = totalChart.ChartAreas.Add("ChartAreaCumulativeReturn");
            SetChartAreaFirst(chartAreaCR);
            chartAreaCR.Position = new ElementPosition(mainChartArea.Position.X, mainChartArea.Position.Y, mainChartArea.Position.Width, 
                (100 - mainChartArea.Position.Y - (int)mainChart.Tag) / 3f);
            chartAreaCR.InnerPlotPosition = new ElementPosition(mainChartArea.InnerPlotPosition.X, mainChartArea.InnerPlotPosition.Y, mainChartArea.InnerPlotPosition.Width, mainChartArea.InnerPlotPosition.Height);
            chartAreaCR.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartAreaCR.AxisY.Enabled = AxisEnabled.False;
            chartAreaCR.AxisY2.LabelStyle.Format = "{0:0.00}%";
            chartAreaCR.AxisY2.IntervalAutoMode = IntervalAutoMode.FixedCount;
            //chartArea6.AxisY2.StripLines.Add(new StripLine() { IntervalOffset = 0, BackColor = ColorSet.Border, StripWidth = 0.5 });

            var chartAreaPR = totalChart.ChartAreas.Add("ChartAreaProfitRate");
            SetChartAreaFirst(chartAreaPR);
            chartAreaPR.Position = new ElementPosition(chartAreaCR.Position.X, chartAreaCR.Position.Y + chartAreaCR.Position.Height, chartAreaCR.Position.Width, chartAreaCR.Position.Height);
            chartAreaPR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaCR.InnerPlotPosition.Height);
            chartAreaPR.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartAreaPR.AxisY.Enabled = AxisEnabled.False;
            chartAreaPR.AxisY2.LabelStyle.Format = "{0:0.00}%";
            chartAreaPR.AxisY2.IntervalAutoMode = IntervalAutoMode.FixedCount;

            var chartAreaHas = totalChart.ChartAreas.Add("ChartAreaHas");
            SetChartAreaFirst(chartAreaHas);
            chartAreaHas.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaPR.Position.Y + chartAreaPR.Position.Height, chartAreaPR.Position.Width, 100 - (chartAreaPR.Position.Y + chartAreaPR.Position.Height));
            chartAreaHas.InnerPlotPosition = new ElementPosition(chartAreaPR.InnerPlotPosition.X, chartAreaPR.InnerPlotPosition.Y, chartAreaPR.InnerPlotPosition.Width, 
                100 - chartAreaPR.InnerPlotPosition.Y - (int)mainChart.Tag / 100f * chartAreaHas.Position.Height);
            chartAreaHas.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartAreaHas.AxisY.Enabled = AxisEnabled.False;
            chartAreaHas.AxisY2.IntervalAutoMode = IntervalAutoMode.FixedCount;

            var seriesLCR = totalChart.Series.Add("LongCR");
            seriesLCR.ChartType = SeriesChartType.Spline;
            seriesLCR.XValueType = ChartValueType.Time;
            seriesLCR.Color = Color.FromArgb(50, ColorSet.PlusPrice);
            seriesLCR.YAxisType = AxisType.Secondary;
            seriesLCR.ChartArea = chartAreaCR.Name;

            var seriesLPR = totalChart.Series.Add("LongProfitRate");
            seriesLPR.ChartType = SeriesChartType.Spline;
            seriesLPR.XValueType = seriesLCR.XValueType;
            seriesLPR.Color = seriesLCR.Color;
            seriesLPR.YAxisType = seriesLCR.YAxisType;
            seriesLPR.ChartArea = chartAreaPR.Name;

            var seriesLH = totalChart.Series.Add("LongHas");
            seriesLH.ChartType = SeriesChartType.Spline;
            seriesLH.XValueType = seriesLCR.XValueType;
            seriesLH.Color = seriesLCR.Color;
            seriesLH.YAxisType = seriesLCR.YAxisType;
            seriesLH.ChartArea = chartAreaHas.Name;

            var seriesSCR = totalChart.Series.Add("ShortCR");
            seriesSCR.ChartType = seriesLCR.ChartType;
            seriesSCR.XValueType = seriesLCR.XValueType;
            seriesSCR.Color = Color.FromArgb(50, ColorSet.MinusPrice);
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
            var runAction = new Action<int>((isALS) =>
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
            metricDic.Add(MetricCompoundAnnualGrowthRate, new MetricData() { MetricName = MetricCompoundAnnualGrowthRate });
            metricListView.AddObject(metricDic[MetricCompoundAnnualGrowthRate]);
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
            form.BeginInvoke(new Action(() => { ClearChart(totalChart, true); }));

            if (simulDays[0].Count == 0 || start < simulDays[0].Keys[0] || end > simulDays[0].Keys[simulDays[0].Keys.Count - 1])
            {
                LoadingSettingFirst(form);
                sw.Reset();
                sw.Start();

                marketDays.Clear();
                foreach (var sd in simulDays)
                    sd.Clear();
                openDaysPerYear.Clear();

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
                AlertStart("done : " + sw.Elapsed.ToString(TimeSpanFormat));
            }

            form.BeginInvoke(new Action(() =>
            {
                CalculateMetric(start, end, isAllLongShort);
                totalButton.PerformClick();
            }));
        }

        void CalculateMetric(DateTime start, DateTime end, int isAllLongShort)
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

            for (int j = 0; j < 2; j++)
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
                            if (isAllLongShort == 0 || (isAllLongShort != 1 ^ resultData.LorS == 0))
                            {
                                resultData.showNow = true;

                                var hasTime = resultData.ExitTime.Subtract(resultData.EnterTime);
                                if (hasTime > MetricVars[resultData.LorS].longestHasTime)
                                {
                                    MetricVars[resultData.LorS].longestHasTime = hasTime;
                                    MetricVars[resultData.LorS].longestHasCode = resultData.Code;
                                }

                                var itemData = itemDataDic[resultData.Code];

                                simulDays[j].Values[i].Count++;
                                itemData.Count++;
                                simulDays[j].Values[i].ProfitRateSum += resultData.ProfitRate;
                                MetricVars[resultData.LorS].HasItemsAtADay++;
                                MetricVars[resultData.LorS].Count++;
                                MetricVars[resultData.LorS].ProfitRateSum += resultData.ProfitRate;
                                if (resultData.ProfitRate > commisionRate)
                                {
                                    simulDays[j].Values[i].Win++;
                                    itemData.Win++;
                                    simulDays[j].Values[i].WinProfitRateSum += resultData.ProfitRate;
                                    MetricVars[resultData.LorS].Win++;
                                    MetricVars[resultData.LorS].ProfitWinRateSum += resultData.ProfitRate;
                                }
                            }
                            else
                                resultData.showNow = false;
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
                            var avgPR = resultData.ProfitRate / resultData.Count;
                            MetricVars[j].CR *= 1 + avgPR * Kelly / 100;
                            MetricVars[j].ProfitRates.Add(avgPR);
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

            foreach (var itemData in itemDataDic.Values)
                itemData.WinRate = itemData.Count == 0 ? -1 : Math.Round((double)itemData.Win / itemData.Count * 100, 2);

            //metricResultListView.Sort(metricResultListView.GetColumn("PRA"), SortOrder.Ascending);
            //var n = 0;
            //foreach (DayData ob in metricResultListView.Objects)
            //    ob.Number = ++n;
            //metricResultListView.Sort(metricResultListView.GetColumn("No."), SortOrder.Descending);

            #region Print Result
            for (int i = 0; i < 2; i++)
            {
                if (MetricVars[i].Count != 0)
                {
                    metricDic[MetricWinRate].SetText(i, Math.Round((double)MetricVars[i].Win / MetricVars[i].Count * 100, 2) + "%");
                    metricDic[MetricAvgProfitRate].SetText(i, Math.Round(MetricVars[i].ProfitRateSum / MetricVars[i].Count, 2) + "%");
                    metricDic[MetricWinAvgProfitRate].SetText(i, MetricVars[i].Win == 0 ? "0%" : (Math.Round(MetricVars[i].ProfitWinRateSum / MetricVars[i].Win, 2) + "%"));
                    metricDic[MetricLoseAvgProfitRate].SetText(i, MetricVars[i].Count == MetricVars[i].Win ? "0%" : (Math.Round((MetricVars[i].ProfitRateSum - MetricVars[i].ProfitWinRateSum) / (MetricVars[i].Count - MetricVars[i].Win), 2) + "%"));
                    metricDic[MetricMaxHasTime].SetText(i, MetricVars[i].highestHasItemsDate.ToString(DateTimeFormat));
                    metricDic[MetricLongestHasDays].SetText(i, MetricVars[i].longestHasTime.ToString(TimeSpanFormat));

                    metricDic[MetricCR].SetText(i, Math.Round((MetricVars[i].CR - 1) * 100, 0) + "%");
                    metricDic[MetricMDD].SetText(i, Math.Round((MetricVars[i].MDD.LowestCumulativeReturn / MetricVars[i].MDD.HighestCumulativeReturn - 1) * 100, 0) + "%");
                    metricDic[MetricMDDStart].SetText(i, simulDays[i].Keys[MetricVars[i].MDD.HighestCumulativeReturnIndex].ToString(DateTimeFormat));
                    metricDic[MetricMDDEnd].SetText(i, simulDays[i].Keys[MetricVars[i].MDD.LowestCumulativeReturnIndex].ToString(DateTimeFormat));
                    metricDic[MetricLDDD].SetText(i, MetricVars[i].MDD.LastCumulativeReturnIndex - MetricVars[i].MDD.HighestCumulativeReturnIndex + "days");
                    metricDic[MetricLDDDStart].SetText(i, simulDays[i].Keys[MetricVars[i].MDD.HighestCumulativeReturnIndex].ToString(DateTimeFormat));
                    metricDic[MetricLDDDEnd].SetText(i, simulDays[i].Keys[MetricVars[i].MDD.LastCumulativeReturnIndex].ToString(DateTimeFormat));
                }
                else
                {
                    metricDic[MetricWinRate].SetText(i, "0%");
                    metricDic[MetricAvgProfitRate].SetText(i, "0%");
                    metricDic[MetricWinAvgProfitRate].SetText(i, "0%");
                    metricDic[MetricLoseAvgProfitRate].SetText(i, "0%");
                    metricDic[MetricMaxHasTime].SetText(i, "");
                    metricDic[MetricLongestHasDays].SetText(i, "");

                    metricDic[MetricCR].SetText(i, "");
                    metricDic[MetricMDD].SetText(i, "");
                    metricDic[MetricMDDStart].SetText(i, "");
                    metricDic[MetricMDDEnd].SetText(i, "");
                    metricDic[MetricLDDD].SetText(i, "");
                    metricDic[MetricLDDDStart].SetText(i, "");
                    metricDic[MetricLDDDEnd].SetText(i, "");
                }
                metricDic[MetricAllCount].SetText(i, MetricVars[i].Count.ToString());
                metricDic[MetricMaxHas].SetText(i, MetricVars[i].highestHasItemsAtADay.ToString());
                metricDic[MetricLongestHasDaysCode].SetText(i, MetricVars[i].longestHasCode);
            }
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
                    for (int j = 0; j < 2; j++)
                    {
                        itemData.foundList[j] = new List<(DateTime foundTime, ChartValues chartValues)>();
                        itemData.found[j] = false;
                    }

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
                            OneChartFindConditionAndAdd(itemData, vc, v.currentIndex - 1);

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
                        for (int j = 0; j < 2; j++)
                            if (itemData.found[j] && (toPast || result.foundTime == DateTime.MinValue))
                                EnterSetting(itemData, vm.lastStick, j);
                    }
                    else if (ExitConditionFinal(itemData))
                    {
                        itemData.Enter = false;

                        var resultData = new BackResultData()
                        {
                            Code = itemData.Code,
                            EnterTime = itemData.EnterTime,
                            ExitTime = vm.lastStick.Time,
                            ProfitRate = Math.Round((double)((itemData.EnterPositionIsLong == 0 ? vm.lastStick.Price[3] / itemData.EnterPrice : itemData.EnterPrice / vm.lastStick.Price[3]) - 1) * 100, 2),
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
                            if (SuddenBurst(v.list[j]).found)
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
                if (iD.firstLastMin.lastMin < from2)
                    return;

                var vm = iD.listDic.Values[minIndex];
                var vmc = iD.listDic.Keys[minIndex];
                for (int j = 0; j < 2; j++)
                {
                    iD.foundList[j] = new List<(DateTime foundTime, ChartValues chartValues)>();
                    iD.found[j] = false;
                }

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
                    OneChartFindConditionAndAdd(iD, vc, v.currentIndex - 1);

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
                    lock (foundLocker)
                        for (int j = 0; j < 2; j++)
                            if (iD.found[j]) 
                                foundItemList[j].Add((iD, iD.foundList[j]));
                }
                else if (ExitConditionFinal(iD))
                {
                    iD.Enter = false;
                    var resultData = new BackResultData()
                    {
                        Code = iD.Code,
                        EnterTime = iD.EnterTime,
                        ExitTime = vm.lastStick.Time,
                        ProfitRate = Math.Round((double)((iD.EnterPositionIsLong == 0 ? vm.lastStick.Price[3] / iD.EnterPrice : iD.EnterPrice / vm.lastStick.Price[3]) - 1) * 100, 2),
                        Duration = vm.lastStick.Time.Subtract(iD.EnterTime).ToString(TimeSpanFormat),
                        BeforeGap = iD.EnterTime.Subtract(iD.BeforeExitTime).ToString(TimeSpanFormat),
                        LorS = iD.EnterPositionIsLong
                    };

                    if (iD.resultDataForMetric.Code == iD.Code)
                        iD.resultDataForMetric.ExitTime = resultData.ExitTime;
                    iD.resultDataForMetric.ProfitRate += resultData.ProfitRate;

                    iD.BeforeExitTime = resultData.ExitTime;

                    var enterIndex = simulDays[0].IndexOfKey(resultData.EnterTime.Date);
                    for (int k = simulDays[0].IndexOfKey(resultData.ExitTime.Date); k >= 0; k--)
                    {
                        if (k < enterIndex)
                            break;

                        lock (dayLocker)
                        {
                            simulDays[iD.EnterPositionIsLong].Values[k].resultDatas.Add(resultData);
                        }
                    }

                    var BeforeGap = TimeSpan.ParseExact(resultData.BeforeGap, TimeSpanFormat, null);
                    if (BeforeGap < iD.ShortestBeforeGap)
                    {
                        iD.ShortestBeforeGap = BeforeGap;
                        iD.ShortestBeforeGapText = BeforeGap.ToString();
                    }
                }
            });

            for (int i = 0; i < size; i++)
            {
                var from2 = from.AddMinutes(i);
                if (!simulDays[0].ContainsKey(from2.Date))
                {
                    foreach (var sd in simulDays)
                        sd.Add(from2.Date, new DayData() { Date = from2.Date });
                    marketDays.Add(from2.Date, new DayData() { Date = from2.Date });

                    var year = from2.Year;
                    if (!openDaysPerYear.ContainsKey(year))
                        openDaysPerYear.Add(year, 0);
                    openDaysPerYear[year]++;

                    AddOrChangeLoadingText("Simulating...(" + from2.ToString(DateTimeFormat) + ")   " + sw.Elapsed.ToString(TimeSpanFormat), false);
                }

                var block = new ActionBlock<BackItemData>(iD => { action(iD, i, from2); }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 6 });

                foundItemList = new List<(BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)>)>[] 
                    { new List<(BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)>)>(), 
                        new List<(BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)>)>() };

                foreach (var iD in itemDataDic.Values)
                    block.Post(iD);

                block.Complete();
                block.Completion.Wait();

                var conditionResult = AllItemFindCondition();
                if (conditionResult.found)
                    for (int j = 0; j < 2; j++)
                    {
                        var simulDay = simulDays[j][from2.Date];
                        if (conditionResult.position[j])
                            foreach (var foundItem in foundItemList[j])
                            {
                                EnterSetting(foundItem.itemData, foundItem.itemData.listDic.Values[minIndex].lastStick, j);
                                if (simulDay.ResultDatasForMetric.Count == 0 || simulDay.ResultDatasForMetric[simulDay.ResultDatasForMetric.Count - 1].ExitTime != default)
                                    simulDay.ResultDatasForMetric.Add(new BackResultData() { EnterTime = from2, Code = foundItem.itemData.Code });
                                var resultData = simulDay.ResultDatasForMetric[simulDay.ResultDatasForMetric.Count - 1];
                                resultData.Count++;
                                (foundItem.itemData as BackItemData).resultDataForMetric = resultData;
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
    }

    public class MetricVar
    {
        public bool isLong;

        public double CR = 1D;
        public double Kelly = 1D;
        public List<double> ProfitRates = new List<double>();

        public DrawDownData DD = default;
        public DrawDownData MDD = default;

        public int highestHasItemsAtADay = int.MinValue;
        public DateTime highestHasItemsDate = default;
        public TimeSpan longestHasTime = TimeSpan.MinValue;
        public string longestHasCode = "";
        public int Count = 0;
        public int Win = 0;
        public double ProfitRateSum = 0;
        public double ProfitWinRateSum = 0;

        public int HasItemsAtADay = 0;
    }
}
