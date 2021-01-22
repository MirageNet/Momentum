using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxController : MonoBehaviour
{

    public Rigidbody rigidBody;

    public Material stuckMaterial;

    private Transform stuckTo;
    private Vector3 stuckPosition;
    private Quaternion stuckRotation;
    public Collider myCollider;

    public void FixedUpdate()
    {
        if (stuckTo == null)
            return;

        // the object is stucked to another object,  move with it.

        transform.position = stuckTo.transform.TransformPoint(stuckPosition);
        transform.rotation = stuckTo.transform.rotation * stuckRotation ;
    }


    //When the Primitive collides with the walls, it will reverse direction
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            rigidBody.isKinematic = true;

            GetComponent<Renderer>().sharedMaterial = stuckMaterial;
            stuckTo = other.transform;
            stuckPosition = other.transform.InverseTransformPoint(transform.position);
            stuckRotation = Quaternion.Inverse(other.transform.rotation) * transform.rotation;

            myCollider.enabled = false;
        }
    }

}
