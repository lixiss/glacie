using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Glacie.Cli.Arz.Dbr;
using Glacie.CommandLine.IO;
using Glacie.Data.Arz;
using Glacie.Data.Compression;
using Glacie.Metadata;
using Glacie.Targeting;

using IO = System.IO;

namespace Glacie.Cli.Arz.Commands
{
    internal sealed class BuildCommand : ProcessInputFilesCommand
    {
        public enum Mode
        {
            Build = 0,
            Update,
            Replace,
            Add,
            RemoveMissing,
        }

        public string? MetadataPath { get; }
        public string? MetadataFallbackPath { get; }

        private bool _detailed = false;

        private Mode _mode;
        private int _totalCount;
        private int _addedCount;
        private int _updatedCount;
        private int _upToDateCount;
        private int _skippedCount;
        private int _removedCount;
        private readonly HashSet<string> _recordNamesToKeep = new HashSet<string>(StringComparer.Ordinal);

        private DbrReader? _dbrReader;

        private MetadataProvider? _metadataProvider;
        private MetadataProvider? _metadataFallbackProvider;

        private ArzDatabase? _ephemeralMetadataDatabase;
        private bool _mustDisposeEphemeralMetadataDatabase;

        public BuildCommand(
            string database,
            List<string> input,
            string relativeTo,
            ArzFileFormat format,
            CompressionLevel compressionLevel,
            bool checksum,
            bool safeWrite,
            bool preserveCase,
            string? metadata = null,
            string? metadataFallback = null,
            string? output = null)
            : base(database: database,
                 input: input,
                 relativeTo: relativeTo,
                 format: format,
                 compressionLevel: compressionLevel,
                 checksum: checksum,
                 safeWrite: safeWrite,
                 preserveCase: preserveCase,
                 output: output)
        {
            MetadataPath = metadata;
            MetadataFallbackPath = metadataFallback;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbrReader?.Dispose();
                _dbrReader = null;

                _metadataProvider?.Dispose();
                _metadataProvider = null;

                _metadataFallbackProvider?.Dispose();
                _metadataFallbackProvider = null;

                if (_mustDisposeEphemeralMetadataDatabase)
                {
                    _ephemeralMetadataDatabase?.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public int Run(Mode mode)
        {
            _mode = mode;

            return RunProcessInputFiles();
        }

        protected override void OnInputDatabaseOpened(ArzDatabase database)
        {
            // TODO: determine correct targeting pack, generally this can be
            // done from --format option or input database format
            // var target = new TitanQuestAnniversaryEditionTarget();
            // var target = new GrimDawnTarget();
            var target = new UnifiedTarget();

            using var progress = StartProgress("Reading Metadata...");
            var readerOptions = CreateReaderOptions(ArzReadingMode.Full);
            readerOptions.CloseUnderlyingStream = true;
            var metadataProviderFactoryOptions = new MetadataProviderFactoryOptions
            {
                ArzReaderOptions = readerOptions,
                TemplateNameMapper = target.GetTemplateNameMapper(),
                TemplateProcessor = target.GetTemplateProcessor(),
                Logger = Log,
            };

            if (MetadataPath == null)
            {
                _metadataProvider = new EphemeralMetadataProvider(database,
                    Log,
                    disposeDatabase: false);
            }
            else
            {
                _metadataProvider = MetadataProviderFactory.Create(MetadataPath,
                    metadataProviderFactoryOptions);
            }

            if (MetadataFallbackPath != null)
            {
                _metadataFallbackProvider = MetadataProviderFactory.Create(MetadataFallbackPath,
                    metadataProviderFactoryOptions);
            }
        }

        protected override string GetProcessInputFilesTitle() => "Processing...";

        protected override void ProcessInputFile(ArzDatabase database, InputFileInfo fileInfo, IIncrementalProgress<long>? progress)
        {
            _totalCount++;

            if (database.TryGet(fileInfo.RecordName, out var record))
            {
                if (_mode == Mode.Build || _mode == Mode.RemoveMissing)
                {
                    _recordNamesToKeep.Add(record.Name);
                }

                switch (_mode)
                {
                    case Mode.Build:
                    case Mode.Update:
                        {
                            // TODO: Want to have option to exact timestamp match.
                            var hasRecordLastWriteTime = record.TryGetLastWriteTime(out var recordLastWriteTime);
                            if (!hasRecordLastWriteTime
                                || (hasRecordLastWriteTime && recordLastWriteTime < fileInfo.LastWriteTime))
                            {
                                UpdateRecord(database, record, fileInfo);
                            }
                            else
                            {
                                _upToDateCount++;
                            }
                        }
                        break;

                    case Mode.Add:
                        _skippedCount++;
                        break;

                    case Mode.Replace:
                        UpdateRecord(database, record, fileInfo);
                        break;

                    case Mode.RemoveMissing:
                        // no-op
                        break;

                    default: throw Error.Unreachable();
                }
            }
            else
            {
                switch (_mode)
                {
                    case Mode.Build:
                    case Mode.Update:
                    case Mode.Add:
                    case Mode.Replace:
                        AddRecord(database, fileInfo);
                        _recordNamesToKeep.Add(fileInfo.RecordName);
                        break;

                    case Mode.RemoveMissing:
                        // no-op
                        break;

                    default: throw Error.Unreachable();
                }
            }

            progress?.AddValue(fileInfo.Length);
        }

        protected override void PostProcess(ArzDatabase database, IIncrementalProgress<long>? progress)
        {
            if (_mode == Mode.Build || _mode == Mode.RemoveMissing)
            {
                var recordsToRemove = new List<ArzRecord>();

                foreach (var record in database.GetAll())
                {
                    if (!_recordNamesToKeep.Contains(record.Name))
                    {
                        recordsToRemove.Add(record);
                        if (_detailed) Console.Out.WriteLine("Remove: {0}", record.Name);
                    }
                }

                foreach (var record in recordsToRemove)
                {
                    database.Remove(record);
                    _removedCount++;
                }
            }

            WriteSummary();
        }

        private void WriteSummary()
        {
            var summaryBuilder = new StringBuilder();
            if (true)
            {
                if (summaryBuilder.Length > 0) summaryBuilder.Append(", ");
                summaryBuilder.AppendFormat("{0} total", _totalCount);
            }
            if (_addedCount > 0 || _mode == Mode.Add)
            {
                if (summaryBuilder.Length > 0) summaryBuilder.Append(", ");
                summaryBuilder.AppendFormat("{0} added", _addedCount);
            }
            if (_updatedCount > 0 || _mode == Mode.Update || _mode == Mode.Replace)
            {
                if (summaryBuilder.Length > 0) summaryBuilder.Append(", ");
                summaryBuilder.AppendFormat("{0} updated", _updatedCount);
            }
            if (_upToDateCount > 0)
            {
                if (summaryBuilder.Length > 0) summaryBuilder.Append(", ");
                summaryBuilder.AppendFormat("{0} up-to-date", _upToDateCount);
            }
            if (_skippedCount > 0)
            {
                if (summaryBuilder.Length > 0) summaryBuilder.Append(", ");
                summaryBuilder.AppendFormat("{0} skipped", _skippedCount);
            }
            if (_removedCount > 0 || _mode == Mode.RemoveMissing)
            {
                if (summaryBuilder.Length > 0) summaryBuilder.Append(", ");
                summaryBuilder.AppendFormat("{0} removed", _removedCount);
            }

            Console.Out.WriteLine("Build: {0}", summaryBuilder);
        }

        private void AddRecord(ArzDatabase database, InputFileInfo fileInfo)
        {
            if (_detailed) Console.Out.WriteLine("Add: {0}", fileInfo.RecordName);

            using var textReader = OpenTextReader(in fileInfo);
            UpdateRecordFromDbrFile(database, in fileInfo, fileInfo.RecordName);

            _addedCount++;
        }

        private void UpdateRecord(ArzDatabase database, ArzRecord record, InputFileInfo fileInfo)
        {
            if (_detailed) Console.Out.WriteLine("Update: {0}", record.Name);

            UpdateRecordFromDbrFile(database, in fileInfo, record.Name);

            _updatedCount++;
        }

        private void UpdateRecordFromDbrFile(ArzDatabase database, in InputFileInfo fileInfo, string recordName)
        {
            using var textReader = OpenTextReader(in fileInfo);
            ArzRecord tempRecord;
            try
            {
                tempRecord = GetDbrReader().Read(textReader, recordName);

                // DbrReader now provide record class.
                Check.That(tempRecord.Class != null);
            }
            catch (Exception e)
            {
                throw new CliErrorException("Unable to read dbr file: " + fileInfo.FileName, e);
            }

            tempRecord.LastWriteTime = fileInfo.LastWriteTime;
            database.Import(tempRecord); // TODO: adopt is better
        }

        private IO.TextReader OpenTextReader(in InputFileInfo fileInfo)
        {
            var inputStream = new IO.FileStream(fileInfo.FileName,
                IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, 4096,
                IO.FileOptions.SequentialScan);
            return new IO.StreamReader(inputStream, DbrUtility.Encoding);
        }

        private DbrReader GetDbrReader()
        {
            if (_dbrReader != null) return _dbrReader;
            Check.That(_metadataProvider != null);
            return (_dbrReader = new DbrReader(_metadataProvider, _metadataFallbackProvider, Log));
        }
    }
}
