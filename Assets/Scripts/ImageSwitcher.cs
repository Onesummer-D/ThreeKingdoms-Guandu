using UnityEngine;
using UnityEngine.UI;

public class ImageSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class NamedImage
    {
        public string name;
        public Sprite sprite;
    }

    [SerializeField] private Image targetImage;
    [SerializeField] private NamedImage[] images;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    public void SwitchTo(string imageName)
    {
        foreach (var img in images)
        {
            if (img.name == imageName)
            {
                targetImage.sprite = img.sprite;
                targetImage.gameObject.SetActive(true);
                return;
            }
        }
        Debug.LogWarning($"Image '{imageName}' not found in ImageSwitcher");
    }

    public void Hide()
    {
        targetImage.gameObject.SetActive(false);
    }
}