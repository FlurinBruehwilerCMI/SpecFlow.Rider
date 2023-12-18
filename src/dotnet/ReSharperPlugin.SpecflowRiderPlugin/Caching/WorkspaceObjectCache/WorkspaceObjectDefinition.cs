using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectCache;

public class WorkspaceObjectDefinition
{
    public string Name { get; set; }
    public int LineNumber { get; set; }
    public IDeclaredElement DeclaredElement { get; set; }
}
