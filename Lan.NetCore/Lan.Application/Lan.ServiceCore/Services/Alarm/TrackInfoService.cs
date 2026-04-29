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
            var alarm = Db.Queryable<AlarmModel>().First(x => x.Id == parm.AlarmId);
            if (alarm == null)
            {
                return new Dictionary<int, object>();
            }

            // 保持原业务：按告警时间窗(15秒)取目标ID
            var targetIds = QueryTrackInfoByTimeRange(alarm.DateTime, alarm.DateTime.AddSeconds(15), alarm.AreaId)
                .Select(t => t.TargetId)
                .Distinct()
                .ToList();

            Dictionary<int, object> dic = new();

            var queryStartTime = DateTime.Parse(parm.Time);
            var queryEndTime = queryStartTime.AddSeconds(15);

            targetIds.ForEach(t =>
            {
                List<JObject> keyValuePairs = new List<JObject>();

                var tt = QueryTrackInfoByTimeRange(queryStartTime, queryEndTime, parm.AreaId, t)
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

                        if (targetIds.Count == 1)
                        {
                            keyValuePairs.Add(postJson);
                        }
                    }
                });
                dic.Add(t, keyValuePairs);
            });

            return dic;
        }

        private List<TrackInfo> QueryTrackInfoByTimeRange(DateTime startTime, DateTime endTime, int areaId, int? targetId = null)
        {
            if (startTime > endTime)
            {
                (startTime, endTime) = (endTime, startTime);
            }

            var tableNames = GetTrackInfoTableNames(startTime, endTime);
            var result = new List<TrackInfo>();

            foreach (var tableName in tableNames)
            {
                if (!Db.DbMaintenance.IsAnyTable(tableName, false))
                    continue;

                var query = Db.Queryable<TrackInfo>()
                    .AS(tableName)
                    .Where(x => x.AreaId == areaId)
                    .Where(x => x.UpdateTime >= startTime && x.UpdateTime <= endTime);

                if (targetId.HasValue)
                {
                    query = query.Where(x => x.TargetId == targetId.Value);
                }

                result.AddRange(query.ToList());
            }

            return result;
        }

        private static List<string> GetTrackInfoTableNames(DateTime startTime, DateTime endTime)
        {
            if (startTime > endTime)
            {
                (startTime, endTime) = (endTime, startTime);
            }

            var names = new List<string>();
            var cursor = new DateTime(startTime.Year, startTime.Month, 1);
            var endCursor = new DateTime(endTime.Year, endTime.Month, 1);

            while (cursor <= endCursor)
            {
                names.Add($"trackinfo{cursor:yyyyMM}");
                cursor = cursor.AddMonths(1);
            }

            return names;
        }
    }
}
