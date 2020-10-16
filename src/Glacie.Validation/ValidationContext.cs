using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;
using Glacie.Data.Arz;
using Glacie.Metadata;
using Glacie.Resources;
using Glacie.Diagnostics;
using Glacie.Logging;

namespace Glacie.Validation
{
    // TODO: Try create ContextSymbol (int) and RecordSymbol (link both ContextSymbol and itself)
    // ContextSymbolMap probably can be dynamic, implemented with ConditionalWeakTable?
    // Then context can track dependent records when do validation, and do it fast...
    // If this will work - then ResolutionTokens can work with same way.


    // TODO: ValidationContext should not be part of Context,
    // it should be executed on top of context. So, Glacie.Context can't
    // consume this assembly directly / nor consume this type directly.
    // However it should able to assist validation,
    // by providing some data.


    // TODO: ValidationContext should work via Context, this means
    // it need reference Glacie.dll, while Glacie.dll/Context want to
    // reference Validation. How to break this dependencies?
    // May be easier introduce Glacie.Context assembly? May be 
    // ValidationContext might implement some common interface / abstraction?

    // Need: IRecordResolver (context.database), IArzRecordResolver (context.database)


    // TODO: Should define all required inputs for validation.
    // e.g. resource provider, rules, etc.
    // Validator however might validate separate records.
    // Validator need access to referenced resources INCLUDING records.
    // This means what it should resolve ArzRecord via some adapter, e.g.
    // IArzRecordProvider/Resolver / simple delegate.

    // TODO: ValidationContext may cache results, intermediate results,
    // because lot of resources references lot of resources ValidateResourceReference
    // (however we should anyway emit error for every field).

    // TODO: Attempt to increase performance by caching path mapping?

    public sealed class ValidationContext : IDiagnosticReporter
    {
        // public Context Context { get; }

        public Logger Log { get; }

        public MetadataResolver MetadataResolver { get; }

        public RecordResolver RecordResolver { get; }

        public ResourceResolver ResourceResolver { get; } // TODO: IResourceResolver

        public DiagnosticBag Bag { get; }

        private bool ResolveReferences { get; }

        private readonly IDiagnosticReporter _diagnosticReporter;

        public ValidationContext(Context context, ValidationConfiguration configuration)
        {
            Check.Argument.NotNull(configuration, nameof(configuration));
            Check.Argument.NotNull(configuration.DiagnosticReporter, nameof(configuration.DiagnosticReporter));
            Check.Argument.NotNull(configuration.MetadataResolver, nameof(configuration.MetadataResolver));
            Check.Argument.NotNull(configuration.RecordResolver, nameof(configuration.RecordResolver));
            Check.Argument.NotNull(configuration.ResourceResolver, nameof(configuration.ResourceResolver));

            // Configure resolvers, providers, rules, etc..., loggers and diagnosters
            // Context = context;

            Log = configuration.Logger ?? Logger.Null;
            _diagnosticReporter = configuration.DiagnosticReporter;

            MetadataResolver = configuration.MetadataResolver;
            RecordResolver = configuration.RecordResolver;
            ResourceResolver = configuration.ResourceResolver;
            ResolveReferences = configuration.ResolveReferences;

            Bag = new DiagnosticBag();
        }

        public void Report(Diagnostic diagnostic)
        {
            // There is something strange: _diagnosticReporter should be listener. ValidationContext is itself diagnostic source/reporter.
            Bag.Add(diagnostic);
            _diagnosticReporter?.Report(diagnostic);
        }

        public ValidationResult GetResult()
        {
            return new ValidationResult(Bag);
        }

        public void Validate(Record record)
        {
            if (Log.TraceEnabled)
            {
                Log.Trace("Validating Record: {0}", record.Name);
            }




            Validate(record.GetUnderlyingRecord());
        }

        private void Validate(ArzRecord record)
        {
            // TODO: Should record.Name itself be validated? YES.
            // Generally we doesn't validate here input, but given ArzRecord can
            // be in one of source database or in target database.
            // We need associated validator for every database, according to
            // their internal path form. So, need to call associated Path
            // validator for given source.
            // 1. Should be correct form, like PathForm.Relative | PathForm.Strict | PathForm.Normalized | PathForm.AltDirectorySeparator | PathForm.LowerInvariant.
            // 2. Should check for invalid characters.
            // TODO: ...
            //if (!Path.CheckInvalidCharacters(record.Name))
            //{
            //    Log.Error("Record name has invalid characters.");
            //}
            ValidateRecordPath(Path.Implicit(record.Name), record);

            if (TryGetRecordTemplateName(record, out var templateName))
            {
                // Here we doesn't care too much about templateName validity,
                // because if it get resolved, assume it valid. If not resolved,
                // then metadata itself invalid. However for pedantic checks
                // might worth it.
                //if (!Path2.CheckInvalidCharacters(templateName))
                //{
                //    Log.Error("TemplateName has invalid characters.");
                //}

                ValidateRecordByTemplateName(record, templateName);
            }

            // TODO: record's integrity: Class should match... Can this be validated by templates?
            // record should have class field.
            // record class field should match to record.Class

            // TODO: Idea: Validate that difficulty triples (game mode index) references to different values.
            // Probably LOCA has issues with that.
        }

        private bool TryGetRecordTemplateName(ArzRecord record, [NotNullWhen(returnValue: true)] out string? result)
        {
            if (record.TryGet(WellKnownFieldNames.TemplateName, ArzRecordOptions.NoFieldMap, out var templateField))
            {
                if (templateField.ValueType == ArzValueType.String)
                {
                    if (templateField.Count == 1)
                    {
                        result = templateField.Get<string>();
                        return true;
                    }
                    else
                    {
                        result = templateField.Get<string>(0);

                        // TODO: emit diagnostics
                        Report(DiagnosticFactory.RecordTemplateNameMustBeSingleValue(
                            GetFieldLocation(in templateField), result));
                        return true;
                    }
                }
                else
                {
                    Report(DiagnosticFactory.RecordTemplateNameMustBeString(
                        GetFieldLocation(in templateField)));

                    result = null;
                    return false;
                }
            }
            else
            {
                Report(DiagnosticFactory.RecordFieldTemplateNameRequired(
                    GetRecordLocation(record)));

                result = null;
                return false;
            }
        }

        private void ValidateRecordByTemplateName(ArzRecord record, string templateName)
        {
            var resolution = MetadataResolver.ResolveRecordTypeByTemplateName(Path.Implicit(templateName));
            if (resolution.HasValue)
            {
                if (resolution.HasToken)
                {
                    Report(DiagnosticFactory.RecordTypeByTemplateNameResolvedByRule(
                        Location.RecordField(record.Name, WellKnownFieldNames.TemplateName),
                        resolution.Token.Name,
                        templateName,
                        resolution.Value.TemplateName.ToString()));
                }

                var recordType = resolution.Value;
                ValidateRecordByRecordType(record, recordType);
            }
            else
            {
                Report(DiagnosticFactory.TemplateResourceNotFound(
                    Location.RecordField(record.Name, WellKnownFieldNames.TemplateName),
                    templateName
                    ));
            }
        }

        private void ValidateRecordByRecordType(ArzRecord record, RecordType recordType)
        {
            // TODO: Validate required fields. E.g. fields which defined by RecordType
            // but not present in record.

            foreach (var field in record.SelectAll())
            {
                if (recordType.TryGetField(field.Name, out var fieldType))
                {
                    ValidateField(in field, fieldType, record, recordType);
                }
                else
                {
                    // TODO: expose templateName by templates...
                    if (field.Name != "templateName")
                    {
                        Report(DiagnosticFactory.FieldTypeNotFound(
                            GetFieldLocation(in field), recordType.Name));
                    }
                }
            }
        }

        private void ValidateField(in ArzField field, FieldType fieldType, ArzRecord record, RecordType recordType)
        {
            if (fieldType.ValueType != field.ValueType)
            {
                Report(DiagnosticFactory.InvalidFieldValueType(
                    GetFieldLocation(in field), field.ValueType, fieldType.ValueType));
            }

            if (!fieldType.Array && field.Count != 1)
            {
                Report(DiagnosticFactory.FieldMustBeSingleValue(
                    GetFieldLocation(in field), field.Count));
            }

            switch (fieldType.VarType)
            {
                case "int":
                case "real":
                case "string":
                case "bool":
                    break;

                case "file_dbr":
                    if (true)
                    {
                        var fieldCount = field.Count;
                        for (var i = 0; i < fieldCount; i++)
                        {
                            var recordReference = field.Get<string>(i);
                            ValidateReferencePath(Path.Implicit(recordReference), field);
                            if (ResolveReferences)
                            {
                                ValidateRecordReference(recordReference,
                                    in field, fieldType, record, recordType);
                            }
                        }
                    }
                    break;

                case "equation":
                    // TODO: equation validation
                    break;

                //TQAE
                case "file_msh":
                case "file_tex":
                case "file_anm":
                case "file_pfx":
                case "file_wav,mp3":
                case "file_mp3,wav":
                case "file_fnt":
                case "file_ssh":
                case "file_qst":
                //GD
                case "file_cnv":
                case "file_snd":
                case "file_lua":
                case "file_txt":
                    if (true)
                    {
                        var fieldCount = field.Count;
                        for (var i = 0; i < fieldCount; i++)
                        {
                            // TODO: Create validation context struct to reference field / record to get location information.
                            var resourceReference = field.Get<string>(i);
                            ValidateReferencePath(Path.Implicit(resourceReference), field);
                            if (ResolveReferences)
                            {
                                ValidateResourceReference(resourceReference,
                                    in field, fieldType, record, recordType);
                            }
                        }
                    }
                    break;

                default:
                    throw Error.NotImplemented(string.Format("VarType = {0}", fieldType.VarType));
            }
        }

        private void ValidateRecordReference(string path, in ArzField field, FieldType fieldType, ArzRecord record, RecordType recordType)
        {
            // TODO: VirtualPath mapping should be property of RecordProvider.
            // Also ArzDatabase should support mapping when reading databases.
            // TODO: VirtualPath should support alt directory separator.
            //var mappedVirtualPath = virtualPath
            //    .ToForm(_PathForm.LowerInvariant | _PathForm.AltDirectorySeparator);
            // VirtualPathMapper.Map(dbrRef);

            var resolution = RecordResolver.ResolveRecord(Path.Implicit(path));
            if (resolution.HasValue)
            {
                if (resolution.HasToken)
                {
                    Report(DiagnosticFactory.RecordResolvedByRule(
                        Location.RecordField(record.Name, WellKnownFieldNames.TemplateName),
                        resolution.Token.Name,
                        path,
                        resolution.Value.Name));
                }

                // TODO: process record
            }
            else
            {
                Report(DiagnosticFactory.RecordResourceNotFound(
                    GetFieldLocation(field), path, fieldType.VarType));
            }
        }

        private void ValidateResourceReference(string path, in ArzField field, FieldType fieldType, ArzRecord record, RecordType recordType)
        {
            // ResourceProviders should care about mapping itself.
            // var mappedVirtualPath = VirtualPathMapper.Map(virtualPath);
            // var mappedVirtualPath = virtualPath.ToForm(_PathForm.Canonical);

            var resolution = ResourceResolver.ResolveResource(Path.Implicit(path));
            if (resolution.HasValue)
            {
                if (resolution.HasToken)
                {
                    Report(DiagnosticFactory.ResourceResolvedByRule(
                        Location.RecordField(record.Name, WellKnownFieldNames.TemplateName),
                        resolution.Token.Name,
                        path,
                        resolution.Value.Name));
                }

                // TODO: process resource
            }
            else
            {
                Report(DiagnosticFactory.ResourceNotFound(
                    GetFieldLocation(field), path, fieldType.VarType));
            }
        }

        private bool ValidateRecordPath(Path path, ArzRecord record)
        {
            const PathValidations validationOptions =
                PathValidations.Relative
                | PathValidations.Normalized
                // | PathValidations.AltDirectorySeparator
                | PathValidations.AsciiChars
                | PathValidations.FileNameCharacters
                // | PathValidations.LowerInvariantChars
                | PathValidations.SegmentNoLeadingWhiteSpace
                | PathValidations.SegmentNoTrailingWhiteSpace
                | PathValidations.SegmentNoTrailingDot
                | PathValidations.NoLeadingWhiteSpace;

            if (!path.Validate(validationOptions, out var validationResult))
            {
                if ((validationResult & PathValidations.HasRootName) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.HasRootName"));
                }
                else if ((validationResult & PathValidations.Absolute) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.Absolute"));
                }
                else if ((validationResult & PathValidations.Relative) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.Relative"));
                }
                else if ((validationResult & PathValidations.Normalized) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.Normalized"));
                }
                else if ((validationResult & PathValidations.DirectorySeparator) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.DirectorySeparator"));
                }
                else if ((validationResult & PathValidations.AltDirectorySeparator) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.AltDirectorySeparator"));
                }
                else if ((validationResult & PathValidations.AsciiChars) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.AsciiChars"));
                }
                else if ((validationResult & PathValidations.FileNameCharacters) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.FileNameChars"));
                }
                else if ((validationResult & PathValidations.LowerInvariantChars) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.LowerInvariantChars"));
                }
                else if ((validationResult & PathValidations.SegmentNoLeadingWhiteSpace) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.SegmentNoLeadingWhiteSpace"));
                }
                else if ((validationResult & PathValidations.SegmentNoTrailingWhiteSpace) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.SegmentNoTrailingWhiteSpace"));
                }
                else if ((validationResult & PathValidations.SegmentNoTrailingDot) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.SegmentNoTrailingDot"));
                }
                else if ((validationResult & PathValidations.NoLeadingWhiteSpace) != 0)
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.NoLeadingWhiteSpace"));
                }
                else
                {
                    Report(DiagnosticFactory.InvalidRecordPathAsError(GetRecordLocation(record), "Record.Path.Unknown"));
                }
                return false;
            }
            return true;
        }

        private bool ValidateReferencePath(Path path, in ArzField field)
        {
            const PathValidations validationOptions =
                PathValidations.Relative
                | PathValidations.Normalized
                // | PathValidations.AltDirectorySeparator
                | PathValidations.AsciiChars
                | PathValidations.FileNameCharacters
                // | PathValidations.LowerInvariantChars
                | PathValidations.SegmentNoLeadingWhiteSpace
                | PathValidations.SegmentNoTrailingWhiteSpace
                | PathValidations.SegmentNoTrailingDot
                | PathValidations.NoLeadingWhiteSpace;

            if (!path.Validate(validationOptions, out var validationResult))
            {
                if ((validationResult & PathValidations.HasRootName) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.HasRootName", path));
                }
                else if ((validationResult & PathValidations.Absolute) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.Absolute", path));
                }
                else if ((validationResult & PathValidations.Relative) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.Relative", path));
                }
                else if ((validationResult & PathValidations.Normalized) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.Normalized", path));
                }
                else if ((validationResult & PathValidations.DirectorySeparator) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.DirectorySeparator", path));
                }
                else if ((validationResult & PathValidations.AltDirectorySeparator) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.AltDirectorySeparator", path));
                }
                else if ((validationResult & PathValidations.AsciiChars) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.AsciiChars", path));
                }
                else if ((validationResult & PathValidations.FileNameCharacters) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.FileNameChars", path));
                }
                else if ((validationResult & PathValidations.LowerInvariantChars) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.LowerInvariantChars", path));
                }
                else if ((validationResult & PathValidations.SegmentNoLeadingWhiteSpace) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.SegmentNoLeadingWhiteSpace", path));
                }
                else if ((validationResult & PathValidations.SegmentNoTrailingWhiteSpace) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.SegmentNoTrailingWhiteSpace", path));
                }
                else if ((validationResult & PathValidations.SegmentNoTrailingDot) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.SegmentNoTrailingDot", path));
                }
                else if ((validationResult & PathValidations.NoLeadingWhiteSpace) != 0)
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.NoLeadingWhiteSpace", path));
                }
                else
                {
                    Report(DiagnosticFactory.InvalidPathAsError(GetFieldLocation(field), "Field.Path.Unknown", path));
                }
                return false;
            }
            return true;
        }

        private RecordLocation GetRecordLocation(ArzRecord record)
        {
            // TODO: Create extension for ArzRecord to make location.
            return Location.Record(record.Name);
        }

        private RecordFieldLocation GetFieldLocation(in ArzField field)
        {
            // TODO: Create extension for ArzField to make location.
            return Location.RecordField(field.Record.Name, field.Name);
        }
    }
}
