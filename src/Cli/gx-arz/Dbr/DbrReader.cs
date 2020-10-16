using System;

using Glacie.Cli.Arz.Templates;
using Glacie.Data.Arz;
using Glacie.Data.Dbr;
using Glacie.Data.Metadata.V1;
using Glacie.Logging;
using Glacie.Metadata.V1;

using IO = System.IO;

namespace Glacie.Cli.Arz
{
    internal sealed class DbrReader : IDisposable
    {
        private readonly IRecordTypeProvider _recordTypeProvider;
        private readonly IRecordTypeProvider? _fallbackRecordTypeProvider;
        private readonly Logger _log;

        // TODO: Create database which points to source database's string table to void reencoding when adopt/import record
        private readonly ArzDatabase _tempDatabase = ArzDatabase.Create();

        public DbrReader(IRecordTypeProvider recordTypeProvider,
            IRecordTypeProvider? fallbackRecordTypeProvider,
            Logger? logger)
        {
            _recordTypeProvider = recordTypeProvider;
            _fallbackRecordTypeProvider = fallbackRecordTypeProvider;
            _log = logger ?? Logger.Null;
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

            Path1 templateName;
            RecordType? recordType = null;
            RecordType? fallbackRecordType = null;
            // First field is templateName - proceed.
            if (fieldReader.NameEqualsTo(WellKnownFieldNames.TemplateName))
            {
                var valueCount = fieldReader.ValueCount;
                if (valueCount != 1)
                {
                    throw DbrError("GX0105: DBR field templateName has invalid value (multiple values, but single value expected).");
                }

                templateName = Path1.From(fieldReader.GetStringValue(0));
                record.Set(WellKnownFieldNames.TemplateName, templateName.Value);

                if (!_recordTypeProvider.TryGetByTemplateName(templateName, out recordType))
                {
                    if (_fallbackRecordTypeProvider != null)
                    {
                        _fallbackRecordTypeProvider.TryGetByTemplateName(templateName, out fallbackRecordType);
                    }
                }

                if (recordType == null && fallbackRecordType == null)
                {
                    throw DbrError("GX0105: Unable to find record type: \"{0}\".", templateName);
                }
            }
            else
            {
                // No templateName, it is warning or error?
                throw DbrError("GX0104: DBR record has no templateName field.");
            }

            while (fieldReader.Read())
            {
                var name = fieldReader.Name;

                FieldType? fieldType = null;
                if (recordType != null)
                {
                    recordType.TryGetField(name, out fieldType);
                }
                if (fieldType == null)
                {
                    if (_fallbackRecordTypeProvider != null)
                    {
                        if (fallbackRecordType == null)
                        {
                            fallbackRecordType = _fallbackRecordTypeProvider
                                .GetByTemplateName(templateName);
                        }

                        fieldType = fallbackRecordType.GetField(name);
                    }
                }
                if (fieldType == null) throw DbrError("Can't resolve field type: \"{0}\" (\"{1}\").", name, recordType?.Name);

                var valueCount = fieldReader.ValueCount;
                if (valueCount == 1)
                {
                    switch (fieldType.ValueType)
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

                    switch (fieldType.ValueType)
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

            // TODO: RecordType should provide Class directly.
            if (record.Class == null)
            {
                var actualRecordType = recordType ?? fallbackRecordType;
                Check.That(actualRecordType != null);

                if (actualRecordType.TryGetField(WellKnownFieldNames.Class, out var fieldDef))
                {
                    record.Class = fieldDef.VarDefaultValue ?? "";
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
            return Error.InvalidOperation(message);
        }

        private static InvalidOperationException DbrError(string format, params object?[] args)
        {
            return Error.InvalidOperation(format, args);
        }
    }
}
