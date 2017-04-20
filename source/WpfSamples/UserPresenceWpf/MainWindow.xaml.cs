﻿//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows;
using System.ComponentModel;
using System.IO;
using EyeXFramework.Wpf;
using System.Windows.Forms;
using System.Windows.Media;
using Tobii.EyeX.Framework;

namespace UserPresenceWpf
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class GazeData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private int _gazeX;
        private int _gazeY;

        private void OnPropertyChanged(String property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
        public int gazeX
        {
            get
            {
                return _gazeX;
            }
            set
            {
                _gazeX = value;
                OnPropertyChanged("gazeX");
            }
        }
        public int gazeY
        {
            get
            {
                return _gazeY;
            }
            set
            {
                _gazeY = value;
                OnPropertyChanged("gazeY");
            }
        }
    }

    public partial class MainWindow : Window
    {
        // folder and filename for the log
        public Boolean recording = false;
        public string logFilename = "";
        public int index = 0;
        public System.IO.StreamWriter logFile = null;

        private WpfEyeXHost _eyeXHost;
        public GazeData publicGazeData = new GazeData();

        public double lastFixationStartTime = 0;
        public bool userPresent;
        public int fixation = 0;
        public string initialTime;

        public MainWindow()
        {
            _eyeXHost = new WpfEyeXHost();
            _eyeXHost.Start();
            InitializeComponent();

            gazeDataTextX.DataContext = publicGazeData;
            gazeDataTextY.DataContext = publicGazeData;

            var fixationGazeDataStream = _eyeXHost.CreateFixationDataStream(FixationDataMode.Sensitive);
            fixationGazeDataStream.Next += (s, e) => updateFixationData(e);
        }

        private void updateFixationData(EyeXFramework.FixationEventArgs e)
        {
            if (e.EventType == FixationDataEventType.Begin)
            {
                lastFixationStartTime = e.Timestamp;
                this.fixation = 1;
            }
            if (e.EventType == FixationDataEventType.End)
            {
                var lastFixationDuration = e.Timestamp - lastFixationStartTime;
                this.fixation = 0;
            }

            publicGazeData.gazeX = (int)e.X;
            publicGazeData.gazeY = (int)e.Y;

            // write data to log file
            writeDataToFile(e.Timestamp, publicGazeData.gazeX, publicGazeData.gazeY);
        }

        private int isGazeOnScreen(int gazeX, int gazeY)
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            if (gazeX < 0 || gazeX > screenWidth) return 0;
            if (gazeY < 0 || gazeY > screenHeight) return 0;

            return 1;
        }

        private void initializeLogFile(string destination)
        {
            // define the logs filenames, the path, and start the log file
            logFilename = string.Format(@"{0}-Tobii-Log-{1}.csv", this.session.Text, getTimestamp("filename"));
            logFilename = Path.Combine(destination, logFilename);
            logFile = new System.IO.StreamWriter(logFilename, true);
            
            // print headers (ugly,should be re-written more cleanly)
            string header = "Timestamp,Milliseconds,Index,Session,fixation,gazeX,gazeY,onScreen";
            logFile.WriteLine(header);
        }

        private void writeDataToFile(double time, int x, int y)
        {
            if(this.logFile != null && this.recording)
            {
                index += 1;
                string text = getTimestamp("datetime").ToString() 
                    + "," + time + "," + index + "," + this.session.Text + "," + this.fixation 
                    + "," + x + "," + y + isGazeOnScreen(x, y);
                this.logFile.WriteLine(text);
            }
        }

        /// <summary>
        /// get the current timestamp
        /// </summary>
        public static string getTimestamp(String type)
        {
            if (type == "filename")
                return DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss");
            else if (type == "datetime")
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            else if (type == "date")
                return DateTime.Now.ToString("yyyy-MM-dd");
            else if (type == "time")
                return DateTime.Now.ToString("HH:mm:ss");
            else if (type == "second")
                return DateTime.Now.ToString("ss");
            else if (type == "unix")
                return "" + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            else
                return DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff");
        }

        private void choose_folder(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            DialogResult result = fbd.ShowDialog();

            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                this.savingDataPath.Text = fbd.SelectedPath.ToString();
            }
        }

        private void record_data(object sender, RoutedEventArgs e)
        {
            // check if we have a folder
            if(System.IO.Directory.Exists(this.savingDataPath.Text) && File.GetAttributes(this.savingDataPath.Text).HasFlag(FileAttributes.Directory))
            {
                if(this.logFile == null)
                    initializeLogFile(this.savingDataPath.Text);

                if (this.recording)
                {
                    this.recording = false;
                    this.startRecording.Content = "Not Recording";
                    this.startRecording.Background = System.Windows.Media.Brushes.Red;
                }
                else if (!this.recording)
                {
                    this.recording = true;
                    this.startRecording.Content = "Recording !";
                    this.startRecording.Background = System.Windows.Media.Brushes.LightGreen;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a valid folder", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        protected override void OnClosed(EventArgs e)
        {
            this.recording = false; 

            if(this.logFile != null)
                this.logFile.Close();

            base.OnClosed(e);

            System.Windows.Application.Current.Shutdown();
        }
    }
}
