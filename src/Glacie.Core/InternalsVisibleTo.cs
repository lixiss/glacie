using System.Runtime.CompilerServices;

// TODO: (VeryLow) Avoid of using InternalsVisibleTo, if not needed.
// Now it is used by IDatabaseApi/ etc stuff, which logically can be backed by
// Glacie.Private.Core, but this interfaces might depend on other types, which
// doesn't present in Glacie.Private.Core.

[assembly: InternalsVisibleTo("Glacie")]
[assembly: InternalsVisibleTo("Glacie.Data.Arz")]
