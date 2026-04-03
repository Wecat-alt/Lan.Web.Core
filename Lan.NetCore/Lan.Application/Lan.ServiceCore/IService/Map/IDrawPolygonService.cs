using Lan.Dto;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.IService
{
    public interface IDrawPolygonService
    {
        List<DrawPolygon> GetList();
        DrawPolygon GetInfo(int Id);
        DrawPolygon AddDrawPolygon(DrawPolygon parm);
        int UpdateDrawPolygon(DrawPolygon parm);
        int DeleteDrawPolygon(string pointListLatLng);
        List<DrawPolygon> GetAllList();
    }
}
