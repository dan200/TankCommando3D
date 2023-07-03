namespace Dan200.Core.Util
{
    internal struct StructEventArgs
    {
        public static StructEventArgs Empty = new StructEventArgs();
    }

    internal delegate void StructEventHandler<TSender>(
        TSender sender,
        StructEventArgs args
    );

    internal delegate void StructEventHandler<TSender, TEventArgs>(
        TSender sender,
        TEventArgs args
    ) where TEventArgs : struct;

    internal delegate void StaticStructEventHandler(
        StructEventArgs args
    );

    internal delegate void StaticStructEventHandler<TEventArgs>(
        TEventArgs args
    ) where TEventArgs : struct;
}
