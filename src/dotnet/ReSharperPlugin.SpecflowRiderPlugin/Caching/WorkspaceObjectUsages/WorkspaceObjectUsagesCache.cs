using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util.PersistentMap;
using ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectDefinitions;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectUsages;

[PsiComponent]
public class WorkspaceObjectUsagesCache : SimpleICache<WorkspaceObjectUsagesCacheEntries>
{

    public WorkspaceObjectUsagesCache(Lifetime lifetime, [NotNull] IShellLocks locks, [NotNull] IPersistentIndexManager persistentIndexManager, IUnsafeMarshaller<WorkspaceObjectUsagesCacheEntries> valueMarshaller, long? version = null) : base(lifetime, locks, persistentIndexManager, valueMarshaller, version)
    {
    }

    public override object Build(IPsiSourceFile sourceFile, bool isStartup)
    {
        if (!sourceFile.IsValid())
            return null;

        var file = sourceFile.GetPrimaryPsiFile().NotNull();

        if (!file.Language.Is<GherkinLanguage>())
            return null;

        var workspaceObjects = new WorkspaceObjectDefinitionsCacheEntries();

        if (file is not GherkinFile gherkinFile)
            return null;

        var feature = gherkinFile.GetFeatures()
            .FirstOrDefault();

        if (feature is null)
            return null;

        foreach (var scenario in feature.GetScenarios())
        {
            foreach (var step in scenario.GetSteps())
            {
                step.();
            }
        }
    }
}
