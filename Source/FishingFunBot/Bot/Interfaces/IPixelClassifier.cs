namespace FishingFun
{
    public interface IPixelClassifier
    {
        double ColourMultiplier { get; set; }

        double ColourClosenessMultiplier { get; set; }
        bool IsMatch(byte red, byte green, byte blue);

        void SetConfiguration(bool isWowClasic);
    }
}