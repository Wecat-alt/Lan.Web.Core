using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAT.NsrRadarSdk
{
    /// <summary>
    /// Exception thrown when get error while using the radar sdk.
    /// </summary>
    [Serializable]
    public class RadarException : ApplicationException
    {
        internal RadarException() : base()
        {

        }

        internal RadarException(string message) : base(message)
        {

        }

        internal RadarException(string message, Exception innerException) : base(message, innerException)
        {

        }

        internal RadarException(Exception innerException) : base(innerException.Message, innerException)
        {

        }
    }

    /// <summary>
    /// Exception thrown when use the sdk without init it.
    /// </summary>
    [Serializable]
    public class RadarSdkNotInitException : RadarException
    {
        internal RadarSdkNotInitException() : base()
        {

        }

        internal RadarSdkNotInitException(string message) : base(message)
        {

        }

        internal RadarSdkNotInitException(string message, Exception innerException) : base(message, innerException)
        {

        }

        internal RadarSdkNotInitException(Exception innerException) : base(innerException.Message, innerException)
        {

        }
    }
}
