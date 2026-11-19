


using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FiveMinuteClock : MonoBehaviour
{
    [Header("Clock Images")]
    public Image clockImage;

    public Sprite[] clockSprites = new Sprite[5];

    [Header("Settings")]
    public float secondsPerImage = 60f;


    private void Start()
    {

        clockImage.gameObject.SetActive(true);
        if (clockImage == null || clockSprites.Length < 5)
        {
            Debug.LogError("Assign the clock Image");
            return;
        }

        StartClock();
    }

    public void StartClock()
    {

        clockImage.gameObject.SetActive(true);
        clockImage.sprite = clockSprites[0];

        StopAllCoroutines();
        StartCoroutine(RunClock());
    }

    IEnumerator RunClock()
    {
        
        clockImage.gameObject.SetActive(true);
        for (int i = 0; i < 5; i++)
        {
            clockImage.sprite = clockSprites[i];

            yield return new WaitForSeconds(secondsPerImage);
        }

        clockImage.gameObject.SetActive(false);
    }
}
 
 // Testing for pull request 