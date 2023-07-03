

using Dan200.Core.Input;
using Dan200.Core.Level;

namespace Dan200.Game.Game
{
    internal class TestState : InGameState
    {
        private string m_levelSavePath;

        public TestState(Game game, string levelLoadPath, string levelSavePath) : base(game, levelLoadPath)
        {
            m_levelSavePath = levelSavePath;
        }

		public override void Update(float dt)
        {
            base.Update(dt);

            // Edit
            if (Game.InputDevices.Keyboard != null &&
                Game.InputDevices.Keyboard.GetInput(Key.Escape).Pressed)
            {
                CutToState(new EditorState(Game, Level.Data.Path, m_levelSavePath));
            }
        }

        public override void Restart()
        {
            CutToState(new TestState(Game, Level.Data.Path, m_levelSavePath));
        }
    }
}
