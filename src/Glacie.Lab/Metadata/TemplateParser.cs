using System.Collections.Generic;

using Glacie.Data.Tpl;
using Glacie.Data.Metadata.Emit;
using Glacie.Metadata;

namespace Glacie.Lab.Metadata
{
    // TODO: This is not a parser actually. This might be RecordDefinitionProvider / something.

    internal sealed class TemplateParser
    {
        private TemplateProvider _templateProvider;
        private DatabaseTypeBuilder _databaseDefinitionBuilder;

        public TemplateParser(TemplateProvider templateProvider, DatabaseTypeBuilder databaseDefinitionBuilder)
        {
            _templateProvider = templateProvider;
            _databaseDefinitionBuilder = databaseDefinitionBuilder;
        }

        public RecordTypeBuilder Parse(string templateName)
        {
            // TODO: this should normalize input name

            if (_databaseDefinitionBuilder
                .TryGetRecordDefinition(templateName, out var result))
            {
                return result;
            }

            //Console.Out.WriteLine("Resolving Template: {0}", templateName);
            var template = _templateProvider.GetTemplate(templateName);
            //Console.Out.WriteLine("  -> {0}", template.Name);

            // If templateName is different after template get resolved, then
            // look again if record definition already defined.
            if (_databaseDefinitionBuilder
                .TryGetRecordDefinition(template.Name, out result))
            {
                return result;
            }

            result = _databaseDefinitionBuilder
                .DefineRecordDefinition(template.Name);

            var context = new Context(_databaseDefinitionBuilder, result);
            ParseTemplate(ref context, template);

            return result;
        }

        private void ParseTemplate(ref Context context, Template template)
        {
            ParseGroup(ref context, template.Root);

            if (context.HasIncludeVariables)
            {
                // TODO: Detect cycles in inheritance chains?
                foreach (var includeVariable in context.IncludeVariables)
                {
                    var includedRecordDefinition = Parse(includeVariable.DefaultValue);
                    context.RecordDefinitionBuilder.AddInheritedFrom(includedRecordDefinition);
                }
                //while (context.IncludeVariables.TryPop(out var includeVariable))
                //{
                //    var includedRecordDefinition = Parse(includeVariable.DefaultValue);
                //    context.RecordDefinitionBuilder.AddInheritedFrom(includedRecordDefinition);
                //}
            }
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
                        "lsit" => false, // Typo in TQAE templates
                        _ => throw Error.InvalidOperation("Invalid group type \"{0}\".", g.Type), // TODO: diagnostics
                    };

                    var currentGroup = context.FieldGroupDefinitions.Peek();

                    // Both TQAE and GD sometimes do includes inside Header group,
                    // and this doesn't looks as intended to have subgroups under header.
                    // So, if this happens, just process like we are in root.
                    if (currentGroup.System && currentGroup.Name == "Header")
                    {
                        currentGroup = context.DatabaseDefinitionBuilder.RootFieldGroupDefinition;
                    }

                    FieldGroupBuilder? targetGroup;
                    if (!currentGroup.TryGetFieldGroupDefinition(g.Name, out targetGroup))
                    {
                        targetGroup = currentGroup.DefineFieldGroupDefinition(g.Name, system);
                    }
                    context.FieldGroupDefinitions.Push(targetGroup);

                    ParseGroup(ref context, g);

                    context.FieldGroupDefinitions.Pop();
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
                    if (variable.Name != "Object Variable") throw Error.InvalidOperation("Template variable of type \"eqnVariable\" has invalid property value (Name).");
                    if (variable.Class != "static") throw Error.InvalidOperation("Template variable of type \"eqnVariable\" has invalid property value (Class).");
                    if (!string.IsNullOrEmpty(variable.Value)) throw Error.InvalidOperation("Template variable of type \"eqnVariable\" has invalid property value (Value).");
                    if (string.IsNullOrWhiteSpace(variable.DefaultValue)) throw Error.InvalidOperation("Template variable of type \"eqnVariable\" must specify defaultValue property.");

                    var expressionVariableDefinition = context.RecordDefinitionBuilder.DefineExpressionVariableDefinition(variable.DefaultValue);
                    expressionVariableDefinition.Description = variable.Description;
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(variable.Name)) throw Error.InvalidOperation("Template variable should have name.");
                    var fieldDefinitionBuilder = context.RecordDefinitionBuilder
                        .DefineFieldDefinition(
                            context.FieldGroupDefinitions.Peek(),
                            variable.Name);
                    BuildField(fieldDefinitionBuilder, variable);
                    break;
            }
        }

        private void BuildField(FieldTypeBuilder fieldDefinitionBuilder, TemplateVariable variable)
        {
            fieldDefinitionBuilder.Description = variable.Description;

            fieldDefinitionBuilder.VarClass = variable.Class;
            fieldDefinitionBuilder.VarType = variable.Type;
            fieldDefinitionBuilder.VarValue = variable.Value;
            fieldDefinitionBuilder.VarDefaultValue = variable.DefaultValue;
        }

        private struct Context
        {
            private DatabaseTypeBuilder _databaseDefinitionBuilder;
            private RecordTypeBuilder _recordDefinitionBuilder;

            private List<TemplateVariable>? _includeVariables;
            private Stack<FieldGroupBuilder> _fieldGroupDefinitionStack;

            public Context(DatabaseTypeBuilder databaseDefinitionBuilder,
                RecordTypeBuilder recordDefinitionBuilder)
            {
                _databaseDefinitionBuilder = databaseDefinitionBuilder;
                _recordDefinitionBuilder = recordDefinitionBuilder;
                _includeVariables = null;
                _fieldGroupDefinitionStack = new Stack<FieldGroupBuilder>();
                _fieldGroupDefinitionStack.Push(databaseDefinitionBuilder.RootFieldGroupDefinition);
            }

            public readonly DatabaseTypeBuilder DatabaseDefinitionBuilder
                => _databaseDefinitionBuilder;

            public readonly RecordTypeBuilder RecordDefinitionBuilder
                => _recordDefinitionBuilder;

            public readonly FieldGroupBuilder CurrentFieldGroupDefinition
                => _fieldGroupDefinitionStack.Peek();

            public readonly bool HasIncludeVariables
                => _includeVariables != null && _includeVariables.Count > 0;

            public List<TemplateVariable> IncludeVariables
                => _includeVariables ?? (_includeVariables = new List<TemplateVariable>());

            //public Stack<TemplateVariable> IncludeVariables
            //    => _includeVariables ?? (_includeVariables = new Stack<TemplateVariable>());

            public readonly Stack<FieldGroupBuilder> FieldGroupDefinitions => _fieldGroupDefinitionStack;
        }
    }
}
