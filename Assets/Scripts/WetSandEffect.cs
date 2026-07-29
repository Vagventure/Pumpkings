using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WetSandEffect : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("Which SpawnTrigger value should cause this wet sand patch to appear.")]
    [SerializeField] private SpawnTrigger triggerOn = SpawnTrigger.WaveSpawnTrigger;

    [Header("Timing")]
    [Tooltip("How long the wet sand stays fully visible before it starts fading.")]
    [SerializeField] private float holdDuration = 1.5f;
    [Tooltip("How long the fade-out from full alpha to zero takes.")]
    [SerializeField] private float fadeDuration = 2f;

    private Image image;
    private Coroutine activeRoutine;

    private void Awake()
    {
        image = GetComponent<Image>();
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        SpawnTriggerEvents.Triggered += HandleTriggered;
    }

    private void OnDisable()
    {
        SpawnTriggerEvents.Triggered -= HandleTriggered;
    }

    private void HandleTriggered(SpawnTriggerContext context)
    {
        if (context.Trigger != triggerOn)
        {
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(ShowThenFade());
    }

    private IEnumerator ShowThenFade()
    {
        SetAlpha(1f);

        yield return new WaitForSeconds(holdDuration);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(1f, 0f, elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        activeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }
}