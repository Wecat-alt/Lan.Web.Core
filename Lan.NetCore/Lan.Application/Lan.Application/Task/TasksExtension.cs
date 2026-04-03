using Lan.ServiceCore.TargetCollection;

namespace Lan.Application.Task
{
    public static class TasksExtension
    {
        public static IApplicationBuilder CameraInit(this IApplicationBuilder app)
        {
            CameraManager.Init();
            return app;
        }
        public static IApplicationBuilder RadarInit(this IApplicationBuilder app)
        {
            ServiceCore.WebScoket.RadarManager.Init();
            return app;
        }
        public static IApplicationBuilder DefenceAreaInit(this IApplicationBuilder app)
        {
            DefenceAreaManager.Init();
            return app;
        }
    }
}
