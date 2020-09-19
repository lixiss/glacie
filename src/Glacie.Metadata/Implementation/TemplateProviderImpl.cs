using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using Glacie.Abstractions;
using Glacie.Data.Tpl;

namespace Glacie.Metadata
{
    internal sealed class TemplateProviderImpl : TemplateProvider
    {
        private ResourceProvider? _templateResourceProvider;
        private readonly TemplateReader _templateReader;

        public TemplateProviderImpl(ResourceProvider templateResourceProvider)
        {
            _templateResourceProvider = templateResourceProvider;
            _templateReader = new TemplateReader();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _templateResourceProvider?.Dispose();
                _templateResourceProvider = null;
            }
            base.Dispose(disposing);
        }

        public override bool TryGetTemplate(in VirtualPath templateName,
            [NotNullWhen(true)] out Template? result)
        {
            ThrowIfDisposed();

            if (_templateResourceProvider!.TryGet(in templateName, out var templateResource))
            {
                using var stream = templateResource.Open();
                result = _templateReader.Read(stream, templateName);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_templateResourceProvider == null) throw Error.ObjectDisposed(GetType().ToString());
        }
    }
}
