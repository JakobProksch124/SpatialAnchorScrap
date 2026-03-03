using UnityEngine;
using TMPro;

public class Tutorial_TransitionCue : MonoBehaviour
{
    [SerializeField]private TMP_Text buttonCountText;
    float pressCount = 0;

    public void IncreaseCount()
    {
        //
        this.pressCount += 1;
        buttonCountText.text=pressCount.ToString();
    }
}
