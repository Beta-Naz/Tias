namespace TIAS.Interface
{
    public interface ISelectable
    {
        bool IsSelected { get; set; }
        void OnSelected();
        void OnDeselected();
    }
}
