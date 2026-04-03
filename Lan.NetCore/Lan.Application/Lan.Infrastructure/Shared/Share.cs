using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Shared
{
    public class Share
    {
        public static Double[] GetLatLon(double latitude, double longitude, double azimuth, float AxesX, float AxesY, float azimuth_angle)
        {
            float distance = (float)Math.Sqrt(AxesX * AxesX + AxesY * AxesY);
            Double[] lonlat = new Double[2];

            if (distance > 0)
            {
                double earthRadius = 6371.393 * 1000; // 地球平均半径（米）

                // 将角度转换为弧度
                double latRad = latitude * Math.PI / 180.0;
                double lonRad = longitude * Math.PI / 180.0;
                double bearingRad = (azimuth - (-azimuth_angle)) * Math.PI / 180.0;

                // 使用大圆距离公式计算新坐标
                double angularDistance = distance / earthRadius;

                double newLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(angularDistance) +
                                           Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

                double newLonRad = lonRad + Math.Atan2(Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
                                                     Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad));

                // 转换回度数
                lonlat[0] = newLatRad * 180.0 / Math.PI;
                lonlat[1] = newLonRad * 180.0 / Math.PI;
            }
            else
            {
                lonlat[0] = latitude;
                lonlat[1] = longitude;
            }
            return lonlat;
        }
    }
}
