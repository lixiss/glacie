using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Glacie.Data.Templates;
using Glacie.Diagnostics;
using Glacie.Metadata.Builder;

using IO = System.IO;

namespace Glacie.Metadata.Providers
{
    // Always by path / resource name.
    // E.g. normalized templateName with stripped configurable suffix...
    // Would make it easier.


    // Always resolve by virtual path (templateName): database/templates/my_template.tpl
    // 1. Path can't be remapped (unlike when resolve from game resource)
    // 2. TemplateResourceProvider -> should provide Stream when it requested.
    //    Should able iterate every .tpl resource
    // 3. Then path should be mapped to name and path (later - allow custom remaps)
    // 4. ParseTemplate(Stream,Name,Path)


    // Templates (.tpl) has no embedded identifiers, so they always
    // identified by their path (in the file system). (Actually by part of path,
    // however they always in the normal form: "database/templates/filename.tpl").
    // Templates (.tpl) files may include other templates by path.
    //
    // Trace back how RecordType can be identified:
    // - uniquely by name (case-sensitive): Name usually contains just original file name,
    //   but not strictly necessary, and may be remapped to some value which will have sense.
    //   There is primary RecordType identity.
    //
    // - uniquely by path (case-sensitive): there is in-metadata paths.
    //   This path is in normal form, internally always strictly normalized path,
    //   forward slash only, case-sensitive. This path is enough to reconstruct
    //   "templateName", by joining with specified base path prefix ("database/templates/").
    //
    // - semi-uniquely by RecordType.Class (requires full metadata built):
    //   in canonical form, each non-empty "Class" resolves uniquely to RecordType.
    //   in non-canonical form, non-empty "Class" can be resolved in multiple RecordTypes.
    //   weakly typed records: some records doesn't have specified "Class" (it specified
    //   as empty), and this types can be identified only via path (templateName).
    //
    // - uniquely by templateName: because game files reference templates, they
    //   virtually exist and should be resolveable.
    //
    // Summary:
    // To define RecordType need:
    //   - Name
    //   - Path
    //   - TemplateName (can be constructed from BasePath + Path)...
    //
    // To define RecordType from .tpl we need:
    //   - TemplateName associated with given .tpl file,
    //   - Name and Path provided together with stream (to parse tpl).
    //
    // So, ParseTemplate(Stream,Name,Path) -> RecordType
    //
    // So, resolve TemplateName into: RecordType,
    // 
    // --
    //
    // All of this means what   correctly load template file we need:
    // - ability to resolve existing RecordType via templateName
    // - able to give RecordType identity from templateName (as there is no
    //   other identity source exist in this case)
    //
    //
    //
    // RecordType resolution:
    // - there is very common task to resolve record type by template name.
    //   it is so common, what I want to have embedded resolver. However, i'm
    //   want to 




    /// <summary>
    /// Read template resources into MetadataBuilder.
    /// 
    /// Each template loading request:
    /// 1. Takes TemplateName.
    /// 
    /// 
    /// 1. TemplateName mapped into: RecordTypeName, RecordTypePath
    ///    and into optionally re-mapped TemplateName.
    /// 2. 
    /// </summary>
    public sealed class TemplateMetadataReader
    {
        private readonly MetadataBuilder _metadataBuilder;
        private readonly TemplateReader _templateReader;
        private readonly ITemplateProcessor? _templateProcessor;

        // TODO: Instead of _resourceExist/_templateResourceProvider
        // just use resource provider (or resolver).
        private readonly Func<string, bool> _resourceExist;
        private readonly Func<string, IO.Stream> _templateResourceProvider;

        private readonly Func<string, string>? _recordTypeNameMapper;
        private readonly Func<Path, Path>? _recordTypePathMapper;

        public TemplateMetadataReader(
            MetadataBuilder metadataBuilder,
            ITemplateProcessor? templateProcessor,
            Func<string, bool> resourceExist,
            Func<string, IO.Stream> resourceProvider,
            Func<string, string>? recordTypeNameMapper = null,
            Func<Path, Path>? recordTypePathMapper = null)
        {
            _metadataBuilder = metadataBuilder;
            _templateReader = new TemplateReader();
            _templateProcessor = templateProcessor;
            _resourceExist = resourceExist;
            _templateResourceProvider = resourceProvider;
            _recordTypeNameMapper = recordTypeNameMapper;
            _recordTypePathMapper = recordTypePathMapper;
        }

        public RecordTypeBuilder Read(string templateName)
        {
            if (TryRead(templateName, out var result)) return result;
            else
            {
                throw Error.InvalidOperation("Unable to read template: \"{0}\".", templateName);
            }
        }

        public bool TryRead(string templateName, [NotNullWhen(true)] out RecordTypeBuilder? result)
        {
            if (TryResolveByTemplateName(templateName, out result))
            {
                return true;
            }
            else
            {
                if (_resourceExist(templateName))
                {
                    using var stream = _templateResourceProvider(templateName);
                    result = Read(stream, templateName);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool TryResolveByTemplateName(string templateName, [NotNullWhen(true)] out RecordTypeBuilder? recordType)
        {
            if (_recordTypeNameMapper != null || _recordTypePathMapper != null)
            {
                var recordTypePath = MapTemplateNameToPath(templateName);
                var recordTypeName = MapRecordTypePathToName(recordTypePath);
                if (_metadataBuilder.TryGetRecordType(recordTypeName, out recordType))
                {
                    return true;
                }
            }

            if (_metadataBuilder.TryGetRecordTypeByTemplateName(Path.Implicit(templateName), out recordType))
            {
                return true;
            }

            return false;
        }

        private RecordTypeBuilder Read(IO.Stream stream, string templateName)
        {
            var template = _templateReader.Read(stream, Path1.From(templateName), leaveOpen: false);
            _templateProcessor?.ProcessTemplate(template);

            var classVariable = template.Root.DescendantsAndSelf().OfType<TemplateVariable>()
                .Where(x => x.Name == "Class")
                .FirstOrDefault();
            var @class = classVariable?.DefaultValue;

            var uTemplateName = RestoreTemplateNameCasing(template.Name, @class);

            var recordTypePath = MapTemplateNameToPath(uTemplateName);
            var recordTypeName = MapRecordTypePathToName(recordTypePath);

            var recordType = _metadataBuilder.DefineRecordType(recordTypeName);
            recordType.Path = recordTypePath;
            new ParseContext(this, _metadataBuilder, template).ParseTo(recordType);
            return recordType;
        }

        private static string RestoreTemplateNameCasing(string templateName, string? @class)
        {
            var pathDirectory = IO.Path.GetDirectoryName(templateName);

            var pathFileName = IO.Path.GetFileNameWithoutExtension(templateName);
            var pathExtension = IO.Path.GetExtension(templateName);

            if (!string.IsNullOrEmpty(@class))
            {
                if (string.Equals(@class, pathFileName, StringComparison.OrdinalIgnoreCase))
                {
                    pathFileName = @class;
                }
            }
            var filename = pathFileName + pathExtension;

            if (!string.IsNullOrEmpty(pathDirectory))
            {
                pathDirectory = Path1.From(pathDirectory).ToFormNonEmpty(Path1Form.DirectorySeparator).ToString();
                pathDirectory = RestoreStringCasing(pathDirectory, "Database/Templates/Engine");
                pathDirectory = RestoreStringCasing(pathDirectory, "Database/Templates/InGameUI");
                pathDirectory = RestoreStringCasing(pathDirectory, "Database/Templates/InGameUI/Includes");
                pathDirectory = RestoreStringCasing(pathDirectory, "Database/Templates/TemplateBase");
                pathDirectory = RestoreStringCasing(pathDirectory, "Database/Templates/UI Templates");
            }

            if (string.IsNullOrEmpty(pathDirectory))
            {
                return filename;
            }
            else return IO.Path.Combine(pathDirectory, filename);
        }

        private static string RestoreStringCasing(string value, string properlyCased)
        {
            if (string.Equals(value, properlyCased, StringComparison.OrdinalIgnoreCase))
            {
                return properlyCased;
            }
            else
            {
                return value;
            }
        }

        private Path MapTemplateNameToPath(string templateName)
        {
            var templatePath = Path.Implicit(templateName);
            templatePath = _metadataBuilder.NormalizePath(templatePath);
            var result = templatePath
                .TrimStart(_metadataBuilder.BasePath, PathComparison.OrdinalIgnoreCase);
            if (_recordTypePathMapper != null)
            {
                result = _recordTypePathMapper(result);
            }
            return result;
        }

        private string MapRecordTypePathToName(Path path)
        {
            string result = path.ToString();
            if (result.EndsWith(".tpl", StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(0, result.Length - ".tpl".Length);
            }
            result = result
                .Replace('/', '.')
                .Replace(' ', '_');
            if (_recordTypeNameMapper != null)
            {
                result = _recordTypeNameMapper(result);
            }
            return result;
        }

        private ref struct ParseContext
        {
            private readonly TemplateMetadataReader _reader;
            private readonly MetadataBuilder _metadataBuilder;
            private readonly Template _template;

            private RecordTypeBuilder _recordTypeBuilder;

            private List<TemplateVariable>? _includeVariables;
            private Stack<FieldVarGroupBuilder> _fieldVarGroupStack;

            public ParseContext(TemplateMetadataReader reader,
                MetadataBuilder databaseTypeBuilder,
                Template template)
            {
                _reader = reader;
                _metadataBuilder = databaseTypeBuilder;
                _template = template;
                _recordTypeBuilder = null!;
                _includeVariables = null;
                _fieldVarGroupStack = new Stack<FieldVarGroupBuilder>();
                _fieldVarGroupStack.Push(databaseTypeBuilder.RootFieldVarGroup);
            }

            public void ParseTo(RecordTypeBuilder recordType)
            {
                Check.That(_recordTypeBuilder == null);
                _recordTypeBuilder = recordType;
                ParseTemplate();
            }

            // TODO: use fields instead
            private readonly MetadataBuilder DatabaseTypeBuilder
                => _metadataBuilder;

            private readonly RecordTypeBuilder RecordTypeBuilder
                => _recordTypeBuilder;

            //private readonly FieldVarGroupBuilder CurrentFieldVarGroup
            //    => _fieldVarGroupStack.Peek();

            private readonly bool HasIncludeVariables
                => _includeVariables != null && _includeVariables.Count > 0;

            private List<TemplateVariable> IncludeVariables
                => _includeVariables ?? (_includeVariables = new List<TemplateVariable>());

            private readonly Stack<FieldVarGroupBuilder> FieldVarGroupStack => _fieldVarGroupStack;

            private void ParseTemplate()
            {
                // TODO: Current implementation parse template into record type,
                // however, there is possible parse it / collect includes
                // and only when they are resolved -> declare new type. This will
                // allow to behave more better.
                // So, first/main method should visit includes first, and then
                // start to define type.

                ParseGroup(_template.Root);

                if (HasIncludeVariables)
                {
                    // TODO: Detect cycles in inheritance chains?
                    foreach (var includeVariable in IncludeVariables)
                    {
                        var includedRecordType = _reader.Read(includeVariable.DefaultValue);
                        Check.That(includedRecordType != null);
                        RecordTypeBuilder.AddInheritedFrom(includedRecordType);
                    }
                }

                Check.That(FieldVarGroupStack.Count == 1);
                Check.That(FieldVarGroupStack.Peek() == DatabaseTypeBuilder.RootFieldVarGroup);
            }

            private void ParseGroup(TemplateGroup group)
            {
                foreach (var node in group.Children)
                {
                    if (node is TemplateVariable v)
                    {
                        ParseVariable(v);
                    }
                    else if (node is TemplateGroup g)
                    {
                        var system = g.Type switch
                        {
                            "system" => true,
                            "list" => false,
                            // "lsit" => false, // Typo in TQAE templates. Fixed by TemplateProcessor.
                            _ => throw DiagnosticFactory
                                .InvalidTemplateGroupType(g.GetLocation(), g.Type)
                                .AsException(),
                        };

                        var currentGroup = FieldVarGroupStack.Peek();

                        // Both TQAE and GD sometimes do includes inside Header group,
                        // and this doesn't looks as intended to have subgroups under header.
                        // So, if this happens, just process like we are in root.
                        if (currentGroup.System && currentGroup.Name == "Header")
                        {
                            currentGroup = DatabaseTypeBuilder.RootFieldVarGroup;
                        }

                        FieldVarGroupBuilder? targetGroup;
                        if (!currentGroup.TryGetFieldVarGroup(g.Name, out targetGroup))
                        {
                            targetGroup = currentGroup.DefineFieldVarGroup(g.Name, system);
                        }
                        FieldVarGroupStack.Push(targetGroup);

                        ParseGroup(g);

                        FieldVarGroupStack.Pop();
                    }
                    else throw Error.Unreachable();
                }
            }

            private void ParseVariable(TemplateVariable variable)
            {
                switch (variable.Type)
                {
                    case "include":
                        IncludeVariables.Add(variable);
                        break;

                    case "eqnVariable":
                        if (variable.Name != "Object Variable")
                        {
                            throw DiagnosticFactory
                                .InvalidTemplateEqnVariableName(variable.GetLocation(), variable.Name, "Object Variable")
                                .AsException();
                        }

                        if (variable.Class != "static")
                        {
                            throw DiagnosticFactory
                                .InvalidTemplateEqnVariableClass(variable.GetLocation(), variable.Name, "static")
                                .AsException();
                        }

                        if (!string.IsNullOrEmpty(variable.Value))
                        {
                            throw DiagnosticFactory
                                .InvalidTemplateEqnVariableValue(variable.GetLocation(), variable.Value)
                                .AsException();
                        }

                        if (string.IsNullOrWhiteSpace(variable.DefaultValue))
                        {
                            throw DiagnosticFactory
                                .InvalidTemplateEqnVariableDefaultValue(variable.GetLocation())
                                .AsException();
                        }

                        var expressionVariableDefinition = RecordTypeBuilder.DefineExpressionVariable(variable.DefaultValue.Trim());
                        expressionVariableDefinition.Documentation = variable.Description?.Trim();
                        break;

                    default:
                        if (!string.IsNullOrWhiteSpace(variable.Name))
                        {
                            var fieldBuilder = RecordTypeBuilder
                                .DefineField(variable.Name.Trim(), FieldVarGroupStack.Peek());
                            BuildField(fieldBuilder, variable);
                        }
                        else
                        {
                            throw DiagnosticFactory
                                .TemplateVariableNameCanNotBeEmpty(variable.GetLocation())
                                .AsException();
                        }
                        break;
                }
            }

            private void BuildField(FieldTypeBuilder fieldType, TemplateVariable variable)
            {
                fieldType.Documentation = variable.Description?.Trim();

                fieldType.VarClass = variable.Class?.Trim();
                fieldType.VarType = variable.Type?.Trim();
                fieldType.VarValue = variable.Value?.Trim();
                fieldType.VarDefaultValue = variable.DefaultValue?.Trim();
            }

            private Location GetLocation()
            {
                // TODO: get location from templates...
                return Location.None;
            }
        }
    }
}
