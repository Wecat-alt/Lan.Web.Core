using Lan.ServiceCore.WebScoket;
using System.Drawing;

namespace Lan.ServiceCore.TargetCollection
{
    public class RadarTargetItem
    {
        /// <summary>
        /// 目标角度
        /// </summary>
        public float Angle { get; set; }
        /// <summary>
        /// 目标距离
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// 目标类型 车1 人2 树3 无法识别0
        /// </summary>
        public uint TargetType { get; set; }
        public float SpeedY { get; set; }

        public float SpeedX { get; set; }

        public float AxesX { get; set; }
        public float AxesY { get; set; }
        public int DefenceId { get; set; }

        public string RadarIP { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string NorthDeviationAngle { get; set; }
        public string MainRadarIP { get; set; }
        public float RadarOrientation { get; set; }

        public float NewX { get; set; }
        public float NewY { get; set; }

        public float NewSpeedX { get; set; }
        public float NewSpeedY { get; set; }

        //方位角
        public float AzimuthAngle { get; set; }

        /// <summary>
        /// 预警
        /// </summary>
        public bool IsEarlyWarning = false;

        /// <summary>
        /// 时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 报警目标唯一识别号
        /// </summary>
        public int TargetId { get; set; }

        /// <summary>
        /// 是否已丢失目标
        /// </summary>
        //public bool Invalid { get; set; }
        WRadar c_WRadar;

        public WRadar Radar
        {
            get { return c_WRadar; }
        }



        public RadarTargetItem(float x, float y, float f_speedY, float f_SpeedX, DateTime time, WRadar f_WRadar, uint f_TargetType, float f_AxesX, float f_AxesY, float f_AzimuthAngle, uint targetid)
        {
            float RadarOrientation1 = 0;

            TargetType = f_TargetType;
            c_WRadar = f_WRadar;
            RadarIP = c_WRadar.Ip;

            Latitude = c_WRadar.Latitude;
            Longitude = c_WRadar.Longitude;
            NorthDeviationAngle = c_WRadar.NorthDeviationAngle;

            DefenceId = c_WRadar.DefenceAreaId;



            #region 判断是否是主雷达

            float fu_x = 0f;
            float fu_y = 0f;

            //判断是否是主雷达
            WDefenceArea defenceArea = DefenceAreaManager.GetInstance()[c_WRadar.DefenceAreaId];

            if (defenceArea.angle == c_WRadar.Ip)
            {
                // 主雷达是自己
                MainRadarIP = defenceArea.angle;
                RadarOrientation1 = c_WRadar.Orientation;
            }
            else
            {
                MainRadarIP = defenceArea.angle;
                RadarOrientation1 = c_WRadar.Orientation;
            }
            #endregion


            if (y == 0)
                y = float.Epsilon;

            float s_x = x;
            float s_y = y;


            #region 坐标系转换
            //主雷达相对于副雷达坐标系的位置
            float pcPoiontx = c_WRadar.X;
            float pcPoionty = c_WRadar.Y;

            //主雷达偏转角度 以副雷达x轴 逆时针旋转到主雷达x轴为准
            float pcAngle = (float)((c_WRadar.Orientation / 180) * Math.PI);


            //此坐标也可以转正北
            //以地图0,0为基准，所有雷达转换都可以用这个转
            //主雷达测到点坐标转成副雷达坐标系的坐标
            //point 主雷达测到的点的坐标
            //返回 该点在副雷达坐标系的坐标
            float newx1 = (float)(x * Math.Cos(pcAngle) + y * Math.Sin(pcAngle) + pcPoiontx);
            float newy1 = (float)(y * Math.Cos(pcAngle) - x * Math.Sin(pcAngle) + pcPoionty);

            float angle = 90 - (float)(Math.Atan2(newx1, newy1) / Math.PI * 180);
            float distance = (float)Math.Sqrt(newx1 * newx1 + newy1 * newy1);
            if (angle > 360)
                angle -= 360;
            else if (angle < 0)
                angle += 360;

            #endregion


            UpdateTime = time;

            AzimuthAngle = f_AzimuthAngle;
            SpeedY = f_speedY;
            SpeedX = f_SpeedX;


            NewX = newx1;
            NewY = newy1;

            //xy转角度 atan2与atan 意思一样的，减少判断
            float s_angle = 90 - (float)(Math.Atan2(s_y, s_x) / Math.PI * 180);

            //x=负时候，angel 可能小于0，加上360
            if (s_angle < 0)
                s_angle += 360;

            //计算距离
            float s_distance = (float)Math.Sqrt(s_x * s_x + s_y * s_y);

            ////叠加雷达方向 ，RadarOrientation是已知的
            s_angle += c_WRadar.Orientation;

            //这里为什么要-=  += 360
            if (s_angle > 360)
                s_angle -= 360;
            else if (s_angle < 0)
                s_angle += 360;

            //重新计算x y 的坐标
            double rad = s_angle * Math.PI / 180;
            s_x = s_distance * (float)Math.Sin(rad);
            s_y = s_distance * (float)Math.Cos(rad);

            //叠加雷达的xy偏移量
            s_x += c_WRadar.X;
            s_y += c_WRadar.Y;

            //叠加雷达的xy偏移量
            s_x += fu_x;// c_WRadar.X;
            s_y += fu_y;// c_WRadar.Y;

            //这里为什么要-=  += 360
            if (s_angle > 360)
                s_angle -= 360;
            else if (s_angle < 0)
                s_angle += 360;


            //换成地图的角度和距离
            s_distance = (float)Math.Sqrt(s_x * s_x + s_y * s_y);
            s_angle = 90 - (float)(Math.Atan2(s_y, s_x) / Math.PI * 180);

            if (s_angle < 0)
                s_angle += 360;




            double rad11 = (s_angle - RadarOrientation1) * Math.PI / 180;
            float s_x11 = s_distance * (float)Math.Sin(rad11);
            float s_y11 = s_distance * (float)Math.Cos(rad11);

            AxesX = f_AxesX;
            AxesY = f_AxesY;

            AxesX = s_x11;
            AxesY = s_y11;


            Angle = s_angle;
            Distance = s_distance;
        }



        public PointF ConvertPoint()
        {
            double rad = this.Angle * Math.PI / 180.0;
            double x = this.Distance * Math.Sin(rad);
            double y = this.Distance * Math.Cos(rad);
            //Log.Debug(" 开始绘制目标点 x= " + x.ToString() + " y= " + y.ToString());
            return new PointF((float)x, (float)y);
        }

    }
}
