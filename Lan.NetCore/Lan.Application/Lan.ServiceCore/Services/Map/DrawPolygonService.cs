using Infrastructure;
using Lan.Dto;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.TargetCollection;
using Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(IDrawPolygonService), ServiceLifetime = LifeTime.Scoped)]
    public class DrawPolygonService : Repository<DrawPolygon>, IDrawPolygonService
    {
        public List<DrawPolygon> GetList()
        {
            var response = Queryable()
                .Where(u => u.Status == 1).ToList();

            return response;
        }

        private static Expressionable<RadarModel> QueryExp(RadarQueryDto parm)
        {
            var predicate = Expressionable.Create<RadarModel>();

            return predicate;
        }

        public DrawPolygon GetInfo(int Id)
        {
            var response = Queryable()
                .Where(x => x.DrawId == Id)
                .First();
            return response;
        }

        public DrawPolygon AddDrawPolygon(DrawPolygon model)
        {
            DrawPolygon drawPolygon = Insertable(model).ExecuteReturnEntity();

            foreach (WDefenceArea defence in DefenceAreaManager.GetInstance())
            {
                defence.UpdateListRadarPolygon();
            }

            return drawPolygon;
        }

        public int UpdateDrawPolygon(DrawPolygon model)
        {
            return Update(model, true);
        }
        public int DeleteDrawPolygon(string pointListLatLng)
        {
            var response =
               Deleteable()
            .EnableDiffLogEvent()
               .Where(f => f.pointListLatLng == pointListLatLng)
               .ExecuteCommand();

            foreach (WDefenceArea defence in DefenceAreaManager.GetInstance())
            {
                defence.UpdateListRadarPolygon();
            }
            return response;
        }


        public List<DrawPolygon> GetAllList()
        {
            return Queryable()
                .Where(x => x.Status == 1)
                .ToList();
        }

        public List<DrawPolygon> GetDrawPolygonByDefenceAreaId(int DefenceAreaId)
        {
            var response = Queryable()
                //.Where(x => x.DefenceAreaId == DefenceAreaId)
                .ToList();
            return response;
        }
    }
}
