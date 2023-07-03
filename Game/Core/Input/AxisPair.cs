using System;
using Dan200.Core.Assets;
using Dan200.Core.Main;
using Dan200.Core.Math;

namespace Dan200.Core.Input
{
    internal struct AxisPair
    {
        public readonly Axis Horizontal;
        public readonly Axis Vertical;

        public Vector2 Value
        {
            get
            {
                return new Vector2(
                    Horizontal.Value,
                    Vertical.Value
                );
            }
        }

        public AxisPair(Input up, Input down, Input left, Input right)
        {
            Horizontal = new Axis(right, left, AxisDirection.Horizontal);
            Vertical = new Axis(up, down, AxisDirection.Vertical);
        }

        public AxisPair(Axis horizontal, Axis vertical)
        {
            App.Assert(horizontal.Direction == AxisDirection.Horizontal);
            App.Assert(vertical.Direction == AxisDirection.Vertical);
            Horizontal = horizontal;
            Vertical = vertical;
        }

        public string TranslatePrompt(Language language)
        {
            var up = Vertical.Positive.Prompt;
            var down = Vertical.Negative.Prompt;
            var left = Horizontal.Negative.Prompt;
            var right = Horizontal.Positive.Prompt;

            var combinedAll = InputPromptUtils.GetCombinedPrompt(up, down, left, right, language);
            if(combinedAll != null)
            {
                return language.Translate(combinedAll);
            }

            var combinedUD = InputPromptUtils.GetCombinedPrompt(down, up, language);
            var combinedLR = InputPromptUtils.GetCombinedPrompt(left, right, language);
            if(combinedUD != null && combinedLR != null)
            {
                return language.Translate(
                    "Inputs.TwoInputCombiner",
                    language.Translate(combinedUD),
                    language.Translate(combinedLR)
                );
            }
            else if(combinedUD != null)
            {
                return language.Translate(
                    "Inputs.ThreeInputCombiner",
                    language.Translate(combinedUD),
                    language.Translate(left),
                    language.Translate(right)
                );
            }
            else if(combinedLR != null)
            {
                return language.Translate(
                    "Inputs.ThreeInputCombiner",
                    language.Translate(up),
                    language.Translate(down),
                    language.Translate(combinedLR)
                );
            }
            else
            {
                return language.Translate(
                    "Inputs.FourInputCombiner",
                    language.Translate(up),
                    language.Translate(left),
                    language.Translate(down),
                    language.Translate(right)
                );
            }
        }
    }
}
