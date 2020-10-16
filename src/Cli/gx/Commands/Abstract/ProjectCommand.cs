using System.Linq;

using IO = System.IO;

namespace Glacie.Cli.Commands
{
    internal abstract class ProjectCommand : Command
    {
        private string _projectPath;
        private string? _projectPhysicalPath;

        protected ProjectCommand(string project)
            : base()
        {
            _projectPath = project;
        }

        private string GetProjectPhysicalPath(string path)
        {
            if (string.IsNullOrEmpty(path)) path = ".";
            var isCurrent = path == ".";
            path = IO.Path.GetFullPath(path);

            if (IO.File.Exists(path))
            {
                return path;
            }
            else if (IO.Directory.Exists(path))
            {
                // TODO: Move project conventions into single class, e.g. dont spread search patterns across code.

                var files = IO.Directory.EnumerateFiles(path, "*.gxproject").Take(2).ToArray();
                if (files.Length == 1)
                {
                    return files[0];
                }
                else if (files.Length == 0)
                {
                    if (isCurrent)
                    {
                        throw DiagnosticFactory
                            .CurrentDirectoryDoesNotContainProjectFile()
                            .AsException();
                    }
                    else
                    {
                        throw DiagnosticFactory
                            .ProjectFileDoesntExist()
                            .AsException();
                    }
                }
                else
                {
                    throw DiagnosticFactory
                        .DirectoryContainsMoreThanOneProjectFile()
                        .AsException();
                }
            }
            else
            {
                throw DiagnosticFactory
                    .ProjectFileDoesntExist()
                    .AsException();
            }
        }

        protected string GetProjectPhysicalPath()
        {
            if (_projectPhysicalPath != null) return _projectPhysicalPath;
            return _projectPhysicalPath = GetProjectPhysicalPath(_projectPath);
        }
    }
}
