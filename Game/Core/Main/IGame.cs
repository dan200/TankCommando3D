namespace Dan200.Core.Main
{
    internal interface IGame
    {
		bool Over { get; set; }
        GameInfo GetInfo();
        void Init();
        void Update(float dt);
        void Render();
        void Shutdown();
    }
}
