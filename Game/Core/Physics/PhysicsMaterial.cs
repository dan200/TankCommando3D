using System;
using System.IO;
using Dan200.Core.Assets;
using Dan200.Core.Lua;

namespace Dan200.Core.Physics
{
	internal class PhysicsMaterial : IBasicAsset
    {
		public static PhysicsMaterial Default
		{
			get
			{
				return Get("physics/concrete.physicsMaterial");
			}
		}

		public static PhysicsMaterial Get(string path)
		{
			return Assets.Assets.Get<PhysicsMaterial>(path);
		}

		public string Path
		{
			get;
			private set;
		}

        public float Friction
		{
			get;
			private set;
		}

		public float Restitution
		{
			get;
			private set;
		}

		public static object LoadData(Stream stream, string path)
		{
			var lon = new LONDecoder(stream);
			return lon.DecodeValue().GetTable();
		}

		public PhysicsMaterial(string path, object data)
        {
			Path = path;
			Reload(data);
        }

		public void Dispose()
		{
		}

		public void Reload(object data)
		{
			var table = (LuaTable)data;
			Friction = table.GetFloat("Friction");
			Restitution = table.GetFloat("Restitution");
		}
	}
}
