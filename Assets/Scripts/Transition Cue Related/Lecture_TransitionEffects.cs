using UnityEngine;

public class Lecture_TransitionEffects : MonoBehaviour
{
    [SerializeField] private string sceneName = "Lecture";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        /*yield return StartCoroutine(TransitionEffects.Instance.LoadSceneOneByOne(
            roomTitle: sceneName,
            DelayBetweenObjects: 0.5f,
            titleHoldSeconds: 1.0f,
            onOverlayReady: go => overlay = go
        ));*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
