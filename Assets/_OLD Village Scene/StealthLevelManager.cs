using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement; 

public class StealthLevelManager : MonoBehaviour
{
    [Header("Intro Models (Turned ON at start)")]
    [SerializeField] private GameObject thiefIntroModel;
    [SerializeField] private GameObject signallerIntroModel;

    [Header("Gameplay Models (Turned OFF at start)")]
    [SerializeField] private GameObject thiefGameplayModel;
    [SerializeField] private GameObject signallerGameplayModel;
    [SerializeField] private GameObject ostrichGameplayModel; // <-- ADDED THIS: Drag the Ostrich here!

    [Header("Cutscene Timing")]
    [Tooltip("Total duration of the introductory walking sequence before fading to the gameplay scene.")]
    [SerializeField] private float cutsceneDuration = 6.0f; 

    [Header("VR Blackout UI Settings")]
    [Tooltip("Drag the pure black UI Image from your Blackout Canvas here.")]
    [SerializeField] private Image blackoutImage;
    [Tooltip("How long it takes to fade completely to black / fade back into the scene.")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("Game Over UI")]
    [Tooltip("Drag your Game Over UI Text or Panel GameObject here.")]
    [SerializeField] private GameObject gameOverPanel; 

    void Start()
    {
        Time.timeScale = 1.0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Enforce initial visibility layout
        if (thiefIntroModel != null) thiefIntroModel.SetActive(true);
        if (signallerIntroModel != null) signallerIntroModel.SetActive(true);
        
        if (thiefGameplayModel != null) thiefGameplayModel.SetActive(false);
        if (signallerGameplayModel != null) signallerGameplayModel.SetActive(false);
        
        // ---> NEW LINE: Turn off the Ostrich at the very start of the game <---
        if (ostrichGameplayModel != null) ostrichGameplayModel.SetActive(false);

        if (blackoutImage != null)
        {
            blackoutImage.gameObject.SetActive(true);
            blackoutImage.color = new Color(0f, 0f, 0f, 0f); 
        }

        StartCoroutine(PlayIntroAndSwapModels());
    }

    IEnumerator PlayIntroAndSwapModels()
    {
        yield return null; 

        Animator thiefIntroAnim = thiefIntroModel != null ? thiefIntroModel.GetComponent<Animator>() : null;
        Animator signallerIntroAnim = signallerIntroModel != null ? signallerIntroModel.GetComponent<Animator>() : null;

        if (thiefIntroAnim != null) thiefIntroAnim.SetTrigger("Walk");
        if (signallerIntroAnim != null) signallerIntroAnim.SetTrigger("Walk");

        yield return new WaitForSeconds(Mathf.Max(0.1f, cutsceneDuration - fadeDuration));

        // PHASE 1: FADE TO BLACK
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            if (blackoutImage != null) blackoutImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        // PHASE 2: SYSTEM SWAP (Happens in complete darkness)
        Debug.Log("Level Manager: Performing model swap including Ostrich...");
        
        // Hide old cutscene actors
        if (thiefIntroModel != null) thiefIntroModel.SetActive(false);
        if (signallerIntroModel != null) signallerIntroModel.SetActive(false);

        // Turn on real gameplay actors
        if (thiefGameplayModel != null) thiefGameplayModel.SetActive(true);
        if (signallerGameplayModel != null) signallerGameplayModel.SetActive(true);
        
        // ---> NEW LINE: Wake up the Ostrich right now in the dark! <---
        if (ostrichGameplayModel != null) ostrichGameplayModel.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // PHASE 3: FADE BACK INTO GAMEPLAY
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(1.0f - (timer / fadeDuration));
            if (blackoutImage != null) blackoutImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
    }

    public void TriggerGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (blackoutImage != null) blackoutImage.color = new Color(0.4f, 0f, 0f, 0.75f); 
        Time.timeScale = 0f; 
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}