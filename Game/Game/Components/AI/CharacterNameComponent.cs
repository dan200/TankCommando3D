using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.AI
{
    internal struct CharacterNameComponentData
    {
        public string[] Parts;
    }

    [RequireSystem(typeof(GUISystem))]
    internal class CharacterNameComponent : Component<CharacterNameComponentData>
    {
        private string m_name;

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        protected override void OnInit(in CharacterNameComponentData properties)
        {
            m_name = Generate(properties.Parts);
        }

        protected override void OnShutdown()
        {
        }

        private string Generate(string[] parts)
        {
            var language = Level.GetSystem<GUISystem>().Screen.Language;
            App.Assert(parts.Length > 0);
            if(parts.Length == 1)
            {
                var key = language.GetRandomVariant(parts[0]);
                return language.Translate(key);
            }
            else
            {
                var builder = new StringBuilder();
                for(int i=0; i<parts.Length; ++i)
                {
                    var key = language.GetRandomVariant(parts[i]);
                    builder.Append( language.Translate(key) );
                    if(i < parts.Length - 1)
                    {
                        builder.Append(" ");
                    }
                }
                return builder.ToString();
            }
        }
    }
}
