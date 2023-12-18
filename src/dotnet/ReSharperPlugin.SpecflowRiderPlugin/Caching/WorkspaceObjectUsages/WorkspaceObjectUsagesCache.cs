using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.PersistentMap;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectUsages;

[PsiComponent]
public class WorkspaceObjectUsagesCache : SimpleICache<WorkspaceObjectUsagesCacheEntries>
{
    private readonly WorkspaceObjectUsageMergeData _mergeData = new();
    public WorkspaceObjectUsagesCache(Lifetime lifetime, [NotNull] IShellLocks locks, [NotNull] IPersistentIndexManager persistentIndexManager, IUnsafeMarshaller<WorkspaceObjectUsagesCacheEntries> valueMarshaller, long? version = null) : base(lifetime, locks, persistentIndexManager, valueMarshaller, version)
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

        if (feature is null)
            return null;

        var cacheEntries = new WorkspaceObjectUsagesCacheEntries();

        foreach (var scenario in feature.GetScenarios())
        {
            foreach (var step in scenario.GetSteps())
            {
                var table = step.Children<GherkinTable>().First();
                foreach (var gherkinTableRow in table.Children<GherkinTableRow>().Skip(1))
                {
                    var secondColumn = gherkinTableRow.Children<GherkinTableCell>().Skip(1).First();
                    cacheEntries.Add(new WorkspaceObjectUsageCacheEntry
                    {
                        Name = secondColumn.GetText(),
                        LineNumber = secondColumn.GetTreeStartOffset().Offset,
                        GherkinTableCell = secondColumn
                    });
                }
            }
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
        AddToLocalCache(sourceFile, builtPart as WorkspaceObjectUsagesCacheEntries);
        base.Merge(sourceFile, builtPart);
    }

    private void PopulateLocalCache()
    {
        foreach (var (psiSourceFile, cacheItem) in Map)
            AddToLocalCache(psiSourceFile, cacheItem);
    }

    private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] WorkspaceObjectUsagesCacheEntries cacheItems)
    {
        if (cacheItems == null)
            return;

        foreach (var cacheEntry in cacheItems)
        {
            _mergeData.WOUPerFile.Add(sourceFile, cacheEntry);
        }
    }

    private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
    {
        _mergeData.WOUPerFile.RemoveKey(sourceFile);
    }
}
