using System;
using UnityEngine;


[Serializable]
public enum PlayerForm
{
    Humanoid,
    Soul
}
public class PlayerFormController : MonoBehaviour
{
    [Header("모델")]
    public GameObject humanoidForm;
    public GameObject soulForm;

    [Header("콜라이더")]
    public CapsuleCollider humanoidCollider;
    public SphereCollider soulCollider;

    public PlayerForm currentForm { get; private set; } = PlayerForm.Soul;

    private InputSystem_Actions _controls;

    void Awake() => _controls = new InputSystem_Actions();
    void OnEnable() => _controls.Enable();
    void OnDisable() => _controls.Disable();
    public SoulCore playerData;

    private void Start()
    {
        playerData = GetComponent<SoulCore>();
    }
    private void Update()
    {
        if (_controls.Player.FormChange.triggered)
        {
            FormChange();
            playerData.currentForm = this.currentForm;
        }
    }
    void FormChange()
    {
        if(currentForm == PlayerForm.Humanoid)
        {
            currentForm = PlayerForm.Soul;
            humanoidForm.SetActive(false);
            soulForm.SetActive(true);
            humanoidCollider.enabled = false;
            soulCollider.enabled = true;
        }
        else
        {
            currentForm = PlayerForm.Humanoid;
            humanoidForm.SetActive(true);
            soulForm.SetActive(false);
            humanoidCollider.enabled = true;
            soulCollider.enabled = false;
        }
    }
}
