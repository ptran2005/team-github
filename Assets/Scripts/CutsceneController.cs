using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(VideoPlayer))]
public class CutsceneController : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private bool isLeaving;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
    }

    private void Start()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame ||
             keyboard.spaceKey.wasPressedThisFrame ||
             keyboard.escapeKey.wasPressedThisFrame))
        {
            SkipCutscene();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            SkipCutscene();
        }
#endif
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        SkipCutscene();
    }

    private void SkipCutscene()
    {
        if (isLeaving)
            return;

        isLeaving = true;
        videoPlayer.Stop();
        SceneManager.LoadScene("MainGame");
    }

    private void OnDestroy()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
    }
}