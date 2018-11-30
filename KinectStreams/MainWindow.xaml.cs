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

namespace KinectStreams
{
    public enum Mode
    {
        Colour,
        Depth,
        Infrared
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Mode _mode = Mode.Colour;

        private KinectSensor _sensor;
        private MultiSourceFrameReader _multisourceReader;
        private BodyFrameSource _bodyFrameSource;
        private BodyFrameReader _bodyFrameReader;

        IList<Body> _bodies;
        bool _drawBody = false;

        #region Window Elements
        TextBox counter;
        Rectangle topRect;
        Rectangle botRect;
        #endregion


        int _count = 0;
        int _frameCount = 0;

        #if DEBUG
        bool debug = true;
        #endif

        #region Initialisers

        public MainWindow()
        {

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _multisourceReader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _bodyFrameSource = _sensor.BodyFrameSource;
                _bodyFrameReader = _bodyFrameSource.OpenReader();

                if (debug)
                {
                    _multisourceReader.MultiSourceFrameArrived += Reader_ShowCameraAndSkeleton;
                    _bodyFrameReader.FrameArrived += BodyReader_RefreshBodyData;
                    _bodyFrameReader.FrameArrived += BodyReader_HighlightKeyAreaBoxes;
                    _bodyFrameReader.FrameArrived += BodyReader_DrawHands;
                    //AddDebugTextBox(_reader);
                }

            }
        }

        private void BodyReader_RefreshBodyData(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    _bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(_bodies);
                }
            }
        }

        private void BodyReader_DrawHands(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null && !_drawBody )
                {
                    canvas.Children.Clear();

                    Body body = _bodies.Where(b => b.IsTracked).FirstOrDefault();

                    if (body != null)
                    {
                        canvas.DrawPoint(body.Joints[JointType.HandLeft], _mode, _sensor.CoordinateMapper);
                        canvas.DrawPoint(body.Joints[JointType.HandRight], _mode, _sensor.CoordinateMapper);
                    }
                }
            }
        }

        private void BodyReader_HighlightKeyAreaBoxes(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    //_bodies = new Body[frame.BodyFrameSource.BodyCount];
                    //frame.GetAndRefreshBodyData(_bodies);

                    Body body = _bodies.Where(b => b.IsTracked).FirstOrDefault();

                    if (body != null)
                    { 
                        // Left Hand Region
                        lhsRect.Highlight_Region(body, JointType.HandLeft, (pos, rect) => pos < rect);

                        // RHS
                        rhsRect.Highlight_Region(body, JointType.HandRight, (pos, rect) => pos > canvas.ActualWidth - rect);
                    }

                }
            }

        }

        private void AddDebugTextBox(MultiSourceFrameReader reader)
        {
            counter = new TextBox
            {
                Name="counter",
                Width=100,
                Margin= new Thickness(10),
                Padding = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            buttonPanel.Children.Add(counter);
            _multisourceReader.MultiSourceFrameArrived += Reader_ApplyDebugInformation;
            
        }

        private void Reader_ApplyDebugInformation(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // Handle changed event
            _frameCount++;
            if (_frameCount == 30)
            {
                _count++;
                counter.Text = _count.ToString();
                _frameCount = 0;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_multisourceReader != null)
            {
                _multisourceReader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        #endregion

        // Fires every time a frame changes
        private void Reader_ShowCameraAndSkeleton(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Colour
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Colour)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Depth
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Depth)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Infrared
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Infrared)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }

            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null && _drawBody)
                {
                    canvas.Children.Clear();

                    //_bodies = new Body[frame.BodyFrameSource.BodyCount];

                    //frame.GetAndRefreshBodyData(_bodies);

                    foreach (var body in _bodies)
                    {
                        if (body != null && body.IsTracked && _drawBody)
                        {
                            canvas.DrawSkeleton(body, _mode, _sensor.CoordinateMapper);
                        }
                    }
                }
            }
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Colour;
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Depth;
        }

        private void Infrared_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Infrared;
        }

        private void Body_Click(object sender, RoutedEventArgs e)
        {
            _drawBody = !_drawBody;
        }
    }
}
