using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// parry: press the parry key just before an enemy's hit lands.
// if the hit lands inside the short window, you take no damage, the enemy staggers,
// and you get a little light back (defending isn't aggression, so the boss isn't fed).
// press too early and the window closes, then there's a short wait before you can try again.
// the boss's slam and lunge can't be parried, only regular enemies.
public class PlayerParry : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private PlayerStats stats;

    [Header("Timing")]
    [Tooltip("How long the parry window stays open after you press (seconds). Tight = harder.")]
    [SerializeField] private float window = 0.18f;
    [Tooltip("Wait after a missed parry before you can try again.")]
    [SerializeField] private float missCooldown = 0.5f;
    [Tooltip("Wait after a good parry (short, so you can chain them).")]
    [SerializeField] private float successCooldown = 0.1f;

    [Header("Reward")]
    [SerializeField] private float lightGain = 3f;
    [Tooltip("Tiny freeze on a good parry so it feels strong.")]
    [SerializeField] private float hitStop = 0.08f;

    [Header("Feedback (optional)")]
    [Tooltip("Plays when you press parry (the shield flash).")]
    [SerializeField] private SpriteFX windowPrefab;
    [Tooltip("Plays on a good parry (the ring burst).")]
    [SerializeField] private SpriteFX successPrefab;
    [SerializeField] private Vector2 effectOffset = new Vector2(0f, 0.45f);
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip parryClip;
    [SerializeField] private AudioClip successClip;

    public bool IsParrying => windowTimer > 0f;
    public event Action OnParrySuccess;

    private InputAction parryAction;
    private bool createdAction;
    private float windowTimer;
    private float cooldownTimer;
    private bool succeededThisWindow;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (inputActions != null)
            parryAction = inputActions.FindAction("Player/Parry", throwIfNotFound: false);
        if (parryAction == null)
        {
            // no parry action in the asset yet, so use q, right mouse and the gamepad's right shoulder
            parryAction = new InputAction("Parry", InputActionType.Button);
            parryAction.AddBinding("<Keyboard>/q");
            parryAction.AddBinding("<Mouse>/rightButton");
            parryAction.AddBinding("<Gamepad>/rightShoulder");
            createdAction = true;
        }
    }

    private void OnEnable() => parryAction?.Enable();
    private void OnDisable() => parryAction?.Disable();
    private void OnDestroy() { if (createdAction) parryAction?.Dispose(); }

    private void Update()
    {
        if (PauseMenu.IsPaused || OutcomeScreen.IsShowing || BlessingChoiceUI.IsShowing) return;

        if (windowTimer > 0f)
        {
            windowTimer -= Time.deltaTime;
            if (windowTimer <= 0f)
                cooldownTimer = succeededThisWindow ? successCooldown : missCooldown;
        }
        else if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (parryAction.WasPressedThisFrame() && windowTimer <= 0f && cooldownTimer <= 0f)
        {
            windowTimer = window + Blessings.ParryWindowBonus;
            succeededThisWindow = false;
            Spawn(windowPrefab);
            Play(parryClip);
        }
    }

    // enemies call this right before they hurt the player. true = the hit was parried.
    public bool TryParry()
    {
        if (!IsParrying) return false;
        if (succeededThisWindow) return true; // same window, already rewarded

        succeededThisWindow = true;
        Spawn(successPrefab);
        Play(successClip);
        if (stats != null && lightGain > 0f) stats.ChangeStateMeter(lightGain);
        if (hitStop > 0f) StartCoroutine(HitStop());
        OnParrySuccess?.Invoke();
        return true;
    }

    private IEnumerator HitStop()
    {
        if (PauseMenu.IsPaused) yield break;
        float old = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStop);
        if (!PauseMenu.IsPaused && !BlessingChoiceUI.IsShowing) Time.timeScale = old;
    }

    private void Spawn(SpriteFX prefab)
    {
        if (prefab == null) return;
        SpriteFX fx = Instantiate(prefab, (Vector2)transform.position + effectOffset, Quaternion.identity);
        fx.transform.SetParent(transform, true); // stays with the player
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
