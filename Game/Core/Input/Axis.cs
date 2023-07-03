using System;
using System.Collections.Generic;
using Dan200.Core.Assets;
using Dan200.Core.Main;

namespace Dan200.Core.Input
{
    internal enum AxisDirection
    {
        Horizontal,
        Vertical,
    }

    internal struct Axis
    {
        public readonly Input Positive;
        public readonly Input Negative;
        public readonly AxisDirection Direction;

        public float Value
        {
            get
            {
                return Positive.Value - Negative.Value;
            }
        }

        public Axis(Input positive, Input negative, AxisDirection direction)
        {
            App.Assert(positive != null);
            App.Assert(negative != null);
            Positive = positive;
            Negative = negative;
            Direction = direction;
        }

        public string TranslatePrompt(Language language)
        {
            var positive = Positive.Prompt;
            var negative = Negative.Prompt;

            var combined = InputPromptUtils.GetCombinedPrompt(negative, positive, language);
            if(combined != null)
            {
                return language.Translate(combined);
            }

            if (Direction == AxisDirection.Horizontal)
            {
                return language.Translate(
                    "Inputs.TwoInputCombiner",
                    language.Translate(negative),
                    language.Translate(positive)
                );
            }
            else
            {
                return language.Translate(
                    "Inputs.TwoInputCombiner",
                    language.Translate(positive),
                    language.Translate(negative)
                );
            }
        }
    }
}
