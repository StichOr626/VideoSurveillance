using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Windows.Media.Imaging;

namespace VideoSurveillance
{
    public partial class MainWindow : Window
    {
        private FilterInfoCollection _videoDevices;
        private List<VideoCaptureDevice> _videoSources = new List<VideoCaptureDevice>();
        private List<System.Windows.Controls.Image> _videoDisplays = new List<System.Windows.Controls.Image>();


        public MainWindow()
        {
            InitializeComponent();
            LoadAvailableCameras();
        }

        private void LoadAvailableCameras()
        {
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (var device in _videoDevices.Cast<FilterInfo>())
            {
                CameraList.Items.Add(device.Name);
            }
            if (CameraList.Items.Count > 0)
                CameraList.SelectedIndex = 0;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (CameraList.SelectedItem == null) return;

            var selectedIndex = CameraList.SelectedIndex;
            var videoSource = new VideoCaptureDevice(_videoDevices[selectedIndex].MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;
            videoSource.Start();
            _videoSources.Add(videoSource);

            var newImage = new System.Windows.Controls.Image { Width = 320, Height = 240, Margin = new Thickness(5) };

            VideoPanel.Children.Add(newImage);
            _videoDisplays.Add(newImage);
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                var videoSource = (VideoCaptureDevice)sender;
                int index = _videoSources.IndexOf(videoSource);
                if (index >= 0 && index < _videoDisplays.Count)
                {
                    using (var bitmap = (Bitmap)eventArgs.Frame.Clone()) // Клонируем изображение
                    {
                        IntPtr hBitmap = bitmap.GetHbitmap();
                        try
                        {
                            _videoDisplays[index].Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        }
                        finally
                        {
                            DeleteObject(hBitmap); // Освобождаем GDI-ресурс
                        }
                    }
                }
            });
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            foreach (var videoSource in _videoSources)
            {
                videoSource?.SignalToStop();
            }
            _videoSources.Clear();
            VideoPanel.Children.Clear();
            _videoDisplays.Clear();
        }

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
