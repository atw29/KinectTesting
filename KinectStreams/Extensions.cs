using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace KinectStreams
{
    public static class Extensions
    {

        #region Camera to Bitmap

        public static ImageSource ToBitmap(this ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public static ImageSource ToBitmap(this DepthFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] depthData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(depthData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex)
            {
                ushort depth = depthData[depthIndex];
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixelData[colorIndex++] = intensity; // Blue
                pixelData[colorIndex++] = intensity; // Green
                pixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8; // Pixel size (width of one row) in bytes. 

            return BitmapSource.Create(width, height, 96, 96, format, null, pixelData, stride);
        }

        public static ImageSource ToBitmap(this InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort[] infraredData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(infraredData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < infraredData.Length; ++infraredIndex)
            {
                ushort ir = infraredData[infraredIndex];
                byte intensity = (byte)(ir >> 8);

                pixelData[colorIndex++] = intensity; // Blue
                pixelData[colorIndex++] = intensity; // Green   
                pixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixelData, stride);
        }

        #endregion

        #region Body

        public static Joint ScaleTo(this Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY, Mode _mode)
        {

            joint.Position = new CameraSpacePoint
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z
            };

            return joint;
        }

        public static Joint ScaleTo(this Joint joint, double width, double height, Mode _mode)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f, _mode);
        }

        private static float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

        #endregion

        #region Draw Points

        public static void DrawPoint(this Canvas canvas, Tuple<Point, bool> point)
        {
            // 1) check whether the joint is tracked (or, more accurately, isn't NOT tracked)
            if (point.Item2)
            {

                // Create a WPF Ellipse
                Ellipse ellipse = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = new SolidColorBrush(Colors.LightBlue)
                };

                // 4) Position the ellipse according to the joint's coordinates
                Canvas.SetLeft(ellipse, point.Item1.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, point.Item1.Y - ellipse.Height / 2);

                // 5) Add the ellipse to canvas
                canvas.Children.Add(ellipse);
            }

        }

        public static void DrawSkeleton(this Canvas canvas, Body body, Mode _mode, CoordinateMapper coordinateMapper)
        {
            if (body != null)
            {

                Dictionary<JointType, Tuple<Point, bool>> pointDict = GetPointDictFromJoints(body.Joints, _mode, coordinateMapper);

                foreach (Tuple<Point, bool> point in pointDict.Values)
                {
                    canvas.DrawPoint(point);
                }

                canvas.DrawLine(pointDict[JointType.Head], pointDict[JointType.Neck]);
                canvas.DrawLine(pointDict[JointType.Neck], pointDict[JointType.SpineShoulder]);
                canvas.DrawLine(pointDict[JointType.SpineShoulder], pointDict[JointType.ShoulderLeft]);
                canvas.DrawLine(pointDict[JointType.SpineShoulder], pointDict[JointType.ShoulderRight]);
                canvas.DrawLine(pointDict[JointType.SpineShoulder], pointDict[JointType.SpineMid]);
                canvas.DrawLine(pointDict[JointType.ShoulderLeft], pointDict[JointType.ElbowLeft]);
                canvas.DrawLine(pointDict[JointType.ShoulderRight], pointDict[JointType.ElbowRight]);
                canvas.DrawLine(pointDict[JointType.ElbowLeft], pointDict[JointType.WristLeft]);
                canvas.DrawLine(pointDict[JointType.ElbowRight], pointDict[JointType.WristRight]);
                canvas.DrawLine(pointDict[JointType.WristLeft], pointDict[JointType.HandLeft]);
                canvas.DrawLine(pointDict[JointType.WristRight], pointDict[JointType.HandRight]);
                canvas.DrawLine(pointDict[JointType.HandLeft], pointDict[JointType.HandTipLeft]);
                canvas.DrawLine(pointDict[JointType.HandRight], pointDict[JointType.HandTipRight]);
                canvas.DrawLine(pointDict[JointType.HandTipLeft], pointDict[JointType.ThumbLeft]);
                canvas.DrawLine(pointDict[JointType.HandTipRight], pointDict[JointType.ThumbRight]);
                canvas.DrawLine(pointDict[JointType.SpineMid], pointDict[JointType.SpineBase]);
                canvas.DrawLine(pointDict[JointType.SpineBase], pointDict[JointType.HipLeft]);
                canvas.DrawLine(pointDict[JointType.SpineBase], pointDict[JointType.HipRight]);
                canvas.DrawLine(pointDict[JointType.HipLeft], pointDict[JointType.KneeLeft]);
                canvas.DrawLine(pointDict[JointType.HipRight], pointDict[JointType.KneeRight]);
                canvas.DrawLine(pointDict[JointType.KneeLeft], pointDict[JointType.AnkleLeft]);
                canvas.DrawLine(pointDict[JointType.KneeRight], pointDict[JointType.AnkleRight]);
                canvas.DrawLine(pointDict[JointType.AnkleLeft], pointDict[JointType.FootLeft]);
                canvas.DrawLine(pointDict[JointType.AnkleRight], pointDict[JointType.FootRight]);

            }

        }

        /// <summary>
        ///     Gets the 2D point representative dictionay from list of joints as a Dictionary of JointType to a Tuple of 2D point and Tracked State
        /// </summary>
        /// <param name="joints"></param>
        /// <param name="_mode"></param>
        /// <param name="coordinateMapper"></param>
        /// <returns>Dictionary of JointType to Tuple. Tuple is of form (2D Point, !Not Tracked) </returns>
        private static Dictionary<JointType, Tuple<Point, bool>> GetPointDictFromJoints(IReadOnlyDictionary<JointType, Joint> joints, Mode _mode, CoordinateMapper coordinateMapper)
        {
            Dictionary<JointType, Tuple<Point, bool>> dict = new Dictionary<JointType, Tuple<Point, bool>>();

            Point point = new Point();
            CameraSpacePoint jointPosition;

            foreach (KeyValuePair<JointType, Joint> pair in joints)
            {
                jointPosition = pair.Value.Position;

                if (_mode == Mode.Colour)
                {
                    ColorSpacePoint colorPoint = coordinateMapper.MapCameraPointToColorSpace(jointPosition);

                    point.X = float.IsInfinity(colorPoint.X) ? 0 : colorPoint.X;
                    point.Y = float.IsInfinity(colorPoint.Y) ? 0 : colorPoint.Y;
                }
                else // Mode == depth or infrared
                {
                    DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(jointPosition);

                    point.X = float.IsInfinity(depthSpacePoint.X) ? 0 : depthSpacePoint.X;
                    point.Y = float.IsInfinity(depthSpacePoint.Y) ? 0 : depthSpacePoint.Y;
                }

                dict.Add(pair.Key, new Tuple<Point, bool>(point, pair.Value.TrackingState != TrackingState.NotTracked));
            }

            return dict;
        }

        public static void DrawLine(this Canvas canvas, Tuple<Point, bool> first, Tuple<Point, bool> second)
        {
            if (!first.Item2 || !second.Item2) return;

            //first = first.ScaleTo(canvas.ActualWidth, canvas.ActualHeight, Mode.Colour);
            //second = second.ScaleTo(canvas.ActualWidth, canvas.ActualHeight, Mode.Colour);

            Line line = new Line
            {
                X1 = first.Item1.X,
                Y1 = first.Item1.Y,
                X2 = second.Item1.X,
                Y2 = second.Item1.Y,
                StrokeThickness = 8,
                Stroke = new SolidColorBrush(Colors.LightBlue)
            };

            canvas.Children.Add(line);
        }

        #endregion
    }
}
