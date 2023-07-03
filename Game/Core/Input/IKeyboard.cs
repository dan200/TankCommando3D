using System.Collections.Generic;

namespace Dan200.Core.Input
{
    internal interface IKeyboard : IDevice
    {
        string Text { get; }
        Input GetInput(Key key);
        void SetClipboardText(string text);
    }
}

