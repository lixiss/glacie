using System;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Templates;

namespace Glacie.Metadata.V1
{
    public abstract class TemplateProvider : ITemplateProvider, IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public abstract bool TryGetTemplate(in Path1 templateName,
            [NotNullWhen(returnValue: true)] out Template? result);

        public virtual Template? GetTemplateOrDefault(in Path1 templateName)
        {
            if (TryGetTemplate(in templateName, out var result)) return result;
            else return null;
        }

        public virtual Template GetTemplate(in Path1 templateName)
        {
            if (TryGetTemplate(in templateName, out var result)) return result;
            else
            {
                throw Error.InvalidOperation("Unable to get template: \"{0}\".", templateName); // TODO: Raise correct exception / error
            }
        }
    }
}
