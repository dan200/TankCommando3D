using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dan200.Core.GUI
{
	internal class Container : Element
    {
		public Container(float width, float height)
        {
            Size = new Vector2(width, height);
        }

		protected override void OnInit()
		{			
		}

		protected override void OnUpdate(float dt)
		{
		}

		protected override void OnRebuild(GUIBuilder builder)
		{
		}
    }
}

