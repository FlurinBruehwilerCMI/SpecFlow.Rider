using ReSharperPlugin.SpecflowRiderPlugin.References;

namespace ReSharperPlugin.SpecflowRiderPlugin.Psi
{
    public class GherkinTableCell : GherkinElement
    {
        private SpecflowTableCellReference _reference;

        public GherkinTableCell() : base(GherkinNodeTypes.TABLE_CELL)
        {
        }

        protected override void PreInit()
        {
            base.PreInit();
            _reference = new SpecflowTableCellReference(this);
        }

        protected override string GetPresentableText()
        {
            var textToken = this.FindChild<GherkinToken>(o => o.NodeType == GherkinTokenTypes.TABLE_CELL);
            return textToken?.GetText();
        }

        public SpecflowTableCellReference GetTableCellReference()
        {
            return _reference;
        }
    }
}
