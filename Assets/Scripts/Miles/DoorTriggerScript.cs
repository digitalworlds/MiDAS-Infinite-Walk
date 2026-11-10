using UnityEngine;

public class DoorTriggerScript : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        transform.parent.gameObject.GetComponent<DoorScript>().OpenDoor();
    }
}
