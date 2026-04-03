using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Infrastructure.Geometries
{
    public class GeoService
    {
        private readonly GeometryFactory _geometryFactory;

        public GeoService()
        {
            _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326); // WGS84
        }

        public bool IsPointInCircle(Coordinate point, Coordinate center, double radiusKm)
        {
            var pointGeometry = _geometryFactory.CreatePoint(point);
            var centerGeometry = _geometryFactory.CreatePoint(center);

            // 创建缓冲区（圆形区域）
            var buffer = centerGeometry.Buffer(radiusKm / 111.0); // 近似转换

            return buffer.Contains(pointGeometry);
        }

        public bool IsPointInPolygon(Coordinate point, Coordinate[] polygonCoordinates)
        {
            var pointGeometry = _geometryFactory.CreatePoint(point);
            var polygon = _geometryFactory.CreatePolygon(polygonCoordinates);

            return polygon.Contains(pointGeometry);
        }
    }
}
