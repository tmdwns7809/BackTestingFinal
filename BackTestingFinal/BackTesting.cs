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
using TradingLibrary.Base.Enum;
using TradingLibrary.Base.Settings;
using TradingLibrary.Base.SticksDB;
using System.Runtime.InteropServices;
using static System.Windows.Forms.AxHost;

namespace BackTestingFinal
{
    public sealed class BackTesting : BaseFunctions
    {
        public static BackTesting instance;

        BackItemData showingItemData;

        Action<DayData> clickResultAction;
        FastObjectListView dayResultListView = new FastObjectListView();

        public Chart[] Charts = new Chart[] { new Chart(), new Chart(), new Chart(), new Chart() };
        public Chart[] Charts2 = new Chart[] { new Chart(), new Chart(), new Chart(), new Chart() };
        public Chart TimeCountChart = new Chart();
        public Button[] Buttons = new Button[] { new Button(), new Button(), new Button(), new Button(), new Button(), new Button(), new Button(), new Button() };
        public Button TimeCountChartButton = new Button();
        public Button captureButton = new Button();
        public Button beforeButton = new Button();
        public Button afterButton = new Button();
        public Button beforeAllChartButton = new Button();
        public Button afterAllChartButton = new Button();
        public TextBox fromTextBox = new TextBox();
        public TextBox midTextBox = new TextBox();
        public TextBox toTextBox = new TextBox();
        public ComboBox CRComboBox = new ComboBox();
        public Button runAllButton = new Button();
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

        //Dictionary<string, BackItemData> itemDataDic = new Dictionary<string, BackItemData>();
        Dictionary<string, MetricData> metricDic = new Dictionary<string, MetricData>();

        string KOSPI = "U001";
        string KOSDAQ = "U201";

        SortedList<int, int> openDaysPerYear = new SortedList<int, int>();
        SortedList<TimeSpan, int> TimeCount = new SortedList<TimeSpan, int>();

        string MetricCR = "CR";
        string MetricCompoundAnnualGrowthRate = "CAGR";
        string MetricMDD = "MDD";
        string MetricMDDStart = "MDDStart";
        string MetricMDDLow = "MDDLow";
        string MetricMDDEnd = "MDDEnd";
        string MetricMDDDays = "MDDDays";
        string MetricLDD = "LDD";
        string MetricLDDStart = "LDDStart";
        string MetricLDDLow = "LDDLow";
        string MetricLDDEnd = "LDDEnd";
        string MetricLDDDays = "LDDDays";
        string MetricDays = "    Days";
        string MetricStart = "    Start";
        string MetricLow = "    Low";
        string MetricEnd = "    End";
        string MetricSingle = "Single";
        string MetricSingleWinRate = "SingleWin Rate";
        string MetricWinRateYearStandardDeviation = "    WRYSD";
        string MetricSingleAllCount = "SingleCount";
        string MetricDisappear = "    Dis";
        string MetricLastDisappear = "    LDis";
        string MetricSingleAvgProfitRate = "SingleAPR";
        string MetricSingleWinAvgProfitRate = "SingleWAPR";
        string MetricSingleLoseAvgProfitRate = "SingleLAPR";
        string MetricAverage = "Average(-c,s)";
        string MetricAverageWinRate = "AverageWin Rate";
        string MetricAverageAllCount = "AverageCount";
        string MetricAverageAvgProfitRate = "AverageAPR";
        string MetricAverageWinAvgProfitRate = "AverageWAPR";
        string MetricAverageLoseAvgProfitRate = "AverageLAPR";
        string MetricWinRate = "    Win Rate";
        string MetricAllCount = "    Count";
        string MetricAvgProfitRate = "    APR";
        string MetricGeoMeanProfitRate = "    GMPR";
        string MetricWinAvgProfitRate = "    WAPR";
        string MetricLoseAvgProfitRate = "    LAPR";
        string MetricCommision = "    Commision";
        string MetricSlippage = "    Slippage";
        string MetricDayMaxHas = "Day Max Has";
        string MetricDayMaxHasDay = "    Date";
        string MetricLongestHasTime = "LH Days";
        string MetricLongestHasTimeCode = "    Code";
        string MetricLongestHasTimeStart = "    Start";
        string MetricMinKelly = "Min Kelly(Real)";
        string MetricMaxKelly = "Max Kelly(Real)";
        string MetricAverageHasDays = "AVGH Days";

        string sticksDBpath;
        string sticksDBbaseName;

        private object dayLocker = new object();
        private object foundLocker = new object();

        Stopwatch sw = new Stopwatch();

        bool TestAll = MessageBox.Show("TestAll?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
        bool AlertOn = MessageBox.Show("AlertOn?", "caption", MessageBoxButtons.YesNo) == DialogResult.Yes;
        int threadN;

        static string STResultDBPath = PathString.Base + @"BackTestingFinal\전략결과\";
        SQLiteConnection STResultDB = new SQLiteConnection(@"Data Source=" + STResultDBPath + "strategy_result.db");

        DateTime startDone = DateTime.MaxValue;
        DateTime endDone = DateTime.MinValue;

        int[] maxHas = new int[] { 0, 0 };

        CR lastCR;

        public BackTesting(Form form, bool isJoo) : base(form, isJoo, 7m)
        {

            sticksDBpath = BaseSticksDB.path;
            sticksDBbaseName = BaseSticksDB.BaseName;
            BinanceSticksDB.SetDB();

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
            //toTextBox.Text = "2022-04-21 14:35:00";
            //fromTextBox.Text = "2022-01-01";
            //toTextBox.Text = "2022-04-24 16:42:00";
            //fromTextBox.Text = "2021-11-01";
            //toTextBox.Text = "2022-02-01";
            //fromTextBox.Text = "2022-02-01";
            //fromTextBox.Text = "2022-05-10 09:34:00";
            //toTextBox.Text = "2022-05-09 06:26:00";
            //toTextBox.Text = "2022-05-06 19:30:00";
            //fromTextBox.Text = "2022-08-01";
            //toTextBox.Text = "2022-08-07";
        }
        void SetAdditionalMainView()
        {
            LeftClickAction += (i) =>
            {
                var chartValues = mainChart.Tag as ChartValues;
                var list = showingItemData.listDic[chartValues].list;

                form.Text = showingItemData.Code + "     H:" + list[i].Price[0] + "  L:" + list[i].Price[1] + "  O:" + list[i].Price[2] + "  C:" + list[i].Price[3] + "  Ms:" + list[i].Ms + "  Md:" + list[i].Md + 
                    " S5:" + Math.Round(mainChart.Series[5].Points[i].YValues[0], 2) + " S6:" + Math.Round(mainChart.Series[6].Points[i].YValues[0], 2);
            };

            /* test?
            RightClickAction += (i) =>
            {
                var chartValues = mainChart.Tag as ChartValues;
                var list = showingItemData.listDic[chartValues].list;

                for (int j = 0; j < mainChart.Series[5].Points.Count; j++)
                {
                    mainChart.Series[5].Points[j].YValues[0] = double.NaN;
                    mainChart.Series[6].Points[j].YValues[0] = double.NaN;
                }

                var list2 = new List<double>();
                var sum = 0D;
                for (int j = 99; j >= 0; j--)
                {
                    var size = 7;
                    if (j < size)
                        continue;
                    var a = GetAngleDiffRatio(list, i, i - j, 7);
                    mainChart.Series[5].Points[i - j].YValues[0] = Math.Round(a, 2);
                    list2.Add(a);
                    sum += a;
                    if (list2.Count > 3)
                    {
                        sum -= list2[0];
                        list2.RemoveAt(0);
                        mainChart.Series[6].Points[i - j].YValues[0] = Math.Round(sum / list2.Count, 2);
                        if (list2.Count != 3)
                            ShowError(form);
                    }
                }

                //DetectWM(list, i - 1, list[i], true);
            };
             */

            clickResultAction = new Action<DayData>((date) =>
            {
                dayResultListView.ClearObjects();

                var n = 0;
                    foreach (var data in date.resultDatas)
                        if (data.EnterTime.Date == date.Date && data.ExitTime.Date <= simulDays[0].Keys.Last())
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

            EventHandler resultChartClick = (sender, e) => {
                var chart = sender as Chart;
                var e2 = e as MouseEventArgs;
                if (e2.Button == MouseButtons.Left)
                {
                    var result = GetCursorPosition(chart, e2);
                    if (!result.isInArea)
                        return;

                    var text = chart.Series[0].Points[result.Xindex].AxisLabel;
                    foreach (var s in chart.Series)
                        if (s.Points.Count != 0)
                            text += "    " + s.Points[result.Xindex].YValues[0];

                    form.Text = text;

                    clickResultAction(simulDays[0][DateTime.Parse(chart.Series[0].Points[result.Xindex].AxisLabel).Date]);
                }
                else
                {
                    foreach (var ca2 in chart.ChartAreas)
                    {
                        ca2.AxisX.ScaleView.ZoomReset();
                        ca2.AxisY2.ScaleView.ZoomReset();
                        if (!double.IsNaN(ca2.CursorX.Position))
                            ca2.CursorX.SetCursorPosition(double.NaN);
                        if (!double.IsNaN(ca2.CursorY.Position))
                            ca2.CursorY.SetCursorPosition(double.NaN);
                    }
                }
            };

            #region Charts
            for (int i = 0; i < Charts.Length; i++)
            {
                SetChart(Charts[i], new Size(mainChart.Size.Width, GetFormHeight(form) - GetFormUpBarSize(form) + 10), new Point(mainChart.Location.X, mainChart.Location.Y), form);
                Charts[i].Hide();
                Charts[i].Click += resultChartClick;

                var mainChartArea = mainChart.ChartAreas[0];
                var areaHeight = 100 - mainChartArea.Position.Y;
                var chartAreaCR = Charts[i].ChartAreas.Add("ChartAreaCumulativeReturn");
                SetChartAreaFirst(chartAreaCR);
                chartAreaCR.Position = new ElementPosition(mainChartArea.Position.X + 3, mainChartArea.Position.Y, mainChartArea.Position.Width - 3, areaHeight / 4f);
                chartAreaCR.InnerPlotPosition = new ElementPosition(mainChartArea.InnerPlotPosition.X, mainChartArea.InnerPlotPosition.Y, mainChartArea.InnerPlotPosition.Width, mainChartArea.InnerPlotPosition.Height - 7);
                ChartAreaSet(chartAreaCR, Charts[i]);

                var chartAreaPR = Charts[i].ChartAreas.Add("ChartAreaProfitRate");
                SetChartAreaFirst(chartAreaPR);
                chartAreaPR.Position = new ElementPosition(chartAreaCR.Position.X, chartAreaCR.Position.Y + chartAreaCR.Position.Height, chartAreaCR.Position.Width, (areaHeight - chartAreaCR.Position.Y - chartAreaCR.Position.Height) / 4 * 3 / 5);
                chartAreaPR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaCR.InnerPlotPosition.Height - 15);
                ChartAreaSet(chartAreaPR, Charts[i]);
                chartAreaPR.AlignWithChartArea = chartAreaCR.Name;

                var chartAreaHas = Charts[i].ChartAreas.Add("ChartAreaHas");
                SetChartAreaFirst(chartAreaHas);
                chartAreaHas.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaPR.Position.Y + chartAreaPR.Position.Height, chartAreaPR.Position.Width, chartAreaPR.Position.Height);
                chartAreaHas.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaPR.InnerPlotPosition.Height);
                ChartAreaSet(chartAreaHas, Charts[i]);
                chartAreaHas.AlignWithChartArea = chartAreaCR.Name;
                chartAreaHas.AxisY.LabelStyle.Format = "";
                chartAreaHas.AxisY2.LabelStyle.Format = "";
                chartAreaHas.AxisY2.IsStartedFromZero = true;
                chartAreaHas.AxisY.IsStartedFromZero = true;

                var chartAreaWR = Charts[i].ChartAreas.Add("ChartAreaWR");
                SetChartAreaFirst(chartAreaWR);
                chartAreaWR.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaHas.Position.Y + chartAreaHas.Position.Height, chartAreaPR.Position.Width, chartAreaHas.Position.Height);
                chartAreaWR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaPR.InnerPlotPosition.Height);
                ChartAreaSet(chartAreaWR, Charts[i]);
                chartAreaWR.AlignWithChartArea = chartAreaCR.Name;
                chartAreaWR.AxisY.Minimum = 0;
                chartAreaWR.AxisY.Maximum = 100;
                chartAreaWR.AxisY2.Minimum = 0;
                chartAreaWR.AxisY2.Maximum = 100;

                var chartAreaWPR = Charts[i].ChartAreas.Add("ChartAreaWPR");
                SetChartAreaFirst(chartAreaWPR);
                chartAreaWPR.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaWR.Position.Y + chartAreaWR.Position.Height, chartAreaPR.Position.Width, chartAreaWR.Position.Height);
                chartAreaWPR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaPR.InnerPlotPosition.Height);
                ChartAreaSet(chartAreaWPR, Charts[i]);
                chartAreaWPR.AlignWithChartArea = chartAreaCR.Name;

                var chartAreaLPR = Charts[i].ChartAreas.Add("ChartAreaLPR");
                SetChartAreaFirst(chartAreaLPR);
                chartAreaLPR.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaWPR.Position.Y + chartAreaWPR.Position.Height, chartAreaPR.Position.Width, chartAreaWPR.Position.Height);
                chartAreaLPR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaPR.InnerPlotPosition.Height);
                ChartAreaSet(chartAreaLPR, Charts[i]);
                chartAreaLPR.AlignWithChartArea = chartAreaCR.Name;

                var chartAreaMCR = Charts[i].ChartAreas.Add("ChartAreaMarketCumulativeReturn");
                SetChartAreaFirst(chartAreaMCR);
                chartAreaMCR.Position = new ElementPosition(chartAreaCR.Position.X, chartAreaLPR.Position.Y + chartAreaLPR.Position.Height, chartAreaCR.Position.Width, (areaHeight - chartAreaLPR.Position.Y - chartAreaLPR.Position.Height) / 5 * 3);
                chartAreaMCR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaPR.InnerPlotPosition.Height + 22);
                ChartAreaSet(chartAreaMCR, Charts[i]);
                chartAreaMCR.AlignWithChartArea = chartAreaCR.Name;

                var chartAreaMV = Charts[i].ChartAreas.Add("ChartAreaMarketVolume");
                SetChartAreaLast(chartAreaMV);
                chartAreaMV.Position = new ElementPosition(chartAreaCR.Position.X, chartAreaMCR.Position.Y + chartAreaMCR.Position.Height, chartAreaCR.Position.Width, 100 - (chartAreaMCR.Position.Y + chartAreaMCR.Position.Height));
                chartAreaMV.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, 50);
                ChartAreaSet(chartAreaMV, Charts[i]);
                chartAreaMV.AxisY.LabelStyle.Enabled = false;
                chartAreaMV.AxisY2.LabelStyle.Enabled = false;
                chartAreaMV.AlignWithChartArea = chartAreaCR.Name;
                chartAreaMV.AxisY.Crossing = double.NaN;
                chartAreaMV.AxisY2.Crossing = double.NaN;

                #region series
                var alp = 200;
                var seriesLCR = Charts[i].Series.Add("LongCR");
                seriesLCR.ChartType = SeriesChartType.Line;
                seriesLCR.XValueType = ChartValueType.Time;
                seriesLCR.Color = Color.FromArgb(alp, ColorSet.PlusPrice);
                seriesLCR.YAxisType = AxisType.Primary;
                seriesLCR.ChartArea = chartAreaCR.Name;
                seriesLCR.Legend = seriesLCR.ChartArea;

                var seriesSCR = Charts[i].Series.Add("ShortCR");
                seriesSCR.ChartType = seriesLCR.ChartType;
                seriesSCR.XValueType = seriesLCR.XValueType;
                seriesSCR.Color = Color.FromArgb(alp, ColorSet.MinusPrice);
                seriesSCR.YAxisType = AxisType.Secondary;
                seriesSCR.ChartArea = seriesLCR.ChartArea;
                seriesSCR.Legend = seriesSCR.ChartArea;

                var seriesMCR1 = Charts[i].Series.Add("MarketCR1");
                seriesMCR1.ChartType = SeriesChartType.Line;
                seriesMCR1.XValueType = ChartValueType.Time;
                seriesMCR1.Color = Color.FromArgb(alp, Color.Red);
                seriesMCR1.YAxisType = AxisType.Primary;
                seriesMCR1.ChartArea = chartAreaMCR.Name;
                seriesMCR1.Legend = seriesMCR1.ChartArea;

                var seriesMCR2 = Charts[i].Series.Add("MarketCR2");
                seriesMCR2.ChartType = SeriesChartType.Line;
                seriesMCR2.XValueType = ChartValueType.Time;
                seriesMCR2.Color = Color.FromArgb(alp, Color.Orange);
                seriesMCR2.YAxisType = AxisType.Secondary;
                seriesMCR2.ChartArea = chartAreaMCR.Name;
                seriesMCR2.Legend = seriesMCR2.ChartArea;

                var alp3 = 100;
                var seriesMV1 = Charts[i].Series.Add("MarketVolume1");
                seriesMV1.ChartType = SeriesChartType.Column;
                seriesMV1.XValueType = ChartValueType.Time;
                seriesMV1.Color = Color.FromArgb(alp3, seriesMCR1.Color);
                seriesMV1.YAxisType = AxisType.Primary;
                seriesMV1.ChartArea = chartAreaMV.Name;
                seriesMV1.Legend = seriesMV1.ChartArea;
                seriesMV1.CustomProperties = "DrawSideBySide=False";

                var seriesMV2 = Charts[i].Series.Add("MarketVolume2");
                seriesMV2.ChartType = seriesMV1.ChartType;
                seriesMV2.XValueType = ChartValueType.Time;
                seriesMV2.Color = Color.FromArgb(alp3, seriesMCR2.Color);
                seriesMV2.YAxisType = AxisType.Secondary;
                seriesMV2.ChartArea = chartAreaMV.Name;
                seriesMV2.Legend = seriesMV2.ChartArea;
                seriesMV2.CustomProperties = seriesMV1.CustomProperties;

                var alp2 = 175;
                var seriesLPR = Charts[i].Series.Add("LongProfitRate");
                seriesLPR.ChartType = SeriesChartType.Column;
                seriesLPR.XValueType = seriesLCR.XValueType;
                seriesLPR.Color = Color.FromArgb(alp2, ColorSet.PlusPrice);
                seriesLPR.YAxisType = seriesLCR.YAxisType;
                seriesLPR.ChartArea = chartAreaPR.Name;
                seriesLPR.Legend = seriesLPR.ChartArea;
                seriesLPR.CustomProperties = "DrawSideBySide=False";

                var seriesSPR = Charts[i].Series.Add("ShortProfitRate");
                seriesSPR.ChartType = seriesLPR.ChartType;
                seriesSPR.XValueType = seriesLCR.XValueType;
                seriesSPR.Color = Color.FromArgb(alp2, ColorSet.MinusPrice);
                seriesSPR.YAxisType = seriesSCR.YAxisType;
                seriesSPR.ChartArea = seriesLPR.ChartArea;
                seriesSPR.Legend = seriesSPR.ChartArea;
                seriesSPR.CustomProperties = seriesLPR.CustomProperties;

                var seriesLH = Charts[i].Series.Add("LongHas");
                seriesLH.ChartType = seriesLPR.ChartType;
                seriesLH.XValueType = seriesLCR.XValueType;
                seriesLH.Color = seriesLPR.Color;
                seriesLH.YAxisType = seriesLCR.YAxisType;
                seriesLH.ChartArea = chartAreaHas.Name;
                seriesLH.Legend = seriesLH.ChartArea;
                seriesLH.CustomProperties = seriesLPR.CustomProperties;

                var seriesSH = Charts[i].Series.Add("ShortHas");
                seriesSH.ChartType = seriesLH.ChartType;
                seriesSH.XValueType = seriesLCR.XValueType;
                seriesSH.Color = seriesSPR.Color;
                seriesSH.YAxisType = seriesSCR.YAxisType;
                seriesSH.ChartArea = seriesLH.ChartArea;
                seriesSH.Legend = seriesSH.ChartArea;
                seriesSH.CustomProperties = seriesLPR.CustomProperties;

                var seriesLWR = Charts[i].Series.Add("LongWinRate");
                seriesLWR.ChartType = seriesLPR.ChartType;
                seriesLWR.XValueType = seriesLCR.XValueType;
                seriesLWR.Color = seriesLPR.Color;
                seriesLWR.YAxisType = seriesLCR.YAxisType;
                seriesLWR.ChartArea = chartAreaWR.Name;
                seriesLWR.Legend = seriesLWR.ChartArea;
                seriesLWR.CustomProperties = seriesLPR.CustomProperties;

                var seriesSWR = Charts[i].Series.Add("ShortWinRate");
                seriesSWR.ChartType = seriesLWR.ChartType;
                seriesSWR.XValueType = seriesLCR.XValueType;
                seriesSWR.Color = seriesSPR.Color;
                seriesSWR.YAxisType = seriesSCR.YAxisType;
                seriesSWR.ChartArea = seriesLWR.ChartArea;
                seriesSWR.Legend = seriesSWR.ChartArea;
                seriesSWR.CustomProperties = seriesLPR.CustomProperties;

                var seriesLWPR = Charts[i].Series.Add("LongWinProfitRate");
                seriesLWPR.ChartType = seriesLPR.ChartType;
                seriesLWPR.XValueType = seriesLCR.XValueType;
                seriesLWPR.Color = seriesLPR.Color;
                seriesLWPR.YAxisType = seriesLCR.YAxisType;
                seriesLWPR.ChartArea = chartAreaWPR.Name;
                seriesLWPR.Legend = seriesLWPR.ChartArea;
                seriesLWPR.CustomProperties = seriesLPR.CustomProperties;

                var seriesSWPR = Charts[i].Series.Add("ShortWinProfitRate");
                seriesSWPR.ChartType = seriesLWR.ChartType;
                seriesSWPR.XValueType = seriesLCR.XValueType;
                seriesSWPR.Color = seriesSPR.Color;
                seriesSWPR.YAxisType = seriesSCR.YAxisType;
                seriesSWPR.ChartArea = seriesLWPR.ChartArea;
                seriesSWPR.Legend = seriesSWPR.ChartArea;
                seriesSWPR.CustomProperties = seriesLPR.CustomProperties;

                var seriesLLPR = Charts[i].Series.Add("LongLoseProfitRate");
                seriesLLPR.ChartType = seriesLPR.ChartType;
                seriesLLPR.XValueType = seriesLCR.XValueType;
                seriesLLPR.Color = seriesLPR.Color;
                seriesLLPR.YAxisType = seriesLCR.YAxisType;
                seriesLLPR.ChartArea = chartAreaLPR.Name;
                seriesLLPR.Legend = seriesLLPR.ChartArea;
                seriesLLPR.CustomProperties = seriesLPR.CustomProperties;

                var seriesSLPR = Charts[i].Series.Add("ShortLoseProfitRate");
                seriesSLPR.ChartType = seriesLWR.ChartType;
                seriesSLPR.XValueType = seriesLCR.XValueType;
                seriesSLPR.Color = seriesSPR.Color;
                seriesSLPR.YAxisType = seriesSCR.YAxisType;
                seriesSLPR.ChartArea = seriesLLPR.ChartArea;
                seriesSLPR.Legend = seriesSLPR.ChartArea;
                seriesSLPR.CustomProperties = seriesLPR.CustomProperties;
                #endregion
            }
            for (int i = 0; i < Charts2.Length; i++)
            {
                SetChart(Charts2[i], new Size(mainChart.Size.Width, GetFormHeight(form) - GetFormUpBarSize(form) + 10), new Point(mainChart.Location.X, mainChart.Location.Y), form);
                Charts2[i].Hide();
                Charts2[i].Click += resultChartClick;

                var mainChartArea = mainChart.ChartAreas[0];
                var areaHeight = 100 - mainChartArea.Position.Y;
                var chartAreaCR = Charts2[i].ChartAreas.Add("ChartAreaCumulativeReturn");
                SetChartAreaFirst(chartAreaCR);
                chartAreaCR.Position = new ElementPosition(mainChartArea.Position.X + 3, mainChartArea.Position.Y, mainChartArea.Position.Width - 3, areaHeight / 13f * 2);
                chartAreaCR.InnerPlotPosition = new ElementPosition(mainChartArea.InnerPlotPosition.X, mainChartArea.InnerPlotPosition.Y, mainChartArea.InnerPlotPosition.Width, mainChartArea.InnerPlotPosition.Height - 8);
                ChartAreaSet(chartAreaCR, Charts2[i]);
                chartAreaCR.AxisY2.Enabled = AxisEnabled.False;
                chartAreaCR.CursorY.AxisType = AxisType.Primary;

                var chartAreaPR = Charts2[i].ChartAreas.Add("ChartAreaProfitRate");
                SetChartAreaFirst(chartAreaPR);
                chartAreaPR.Position = new ElementPosition(chartAreaCR.Position.X, chartAreaCR.Position.Y + chartAreaCR.Position.Height, chartAreaCR.Position.Width, chartAreaCR.Position.Height);
                chartAreaPR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaCR.InnerPlotPosition.Height);
                ChartAreaSet(chartAreaPR, Charts2[i]);
                chartAreaPR.AlignWithChartArea = chartAreaCR.Name;
                chartAreaPR.AxisY2.Enabled = AxisEnabled.False;
                chartAreaPR.CursorY.AxisType = AxisType.Primary;

                var chartAreaHas = Charts2[i].ChartAreas.Add("ChartAreaHas");
                SetChartAreaFirst(chartAreaHas);
                chartAreaHas.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaPR.Position.Y + chartAreaPR.Position.Height, chartAreaPR.Position.Width, chartAreaPR.Position.Height);
                chartAreaHas.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaCR.InnerPlotPosition.Height);
                ChartAreaSet(chartAreaHas, Charts2[i]);
                chartAreaHas.AlignWithChartArea = chartAreaCR.Name;
                chartAreaHas.AxisY.LabelStyle.Format = "";
                chartAreaHas.AxisY.IsStartedFromZero = true;
                chartAreaHas.AxisY2.Enabled = AxisEnabled.False;
                chartAreaHas.CursorY.AxisType = AxisType.Primary;

                var chartAreaWR = Charts2[i].ChartAreas.Add("ChartAreaWR");
                SetChartAreaFirst(chartAreaWR);
                chartAreaWR.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaHas.Position.Y + chartAreaHas.Position.Height, chartAreaPR.Position.Width, chartAreaHas.Position.Height);
                chartAreaWR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaPR.InnerPlotPosition.Height);
                ChartAreaSet(chartAreaWR, Charts2[i]);
                chartAreaWR.AlignWithChartArea = chartAreaCR.Name;
                chartAreaWR.AxisY.Minimum = 0;
                chartAreaWR.AxisY.Maximum = 100;
                chartAreaWR.AxisY2.Enabled = AxisEnabled.False;
                chartAreaWR.CursorY.AxisType = AxisType.Primary;

                var chartAreaWPR = Charts2[i].ChartAreas.Add("ChartAreaWPR");
                SetChartAreaFirst(chartAreaWPR);
                chartAreaWPR.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaWR.Position.Y + chartAreaWR.Position.Height, chartAreaPR.Position.Width, chartAreaHas.Position.Height);
                chartAreaWPR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, chartAreaPR.InnerPlotPosition.Height);
                ChartAreaSet(chartAreaWPR, Charts2[i]);
                chartAreaWPR.AlignWithChartArea = chartAreaCR.Name;
                chartAreaWPR.AxisY2.Enabled = AxisEnabled.False;
                chartAreaWPR.CursorY.AxisType = AxisType.Primary;

                var chartAreaLPR = Charts2[i].ChartAreas.Add("ChartAreaLPR");
                SetChartAreaLast(chartAreaLPR);
                chartAreaLPR.Position = new ElementPosition(chartAreaPR.Position.X, chartAreaWPR.Position.Y + chartAreaWPR.Position.Height, chartAreaPR.Position.Width, 100 - (chartAreaWPR.Position.Y + chartAreaWPR.Position.Height));
                chartAreaLPR.InnerPlotPosition = new ElementPosition(chartAreaCR.InnerPlotPosition.X, chartAreaCR.InnerPlotPosition.Y, chartAreaCR.InnerPlotPosition.Width, 70);
                ChartAreaSet(chartAreaLPR, Charts2[i]);
                chartAreaLPR.AlignWithChartArea = chartAreaCR.Name;
                chartAreaLPR.AxisY2.Enabled = AxisEnabled.False;
                chartAreaLPR.CursorY.AxisType = AxisType.Primary;
                chartAreaLPR.AxisY.Crossing = double.NaN;
                chartAreaLPR.AxisY2.Crossing = double.NaN;

                #region series
                var alp = 200;
                var seriesLCR = Charts2[i].Series.Add("CR");
                seriesLCR.ChartType = SeriesChartType.Line;
                seriesLCR.XValueType = ChartValueType.Time;
                seriesLCR.Color = Color.FromArgb(alp, (i == 0 || i == 2) ? ColorSet.PlusPrice : ColorSet.MinusPrice);
                seriesLCR.YAxisType = AxisType.Primary;
                seriesLCR.ChartArea = chartAreaCR.Name;
                seriesLCR.Legend = seriesLCR.ChartArea;

                var seriesLPR = Charts2[i].Series.Add("ProfitRate");
                seriesLPR.ChartType = SeriesChartType.Column;
                seriesLPR.XValueType = seriesLCR.XValueType;
                seriesLPR.Color = seriesLCR.Color;
                seriesLPR.YAxisType = seriesLCR.YAxisType;
                seriesLPR.ChartArea = chartAreaPR.Name;
                seriesLPR.Legend = seriesLPR.ChartArea;

                var seriesLH = Charts2[i].Series.Add("Has");
                seriesLH.ChartType = seriesLPR.ChartType;
                seriesLH.XValueType = seriesLCR.XValueType;
                seriesLH.Color = seriesLPR.Color;
                seriesLH.YAxisType = seriesLCR.YAxisType;
                seriesLH.ChartArea = chartAreaHas.Name;
                seriesLH.Legend = seriesLH.ChartArea;

                var seriesLWR = Charts2[i].Series.Add("WinRate");
                seriesLWR.ChartType = seriesLPR.ChartType;
                seriesLWR.XValueType = seriesLCR.XValueType;
                seriesLWR.Color = seriesLPR.Color;
                seriesLWR.YAxisType = seriesLCR.YAxisType;
                seriesLWR.ChartArea = chartAreaWR.Name;
                seriesLWR.Legend = seriesLWR.ChartArea;
                seriesLWR.CustomProperties = seriesLPR.CustomProperties;

                var seriesLWPR = Charts2[i].Series.Add("WinProfitRate");
                seriesLWPR.ChartType = seriesLPR.ChartType;
                seriesLWPR.XValueType = seriesLCR.XValueType;
                seriesLWPR.Color = seriesLPR.Color;
                seriesLWPR.YAxisType = seriesLCR.YAxisType;
                seriesLWPR.ChartArea = chartAreaWPR.Name;
                seriesLWPR.Legend = seriesLWPR.ChartArea;
                seriesLWPR.CustomProperties = seriesLPR.CustomProperties;

                var seriesLLPR = Charts2[i].Series.Add("LoseProfitRate");
                seriesLLPR.ChartType = seriesLPR.ChartType;
                seriesLLPR.XValueType = seriesLCR.XValueType;
                seriesLLPR.Color = seriesLPR.Color;
                seriesLLPR.YAxisType = seriesLCR.YAxisType;
                seriesLLPR.ChartArea = chartAreaLPR.Name;
                seriesLLPR.Legend = seriesLLPR.ChartArea;
                seriesLLPR.CustomProperties = seriesLPR.CustomProperties;
                #endregion
            }
            SetChart(TimeCountChart, mainChart.Size, mainChart.Location, form);
            TimeCountChart.Hide();

            var mainChartArea2 = mainChart.ChartAreas[0];
            var chartAreaTC = TimeCountChart.ChartAreas.Add("ChartAreaTimeCount");
            SetChartAreaLast(chartAreaTC);
            chartAreaTC.Position = new ElementPosition(mainChartArea2.Position.X, mainChartArea2.Position.Y, mainChartArea2.Position.Width, 100);
            chartAreaTC.InnerPlotPosition = new ElementPosition(mainChartArea2.InnerPlotPosition.X, mainChartArea2.InnerPlotPosition.Y + 2, mainChartArea2.InnerPlotPosition.Width, 90);
            chartAreaTC.AxisY2.IsStartedFromZero = true;

            var seriesTC = TimeCountChart.Series.Add("TC");
            seriesTC.ChartType = SeriesChartType.Column;
            seriesTC.XValueType = ChartValueType.Time;
            seriesTC.Color = Color.White;
            seriesTC.YAxisType = AxisType.Secondary;
            seriesTC.ChartArea = chartAreaTC.Name;
            #endregion

            #region Buttons
            var charButton = new Action<Chart, Button>((c, b) =>
            {
                if (!c.Visible)
                {
                    c.Visible = true;
                    c.BringToFront();
                    b.BackColor = ColorSet.ButtonSelected;

                    foreach (var b2 in buttonDic.Values)
                        b2.BringToFront();
                    foreach (var b2 in Buttons)
                        b2.BringToFront();
                    captureButton.BringToFront();
                    TimeCountChartButton.BringToFront();
                }
                else
                {
                    c.Visible = false;
                    b.BackColor = ColorSet.Button;
                }
            });

            SetButton(Buttons[2], "T", (sender, e) => { charButton(Charts[2], Buttons[2]); });
            Buttons[2].Size = buttonDic.Values.Last().Size;
            Buttons[2].Location = new Point(buttonDic.Values.Last().Location.X, buttonDic.Values.Last().Location.Y + buttonDic.Values.Last().Height + 5);

            SetButton(Buttons[3], "TD", (sender, e) => { charButton(Charts[3], Buttons[3]); });
            Buttons[3].Size = Buttons[2].Size;
            Buttons[3].Location = new Point(Buttons[2].Location.X, Buttons[2].Location.Y + Buttons[2].Height + 5);

            SetButton(Buttons[0], "L", (sender, e) => { charButton(Charts[0], Buttons[0]); });
            Buttons[0].Size = new Size(Buttons[3].Width / 2 - 5, Buttons[3].Height);
            Buttons[0].Location = new Point(Buttons[3].Location.X, Buttons[3].Location.Y + Buttons[3].Height + 5);

            SetButton(Buttons[1], "S", (sender, e) => { charButton(Charts[1], Buttons[1]); });
            Buttons[1].Size = Buttons[0].Size;
            Buttons[1].Location = new Point(Buttons[0].Location.X + Buttons[0].Width + 10, Buttons[0].Location.Y);

            SetButton(Buttons[4], "l", (sender, e) => { charButton(Charts2[0], Buttons[4]); });
            Buttons[4].Size = Buttons[0].Size;
            Buttons[4].Location = new Point(Buttons[0].Location.X, Buttons[0].Location.Y + Buttons[0].Height + 5);

            SetButton(Buttons[5], "s", (sender, e) => { charButton(Charts2[1], Buttons[5]); });
            Buttons[5].Size = Buttons[0].Size;
            Buttons[5].Location = new Point(Buttons[1].Location.X, Buttons[1].Location.Y + Buttons[1].Height + 5);

            SetButton(Buttons[6], "l", (sender, e) => { charButton(Charts2[2], Buttons[6]); });
            Buttons[6].Size = Buttons[0].Size;
            Buttons[6].Location = new Point(Buttons[4].Location.X, Buttons[4].Location.Y + Buttons[4].Height + 5);

            SetButton(Buttons[7], "s", (sender, e) => { charButton(Charts2[3], Buttons[7]); });
            Buttons[7].Size = Buttons[0].Size;
            Buttons[7].Location = new Point(Buttons[5].Location.X, Buttons[5].Location.Y + Buttons[5].Height + 5);

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
            captureButton.Size = new Size(Buttons[2].Width / 2, Buttons[2].Height / 2);
            captureButton.Location = new Point(Buttons[6].Location.X, Buttons[6].Location.Y + Buttons[6].Height + 5);

            SetButton(TimeCountChartButton, "TC", (sender, e) => { charButton(TimeCountChart, TimeCountChartButton); });
            TimeCountChartButton.Size = Buttons[2].Size;
            TimeCountChartButton.Location = new Point(captureButton.Location.X, captureButton.Location.Y + captureButton.Height + 5);
            #endregion

            #region Controller
            var ca = mainChart.ChartAreas[1];
            SetButton(beforeButton, "<", (sender, e) => { FindSimulAndShow(true); });
            beforeButton.Size = new Size((int)((mainChart.Width * (ca.Position.X + ca.Position.Width * ca.InnerPlotPosition.X / 100) / 100 - 15) / 2), Buttons[3].Height);
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
                ShowChart(result.itemData, (result.time, chartViewSticksSize, true));
            });
            lastButton.Size = beforeButton.Size;
            lastButton.Location = new Point(afterButton.Location.X, firstButton.Location.Y);
            #endregion
        }
        void ChartAreaSet(ChartArea ca, Chart chart)
        {
            ca.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            ca.AxisX.LineColor = Color.Gray;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.AxisY.LabelStyle.Format = "{0:#,0}%";
            ca.AxisY.IntervalAutoMode = IntervalAutoMode.FixedCount;
            ca.AxisY.ScrollBar.Enabled = true;
            ca.AxisY.MajorGrid.Enabled = false;
            ca.AxisY.LabelStyle.Interval = double.NaN;
            ca.AxisY.Maximum = double.NaN;
            ca.AxisY.Minimum = double.NaN;
            ca.AxisY.Crossing = 0;
            //ca.AxisY.StripLines.Add(new StripLine() { IntervalOffset = 0, BackColor = Color.White, StripWidth = 5 });
            ca.AxisY2.LabelStyle.Format = "{0:#,0}%";
            ca.AxisY2.IntervalAutoMode = IntervalAutoMode.FixedCount;
            ca.AxisY2.ScrollBar.Enabled = true;
            ca.AxisY2.MajorGrid.Enabled = false;
            ca.AxisY2.Crossing = 0;
            //ca.AxisY2.StripLines.Add(new StripLine() { IntervalOffset = 0, BackColor = Color.White, StripWidth = 5 });
            ca.CursorY.IsUserEnabled = true;
            ca.CursorY.IsUserSelectionEnabled = true;
            ca.CursorY.AxisType = AxisType.Secondary;


            var legend = chart.Legends.Add(ca.Name);
            legend.BackColor = Color.Transparent;
            legend.ForeColor = Color.FromArgb(50, ColorSet.FormText);
            legend.Docking = Docking.Top;
            //legend.Alignment = StringAlignment.Center;
            legend.BorderWidth = 1;
            legend.DockedToChartArea = ca.Name;
        }
        protected override void SetRestView()
        {
            var runAction = new Action<Position>((isALS) =>
            {
                if (DateTime.TryParse(fromTextBox.Text, out DateTime from) &&
                    DateTime.TryParse(toTextBox.Text, out DateTime to) && from <= to)
                {
                    var CRType = (CR)Enum.Parse(typeof(CR), CRComboBox.Text);
                    Task.Run(new Action(() =>
                    {
                        BinanceSticksDB.SetDB();

                        foreach (BackItemData itemData in itemDataDic.Values)
                            itemData.firstLastMin = (GetFirstOrLastTime(true, itemData, BaseChartTimeSet.OneMinute).time, GetFirstOrLastTime(false, itemData, BaseChartTimeSet.OneMinute).time);

                        var first = GetFirstOrLastTime(true, default, BaseChartTimeSet.OneMinute).time;
                        if (from < first)
                            from = first;
                        form.BeginInvoke(new Action(() => { fromTextBox.Text = from.ToString(TimeFormat); }));

                        var last = GetFirstOrLastTime(false, default, BaseChartTimeSet.OneMinute).time;
                        if (to > last)
                            to = last;
                        form.BeginInvoke(new Action(() => { toTextBox.Text = to.ToString(TimeFormat); }));

                        if (from > to)
                        {
                            ShowError(form, "input error");
                            return;
                        }
                        RunMain(from, to, isALS, CRType);
                    }));
                }
                else
                    ShowError(form, "input error");
            });

            #region From_To_Run
            SetTextBox(fromTextBox, "");
            fromTextBox.ReadOnly = false;
            fromTextBox.BorderStyle = BorderStyle.Fixed3D;
            fromTextBox.Size = new Size((GetFormWidth(form) - mainChart.Location.X - mainChart.Width) / 2 - 20, 30);
            fromTextBox.Location = new Point(mainChart.Location.X + mainChart.Width + 5, mainChart.Location.Y + 5);

            SetTextBox(midTextBox, "~");
            midTextBox.Size = new Size(10, fromTextBox.Height);
            midTextBox.Location = new Point(fromTextBox.Location.X + fromTextBox.Width + 10, fromTextBox.Location.Y);

            SetTextBox(toTextBox, "");
            toTextBox.ReadOnly = false;
            toTextBox.BorderStyle = BorderStyle.Fixed3D;
            toTextBox.Size = new Size(fromTextBox.Width, fromTextBox.Height);
            toTextBox.Location = new Point(midTextBox.Location.X + midTextBox.Width + 10, midTextBox.Location.Y);

            SetComboBox(CRComboBox, Enum.GetNames(typeof(CR)));
            CRComboBox.Size = new Size(150, toTextBox.Height);
            CRComboBox.Location = new Point(fromTextBox.Location.X, fromTextBox.Location.Y + fromTextBox.Height + 3);
            CRComboBox.SelectedIndex = CRComboBox.FindString(CR.Average.ToString());


            SetButton(runAllButton, "All", (sender, e) => { runAction(Position.All); });
            runAllButton.Size = new Size((GetFormWidth(form) - CRComboBox.Location.X - CRComboBox.Width - 10) / 3 - 2, toTextBox.Height);
            runAllButton.Location = new Point(CRComboBox.Location.X + CRComboBox.Width + 5, CRComboBox.Location.Y);

            SetButton(runLongButton, "Long", (sender, e) => { runAction(Position.Long); });
            runLongButton.Size = new Size(runAllButton.Width - 2, toTextBox.Height);
            runLongButton.Location = new Point(runAllButton.Location.X + runAllButton.Width + 4, CRComboBox.Location.Y);
            //runLongButton.Font = new Font(runLongButton.Font.FontFamily, 5);
            runLongButton.BackColor = ColorSet.PlusPrice;

            SetButton(runShortButton, "Short", (sender, e) => { runAction(Position.Short); });
            runShortButton.Size = new Size(runLongButton.Width, toTextBox.Height);
            runShortButton.Location = new Point(runLongButton.Location.X + runLongButton.Width + 4, CRComboBox.Location.Y);
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
                (GetFormHeight(form) - CRComboBox.Location.Y - CRComboBox.Height) / 10 * 7 - 10);
            metricListView.Location = new Point(mainChart.Location.X + mainChart.Size.Width, CRComboBox.Location.Y + CRComboBox.Height + 5);
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
                ShowChart(itemData, (GetFirstOrLastTime(false, itemData).time, chartViewSticksSize, false));

                ShowCodeResult(itemData);
            };
            #endregion

            #region Results
            var action = new Action<FastObjectListView>((sender) =>
            {
                if (sender.SelectedIndices.Count != 1)
                    return;

                var data = sender.SelectedObject as BackResultData;
                var itemData = itemDataDic[data.Code] as BackItemData;
                var result = LoadAndCheckSticks(itemData, true, false, default, data.OutEnterTime == default ? data.EnterTime : data.OutEnterTime, default, false);
                //SetChartNowOrLoad(result.chartValues);
                ShowChart(itemData, (result.foundTime, chartViewSticksSize / 2, true), true, result.chartValues);
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
                    ("Code", "Code", 4),
                    ("EnterTime", "EnterTime", 6),
                    ("ExitTime", "ExitTime", 6),
                    ("Dura", "Duration", 4),
                    ("Long", "LorS", 2),
                    ("PR(%)", "ProfitRate", 3),
                    ("LM", "EnterMarketLastMin", 3),
                    ("LMs", "EnterMarketLastMins", 3)
                });
            dayResultListView.Size = new Size(tabPage.Width - 12, tabPage.Height - 6);
            dayResultListView.Location = new Point(6, 6);
            dayResultListView.SelectionChanged += (sender, e) =>
            {
                if (dayResultListView.SelectedObject == null)
                    return;

                action(dayResultListView);
                ShowCodeResult(itemDataDic[(dayResultListView.SelectedObject as BackResultData).Code] as BackItemData);
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
                        if (itemData.Code == resultData.Code && resultData.EnterTime.Date == day.Key && resultData.ExitTime.Date >= sd.Values[0].Date)
                        {
                            resultData.NumberForSingle = ++n;
                            codeResultListView.AddObject(resultData);
                        }
        }

        void LoadCodeListAndMetric()
        {
            var conn = BinanceSticksDB.DBDic[BaseChartTimeSet.OneMinute];

            var reader = new SQLiteCommand("Select name From sqlite_master where type='table'", conn).ExecuteReader();

            var number = 1;
            while (reader.Read())
            {
                var itemData = new BackItemData(reader["name"].ToString(), number++);
                codeListView.AddObject(itemData);
                itemDataDic.Add(itemData.Code, itemData);
            }


            metricDic.Add(MetricCR, new MetricData() { MetricName = MetricCR });
            metricListView.AddObject(metricDic[MetricCR]);
            metricListView.AddObject(new MetricData());

            metricDic.Add(MetricSingle, new MetricData() { MetricName = MetricSingle });
            metricListView.AddObject(metricDic[MetricSingle]);
            metricDic.Add(MetricSingleWinRate, new MetricData() { MetricName = MetricWinRate });
            metricListView.AddObject(metricDic[MetricSingleWinRate]);
            metricDic.Add(MetricSingleAllCount, new MetricData() { MetricName = MetricAllCount });
            metricListView.AddObject(metricDic[MetricSingleAllCount]);
            metricDic.Add(MetricDisappear, new MetricData() { MetricName = MetricDisappear });
            metricListView.AddObject(metricDic[MetricDisappear]);
            metricDic.Add(MetricLastDisappear, new MetricData() { MetricName = MetricLastDisappear });
            metricListView.AddObject(metricDic[MetricLastDisappear]);
            metricDic.Add(MetricSingleAvgProfitRate, new MetricData() { MetricName = MetricAvgProfitRate });
            metricListView.AddObject(metricDic[MetricSingleAvgProfitRate]);
            metricDic.Add(MetricSingleWinAvgProfitRate, new MetricData() { MetricName = MetricWinAvgProfitRate });
            metricListView.AddObject(metricDic[MetricSingleWinAvgProfitRate]);
            metricDic.Add(MetricSingleLoseAvgProfitRate, new MetricData() { MetricName = MetricLoseAvgProfitRate });
            metricListView.AddObject(metricDic[MetricSingleLoseAvgProfitRate]);
            metricDic.Add(MetricCommision, new MetricData() { MetricName = MetricCommision });
            metricListView.AddObject(metricDic[MetricCommision]);
            metricDic.Add(MetricSlippage, new MetricData() { MetricName = MetricSlippage });
            metricListView.AddObject(metricDic[MetricSlippage]);
            metricDic.Add(MetricAverage, new MetricData() { MetricName = MetricAverage });
            metricListView.AddObject(metricDic[MetricAverage]);
            metricDic.Add(MetricAverageWinRate, new MetricData() { MetricName = MetricWinRate });
            metricListView.AddObject(metricDic[MetricAverageWinRate]);
            metricDic.Add(MetricAverageAllCount, new MetricData() { MetricName = MetricAllCount });
            metricListView.AddObject(metricDic[MetricAverageAllCount]);
            metricDic.Add(MetricGeoMeanProfitRate, new MetricData() { MetricName = MetricGeoMeanProfitRate });
            metricListView.AddObject(metricDic[MetricGeoMeanProfitRate]);
            metricDic.Add(MetricAverageAvgProfitRate, new MetricData() { MetricName = MetricAvgProfitRate });
            metricListView.AddObject(metricDic[MetricAverageAvgProfitRate]);
            metricDic.Add(MetricAverageWinAvgProfitRate, new MetricData() { MetricName = MetricWinAvgProfitRate });
            metricListView.AddObject(metricDic[MetricAverageWinAvgProfitRate]);
            metricDic.Add(MetricAverageLoseAvgProfitRate, new MetricData() { MetricName = MetricLoseAvgProfitRate });
            metricListView.AddObject(metricDic[MetricAverageLoseAvgProfitRate]);
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
            metricDic.Add(MetricLDD, new MetricData() { MetricName = MetricLDD });
            metricListView.AddObject(metricDic[MetricLDD]);
            metricDic.Add(MetricLDDDays, new MetricData() { MetricName = MetricDays });
            metricListView.AddObject(metricDic[MetricLDDDays]);
            metricDic.Add(MetricLDDStart, new MetricData() { MetricName = MetricStart });
            metricListView.AddObject(metricDic[MetricLDDStart]);
            metricDic.Add(MetricLDDLow, new MetricData() { MetricName = MetricLow });
            metricListView.AddObject(metricDic[MetricLDDLow]);
            metricDic.Add(MetricLDDEnd, new MetricData() { MetricName = MetricEnd });
            metricListView.AddObject(metricDic[MetricLDDEnd]);
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
            metricListView.AddObject(new MetricData());

            metricDic.Add(MetricMinKelly, new MetricData() { MetricName = MetricMinKelly });
            metricListView.AddObject(metricDic[MetricMinKelly]);
            metricDic.Add(MetricMaxKelly, new MetricData() { MetricName = MetricMaxKelly });
            metricListView.AddObject(metricDic[MetricMaxKelly]);
        }

        void RunMain(DateTime start, DateTime end, Position isAllLongShort, CR CRType)
        {
            form.BeginInvoke(new Action(() => 
            {
                foreach (var c in Charts)
                    ClearChart(c, true);
                foreach (var c in Charts2)
                    ClearChart(c, true);
                ClearChart(TimeCountChart, true);
            }));

            if (lastCR != CRType || start < startDone || end > endDone)
            {
                LoadingSettingFirst(form);
                sw.Reset();
                sw.Start();

                ClearBeforeRun();

                try
                {
                    Run(start, end, CRType);
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
                    AlertStart("done : " + sw.Elapsed.ToString(TimeSpanFormat), Settings.settings[Settings.ProgramName].others[Settings.AlertSoundName]);

                startDone = start;
                endDone = end;

                lastCR = CRType;
            }

            form.BeginInvoke(new Action(() =>
            {
                form.Text += ST + " done : " + sw.Elapsed.ToString(TimeSpanFormat);
                CalculateMetric(start, end, isAllLongShort);
                if (!Charts[2].Visible)
                    Buttons[2].PerformClick();
            }));
        }
        void ClearBeforeRun()
        {
            for (int i = 0; i < simulDays.Length; i++)
            {
                simulDays[i].Clear();
                simulDaysDetail[i].Clear();
            }
            openDaysPerYear.Clear();
            TimeCount.Clear();
        }

        void CalculateMetric(DateTime start, DateTime end, Position isAllLongShort)
        {
            #region First Setting
            var startStartTime = GetDetailStartTime(start.Date);
            var endStartTime = GetDetailStartTime(end);
            var startIndex = simulDaysDetail[0].IndexOfKey(startStartTime);
            var endIndex = simulDaysDetail[0].IndexOfKey(endStartTime);
            var d = BaseChartTimeSet.OneDay.seconds / DetailCV.seconds;

            metricResultListView.ClearObjects();

            var MetricVars = new MetricVar[] { new MetricVar() { isLong = true }, new MetricVar() { isLong = false } };

            var n = 0;
            #endregion

            foreach (var tc in TimeCount)
                TimeCountChart.Series[0].Points.AddXY(tc.Key.ToString(TimeSpanFormatdX), tc.Value);

            foreach (BackItemData itemData in itemDataDic.Values)
                itemData.Reset();

            var CRType = (CR)Enum.Parse(typeof(CR), CRComboBox.Text);

            var market1Day = LoadSticks(itemDataDic["BTCUSDT"] as BackItemData, BaseChartTimeSet.OneDay, start.Date, (int)end.Date.AddDays(1).Subtract(start.Date).TotalDays, false);
            var market2Day = LoadSticks(itemDataDic["ETHUSDT"] as BackItemData, BaseChartTimeSet.OneDay, start.Date, (int)end.Date.AddDays(1).Subtract(start.Date).TotalDays, false);
            var m1DI = 0;
            var m2DI = 0;
            var market1DayDetail = LoadSticks(itemDataDic["BTCUSDT"] as BackItemData, DetailCV, startStartTime, (int)endStartTime.AddSeconds(DetailCV.seconds).Subtract(startStartTime).TotalSeconds / DetailCV.seconds, false);
            var market2DayDetail = LoadSticks(itemDataDic["ETHUSDT"] as BackItemData, DetailCV, startStartTime, (int)endStartTime.AddSeconds(DetailCV.seconds).Subtract(startStartTime).TotalSeconds / DetailCV.seconds, false);
            var m1DDI = 0;
            var m2DDI = 0;

            for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                if (isAllLongShort == Position.All || isAllLongShort == (Position)j)
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        var di = i / d;
                        if (i % d == 0)
                        {
                            simulDays[j].Values[di].Count = 0;
                            simulDays[j].Values[di].Win = 0;
                            simulDays[j].Values[di].WinProfitRateSum = 0;
                            simulDays[j].Values[di].ProfitRateSum = 0;
                            simulDays[j].Values[di].kelly = CalculateKelly(MetricVars[j].ProfitRates, MetricVars[j].minKelly, MetricVars[j].maxKelly);
                        }

                        simulDaysDetail[j].Values[i].Count = 0;
                        simulDaysDetail[j].Values[i].Win = 0;
                        simulDaysDetail[j].Values[i].WinProfitRateSum = 0;
                        simulDaysDetail[j].Values[i].ProfitRateSum = 0;

                        foreach (var resultData in simulDaysDetail[j].Values[i].resultDatas)
                            if (GetDetailStartTime(resultData.EnterTime) == simulDaysDetail[j].Keys[i] && GetDetailStartTime(resultData.ExitTime) <= endStartTime)
                            {
                                var hasTime = resultData.ExitTime.Subtract(resultData.EnterTime);
                                if (hasTime > MetricVars[(int)resultData.LorS].longestHasTime)
                                {
                                    MetricVars[(int)resultData.LorS].longestHasTime = hasTime;
                                    MetricVars[(int)resultData.LorS].longestHasCode = resultData.Code;
                                    MetricVars[(int)resultData.LorS].longestHasTimeStart = resultData.EnterTime;
                                }

                                var itemData = itemDataDic[resultData.Code] as BackItemData;

                                simulDays[j].Values[di].Count++;
                                simulDaysDetail[j].Values[i].Count++;
                                itemData.Count++;
                                simulDays[j].Values[di].ProfitRateSum += resultData.ProfitRate;
                                simulDaysDetail[j].Values[i].ProfitRateSum += resultData.ProfitRate;
                                MetricVars[(int)resultData.LorS].SingleCount++;
                                MetricVars[(int)resultData.LorS].SingleProfitRateSum += resultData.ProfitRate;
                                if (resultData.ProfitRate > commisionRate + slippage)
                                {
                                    simulDays[j].Values[di].Win++;
                                    simulDaysDetail[j].Values[i].Win++;
                                    itemData.Win++;
                                    simulDays[j].Values[di].WinProfitRateSum += resultData.ProfitRate;
                                    simulDaysDetail[j].Values[i].WinProfitRateSum += resultData.ProfitRate;
                                    MetricVars[(int)resultData.LorS].SingleWin++;
                                    MetricVars[(int)resultData.LorS].SingleProfitWinRateSum += resultData.ProfitRate;
                                }
                            }

                        if (simulDays[j].Values[di].Count > MetricVars[j].highestHasItemsAtADay)
                        {
                            MetricVars[j].highestHasItemsAtADay = simulDays[j].Values[di].Count;
                            MetricVars[j].highestHasItemsDate = simulDays[j].Keys[di];
                        }

                        if (MetricVars[j].lowestKelly > simulDays[j].Values[di].kelly)
                            MetricVars[j].lowestKelly = simulDays[j].Values[di].kelly;
                        if (MetricVars[j].highestKelly < simulDays[j].Values[di].kelly)
                            MetricVars[j].highestKelly = simulDays[j].Values[di].kelly;

                        foreach (var resultData in simulDaysDetail[j].Values[i].ResultDatasForMetricReal)
                            if (GetDetailStartTime(resultData.EnterTime) == simulDaysDetail[j].Keys[i] && GetDetailStartTime(resultData.ExitTime) <= endStartTime)
                            {
                                if (resultData.Count == 0)
                                    continue;
                                    
                                if (MetricVars[j].beforeCR != default && MetricVars[j].beforeCR != MetricVars[j].CR)
                                    ShowError(form, "CR Error");

                                var avgPR = (resultData.ProfitRate / resultData.Count - 1) * 100 - commisionRate - slippage;

                                if (CRType != CR.AllWithMinimum)
                                    MetricVars[j].CR *= 1 + avgPR * simulDays[j].Values[di].kelly / 100;
                                else
                                    MetricVars[j].CR += avgPR / 100 / maxHas[j] * resultData.Count;

                                MetricVars[j].beforeCR = MetricVars[j].CR;

                                MetricVars[j].ProfitRates.Add(avgPR);

                                MetricVars[j].AverageCount++;
                                MetricVars[j].AverageProfitRateSum += avgPR;
                                MetricVars[j].AverageProfitRateMul *= 1 + avgPR / 100;
                                if (avgPR > 0)
                                {
                                    MetricVars[j].AverageWin++;
                                    MetricVars[j].AverageProfitWinRateSum += avgPR;
                                }

                                var xLabel = resultData.EnterTime.ToString(TimeFormat);
                                Charts2[j].Series[0].Points.AddXY(xLabel, Math.Round((MetricVars[j].CR - 1) * 100, 1));
                                Charts2[j].Series[1].Points.AddXY(xLabel, Math.Round(avgPR, 2));
                                Charts2[j].Series[2].Points.AddXY(xLabel, resultData.Count);
                                Charts2[j].Series[3].Points.AddXY(xLabel, Math.Round((double)resultData.WinCount / resultData.Count * 100, 2));
                                Charts2[j].Series[4].Points.AddXY(xLabel, resultData.WinCount == 0 ? 0 : Math.Round((resultData.WinProfitRateSum / resultData.WinCount - 1) * 100, 2));
                                Charts2[j].Series[5].Points.AddXY(xLabel, resultData.Count == resultData.WinCount ? 0 :
                                Math.Round(((resultData.ProfitRate - resultData.WinProfitRateSum) / (resultData.Count - resultData.WinCount) - 1) * 100, 2));
                            }

                        foreach (var resultData in simulDaysDetail[j].Values[i].disResultDatas)
                            if (GetDetailStartTime(resultData.EnterTime) == simulDaysDetail[j].Keys[i] && GetDetailStartTime(resultData.ExitTime) <= endStartTime)
                                MetricVars[j].disappearCount++;

                        foreach (var resultData in simulDaysDetail[j].Values[i].lastResultDatas)
                            if (GetDetailStartTime(resultData.EnterTime) == simulDaysDetail[j].Keys[i] && GetDetailStartTime(resultData.ExitTime) <= endStartTime)
                                MetricVars[j].lastDisappearCount++;

                        if (MetricVars[j].DD == default)
                        {
                            MetricVars[j].DD = new DrawDownData()
                            {
                                HighestCumulativeReturn = MetricVars[j].CR,
                                HighestCumulativeReturnIndex = di,
                                LowestCumulativeReturn = MetricVars[j].CR,
                                LowestCumulativeReturnIndex = di,
                                LastCumulativeReturnIndex = di,
                            };
                            MetricVars[j].MDD = MetricVars[j].DD;
                            MetricVars[j].LDD = MetricVars[j].DD;
                        }
                        else
                        {
                            if (MetricVars[j].CR <= MetricVars[j].DD.LowestCumulativeReturn && i != endIndex)
                            {
                                MetricVars[j].DD.LowestCumulativeReturn = MetricVars[j].CR;
                                MetricVars[j].DD.LowestCumulativeReturnIndex = di;
                            }
                            else if (MetricVars[j].CR > MetricVars[j].DD.HighestCumulativeReturn || i == endIndex)
                            {
                                MetricVars[j].DD.LastCumulativeReturnIndex = di;

                                if (MetricVars[j].MDD == default || MetricVars[j].DD.LowestCumulativeReturn / MetricVars[j].DD.HighestCumulativeReturn < MetricVars[j].MDD.LowestCumulativeReturn / MetricVars[j].MDD.HighestCumulativeReturn
                                    || (MetricVars[j].DD.LowestCumulativeReturn / MetricVars[j].DD.HighestCumulativeReturn == MetricVars[j].MDD.LowestCumulativeReturn / MetricVars[j].MDD.HighestCumulativeReturn &&
                                        MetricVars[j].DD.LastCumulativeReturnIndex - MetricVars[j].DD.HighestCumulativeReturnIndex > MetricVars[j].MDD.LastCumulativeReturnIndex - MetricVars[j].MDD.HighestCumulativeReturnIndex))
                                    MetricVars[j].MDD = MetricVars[j].DD;
                                if (MetricVars[j].LDD == default || 
                                    MetricVars[j].DD.LastCumulativeReturnIndex - MetricVars[j].DD.HighestCumulativeReturnIndex > MetricVars[j].LDD.LastCumulativeReturnIndex - MetricVars[j].LDD.HighestCumulativeReturnIndex)
                                    MetricVars[j].LDD = MetricVars[j].DD;

                                MetricVars[j].DD = new DrawDownData()
                                {
                                    HighestCumulativeReturn = MetricVars[j].CR,
                                    HighestCumulativeReturnIndex = di,
                                    LowestCumulativeReturn = MetricVars[j].CR,
                                    LowestCumulativeReturnIndex = di,
                                    LastCumulativeReturnIndex = di,
                                };
                            }
                        }

                        if (simulDaysDetail[j].Values[i].Count != 0)
                        {
                            simulDaysDetail[j].Values[i].WinRate = Math.Round((double)simulDaysDetail[j].Values[i].Win / simulDaysDetail[j].Values[i].Count * 100, 2);
                            simulDaysDetail[j].Values[i].ProfitRateAvg = Math.Round(simulDaysDetail[j].Values[i].ProfitRateSum / simulDaysDetail[j].Values[i].Count, 2);
                            simulDaysDetail[j].Values[i].WinProfitRateAvg = simulDaysDetail[j].Values[i].Win == 0 ? 0 : Math.Round(simulDaysDetail[j].Values[i].WinProfitRateSum / simulDaysDetail[j].Values[i].Win, 2);
                            simulDaysDetail[j].Values[i].LoseProfitRateAvg = simulDaysDetail[j].Values[i].Count == simulDaysDetail[j].Values[i].Win ? 0 :
                                Math.Round((simulDaysDetail[j].Values[i].ProfitRateSum - simulDaysDetail[j].Values[i].WinProfitRateSum) / (simulDaysDetail[j].Values[i].Count - simulDaysDetail[j].Values[i].Win), 2);
                        }
                        else
                        {
                            simulDaysDetail[j].Values[i].WinRate = 0;
                            simulDaysDetail[j].Values[i].ProfitRateAvg = 0;
                            simulDaysDetail[j].Values[i].WinProfitRateAvg = 0;
                            simulDaysDetail[j].Values[i].LoseProfitRateAvg = 0;
                        }

                        //if (simulDaysDetail[j].Values[i].Count != 0)
                        //{
                        //    simulDaysDetail[j].Values[i].Number = ++n;
                        //    metricResultListView.AddObject(simulDaysDetail[j].Values[i]);
                        //}

                        var axisLabel = simulDaysDetail[j].Keys[i].ToString(TimeFormat);
                        Charts[3].Series[j].Points.AddXY(axisLabel, Math.Round((MetricVars[j].CR - 1) * 100, 0));
                        Charts[3].Series[j + 6].Points.AddXY(axisLabel, simulDaysDetail[j].Values[i].ProfitRateAvg);
                        Charts[3].Series[j + 8].Points.AddXY(axisLabel, simulDaysDetail[j].Values[i].Count);
                        Charts[3].Series[j + 10].Points.AddXY(axisLabel, simulDaysDetail[j].Values[i].WinRate);
                        Charts[3].Series[j + 12].Points.AddXY(axisLabel, simulDaysDetail[j].Values[i].WinProfitRateAvg);
                        Charts[3].Series[j + 14].Points.AddXY(axisLabel, simulDaysDetail[j].Values[i].LoseProfitRateAvg);

                        if (isAllLongShort != Position.All || j == (int)Position.Long)
                        {
                            var date = simulDaysDetail[j].Keys[i];
                            if (date >= market1DayDetail[0].Time && date <= market1DayDetail[market1DayDetail.Count - 1].Time)
                            {
                                Charts[3].Series[2].Points.AddXY(axisLabel, Math.Round((market1DayDetail[m1DDI].Price[3] / market1DayDetail[0].Price[3] - 1) * 100, 0));
                                Charts[3].Series[4].Points.AddXY(axisLabel, market1DayDetail[m1DDI].Ms + market1DayDetail[m1DDI].Md);
                                m1DDI++;
                            }
                            else
                            {
                                Charts[3].Series[2].Points.AddXY(axisLabel, 0);
                                Charts[3].Series[4].Points.AddXY(axisLabel, 0);
                            }
                            if (date >= market2DayDetail[0].Time && date <= market2DayDetail[market2DayDetail.Count - 1].Time)
                            {
                                Charts[3].Series[3].Points.AddXY(axisLabel, Math.Round((market2DayDetail[m2DDI].Price[3] / market2DayDetail[0].Price[3] - 1) * 100, 0));
                                Charts[3].Series[5].Points.AddXY(axisLabel, market2DayDetail[m2DDI].Ms + market2DayDetail[m2DDI].Md);
                                m2DDI++;
                            }
                            else
                            {
                                Charts[3].Series[3].Points.AddXY(axisLabel, 0);
                                Charts[3].Series[5].Points.AddXY(axisLabel, 0);
                            }
                        }

                        Charts[j].Series[j].Points.AddXY(axisLabel, Charts[3].Series[j].Points.Last().YValues[0]);
                        Charts[j].Series[2].Points.AddXY(axisLabel, Charts[3].Series[2].Points[Charts[3].Series[j].Points.Count - 1].YValues[0]);
                        Charts[j].Series[3].Points.AddXY(axisLabel, Charts[3].Series[3].Points[Charts[3].Series[j].Points.Count - 1].YValues[0]);
                        Charts[j].Series[4].Points.AddXY(axisLabel, Charts[3].Series[4].Points[Charts[3].Series[j].Points.Count - 1].YValues[0]);
                        Charts[j].Series[5].Points.AddXY(axisLabel, Charts[3].Series[5].Points[Charts[3].Series[j].Points.Count - 1].YValues[0]);
                        Charts[j].Series[j + 6].Points.AddXY(axisLabel, Charts[3].Series[j + 6].Points.Last().YValues[0]);
                        Charts[j].Series[j + 8].Points.AddXY(axisLabel, Charts[3].Series[j + 8].Points.Last().YValues[0]);
                        Charts[j].Series[j + 10].Points.AddXY(axisLabel, Charts[3].Series[j + 10].Points.Last().YValues[0]);
                        Charts[j].Series[j + 12].Points.AddXY(axisLabel, Charts[3].Series[j + 12].Points.Last().YValues[0]);
                        Charts[j].Series[j + 14].Points.AddXY(axisLabel, Charts[3].Series[j + 14].Points.Last().YValues[0]);

                        //if (simulDaysDetail[j].Values[i].Count != 0)
                        {
                            Charts2[j + 2].Series[0].Points.AddXY(axisLabel, Charts[3].Series[j].Points.Last().YValues[0]);
                            Charts2[j + 2].Series[1].Points.AddXY(axisLabel, Charts[3].Series[j + 6].Points.Last().YValues[0]);
                            Charts2[j + 2].Series[2].Points.AddXY(axisLabel, Charts[3].Series[j + 8].Points.Last().YValues[0]);
                            Charts2[j + 2].Series[3].Points.AddXY(axisLabel, Charts[3].Series[j + 10].Points.Last().YValues[0]);
                            Charts2[j + 2].Series[4].Points.AddXY(axisLabel, Charts[3].Series[j + 12].Points.Last().YValues[0]);
                            Charts2[j + 2].Series[5].Points.AddXY(axisLabel, Charts[3].Series[j + 14].Points.Last().YValues[0]);
                        }

                        if ((i + 1) % d == 0 || i == endIndex)
                        {
                            if (simulDays[j].Values[di].Count != 0)
                            {
                                simulDays[j].Values[di].WinRate = Math.Round((double)simulDays[j].Values[di].Win / simulDays[j].Values[di].Count * 100, 2);
                                simulDays[j].Values[di].ProfitRateAvg = Math.Round(simulDays[j].Values[di].ProfitRateSum / simulDays[j].Values[di].Count, 2);
                                simulDays[j].Values[di].WinProfitRateAvg = simulDays[j].Values[di].Win == 0 ? 0 : Math.Round(simulDays[j].Values[di].WinProfitRateSum / simulDays[j].Values[di].Win, 2);
                                simulDays[j].Values[di].LoseProfitRateAvg = simulDays[j].Values[di].Count == simulDays[j].Values[di].Win ? 0 :
                                    Math.Round((simulDays[j].Values[di].ProfitRateSum - simulDays[j].Values[di].WinProfitRateSum) / (simulDays[j].Values[di].Count - simulDays[j].Values[di].Win), 2);
                            }
                            else
                            {
                                simulDays[j].Values[di].WinRate = 0;
                                simulDays[j].Values[di].ProfitRateAvg = 0;
                                simulDays[j].Values[di].WinProfitRateAvg = 0;
                                simulDays[j].Values[di].LoseProfitRateAvg = 0;
                            }

                            if (simulDays[j].Values[di].Count != 0)
                            {
                                simulDays[j].Values[di].Number = ++n;
                                metricResultListView.AddObject(simulDays[j].Values[di]);
                            }

                            axisLabel = simulDays[j].Keys[di].ToString(DateTimeFormat);
                            Charts[2].Series[j].Points.AddXY(axisLabel, Math.Round((MetricVars[j].CR - 1) * 100, 0));
                            Charts[2].Series[j + 6].Points.AddXY(axisLabel, simulDays[j].Values[di].ProfitRateAvg);
                            Charts[2].Series[j + 8].Points.AddXY(axisLabel, simulDays[j].Values[di].Count);
                            Charts[2].Series[j + 10].Points.AddXY(axisLabel, simulDays[j].Values[di].WinRate);
                            Charts[2].Series[j + 12].Points.AddXY(axisLabel, simulDays[j].Values[di].WinProfitRateAvg);
                            Charts[2].Series[j + 14].Points.AddXY(axisLabel, simulDays[j].Values[di].LoseProfitRateAvg);

                            if (isAllLongShort != Position.All || j == (int)Position.Long)
                            {
                                var date = simulDays[j].Keys[di];
                                if (date >= market1Day[0].Time && date <= market1Day[market1Day.Count - 1].Time)
                                {
                                    Charts[2].Series[2].Points.AddXY(axisLabel, Math.Round((market1Day[m1DI].Price[3] / market1Day[0].Price[3] - 1) * 100, 0));
                                    Charts[2].Series[4].Points.AddXY(axisLabel, market1Day[m1DI].Ms + market1Day[m1DI].Md);
                                    m1DI++;
                                }
                                else
                                {
                                    Charts[2].Series[2].Points.AddXY(axisLabel, 0);
                                    Charts[2].Series[4].Points.AddXY(axisLabel, 0);
                                }
                                if (date >= market2Day[0].Time && date <= market2Day[market2Day.Count - 1].Time)
                                {
                                    Charts[2].Series[3].Points.AddXY(axisLabel, Math.Round((market2Day[m2DI].Price[3] / market2Day[0].Price[3] - 1) * 100, 0));
                                    Charts[2].Series[5].Points.AddXY(axisLabel, market2Day[m2DI].Ms + market2Day[m2DI].Md);
                                    m2DI++;
                                }
                                else
                                {
                                    Charts[2].Series[3].Points.AddXY(axisLabel, 0);
                                    Charts[2].Series[5].Points.AddXY(axisLabel, 0);
                                }
                            }
                        }
                    }

            for (int j = 0; j < Charts.Length; j++)
                for (int i = 0; i < Charts[j].Series.Count / 2; i++)
                {
                    var ca = Charts[j].ChartAreas[Charts[j].Series[i * 2].ChartArea];

                    var min = 0m;
                    var max = 0m;
                    var min2 = 0m;
                    var max2 = 0m;
                    if (Charts[j].Series[i * 2].Points.Count != 0)
                    {
                        min = (decimal)Charts[j].Series[i * 2].Points.FindMinByValue("Y1").YValues[0];
                        max = (decimal)Charts[j].Series[i * 2].Points.FindMaxByValue("Y1").YValues[0];
                    }
                    if (Charts[j].Series[i * 2 + 1].Points.Count != 0)
                    {
                        min2 = (decimal)Charts[j].Series[i * 2 + 1].Points.FindMinByValue("Y1").YValues[0];
                        max2 = (decimal)Charts[j].Series[i * 2 + 1].Points.FindMaxByValue("Y1").YValues[0];
                    }

                    if (ca.AxisY.Minimum != 0 || ca.AxisY2.Minimum != 0 || ca.AxisY.Maximum != 100 || ca.AxisY2.Maximum != 100)
                    {
                        if ((min != 0 || max != 0) && (min2 != 0 || max2 != 0))
                        {
                            if (min >= 0 && min2 >= 0)
                            {
                                ca.AxisY.Minimum = 0;
                                ca.AxisY2.Minimum = 0;

                                var e = (decimal)Math.Pow(10, (int)Math.Log10((double)max / 4) - 1);
                                var e2 = (decimal)Math.Pow(10, (int)Math.Log10((double)max2 / 4) - 1);

                                ca.AxisY.Maximum = (double)((int)(max / 4 / e) * e * 5);
                                ca.AxisY2.Maximum = (double)((int)(max2 / 4 / e2) * e2 * 5);
                            }
                            else
                            {
                                if (min >= 0 || (int)(max / -min) + 1 > 4)
                                {
                                    var e = (decimal)Math.Pow(10, (int)Math.Log10((double)max / 4) - 1);
                                    ca.AxisY.Minimum = -(double)((int)(max / 4 / e) * e);
                                    ca.AxisY.Maximum = -ca.AxisY.Minimum * 5;
                                }
                                else if (max / -min >= 1)
                                {
                                    var e = (decimal)Math.Pow(10, (int)Math.Log10(-(double)min / 4) - 1);
                                    ca.AxisY.Minimum = -(double)((int)(-min / 4 / e) * e * 5);
                                    ca.AxisY.Maximum = -ca.AxisY.Minimum * ((int)(max / -min) + 1);
                                }
                                else if (max <= 0 || (int)(-min / max) + 1 > 4)
                                {
                                    var e = (decimal)Math.Pow(10, (int)Math.Log10(-(double)min / 4) - 1);
                                    ca.AxisY.Maximum = (double)((int)(-min / 4 / e) * e);
                                    ca.AxisY.Minimum = -ca.AxisY.Maximum * 5;
                                }
                                else
                                {
                                    var e = (decimal)Math.Pow(10, (int)Math.Log10((double)max / 4) - 1);
                                    ca.AxisY.Maximum = (double)((int)(max / 4 / e) * e * 5);
                                    ca.AxisY.Minimum = -ca.AxisY.Maximum * ((int)(min / -max) + 1);
                                }

                                if (min2 >= 0 || (int)(max2 / -min2) + 1 > 4)
                                {
                                    var e2 = (decimal)Math.Pow(10, (int)Math.Log10((double)max2 / 4) - 1);
                                    ca.AxisY2.Minimum = -(double)((int)(max2 / 4 / e2) * e2);
                                    ca.AxisY2.Maximum = -ca.AxisY2.Minimum * 5;
                                }
                                else if (max2 / -min2 >= 1)
                                {
                                    var e2 = (decimal)Math.Pow(10, (int)Math.Log10(-(double)min2 / 4) - 1);
                                    ca.AxisY2.Minimum = -(double)((int)(-min2 / 4 / e2) * e2 * 5);
                                    ca.AxisY2.Maximum = -ca.AxisY2.Minimum * ((int)(max2 / -min2) + 1);
                                }
                                else if (max2 <= 0 || (int)(-min2 / max2) + 1 > 4)
                                {
                                    var e2 = (decimal)Math.Pow(10, (int)Math.Log10(-(double)min2 / 4) - 1);
                                    ca.AxisY2.Maximum = (double)((int)(-min2 / 4 / e2) * e2);
                                    ca.AxisY2.Minimum = -ca.AxisY2.Maximum * 5;
                                }
                                else
                                {
                                    var e2 = (decimal)Math.Pow(10, (int)Math.Log10((double)max2 / 4) - 1);
                                    ca.AxisY2.Maximum = (double)((int)(max2 / 4 / e2) * e2 * 5);
                                    ca.AxisY2.Minimum = -ca.AxisY2.Maximum * ((int)(min2 / -max2) + 1);
                                }
                            }

                            if (ca.AxisY.Minimum == 0 && ca.AxisY2.Minimum == 0)
                            {
                                ca.AxisY.Interval = (ca.AxisY.Maximum - ca.AxisY.Minimum) / 2;
                                ca.AxisY2.Interval = (ca.AxisY2.Maximum - ca.AxisY2.Minimum) / 2;
                                continue;
                            }
                            else if (ca.AxisY.Minimum == 0 && ca.AxisY.Maximum == 0)
                            {
                                ca.AxisY.Minimum = ca.AxisY2.Minimum;
                                ca.AxisY.Maximum = ca.AxisY2.Maximum;
                            }
                            else if (ca.AxisY2.Minimum == 0 && ca.AxisY2.Maximum == 0)
                            {
                                ca.AxisY2.Minimum = ca.AxisY.Minimum;
                                ca.AxisY2.Maximum = ca.AxisY.Maximum;
                            }

                            if (ca.AxisY.Minimum == 0)
                                ca.AxisY.Minimum = ca.AxisY.Maximum * ca.AxisY2.Minimum / ca.AxisY2.Maximum;
                            else if (ca.AxisY2.Minimum == 0)
                                ca.AxisY2.Minimum = ca.AxisY2.Maximum * ca.AxisY.Minimum / ca.AxisY.Maximum;
                            else if (ca.AxisY.Maximum / -ca.AxisY.Minimum > ca.AxisY2.Maximum / -ca.AxisY2.Minimum)
                                ca.AxisY2.Maximum = ca.AxisY2.Minimum * ca.AxisY.Maximum / ca.AxisY.Minimum;
                            else
                                ca.AxisY.Maximum = ca.AxisY.Minimum * ca.AxisY2.Maximum / ca.AxisY2.Minimum;
                        }
                        else if (min != 0 || max != 0)
                        {
                            if (min >= 0)
                            {
                                ca.AxisY.Minimum = 0;

                                var e = (decimal)Math.Pow(10, (int)Math.Log10((double)max / 4) - 1);

                                ca.AxisY.Maximum = (double)((int)(max / 4 / e) * e * 5);
                            }
                            else if (max <= 0)
                            {
                                ca.AxisY.Maximum = 0;

                                var e = (decimal)Math.Pow(10, (int)Math.Log10((double)-min / 4) - 1);

                                ca.AxisY.Minimum = -(double)((int)(-min / 4 / e) * e * 5);
                            }
                            else if ((int)(max / -min) + 1 > 4)
                            {
                                var e = (decimal)Math.Pow(10, (int)Math.Log10((double)max / 4) - 1);
                                ca.AxisY.Minimum = -(double)((int)(max / 4 / e) * e);
                                ca.AxisY.Maximum = -ca.AxisY.Minimum * 5;
                            }
                            else if (max / -min >= 1)
                            {
                                var e = (decimal)Math.Pow(10, (int)Math.Log10(-(double)min / 4) - 1);
                                ca.AxisY.Minimum = -(double)((int)(-min / 4 / e) * e * 5);
                                ca.AxisY.Maximum = -ca.AxisY.Minimum * ((int)(max / -min) + 1);
                            }
                            else if ((int)(-min / max) + 1 > 4)
                            {
                                var e = (decimal)Math.Pow(10, (int)Math.Log10(-(double)min / 4) - 1);
                                ca.AxisY.Maximum = (double)((int)(-min / 4 / e) * e);
                                ca.AxisY.Minimum = -ca.AxisY.Maximum * 5;
                            }
                            else
                            {
                                var e = (decimal)Math.Pow(10, (int)Math.Log10((double)max / 4) - 1);
                                ca.AxisY.Maximum = (double)((int)(max / 4 / e) * e * 5);
                                ca.AxisY.Minimum = -ca.AxisY.Maximum * ((int)(min / -max) + 1);
                            }

                            ca.AxisY2.Minimum = ca.AxisY.Minimum;
                            ca.AxisY2.Maximum = ca.AxisY.Maximum;
                        }
                        else if (min2 != 0 || max2 != 0)
                        {
                            if (min2 >= 0)
                            {
                                ca.AxisY2.Minimum = 0;

                                var e = (decimal)Math.Pow(10, (int)Math.Log10((double)max2 / 4) - 1);

                                ca.AxisY2.Maximum = (double)((int)(max2 / 4 / e) * e * 5);
                            }
                            else if (max2 <= 0)
                            {
                                ca.AxisY2.Maximum = 0;

                                var e = (decimal)Math.Pow(10, (int)Math.Log10((double)-min2 / 4) - 1);

                                ca.AxisY2.Minimum = -(double)((int)(-min2 / 4 / e) * e * 5);
                            }
                            else if ((int)(max2 / -min2) + 1 > 4)
                            {
                                var e = (decimal)Math.Pow(10, (int)Math.Log10((double)max2 / 4) - 1);
                                ca.AxisY2.Minimum = -(double)((int)(max2 / 4 / e) * e);
                                ca.AxisY2.Maximum = -ca.AxisY2.Minimum * 5;
                            }
                            else if (max2 / -min2 >= 1)
                            {
                                var e = (decimal)Math.Pow(10, (int)Math.Log10(-(double)min2 / 4) - 1);
                                ca.AxisY2.Minimum = -(double)((int)(-min2 / 4 / e) * e * 5);
                                ca.AxisY2.Maximum = -ca.AxisY2.Minimum * ((int)(max2 / -min2) + 1);
                            }
                            else if ((int)(-min2 / max2) + 1 > 4)
                            {
                                var e = (decimal)Math.Pow(10, (int)Math.Log10(-(double)min2 / 4) - 1);
                                ca.AxisY2.Maximum = (double)((int)(-min2 / 4 / e) * e);
                                ca.AxisY2.Minimum = -ca.AxisY2.Maximum * 5;
                            }
                            else
                            {
                                var e = (decimal)Math.Pow(10, (int)Math.Log10((double)max2 / 4) - 1);
                                ca.AxisY2.Maximum = (double)((int)(max2 / 4 / e) * e * 5);
                                ca.AxisY2.Minimum = -ca.AxisY2.Maximum * ((int)(min2 / -max2) + 1);
                            }

                            ca.AxisY.Minimum = ca.AxisY2.Minimum;
                            ca.AxisY.Maximum = ca.AxisY2.Maximum;
                        }
                        else
                            continue;
                    }

                    ca.AxisY.Interval = (ca.AxisY.Maximum - ca.AxisY.Minimum) / 2;
                    ca.AxisY2.Interval = (ca.AxisY2.Maximum - ca.AxisY2.Minimum) / 2;
                }

            foreach (BackItemData itemData in itemDataDic.Values)
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
                var CR = "";
                var SingleWinRate = "";
                var AllCount = "";
                var DisappearCount = MetricVars[i].disappearCount.ToString("#,0");
                var LastDisappearCount = MetricVars[i].lastDisappearCount.ToString("#,0");
                var AvgProfitRate = "";
                var WinAvgProfitRate = "";
                var LoseAvgProfitRate = "";
                var AverageWinRate = "";
                var AverageAllCount = "";
                var GeoMeanProfitRate = "";
                var AverageAvgProfitRate = "";
                var AverageWinAvgProfitRate = "";
                var AverageLoseAvgProfitRate = "";
                var Commision = "";
                var Slippage = "";
                var MDD = "";
                var MDDDays = "";
                var MDDStart = "";
                var MDDLow = "";
                var MDDEnd = "";
                var LDD = "";
                var LDDDays = "";
                var LDDStart = "";
                var LDDLow = "";
                var LDDEnd = "";
                var DayMaxHas = "";
                var DayMaxHasDay = "";
                var LongestHasTime = "";
                var LongestHasTimeCode = "";
                var LongestHasTimeStart = "";
                var MinKelly = "";
                var MaxKelly = "";
                if (MetricVars[i].SingleCount != 0)
                {
                    CR = Math.Round((MetricVars[i].CR - 1) * 100, 0).ToString("#,0") + "%";

                    SingleWinRate = Math.Round((double)MetricVars[i].SingleWin / MetricVars[i].SingleCount * 100, 1) + "%";
                    AllCount = MetricVars[i].SingleCount.ToString("#,0");
                    AvgProfitRate = Math.Round(MetricVars[i].SingleProfitRateSum / MetricVars[i].SingleCount, 2) + "%";
                    WinAvgProfitRate = MetricVars[i].SingleWin == 0 ? "0%" : (Math.Round(MetricVars[i].SingleProfitWinRateSum / MetricVars[i].SingleWin, 2) + "%");
                    LoseAvgProfitRate = MetricVars[i].SingleCount == MetricVars[i].SingleWin ? "0%" : (Math.Round((MetricVars[i].SingleProfitRateSum - MetricVars[i].SingleProfitWinRateSum) / (MetricVars[i].SingleCount - MetricVars[i].SingleWin), 2) + "%");
                    Commision = Math.Round(commisionRate, 2) + "%";
                    Slippage = Math.Round(slippage, 2) + "%";

                    MDD = Math.Round((MetricVars[i].MDD.LowestCumulativeReturn / MetricVars[i].MDD.HighestCumulativeReturn - 1) * 100, 0) + "%";
                    MDDDays = MetricVars[i].MDD.LastCumulativeReturnIndex - MetricVars[i].MDD.HighestCumulativeReturnIndex + " days";
                    MDDStart = simulDays[i].Keys[MetricVars[i].MDD.HighestCumulativeReturnIndex].ToString(DateTimeFormat);
                    MDDLow = simulDays[i].Keys[MetricVars[i].MDD.LowestCumulativeReturnIndex].ToString(DateTimeFormat);
                    MDDEnd = simulDays[i].Keys[MetricVars[i].MDD.LastCumulativeReturnIndex].ToString(DateTimeFormat);
                    LDD = Math.Round((MetricVars[i].LDD.LowestCumulativeReturn / MetricVars[i].LDD.HighestCumulativeReturn - 1) * 100, 0) + "%";
                    LDDDays = MetricVars[i].LDD.LastCumulativeReturnIndex - MetricVars[i].LDD.HighestCumulativeReturnIndex + " days";
                    LDDStart = simulDays[i].Keys[MetricVars[i].LDD.HighestCumulativeReturnIndex].ToString(DateTimeFormat);
                    LDDLow = simulDays[i].Keys[MetricVars[i].LDD.LowestCumulativeReturnIndex].ToString(DateTimeFormat);
                    LDDEnd = simulDays[i].Keys[MetricVars[i].LDD.LastCumulativeReturnIndex].ToString(DateTimeFormat);

                    DayMaxHas = MetricVars[i].highestHasItemsAtADay.ToString("#,0");
                    DayMaxHasDay = MetricVars[i].highestHasItemsDate.ToString(DateTimeFormat);

                    LongestHasTime = MetricVars[i].longestHasTime.ToString(TimeSpanFormat);
                    LongestHasTimeCode = MetricVars[i].longestHasCode;
                    LongestHasTimeStart = MetricVars[i].longestHasTimeStart.ToString(TimeFormat);

                    MinKelly = Math.Round(MetricVars[i].minKelly, 2) + "(" + Math.Round(MetricVars[i].lowestKelly, 2) + ")";
                    MaxKelly = Math.Round(MetricVars[i].maxKelly, 2) + "(" + Math.Round(MetricVars[i].highestKelly, 2) + ")";
                }
                if (MetricVars[i].AverageCount != 0)
                {
                    AverageWinRate = Math.Round((double)MetricVars[i].AverageWin / MetricVars[i].AverageCount * 100, 1) + "%";
                    AverageAllCount = MetricVars[i].AverageCount.ToString("#,0");
                    GeoMeanProfitRate = Math.Round((Math.Pow(MetricVars[i].AverageProfitRateMul, (double)1 / MetricVars[i].AverageCount) - 1) * 100, 2) + "%";
                    AverageAvgProfitRate = Math.Round(MetricVars[i].AverageProfitRateSum / MetricVars[i].AverageCount, 2) + "%";
                    AverageWinAvgProfitRate = MetricVars[i].AverageWin == 0 ? "0%" : (Math.Round(MetricVars[i].AverageProfitWinRateSum / MetricVars[i].AverageWin, 2) + "%");
                    AverageLoseAvgProfitRate = MetricVars[i].AverageCount == MetricVars[i].AverageWin ? "0%" : (Math.Round((MetricVars[i].AverageProfitRateSum - MetricVars[i].AverageProfitWinRateSum) / (MetricVars[i].AverageCount - MetricVars[i].AverageWin), 2) + "%");
                }

                var i2 = i;
                if (isAllLongShort == Position.All || i == (int)isAllLongShort)
                    que.Enqueue(new Task(() =>
                    {
                        try
                        {
                            var columnDic = new Dictionary<string, string>();
                            var isJooName = "isJoo";
                            var isFuturesName = "isFutures";
                            var strategyName = "strategy";
                            var CRName = "CR";
                            var isLongName = "isLong";
                            var start_dayName = "start_day";
                            var end_dayName = "end_day";
                            var daysName = "days";
                            var Cumulative_ReturnName = "Cumulative_Return";
                            var Win_RateName = "Win_Rate";
                            var CountName = "Count";
                            var DisappearName = "Disappear";
                            var LastDisappearName = "LastDisappear";
                            var Average_Profit_RateName = "Average_Profit_Rate";
                            var Win_APRName = "Win_APR";
                            var Lose_APRName = "Lose_APR";
                            var CommisionName = "Commision";
                            var SlippageName = "Slippage";
                            var Max_Draw_DownName = "Max_Draw_Down";
                            var MDD_DaysName = "MDD_Days";
                            var MDD_Start_DayName = "MDD_Start_Day";
                            var MDD_Low_DayName = "MDD_Low_Day";
                            var MDD_End_DayName = "MDD_End_Day";
                            var Longest_Draw_DownName = "Longest_Draw_Down";
                            var LDD_DaysName = "LDD_Days";
                            var LDD_Start_DayName = "LDD_Start_Day";
                            var LDD_Low_DayName = "LDD_Low_Day";
                            var LDD_End_DayName = "LDD_End_Day";
                            var Day_Max_HasName = "Day_Max_Has";
                            var DMH_DayName = "DMH_Day";
                            var Longest_Has_TimeName = "Longest_Has_Time";
                            var LHT_CodeName = "LHT_Code";
                            var LHT_StartName = "LHT_Start";
                            var Min_KellyName = "Min_Kelly_R";
                            var Max_KellyName = "Max_Kelly_R";
                            var ImageName = "Image";
                            var test_timeName = "test_time";
                            var threadName = "thread";
                            var test_spend_timeName = "test_spend_time";

                            columnDic.Add(isJooName, "TEXT");
                            columnDic.Add(isFuturesName, "TEXT");
                            columnDic.Add(strategyName, "INTEGER");
                            columnDic.Add(CRName, "TEXT");
                            columnDic.Add(isLongName, "TEXT");
                            columnDic.Add(start_dayName, "TEXT");
                            columnDic.Add(end_dayName, "TEXT");
                            columnDic.Add(daysName, "TEXT");
                            columnDic.Add(Cumulative_ReturnName, "TEXT");
                            columnDic.Add(Win_RateName, "TEXT");
                            columnDic.Add(CountName, "INTEGER");
                            columnDic.Add(DisappearName, "INTEGER");
                            columnDic.Add(LastDisappearName, "INTEGER");
                            columnDic.Add(Average_Profit_RateName, "TEXT");
                            columnDic.Add(Win_APRName, "TEXT");
                            columnDic.Add(Lose_APRName, "TEXT");
                            columnDic.Add(CommisionName, "TEXT");
                            columnDic.Add(SlippageName, "TEXT");
                            columnDic.Add(Max_Draw_DownName, "TEXT");
                            columnDic.Add(MDD_DaysName, "TEXT");
                            columnDic.Add(MDD_Start_DayName, "TEXT");
                            columnDic.Add(MDD_Low_DayName, "TEXT");
                            columnDic.Add(MDD_End_DayName, "TEXT");
                            columnDic.Add(Longest_Draw_DownName, "TEXT");
                            columnDic.Add(LDD_DaysName, "TEXT");
                            columnDic.Add(LDD_Start_DayName, "TEXT");
                            columnDic.Add(LDD_Low_DayName, "TEXT");
                            columnDic.Add(LDD_End_DayName, "TEXT");
                            columnDic.Add(Day_Max_HasName, "INTEGER");
                            columnDic.Add(DMH_DayName, "TEXT");
                            columnDic.Add(Longest_Has_TimeName, "TEXT");
                            columnDic.Add(LHT_CodeName, "TEXT");
                            columnDic.Add(LHT_StartName, "TEXT");
                            columnDic.Add(Min_KellyName, "TEXT");
                            columnDic.Add(Max_KellyName, "TEXT");
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
                            columnDic[CRName] = "'" + CRType.ToString() + "'";
                            columnDic[isLongName] = "'" + Enum.GetName(typeof(Position), i2).ToString() + "'";
                            columnDic[start_dayName] = "'" + start.ToString(DateTimeFormat) + "'";
                            columnDic[end_dayName] = "'" + end.ToString(DateTimeFormat) + "'";
                            columnDic[daysName] = "'" + Math.Round(end.Subtract(start).TotalDays, 0) + " days'";
                            columnDic[Cumulative_ReturnName] = "'" + CR + "'";
                            columnDic[Win_RateName] = "'" + SingleWinRate + "'";
                            columnDic[CountName] = "'" + AllCount + "'";
                            columnDic[DisappearName] = "'" + DisappearCount + "'";
                            columnDic[LastDisappearName] = "'" + LastDisappearCount + "'";
                            columnDic[Average_Profit_RateName] = "'" + AvgProfitRate + "'";
                            columnDic[Win_APRName] = "'" + WinAvgProfitRate + "'";
                            columnDic[Lose_APRName] = "'" + LoseAvgProfitRate + "'";
                            columnDic[CommisionName] = "'" + Commision + "'";
                            columnDic[SlippageName] = "'" + Slippage + "'";
                            columnDic[Max_Draw_DownName] = "'" + MDD + "'";
                            columnDic[MDD_DaysName] = "'" + MDDDays + "'";
                            columnDic[MDD_Start_DayName] = "'" + MDDStart + "'";
                            columnDic[MDD_Low_DayName] = "'" + MDDLow + "'";
                            columnDic[MDD_End_DayName] = "'" + MDDEnd + "'";
                            columnDic[Longest_Draw_DownName] = "'" + LDD + "'";
                            columnDic[LDD_DaysName] = "'" + LDDDays + "'";
                            columnDic[LDD_Start_DayName] = "'" + LDDStart + "'";
                            columnDic[LDD_Low_DayName] = "'" + LDDLow + "'";
                            columnDic[LDD_End_DayName] = "'" + LDDEnd + "'";
                            columnDic[Day_Max_HasName] = "'" + DayMaxHas + "'";
                            columnDic[DMH_DayName] = "'" + DayMaxHasDay + "'";
                            columnDic[Longest_Has_TimeName] = "'" + LongestHasTime + "'";
                            columnDic[LHT_CodeName] = "'" + LongestHasTimeCode + "'";
                            columnDic[LHT_StartName] = "'" + LongestHasTimeStart + "'";
                            columnDic[Min_KellyName] = "'" + MinKelly + "'";
                            columnDic[Max_KellyName] = "'" + MaxKelly + "'";
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
                            columnDic2.Add(strategyName, columnDic[strategyName]);
                            columnDic2.Add(CRName, columnDic[CRName]);
                            columnDic2.Add(start_dayName, columnDic[start_dayName]);
                            columnDic2.Add(end_dayName, columnDic[end_dayName]);
                            columnDic2.Add(isLongName, columnDic[isLongName]);
                            columnDic2.Add(isJooName, columnDic[isJooName]);
                            columnDic2.Add(isFuturesName, columnDic[isFuturesName]);
                            columnDic2.Add(Min_KellyName, columnDic[Min_KellyName]);
                            columnDic2.Add(Max_KellyName, columnDic[Max_KellyName]);
                            columnDic2.Add(CommisionName, columnDic[CommisionName]);
                            columnDic2.Add(SlippageName, columnDic[SlippageName]);

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

                            //try
                            //{
                            //    if (isAllLongShort != Position.All || i2 == (int)Position.Long)
                            //    {
                            //        if (isAllLongShort == Position.All)
                            //            columnDic2[isLongName] = "'All'";

                            //        count = 0;
                            //        foreach (var column in columnDic2)
                            //        {
                            //            if (count == 0)
                            //                statement = "";
                            //            else
                            //                statement += ", ";

                            //            statement += column.Key + "=" + column.Value;

                            //            count++;
                            //        }

                            //        image.Save(STResultDBPath + statement + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            //    }
                            //}
                            //catch (Exception e)
                            //{

                            //    throw;
                            //}
                        }
                        catch (Exception e)
                        {
                            throw;
                        }
                    }));

                metricDic[MetricCR].SetText(i, CR);

                metricDic[MetricSingleWinRate].SetText(i, SingleWinRate);
                metricDic[MetricSingleAllCount].SetText(i, AllCount);
                metricDic[MetricDisappear].SetText(i, DisappearCount);
                metricDic[MetricLastDisappear].SetText(i, LastDisappearCount);
                metricDic[MetricSingleAvgProfitRate].SetText(i, AvgProfitRate);
                metricDic[MetricSingleWinAvgProfitRate].SetText(i, WinAvgProfitRate);
                metricDic[MetricSingleLoseAvgProfitRate].SetText(i, LoseAvgProfitRate);
                metricDic[MetricCommision].SetText(i, Commision);
                metricDic[MetricSlippage].SetText(i, Slippage);
                metricDic[MetricAverageWinRate].SetText(i, AverageWinRate);
                metricDic[MetricAverageAllCount].SetText(i, AverageAllCount);
                metricDic[MetricGeoMeanProfitRate].SetText(i, GeoMeanProfitRate);
                metricDic[MetricAverageAvgProfitRate].SetText(i, AverageAvgProfitRate);
                metricDic[MetricAverageWinAvgProfitRate].SetText(i, AverageWinAvgProfitRate);
                metricDic[MetricAverageLoseAvgProfitRate].SetText(i, AverageLoseAvgProfitRate);

                metricDic[MetricMDD].SetText(i, MDD);
                metricDic[MetricMDDDays].SetText(i, MDDDays);
                metricDic[MetricMDDStart].SetText(i, MDDStart);
                metricDic[MetricMDDLow].SetText(i, MDDLow);
                metricDic[MetricMDDEnd].SetText(i, MDDEnd);
                metricDic[MetricLDD].SetText(i, LDD);
                metricDic[MetricLDDDays].SetText(i, LDDDays);
                metricDic[MetricLDDStart].SetText(i, LDDStart);
                metricDic[MetricLDDLow].SetText(i, LDDLow);
                metricDic[MetricLDDEnd].SetText(i, LDDEnd);

                metricDic[MetricDayMaxHas].SetText(i, DayMaxHas);
                metricDic[MetricDayMaxHasDay].SetText(i, DayMaxHasDay);

                metricDic[MetricLongestHasTime].SetText(i, LongestHasTime);
                metricDic[MetricLongestHasTimeCode].SetText(i, LongestHasTimeCode);
                metricDic[MetricLongestHasTimeStart].SetText(i, LongestHasTimeStart);

                metricDic[MetricMinKelly].SetText(i, MinKelly);
                metricDic[MetricMaxKelly].SetText(i, MaxKelly);
            }

            metricListView.Refresh();

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
                GC.Collect();
                GC.WaitForPendingFinalizers();

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
                        RunMain(start, end, Position.All, CRType);
                    }
                    else
                        form.BeginInvoke(new Action(() => { form.Text += "done"; }));
                }

                if (false && isAllLongShort == Position.All)
                {
                    form.BeginInvoke(new Action(() => { runLongButton.PerformClick(); }));

                    Thread.Sleep(3000);

                    form.BeginInvoke(new Action(() => { runShortButton.PerformClick(); }));

                    Thread.Sleep(3000);

                    //form.BeginInvoke(new Action(() => {
                    //    fromTextBox.Text = DateTime.MinValue.ToString(DateTimeFormat);
                    //    toTextBox.Text = "2021-04-29";
                    //    runShortButton.PerformClick(); 
                    //}));

                    //Thread.Sleep(3000);

                    //form.BeginInvoke(new Action(() => {
                    //    fromTextBox.Text = "2021-04-29";
                    //    toTextBox.Text = "2022-01-22";
                    //    runShortButton.PerformClick();
                    //}));
                }
            }));
            #endregion
        }
        double CalculateKelly(List<double> list, double minKelly, double maxKelly)
        {
            if (list.Count <= 1)
                return minKelly;

            var beforeKelly = minKelly + 0.01;
            var beforeGeoMean = double.MinValue;
            var beforeKelly2 = minKelly + 0.02;
            var beforeGeoMean2 = double.MinValue;
            var kelly = minKelly;
            var geoMean = 1D;
            while (true)
            {
                for (int i = 0; i < list.Count; i++)
                    geoMean *= 1 + list[i] * kelly / 100;

                if (geoMean == beforeGeoMean)
                    ShowError(form);
                else if (geoMean > beforeGeoMean)
                {
                    if (beforeGeoMean != double.MinValue)
                    {
                        if (kelly > beforeKelly)
                        {
                            if (kelly >= maxKelly)
                                return maxKelly;
                        }
                        else
                        {
                            if (kelly <= minKelly)
                                return minKelly;
                        }
                    }

                    beforeKelly2 = beforeKelly;
                    beforeGeoMean2 = beforeGeoMean;
                    beforeKelly = kelly;
                    beforeGeoMean = geoMean;
                    kelly = beforeKelly - (beforeKelly2 - beforeKelly);
                    geoMean = 1D;
                }
                else if (beforeGeoMean2 == double.MinValue)
                {
                    var kelly2 = beforeKelly2;

                    beforeKelly2 = kelly;
                    beforeGeoMean2 = geoMean;

                    kelly = kelly2;
                    geoMean = 1D;
                }
                else
                    return beforeKelly;
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
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

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
                        foreach (BackItemData itemData2 in itemDataDic.Values)
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
                        ShowChart(itemData, (result.foundTime, chartViewSticksSize / 2, true), true);
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
                AlertStart("done : " + sw.Elapsed.ToString(TimeSpanFormat), Settings.settings[Settings.ProgramName].others[Settings.AlertSoundName]);
            }));
        }

        public override void SetChartNowOrLoad(ChartValues chartValues)
        {
            mainChart.Visible = true;
            for (int i = 0; i < Charts.Length; i ++)
            {
                Charts[i].Visible = false;
                Buttons[i].BackColor = ColorSet.Button;
            }
            TimeCountChart.Visible = false;
            TimeCountChartButton.BackColor = ColorSet.Button;

            var from = mainChart.Tag != null ? GetStandardDate() : default;
            var position = double.IsNaN(mainChart.ChartAreas[0].CursorX.Position) ? chartViewSticksSize / 2
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
        void ShowChart(BackItemData itemData, (DateTime time, int position, bool on) cursor, bool loaded = false, ChartValues chartValues = default)
        {
            if (chartValues == default)
                chartValues = mainChart.Tag as ChartValues;
            showingItemData = itemData;
            form.Text = itemData.Code;
            var v = itemData.listDic[chartValues];
            cursor.time = cursor.time.AddSeconds(-(int)cursor.time.TimeOfDay.TotalSeconds % chartValues.seconds);

            if (!loaded)
            {
                var more = chartViewSticksSize - cursor.position + chartViewSticksSize / 2;
                LoadAndCheckSticks(itemData, true, true, chartViewSticksSize * 2,
                    chartValues != BaseChartTimeSet.OneMonth ? cursor.time.AddSeconds(more * chartValues.seconds) : cursor.time.AddMonths(more),
                    chartValues);
            }
            else
            {
                if (cursor.time < v.list[0].Time)
                    LoadAndCheckSticks(itemData, false, true, (int)v.list[0].Time.Subtract(cursor.time).TotalSeconds / chartValues.seconds + chartViewSticksSize);
                else if (cursor.time > v.list[v.list.Count - 1].Time)
                    LoadAndCheckSticks(itemData, false, true, (int)cursor.time.Subtract(v.list[v.list.Count - 1].Time).TotalSeconds / chartValues.seconds + chartViewSticksSize);

                if (cursor.time.Subtract(v.list[0].Time).TotalSeconds / chartValues.seconds < chartViewSticksSize)
                    LoadAndCheckSticks(itemData, false, true);
                if (v.list[v.list.Count - 1].Time.Subtract(cursor.time).TotalSeconds / chartValues.seconds < chartViewSticksSize)
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

                if (v.list.Count > right + chartViewSticksSize)
                    v.list.RemoveRange(right + chartViewSticksSize, v.list.Count - (right + chartViewSticksSize));
                if (left > chartViewSticksSize)
                    v.list.RemoveRange(0, left - chartViewSticksSize);
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
            ZoomX(mainChart, zoomStart, zoomStart + chartViewSticksSize);

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
                for (int i = BaseChartTimeSet.OneMinute.index; i <= maxCV.index; i++)
                {
                    //if (i != BaseChartTimeSet.OneMinute.index && i < minCV.index)
                    //    continue;

                    var v = itemData.listDic.Values[i];
                    var vc = itemData.listDic.Keys[i];
                    var from2 = toPast ? from : from.AddSeconds(-vc.seconds * (TotalNeedDays - 1));
                    if (i == BaseChartTimeSet.OneMinute.index)
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
                            var size2 = minituesInADay * BaseChartTimeSet.OneMinute.seconds / vc.seconds * (i - BaseChartTimeSet.OneMinute.index + 1) + (TotalNeedDays - 1);
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

                    for (int j = BaseChartTimeSet.OneMinute.index; j <= maxCV.index; j++)
                    {
                        var v = itemData.listDic.Values[j];
                        var vc = itemData.listDic.Keys[j];
                        if (j != BaseChartTimeSet.OneMinute.index)
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

                        if (!calLater && (j == BaseChartTimeSet.OneMinute.index || !calOnlyFullStick))
                            SetRSIAandDiff(v.list, v.lastStick, v.currentIndex - 1);

                        if (toPast ? vm.lastStick.Time <= checkStartTime : vm.lastStick.Time >= checkStartTime)
                            OneChartFindConditionAndAdd(itemData, vc, vm.lastStick, v.lastStick, vm.currentIndex - 1, v.currentIndex - 1);
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
                        else if (positionData.Enter)
                        {
                            var v = itemData.listDic[positionData.EnterFoundForExit.chartValues];
                            if (ExitConditionFinal(itemData, (Position)j, vm.lastStick, v.lastStick, v.currentIndex - 1))
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
                        for (int i = BaseChartTimeSet.OneMinute.index; i <= maxCV.index; i++)
                        {
                            if (i != BaseChartTimeSet.OneMinute.index && i < minCV.index)
                                continue;

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

                    for (int i = BaseChartTimeSet.OneMinute.index; i <= maxCV.index; i++)
                    {
                        //if (i != BaseChartTimeSet.OneMinute.index && i < minCV.index)
                        //    continue;

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
        void Run(DateTime start, DateTime end, CR CRType)
        {
            if (TestAll)
                threadN = 3;
            else
                threadN = 6;

            var simulStartTime = calSimul ? start.Subtract(ReadyTimeToCheckBeforeStart) : start;
            var from = simulStartTime.Date;
            var size = (int)end.Date.AddDays(1).Subtract(from).TotalSeconds / BaseChartTimeSet.OneMinute.seconds;

            AddOrChangeLoadingText("Simulating ST" + ST + " ...(" + from.ToString(DateTimeFormat) + ")", true);

            foreach (BackItemData itemData in itemDataDic.Values)
            {
                itemData.Reset();
                itemData.BeforeExitTime = start;

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

                for (int j = BaseChartTimeSet.OneMinute.index; j <= maxCV.index; j++)
                {
                    try
                    {
                        if (j != BaseChartTimeSet.OneMinute.index && j < minCV.index)
                            continue;

                        var v = itemData.listDic.Values[j];
                        var vc = itemData.listDic.Keys[j];

                        if (i % minituesInADay == 0)
                        {
                            if (v.list.Count == 0)
                            {
                                if (itemData.firstLastMin.firstMin < from2.AddMinutes(minituesInADay))
                                {
                                    v.list = LoadSticks(itemData, vc, from2.AddSeconds(-vc.seconds * (TotalNeedDays - 1)),
                                        minituesInADay * BaseChartTimeSet.OneMinute.seconds / vc.seconds * (j - BaseChartTimeSet.OneMinute.index + 1) + (TotalNeedDays - 1), false);
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
                                    v.list.AddRange(LoadSticks(itemData, vc, from2, minituesInADay * BaseChartTimeSet.OneMinute.seconds / vc.seconds * (j - BaseChartTimeSet.OneMinute.index + 1), false));
                                }
                            }
                        }

                        if (itemData.firstLastMin.firstMin > from2)
                            continue;

                        if (j != BaseChartTimeSet.OneMinute.index)
                        {
                            var timeDiff = from2.Subtract(v.lastStick.Time).TotalSeconds;
                            if (timeDiff >= vc.seconds)
                            {
                                if (vm.list[vm.currentIndex - 1].Time.Subtract(v.lastStick.Time).TotalSeconds >= vc.seconds)
                                    ShowError(form);

                                if (!BackTradeStick.isEqual(v.lastStick as BackTradeStick, v.list[v.currentIndex] as BackTradeStick)
                                    && (itemData.Code != "BTCUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2019-09-24")
                                    && (itemData.Code != "ETHUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2019-12-11")
                                    && (itemData.Code != "XRPUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2020-01-16")
                                    && (itemData.Code != "XRPUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2020-01-17")
                                    && (itemData.Code != "EOSUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2020-01-19")
                                    && (itemData.Code != "PEOPLEUSDT" || v.lastStick.Time.ToString(DateTimeFormat) != "2022-04-18")
                                    && (v.lastStick.Time.ToString(DateTimeFormat) != "2021-01-12")
                                    && (v.lastStick.Time.ToString(DateTimeFormat) != "2021-05-15"))
                                {
                                    ShowError(form);
                                    BackTradeStick.isEqual(v.lastStick as BackTradeStick, v.list[v.currentIndex] as BackTradeStick);
                                }

                                SetRSIAandDiff(v.list, v.list[v.currentIndex], v.currentIndex - 1);

                                v.currentIndex++;

                                if (v.currentIndex == v.list.Count)
                                    continue;

                                v.lastStick = new BackTradeStick() { Time = v.list[v.currentIndex].Time };

                                if (from2 < v.lastStick.Time || from2 >= v.lastStick.Time.AddSeconds(vc.seconds))
                                    ShowError(form);
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
                            if (from2 > v.lastStick.Time)
                                v.currentIndex++;

                            v.lastStick = v.list[v.currentIndex] as BackTradeStick;

                            if (v.lastStick.Time > from2)
                                break;
                            else if (v.lastStick.Time != from2)
                                ShowError(form);
                        }

                        if (!calLater && (j == BaseChartTimeSet.OneMinute.index || !calOnlyFullStick))
                            SetRSIAandDiff(v.list, v.lastStick, v.currentIndex - 1);

                        OneChartFindConditionAndAdd(itemData, vc, vm.lastStick, v.lastStick, vm.currentIndex - 1, v.currentIndex - 1);
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
                    if ((canLStogether ? !positionData.Enter : (!itemData.positionData[(int)Position.Long].Enter && !itemData.positionData[(int)Position.Short].Enter)) && positionData.found)
                        lock (foundLocker)
                            foundItemList[j].Add(itemData.number, (itemData, positionData.foundList));
                    else if (positionData.Enter)
                    {
                        var v = itemData.listDic[positionData.EnterFoundForExit.chartValues];
                        if (ExitConditionFinal(itemData, (Position)j, vm.lastStick, v.lastStick, v.currentIndex - 1) || itemData.firstLastMin.lastMin == from2)
                        {
                            positionData.Enter = false;

                            var profitRow = (double)((Position)j == Position.Long ? vm.lastStick.Price[3] / positionData.EnterPrice : positionData.EnterPrice / vm.lastStick.Price[3]);
                            var resultData = new BackResultData()
                            {
                                Code = itemData.Code,
                                EnterTime = positionData.EnterTime,
                                ExitTime = vm.lastStick.Time,
                                ProfitRate = Math.Round((profitRow - 1) * 100, 2),
                                Duration = vm.lastStick.Time.Subtract(positionData.EnterTime).ToString(TimeSpanFormat),
                                BeforeGap = positionData.EnterTime.Subtract(itemData.BeforeExitTime).ToString(TimeSpanFormat),
                                LorS = (Position)j,
                                EnterMarketLastMin = positionData.EnterMarketLastMin,
                                EnterMarketLastMins = positionData.EnterMarketLastMins
                            };

                            if (itemData.firstLastMin.lastMin != from2)
                            {
                                if (!inside)
                                {
                                    if (itemData.resultDataForMetric[j] != null)
                                    {
                                        if (itemData.resultDataForMetric[j].Code == itemData.Code)
                                            itemData.resultDataForMetric[j].ExitTime = resultData.ExitTime;
                                        lock (itemData.resultDataForMetric[j].locker)
                                        {
                                            itemData.resultDataForMetric[j].Count++;
                                            itemData.resultDataForMetric[j].ProfitRate += profitRow;
                                            if ((profitRow - 1) * 100 > commisionRate + slippage)
                                            {
                                                itemData.resultDataForMetric[j].WinCount++;
                                                itemData.resultDataForMetric[j].WinProfitRateSum += profitRow;
                                            }
                                            itemData.resultDataForMetric[j].doneResults.Add(resultData);
                                            itemData.resultDataForMetric[j].ingItems.Remove(itemData.Code);
                                        }
                                        itemData.resultDataForMetric[j] = null;
                                    }

                                    if (!calSimul || positionData.Real)
                                    {
                                        if (itemData.resultDataForMetricReal[j] != null)
                                        {
                                            if (itemData.resultDataForMetricReal[j].Code == itemData.Code)
                                                itemData.resultDataForMetricReal[j].ExitTime = resultData.ExitTime;
                                            lock (itemData.resultDataForMetricReal[j].locker)
                                            {
                                                itemData.resultDataForMetricReal[j].Count++;
                                                itemData.resultDataForMetricReal[j].ProfitRate += profitRow;
                                                if ((profitRow - 1) * 100 > commisionRate + slippage)
                                                {
                                                    itemData.resultDataForMetricReal[j].WinCount++;
                                                    itemData.resultDataForMetricReal[j].WinProfitRateSum += profitRow;
                                                }
                                                itemData.resultDataForMetricReal[j].doneResults.Add(resultData);
                                                itemData.resultDataForMetricReal[j].ingItems.Remove(itemData.Code);
                                            }
                                            itemData.resultDataForMetricReal[j] = null;
                                        }

                                        itemData.BeforeExitTime = resultData.ExitTime;

                                        PutResultDataToSimulDays(resultData, simulDays[j], ResultDatasType.Normal);
                                        PutResultDataToSimulDays(resultData, simulDaysDetail[j], ResultDatasType.Normal, true);

                                        var BeforeGap = TimeSpan.ParseExact(resultData.BeforeGap, TimeSpanFormat, null);
                                        if (BeforeGap < itemData.ShortestBeforeGap)
                                        {
                                            itemData.ShortestBeforeGap = BeforeGap;
                                            itemData.ShortestBeforeGapText = BeforeGap.ToString();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (itemData.resultDataForMetric[j] != null && itemData.resultDataForMetric[j].Code == itemData.Code)
                                    itemData.resultDataForMetric[j].ExitTime = resultData.ExitTime;
                                itemData.resultDataForMetric[j].ingItems.Remove(itemData.Code);

                                if (!calSimul || positionData.Real)
                                {
                                    if (itemData.resultDataForMetricReal[j] != null && itemData.resultDataForMetricReal[j].Code == itemData.Code)
                                        itemData.resultDataForMetricReal[j].ExitTime = resultData.ExitTime;
                                    itemData.resultDataForMetricReal[j].ingItems.Remove(itemData.Code);

                                    PutResultDataToSimulDays(resultData, simulDays[j], from2 == end ? ResultDatasType.Last : ResultDatasType.Disappear);
                                    PutResultDataToSimulDays(resultData, simulDaysDetail[j], from2 == end ? ResultDatasType.Last : ResultDatasType.Disappear, true);
                                }
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
                                    itemData.resultDataForMetric[j].doneResults.Add(resultData);
                                }
                                itemData.resultDataForMetric[j] = null;
                            }

                            itemData.BeforeExitTime = resultData.ExitTime;

                            PutResultDataToSimulDays(resultData, simulDays[j], ResultDatasType.Normal);
                            PutResultDataToSimulDays(resultData, simulDaysDetail[j], ResultDatasType.Normal, true);

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

            maxHas = new int[] { 0, 0 };

            lastResultDataForCheckTrend = new BackResultData[] { null, null };

            var market1Day = LoadSticks(itemDataDic["BTCUSDT"] as BackItemData, BaseChartTimeSet.OneMinute, from, minituesInADay * 30, false);
            var m1DI = 0;
            var enterCount = new List<DateTime>[] { new List<DateTime>(), new List<DateTime>() };

            for (int i = 0; i < size; i++)
            {
                var from2 = from.AddMinutes(i);

                if (from2 > end)
                    break;

                if (market1Day.Count == m1DI)
                {
                    market1Day.AddRange(LoadSticks(itemDataDic["BTCUSDT"] as BackItemData, BaseChartTimeSet.OneMinute, from2, minituesInADay * 30, false));
                    if (m1DI >= minituesInADay * 30 * 2)
                    {
                        market1Day.RemoveRange(0, minituesInADay * 30);
                        m1DI -= minituesInADay * 30;
                    }
                }

                if (!TimeCount.ContainsKey(from2.TimeOfDay))
                    TimeCount.Add(from2.TimeOfDay, 0);

                if (!simulDays[0].ContainsKey(from2.Date))
                {
                    for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                        simulDays[j].Add(from2.Date, new DayData() { Date = from2.Date, isL = j });

                    var year = from2.Year;
                    if (!openDaysPerYear.ContainsKey(year))
                        openDaysPerYear.Add(year, 0);
                    openDaysPerYear[year]++;

                    AddOrChangeLoadingText("Simulating ST" + ST + " ...(" + from2.ToString(DateTimeFormat) + ")   " + sw.Elapsed.ToString(TimeSpanFormat), false);
                }

                var detailStartTime = GetDetailStartTime(from2);
                if (!simulDaysDetail[0].ContainsKey(detailStartTime))
                    for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                    {
                        simulDaysDetail[j].Add(detailStartTime, new DayData() { Date = detailStartTime, isL = j });
                        var beforeKey = detailStartTime.AddMinutes(-DetailMinutes);
                        if (simulDaysDetail[j].Count >= 2 && simulDaysDetail[j][beforeKey].ResultDatasForMetric.Count != 0 && simulDaysDetail[j][beforeKey].ResultDatasForMetric.Last().ExitTime == default)
                            simulDaysDetail[j][detailStartTime].ResultDatasForMetric.Add(simulDaysDetail[j][beforeKey].ResultDatasForMetric.Last());
                        if (simulDaysDetail[j].Count >= 2 && simulDaysDetail[j][beforeKey].ResultDatasForMetricReal.Count != 0 && simulDaysDetail[j][beforeKey].ResultDatasForMetricReal.Last().ExitTime == default)
                            simulDaysDetail[j][detailStartTime].ResultDatasForMetricReal.Add(simulDaysDetail[j][beforeKey].ResultDatasForMetricReal.Last());
                    }

                var block = new ActionBlock<BackItemData>(iD => { action(iD, i, from2); }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = threadN });

                foundItemList = new SortedList<int, (BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)> foundList)>[] {
                    new SortedList<int, (BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)> foundList)>(),
                    new SortedList<int, (BaseItemData itemData, List<(DateTime foundTime, ChartValues chartValues)> foundList)>()
                };

                foreach (BackItemData iD in itemDataDic.Values)
                    if (isAllItems || checkItems.ContainsKey(iD.Code))
                        block.Post(iD);
                block.Complete();
                block.Completion.Wait();

                if (from2 >= simulStartTime)
                {
                    var conditionResult = AllItemFindCondition();
                    if (conditionResult.found)
                        for (int j = (int)Position.Long; j <= (int)Position.Short; j++)
                        {
                            while (enterCount[j].Count > 0 && from2.Subtract(enterCount[j][0]).TotalMinutes > 60)
                                enterCount[j].RemoveAt(0);

                            var simulDayDetail = simulDaysDetail[j][detailStartTime];
                            if (conditionResult.position[j])
                                foreach (var foundItem in foundItemList[j].Values)
                                //if (!foundItem.itemData.positionData[(int)Position.Long].Enter && !foundItem.itemData.positionData[(int)Position.Short].Enter)
                                //if (!foundItem.itemData.positionData[j].Enter)
                                {
                                    var minV = foundItem.itemData.listDic.Values[BaseChartTimeSet.OneMinute.index];
                                    var positionData = foundItem.itemData.positionData[j];
                                    EnterSetting(positionData, minV.lastStick);
                                    if (calSimul && from2 >= start)
                                        positionData.Real = CheckTrend((Position)j, from2,
                                            (market1Day[m1DI].Price[3] > market1Day[m1DI].Price[2] ? 1 : -1) * market1Day[m1DI].Price[0] / market1Day[m1DI].Price[1], enterCount[j].Count);

                                    if (market1Day[m1DI].Time != from2)
                                        ShowError(form);
                                    positionData.EnterMarketLastMin = (market1Day[m1DI].Price[3] > market1Day[m1DI].Price[2] ? 1 : -1) * market1Day[m1DI].Price[0] / market1Day[m1DI].Price[1];
                                    positionData.EnterMarketLastMins = enterCount[j].Count;
                                    //if (!calSimul || positionData.Real)
                                        enterCount[j].Add(from2);

                                    if (inside)
                                    {
                                        InsideFirstSetting(foundItem.itemData, (Position)j);
                                        if (InsideEnterCondition(foundItem.itemData, (Position)j))
                                        {
                                            EnterSetting(foundItem.itemData.positionData2[j], minV.lastStick);
                                            foundItem.itemData.positionData2[j].OutEnterTime = foundItem.itemData.positionData[j].EnterTime;
                                        }
                                    }

                                    if (simulDayDetail.ResultDatasForMetric.Count == 0 || simulDayDetail.ResultDatasForMetric[simulDayDetail.ResultDatasForMetric.Count - 1].ExitTime != default)
                                    {
                                        var backResultData = new BackResultData() { EnterTime = from2, Code = foundItem.itemData.Code };
                                        simulDayDetail.ResultDatasForMetric.Add(backResultData);
                                        backResultData.beforeResultData = lastResultDataForCheckTrend[j];
                                        lastResultDataForCheckTrend[j] = backResultData;
                                    }
                                    if ((!calSimul || positionData.Real) && (simulDayDetail.ResultDatasForMetricReal.Count == 0 || simulDayDetail.ResultDatasForMetricReal[simulDayDetail.ResultDatasForMetricReal.Count - 1].ExitTime != default))
                                    {
                                        var backResultData = new BackResultData() { EnterTime = from2, Code = foundItem.itemData.Code };
                                        simulDayDetail.ResultDatasForMetricReal.Add(backResultData);
                                    }

                                    var resultData = simulDayDetail.ResultDatasForMetric[simulDayDetail.ResultDatasForMetric.Count - 1];
                                    (foundItem.itemData as BackItemData).resultDataForMetric[j] = resultData;
                                    resultData.ingItems.Add(foundItem.itemData.Code, foundItem.itemData.Code);

                                    if (!calSimul || positionData.Real)
                                    {
                                        var resultDataReal = simulDayDetail.ResultDatasForMetricReal.Last();
                                        if (CRType < CR.LimitPlusCount0 || resultDataReal.EnterCount < ItemLimit + (int)CRType)
                                        {
                                            (foundItem.itemData as BackItemData).resultDataForMetricReal[j] = resultDataReal;
                                            resultDataReal.EnterCount++;
                                        }

                                        if (resultDataReal.Count > maxHas[j])
                                            maxHas[j] = resultDataReal.Count;

                                        TimeCount[from2.TimeOfDay]++;
                                    }
                                }
                        }
                }

                if (market1Day[m1DI].Time == from2)
                    m1DI++;
            }

            foreach (var iD in itemDataDic.Values)
                foreach (var l in iD.listDic.Values)
                    l.Reset();
        }
        decimal GetAverageAmp(List<TradeStick> sticks)
        {
            decimal sum = 0;
            for (int i = 0; i < sticks.Count; i++)
                sum += sticks[i].Price[0] / sticks[i].Price[1];
            return sum / sticks.Count;
        }
        DateTime GetDetailStartTime(DateTime time)
        {
            return time.Date.AddMinutes((int)(time.TimeOfDay.TotalMinutes / DetailMinutes) * DetailMinutes);
        }
        void PutResultDataToSimulDays(BackResultData resultData, SortedList<DateTime, DayData> simulDays, ResultDatasType type, bool detail = false)
        {
            var enterIndex = simulDays.IndexOfKey(resultData.EnterTime.Date);
            var exitIndex = simulDays.IndexOfKey(resultData.ExitTime.Date);
            if (detail)
            {
                enterIndex = simulDays.IndexOfKey(GetDetailStartTime(resultData.EnterTime));
                exitIndex = simulDays.IndexOfKey(GetDetailStartTime(resultData.ExitTime));
            }

            for (int k = exitIndex; k >= 0; k--)
            {
                if (k < enterIndex)
                    break;

                lock (dayLocker)
                {
                    simulDays.Values[k].resultDatas.Add(resultData);

                    if (type == ResultDatasType.Disappear)
                        simulDays.Values[k].disResultDatas.Add(resultData);
                    else if (type == ResultDatasType.Last)
                        simulDays.Values[k].lastResultDatas.Add(resultData);
                }
            }
        }

        List<TradeStick> LoadSticks(BackItemData itemData, ChartValues chartValues = default, DateTime from = default, int size = default, bool toPast = true)
        {
            if (chartValues == default)
                chartValues = mainChart.Tag as ChartValues;

            if (size == default)
                size = baseLoadSticksSize;

            var multiplier = toPast ? -1 : 1;

            var conn = BinanceSticksDB.DBDic[chartValues];

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
                var centerViewIndex = (int)mainChart.ChartAreas[0].AxisX.ScaleView.ViewMaximum - 2 - (chartViewSticksSize + 1) / 2;
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

            var conn = BinanceSticksDB.DBDic[chartValues];

            var time = first ? DateTime.MaxValue : DateTime.MinValue;
            if (itemData == default)
                foreach (BackItemData itemData2 in itemDataDic.Values)
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