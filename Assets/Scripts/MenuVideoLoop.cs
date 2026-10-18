using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class MenuVideoLoop : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer != null)
        {
            videoPlayer.isLooping = true;
            // Disable direct audio output on VideoPlayer since audio is handled by BGM AudioSource
            // This prevents Windows Media Foundation audio clock drift from freezing video frames
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.loopPointReached += OnLoopPointReached;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.skipOnDrop = true;
            videoPlayer.waitForFirstFrame = true;
        }
    }

    private void Start()
    {
        PlayVideo();
    }

    private void OnEnable()
    {
        PlayVideo();
    }

    private void Update()
    {
        // Safety check: if playback unexpectedly stalled at the end, force restart
        if (videoPlayer != null && videoPlayer.isPrepared && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
    }

    private void PlayVideo()
    {
        if (videoPlayer != null)
        {
            if (!videoPlayer.isPlaying)
            {
                videoPlayer.Play();
            }
        }
    }

    private void OnLoopPointReached(VideoPlayer source)
    {
        source.time = 0;
        source.Play();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("[MenuVideoLoop] Video warning: " + message + ". Restarting loop...");
        source.time = 0;
        source.Play();
    }
}
