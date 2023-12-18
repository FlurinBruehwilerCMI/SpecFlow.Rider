using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectDefinitions;

public class WorkspaceObjectDefinitionsMergeData
{
    public readonly OneToSetMap<IPsiSourceFile, VariableScope> WODPerFile = new();
}
