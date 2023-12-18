using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectDefinitions;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectUsages;

public class WorkspaceObjectUsageMergeData
{
    public readonly OneToSetMap<IPsiSourceFile, WorkspaceObjectUsageCacheEntry> WOUPerFile = new();
}
