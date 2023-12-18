using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectDefinitions;
using ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectUsages;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Searchers;

[PsiSharedComponent]
public class SpeclFlowTablelCellReferenceSearcherFactory : DomainSpecificSearcherFactoryBase
{
    public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
    {
        return languageType.Is<CSharpLanguage>() || languageType.Is<GherkinLanguage>();
    }

    public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
    {
    }

    public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements, ReferenceSearcherParameters referenceSearcherParameters)
    {
        return new SpecflowTableCellReferenceSearcher(elements);
    }

    public override ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
    {
        if (!(declaredElement is GherkinStep gherkinStep))
            return base.GetDeclaredElementSearchDomain(declaredElement);

        var workspaceObjectDefinitionsCache = declaredElement.GetPsiServices().GetComponent<WorkspaceObjectDefinitionsCache>();
        var workspaceObjectUsagesCache = declaredElement.GetPsiServices().GetComponent<WorkspaceObjectUsagesCache>();

        var files = new List<IPsiSourceFile>();
        foreach (var sourceFile in declaredElement.GetSourceFiles())
        {
            var wodsInFile = workspaceObjectDefinitionsCache.VariableScopesPerFile[sourceFile];
            foreach (var variableScope in wodsInFile
                         .Where(x =>
                             x.Name == gherkinStep.DeclaredName))//todo compare with name parameter
            {

            }
        }

        return SearchDomainFactory.Instance.CreateSearchDomain(files);
    }
}
