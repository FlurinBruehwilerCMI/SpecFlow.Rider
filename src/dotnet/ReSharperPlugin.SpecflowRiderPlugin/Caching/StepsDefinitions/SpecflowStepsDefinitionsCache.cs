using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using ReSharperPlugin.SpecflowRiderPlugin.Caching.StepsDefinitions.AssemblyStepDefinitions;
using ReSharperPlugin.SpecflowRiderPlugin.Helpers;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;
using ReSharperPlugin.SpecflowRiderPlugin.Utils.Steps;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.StepsDefinitions
{
    [PsiComponent]
    public class SpecflowStepsDefinitionsCache : SimpleICache<SpecflowStepsDefinitionsCacheEntries>
    {
        private const int VersionInt = 12;
        public override string Version => VersionInt.ToString();

        // FIXME: per step kind
        public OneToSetMap<IPsiSourceFile, SpecflowStepInfo> AllStepsPerFiles => _mergeData.StepsDefinitionsPerFiles;
        private readonly SpecflowStepsDefinitionMergeData _mergeData = new SpecflowStepsDefinitionMergeData();
        private readonly ISpecflowStepInfoFactory _specflowStepInfoFactory;
        private readonly IUnderscoresMethodNameStepDefinitionUtil _underscoresMethodNameStepDefinitionUtil;

        public SpecflowStepsDefinitionsCache(Lifetime lifetime, IShellLocks locks, IPersistentIndexManager persistentIndexManager, ISpecflowStepInfoFactory specflowStepInfoFactory, IUnderscoresMethodNameStepDefinitionUtil underscoresMethodNameStepDefinitionUtil)
            : base(lifetime, locks, persistentIndexManager, new SpecflowStepDefinitionsEntriesMarshaller(), VersionInt)
        {
            _specflowStepInfoFactory = specflowStepInfoFactory;
            _underscoresMethodNameStepDefinitionUtil = underscoresMethodNameStepDefinitionUtil;
        }

        public IEnumerable<SpecflowStepInfo> GetStepAccessibleForModule(IPsiModule module, GherkinStepKind stepKind)
        {
            foreach (var (stepSourceFile, stepDefinitions) in _mergeData.StepsDefinitionsPerFiles)
            {
                if (!ReferenceEquals(module, stepSourceFile.PsiModule) && !module.References(stepSourceFile.PsiModule))
                    continue;
                foreach (var stepDefinitionInfo in stepDefinitions.Where(s => s.StepKind == stepKind))
                    yield return stepDefinitionInfo;
            }
        }

        public IEnumerable<AvailableBindingClass> GetBindingTypes(IPsiModule module)
        {
            foreach (var (fullClassName, sourceFile) in _mergeData.SpecflowBindingTypes.SelectMany(e => e.Value.Select(v => (fullClassName: e.Key, sourceFile: v))))
            {
                if (!module.Equals(sourceFile.PsiModule) && !module.References(sourceFile.PsiModule))
                    continue;
                yield return new AvailableBindingClass(sourceFile, fullClassName);
            }

            // Since we cannot know when cache are built if a partial class in a given file has an attribute or not.
            // This is due to the fact that `DeclaredElement` are resolved using cache and we cannot depends ond cache when building a cache.
            // The check need to be done at this time.
            foreach (var (fullClassName, sourceFile) in _mergeData.PotentialSpecflowBindingTypes.SelectMany(e => e.Value.Select(v => (fullClassName: e.Key, sourceFile: v))))
            {
                if (!module.Equals(sourceFile.PsiModule) && !module.References(sourceFile.PsiModule))
                    continue;
                var type = CSharpTypeFactory.CreateType(fullClassName, sourceFile.PsiModule);
                if (!(type is DeclaredTypeFromCLRName declaredTypeFromClrName))
                    continue;
                var resolveResult = declaredTypeFromClrName.Resolve();
                if (!resolveResult.IsValid())
                    continue;
                if (!(resolveResult.DeclaredElement is IClass @class))
                    continue;
                if (@class.GetAttributeInstances(AttributesSource.Self).All(x => x.GetAttributeType().GetClrName().FullName != "TechTalk.SpecFlow.BindingAttribute"))
                    continue;
                yield return new AvailableBindingClass(sourceFile, fullClassName);
            }
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!sourceFile.IsValid())
                return null;
            var file = sourceFile.GetPrimaryPsiFile().NotNull();
            if (!file.Language.Is<CSharpLanguage>())
                return null;

            var stepDefinitions = new SpecflowStepsDefinitionsCacheEntries();
            foreach (var type in GetTypeDeclarations(file))
            {
                if (!(type is IClassDeclaration classDeclaration))
                    continue;
                var hasSpecflowBindingAttribute = HasSpecflowBindingAttribute(classDeclaration);
                if (!hasSpecflowBindingAttribute && !classDeclaration.IsPartial)
                    continue;
                if (IsSpecflowFeatureFile(classDeclaration))
                    continue;
                stepDefinitions.Add(BuildBindingClassCacheEntry(classDeclaration, hasSpecflowBindingAttribute));
            }

            if (stepDefinitions.Count == 0)
                return null;

            return stepDefinitions;
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.LanguageType.Is<CSharpProjectFileType>();
        }

        private bool IsSpecflowFeatureFile(IClassDeclaration classDeclaration)
        {

            if (classDeclaration.Attributes.Count == 0)
                return false;

            // Optimization: do not resolve all attribute. We are looking for `System.CodeDom.Compiler.GeneratedCodeAttribute` only
            var potentialBindingAttributes = classDeclaration.Attributes.Where(x => x.Arguments.Count == 2).ToList();
            if (potentialBindingAttributes.Count == 0)
                return false;

            var specflowGeneratedAttribute = false;
            foreach (var potentialBindingAttribute in potentialBindingAttributes.Select(x => x.GetAttributeInstance()))
            {
                if (potentialBindingAttribute.GetClrName().FullName == "System.CodeDom.Compiler.GeneratedCodeAttribute"
                    && potentialBindingAttribute.PositionParameter(0).ConstantValue.Value as string == "TechTalk.SpecFlow")
                    specflowGeneratedAttribute = true;
            }
            return specflowGeneratedAttribute;
        }

        private static bool HasSpecflowBindingAttribute(IClassDeclaration classDeclaration)
        {
            if (classDeclaration.Attributes.Count == 0)
                return false;

            // Optimization: do not resolve all attribute. We are looking for `TechTalk.SpecFlow.BindingAttribute` only
            var potentialBindingAttributes = classDeclaration.Attributes.Where(x => x.Arguments.Count == 0).ToList();
            if (potentialBindingAttributes.Count == 0)
                return false;

            var bindingAttributeFound = false;
            foreach (var potentialBindingAttribute in potentialBindingAttributes.Select(x => x.GetAttributeInstance()))
            {
                var fullName = potentialBindingAttribute.GetClrName().FullName;
                if (fullName == "TechTalk.SpecFlow.BindingAttribute")
                {
                    bindingAttributeFound = true;
                    break;
                }
                // FIXME: Should we check for `using TechTalk.SpecFlow` ?
                if (fullName.IsEmpty() && potentialBindingAttribute.GetAttributeShortName() == "Binding")
                {
                    bindingAttributeFound = true;
                    break;
                }
            }
            return bindingAttributeFound;
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);
            PopulateLocalCache();
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            AddToLocalCache(sourceFile, builtPart as SpecflowStepsDefinitionsCacheEntries);
            base.Merge(sourceFile, builtPart);
        }

        private void PopulateLocalCache()
        {
            foreach (var (psiSourceFile, cacheItem) in Map)
                AddToLocalCache(psiSourceFile, cacheItem);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] SpecflowStepsDefinitionsCacheEntries cacheItems)
        {
            if (cacheItems == null)
                return;

            foreach (var classEntry in cacheItems)
            {
                if (classEntry.HasSpecflowBindingAttribute)
                    _mergeData.SpecflowBindingTypes.Add(classEntry.ClassName, sourceFile);
                else
                    _mergeData.PotentialSpecflowBindingTypes.Add(classEntry.ClassName, sourceFile);
                foreach (var method in classEntry.Methods)
                foreach (var step in method.Steps)
                    _mergeData.StepsDefinitionsPerFiles.Add(sourceFile, _specflowStepInfoFactory.Create(classEntry.ClassName, method.MethodName, step.StepKind, step.Pattern));
            }
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            var fileSteps = _mergeData.StepsDefinitionsPerFiles[sourceFile];
            foreach (var classNameInFile in fileSteps.Select(x => x.ClassFullName).Distinct())
            {
                _mergeData.SpecflowBindingTypes.Remove(classNameInFile, sourceFile);
                _mergeData.PotentialSpecflowBindingTypes.Remove(classNameInFile, sourceFile);
            }

            _mergeData.StepsDefinitionsPerFiles.RemoveKey(sourceFile);
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private IEnumerable<ITypeDeclaration> GetTypeDeclarations(ITreeNode node)
        {
            if (node is INamespaceDeclarationHolder namespaceDeclarationHolder)
                foreach (var typeDeclaration in namespaceDeclarationHolder.NamespaceDeclarations.SelectMany(GetTypeDeclarations))
                    yield return typeDeclaration;

            if (node is ITypeDeclarationHolder typeDeclarationHolder)
                foreach (var typeDeclaration in typeDeclarationHolder.TypeDeclarations)
                    yield return typeDeclaration;
        }

        private SpecflowStepDefinitionCacheClassEntry BuildBindingClassCacheEntry(IClassDeclaration classDeclaration, bool hasSpecflowBindingAttribute)
        {
            var classCacheEntry = new SpecflowStepDefinitionCacheClassEntry(classDeclaration.CLRName, hasSpecflowBindingAttribute);

            foreach (var member in classDeclaration.MemberDeclarations)
            {
                if (!(member is IMethodDeclaration methodDeclaration))
                    continue;

                var methodCacheEntry = classCacheEntry.AddMethod(methodDeclaration.DeclaredName);

                foreach (var attribute in methodDeclaration.Attributes)
                {
                    if (attribute.Arguments.Count == 1)
                        AddToCacheEntryBasedOnAttributeRegex(attribute, methodCacheEntry);
                    if (attribute.Arguments.Count == 0)
                        AddToCacheEntryBasedOnMethodName(methodDeclaration, attribute, methodCacheEntry);
                }
            }
            return classCacheEntry;
        }

        private static void AddToCacheEntryBasedOnAttributeRegex(IAttribute attribute, SpecflowStepDefinitionCacheMethodEntry methodCacheEntry)
        {

            var attributeArgument = attribute.Arguments[0];

            var regex = attributeArgument.Value?.ConstantValue.Value as string;
            if (regex == null)
                return;

            // FIXME: If at some point this is not enought we could check that attribute.Name.QualifiedName contains TechTalk.SpecFlow or that
            // TechTalk.SpecFlow is in the `using` list somewhere in a parent

            if (SpecflowAttributeHelper.IsAttributeForKindUsingShortName(GherkinStepKind.Given, attribute.Name.ShortName))
                methodCacheEntry.AddStep(GherkinStepKind.Given, regex);
            if (SpecflowAttributeHelper.IsAttributeForKindUsingShortName(GherkinStepKind.When, attribute.Name.ShortName))
                methodCacheEntry.AddStep(GherkinStepKind.When, regex);
            if (SpecflowAttributeHelper.IsAttributeForKindUsingShortName(GherkinStepKind.Then, attribute.Name.ShortName))
                methodCacheEntry.AddStep(GherkinStepKind.Then, regex);
        }

        private void AddToCacheEntryBasedOnMethodName(IMethodDeclaration memberDeclaration, IAttribute attribute, SpecflowStepDefinitionCacheMethodEntry methodCacheEntry)
        {
            var regex = _underscoresMethodNameStepDefinitionUtil.BuildRegexFromMethodName(memberDeclaration);
            if (SpecflowAttributeHelper.IsAttributeForKindUsingShortName(GherkinStepKind.Given, attribute.Name.ShortName))
                methodCacheEntry.AddStep(GherkinStepKind.Given, regex);
            if (SpecflowAttributeHelper.IsAttributeForKindUsingShortName(GherkinStepKind.When, attribute.Name.ShortName))
                methodCacheEntry.AddStep(GherkinStepKind.When, regex);
            if (SpecflowAttributeHelper.IsAttributeForKindUsingShortName(GherkinStepKind.Then, attribute.Name.ShortName))
                methodCacheEntry.AddStep(GherkinStepKind.Then, regex);
        }

        public class AvailableBindingClass
        {
            public IPsiSourceFile SourceFile { get; }
            public string ClassClrName { get; }

            public AvailableBindingClass(IPsiSourceFile sourceFile, string classClrName)
            {
                SourceFile = sourceFile;
                ClassClrName = classClrName;
            }
        }
    }
}