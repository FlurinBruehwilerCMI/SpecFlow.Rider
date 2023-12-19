using JetBrains.ReSharper.Psi;
using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectDefinitions;

//where the WO is defined
public class WorkspaceObjectDefinitionsCacheEntry
{
    public string Name { get; set; }
    public int LineNumber { get; set; }
    public GherkinStep GherkinStep { get; set; }
}
