namespace TIAS.Interface
{
    public interface IHighlightable
    {
        void Highlight(bool isHighlighted);
        void ShowMoveRange(int range);
        void ShowAttackRange(float range);
        void ClearHighlights();
    }
}
