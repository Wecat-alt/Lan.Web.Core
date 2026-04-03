namespace Infrastructure
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AppServiceAttribute : Attribute
    {
        public LifeTime ServiceLifetime { get; set; } = LifeTime.Scoped;
        /// <summary>
        /// 指定服务类型
        /// </summary>
        public Type ServiceType { get; set; }
        public bool InterfaceServiceType { get; set; }
    }

    public enum LifeTime
    {
        Transient, Scoped, Singleton
    }
}
