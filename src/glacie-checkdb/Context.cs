using System;
using System.Collections.Generic;
using System.IO;

namespace Glacie
{
    public sealed class Context
    {
        private readonly List<string> _diagnostics = new List<string>();

        public Context(string databasePath)
        {
            if (string.IsNullOrEmpty(databasePath)) throw new ArgumentNullException(nameof(databasePath));

            DatabasePath = databasePath;
        }

        public string DatabasePath { get; }

        public string GetRelativeDatabasePath(string path)
        {
            return Path.GetRelativePath(DatabasePath, path);
        }

        public string GetAbsoluteDatabasePath(string path)
        {
            return Path.Combine(DatabasePath, path);
        }

        public bool IsResourceExist(string path)
        {
            var realPath = GetAbsoluteDatabasePath(path);
            return File.Exists(realPath);
        }

        public string GetContent(string path)
        {
            string realPath;
            if (Path.IsPathRooted(path))
            {
                realPath = path;
            }
            else
            {
                realPath = GetAbsoluteDatabasePath(path);
            }

            return File.ReadAllText(realPath);
        }

        public void Report(string message)
        {
            _diagnostics.Add(message);
            Console.WriteLine(message);
        }

    }
}
