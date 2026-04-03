using Microsoft.AspNetCore.SignalR;

namespace Lan.ServiceCore.Signalr
{
    public class MessageHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Clients.All.SendAsync("onlineNum", new
            {
                num = 1
            });
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }


        // 添加自定义方法供客户端调用
        public Task SendMessage(string message)
        {
            return Clients.All.SendAsync("ReceiveMessage", message);
        }

        // 控制指定客户端的方法
        public Task ControlClient(string command)
        {
            // 发送给所有客户端
            return Clients.All.SendAsync("ReceiveCommand", command);

            // 或者只发送给当前客户端
            // return Clients.Caller.SendAsync("ReceiveCommand", command);
        }

        // 控制视频播放
        public Task PlayVideo(string streamUrl)
        {
            return Clients.All.SendAsync("PlayStream", streamUrl);
        }

        // 控制窗口显示/隐藏
        public Task ShowWindow()
        {
            return Clients.All.SendAsync("ShowWindow");
        }

        public Task HideWindow()
        {
            return Clients.All.SendAsync("HideWindow");
        }
    }
}
