using System;

using Glacie.Data.Resources;

namespace Glacie.Metadata.Builders.Templates
{
    // TODO: Instead create ResourceBundleRegistration class with all necessary
    // options, and pass them into ResourceManager into AddBundle method.
    internal readonly struct __ResourceBundleDiscoveryResult
    {
        private readonly ResourceBundle? _bundle;
        private readonly Func<ResourceBundle>? _bundleFactory;
        private readonly string? _prefix;
        private readonly string? _externalPrefix;

        public __ResourceBundleDiscoveryResult(
            ResourceBundle bundle,
            string? prefix,
            string? externalPrefix)
        {
            Check.Argument.NotNull(bundle, nameof(bundle));

            _bundle = bundle;
            _bundleFactory = null;
            _prefix = prefix;
            _externalPrefix = externalPrefix;
        }

        public __ResourceBundleDiscoveryResult(
            Func<ResourceBundle> bundleFactory,
            string? prefix,
            string? externalPrefix)
        {
            Check.Argument.NotNull(bundleFactory, nameof(bundleFactory));

            _bundle = null;
            _bundleFactory = bundleFactory;
            _prefix = prefix;
            _externalPrefix = externalPrefix;
        }


        public ResourceBundle Bundle
        {
            get
            {
                Check.That(_bundle != null);
                return _bundle;
            }
        }

        public Func<ResourceBundle> BundleFactory
        {
            get
            {
                Check.That(_bundleFactory != null);
                return _bundleFactory;
            }
        }

        public string? Prefix => _prefix;

        public string? ExternalPrefix => _externalPrefix;

        public bool IsFactory => _bundleFactory != null;
    }
}
