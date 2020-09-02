using System.Threading;

using Xunit.Abstractions;

namespace Glacie.Testing
{
    public sealed class TestFileInfo : IXunitSerializable
    {
        public string Profile { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string Path { get; private set; }

        public TestFileInfo() { }

        public TestFileInfo(string profile, string name, string path)
        {
            Profile = profile;
            Name = name;
            FullName = "{" + profile + "}/" + Name;

            Path = path;
        }

        public override string ToString()
        {
            return FullName;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }


        public void Deserialize(IXunitSerializationInfo info)
        {
            Profile = info.GetValue<string>("Profile");
            Check.That(Profile != null);

            Name = info.GetValue<string>("Name");
            Check.That(Name != null);

            FullName = info.GetValue<string>("FullName");
            Check.That(FullName != null);

            Path = info.GetValue<string>("Path");
            Check.That(Path != null);
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("Profile", Profile, typeof(string));
            info.AddValue("Name", Name, typeof(string));
            info.AddValue("FullName", FullName, typeof(string));
            info.AddValue("Path", Path, typeof(string));
        }
    }
}
