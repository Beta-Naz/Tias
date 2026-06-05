namespace TIAS.Interface
{
    public interface IBuy
    {
        float Price { get; }
        bool CanBuy(float price);
        void Buy(float price);
    }
}
