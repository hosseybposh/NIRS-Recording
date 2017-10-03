using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using System.Diagnostics;

namespace MBLL
{
    public partial class frmMain : Form
    {
        List<double[]> dataValues = new List<double[]>();
        List<double[]> oxyValues = new List<double[]>();
        List<double[]> deoxyValues = new List<double[]>();


        public frmMain()
        {
            InitializeComponent();
        }

        private void btnReadFile_Click(object sender, EventArgs e)
        {
            CurrentDevConfig = propertyGrid1.SelectedObject as DeviceConfig;

            for (int i = 0; i < CurrentDevConfig.ChannelsSetting.Count; i++)
            {
                chlChannelVisible.Items.Add(CurrentDevConfig.ChannelsSetting[i].ChannelName, i % 7 == 0);
            }

            //columnsCount = (int.Parse(txtColumns.Value.ToString()) * 2) + 1;
            OpenFileDialog of = new OpenFileDialog();
            double interval = CurrentDevConfig.Interval;
            double timeSpent = 0;
            if (of.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string[] lines = System.IO.File.ReadAllLines(of.FileName);

                foreach (string s in lines)
                {
                    string[] vals = s.Split(new char[] { ' ', ',', ';', '\t' });

                    NextValue(timeSpent += interval);

                    foreach (string v in vals)
                        NextValue(double.Parse(v));
                }
            }
        }

        public int currentColumnsIndex = 0;
        public int currentRowIndex = -1;
        public DeviceConfig CurrentDevConfig = null;

        ButterworthLowPassFilter[] LPF;

        public void NextValue(double nextValue)
        {
            if (currentColumnsIndex == 0)
            {
                dataValues.Add(new double[CurrentDevConfig.ChannelCount + 1]);
                oxyValues.Add(new double[(CurrentDevConfig.ChannelCount / 2) + 1]);
                deoxyValues.Add(new double[(CurrentDevConfig.ChannelCount / 2) + 1]);
                currentRowIndex++;
            }

            if (currentColumnsIndex == 0)
                dataValues[currentRowIndex].SetValue(nextValue, currentColumnsIndex);
            else
                dataValues[currentRowIndex].SetValue(LPF[currentColumnsIndex-1].Filter(nextValue), currentColumnsIndex);

            currentColumnsIndex++;

            if (currentColumnsIndex % (CurrentDevConfig.ChannelCount + 1) == 0)
            {
                currentColumnsIndex = 0;
                CalculateDeltaCons(currentRowIndex);

            }

        }

        private void CalculateDeltaCons(int currentRowIndex)
        {
            double[] muValues = new double[CurrentDevConfig.ChannelCount];

            if (currentRowIndex == 0)
                return;

            for (int i = 0; i < muValues.Length; i++)
            {

                double opticalIntensityPreviusRow = (double)dataValues[currentRowIndex - 1].GetValue(i + 1);
                double opticalIntensityCurrentRow = (double)dataValues[currentRowIndex].GetValue(i + 1);

                muValues[i] = (double)Math.Log10(opticalIntensityCurrentRow / opticalIntensityPreviusRow) / CurrentDevConfig.ChannelsSetting[i].SensorDetectorDistance;
            }

            for (int i = 0; i < muValues.Length / 2; i++)
            {
                double time = (double)dataValues[currentRowIndex - 1].GetValue(0);

                double mu735 = muValues[i];
                double mu850 = muValues[(muValues.Length / 2) + i];

                oxyValues[currentRowIndex].SetValue(time, 0);
                deoxyValues[currentRowIndex].SetValue(time, 0);
                //oxyValues[currentRowIndex].SetValue((CurrentDevConfig.Epsilon_Hbo2_735 * mu850 - CurrentDevConfig.Epsilon_Hbo2_850 * mu735) / (CurrentDevConfig.Epsilon_Hbo2_735 * CurrentDevConfig.Epsilon_Hb_850 - CurrentDevConfig.Epsilon_Hbo2_850 * CurrentDevConfig.Epsilon_Hb_735), i + 1);
                //deoxyValues[currentRowIndex].SetValue((CurrentDevConfig.Epsilon_Hb_735 * mu850 - CurrentDevConfig.Epsilon_Hb_850 * mu735) / (CurrentDevConfig.Epsilon_Hb_735 * CurrentDevConfig.Epsilon_Hbo2_850 - CurrentDevConfig.Epsilon_Hb_850 * CurrentDevConfig.Epsilon_Hbo2_735), i + 1);

                double ox = 1 * (-0.5497 * Math.Log10(1/(double)dataValues[currentRowIndex].GetValue(i + 1)) + 1.04579 * Math.Log10(1/(double)dataValues[currentRowIndex].GetValue(i + 1)));
                double dox = 1 * (-0.81386 * Math.Log10(1/(double)dataValues[currentRowIndex].GetValue(i + 1)) + 1.430549 * Math.Log10(1/(double)dataValues[currentRowIndex].GetValue(i + 1)));
                oxyValues[currentRowIndex].SetValue(ox, i + 1);
                deoxyValues[currentRowIndex].SetValue(dox, i + 1);

            }

            DrawCalculatedGraphPoints(oxyValues[currentRowIndex].ToArray(), deoxyValues[currentRowIndex].ToArray());
        }

        private void DrawCalculatedGraphPoints(double[] oxyValues, double[] deoxyValues)
        {
            if (!InitializedGraph)
            {
                InitializeGraph(0.1, 0.1);
                InitializedGraph = true;
            }

            AddDataToGraph(zedGraphControl1, dataValues[currentRowIndex][0], oxyValues, deoxyValues);


            //System.Threading.Thread.Sleep(100);

            //zedGraphControl1.GraphPane.XAxis.Scale.Max = oxyValues[0];

            //zedGraphControl1.PerformAutoScale();

            Application.DoEvents();
        }

        private void InitializeGraph(double yPos,double yNeg)
        {
            // Get a reference to the GraphPane instance in the ZedGraphControl
            GraphPane myPane = zedGraphControl1.GraphPane;
            var zg1 = zedGraphControl1;

            // Set the titles and axis labels
            myPane.Title.Text = "Demonstration of Dual Y Graph";
            myPane.XAxis.Title.Text = "Time, seconds";
            myPane.YAxis.Title.Text = "Parameter A";
            myPane.Y2Axis.Title.Text = "Parameter B";



            // Make up some data points based on the Sine function

            // Generate a red curve with diamond symbols, and "Alpha" in the legend
            LineItem myCurve;

            for (int i = 0; i < CurrentDevConfig.ChannelsSetting.Count; i++)
            {
                PointPairList list = new PointPairList();
                //myCurve = myPane.AddCurve(CurrentDevConfig.ChannelsSetting[i].ChannelName,
                //    list, CurrentDevConfig.ChannelsSetting[i].GraphColor, (i < CurrentDevConfig.ChannelsSetting.Count / 2) ? SymbolType.Star : SymbolType.Circle);
                myCurve = myPane.AddCurve(CurrentDevConfig.ChannelsSetting[i].ChannelName,
                    list, CurrentDevConfig.ChannelsSetting[i].GraphColor, (i < CurrentDevConfig.ChannelsSetting.Count / 2) ? SymbolType.None: SymbolType.None);
                // Fill the symbols with white
                myCurve.Symbol.Fill = new Fill(Color.White);
                myCurve.Line.IsOptimizedDraw = true;
                myCurve.IsVisible = chlChannelVisible.GetItemCheckState(i) == CheckState.Checked;
            }

            // Generate a blue curve with circle symbols, and "Beta" in the legend
            //myCurve = myPane.AddCurve("Beta",
            //    list2, Color.Blue, SymbolType.Circle);
            //// Fill the symbols with white
            //myCurve.Symbol.Fill = new Fill(Color.White);
            // Associate this curve with the Y2 axis
            //myCurve.IsY2Axis = true;

            // Show the x axis grid
            //myPane.XAxis.CrossAuto = true;
            //myPane.XAxis.Scale.MaxAuto = true;
            myPane.XAxis.IsAxisSegmentVisible = false;
            // Make the Y axis scale red
            myPane.YAxis.Scale.FontSpec.FontColor = Color.Red;
            myPane.YAxis.Title.FontSpec.FontColor = Color.Red;
            // turn off the opposite tics so the Y tics don't show up on the Y2 axis
            myPane.YAxis.MajorTic.IsOpposite = false;
            myPane.YAxis.MinorTic.IsOpposite = false;
            // Don't display the Y zero line
            myPane.YAxis.MajorGrid.IsZeroLine = true;
            // Align the Y axis labels so they are flush to the axis
            myPane.YAxis.Scale.Align = AlignP.Inside;
            // Manually set the axis range
            myPane.YAxis.Scale.Min = -yNeg;
            myPane.YAxis.Scale.Max = yPos;


            // Fill the axis background with a gradient
            myPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);

            // Add a text box with instructions
            //TextObj text = new TextObj(
            //    "Zoom: left mouse & drag\nPan: middle mouse & drag\nContext Menu: right mouse",
            //    0.05f, 0.95f, CoordType.ChartFraction, AlignH.Left, AlignV.Bottom);
            //text.FontSpec.StringAlignment = StringAlignment.Near;
            //myPane.GraphObjList.Add(text);

            // Enable scrollbars if needed
            zg1.IsShowHScrollBar = false;
            zg1.IsAutoScrollRange = false;

            // OPTIONAL: Show tooltips when the mouse hovers over a point
            zg1.IsShowPointValues = true;

            //zg1.PointValueEvent += new ZedGraphControl.PointValueHandler(MyPointValueHandler);

            //// OPTIONAL: Add a custom context menu item
            //zg1.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(
            //                MyContextMenuBuilder);

            //// OPTIONAL: Handle the Zoom Event
            //zg1.ZoomEvent += new ZedGraphControl.ZoomEventHandler(MyZoomEvent);

            // Size the control to fit the window
            //SetSize();

            // Tell ZedGraph to calculate the axis ranges
            // Note that you MUST call this after enabling IsAutoScrollRange, since AxisChange() sets
            // up the proper scrolling parameters

            //myPane.YAxis.Scale.MinAuto = true;
            //myPane.YAxis.Scale.MaxAuto = true;
            //myPane.XAxis.Scale.MinAuto = true;
            //myPane.XAxis.Scale.MaxAuto = true;

            zg1.AxisChange();
            // Make sure the Graph gets redrawn
            zg1.Invalidate();
        }

        private void AddDataToGraph(ZedGraphControl zg1, double xValue, double[] yValue1, double[] yValue2)
        {
            // Make sure that the curvelist has at least one curve
            if (zg1.GraphPane == null || zg1.GraphPane.CurveList.Count <= 0)
                return;

            LineItem[] curves = new LineItem[CurrentDevConfig.ChannelsSetting.Count];
            IPointListEdit[] lists = new IPointListEdit[CurrentDevConfig.ChannelsSetting.Count];

            for (int i = 0; i < CurrentDevConfig.ChannelsSetting.Count; i++)
            {
                curves[i] = zg1.GraphPane.CurveList[i] as LineItem;
                lists[i] = curves[i].Points as IPointListEdit;
                curves[i].Symbol.Fill.IsVisible = false;
                lists[i].Add(xValue, (i < CurrentDevConfig.ChannelsSetting.Count / 2) ? yValue1[i + 1] : yValue2[1 + i - (CurrentDevConfig.ChannelsSetting.Count / 2)]);
            }

            if (dataValues.Count % 10 == 0)
            {

                double minAxis = xValue - 22;
                zedGraphControl1.GraphPane.XAxis.Scale.Min = minAxis;
                zedGraphControl1.GraphPane.XAxis.Scale.Max = xValue + 3;
                zedGraphControl1.AxisChange();
                // force redraw
                zg1.Invalidate();

            }
        }

        

        public bool InitializedGraph { get; set; }

        private void frmMain_Load(object sender, EventArgs e)
        {
            var devConf = new DeviceConfig();

            devConf.FirstWaveLength = 735;
            devConf.SecondWaveLength = 850;

            devConf.Epsilon_Hbo2_850 = 0.2525936f;
            devConf.Epsilon_Hb_850 = 0.1798319f;
            devConf.Epsilon_Hbo2_735 = 0.1224975f;
            devConf.Epsilon_Hb_735 = 0.2961124f;

            devConf.FirstWaveLength = 735;
            devConf.SecondWaveLength = 850;

            devConf.ChannelCount = 8;
            devConf.Interval = 0.02;
            devConf.ChannelsSetting = new List<ChannelSetting>();

            Color[] cols = new Color[] { Color.Blue, Color.Red, Color.Yellow, Color.Green, Color.Brown, Color.Orange, Color.Pink, Color.Purple, Color.Black, Color.LightYellow, Color.DarkBlue, Color.DarkRed, Color.DarkGreen, Color.DarkGoldenrod };

            for (int i = 0; i < devConf.ChannelCount; i++)
            {
                devConf.ChannelsSetting.Add(new ChannelSetting { ChannelName = i + "", GraphColor = cols[i], SensorDetectorDistance = 1.0f, Visible = true });
            }
            propertyGrid1.SelectedObject = devConf;

        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            DeviceConfig devcfg = propertyGrid1.SelectedObject as DeviceConfig;

            OpenFileDialog sv = new OpenFileDialog();
            sv.Filter = "*.dcfg|*.dcfg";
            if (sv.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                devcfg = DeviceConfig.Deserialize(sv.FileName);
                propertyGrid1.SelectedObject = devcfg;
            }
        }

        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            DeviceConfig devcfg = propertyGrid1.SelectedObject as DeviceConfig;

            SaveFileDialog sv = new SaveFileDialog();
            sv.Filter = "*.dcfg|*.dcfg";
            if (sv.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DeviceConfig.Serialize(devcfg, sv.FileName);
            }
        }


        private void chlChannelVisible_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (zedGraphControl1.GraphPane.CurveList.Count > e.Index)
                (zedGraphControl1.GraphPane.CurveList[e.Index] as LineItem).IsVisible = e.NewValue == CheckState.Checked;

            zedGraphControl1.Invalidate();

            Application.DoEvents();

        }

        private void zedGraphControl1_Load(object sender, EventArgs e)
        {

        }

        Encoding ascii = Encoding.ASCII;


        private void btnConnect_Click(object sender, EventArgs e)
        {
            int count = 0;

            IsStarted = true;

            Control.CheckForIllegalCrossThreadCalls = false;

            CurrentDevConfig = propertyGrid1.SelectedObject as DeviceConfig;
            chlChannelVisible.Items.Clear();


            for (int i = 0; i < CurrentDevConfig.ChannelsSetting.Count; i++)
            {
                chlChannelVisible.Items.Add(CurrentDevConfig.ChannelsSetting[i].ChannelName, i % 7 == 0);
            }

            System.Threading.Thread th = new System.Threading.Thread(delegate()
                {
                    SerialPort port = new SerialPort("COM6");
                    port.BaudRate = 115200;
                    port.Encoding = ascii;
                    port.DataBits = 8;
                    port.ReadTimeout = 500;
                    port.NewLine = "~";
                    port.Open();

                    //tikCount = 0;
                    //tmrTime.Start();

                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    double interval = 0;
                    long countReceived = 0;
                    long countErrReceived = 0;

                    int currValue;
                    int wrong = 0;

                    LPF = new ButterworthLowPassFilter[CurrentDevConfig.ChannelCount];

                    for (int i = 0; i < LPF.Length; i++)
                        LPF[i] = new ButterworthLowPassFilter();

                    while (port.IsOpen && this.Created && IsStarted)
                    {

                        /// Removed due to test of: regression
                        interval += 1d / 50d;
                        NextValue(interval);

                        //////////////////// Read 6 bytes////////////////////
                        string strInPort = "";
                        byte[] asciiTobyte;

                        //test
                        int test = 0;

                        do
                        {
                            test++;
                            try
                            {
                                strInPort = port.ReadLine(); // caution
                            }
                            catch
                            {
                                count++;
                                countErrReceived++;
                            }

                        }
                        while (strInPort.Length != 16 && count < 3);
                        /////////////////////////////////////////////////////
                        if (test > 1)
                            wrong++;
                        //count = 0;

                        if (strInPort.Length == 16)
                        {
                            asciiTobyte = ascii.GetBytes(strInPort);

                            for (int index = 0; index < 16; index += 2)
                            {
                                currValue = ((int)(asciiTobyte[index+1] * 256 + asciiTobyte[index]));
                                NextValue(currValue);
                            }

                            countReceived++;
                        }

                        if (stopWatch.ElapsedMilliseconds > 1000 && label1.Created)
                        {
                            label1.Text = countReceived / (stopWatch.ElapsedMilliseconds / 1000) + "";
                            label2.Text = countErrReceived + "";
                        }
                    }

                    if (port.IsOpen)
                    {
                        try
                        {
                            port.DiscardInBuffer();
                            port.Close();
                            port.Dispose();
                        }
                        catch
                        {
                        }
                    }
                });
            th.Start();
        }


        public bool IsStarted { get; set; }

        private void button1_Click(object sender, EventArgs e)
        {
            IsStarted = false;
        }








        //test
        string FileName;


        private void btnSave_Click(object sender, EventArgs e)
        {
            double temp = 0;
            string data = "";

            saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            DialogResult dr = saveFileDialog.ShowDialog();
            if (dr == DialogResult.Cancel)
                return;

            if (dr == DialogResult.OK)
                FileName = saveFileDialog.FileName;



            int maxCol = CurrentDevConfig.ChannelCount + 1;
            int maxColNim=(maxCol - 1) / 2;

            prgExport.Maximum = dataValues.Count;
            prgExport.Visible = true;
            var ddataValues = dataValues.ToArray();
            var doxyValues = oxyValues.ToArray();
            var ddeoxyValues = deoxyValues.ToArray();

            for (int row = 0; row < dataValues.Count; row++)
            {
                for (byte col = 0; col < maxCol; col++)
                {
                    temp = ddataValues[row][col ];
                    data += (col == 0 ? "" : ",") + temp;
                }
                for (byte col = 0; col < maxColNim; col++)
                {
                    temp = doxyValues[row][col + 1];
                    data += "," + temp;

                    temp = ddeoxyValues[row][col + 1];
                    data += "," + temp;
                }
                data += "\n";
                System.IO.File.AppendAllText(FileName, data);
                data = "";
                prgExport.Value = row;
                Application.DoEvents();
            }


            prgExport.Visible = false;

        }

        int tikCount = 0;
        private void tmrTime_Tick(object sender, EventArgs e)
        {
            //label1.Text = (tikCount++) + "";
        }
    }
}
