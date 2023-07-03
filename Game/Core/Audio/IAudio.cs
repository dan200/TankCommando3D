using Dan200.Core.Main;
using Dan200.Core.Math;
using System;

namespace Dan200.Core.Audio
{
    internal interface IAudio : IDisposable
    {
        Matrix4 ListenerTransform { get; set; }
        Vector3 ListenerVelocity { get; set; }

		void SetVolume(AudioCategory category, float volume);
		float GetVolume(AudioCategory category);

		ISoundPlayback PlaySound(Sound sound, bool looping=false, AudioCategory category=AudioCategory.Sound);
		IMusicPlayback PlayMusic(Music music, bool looping=true, float fadeInTime=0.0f, AudioCategory category=AudioCategory.Music);
        ICustomPlayback PlayCustom(ICustomAudioSource source, int channels, int sampleRate, AudioCategory category=AudioCategory.Sound);

        void Update(float dt);
    }
}

