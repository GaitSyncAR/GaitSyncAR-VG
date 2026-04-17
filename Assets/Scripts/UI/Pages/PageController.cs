using UnityEngine;
using UnityEngine.UIElements;

// Every controller inherits from this. Handles common boilerplate.
public abstract class PageController : PageInterface
{
    protected UIDocument    Document  { get; private set; }
    protected VisualElement Root     { get; private set; }
    protected bool         IsActive { get; private set; }

    public virtual void Initialize(UIDocument doc)
    {
        Document = doc;
        Root     = doc.rootVisualElement;
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

    protected void PlayHaptic()
    {
        #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
        #endif
    }
}
