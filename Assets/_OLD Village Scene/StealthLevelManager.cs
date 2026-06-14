using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Required for handling the Blackout Canvas Image

public class StealthLevelManager : MonoBehaviour
{
    [Header("Intro Models (Turned ON at start)")]
    [SerializeField] private GameObject thiefIntroModel;
    [SerializeField] private GameObject signallerIntroModel;

    [Header("Gameplay Models (Turned OFF at start)")]
    [SerializeField] private GameObject thiefGameplayModel;
    [SerializeField] private GameObject signallerGameplayModel;

    [Header("Cutscene Timing")]
    [Tooltip("Total duration of the introductory walking sequence before fading to the gameplay scene.")]
    [SerializeField] private float cutsceneDuration = 6.0f; 

    [Header("VR Blackout UI Settings")]
    [Tooltip("Drag the pure black UI Image from your Blackout Canvas here.")]
    [SerializeField] private Image blackoutImage;
    [Tooltip("How long it takes to fade completely to black / fade back into the scene.")]
    [SerializeField] private float fadeDuration = 1.0f;

    void Start()
    {
        // 1. Enforce initial visibility layout
        if (thiefIntroModel != null) thiefIntroModel.SetActive(true);
        if (signallerIntroModel != null) signallerIntroModel.SetActive(true);

        if (thiefGameplayModel != null) thiefGameplayModel.SetActive(false);
        if (signallerGameplayModel != null) signallerGameplayModel.SetActive(false);

        // 2. Clear the blindfold screen to transparent at startup
        if (blackoutImage != null)
        {
            blackoutImage.gameObject.SetActive(true);
            blackoutImage.color = new Color(0f, 0f, 0f, 0f); // Purely clear alpha
        }

        // 3. Launch the sequence sequence
        StartCoroutine(PlayIntroAndSwapModels());
    }

    IEnumerator PlayIntroAndSwapModels()
    {
        // Give the Animators a split second to finish initializing on frame 1
        yield return null; 

        // 4. Fire the exact "Walk" triggers to force them out of Idle!
        Animator thiefIntroAnim = thiefIntroModel != null ? thiefIntroModel.GetComponent<Animator>() : null;
        Animator signallerIntroAnim = signallerIntroModel != null ? signallerIntroModel.GetComponent<Animator>() : null;

        if (thiefIntroAnim != null) thiefIntroAnim.SetTrigger("Walk");
        if (signallerIntroAnim != null) signallerIntroAnim.SetTrigger("Walk");

        Debug.Log("Level Manager: Intro models successfully commanded to walk.");

        // Wait for the timeline movement phase (subtracting fade time so we hit our total duration target)
        yield return new WaitForSeconds(Mathf.Max(0.1f, cutsceneDuration - fadeDuration));

        // ==========================================
        // PHASE 1: VR FADE TO BLACK
        // ==========================================
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            if (blackoutImage != null) blackoutImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        // ==========================================
        // PHASE 2: SYSTEM SWAP (In Total Darkness)
        // ==========================================
        Debug.Log("Level Manager: Screen is black. Performing 4-model swap...");

        // Hide old props
        if (thiefIntroModel != null) thiefIntroModel.SetActive(false);
        if (signallerIntroModel != null) signallerIntroModel.SetActive(false);

        // Turn on real gameplay actors (this wakes up ThiefStealthAI.cs automatically)
        if (thiefGameplayModel != null) thiefGameplayModel.SetActive(true);
        if (signallerGameplayModel != null) signallerGameplayModel.SetActive(true);

        // Give physics and AI frameworks one moment to process positions in the dark
        yield return new WaitForSeconds(0.5f);

        // ==========================================
        // PHASE 3: FADE BACK INTO GAMEPLAY
        // ==========================================
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(1.0f - (timer / fadeDuration));
            if (blackoutImage != null) blackoutImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        Debug.Log("Level Manager: Fade complete. Stealth level is officially live!");
    }
}