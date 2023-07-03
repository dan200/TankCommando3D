namespace Dan200.Core.Render
{
    internal class PostProcessSettings
    {
        public float Gamma;
        public float Saturation;

        public PostProcessSettings()
        {
            Gamma = 1.0f;
            Saturation = 1.0f;
        }
    }

    internal class View
    {
        public readonly RenderTexture Target; // null = the screen
        public readonly Quad Viewport; // 0-1 screen co-ordinates
        public readonly Camera Camera;
        public readonly PostProcessSettings PostProcessSettings;

        public View(RenderTexture target, Quad viewport)
        {
            Target = target;
            Viewport = viewport;
            Camera = new Camera();
            PostProcessSettings = new PostProcessSettings();
        }
    }
}
