using System;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectCache;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.References;

public class SpecflowTableCellReference : TreeReferenceBase<GherkinTableCell>
{
    public SpecflowTableCellReference(GherkinTableCell owner) : base(owner)
    {
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
        var psiServices = myOwner.GetPsiServices();
        var cache = psiServices.GetComponent<WorkspaceObjectDefinitionsCache>();
        throw new NotImplementedException();
    }

    public override string GetName()
    {
        return myOwner.GetText();
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        throw new System.NotImplementedException();
    }

    public override TreeTextRange GetTreeTextRange()
    {
        return myOwner.GetTreeTextRange();
    }

    public override IReference BindTo(IDeclaredElement element)
    {
        return BindTo(element, EmptySubstitution.INSTANCE);
    }

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
    {
        return myOwner.GetReferences<IReference>().Single();//this should be multiple????
    }

    public override IAccessContext GetAccessContext()
    {
        return new ElementAccessContext(myOwner);
    }
}
