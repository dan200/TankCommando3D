using System.Collections.Generic;
using System.Text;
using Dan200.Core.Lua;
using Dan200.Core.Script;
using Dan200.Core.Main;
using Dan200.Core.Util;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;

namespace Dan200.Core.Systems
{
    internal class ScriptSystem : Level.SystemBase, IUpdate
    {
        private LuaMachine m_machine;
        private List<LuaCoroutine> m_activeCoroutines;

        protected override void OnInit(LuaTable properties)
        {
            // Create the machine
            m_machine = new LuaMachine(null, true);
            m_machine.AllowByteCodeLoading = false;
            m_machine.RemoveUnsafeGlobals();
            m_activeCoroutines = new List<LuaCoroutine>();

            // Install APIs
            AddAPI(new LevelAPI(Level));
            AddAPI(new EntityAPI(Level));

            // Install globals
            m_machine.SetGlobal(ByteString.Intern("dofile"), (LuaCFunction)dofile);
            m_machine.SetGlobal(ByteString.Intern("print"), (LuaCFunction)print);
            m_machine.SetGlobal(ByteString.Intern("start"), (LuaCFunction)start);
            m_machine.DoString(ByteString.Intern(@"
                local function expect( value, sExpectedType, nArg )
                    local sFoundType = type( value )
                    if sFoundType ~= sExpectedType then
                        error( ""Expected "" .. sExpectedType .. "" at argument "" .. nArg .. "", got "" .. sFoundType, 3 )
                    end
                end

                local tResults = {}
                function require( s )
                    expect( s, ""string"", 1 )
                    if tResults[s] == nil then
                        local ok, result = pcall( dofile, ""scripts/"" .. s .. "".lua"" )
                        if not ok then
                            error( result, 0 )
                        elseif result == nil then
                            tResults[s] = true
                        else
                            tResults[s] = result
                        end
                    end
                    return tResults[s]
                end

				yield = coroutine.yield

				function sleep( t )
	                expect( t, ""number"", 1 )
	                local l = level.getTime() + t
	                repeat
	                    yield()
	                until level.getTime() >= l
	            end"),
                ByteString.Intern("=ScriptController.ctor\0")
            );
        }

        protected override void OnShutdown()
        {
            m_activeCoroutines.Clear();
            m_machine.Dispose();
            m_machine = null;
        }

        public void Update(float dt)
        {
            // Resume all coroutines
            for (int i = 0; i < m_activeCoroutines.Count; ++i)
            {
                // Resume coroutine
                var coroutine = m_activeCoroutines[i];
                try
                {
                    coroutine.Resume(LuaArgs.Empty);
                }
                catch (LuaError e)
                {
					App.LogError(e.Message);
                }

                // Discard if finished
                if(coroutine.IsFinished)
                {
                    m_activeCoroutines.UnorderedRemoveAt(i);
                    i--;
                }
            }
        }

        public void AddAPI(LuaAPI api)
        {
            api.Install(m_machine);
        }

        public void RunScript(LuaScript script)
        {
            App.Assert(script != null);
            try
            {
                m_machine.DoString(script.ByteCode, script.ChunkName, true);
            }
            catch (LuaError e)
            {
                App.LogError(e.Message);
            }
        }

        public void StartCoroutine(LuaFunction function, in LuaArgs args)
        {
            try
            {
                var coroutine = m_machine.CreateCoroutine(function);
                coroutine.Resume(args);
                if (!coroutine.IsFinished)
                {
                    m_activeCoroutines.Add(coroutine);
                }
            }
            catch(LuaError e)
            {
				App.LogError(e.Message);
            }
        }

		public LuaFunction LoadString(string str, string chunkName)
		{
			return m_machine.LoadString(new ByteString(str), new ByteString(chunkName + '\0'), false);
		}

		public LuaArgs DoString(string str, string chunkName)
		{
			return m_machine.DoString(new ByteString(str), new ByteString(chunkName + '\0'), false);
		}

        private LuaArgs dofile(in LuaArgs args)
        {
            var path = args.GetString(0);
            if (Assets.Assets.Exists<LuaScript>(path))
            {
                var script = LuaScript.Get(path);
                return m_machine.DoString(script.ByteCode, script.ChunkName, true);
            }
            throw new LuaError("Script not found: " + path);
        }

        private LuaArgs print(in LuaArgs args)
        {
            var output = new StringBuilder();
            for (int i = 0; i < args.Length; ++i)
            {
                output.Append(args.ToString(i));
                if (i < args.Length - 1)
                {
                    output.Append('\t');
                }
            }
			App.Log("{0}", output.ToString());
            return LuaArgs.Empty;
        }

        private LuaArgs start(in LuaArgs args)
        {
            var function = args.GetFunction(0);
            var functionArgs = args.Select(1);
            StartCoroutine(function, functionArgs);
            return LuaArgs.Empty;
        }
    }
}
