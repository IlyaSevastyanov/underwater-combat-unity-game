using UnityEngine;

public interface IHarvestUI
{
    void ShowPrompt(FishData fish, GameObject fishObj, FishCatcher whoCalled);
}
