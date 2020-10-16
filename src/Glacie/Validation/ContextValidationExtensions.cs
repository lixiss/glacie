using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Glacie.Abstractions;
using Glacie.Data.Arz;
using Glacie.Metadata;
using Glacie.Resources;

namespace Glacie.Validation
{
    // TODO: Add ValidationMode/or ValidationOptions: ChangesOnly or All, RecordsForOutput or AllRecordsForOutput.
    // TODO: Want to have ability pre-validate everything.
    // TODO: Or validate only records included in output.
    // TODO: Still good idea to validate changed records since last call.

    // TODO: Need a way to select changed records from context.
    // Context may act as tracker factory to pull records from tracker.

    public static class ContextValidationExtensions
    {
        /// <summary>
        /// Validate context.
        /// By default only changed records since previous call will be validated.
        /// </summary>
        public static ValidationResult Validate(this Context context, bool resolveReferences, bool all = false, IIncrementalProgress<int>? progress = null)
        {
            // _recordVersionTrackerForValidation ??= new RecordVersionTracker(Database);
            var validationConfiguration = CreateValidationConfiguration(context, resolveReferences);
            var validationContext = new ValidationContext(context, validationConfiguration);

            //IEnumerable<ArzRecord> recordsToValidate;
            //if (all)
            //{
            //    recordsToValidate = _recordVersionTrackerForValidation.SelectChangedUnderlyingRecords();
            //}
            //else
            //{
            //    recordsToValidate = _recordVersionTrackerForValidation.SelectAllUnderlyingRecords();
            //}

            IEnumerable<Record> recordsToValidate = context.Database.SelectAll();

            if (progress == null)
            {
                foreach (var record in recordsToValidate)
                {
                    validationContext.Validate(record);
                }
            }
            else
            {
                var materializedRecords = recordsToValidate.ToList();
                //progress.AddMaximumValue(materializedRecords.Select(x => x.Count).Sum());
                progress.AddMaximumValue(materializedRecords.Count);
                foreach (var record in materializedRecords)
                {
                    validationContext.Validate(record);

                    progress.AddValue(1);
                    // progress.AddValue(record.Count);
                }
            }

            var result = validationContext.GetResult();
            context.GetDiagnosticBag().AddRange(result.Bag);
            return result;
        }

        private static ValidationConfiguration CreateValidationConfiguration(Context context, bool resolveReferences)
        {
            // TODO: Validation service can be constructed with DI...
            return new ValidationConfiguration
            {
                Logger = context.Log, // TODO: should be validation context's logger
                DiagnosticReporter = context, // TODO: should be resolved?
                MetadataResolver = context.Services.Resolve<MetadataResolver>(),
                RecordResolver = context.Database.AsResolver(), // TODO: add resolver directly into context
                ResourceResolver = context.ResourceResolver,

                ResolveReferences = resolveReferences,
            };
        }
    }
}
