using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public static class ServiceExtensions
    {
        public static void AddService(this IServiceCollection services)
        {
            string[] cls = ["Lan.Repository", "Lan.ServiceCore"];
            foreach (var item in cls)
            {
                Register(services, item);
            }
        }

        private static void Register(IServiceCollection services, string item)
        {
            Assembly assembly = Assembly.Load(item);
            foreach (var type in assembly.GetTypes())
            {
                var serviceAttribute = type.GetCustomAttribute<AppServiceAttribute>();

                if (serviceAttribute != null)
                {
                    var serviceType = serviceAttribute.ServiceType;

                    if (serviceType == null && serviceAttribute.InterfaceServiceType)
                    {
                        serviceType = type.GetInterfaces().FirstOrDefault();
                    }
                    if (serviceType == null)
                    {
                        serviceType = type;
                    }

                    switch (serviceAttribute.ServiceLifetime)
                    {
                        case LifeTime.Singleton:
                            services.AddSingleton(serviceType, type);
                            break;
                        case LifeTime.Scoped:
                            services.AddScoped(serviceType, type);
                            break;
                        case LifeTime.Transient:
                            services.AddTransient(serviceType, type);
                            break;
                        default:
                            services.AddTransient(serviceType, type);
                            break;
                    }
                }
            }
        }
    }
}
