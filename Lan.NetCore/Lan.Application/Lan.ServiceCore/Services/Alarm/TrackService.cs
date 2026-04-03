using Infrastructure;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Model;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(ITrackService), ServiceLifetime = LifeTime.Transient)]
    public class TrackService : Repository<TrackModel>, ITrackService
    {
        public TrackService() { }

        public TrackModel AddTrack(TrackModel track)
        {
            return  Insertable(track).ExecuteReturnEntity();
        }
    }
}
