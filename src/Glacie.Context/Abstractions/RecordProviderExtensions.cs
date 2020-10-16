using Glacie.Infrastructure.Resolvers;

namespace Glacie.Abstractions
{
    public static class RecordProviderExtensions
    {
        public static RecordResolver AsResolver(this RecordProvider recordProvider, bool takeOwnership = false)
        {
            return new DefaultRecordResolver(recordProvider, disposeRecordProvider: takeOwnership);
        }
    }
}
