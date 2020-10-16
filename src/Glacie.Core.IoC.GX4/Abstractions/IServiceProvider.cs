namespace Glacie.Abstractions
{
    public interface IServiceProvider
    {
        TService Resolve<TService>();
    }
}
