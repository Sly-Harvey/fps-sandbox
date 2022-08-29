using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSliding : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;
    private float slideTimer;

    public float slideYScale;
    private float startYScale;

    public KeyCode slideKey = KeyCode.LeftControl;

    private float horizontalInput;
    private float verticalInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();

        startYScale = player.localScale.y;
    }

    private void FixedUpdate()
    {
        if (pm.sliding)
        {
            slidingMovement();
        }
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0))
            startSlide();
        if (Input.GetKeyUp(slideKey) && pm.sliding)
            stopSlide();
    }
    void slidingMovement()
    {
        Vector3 inputDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        if(!pm.OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime;
        }
        else
        {
            rb.AddForce(pm.getSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
        }

        if (slideTimer <= 0)
            stopSlide();
    }

    void startSlide()
    {
        pm.sliding = true;
        player.localScale = new Vector3(player.localScale.x, slideYScale, player.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }

    void stopSlide()
    {
        pm.sliding = false;
        player.localScale = new Vector3(player.localScale.x, startYScale, player.localScale.z);
    }
}
