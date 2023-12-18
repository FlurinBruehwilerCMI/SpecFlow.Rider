using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
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
    }
}
