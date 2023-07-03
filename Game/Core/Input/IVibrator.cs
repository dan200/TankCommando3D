namespace Dan200.Core.Input
{
    internal interface IVibrator
    {
        bool CanVibrate { get; }
        void Vibrate(float strength, float duration);
    }
}
