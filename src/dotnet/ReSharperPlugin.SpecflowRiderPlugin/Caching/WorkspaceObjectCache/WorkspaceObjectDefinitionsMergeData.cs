using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using ReSharperPlugin.SpecflowRiderPlugin.Caching.StepsDefinitions;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectCache;

public class WorkspaceObjectDefinitionsMergeData
{
    public readonly OneToSetMap<IPsiSourceFile, VariableScope> WODPerFile = new();
}
