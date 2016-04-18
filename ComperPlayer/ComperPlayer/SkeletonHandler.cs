using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows;

namespace ComperPlayer
{
    public class KinectHandler
    {
        #region 字段、属性、初始化
        public event PropertyChangedEventHandler PropertyChanged; // 通知事件处理程序

        protected string notifyMessage = null; // 断开连接的原因
        public string NotifyMessage
        {
            get
            { // 对外部可读取
                return notifyMessage;
            }
            protected set
            { // 对外部不可写
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                int index = value.IndexOf('#');
                if (index != -1)
                {
                    notifyMessage = value.Substring(index + 1);
                    if (this.PropertyChanged != null && notifyMessage != null)
                    { // 通知外部状态转换原因
                        this.PropertyChanged(this, new PropertyChangedEventArgs(value.Substring(0, index)));
                    }
                }
                else
                {
                    notifyMessage = value;
                    if (this.PropertyChanged != null && notifyMessage != null)
                    { // 通知外部状态转换原因
                        this.PropertyChanged(this, new PropertyChangedEventArgs("NOTIFY"));
                    }
                }
            }
        }

        protected bool isActive = false;

        #endregion

        #region 激活与注销抽象方法
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        protected KinectSensor sensor;       
        #endregion
    }
    public class SkeletonHandler : KinectHandler
    {


        #region 字段、属性、初始化
        private const int BytesPerPixel = 4;
       
        //指示是否找到玩家，如没有这
        public bool controllerTranked = false; //
        private bool trackerAtend = false;//every time to see if the user find, if lost, manTranked is false and clear user data to look for new user
        public int controllerId = 100;// the imposible user ID as the fresh manId data
        public JointType MouseHand;
        public JointType MouseShoulder;
        public JointType CmdHand;
        public Point mouseOnScreen;
        private int screenWidth;
        private int screenHeight;

        private const float InferredZPositionClamp = 0.1f;

        private CoordinateMapper coordinateMapper = null;
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;

        private int skeletonWidth;
        private int skeletonHeight;
       
        //骨骼数据工作模式标志，
        public enum bodyTrackState
        {
            Donothing,
            Usertracking,
            Gesturerecognition
        }
        public bodyTrackState bodyState = bodyTrackState.Donothing;        
        #endregion


        #region 轮询模式激活与注销
        public void Active(KinectSensor sensor)
        {
            if (isActive)
            {
                return;
            }
            isActive = true;
            this.sensor = sensor;
            this.coordinateMapper = sensor.CoordinateMapper;
            this.bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            this.skeletonWidth = sensor.ColorFrameSource.FrameDescription.Width;
            this.skeletonHeight = sensor.ColorFrameSource.FrameDescription.Height;
           
            this.bodyFrameReader.FrameArrived += this.Reader_Body_FrameArrived;

        }

        public void Deactive(KinectSensor sensor)
        {
            if (!isActive)
            {
                return;
            }
            isActive = false;
            this.bodyFrameReader.FrameArrived -= this.Reader_Body_FrameArrived;

        }
        #endregion

        #region 获取骨骼并处理
        //Body Resource Refresh
        private void Reader_Body_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {          
            bool dataReceived = false;
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }
            if (dataReceived)
            {
                trackerAtend = false;
                for (int i = 0; i < 6; i++)
                {
                    Body body = bodies[i];

                    if (body.IsTracked)
                    {
                        if (controllerTranked == true && body == bodies[controllerId])
                        {
                            trackerAtend = true;
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();
                            if (body == null)
                            {
                                controllerTranked = false;
                            }
                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }
                                ColorSpacePoint colorSpacePoint = coordinateMapper.MapCameraPointToColorSpace(position);
                                jointPoints[jointType] = new Point((int)colorSpacePoint.X, (int)colorSpacePoint.Y);
                                //DrawRoundImage<uint>(bodyinColorPixel, (int)jointPoints[jointType].Y, (int)jointPoints[jointType].X, displayHeight, displayWidth, 20, BodyColor[4]);                                                    
                            }
                            
                            //Action and gesture recognition
                            if (bodyState == bodyTrackState.Gesturerecognition)
                            {
                              
                            }
                            else if (bodyState == bodyTrackState.Usertracking)
                            {
                               
                            }
                        }
                        else //the user didn't identifered, now find the userId
                        {
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                            if (joints[JointType.HandLeft].Position.Y > joints[JointType.SpineShoulder].Position.Y && joints[JointType.HandRight].Position.Y > joints[JointType.SpineShoulder].Position.Y)
                            {
                                controllerTranked = true;
                                trackerAtend = true;
                                controllerId = i;

                                bodyState = bodyTrackState.Usertracking;
                                // NotifyMessage = "User Tracked " + i;

                            }
                        }

                    }
                }
                if (trackerAtend == false)
                {
                    controllerTranked = false;                 
                }
               
            }

        }

        private void DrawRoundImage<T>(T[] Dpixels, int centerH, int centerW, int depthHeight, int depthWidth, int radius, T value)
        {
            int up, down, left, right, h, w, index;
            up = (centerH - radius > 0) ? centerH - radius : 0;
            down = (centerH + radius < depthHeight - 1) ? centerH + radius : depthHeight - 1;
            left = (centerW - radius > 0) ? centerW - radius : 0;
            right = (centerW + radius < depthWidth - 1) ? centerW + radius : depthWidth - 1;

            for (h = up; h <= down; h++)
            {
                for (w = left; w <= right; w++)
                {
                    index = h * depthWidth + w;
                    if (Math.Pow(Math.Pow(h - centerH, 2) + Math.Pow(w - centerW, 2), 0.5) <= radius)
                    {
                        Dpixels[index] = value;
                    }
                }
            }
        }
        #endregion

        #region 拉模式处理所有接口
        public void LaModleInit(KinectSensor sensor)
        {
            if (isActive)
            {
                return;
            }
            isActive = true;
            mouseOnScreen = new Point();
            screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            this.sensor = sensor;
            this.coordinateMapper = sensor.CoordinateMapper;
            this.bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            this.skeletonWidth = sensor.ColorFrameSource.FrameDescription.Width;
            this.skeletonHeight = sensor.ColorFrameSource.FrameDescription.Height;
           
        }

        public void LaSkeletonRefresh(ref int fps)
        {            
            bool dataReceived = false;
            using (BodyFrame bodyFrame = bodyFrameReader.AcquireLatestFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                    ++fps;
                }
            }
            if (dataReceived)
            {
                trackerAtend = false;
                for (int i = 0; i < 6; i++)
                {
                    Body body = bodies[i];

                    if (body.IsTracked)
                    {
                        //需找到操作者用户，则根据操作者手位置和动作控制鼠标事件及移动，
                        //否则就要重新看是否有被识别到的人体举起了左手或者右手
                        if (controllerTranked == true && body == bodies[controllerId])
                        {
                            trackerAtend = true;
                            if (body == null)
                            {
                                controllerTranked = false;
                            }
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();
                            
                            //foreach (JointType jointType in joints.Keys)
                            //{
                            //    // sometimes the depth(Z) of an inferred joint may show as negative
                            //    // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            //    CameraSpacePoint position = joints[jointType].Position;
                            //    if (position.Z < 0)
                            //    {
                            //        position.Z = InferredZPositionClamp;
                            //    }
                            //    ColorSpacePoint colorSpacePoint = coordinateMapper.MapCameraPointToColorSpace(position);
                            //    jointPoints[jointType] = new Point((int)colorSpacePoint.X, (int)colorSpacePoint.Y);                                
                            //} 
                            //Console.WriteLine(jointPoints[CmdHand].X + "and" + jointPoints[CmdHand].Y);
                            //根据各个关节点坐标值关系输出控制
                            float disBase = (joints[JointType.SpineMid].Position.Y - joints[JointType.SpineBase].Position.Y);
                            mouseOnScreen.X = screenWidth * (Math.Abs(joints[MouseHand].Position.X - (joints[MouseShoulder].Position.X - disBase)) / (2 * disBase));
                            mouseOnScreen.Y = screenHeight * (1 - (Math.Abs(joints[MouseHand].Position.Y - (joints[MouseShoulder].Position.Y - disBase)) / (2 * disBase)));
                            MouseControl.SetCursorPos((int)mouseOnScreen.X, (int)mouseOnScreen.Y);
                        }
                        else if(!controllerTranked)//如果操作者没有在kinect视野区
                        {
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                            if (joints[JointType.HandLeft].Position.Y > joints[JointType.SpineShoulder].Position.Y || joints[JointType.HandRight].Position.Y > joints[JointType.SpineShoulder].Position.Y)
                            {
                                controllerTranked = true;
                                trackerAtend = true;
                                controllerId = i;

                                //根据谁举手更高判断哪只手作为控制鼠标移动，哪只手是控制命令的
                                if (joints[JointType.HandLeft].Position.Y > joints[JointType.HandRight].Position.Y)
                                {
                                    MouseHand = JointType.HandLeft;
                                    MouseShoulder = JointType.ShoulderLeft;
                                    CmdHand = JointType.HandRight;
                                }
                                else
                                {
                                    MouseHand = JointType.HandRight;
                                    MouseShoulder = JointType.ShoulderRight;
                                    CmdHand = JointType.HandLeft;
                                }
                                bodyState = bodyTrackState.Usertracking;
                                NotifyMessage = "User Tracked " + i;
                            }
                        }

                    }
                }
                if (trackerAtend == false)
                {
                    controllerTranked = false;                  
                }              
            }
        }
        #endregion

        internal void ControlImpliment()
        {
            
        }
    }
}
