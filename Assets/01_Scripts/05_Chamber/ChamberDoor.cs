using UnityEngine;

public class ChamberDoor : MonoBehaviour
{
    [Header("Settings")]
    public bool isLocked = true;

    [Header("References")]
    public Animator anim;

    [Tooltip("문 너머 Transition Room — 잠겨있을 때 비활성화")]
    public GameObject transitionRoom;

    // ── 문 열기 ──────────────────────────────────────────────────

    public void Unlock()
    {
        isLocked = false;

        if (anim != null)
            anim.SetTrigger("Open");

        // 문 열릴 때 Transition Room 활성화
        if (transitionRoom != null)
            transitionRoom.SetActive(true);

        Debug.Log("Door unlocked.");
    }
}