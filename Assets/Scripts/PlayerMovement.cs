using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float sensitivity = 10f;
    public float speed = 10f;
    public float gravity = 9.81f;
    public float jumpHeight = 3f;
    Transform mainCamera;
    Transform groundCheck;
    float groundDistance = 0.4f;
    LayerMask groundMask;
    float rotationX = 0f;
    CharacterController controller;
    Vector3 gravityVelocity = Vector3.zero;
    bool grounded = true;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main.transform;
        groundCheck = transform.Find("GroundCheck");
        groundMask = LayerMask.GetMask("Ground");
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        grounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (grounded && gravityVelocity.y < 0f)
        {
            gravityVelocity.y = -2f;
        }
        if (Input.GetButtonDown("Jump") && grounded)
        {
            gravityVelocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime,
                                    Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime);

        Vector2 direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector3 move = transform.right * direction.x + transform.forward * direction.y;
        controller.Move(speed * Time.deltaTime * move);

        rotationX -= mouseInput.y;
        Mathf.Clamp(rotationX, -80f, 80f);
        transform.Rotate(Vector3.up, mouseInput.x);
        mainCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        gravityVelocity.y -= gravity * Time.deltaTime;
        controller.Move(gravityVelocity * Time.deltaTime);
    }
}
