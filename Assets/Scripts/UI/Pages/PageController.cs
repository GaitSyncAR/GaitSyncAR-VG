using UnityEngine;
using UnityEngine.UIElements;

// Every controller inherits from this. Handles common boilerplate.
public abstract class PageController : PageInterface
{
    protected UIDocument    Document  { get; private set; }
    protected VisualElement Root     { get; private set; }
    protected bool         IsActive { get; private set; }

    public PageController(UIDocument doc, VisualElement pageRoot = null)
    {
        Document = doc;
        Root     = doc.rootVisualElement;

        // adding ourselves to page map for easy access later
        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager != null)        {
            uiManager.RegisterPage(pageRoot, this);
        }
    }

    public virtual void OnPageShow() 
    { 
        IsActive = true; 
    }

    public virtual void OnPageHide() 
    { 
        IsActive = false; 
    }

    protected T Q<T>(string name) where T : VisualElement
        => Root.Q<T>(name);

    public void PlayHaptic()
    {
        #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
        #endif
    }
}
