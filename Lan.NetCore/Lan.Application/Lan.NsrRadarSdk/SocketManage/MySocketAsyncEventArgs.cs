using System.Net.Sockets;

namespace CAT.NsrRadarSdk.SocketManage
{
    internal sealed class MySocketAsyncEventArgs : SocketAsyncEventArgs
    {
        private string uid;
        private string property;
        private bool isUsing;

        internal string UID
        {
            get { return uid; }
            set { uid = value; }
        }

        internal bool IsUsing
        {
            get { return isUsing; }
            set { isUsing = value; }
        }
        internal MySocketAsyncEventArgs(string property)
        {
            this.property = property;
        }

        internal MySocketAsyncEventArgs(string property, bool isUsing)
        {
            this.property = property;
            this.isUsing = isUsing;
        }
    }
}
