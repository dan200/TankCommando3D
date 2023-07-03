namespace Dan200.Game.Options
{
    internal interface IOption<T>
    {
        T Value { get; set; }
    }
}
