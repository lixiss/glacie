using System;
using System.Linq;

using Glacie.Abstractions;

using Xunit;

namespace Glacie.Data.Resources.Providers.Tests
{
    [Trait("Category", "ResourceProvider")]
    public sealed class FileSystemResourceProviderTests
    {
        [Fact]
        public void SelectDefault()
        {
            using var provider = CreateProvider("./test-data");
            var resources = provider.SelectAll().ToList();
            Assert.Equal(4, resources.Count);

            Assert.Contains(resources, (x) => x == "Dir1/Dir2/TemplateResource1.tpl");
            Assert.Contains(resources, (x) => x == "Dir1/Dir2/TemplateResource2.tpl");
            Assert.Contains(resources, (x) => x == "Dir1/Dir2/UnknownResource.tplunknown");
            Assert.Contains(resources, (x) => x == "UnrelatedDir1/Dir2/UnrelatedResource1.tpl");

            // Assert.Equal(resources[0].PhysicalPath, provider.GetByPhysicalPath(resources[0].PhysicalPath).PhysicalPath);
        }

        /*

        [Fact]
        public void SelectFiltered()
        {
            using var provider = CreateProvider("./test-data", resourceType: ResourceType.Template);
            var resources = provider.SelectAll().ToList();
            Assert.Equal(3, resources.Count);

            Assert.Contains(resources, (r) => r.Name == "Dir1/Dir2/TemplateResource1.tpl");
            Assert.Contains(resources, (r) => r.Name == "Dir1/Dir2/TemplateResource2.tpl");
            Assert.Contains(resources, (r) => r.Name == "UnrelatedDir1/Dir2/UnrelatedResource1.tpl");

            Assert.Equal(resources[0].PhysicalPath, provider.GetByPhysicalPath(resources[0].PhysicalPath).PhysicalPath);
        }

        [Fact]
        public void SelectWithVirtualBasePath()
        {
            using var provider = CreateProvider("./test-data",
                virtualBasePath: Path.From("abc"),
                resourceType: ResourceType.Template);
            var resources = provider.SelectAll().ToList();
            Assert.Equal(3, resources.Count);

            Assert.Contains(resources, (r) => r.Name == "abc/Dir1/Dir2/TemplateResource1.tpl");
            Assert.Contains(resources, (r) => r.Name == "abc/Dir1/Dir2/TemplateResource2.tpl");
            Assert.Contains(resources, (r) => r.Name == "abc/UnrelatedDir1/Dir2/UnrelatedResource1.tpl");

            Assert.Equal(resources[0].PhysicalPath, provider.GetByPhysicalPath(resources[0].PhysicalPath).PhysicalPath);
        }

        [Fact]
        public void SelectWithPhysicalBasePath()
        {
            using var provider = CreateProvider("./test-data",
                physicalBasePath: Path.From("Dir1"),
                resourceType: ResourceType.Template);
            var resources = provider.SelectAll().ToList();
            Assert.Equal(2, resources.Count);

            Assert.Contains(resources, (r) => r.Name == "Dir2/TemplateResource1.tpl");
            Assert.Contains(resources, (r) => r.Name == "Dir2/TemplateResource2.tpl");

            Assert.Equal(resources[0].PhysicalPath, provider.GetByPhysicalPath(resources[0].PhysicalPath).PhysicalPath);
        }

        [Fact]
        public void SelectWithVirtualAndPhysicalBasePath()
        {
            using var provider = CreateProvider("./test-data",
                virtualBasePath: Path.From("abc"),
                physicalBasePath: Path.From("Dir1"),
                resourceType: ResourceType.Template);
            var resources = provider.SelectAll().ToList();
            Assert.Equal(2, resources.Count);

            Assert.Contains(resources, (r) => r.Name == "abc/Dir2/TemplateResource1.tpl");
            Assert.Contains(resources, (r) => r.Name == "abc/Dir2/TemplateResource2.tpl");

            Assert.Equal(resources[0].PhysicalPath, provider.GetByPhysicalPath(resources[0].PhysicalPath).PhysicalPath);
        }

        */

        /*
        [Fact]
        public void AccessToUnrelatedResourcesShouldBeBlocked()
        {
            using var provider = CreateProvider("./test-data/Dir1");

            var result = provider.TryGet("../UnrelatedDir1/Dir2/UnrelatedResource1.tpl",
                out var resource);

            Assert.Null(resource);
            Assert.False(result);
        }
        */

        private FileSystemBundle CreateProvider(string basePath,
            in Path1 virtualBasePath = default, in Path1 physicalBasePath = default,
            ResourceType resourceType = ResourceType.None)
        {
            var resourceTypes = resourceType != ResourceType.None
                ? new ResourceType[] { resourceType }
                : null;

            return new FileSystemBundle(
                name: null,
                // virtualBasePath, PathForm.DirectorySeparator,
                // physicalBasePath, PathForm.DirectorySeparator,
                // resourceTypes,
                physicalPath: basePath,
                supportedResourceTypes: null
                );
        }
    }
}
