using System.IO;
using Dan200.Core.Main;
using Dan200.Core.Network;

namespace Dan200.Game.User
{
    internal class User
    {
        public Settings Settings
        {
            get;
            private set;
        }

        public Progress Progress
        {
            get;
            private set;
        }

        public User(INetwork network)
        {
            Settings = new Settings(Path.Combine(App.SavePath, "settings.txt"));
			if (!Settings.Load())
			{
                Settings.Load(Path.Combine(App.AssetPath, "default_settings.txt"));
				Settings.Save();
			}
            Progress = new Progress(network, Path.Combine(App.SavePath, "progress.txt"));
            if (!Progress.Load())
            {
                Progress.Reset();
                Progress.Save();
            }
        }
    }
}
