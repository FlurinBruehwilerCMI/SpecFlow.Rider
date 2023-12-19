using System.Collections.Generic;
using JetBrains.Annotations;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.StepsDefinitions
{
    /// <summary>
    /// A class with a set of StepDefinition Methods in it
    /// </summary>
    public class SpecflowStepDefinitionCacheClassEntry
    {
        public string ClassName { get; }
        public bool HasSpecflowBindingAttribute { get; }
        [CanBeNull] public IReadOnlyList<SpecflowStepScope> Scopes { get; }
        //List of StepDefinition Methods
        public IList<SpecflowStepDefinitionCacheMethodEntry> Methods { get; } = new List<SpecflowStepDefinitionCacheMethodEntry>();

        public SpecflowStepDefinitionCacheClassEntry(string className, bool hasSpecflowBindingAttribute, [CanBeNull] IReadOnlyList<SpecflowStepScope> scopes = null)
        {
            ClassName = className;
            HasSpecflowBindingAttribute = hasSpecflowBindingAttribute;
            Scopes = scopes;
        }

        public SpecflowStepDefinitionCacheMethodEntry AddMethod(
            string methodName,
            string[] methodParameterTypes,
            string[] methodParameterNames,
            [CanBeNull] IReadOnlyList<SpecflowStepScope> methodScopes
        )
        {
            var methodCacheEntry = new SpecflowStepDefinitionCacheMethodEntry(methodName, methodParameterTypes, methodParameterNames, methodScopes);
            Methods.Add(methodCacheEntry);
            return methodCacheEntry;
        }
    }

    public class SpecflowStepDefinitionCacheMethodEntry
    {
        public string MethodName { get; }
        //multiple steps can bind to one method (multiple attributes)
        public IList<SpecflowStepDefinitionCacheStepEntry> Steps { get; } = new List<SpecflowStepDefinitionCacheStepEntry>();
        [CanBeNull] public IReadOnlyList<SpecflowStepScope> Scopes { get; }
        public string[] MethodParameterTypes { get; }
        public string[] MethodParameterNames { get; }

        public SpecflowStepDefinitionCacheMethodEntry(
            string methodName,
            string[] methodParameterTypes,
            string[] methodParameterNames,
            [CanBeNull] IReadOnlyList<SpecflowStepScope> scopes
        )
        {
            MethodName = methodName;
            MethodParameterTypes = methodParameterTypes;
            MethodParameterNames = methodParameterNames;
            Scopes = scopes;
        }

        public void AddStep(
            GherkinStepKind stepKind,
            string pattern
        )
        {
            var stepCacheEntry = new SpecflowStepDefinitionCacheStepEntry(stepKind, pattern);
            Steps.Add(stepCacheEntry);
        }
    }

    public class SpecflowStepDefinitionCacheStepEntry
    {
        public GherkinStepKind StepKind { get; }
        public string Pattern { get; }

        public SpecflowStepDefinitionCacheStepEntry(GherkinStepKind stepKind, string pattern)
        {
            Pattern = pattern;
            StepKind = stepKind;
        }
    }
}
