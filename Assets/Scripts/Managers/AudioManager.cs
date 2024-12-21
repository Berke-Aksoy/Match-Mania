using Unity.VisualScripting;
using UnityEngine;

public class AudioManager : BaseSingleton<AudioManager>
{
    private AudioSource _audioSource;

    protected override void Awake()
    {
        base.Awake();
        GetAudioSource();
    }

    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        _audioSource?.PlayOneShot(clip, volume);
    }

    private void GetAudioSource()
    {
        TryGetComponent<AudioSource>(out _audioSource);

        if (_audioSource == null)
        {
            _audioSource = this.AddComponent<AudioSource>();
        }
    }
}
