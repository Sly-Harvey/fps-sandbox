using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;
    public float wallRunSpeed;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private Vector3 startPos;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;

    public float groundDrag;

    public Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump = true;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask groundMask;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Step Handling")]
    public GameObject _stepRayUpper;
    public GameObject _stepRayLower;
    public float stepHeight = 0.3f;
    public float stepSmooth = 0.1f;
    public float rayLowerLength = 0.3f;
    public float rayHigherLength = 0.2f;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        sliding,
        wallRunning,
        air
    }

    public bool wallRunning;
    public bool crouching;
    public bool sliding;

    private void Start()
    {
        startPos = transform.position;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        moveDirection = moveDirection.normalized;

        startYScale = transform.localScale.y;

        _stepRayUpper.transform.localPosition = new Vector3(_stepRayUpper.transform.position.x, stepHeight, _stepRayUpper.transform.position.z);
    }

    private void FixedUpdate()
    {
        movePlayer();
        stepClimb();
    }

    private void Update()
    {
        //if (transform.position.y < 2)
            //transform.position = startPos;

        _input();
        speedControl();
        stateHandler();

        Physics.gravity = Gravity;

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundMask);
        if (grounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
    }

    void _input()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            jump();

            Invoke("resetJump", jumpCooldown);
        }

        if(Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        if(Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    void stateHandler()
    {
        //wallrunning
        if(wallRunning)
        {
            state = MovementState.wallRunning;
            desiredMoveSpeed = wallRunSpeed;
        }

        //sliding
        if(sliding)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.velocity.y < 0.1f)
                desiredMoveSpeed = slideSpeed;
            else
                desiredMoveSpeed = sprintSpeed;
        }

        //crouching
        else if(Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
        }

        //sprinting
        else if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }
        //walking
        else if(grounded)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }
        //air
        else
        {
            state = MovementState.air;
        }

        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(lerpMoveSpeed());
        }
        else
        {
            moveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private IEnumerator lerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    void movePlayer()
    {
        moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        if(OnSlope())
        {
            rb.AddForce(getSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            if(rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        else if(grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else if(!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        rb.useGravity = !OnSlope();
    }

    void speedControl()
    {
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    void jump()
    {
        exitingSlope = true;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    void resetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    public bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 getSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    void stepClimb()
    {
        if (rb.velocity.magnitude > 0)
        {
            // forward hit angle
            RaycastHit hitLower;
            if (Physics.Raycast(_stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLower, rayLowerLength))
            {
                float lowerNormalAngle = Vector3.Angle(transform.up, hitLower.normal);
                RaycastHit hitUpper;
                if (!Physics.Raycast(_stepRayUpper.transform.position, transform.TransformDirection(Vector3.forward), out hitUpper, rayHigherLength) && lowerNormalAngle >= 90f)
                {
                    rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
                }
            }

            // 45 degree angle
            RaycastHit hitLower45;
            if (Physics.Raycast(_stepRayLower.transform.position, transform.TransformDirection(1.5f, 0f, 1), out hitLower45, rayLowerLength))
            {
                float lowerNormalAngle45 = Vector3.Angle(transform.up, hitLower45.normal);
                RaycastHit hitUpper45;
                if (!Physics.Raycast(_stepRayUpper.transform.position, transform.TransformDirection(1.5f, 0f, 1), out hitUpper45, rayHigherLength) && lowerNormalAngle45 >= 90f)
                {
                    rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
                }
            }

            // minus 45 degree angle
            RaycastHit hitLowerMinus45;
            if (Physics.Raycast(_stepRayLower.transform.position, transform.TransformDirection(-1.5f, 0f, 1), out hitLowerMinus45, rayLowerLength))
            {
                float lowerNormalAngleMinus45 = Vector3.Angle(transform.up, hitLowerMinus45.normal);
                RaycastHit hitUpperMinus45;
                if (!Physics.Raycast(_stepRayUpper.transform.position, transform.TransformDirection(-1.5f, 0f, 1), out hitUpperMinus45, rayHigherLength) && lowerNormalAngleMinus45 >= 90f)
                {
                    rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
                }
            }
        }
    }
}
