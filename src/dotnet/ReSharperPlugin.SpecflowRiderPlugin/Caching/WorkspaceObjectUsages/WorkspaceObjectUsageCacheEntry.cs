using ReSharperPlugin.SpecflowRiderPlugin.Psi;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectUsages;

//where the wo is used
public class WorkspaceObjectUsageCacheEntry
{
    public string Name { get; set; }
    public int LineNumber { get; set; }
    public GherkinTableCell GherkinTableCell { get; set; }
}
