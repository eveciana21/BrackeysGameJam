using UnityEngine;
using UnityEngine.UI;

public class HealthIndicator : MonoBehaviour
{
    [SerializeField] private GameObject indicatorSprite;

    private RectTransform rectTransform;

    public void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetHealth(int health)
    {
        if (transform.childCount > health)
            for (int i = transform.childCount - 1; i >= health; i--)
                Destroy(transform.GetChild(i).gameObject);
        else
            for (int i = transform.childCount; i < health; i++)
            {
                GameObject indicator = Instantiate(indicatorSprite);
                indicator.transform.SetParent(rectTransform, false);
            }

        if (rectTransform != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        
    }
}