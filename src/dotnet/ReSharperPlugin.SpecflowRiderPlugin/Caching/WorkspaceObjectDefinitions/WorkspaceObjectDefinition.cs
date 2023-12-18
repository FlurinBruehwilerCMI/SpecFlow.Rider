using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectDefinitions;

public class WorkspaceObjectDefinition
{
    public string Name { get; set; }
    public int LineNumber { get; set; }
    public IDeclaredElement DeclaredElement { get; set; }
}
