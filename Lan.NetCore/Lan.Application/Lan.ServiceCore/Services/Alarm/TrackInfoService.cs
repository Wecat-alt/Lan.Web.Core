using Infrastructure;
using Lan.Model.Dto;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.TargetCollection;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(ITrackInfoService), ServiceLifetime = LifeTime.Transient)]
    public class TrackInfoService : Repository<TrackInfo>, ITrackInfoService
    {
        public int InsertTrackInfo(List<TrackInfo> trackInfoModels)
        {
            Insert(trackInfoModels);
            return 0;
        }

        public Dictionary<int, object> GetTrackInfoByAlarmId(TrackInfoQueryDto parm)
        {

            // 然后根据排序后的 TrackId 获取数据并去重
            var TargetIDs = Queryable()
                .InnerJoin<AlarmModel>((t, a) => t.AreaId == a.AreaId
                    && a.Id == parm.AlarmId
                    && t.UpdateTime >= a.DateTime
                         && t.UpdateTime <= SqlFunc.DateAdd(a.DateTime, 15, DateType.Second))

                    .Select(t => t.TargetId)
                    .Distinct()
                    .ToList();




            Dictionary<int, object> dic = new();

            TargetIDs.ForEach(t =>
            {
                List<JObject> keyValuePairs = new List<JObject>();


                var tt
                = Queryable()

                     .Where(x => x.AreaId == parm.AreaId && x.TargetId == t && x.UpdateTime >= DateTime.Parse(parm.Time)
                         && x.UpdateTime <= SqlFunc.DateAdd(DateTime.Parse(parm.Time) , 15, DateType.Second))
                     .OrderBy(u => u.TrackId)
                     .ToList();

                tt.ForEach(item =>
                {
                    if (t == item.TargetId)
                    {
                        JObject postJson = new JObject();
                        postJson["lat"] = item.Lat;
                        postJson["lng"] = item.Lng;
                        keyValuePairs.Add(postJson);

                        if (TargetIDs.Count == 1)
                        {
                            keyValuePairs.Add(postJson);
                        }
                    }
                });
                dic.Add(t, keyValuePairs);

            });
            return dic;

        }
    }
}
