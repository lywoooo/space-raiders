using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fracture : MonoBehaviour
{
    [Tooltip("\"Fractured\" is the object that this will break into")]
    public GameObject fractured;

    [Header("Destruction Settings")]
    [SerializeField] private float destructionDelay = 10f;

    private bool isDestroying = false;

    public void FractureObject()
    {
        if (isDestroying)
            return;

        isDestroying = true;
        
        Instantiate(fractured, transform.position, transform.rotation); //Spawn in the broken version
        
        // Wait 10 seconds before destroying the object
        StartCoroutine(DestroyAfterDelay(destructionDelay));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
