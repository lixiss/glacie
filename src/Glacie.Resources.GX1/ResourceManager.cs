using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

using Glacie.Data.Resources;
using Glacie.Logging;

namespace Glacie.Resources
{
    using ResourceMap = Dictionary<string, Resource>;

    // TODO: (High) (ResourceManager) ResourceManager is ResourceProvider which currently maps
    // prefix + inBundleResourceName to single key. This key is normalized, but
    // there is not a part of resource provider. Resource names normally should preserve their
    // original name. See (Note1) place where paths forcible converted.
    // TODO: (ResourceManager) Try to redesign it, may be in something similar to
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers?view=aspnetcore-3.1#physicalfileprovider
    // https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.fileproviders.ifileprovider?view=dotnet-plat-ext-3.1
    // Also ResourceType currently includes .tpl, .gxmd, .gxmdi / etc which is not part of this resources...
    // So need new redesign. Also see comments in RoadMap.md.

    // TODO: (Low) (ResourceManager) make resource manager to properly work in case-sensitive mode.
    // TODO: Probably insertion (prefixes) should be organized in some other form, there is tree-like
    // structure.

    // TODO: Register bundles with priorities (generally source_ids also part of priority),
    // internal language support already use local priorities.
    // Note, what bundles with same priority should not override resources with same priority regardles from which bundle they come.

    /// <summary>
    /// Manages resources.
    /// Every bundle can be registered with own prefix.
    /// All exposed resource are keyed by virtual path (Path) property.
    /// </summary>
    public sealed partial class ResourceManager : ResourceProvider
    {
        // TODO: This applicable only for prefix
        private const PathConversions DefaultVirtualPathForm
            = PathConversions.Relative
            | PathConversions.Strict
            | PathConversions.Normalized;

        private List<IDisposable> _disposables;
        private bool _disposed;

        private readonly bool _isCaseSensitive;

        private readonly PathConversions _virtualPathForm;
        private readonly Logger _log;

        private readonly string? _language;

        private readonly Dictionary<string, PrefixRegistration> _prefixRegistrations;
        private ResourceMap? _resourceMap;

        private ResourceResolver? _resolver;
        private ResourceResolver? _resolverWithTakenOwnership;

        public ResourceManager(string? language, Logger? logger)
            : this(pathForm: DefaultVirtualPathForm, language: language, caseSensitive: false, logger: logger)
        { }

        public ResourceManager(PathConversions pathForm, string? language, bool caseSensitive, Logger? logger = null)
        {
            _isCaseSensitive = caseSensitive;

            _disposables = new List<IDisposable>();

            _virtualPathForm = pathForm;
            _language = NormalizeLanguage(language);

            _log = logger ?? Logger.Null;

            _prefixRegistrations = new Dictionary<string, PrefixRegistration>(GetPrefixMapComparer(caseSensitive));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                List<Exception>? observedExceptions = null;

                if (!_disposed)
                {
                    foreach (var disposable in _disposables)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception ex)
                        {
                            if (observedExceptions == null) observedExceptions = new List<Exception>();
                            observedExceptions.Add(ex);

                            try
                            {
                                _log.Log(LogLevel.Critical, ex, "Failed to dispose object: {0}", disposable.GetType().ToString());
                            }
                            catch (Exception ex2)
                            {
                                if (observedExceptions == null) observedExceptions = new List<Exception>();
                                observedExceptions.Add(ex2);
                            }
                        }
                    }
                    _disposables = null!;

                    _disposed = true;
                }

                if (observedExceptions != null)
                {
                    throw new AggregateException("ResourceManager::Dispose() failed.", observedExceptions);
                }
            }

            base.Dispose(disposing);
        }

        private void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        public bool IsCaseSensitive => _isCaseSensitive;

        private PrefixRegistration GetOrCreatePrefixRegistration(Path prefix)
        {
            var key = prefix.ToString();

            if (!_prefixRegistrations.TryGetValue(key, out var prefixRegistration))
            {
                prefixRegistration = new PrefixRegistration(this, prefix);
                _prefixRegistrations.Add(key, prefixRegistration);
            }

            return prefixRegistration;
        }

        private Path MapPrefix(string? prefix, bool strict = false)
            => MapPrefix(Path.Implicit(prefix), strict);

        private Path MapPrefix(Path prefix, bool strict = false)
        {
            return prefix.ConvertNonEmpty(_virtualPathForm, check: true);

            //throw Error.NotImplemented("Conversion still need, but not sure what they should be in this way.");

            //var result = prefix.ToFormNonEmpty(_virtualPathForm);
            //if (strict && !result.IsEmpty && !result.IsInForm(_virtualPathForm))
            //{
            //    throw Error.Argument(nameof(prefix), "Prefix doesn't conform to virtual path form.");
            //}
            //return result;
        }

        public int Count => GetResourceMap().Values.Count;

        #region ResourceProvider

        public override IEnumerable<Resource> SelectAll()
        {
            return GetResourceMap().Values;
        }

        public override bool TryGetResource(Path path, [NotNullWhen(true)] out Resource? result)
        {
            if (GetResourceMap().TryGetValue(path.ToString(), out result)) return true;
            else return false;
        }

        public override bool Exists(Path path)
        {
            // TODO: Implement look-up reverse logic, e.g. when we check presense,
            // there is no matter in which active bundle resource found.

            return TryGetResource(path, out var _);
        }

        #endregion

        public void AddBundle(string prefix, string? language, string? bundleName, ushort sourceId, Func<ResourceBundle> bundleFactory)
        {
            // TODO: Support lazy bundle creation.

            var bundle = bundleFactory.Invoke();
            try
            {
                AddBundle(prefix, language, bundleName, sourceId, bundle);
            }
            catch
            {
                bundle.Dispose();
                throw;
            }
        }

        public void AddBundle(string prefix, string? language, string? bundleName, ushort sourceId, ResourceBundle bundle, bool disposeBundle = true)
        {
            // TODO: Note, that registering bundle with same priority, for same prefix is error. Need check.

            // TODO: With new PathComparer - prefixes always should ends with directory separartor,
            // except empty prefix

            if (!string.IsNullOrEmpty(prefix) && !Path.EndsInDirectorySeparator(prefix))
            {
                prefix = prefix + Path.PreferredDirectorySeparatorChar;
            }

            var mappedPrefixKey = MapPrefix(prefix, strict: true);
            var prefixRegistration = GetOrCreatePrefixRegistration(mappedPrefixKey);

            var bundleRegistration = prefixRegistration.AddBundle(language, bundleName, sourceId, bundle, disposeBundle);
            _log.Information("  Bundle: {0} => {1} ({2})",
                bundleRegistration.Name, bundleRegistration.Prefix.Path.ToString(), bundleRegistration.Language);
        }

        public ResourceResolver AsResolver(bool takeOwnership = false)
        {
            if (!takeOwnership)
            {
                if (_resolver != null) return _resolver;
                return _resolver = new Resolver(this, disposeResourceManager: takeOwnership);
            }
            else
            {
                if (_resolverWithTakenOwnership != null) return _resolverWithTakenOwnership;
                return _resolverWithTakenOwnership = new Resolver(this, disposeResourceManager: takeOwnership);
            }

        }

        private IEnumerable<BundleRegistration> SelectBundleRegistrations()
        {
            foreach (var prefixRegistration in _prefixRegistrations.Values)
            {
                foreach (var bundleRegistration in prefixRegistration.SelectBundleRegistrations())
                {
                    if (bundleRegistration.IsLanguageNeutral
                        || bundleRegistration.Language == _language)
                    {
                        yield return bundleRegistration;
                    }
                }
            }
        }

        private ResourceMap GetResourceMap()
        {
            if (_resourceMap != null) return _resourceMap;
            return _resourceMap = CreateResourceMap();
        }

        private ResourceMap CreateResourceMap()
        {
            // TODO: We need LogManager, to have named loggers. How to better do this?
            _log.Debug("resman: Creating resource map...");

            var resourceMap = new ResourceMap(GetResourceMapComparer(_isCaseSensitive));

            // Process bundleRegistrations in reverse priority order, to ensures
            // what all bundles with same priority will be processed in order,
            // so we ensure what there is no resource overrides with same resource priority.
            // TODO: However, in this case there is should be diagnostic error emitted
            // instead of throw exception.
            var bundleRegistrations = SelectBundleRegistrations()
                .OrderByDescending(x => x.Priority);

            foreach (var bundleRegistration in bundleRegistrations)
            {
                // TODO: Support languages properly, currently ignoring all language-dependent bundles.
                //if (bundleRegistration.Language != null)
                //{
                //    _log.Warning("Language-dependent bundle registration found: ({0}) {1}. Skipping bundle, because it is currently is not supported.",
                //        bundleRegistration.Language, bundleRegistration.Name);
                //    continue;
                //}

                _log.Debug("resman:   Mapping bundle: {0}", bundleRegistration.Name);

                var prefixPath = bundleRegistration.Prefix.Path;

                var bundlePriority = bundleRegistration.Priority;

                var bundle = bundleRegistration.GetBundle();
                foreach (var bundleResourcePath in bundle.SelectAll())
                {
                    var resourcePath = Path.Join(prefixPath, bundleResourcePath,
                        Path.GetPreferredDirectorySeparator(_virtualPathForm));

                    var mappedResourcePath = resourcePath; // Map(resourcePath);

                    // (Note1) See TODO note above.
                    var resourceKeyPath = mappedResourcePath;
                    var resourceKeyPathString = mappedResourcePath.ToString();

                    if (resourceMap.TryGetValue(resourceKeyPathString, out var resource))
                    {
                        if (IsOverrideAllowed(resource.Priority, bundlePriority)) // zero is highest priority, matches to target
                        {
                            // Allow override.
                        }
                        else
                        {
                            // Block overriding with invalid priority.
                            throw DiagnosticFactory.AttemptToOverrideResourceWithSamePriorityWasBlocked(resource.Path,
                                resource.Bundle.Name, bundle.Name)
                                .AsException();
                        }
                    }
                    else
                    {
                        resource = new Resource(resourceKeyPath, bundle, bundleResourcePath,
                            sourceId: bundleRegistration.SourceId,
                            localPriority: bundleRegistration.LocalPriority);

                        resourceMap.Add(resourceKeyPathString, resource);
                    }
                }
            }

            _log.Debug("resman: {0} resources", resourceMap.Count);

            return resourceMap;
        }

        private static bool IsOverrideAllowed(uint currentPriority, uint newPriority)
        {
            return currentPriority > newPriority;
        }

        private static string? NormalizeLanguage(string? language)
        {
            if (string.IsNullOrEmpty(language)) return null;
            if ("en".Equals(language, StringComparison.OrdinalIgnoreCase)) return null;
            return language.ToLowerInvariant();
        }

        // TODO: Language-based resource resolving currently uses EN as fallback, and there is not in all cases a good choice.
        // For validation purposes we may want ensure what we have all language-dependent resources too for specific language.
        // But currently there is easier to use EN as fallback.
        private static bool IsLanguageNeutral(string? language)
        {
            // Note, this check happens after NormalizeLanguage, 
            // so current EN always turned into null.

            if (string.IsNullOrEmpty(language)) return true;
            return "en".Equals(language, StringComparison.OrdinalIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint GetGlobalPriority(ushort sourceId, uint localPriority)
        {
            return (uint)sourceId << 16 | localPriority;
        }

        private static IEqualityComparer<string> GetPrefixMapComparer(bool caseSensitive)
        {
            if (caseSensitive) return PathComparer.Ordinal;
            else return PathComparer.OrdinalIgnoreCase;
        }

        private static IEqualityComparer<string> GetResourceMapComparer(bool caseSensitive)
        {
            if (caseSensitive) return PathComparer.Ordinal;
            else return PathComparer.OrdinalIgnoreCase;
        }
    }
}
