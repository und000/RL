using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class DisplayOnlyUI
{
    public static void Configure(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        Navigation navigation = slider.navigation;
        navigation.mode = Navigation.Mode.None;
        slider.navigation = navigation;
        slider.interactable = false;

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == slider.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
