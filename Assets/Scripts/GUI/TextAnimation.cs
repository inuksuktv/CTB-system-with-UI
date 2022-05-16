using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextAnimation : MonoBehaviour
{
    private float animationSpeed = 40f;
    private RectTransform myRT;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 2f);
        myRT = GetComponent<RectTransform>();
        Vector3 myPosition = myRT.localPosition;
        myPosition += 64 * Vector3.up;
        myRT.localPosition = myPosition;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        Vector3 myPosition = myRT.localPosition;
        myPosition += animationSpeed * Time.deltaTime * Vector3.up;
        myRT.localPosition = myPosition;
    }
}
