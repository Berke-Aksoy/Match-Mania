using UnityEngine;

public sealed class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Singleton { get => _instance; }
    private AudioSource _audioSource;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        _audioSource.PlayOneShot(clip, volume);
    }
}
