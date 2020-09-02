using System.Collections.Generic;
using System.IO;

namespace Glacie.Testing
{
    public static class TestSuiteData
    {
        public static IEnumerable<TestFileInfo> GetArcFileNames(string profile)
        {
            var configuration = Configuration.Current;

            switch (profile)
            {
                case "tq":
                case "tqit":
                    foreach (var path in Profiles.TQIT.GetKnownArcFiles())
                    {
                        yield return new TestFileInfo("tq", path, Path.Combine(configuration.TitanQuestPath, path));
                    }
                    break;

                case "tqae":
                    foreach (var path in Profiles.TQAE.GetKnownArcFiles())
                    {
                        yield return new TestFileInfo("tqae", path, Path.Combine(configuration.TitanQuestAnniversaryEditionPath, path));
                    }
                    break;

                case "gd":
                    foreach (var path in Profiles.GD.GetKnownArcFiles())
                    {
                        yield return new TestFileInfo("gd", path, Path.Combine(configuration.GrimDawnPath, path));
                    }
                    break;

                default:
                    throw Error.Argument(nameof(profile));
            }
        }

        public static IEnumerable<TestFileInfo> GetAllArcFileNames()
        {
            foreach (var x in GetArcFileNames("tq"))
            {
                yield return x;
            }

            foreach (var x in GetArcFileNames("tqae"))
            {
                yield return x;
            }

            foreach (var x in GetArcFileNames("gd"))
            {
                yield return x;
            }
        }
    }
}
