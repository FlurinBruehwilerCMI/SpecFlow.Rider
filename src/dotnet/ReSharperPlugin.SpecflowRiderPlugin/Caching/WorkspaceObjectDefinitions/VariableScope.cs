using System.Collections.Generic;

namespace ReSharperPlugin.SpecflowRiderPlugin.Caching.WorkspaceObjectDefinitions;

public class VariableScope
{
    public List<VariableScope> ReferencedScopes { get; set; }
    public List<WorkspaceObjectDefinition> WorkspaceObjects { get; set; }
}
