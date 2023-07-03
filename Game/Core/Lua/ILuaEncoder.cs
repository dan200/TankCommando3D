namespace Dan200.Core.Lua
{
    internal interface ILuaEncoder
    {
        void EncodeComment(string comment);
        void Encode(LuaValue arg);
    }
}
