using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;
using Glacie.Data.Templates;
using Glacie.Diagnostics;
using Glacie.Logging;
using Glacie.Metadata.Builder;

namespace Glacie.Metadata.Builders.Templates
{
    // TODO: use template provider instead of resolver?

    public sealed class __TemplateMetadataProvider
        : __MetadataProvider
    {
        // TODO: should use provider instead of resolver?
        private readonly __obs__TemplateResolver _templateResolver;
        private readonly ITemplateProcessor? _templateProcessor; // TODO: use concrete class
        private readonly IPath1Mapper _templateNameMapper; // TODO: avoid use this.
        private readonly Logger _log;

        private readonly MetadataBuilder _databaseTypeBuilder;

        public __TemplateMetadataProvider(__obs__TemplateResolver templateResolver,
            ITemplateProcessor? templateProcessor,
            Logger? logger = null)
        {
            _templateResolver = templateResolver;
            _templateProcessor = templateProcessor;
            _log = logger ?? Logger.Null;

            _databaseTypeBuilder = new MetadataBuilder();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _templateResolver?.Dispose();
            }
            base.Dispose(disposing);
        }

        // TODO: need ability to select DatabaseType
        public override MetadataBuilder GetDatabaseTypeBuilder()
        {
            // TODO: There is not efficient.
            foreach (var x in _templateResolver.SelectAll())
            {
                // if (x.Type != ResourceType.Template) continue;

                var recordType = GetRecordTypeBuilder(x.Path);
            }

            return _databaseTypeBuilder;
        }


        public override bool TryGetRecordTypeBuilder(in Path1 templateName,
            [NotNullWhen(returnValue: true)] out RecordTypeBuilder? result)
        {
            if (TryGetOrCreateRecordType(in templateName, out var builder))
            {
                result = builder;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private RecordTypeBuilder GetOrCreateRecordType(in Path1 templateName)
        {
            if (TryGetOrCreateRecordType(in templateName, out var result)) return result;
            else throw Error.InvalidOperation("Unable to find record type: \"{0}\".", templateName.ToString());
        }

        private bool TryGetOrCreateRecordType(in Path1 templateName,
            [NotNullWhen(returnValue: true)] out RecordTypeBuilder? result)
        {
            if (_databaseTypeBuilder.TryGetRecordType(templateName.Value, out result))
            {
                return true;
            }

            // Map and normalize template name, (needs resource resolver or path mapper).
            Path1 vTemplateName;
            if (_templateNameMapper != null)
            {
                vTemplateName = _templateNameMapper.Map(templateName);
            }
            else
            {
                vTemplateName = templateName;
            }
            vTemplateName = vTemplateName.ToForm(Constants.InternalTemplatePathForm);

            // TODO: lookup for possibly re-mapped value before resolving template
            if (_databaseTypeBuilder.TryGetRecordType(vTemplateName.Value, out result))
            {
                return true;
            }

            // TODO: template provider should map template name too...
            // _log.Trace("Resolving template: \"{0}\"", vTemplateName);
            if (_templateResolver.TryResolve(in vTemplateName, out var template))
            {
                // If templateName is different after template get resolved, then
                // look again if record definition already defined.
                if (_databaseTypeBuilder
                    .TryGetRecordType(template.Name, out result))
                {
                    return true;
                }

                // _log.Trace("Creating record type: \"{0}\"", template.Name);

                _templateProcessor?.ProcessTemplate(template);

                result = _databaseTypeBuilder
                    .DefineRecordType(template.Name);

                var context = new Context(_databaseTypeBuilder, result);
                ParseTemplate(ref context, template);

                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
        private void ParseTemplate(ref Context context, Template template)
        {
            ParseGroup(ref context, template.Root);

            if (context.HasIncludeVariables)
            {
                // TODO: Detect cycles in inheritance chains?
                foreach (var includeVariable in context.IncludeVariables)
                {
                    var includedRecordType = GetOrCreateRecordType(Path1.From(includeVariable.DefaultValue));
                    context.RecordTypeBuilder.AddInheritedFrom(includedRecordType);
                }
            }

            Check.That(context.FieldVarGroupStack.Count == 1);
            Check.That(context.FieldVarGroupStack.Peek() == context.DatabaseTypeBuilder.RootFieldVarGroup);
        }

        private void ParseGroup(ref Context context, TemplateGroup group)
        {
            foreach (var node in group.Children)
            {
                if (node is TemplateVariable v)
                {
                    ParseVariable(ref context, v);
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

                    var currentGroup = context.FieldVarGroupStack.Peek();

                    // Both TQAE and GD sometimes do includes inside Header group,
                    // and this doesn't looks as intended to have subgroups under header.
                    // So, if this happens, just process like we are in root.
                    if (currentGroup.System && currentGroup.Name == "Header")
                    {
                        currentGroup = context.DatabaseTypeBuilder.RootFieldVarGroup;
                    }

                    FieldVarGroupBuilder? targetGroup;
                    if (!currentGroup.TryGetFieldVarGroup(g.Name, out targetGroup))
                    {
                        targetGroup = currentGroup.DefineFieldVarGroup(g.Name, system);
                    }
                    context.FieldVarGroupStack.Push(targetGroup);

                    ParseGroup(ref context, g);

                    context.FieldVarGroupStack.Pop();
                }
                else throw Error.Unreachable();
            }
        }

        private void ParseVariable(ref Context context, TemplateVariable variable)
        {
            switch (variable.Type)
            {
                case "include":
                    context.IncludeVariables.Add(variable);
                    // context.IncludeVariables.Push(variable);
                    break;

                case "eqnVariable":
                    // TODO: Add diagnostics. Also Template should provide location info.
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

                    var expressionVariableDefinition = context.RecordTypeBuilder.DefineExpressionVariable(variable.DefaultValue);
                    expressionVariableDefinition.Documentation = variable.Description;
                    break;

                default:
                    if (!string.IsNullOrWhiteSpace(variable.Name))
                    {
                        var fieldBuilder = context
                            .RecordTypeBuilder
                            .DefineField(variable.Name, context.FieldVarGroupStack.Peek());
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
            fieldType.Documentation = variable.Description;

            fieldType.VarClass = variable.Class;
            fieldType.VarType = variable.Type;
            fieldType.VarValue = variable.Value;
            fieldType.VarDefaultValue = variable.DefaultValue;
        }

        private struct Context
        {
            private MetadataBuilder _databaseTypeBuilder;
            private RecordTypeBuilder _recordTypeBuilder;

            private List<TemplateVariable>? _includeVariables;
            private Stack<FieldVarGroupBuilder> _fieldVarGroupStack;

            public Context(MetadataBuilder databaseTypeBuilder,
                RecordTypeBuilder recordTypeBuilder)
            {
                _databaseTypeBuilder = databaseTypeBuilder;
                _recordTypeBuilder = recordTypeBuilder;
                _includeVariables = null;
                _fieldVarGroupStack = new Stack<FieldVarGroupBuilder>();
                _fieldVarGroupStack.Push(databaseTypeBuilder.RootFieldVarGroup);
            }

            public readonly MetadataBuilder DatabaseTypeBuilder
                => _databaseTypeBuilder;

            public readonly RecordTypeBuilder RecordTypeBuilder
                => _recordTypeBuilder;

            public readonly FieldVarGroupBuilder CurrentFieldVarGroup
                => _fieldVarGroupStack.Peek();

            public readonly bool HasIncludeVariables
                => _includeVariables != null && _includeVariables.Count > 0;

            public List<TemplateVariable> IncludeVariables
                => _includeVariables ?? (_includeVariables = new List<TemplateVariable>());

            public readonly Stack<FieldVarGroupBuilder> FieldVarGroupStack => _fieldVarGroupStack;

            public Location GetLocation()
            {
                // TODO: get location from templates...
                return Location.None;
            }
        }
    }
}
