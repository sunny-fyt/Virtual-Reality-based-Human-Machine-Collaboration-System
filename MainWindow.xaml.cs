//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Media;
    using System.Threading;
    using System.Timers;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using ConsoleApp2;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //位置信息
        private double lastRadian;
        private double currentShoulderWidth;
        private double shoulderZDifference;
        private double lastZPosition;
        private double lastXPosition;
        private double currentZPosition;
        private double currentXPosition;
        //socket对象
        private Program program = new Program();
        //手势信息
        private bool isRightHandOverHead = false;
        private bool isLeftHandOverHead = false;
        private bool isLeftHandLasso = false;
        private bool isRightHandLasso = false;
        private HandState handleftstate;
        private HandState handrightstate;
        private bool isLeftSidelift=false;
        private bool isRightSidelift=false;
        private bool isRightHandSlide = false;
        private bool isLeftHandSlide = false;
        //时间对象
        private bool newThread = false;
        //重定向
        private bool isToTheEnd = false;
        //发送消息线程
        Thread childThread;
        //检测转身的变量
        int isTurnAround = 1;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            //添加骨架
            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];//当前追踪的用户数目
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)//遍历所有的追踪的用户
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)//这里可以修改为获取数据
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }
                            this.DrawBody(joints, jointPoints, dc, drawPen);
                            //初始化线程
                            if (!newThread)
                            {
                                ThreadStart childref = new ThreadStart(sendMessage);
                                Console.WriteLine("new thread");
                                childThread = new Thread(childref);
                                childThread.Start();
                                newThread = true;
                                
                            }
                            //获取位置数据
                            if (lastZPosition == 0)
                            {
                                lastZPosition = body.Joints[JointType.SpineBase].Position.Z;
                                //Console.WriteLine("lastZ:" + lastZPosition);
                            }
                            if (lastXPosition == 0)
                            {
                                lastXPosition = body.Joints[JointType.SpineBase].Position.X;
                                //Console.WriteLine("lastX:" + lastXPosition);
                            }
                            currentZPosition = body.Joints[JointType.SpineBase].Position.Z;
                            currentXPosition = body.Joints[JointType.SpineBase].Position.X;
                            //Console.WriteLine("currentZ:" + currentZPosition + "currentX:" + currentXPosition);
                            //获取朝向数据
                            currentShoulderWidth = body.Joints[JointType.ShoulderLeft].Position.X - body.Joints[JointType.ShoulderRight].Position.X;
                            if (currentShoulderWidth < 0)
                            {
                                currentShoulderWidth = -currentShoulderWidth;
                            }
                            shoulderZDifference=body.Joints[JointType.ShoulderLeft].Position.Z - body.Joints[JointType.ShoulderRight].Position.Z;//左转为正

                            //检测左右手势
                            if (!isLeftHandOverHead)
                            {
                                if (isTurnAround == 1)
                                {
                                    isLeftHandOverHead = IsHandOverHead(body, JointType.HandLeft);
                                }
                                else
                                {
                                    isLeftHandOverHead = IsHandOverHead(body, JointType.HandRight);
                                }
                                
                            }
                            if (!isRightHandOverHead)
                            {
                                if (isTurnAround == 1)
                                {
                                    isRightHandOverHead = IsHandOverHead(body, JointType.HandRight);
                                }
                                else
                                {
                                    isRightHandOverHead = IsHandOverHead(body, JointType.HandLeft);
                                }
                            }
                            if (!isLeftHandLasso)
                            {
                                if (isTurnAround == 1)
                                {
                                    if (body.HandLeftState == HandState.Lasso)
                                    {
                                        isLeftHandLasso = true;
                                    }
                                }
                                else
                                {
                                    if (body.HandRightState == HandState.Lasso)
                                    {
                                        isLeftHandLasso = true;
                                    }
                                }
                            }
                            if (!isRightHandLasso)
                            {
                                if (isTurnAround == 1)
                                {
                                    if (body.HandRightState == HandState.Lasso)
                                    {
                                        isRightHandLasso = true;
                                    }
                                }
                                else
                                {
                                    if (body.HandLeftState == HandState.Lasso)
                                    {
                                        isRightHandLasso = true;
                                    }
                                }
                            }
                            if (!isLeftSidelift)
                            {
                                if (isTurnAround == 1)
                                {
                                    isLeftSidelift = IsSidelift(body, JointType.HandLeft, JointType.ElbowLeft, JointType.ShoulderLeft);
                                }
                                else
                                {
                                    isLeftSidelift = IsSidelift(body, JointType.HandRight, JointType.ElbowRight, JointType.ShoulderRight);
                                }
                                
                            }
                            if (!isRightSidelift)
                            {
                                if (isTurnAround == 1)
                                {
                                    isRightSidelift = IsSidelift(body, JointType.HandRight, JointType.ElbowRight, JointType.ShoulderRight);
                                }
                                else
                                {
                                    isRightSidelift = IsSidelift(body, JointType.HandLeft, JointType.ElbowLeft, JointType.ShoulderLeft);
                                }
                            }
                            //检测左右手是否滑过
                            if (!isLeftHandSlide)
                            {
                                isLeftHandSlide = IsLHandSlide(body, JointType.HandLeft);
                            }
                            if (!isRightHandSlide)
                            {
                                isRightHandSlide = IsRHandSlide(body, JointType.HandRight);
                            }

                            //检测是否需要重定向
                            if (body.Joints[JointType.SpineBase].Position.Z < 1&&!isToTheEnd)
                            {
                                childThread.Suspend();
                                SoundPlayer player = new SoundPlayer();
                                player.SoundLocation = @".\EndLocation.wav";
                                player.Load(); //同步加载声音
                                player.Play(); //启用新线程播放
                                isToTheEnd = true;
                                
                            }
                            if (body.Joints[JointType.SpineBase].Position.Z > 4&&isToTheEnd)
                            {
                                isToTheEnd = false;
                                SoundPlayer player = new SoundPlayer();
                                player.SoundLocation = @".\Suitablelocation.wav";
                                player.Load(); //同步加载声音
                                player.Play(); //启用新线程播放
                                Thread.Sleep(7000);
                                SoundPlayer player2 = new SoundPlayer();
                                player2.SoundLocation = @".\ContinueWalk.wav";
                                player2.Load(); //同步加载声音
                                player2.Play(); //启用新线程播放
                                setFalse();
                                childThread.Resume();                           
                            }

                            //绘制手
                            this.DrawHand(isLeftHandOverHead, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(isRightHandOverHead, jointPoints[JointType.HandRight], dc);
                        }

                        // prevent drawing outside of our render area
                        this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                    }
                }
            }
        }

        /// <summary>
        /// 判断手是否举过头顶
        /// </summary>
        /// <param name="body"></param>
        /// <param name="jointType"></param>
        /// <returns></returns>
        private bool IsHandOverHead(Body body, JointType jointType)
        {
            var head = body.Joints[JointType.Head];
            var hand = body.Joints[jointType];

            bool isDetected = hand.Position.Y > head.Position.Y;
            return isDetected;

        }
        private bool IsSidelift(Body body, JointType jointType1, JointType jointType2, JointType jointType3)
        {
            var hand = body.Joints[jointType1];
            var elbow = body.Joints[jointType2];
            var shoulder = body.Joints[jointType3];
            if(Math.Abs(hand.Position.Y - elbow.Position.Y)<0.05&& Math.Abs(shoulder.Position.Y - elbow.Position.Y) < 0.05)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }


        private bool IsRHandSlide(Body body, JointType jointType)
        {
            var Spine = body.Joints[JointType.SpineMid];
            var hand = body.Joints[jointType];

            bool isDetected = hand.Position.X < Spine.Position.X;
            return isDetected;

        }
        private bool IsLHandSlide(Body body, JointType jointType)
        {
            var Spine = body.Joints[JointType.SpineMid];
            var hand = body.Joints[jointType];

            bool isDetected = hand.Position.X > Spine.Position.X;
            return isDetected;

        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(bool isDetected, Point handPosition, DrawingContext drawingContext)
        {
            //手的状态不同就绘制不同的内容
            switch (isDetected)
            {
                case false:
                    //手没有举过头顶，红色
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case true:
                    //手举过头顶，绿色
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

            }
        }


        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        private void sendMessage()
        {
            while (true)
            {

                if (isLeftHandOverHead && isRightHandOverHead)
                {
                    Console.WriteLine("双手举过头顶，转身180度");
                    
                    SoundPlayer player = new SoundPlayer();
                    player.SoundLocation = @".\turn180.wav";
                    player.Load(); //同步加载声音
                    player.Play(); //启用新线程播放
                    program.turn180(isTurnAround);
                    isTurnAround = -isTurnAround;
                    Thread.Sleep(8000);
                    //重新初始化
                    lastRadian = 0;
                    lastXPosition = 0;
                    lastZPosition = 0;
                    setFalse();
                }
                else
                //手势识别有优先级，优先级为自上向下
                if (isRightHandSlide)
                {
                    Console.WriteLine("右手滑过");
                    program.turn90(1);
                    Thread.Sleep(5000);
                    setFalse();
                }else
                if (isLeftHandSlide)
                {
                    Console.WriteLine("左手滑过");
                    program.turn90(0);
                    Thread.Sleep(5000);
                    setFalse();
                }else
                
                if (isLeftHandOverHead)
                {
                    Console.WriteLine("举左手");
                    program.forwordtoDANIU();
                    Thread.Sleep(27000);
                    setFalse();
                }else
                if (isRightHandOverHead)
                {

                    Console.WriteLine("举右手");
                    program.backtoMark("name" + (program.getK()-1));
                    Thread.Sleep(20000);
                    setFalse();
                }
                if (isLeftSidelift && isRightSidelift)
                {
                    Console.WriteLine("双臂平举");
                    program.deletemark();
                    Thread.Sleep(5000);
                    setFalse();
                }
                else
                 if (isLeftSidelift)
                {
                    Console.WriteLine("左臂平举");
                    program.forword1M();
                    Thread.Sleep(5000);
                    setFalse();
                }
                else
                if (isRightSidelift)
                {
                    Console.WriteLine("右臂平举");
                    program.remark();
                    Thread.Sleep(5000);
                    setFalse();
                }
                

                else

                {
                    //角速度
                    double tanx = shoulderZDifference / currentShoulderWidth;//左转时是正的
                    if (lastRadian == 0)
                    {
                        lastRadian = Math.Atan(tanx);
                    }
                    double currentRadian = Math.Atan(tanx);
                    double radian = currentRadian - lastRadian;
                    double w = radian / 0.2;
                    //线速度
                    double xDistance = currentXPosition - lastXPosition;
                    double zDistance = currentZPosition - lastZPosition;
                    //Console.WriteLine("xDistance:" + xDistance + "zDistance:" + zDistance);                                  
                    double distance = Math.Sqrt(xDistance * xDistance + zDistance * zDistance);
                    if (zDistance > 0)
                    {
                        distance = -distance;//若后退则线速度为负值
                    }
                    double v = distance / 0.2;
                    Console.WriteLine("角速度：" + w + "线速度" + v);
                    program.move(v*isTurnAround, w);
                    while (!program.getIsReceived())
                    {
                        //Console.WriteLine("WAITING");
                    }
                    program.setIsReceived(false);
                    //更新
                    lastRadian = currentRadian;
                    lastXPosition = currentXPosition;
                    lastZPosition = currentZPosition;
                    Thread.Sleep(200);
                }  


            }
        }
        private void setFalse()
        {
            //将所有的手势识别置为false
            isLeftHandOverHead = false;
            isRightHandOverHead = false;
            isLeftHandLasso = false;
            isRightHandLasso = false;
            isLeftSidelift = false;
            isRightSidelift = false;
            isRightHandSlide = false;
            isLeftHandSlide = false;
        }


    }
}
