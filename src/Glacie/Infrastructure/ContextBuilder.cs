using System;
using System.Collections.Generic;
using Glacie.Abstractions;

namespace Glacie.Infrastructure
{
    internal sealed class ContextBuilder : IContextBuilder
    {
        private List<SourceBuilder> _sourceBuilders = new List<SourceBuilder>();
        private TargetBuilder? _targetBuilder;

        public Context Build()
        {
            Validate();

            AssignSourceIdentifiers();

            var sources = new List<Source>();
            foreach (var sourceBuilder in _sourceBuilders)
            {
                var source = sourceBuilder.Build();
                sources.Add(source);
            }

            var target = _targetBuilder!.Build();

            // TODO: (Gx) runtime validations for sources & target
            // E.g. they should not specify same location / etc something

            Context? context = null;
            try
            {
                context = new Context(sources.ToArray(), target);

                // configure logging
                context.Open();

                var result = context;
                context = null;
                return result;
            }
            finally
            {
                context?.Dispose();
            }
        }

        private void Validate()
        {
            if (_targetBuilder == null) throw GxError.ContextTargetIsNotSpecified();
        }

        private void AssignSourceIdentifiers()
        {
            for (var i = 0; i < _sourceBuilders.Count; i++)
            {
                _sourceBuilders[i].SetIdentifier(_sourceBuilders.Count - i);
            }
        }

        private SourceBuilder CreateAndRegisterSourceBuilder()
        {
            var sourceBuilder = new SourceBuilder();
            _sourceBuilders.Add(sourceBuilder);
            return sourceBuilder;
        }

        private TargetBuilder CreateAndRegisterTargetBuilder()
        {
            if (_targetBuilder != null) throw GxError.ContextMultipleTargets();
            return _targetBuilder = new TargetBuilder();
        }

        #region API

        void IContextBuilder.Source(Action<ISourceBuilder> configureSource)
        {
            Check.Argument.NotNull(configureSource, nameof(configureSource));

            var sourceBuilder = CreateAndRegisterSourceBuilder();
            configureSource(sourceBuilder);
            sourceBuilder.Validate();
        }

        void IContextBuilder.Target(Action<ITargetBuilder> configureTarget)
        {
            Check.Argument.NotNull(configureTarget, nameof(configureTarget));

            var targetBuilder = CreateAndRegisterTargetBuilder();
            configureTarget(targetBuilder);
            targetBuilder.Validate();
        }

        #endregion
    }
}
