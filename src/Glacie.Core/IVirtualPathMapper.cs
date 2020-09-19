namespace Glacie
{
    public interface IVirtualPathMapper
    {
        VirtualPath Map(in VirtualPath path);
    }
}
