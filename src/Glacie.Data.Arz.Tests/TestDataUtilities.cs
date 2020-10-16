using System;
using System.IO;

namespace Glacie.Data.Arz.Tests
{
    internal static class TestDataUtilities
    {
        private static string? s_testDataPath;

        public static string GetPath(string path)
        {
            if (s_testDataPath == null)
            {
                s_testDataPath = System.IO.Path.Combine(Environment.CurrentDirectory, "./test-data");
            }
            return System.IO.Path.Combine(s_testDataPath, path);
        }
    }
}
