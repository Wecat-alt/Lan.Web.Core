
using Infrastructure;
using Lan.Application.Task;
using Lan.Infrastructure.CameraOnvif;
using Lan.Onvif;
using Lan.Repository.SqlSugar;
using Lan.ServiceCore.Onvif;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Signalr;
using Lan.ServiceCore.TargetCollection;
using MemoryCache.Core;
using Microsoft.Extensions.Caching.Memory;
using SharpRTSPtoWebRTC.WebRTCProxy;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;


namespace Lan.Application
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);
            Assembly assembly2 = typeof(onvifsdk).Assembly;
            try
            {
                NativeLibrary.SetDllImportResolver(assembly2, delegate (string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
                {
                    if (string.Equals(libraryName, "onvif_native", StringComparison.Ordinal))
                    {
                        string libraryPath = null;
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            string path = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
                            string text2 = Path.Combine(path, "onvifSdk", "x64", "OnvifClient.dll");
                            libraryPath = (File.Exists(text2) ? text2 : "OnvifClient.dll");
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            string path = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
                            string text2 = Path.Combine(path, "onvifSdk", "x64", "libonvif.so");
                            libraryPath = (File.Exists(text2) ? text2 : "libonvif.so");
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            libraryPath = "libonvif.dylib";
                        }
                        try
                        {
                            return NativeLibrary.Load(libraryPath);
                        }
                        catch
                        {
                            return IntPtr.Zero;
                        }
                    }
                    if (string.Equals(libraryName, "rbtrack_native", StringComparison.Ordinal))
                    {
                        string libraryPath2 = null;
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            string path2 = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
                            string text3 = Path.Combine(path2, "RBTrackSdk", "x64", "RBTrack.dll");
                            libraryPath2 = (File.Exists(text3) ? text3 : "RBTrack.dll");
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            string path2 = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
                            string text3 = Path.Combine(path2, "RBTrackSdk", "x64", "librbtrack.so");
                            libraryPath2 = (File.Exists(text3) ? text3 : "librbtrack.so");
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            libraryPath2 = "librbtrack.dylib";
                        }
                        try
                        {
                            return NativeLibrary.Load(libraryPath2);
                        }
                        catch
                        {
                            return IntPtr.Zero;
                        }
                    }
                    return IntPtr.Zero;
                });
            }
            catch (InvalidOperationException)
            {
            }

            var builder = webApplicationBuilder;

            //builder.WebHost.ConfigureKestrel(serverOptions =>
            //{
            //    serverOptions.ListenLocalhost(520); // 更改端口号为520或其他未被使用的端口号
            //});

            // Add services to the container.111222

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //app服务注册
            builder.Services.AddService();

            builder.Services.AddMemoryCacheCore();

            //绑定整个对象到Model上
            builder.Services.Configure<OptionsSettings>(builder.Configuration);
            //配置文件
            builder.Services.AddSingleton(new AppSettings(builder.Configuration));

            //注册SqlSugar
            builder.Services.AddSingleton(typeof(DbContext<>));

            builder.Services.AddSingleton<GlobalVariable>();

            builder.Services.AddSingleton<Lan.ServiceCore.Onvif.IOnvifManage, Lan.ServiceCore.Onvif.OnvifManage>();

            //注入SignalR实时通讯，默认用json传输
            builder.Services.AddSignalR();
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddHostedService<TrackTarget>();

            builder.Services.AddHostedService<RadarDataChannelService>();
            builder.Services.AddSingleton<RadarDataChannelService>(); //

            builder.Services.AddHostedService<AlarmBackgroundService>();
            builder.Services.AddSingleton<AlarmBackgroundService>(); // 作为单例供其他服务调用

            // 新版雷达 SDK 连接管理（独立于旧版 NsrRadarSdk）
            builder.Services.AddHostedService<RadarClientManager>();
            builder.Services.AddSingleton<RadarClientManager>(); // 作为单例供其他服务调用

            builder.Services.AddSingleton<RTSPtoWebRTCProxyService>();

            // 前端 config.js 自动更新
            builder.Services.AddTransient<ConfigJsUpdater>();

            //业务APP
            if (OperatingSystem.IsLinux())
            {
                string[] nativeFolders =
                {
                    Library_o.ResolveLinuxNativeFolder("onvifSdk"),
                    Library_o.ResolveLinuxNativeFolder("NovaPlayer"),
                    Library_o.ResolveLinuxNativeFolder("RBTrackSdk")
                };

                if (Library_o.EnsureRestartWithLdLibraryPath(nativeFolders))
                {
                    return;
                }
            }
            else
            {
                Library_o.LoadPath("onvifSdk");
                Library_o.LoadPath("NovaPlayer");
                Library_o.LoadPath("RBTrackSdk");
            }

            var app = builder.Build();

            InternalApp.ServiceProvider = app.Services;
            InternalApp.Configuration = builder.Configuration;
            InternalApp.WebHostEnvironment = app.Environment;

            // 触发 GlobalVariable 初始化（依赖 App.GetService，必须在容器构建后调用）
            app.Services.GetRequiredService<GlobalVariable>();

            // Read CORS origins from configuration array `CorsUrls`.
            var corsOrigins = builder.Configuration.GetSection("CorsUrls").Get<string[]>() ?? [];

            // ---- 应用初始化（在管道构建前执行） ----
            app.CameraInit();
            app.RadarInit();
            app.DefenceAreaInit();

            var onvifManage = app.Services.GetService<Lan.ServiceCore.Onvif.IOnvifManage>();
            onvifManage?.Init();

            RBTrackSdk.RBTRACK_DeInit();
            RBTrackSdk.RBTRACK_Init(256);

            RBTRACKManage.Init();

            DefenceAreaManager.GetInstance().EnbaleRadarEvent();

            // ==================== HTTP 中间件管道 ====================
            // 顺序：异常处理 → HTTPS → Swagger → Routing → CORS → 认证 → 授权 → 终结点

            // 1. 全局异常处理（必须在管道最前面）
            app.UseExceptionHandler(exceptionHandlerApp =>
            {
                exceptionHandlerApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"code\":500,\"msg\":\"Internal Server Error\"}");
                });
            });

            app.UseHttpsRedirection();

            // Swagger（开发/演示环境始终启用）
            app.UseSwagger();
            app.UseSwaggerUI();

            // 2. Routing 必须在 CORS 和 Auth 之前
            app.UseRouting();

            // 3. CORS
            app.UseCors(opt =>
            {
                opt.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });

            // 4. 认证 & 授权
            app.UseAuthentication();
            app.UseAuthorization();

            // 5. 终结点
            app.MapHub<MessageHub>("/hubs/stock");
            app.MapControllers();

            app.Run();
        }
    }
}
