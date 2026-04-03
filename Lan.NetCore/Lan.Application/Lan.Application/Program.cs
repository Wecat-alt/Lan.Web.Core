
using Infrastructure;
using Lan.Application.Task;
using Lan.Infrastructure.CameraOnvif;
using Lan.Onvif;
using Lan.Repository.SqlSugar;
using Lan.ServiceCore.Onvif;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Signalr;
using Lan.ServiceCore.TargetCollection;
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

            // Add services to the container.111

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //app服务注册
            builder.Services.AddService();

            //绑定整个对象到Model上
            builder.Services.Configure<OptionsSettings>(builder.Configuration);
            //配置文件
            builder.Services.AddSingleton(new AppSettings(builder.Configuration));

            //注册SqlSugar
            builder.Services.AddSingleton(typeof(DbContext<>));

            builder.Services.AddSingleton<GlobalVariable>();
            GlobalVariable globalVariable = new GlobalVariable();

            //注入SignalR实时通讯，默认用json传输
            builder.Services.AddSignalR();
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddHostedService<TrackTarget>();

            builder.Services.AddHostedService<RadarDataChannelService>();
            builder.Services.AddSingleton<RadarDataChannelService>(); // 

            builder.Services.AddHostedService<AlarmBackgroundService>();
            builder.Services.AddSingleton<AlarmBackgroundService>(); // 作为单例供其他服务调用

            builder.Services.AddSingleton<RTSPtoWebRTCProxyService>();

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

            // Read CORS origins from configuration array `CorsUrls`.
            var corsOrigins = builder.Configuration.GetSection("CorsUrls").Get<string[]>() ?? new string[0];

            app.UseCors(opt =>
            {
                opt.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });

            

            app.CameraInit();
            app.RadarInit();
            app.DefenceAreaInit();

            OnvifManage.Init();

            int ret1 = RBTrackSdk.RBTRACK_DeInit();
            int ret2 = RBTrackSdk.RBTRACK_Init(256);

            RBTRACKManage.Init();

            DefenceAreaManager.GetInstance().EnbaleRadarEvent();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            app.UseSwagger();
            app.UseSwaggerUI();
            //}

            app.UseHttpsRedirection();

            app.UseAuthorization();

            //signalr
            app.UseCors("AllowSpecificOrigin");
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<MessageHub>("/hubs/stock"); // 映射你的 Hub
            });

            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("An error occurred");
                }
            });

            //// 在程序结束时关闭 NLog
            //app.Lifetime.ApplicationStopped.Register(() =>
            //{
            //    LogManager.Shutdown();
            //});

            app.MapControllers();

            app.Run();
        }
    }
}
