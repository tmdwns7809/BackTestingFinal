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
using TradingLibrary.Base.DB;
using TradingLibrary.Base.DB.Binance;
using System.Runtime.InteropServices;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Collections;
using System.Reflection;
using MathNet.Numerics;
using TradingLibrary.Base.Values;
using TradingLibrary.Base.Values.Chart;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;
using Series = System.Windows.Forms.DataVisualization.Charting.Series;

namespace BackTestingFinal
{
    public sealed class FundingRateBackTesting : BaseFunctions
    {
        public static FundingRateBackTesting instance;

        public FastObjectListView codeListView = new FastObjectListView();

        public Button runButton = new Button();

        public FundingRateBackTesting(Form form, string programName, decimal st) : base(form, programName, st)
        {
            FuturesCoinFundingRate.SetDB();
            
            LoadCodeListAndMetric();
        }
        public override void SetInitValues()
        {
            commisionRate = Commision.Binance;
        }
        public override void SetChartMainArea()
        {
            var chartAreaPrice = mainChart.ChartAreas.Add(ChartNames.AREA_FUNDING_RATE);
            SetChartAreaLast(chartAreaPrice);
            chartAreaPrice.Position = new ElementPosition(0, 0, 100, 95);
            var innerSpaceRatio = (int)((double)ChartValues.CURSOR_TEXT_HEIGHT / mainChart.Height * 100 / chartAreaPrice.Position.Height * 100);
            chartAreaPrice.InnerPlotPosition = new ElementPosition(3, innerSpaceRatio, 94, 100 - innerSpaceRatio * 2);
            chartAreaPrice.AxisX.LineColor = ColorSet.Border;
            chartAreaPrice.AxisX.LabelStyle.Format = Formats.TIME;
            chartAreaPrice.AxisX2.LineColor = chartAreaPrice.AxisX.LineColor;
            chartAreaPrice.AxisY.LabelStyle.Format = "0.###";
            chartAreaPrice.AxisY.Name = ChartNames.AXIS_Y_CUMULATIVE_RETURN;
            chartAreaPrice.AxisY.IsStartedFromZero = true;
            chartAreaPrice.AxisY.LabelStyle.Interval = 0.01;
            chartAreaPrice.AxisY2.LabelStyle.Format = "0.#####";
            chartAreaPrice.AxisY2.Name = ChartNames.AXIS_Y_FUNDING_RATE;
            chartAreaPrice.AxisY2.IsStartedFromZero = false;
            chartAreaPrice.AxisY2.LabelStyle.Interval = 0.0002;
            //chartAreaPrice.AxisY2.ScaleView.SmallScrollSize = 5;
            chartAreaPrice.CursorX.LineColor = Color.FromArgb(ColorSet.CursorLineAlpha / 2, ColorSet.CursorLineColor);
            chartAreaPrice.CursorX.LineWidth = 1;
            chartAreaPrice.CursorY.IsUserEnabled = true;
            chartAreaPrice.CursorY.LineColor = ColorSet.CursorLineColor;
            chartAreaPrice.CursorY.LineWidth = 1;
            chartAreaPrice.CursorY.AxisType = AxisType.Secondary;

            if (minorGrid != 0)
            {
                chartAreaPrice.AxisX.MinorGrid.Enabled = true;
                chartAreaPrice.AxisX.MinorGrid.Interval = minorGrid;
                chartAreaPrice.AxisX.MinorGrid.LineWidth = 1;
                chartAreaPrice.AxisX.MinorGrid.LineColor = ColorSet.ChartGridWeak;
            }

            if (majorGrid != 0)
            {
                chartAreaPrice.AxisX.MajorGrid.Enabled = true;
                chartAreaPrice.AxisX.MajorGrid.Interval = majorGrid;
                chartAreaPrice.AxisX.MajorGrid.LineWidth = 1;
                chartAreaPrice.AxisX.MajorGrid.LineColor = ColorSet.ChartGrid;
            }

            SetFundingRateSeries(mainChart.Series.Add(ChartNames.SERIES_FUNDING_RATE), chartAreaPrice);
            SetCumulativeReturnSeries(mainChart.Series.Add(ChartNames.SERIES_CUMULATIVE_RETURN), chartAreaPrice);
        }
        public override void SetRestView()
        {
            SetButton(runButton, "Run", Run());
            runButton.Size = new Size(40, 20);
            runButton.Location = new Point(mainChart.Location.X + mainChart.Width + 10, mainChart.Location.Y + 10);

            SetListView(codeListView, new (string, string, int)[]
                {
                    ("No.", "number", 50),
                    ("Code", "Code", 150),
                    ("Days", "days", 50),
                    ("CR", "cumulativeReturn", 100),
                    ("CRPD", "cumulativeReturnPerDay", 100),
                    ("MDD", "maxDrawDown", 100),
                    ("MDDPD", "maxDrawDownPerDay", 100),
                    ("MDDUPD", "maxDrawDownPerUpDays", 100)
                }, isFillProportion: false);
            codeListView.Size = new Size(GetFormWidth(form) - mainChart.Width - 5, mainChart.Height - runButton.Location.Y - runButton.Height - GetFormUpBarSize(form));
            codeListView.Location = new Point(runButton.Location.X, runButton.Location.Y + runButton.Height + 10);
            codeListView.SelectionChanged += (sender, e) =>
            {
                if (codeListView.SelectedIndices.Count != 1)
                    return;

                var itemData = codeListView.SelectedObject as BackItemData;

                ShowChart(itemData);
            };
        }
        // 모든 종목들 시뮬레이션 진행
        EventHandler Run()
        {
            return (sender, e) =>
            {
                foreach (var itemData in itemDataDic.Values)
                {
                    var list = itemData.listForFundingRate.list;

                    if (list.Count == 0)
                        list = LoadSticks(itemData as BackItemData);

                    itemData.cumulativeReturn = 1;
                    itemData.days = (decimal)list[list.Count - 1].Time.Subtract(list[0].Time).TotalDays;

                    // 진입 수수료
                    itemData.cumulativeReturn += itemData.cumulativeReturn * -0.0005m;

                    var startSize = itemData.cumulativeReturn;
                    var highest = itemData.cumulativeReturn;
                    var highestTime = list[0].Time;
                    var low = itemData.cumulativeReturn;
                    itemData.maxDrawDown = 0;
                    var lowTime = list[0].Time;
                    itemData.CRlist = new List<decimal>();

                    foreach (var stick in list)
                    {
                        itemData.cumulativeReturn += startSize * stick.Price[0];
                        itemData.CRlist.Add(itemData.cumulativeReturn);

                        if (itemData.cumulativeReturn > highest)
                        {
                            var drawDown = low - highest;
                            if (drawDown < itemData.maxDrawDown)
                            {
                                itemData.maxDrawDown = drawDown;
                                itemData.maxDrawDownDays = (decimal)lowTime.Subtract(highestTime).TotalDays;
                                itemData.maxDrawDownPerDay = itemData.maxDrawDown / itemData.maxDrawDownDays;
                                itemData.maxDrawDownUpDays = (decimal)stick.Time.Subtract(lowTime).TotalDays;
                                itemData.maxDrawDownPerUpDays = itemData.maxDrawDownDays / itemData.maxDrawDownUpDays;
                            }

                            highest = itemData.cumulativeReturn;
                            highestTime = stick.Time;
                            low = itemData.cumulativeReturn;
                            lowTime = stick.Time;
                        }
                        else if (itemData.cumulativeReturn < low)
                        {
                            low = itemData.cumulativeReturn;
                            lowTime = stick.Time;
                        }
                    }

                    itemData.cumulativeReturn += startSize * -0.0005m;

                    itemData.cumulativeReturnPerDay = (itemData.cumulativeReturn - 1) / itemData.days;
                }
            };
        }
        public void SetFundingRateSeries(Series series, ChartArea chartArea)
        {
            series.ChartType = SeriesChartType.Column;
            series.XValueMember = "Time";
            series.XValueType = ChartValueType.Time;
            series.Color = ColorSet.PlusPrice;
            series.YAxisType = AxisType.Secondary;
            series.ChartArea = chartArea.Name;
            series["PointWidth"] = "0.8";
        }
        public void SetCumulativeReturnSeries(Series series, ChartArea chartArea)
        {
            series.ChartType = SeriesChartType.Line;
            series.XValueMember = "Time";
            series.XValueType = ChartValueType.Time;
            series.Color = Color.AliceBlue;
            series.YAxisType = AxisType.Primary;
            series.ChartArea = chartArea.Name;
        }
        void LoadCodeListAndMetric()
        {
            var conn = FuturesCoinFundingRate.DB;

            var reader = new SQLiteCommand("Select name From sqlite_master where type='table'", conn).ExecuteReader();

            var number = 1;
            while (reader.Read())
            {
                var code = reader["name"].ToString();

                var itemData = new BackItemData(code, number++);
                codeListView.AddObject(itemData);
                itemDataDic.Add(itemData.Code, itemData);
            }
        }

        BackTradeStick GetStickFromSQL(SQLiteDataReader reader)
        {
            return new BackTradeStick(null)
            {
                Time = DateTime.ParseExact(reader["time"].ToString(), Formats.DB_TIME, null),
                Price = new decimal[] { decimal.Parse(reader["fundingRate"].ToString()) }
            };
        }
        List<TradeStick> LoadSticks(BackItemData itemData)
        {
            var conn = FuturesCoinFundingRate.DB;

            var start = DateTime.Parse("2000-01-01 00:00:00");
            //var start = DateTime.Parse("2024-01-01 00:00:00");
            var end = DateTime.Parse("2100-11-01 00:00:00");

            var list = new List<TradeStick>();
            try
            {
                var reader = new SQLiteCommand("Select *, rowid From '" + itemData.Code + "' where " +
                                "(time>='" + start.ToString(Formats.DB_TIME) + "') and (time<='" + end.ToString(Formats.DB_TIME) + "') " +
                                "order by rowid", conn).ExecuteReader();
                while (reader.Read())
                    list.Add(GetStickFromSQL(reader));
            }
            catch (Exception e)
            {
                Error.Show(message: e.Message);
                throw;
            }

            return list;
        }
        void ShowChart(BackItemData itemData)
        {
            showingItemData = itemData;
            form.Text = itemData.Code;

            var v = itemData.listForFundingRate;

            itemData.listForFundingRate.list = LoadSticks(itemData);

            ClearChart(mainChart);

            if (v.list.Count == 0)
                return;

            for (int i = 0; i < v.list.Count; i++)
            {
                mainChart.Series[ChartNames.SERIES_FUNDING_RATE].Points
                    .AddXY(v.list[i].Time.ToString(mainChart.ChartAreas[ChartNames.AREA_FUNDING_RATE].AxisX.LabelStyle.Format)
                    , (double)v.list[i].Price[0]);
                if (itemData.CRlist.Count > 0)
                    mainChart.Series[ChartNames.SERIES_CUMULATIVE_RETURN].Points
                        .AddXY(v.list[i].Time.ToString(mainChart.ChartAreas[ChartNames.AREA_FUNDING_RATE].AxisX.LabelStyle.Format)
                        , (double)itemData.CRlist[i]);
            }

            RecalculateChart(mainChart);
        }

        public override DateTime NowTime()
        {
            return DateTime.UtcNow;
        }

        public override bool isBottomFixed(string axisName)
        {
            throw new NotImplementedException();
        }

        public override int GetZoomScale(string axisName)
        {
            throw new NotImplementedException();
        }
    }
}