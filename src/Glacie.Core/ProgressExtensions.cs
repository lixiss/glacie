namespace Glacie
{
    public static class ProgressExtensions
    {
        public static void SetValue(this IProgress self, long value)
        {
            self.Value = value;
        }

        public static void SetMaxValue(this IProgress self, long value)
        {
            self.MaxValue = value;
        }

        public static void SetTitle(this IProgress self, string? value)
        {
            self.Title = value;
        }

        public static void SetMessage(this IProgress self, string? value)
        {
            self.Message = value;
        }
    }
}
