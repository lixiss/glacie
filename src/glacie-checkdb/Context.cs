using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Glacie
{
    public sealed class Context
    {
        private readonly List<string> _diagnostics = new List<string>();

        public Context(string databasePath, string secondaryDatabasePath)
        {
            if (string.IsNullOrEmpty(databasePath)) throw new ArgumentNullException(nameof(databasePath));

            DatabasePath = databasePath;
            SecondaryDatabasePath = secondaryDatabasePath;
        }

        public string DatabasePath { get; }

        public string SecondaryDatabasePath { get; }

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
            var exists = File.Exists(realPath);
            if (!exists && !string.IsNullOrEmpty(SecondaryDatabasePath))
            {
                exists = File.Exists(Path.Combine(SecondaryDatabasePath, path));
            }
            return exists;
        }

        public string GetContent(string path)
        {
            string realPath;
            if (Path.IsPathRooted(path))
            {
                realPath = path;
                return File.ReadAllText(realPath);
            }
            else
            {
                realPath = GetAbsoluteDatabasePath(path);
                if (File.Exists(realPath))
                {
                    return File.ReadAllText(realPath);
                }
                else // try read item from secondary database path
                {
                    realPath = Path.Combine(SecondaryDatabasePath, path);
                    return File.ReadAllText(realPath);
                }
            }
        }

        public DbRecord OpenDbRecord(string relativePath)
        {
            var content = GetContent(relativePath);
            var dbRecord = new DbRecord(this, relativePath, content);
            return dbRecord;
        }

        public void Report(string message)
        {
            _diagnostics.Add(message);
            Console.WriteLine(message);
        }

        public void Report(string format, params object[] args)
        {
            var message = string.Format(CultureInfo.InvariantCulture, format, args);
            Report(message);
        }
    }
}
