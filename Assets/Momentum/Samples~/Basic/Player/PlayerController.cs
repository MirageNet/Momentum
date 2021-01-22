using UnityEngine;
using UnityEngine.InputSystem;

namespace Mirror.Examples.Basic
{
    public class PlayerController : MonoBehaviour
    {
        public Rigidbody rigidBody;

        public float moveForce = 3;
        public float topSpeed = 10;

        private Vector3 force;

        public void FixedUpdate()
        {
            if (rigidBody.velocity.magnitude < topSpeed)
                rigidBody.AddForce(force);
        }

        public void Move(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();

            Vector3 up = Camera.main.transform.TransformDirection(Vector3.up);

            up.y = 0;
            up = up.normalized;

            Vector3 right = Camera.main.transform.TransformDirection(Vector3.right);
            right.y = 0;
            right = right.normalized;

            Vector3 direction = right * input.x + up * input.y;

            force = direction * moveForce;   
        }
    }
}
