namespace Dan200.Core.Async
{
    internal interface IProgress
    {
        int CurrentProgress { get; }
        int TotalProgress { get; }
    }
}
