using Dan200.Core.Main;
using System;
using Dan200.Core.Platform.SDL2;

namespace Dan200.Game.Main
{
    public class Program
    {
        public static void Main(string[] args)
        {
			SDL2Platform.Run(new Game.Game(), args);
        }
    }
}

