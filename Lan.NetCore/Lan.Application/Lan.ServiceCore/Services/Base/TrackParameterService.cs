using Infrastructure;
using Lan.Model;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(ITrackParameterService), ServiceLifetime = LifeTime.Transient)]
    public class TrackParameterService : Repository<TrackParameter>, ITrackParameterService
    {
        public TrackParameter GetInfo(int sys_dict_data)
        {
            var response = Queryable()
                .Where(x => x.sys_dict_data == sys_dict_data)
                .First();
            return response;
        }
    }
}
