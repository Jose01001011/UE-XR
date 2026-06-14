using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundscapeController : MonoBehaviour
{
    [Header("Soundscape Audio")]
    [Tooltip("Drag and drop your audio clip here.")]
    [SerializeField] private AudioClip soundscapeClip;

    [Header("Playback Settings")]
    [Tooltip("Set the volume of the soundscape (0.0 to 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;

    void Start()
    {
        // Fetch the AudioSource component automatically added to this GameObject
        audioSource = GetComponent<AudioSource>();

        if (soundscapeClip != null)
        {
            // Configure the AudioSource
            audioSource.clip = soundscapeClip;
            audioSource.loop = true;          // Soundscapes generally loop endlessly
            audioSource.volume = volume;
            
            // Start playing the audio
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("SoundscapeController: No Audio Clip was assigned. Please drag an audio file into the Inspector.");
        }
    }
}