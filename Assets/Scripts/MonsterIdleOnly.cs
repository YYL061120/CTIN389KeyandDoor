using UnityEngine;

/// <summary>Keeps the imported monster stationary in one looping idle state.</summary>
public class MonsterIdleOnly : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "anim_idle_1";

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void Start() => PlayIdle();

    private void LateUpdate()
    {
        if (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName(idleStateName))
            PlayIdle();
    }

    private void PlayIdle()
    {
        if (animator != null && !string.IsNullOrEmpty(idleStateName))
            animator.Play(idleStateName, 0, 0f);
    }
}
