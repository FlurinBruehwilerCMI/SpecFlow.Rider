using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using PCRE;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.StepsDefinitions
{
    public class SpecflowStepsDefinitionMergeData
    {
        public readonly OneToSetMap<IPsiSourceFile, SpecflowStepInfo> StepsDefinitionsPerFiles = new OneToSetMap<IPsiSourceFile, SpecflowStepInfo>();
        public readonly OneToSetMap<string, IPsiSourceFile> SpecflowBindingTypes = new OneToSetMap<string, IPsiSourceFile>();
        public readonly OneToSetMap<string, IPsiSourceFile> PotentialSpecflowBindingTypes = new OneToSetMap<string, IPsiSourceFile>();
    }

    public class SpecflowAssemblyStepsDefinitionMergeData
    {
        public readonly OneToSetMap<IPsiAssembly, SpecflowStepInfo> StepsDefinitionsPerFiles = new OneToSetMap<IPsiAssembly, SpecflowStepInfo>();
        public readonly Dictionary<string, IPsiAssembly> SpecflowBindingTypes = new Dictionary<string, IPsiAssembly>();
        public readonly Dictionary<string, IPsiAssembly> PotentialSpecflowBindingTypes = new Dictionary<string, IPsiAssembly>();
    }

    public class SpecflowStepInfo
    {
        public string ClassFullName { get; }
        public string MethodName { get; }
        public GherkinStepKind StepKind { get; }
        public string Pattern { get; }
        [CanBeNull]
        public Regex Regex { get; }
        [CanBeNull]
        public PcreRegex RegexForPartialMatch { get; }
        public List<Regex> RegexesPerCapture { get; }

        public SpecflowStepInfo(
            string classFullName,
            string methodName,
            GherkinStepKind stepKind,
            string pattern,
            [CanBeNull] Regex regex,
            [CanBeNull] PcreRegex regexForPartialMatch,
            List<Regex> regexesPerCapture
        )
        {
            ClassFullName = classFullName;
            MethodName = methodName;
            StepKind = stepKind;
            Pattern = pattern;
            Regex = regex;
            RegexForPartialMatch = regexForPartialMatch;
            RegexesPerCapture = regexesPerCapture;
        }
    }
}