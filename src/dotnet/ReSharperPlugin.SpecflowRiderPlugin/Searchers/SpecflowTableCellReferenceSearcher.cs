using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.SpecflowRiderPlugin.Extensions;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Searchers;

public class SpecflowTableCellReferenceSearcher : IDomainSpecificSearcher
{
    private readonly IDeclaredElementsSet _declaredElements;

    public SpecflowTableCellReferenceSearcher(IDeclaredElementsSet declaredElements)
    {
        _declaredElements = declaredElements;
    }

    public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
    {
        return sourceFile.LanguageType.Is<GherkinProjectFileType>()
               && sourceFile.GetPsiFiles<GherkinLanguage>().Any(file => ProcessElement(file, consumer));
    }

    public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
    {
        if (!element.Language.Is<GherkinLanguage>())
            return false;
        var containingFile = element.GetContainingFile();
        if (containingFile == null)
            return false;
        var projectFile = containingFile.GetSourceFile().ToProjectFile();
        if (projectFile == null || !projectFile.IsValid())
            return false;
        foreach (var declaredElement in _declaredElements)
        {
            if (declaredElement is not GherkinStep gherkinStep)
                continue;
            foreach (var gherkinTableCell in element.GetChildrenInSubtrees<GherkinTableCell>())
            {
                var reference = gherkinTableCell.GetTableCellReference();
                var resolveResultWithInfo = reference.Resolve();
                if (resolveResultWithInfo.ResolveErrorType == ResolveErrorType.OK)
                {
                    foreach (var woDeclaration in resolveResultWithInfo.Result.Elements<GherkinStep>())
                    {
                        if (woDeclaration.Element.Equals(gherkinStep) && consumer.Accept(new FindResultReference(reference)) == FindExecution.Stop)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
