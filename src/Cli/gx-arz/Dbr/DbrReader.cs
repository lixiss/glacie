using System;

using Glacie.Cli.Arz.Templates;
using Glacie.Data.Arz;
using Glacie.Data.Dbr;
using Glacie.Logging;

using IO = System.IO;

namespace Glacie.Cli.Arz
{
    internal sealed class DbrReader : IDisposable
    {
        private readonly IRecordDefinitionProvider _recordDefinitionProvider;
        private readonly Logger _log;

        // TODO: Create database which points to source database's string table to void reencoding when adopt/import record
        private readonly ArzDatabase _tempDatabase = ArzDatabase.Create();

        public DbrReader(IRecordDefinitionProvider recordDefinitionProvider, Logger log)
        {
            _recordDefinitionProvider = recordDefinitionProvider;
            _log = log;
        }

        public void Dispose()
        {
            _tempDatabase?.Dispose();
        }

        public ArzRecord Read(IO.TextReader reader, string recordName)
        {
            var record = _tempDatabase.Add(recordName);

            Read(reader, record);
            _tempDatabase.Remove(record);

            return record;
        }

        private void Read(IO.TextReader reader, ArzRecord record)
        {
            var text = reader.ReadToEnd();

            using var fieldReader = new DbrFieldReader(text);

            if (!fieldReader.Read())
            {
                throw DbrError("GX0103: DBR record without fields.");
            }

            RecordDefinition recordDefinition;
            // First field is templateName - proceed.
            if (fieldReader.NameEqualsTo(WellKnownFieldNames.TemplateName))
            {
                var valueCount = fieldReader.ValueCount;
                if (valueCount != 1)
                {
                    throw DbrError("GX0105: DBR field templateName has invalid value (multiple values, but single value expected).");
                }

                var templateNameValue = fieldReader.GetStringValue(0);
                record.Set(WellKnownFieldNames.TemplateName, templateNameValue);

                recordDefinition = _recordDefinitionProvider.GetRecordDefinition(templateNameValue);
            }
            else
            {
                // No templateName, it is warning or error?
                throw DbrError("GX0104: DBR record has no templateName field.");
            }

            while (fieldReader.Read())
            {
                var name = fieldReader.Name;

                var fieldDefinition = recordDefinition.GetFieldDefinition(name);

                var valueCount = fieldReader.ValueCount;
                if (valueCount == 1)
                {
                    switch (fieldDefinition.ValueType)
                    {
                        case ArzValueType.Integer:
                            {
                                var value = fieldReader.GetInt32Value(0);
                                record.Set(name, value); // TODO: use Add
                            }
                            break;

                        case ArzValueType.Real:
                            {
                                var value = fieldReader.GetFloat32Value(0);
                                record.Set(name, value); // TODO: use Add
                            }
                            break;

                        case ArzValueType.String:
                            {
                                var value = fieldReader.GetStringValue(0);
                                record.Set(name, value); // TODO: use Add

                                if (name == WellKnownFieldNames.Class)
                                {
                                    record.Class = value;
                                }
                            }
                            break;

                        case ArzValueType.Boolean:
                            {
                                var value = fieldReader.GetBooleanValue(0);
                                record.Set(name, value); // TODO: use Add
                            }
                            break;

                        default: throw Error.Unreachable();
                    }
                }
                else
                {
                    // TODO: allow fieldReader parse arrays, and return
                    // some buffered segments, but this will need support in ArzRecord
                    // to set values from spans or array segments.

                    switch (fieldDefinition.ValueType)
                    {
                        case ArzValueType.Integer:
                            {
                                var values = new int[valueCount];
                                for (var i = 0; i < valueCount; i++)
                                {
                                    values[i] = fieldReader.GetInt32Value(i);
                                }
                                record.Set(name, values); // TODO: use Add
                            }
                            break;

                        case ArzValueType.Real:
                            {
                                var values = new float[valueCount];
                                for (var i = 0; i < valueCount; i++)
                                {
                                    values[i] = fieldReader.GetFloat32Value(i);
                                }
                                record.Set(name, values); // TODO: use Add
                            }
                            break;

                        case ArzValueType.String:
                            {
                                var values = new string[valueCount];
                                for (var i = 0; i < valueCount; i++)
                                {
                                    values[i] = fieldReader.GetStringValue(i);
                                }
                                record.Set(name, values); // TODO: use Add
                            }
                            break;

                        case ArzValueType.Boolean:
                            {
                                var values = new bool[valueCount];
                                for (var i = 0; i < valueCount; i++)
                                {
                                    values[i] = fieldReader.GetBooleanValue(i);
                                }
                                record.Set(name, values); // TODO: use Add
                            }
                            break;

                        default: throw Error.Unreachable();
                    }
                }
            }

            if (record.Class == null)
            {
                if (recordDefinition.TryGetFieldDefinition(WellKnownFieldNames.Class, out var fieldDef))
                {
                    if (!fieldDef.HasDefaultValue)
                    {
                        // _log.Warning("GX0301: Definition for field \"{0}\" doesn't provide default value. Empty value will be used.", WellKnownFieldNames.Class);
                    }

                    record.Class = fieldDef.DefaultValue ?? "";
                }
                else
                {
                    // Class doesn't defined by template, so we also can not specify it. So just use empty class.
                    // _log.Error("GX0300: No definition for field \"{0}\".", WellKnownFieldNames.Class);
                    record.Class = "";
                }
            }
        }

        private static InvalidOperationException DbrError(string message)
        {
            return new InvalidOperationException(message);
        }
    }
}
