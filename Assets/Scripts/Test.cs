using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    [SerializeField] private Button button;

    private void OnEnable()
    {
        if (button == null)
        {
            Debug.LogWarning("Button reference is not assigned in the Inspector.", this);
            return;
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ClickChoice);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(ClickChoice);
    }

    public void ClickChoice()
    {
        Debug.Log("Button clicked!");
    }
}
