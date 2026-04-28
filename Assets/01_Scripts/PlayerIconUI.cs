using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerIconUI : MonoBehaviour
{
    public Image icon;
    public Image iconBack;
    public TextMeshProUGUI percentText;
    public Sprite humanoid;
    public Sprite soul;

    public PlayerFormController pfc;
    private void Update()
    {
        if(pfc.currentForm == PlayerForm.Soul)
        {
            icon.sprite = soul;
            iconBack.sprite = soul;
            icon.fillAmount = 1.0f;
            percentText.text = "Soul";
        }
        else
        {
            icon.sprite = humanoid;
            iconBack.sprite = humanoid;
            icon.fillAmount = pfc.sizeIndex/ (float)500.0f;
            percentText.text = Mathf.RoundToInt((pfc.sizeIndex / 500.0f) * 100).ToString() + "%";
        }
    }
}