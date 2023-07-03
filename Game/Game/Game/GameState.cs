using Dan200.Core.Render;
using Dan200.Core.Util;

namespace Dan200.Game.Game
{
    internal abstract class GameState : IState<GameState>
    {
        private readonly Game m_game;

        public Game Game
        {
            get
            {
                return m_game;
            }
        }

        public GameState(Game game)
        {
            m_game = game;
        }

		public abstract void Enter(GameState previous);
		public abstract void Leave(GameState next);

		public abstract void OnConsoleCommand(string command);

		public abstract void Update(float dt);
		public abstract void PopulateCamera(View view);
		public abstract void Draw(IRenderer renderer, View view);
	}
}
