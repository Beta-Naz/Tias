namespace TIAS.Interface
{
    public interface ITurnBased
    {
        bool HasMovedThisTurn { get; set; }
        bool HasAttackedThisTurn { get; set; }
        void ResetTurn();
        bool CanAct();
    }
}
