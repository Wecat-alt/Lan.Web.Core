using Lan.Model;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.IService
{
    public interface ITrackParameterService
    {
        TrackParameter GetInfo(int Id);
    }
}
