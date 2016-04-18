using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;
using System.Windows.Threading;
using System.ComponentModel;

namespace ComperPlayer
{

    public partial class MainWindow : Window
    {
        #region Init Properties
        private KinectSensor sensor = null;
        private SkeletonHandler skeleton;

        //for fps timer
        private DispatcherTimer FPSTimer = null;
        private int FPSCount = 0;

        //background worker
        private System.ComponentModel.BackgroundWorker backgroundWorker;
        #endregion
        public MainWindow()
        {
            //init the kinect sensor
            this.sensor = KinectSensor.GetDefault();
            skeleton = new SkeletonHandler();
            skeleton.LaModleInit(sensor);

            this.sensor.Open();
            skeleton.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(skeleton_PropertyChanged);
            this.sensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            /*注册后台程序来执行耗时操作*/
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            backgroundWorker.RunWorkerAsync();

            FPSTimer = new DispatcherTimer();
            FPSTimer.Interval = TimeSpan.FromSeconds(1);
            FPSTimer.Tick += new EventHandler(FPSTimer_Tick);
            if (FPSTimer.IsEnabled == false)
            {
                FPSCount = 0;
                FPSTimer.Start();
            }
            InitializeComponent();
        }

        private void skeleton_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (worker != null)
            {
                while (!worker.CancellationPending)
                {
                    //here to do all tasks
                    skeleton.LaSkeletonRefresh(ref FPSCount);
                    skeleton.ControlImpliment();
                    //FPSCount++;
                }
            }
        }

        private void FPSTimer_Tick(object sender, EventArgs e)
        {
            FPSTimer.Stop();
            FPSText.Content = "FPS: " + FPSCount;
            FPSCount = 0;            
            FPSTimer.Start();
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            string status = this.sensor.IsAvailable ? "Kinect is Running"
                                                            : "Sensor break down, Please check and wait!";
            this.StatusText.Content = status;
        }
    }
}
