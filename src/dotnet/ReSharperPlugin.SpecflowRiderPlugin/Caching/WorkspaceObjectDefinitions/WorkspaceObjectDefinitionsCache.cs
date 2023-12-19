using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectDefinitions;

[PsiComponent]
public class WorkspaceObjectDefinitionsCache : SimpleICache<WorkspaceObjectDefinitionsCacheEntries>
{
    public OneToSetMap<IPsiSourceFile, WorkspaceObjectDefinitionsCacheEntry> WODPerFile => _mergeData.WODPerFile;

    private readonly Regex _regex = new(@"ein Objekt vom Typ (?<objTyp>\w*) namens (?<objName>\w*)");
    private readonly WorkspaceObjectDefinitionsMergeData _mergeData = new();

    public WorkspaceObjectDefinitionsCache(Lifetime lifetime, [NotNull] IShellLocks locks, [NotNull] IPersistentIndexManager persistentIndexManager,
                                           IUnsafeMarshaller<WorkspaceObjectDefinitionsCacheEntries> valueMarshaller, long? version = null) : base(lifetime, locks, persistentIndexManager,
        valueMarshaller, version)
    {
    }

    protected override bool IsApplicable(IPsiSourceFile sf)
    {
        return sf.LanguageType.Is<GherkinProjectFileType>();
    }

    public override object Build(IPsiSourceFile sourceFile, bool isStartup)
    {
        if (!sourceFile.IsValid())
            return null;

        var file = sourceFile.GetPrimaryPsiFile().NotNull();

        if (!file.Language.Is<GherkinLanguage>())
            return null;

        if (file is not GherkinFile gherkinFile)
            return null;

        var feature = gherkinFile.GetFeatures()
            .FirstOrDefault();

        var bg = feature?.GetScenarios()
            .FirstOrDefault(x => x.IsBackground());

        if (bg is null)
            return null;

        var cacheEntries = new WorkspaceObjectDefinitionsCacheEntries();

        foreach (var gherkinStep in bg.GetSteps())
        {
            var match = _regex.Match(gherkinStep.GetFirstLineText());

            if (!match.Success)
                continue;

            var objName = match.Groups["objName"].Value;

            cacheEntries.Add(new WorkspaceObjectDefinitionsCacheEntry
            {
                Name = objName,
                LineNumber = gherkinFile.GetTreeStartOffset().Offset,
                GherkinStep = gherkinStep
            });
        }

        return cacheEntries;
    }

    public override void MergeLoaded(object data)
    {
        base.MergeLoaded(data);
        PopulateLocalCache();
    }

    public override void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
        RemoveFromLocalCache(sourceFile);
        AddToLocalCache(sourceFile, builtPart as WorkspaceObjectDefinitionsCacheEntries);
        base.Merge(sourceFile, builtPart);
    }

    private void PopulateLocalCache()
    {
        foreach (var (psiSourceFile, cacheItem) in Map)
            AddToLocalCache(psiSourceFile, cacheItem);
    }

    private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] WorkspaceObjectDefinitionsCacheEntries cacheItems)
    {
        if (cacheItems == null)
            return;

        foreach (var cacheEntry in cacheItems)
        {
            _mergeData.WODPerFile.Add(sourceFile, cacheEntry);
        }
    }

    private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
    {
        _mergeData.WODPerFile.RemoveKey(sourceFile);
    }
}
