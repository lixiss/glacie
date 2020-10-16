using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data;
using Glacie.Data.Templates;
using Glacie.Targeting.Infrastructure;

namespace Glacie.Targeting
{
    internal interface IEngineType
    {
        /// <summary>
        /// There is sometimes may be handful to check which engine class is
        /// used without relying on implemented type. E.g. allows to check
        /// what given IEngineType type instance is for TQ/TQIT/TQAE/GD.
        /// </summary>
        EngineClass EngineClass { get; }

        #region Metadata

        /// <summary>
        /// Template processor used to fix some typo's or errors in templates,
        /// after them loaded.
        /// </summary>
        ITemplateProcessor? GetTemplateProcessor();

        /// <summary>
        /// Allow exclude some template names from loading.
        /// </summary>
        bool IsExcludedTemplateName(Path templateName);

        /// <summary>
        /// Remap record type names (used for restore proper type name casing or
        /// to perform consistent renaming), during template reading.
        /// </summary>
        string MapRecordTypeName(string name);

        /// <summary>
        /// Remap record type paths (used for restore proper type path casing) during template reading.
        /// </summary>
        bool TryMapRecordTypePath(Path path, [NotNullWhen(true)] out Path result);

        /// <summary>
        /// Same as <see cref="TryMapRecordTypePath(in Path1, out Path1)"/> but returns
        /// mapped path, or original path if it has not been mapped.
        /// </summary>
        Path MapRecordTypePath(Path path);

        #endregion

        #region Database

        DatabaseConventions DatabaseConventions { get; }

        #endregion
    }
}
