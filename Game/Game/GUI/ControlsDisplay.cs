using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Game.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.GUI
{
    internal class ControlsDisplay : Element
    {
        private Settings m_settings;
        private InputMapper m_mapper;
        private DeviceCategory m_displayedDeviceCategory;

        public ControlsDisplay(Settings settings, InputMapper mapper)
        {
            m_settings = settings;
            m_mapper = mapper;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
            if(m_mapper.LastUsedDeviceCategory != m_displayedDeviceCategory)
            {
                RequestRebuild();
            }
        }

        private void AddInput(GUIBuilder builder, ref Vector2 io_pos, string prompt, string description)
        {
            var font = LowResUI.TextFont;
            var fontSize = LowResUI.TextFontSize;
            var fontHeight = font.GetHeight(fontSize);
            builder.AddText(prompt, io_pos - new Vector2(5.0f, 0.0f), font, fontSize, Colour.White, TextAlignment.Right);
            builder.AddText(Screen.Language.Translate(description), io_pos + new Vector2(5.0f, 0.0f), font, fontSize, Colour.White, TextAlignment.Left);
            io_pos.Y += fontHeight;
        }

        protected override void OnRebuild(GUIBuilder builder)
        {
            var position = Position;
            var size = Size;
            builder.AddQuad(position, position + size, LowResUI.Blank);

            var numLines = 13;
            var font = LowResUI.TextFont;
            var fontSize = LowResUI.TextFontSize;
            var fontHeight = font.GetHeight(fontSize);

            var pos = new Vector2(
                position.X + 0.5f * size.X,
                position.Y + 0.5f * size.Y - 0.5f * numLines * fontHeight
            );

            builder.AddText(Screen.Language.Translate("controls.title"), pos, font, fontSize, Colour.White, TextAlignment.Center);
            pos.Y += 2.0f * fontHeight;

            AddInput(builder, ref pos, m_mapper.GetAxisPair(
                "MoveForward", "MoveBack", "StrafeLeft", "StrafeRight"
            ).TranslatePrompt(Screen.Language), "controls.move");

            string lookPrompt;
            if((m_mapper.LastUsedDeviceCategory == DeviceCategory.Mouse ||
                m_mapper.LastUsedDeviceCategory == DeviceCategory.Keyboard) &&
                m_settings.EnableMouseLook)
            {
                lookPrompt = Screen.Language.Translate("Inputs.Mouse.Cursor");
            }
            else
            {
                lookPrompt = m_mapper.GetAxisPair(
                    "LookUp", "LookDown", "LookLeft", "LookRight"
                ).TranslatePrompt(Screen.Language);
            }
            AddInput(builder, ref pos, lookPrompt, "controls.look");

            AddInput(builder, ref pos, m_mapper.GetInput("Interact").TranslatePrompt(Screen.Language), "controls.interact");
            AddInput(builder, ref pos, m_mapper.GetInput("Fire").TranslatePrompt(Screen.Language), "controls.fire");
            AddInput(builder, ref pos, m_mapper.GetInput("Throw").TranslatePrompt(Screen.Language), "controls.throw");
            AddInput(builder, ref pos, m_mapper.GetInput("Crouch").TranslatePrompt(Screen.Language), "controls.crouch");
            AddInput(builder, ref pos, m_mapper.GetInput("Jump").TranslatePrompt(Screen.Language), "controls.jump");
            AddInput(builder, ref pos, m_mapper.GetInput("Run").TranslatePrompt(Screen.Language), "controls.run");
            pos.Y += fontHeight;

            builder.AddText(Screen.Language.Translate("controls.press_to_begin"), pos, font, fontSize, Colour.White, TextAlignment.Center);
            pos.Y += fontHeight;

            builder.AddText(Screen.Language.Translate("controls.press_to_quit"), pos, font, fontSize, Colour.White, TextAlignment.Center);
            pos.Y += fontHeight;

            m_displayedDeviceCategory = m_mapper.LastUsedDeviceCategory;
        }
    }
}
