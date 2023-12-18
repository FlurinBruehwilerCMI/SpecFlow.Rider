using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util.PersistentMap;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectCache;

[PsiComponent]
public class WorkspaceObjectDefinitionsCache : SimpleICache<WorkspaceObjectDefinitionsCacheEntries>
{
    private readonly Regex _regex = new(@"ein Objekt vom Typ (?<objTyp>\w*) namens (?<objName>\w*)");

    public WorkspaceObjectDefinitionsCache(Lifetime lifetime, [NotNull] IShellLocks locks, [NotNull] IPersistentIndexManager persistentIndexManager,
                                           IUnsafeMarshaller<WorkspaceObjectDefinitionsCacheEntries> valueMarshaller, long? version = null) : base(lifetime, locks, persistentIndexManager,
        valueMarshaller, version)
    {
    }

    protected override bool IsApplicable(IPsiSourceFile sf)
    {
        return sf.LanguageType.Is<CSharpProjectFileType>();
    }

    public override object Build(IPsiSourceFile sourceFile, bool isStartup)
    {
        if (!sourceFile.IsValid())
            return null;

        var file = sourceFile.GetPrimaryPsiFile().NotNull();

        if (sourceFile.Name != "Geschäft wiedereroeffnen.feature")
            return null;

        if (!file.Language.Is<GherkinLanguage>())
            return null;

        var workspaceObjects = new WorkspaceObjectDefinitionsCacheEntries();

        if (file is not GherkinFile gherkinFile)
            return null;

        var feature = gherkinFile.GetFeatures()
            .FirstOrDefault();

        var bg = feature?.GetScenarios()
            .FirstOrDefault(x => x.IsBackground());

        if (bg is null)
            return null;

        var cacheEntry = new FeatureFileCacheEntry
        {
            FeatureFileId = feature.GetFeatureText(),
            FeatureFileVariableScope = new VariableScope()
        };

        foreach (var gherkinStep in bg.GetSteps())
        {
            var match = _regex.Match(gherkinStep.GetFirstLineText());

            if (!match.Success)
                continue;

            var objName = match.Groups["objName"].Value;
            cacheEntry.FeatureFileVariableScope.WorkspaceObjects.Add(new WorkspaceObjectDefinition
            {
                Name = objName,
                LineNumber = gherkinFile.GetTreeStartOffset().Offset
            });
        }

        return workspaceObjects;
    }
}
