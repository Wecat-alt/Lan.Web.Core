using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAT.NsrRadarSdk
{
    internal class RadarMessage
    {
        internal byte[] Buffer { get; set; }
        internal int Length { get; set; }
        internal string Ip { get; set; }
        internal int Port { get; set; }
    }
}
