using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.UI.Icons;
using ReSharperPlugin.SpecflowRiderPlugin.Caching.StepsDefinitions;
using ReSharperPlugin.SpecflowRiderPlugin.References;
using ReSharperPlugin.SpecflowRiderPlugin.Utils.TestOutput;

namespace ReSharperPlugin.SpecflowRiderPlugin.Psi
{
    public class GherkinStep : GherkinElement, IDeclaredElement, IDeclaration
    {
        public GherkinStepKind StepKind { get; }
        public GherkinStepKind EffectiveStepKind { get; }
        public SpecflowStepDeclarationReference _reference;

        public GherkinStep(GherkinStepKind stepKind, GherkinStepKind effectiveStepKind) : base(GherkinNodeTypes.STEP)
        {
            StepKind = stepKind;
            EffectiveStepKind = effectiveStepKind;
        }

        protected override void PreInit()
        {
            base.PreInit();
            _reference = new SpecflowStepDeclarationReference(this);
        }

        public DocumentRange GetStepTextRange()
        {
            var token = GetFirstTextToken();
            if (token == null)
                return new DocumentRange(LastChild.GetDocumentEndOffset(), LastChild.GetDocumentEndOffset());
            return new DocumentRange(token.GetDocumentStartOffset(), LastChild.GetDocumentEndOffset());
        }

        public IEnumerable<string> GetEffectiveTags()
        {
            var gherkinScenario = GetContainingNode<GherkinScenario>();
            if (gherkinScenario == null)
                return Enumerable.Empty<string>();
            var gherkinFeature = gherkinScenario.GetContainingNode<GherkinFeature>();
            if (gherkinFeature == null)
                return gherkinScenario.GetTags();
            return gherkinScenario.GetTags().Concat(gherkinFeature.GetTags());
        }

        private ITreeNode GetFirstTextToken()
        {
            for (var node = FirstChild; node != null; node = node.NextSibling)
            {
                if (node is GherkinToken token)
                {
                    if (token.NodeType == GherkinTokenTypes.STEP_KEYWORD)
                        continue;
                    if (token.NodeType == GherkinTokenTypes.WHITE_SPACE)
                        continue;
                }
                return node;
            }
            return null;
        }

        public string GetStepTextBeforeCaret(DocumentOffset caretLocation)
        {
            var sb = new StringBuilder();
            for (var te = GetFirstTextToken(); te != null; te = te.NextSibling)
            {
                if (te.GetDocumentStartOffset() > caretLocation)
                    break;
                var truncateTextSize = 0;
                if (te.GetDocumentEndOffset() > caretLocation)
                {
                    truncateTextSize = te.GetDocumentEndOffset().Offset - caretLocation.Offset;
                }
                switch (te)
                {
                    case GherkinStepParameter p:
                        sb.Append(p.GetText());
                        break;
                    case GherkinToken token:
                        if (token.NodeType != GherkinTokenTypes.STEP_KEYWORD)
                            sb.Append(token.GetText());
                        break;
                }
                if (truncateTextSize >= sb.Length)
                    return string.Empty;
                sb.Length -= truncateTextSize;
            }
            return sb.ToString().Trim();
        }

        public string GetStepText(bool withStepKeyWord = false)
        {
            var sb = new StringBuilder();

            var element = (TreeElement)FirstChild;
            if (!withStepKeyWord)
            {
                // Skip keyword and white space at tbe begining
                for (; element != null && (element.NodeType == GherkinTokenTypes.STEP_KEYWORD || element.NodeType == GherkinTokenTypes.WHITE_SPACE); element = element.nextSibling)
                    element = element.nextSibling;
            }

            var eol = false;
            for (; element != null && !eol; element = element.nextSibling)
            {
                switch (element)
                {
                    case GherkinStepParameter p:
                        sb.Append(p.GetText());
                        break;
                    case GherkinToken token:
                    {
                        if (token.IsWhitespaceToken() && element.nextSibling?.IsWhitespaceToken() == false)
                            sb.Append(token.GetText());
                        else if (!token.IsWhitespaceToken())
                            sb.Append(token.GetText());
                        if (token.NodeType == GherkinTokenTypes.NEW_LINE)
                            eol = true;
                        break;
                    }
                }
            }
            return sb.ToString();
        }

        public string GetStepTextForExample(IDictionary<string, string> exampleData)
        {
            var sb = new StringBuilder();
            var previousTokenWasAParameter = false;
            // Skip keyword and white space at tbe begining
            var element = (TreeElement)FirstChild;
            for (; element != null && (element.NodeType == GherkinTokenTypes.STEP_KEYWORD || element.NodeType == GherkinTokenTypes.WHITE_SPACE); element = element.nextSibling)
                element = element.nextSibling;
            var eol = false;
            for (; element != null && !eol; element = element.nextSibling)
            {
                switch (element)
                {
                    case GherkinStepParameter p:
                        if (exampleData.TryGetValue(p.GetParameterName(), out var value))
                        {
                            previousTokenWasAParameter = true;
                            sb.Length--; // Remove `<`
                            sb.Append(value);
                        }
                        else
                        {
                            sb.Append(p.GetText());
                        }

                        break;

                    case GherkinToken token:
                        // Remove `>`

                        if (token.IsWhitespaceToken() && element.nextSibling?.IsWhitespaceToken() == false)
                        {
                            sb.Append(token.GetText());
                            if (previousTokenWasAParameter)
                                sb.Length--;
                        }
                        else if (!token.IsWhitespaceToken())
                        {
                            sb.Append(token.GetText());
                            if (previousTokenWasAParameter)
                                sb.Length--;
                        }
                        if (token.NodeType == GherkinTokenTypes.NEW_LINE)
                            eol = true;
                        previousTokenWasAParameter = false;
                        break;
                }
            }
            return sb.ToString();
        }

        public SpecflowStepDeclarationReference GetStepReference()
        {
            return _reference;
        }

        public override ReferenceCollection GetFirstClassReferences()
        {
            return new ReferenceCollection(_reference);
        }

        public string GetFirstLineText()
        {
            var sb = new StringBuilder();
            for (var te = (TreeElement)FirstChild; te != null; te = te.nextSibling)
            {
                if (te.GetTokenType() == GherkinTokenTypes.NEW_LINE)
                    break;

                sb.Append(te.GetText());
            }
            return sb.ToString().Trim();
        }

        public bool Match(StepTestOutput failedStepStepsOutput)
        {
            return GetFirstLineText() == failedStepStepsOutput.FirstLine;
        }

        public bool MatchScope([CanBeNull] IReadOnlyList<SpecflowStepScope> scopes)
        {
            if (scopes == null)
                return true;

            foreach (var scope in scopes)
            {
                if (scope.Scenario is not null)
                {
                    var matchScenario = GetScenarioText() == scope.Scenario;
                    if (!matchScenario)
                        continue;
                }

                if (scope.Feature is not null)
                {
                    var matchFeature = GetFeatureText() == scope.Feature;
                    if (!matchFeature)
                        continue;
                }

                if (scope.Tag is not null)
                {
                    var matchTag = GetEffectiveTags().Contains(scope.Tag);
                    if (!matchTag)
                        continue;
                }

                return true;
            }
            return false;
        }

        [CanBeNull]
        public string GetFeatureText()
        {
            return GetContainingNode<GherkinFeature>()?.GetFeatureText();
        }

        [CanBeNull]
        public string GetScenarioText()
        {
            return GetContainingNode<GherkinScenario>()?.GetScenarioText();
        }

        public DeclaredElementType GetElementType() => GherkinDeclaredElementType.STEP;

        public void SetName(string name)
        {

        }

        public TreeTextRange GetNameRange()
        {
            return new TreeTextRange();
        }

        public bool IsSynthetic()
        {
            return false;
        }

        public IDeclaredElement DeclaredElement { get; }
        public string DeclaredName { get; } = string.Empty;

        public IList<IDeclaration> GetDeclarations()
        {
            return new List<IDeclaration>
            {
                this
            };
        }

        public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
        {
            return new List<IDeclaration>
            {
                this
            };
        }

        public XmlNode GetXMLDoc(bool inherit)
        {
            return new XmlDocument();
        }

        public XmlNode GetXMLDescriptionSummary(bool inherit)
        {
            return new XmlDocument();
        }

        public string ShortName { get; } = string.Empty;
        public bool CaseSensitiveName { get; } = false;
        public PsiLanguageType PresentationLanguage { get; } = GherkinLanguage.Instance;

    }

    public class GherkinDeclaredElementType : DeclaredElementTypeBase
    {
        [NotNull]
        public static readonly DeclaredElementType STEP = new GherkinDeclaredElementType("step", PsiSymbolsThemedIcons.Method.Id);

        private GherkinDeclaredElementType(string name, [CanBeNull] IconId imageName) : base(name, imageName)
        {
        }

        protected override IDeclaredElementPresenter DefaultPresenter { get; }
    }
}
