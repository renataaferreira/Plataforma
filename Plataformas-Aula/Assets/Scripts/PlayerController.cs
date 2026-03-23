using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerController for a roll-a-ball style game.
/// Designed to be used with the new Input System's PlayerInput component
/// configured to "Invoke Unity Events". Bind the `Player/Move` action to
/// the public `OnMove(Vector2)` method in the inspector.
///
/// Defaults to camera-relative movement and uses AddForce for translational
/// movement. Toggle `useTorque` to apply torque instead for more realistic
/// rolling (may require tuning).
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Rigidbody on the player (ball). If null, will try to GetComponent<Rigidbody> on Awake.")]
    public Rigidbody rb;

    [Tooltip("Camera used to make movement camera-relative. If null, Camera.main will be used.")]
    public Camera targetCamera;

    [Header("Movement")]
    [Tooltip("Movement strength applied. Interpreted as acceleration when using AddForce.")]
    public float speed = 10f;

    [Tooltip("If true, apply torque to make the ball roll instead of direct AddForce.")]
    public bool useTorque = false;

    [Tooltip("Maximum horizontal speed (meters/second). Velocity is clamped on the XZ plane.")]
    public float maxSpeed = 8f;

    // Internal state populated by the PlayerInput UnityEvent (OnMove)
    Vector2 moveInput = Vector2.zero;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    /// <summary>
    /// Called from PlayerInput (Invoke Unity Events) for the Move action.
    /// Signature with Vector2 is convenient to wire in the inspector.
    /// </summary>
    /// <param name="value">Vector2 input (x = horizontal, y = vertical)</param>
    public void OnMove(Vector2 value)
    {
        moveInput = value;
    }

    /// <summary>
    /// Alternative binding signature if you prefer to connect an InputValue.
    /// PlayerInput UnityEvent can also call this if you choose InputValue.
    /// </summary>
    /// <param name="value"></param>
    public void OnMoveInput(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        Vector3 direction = GetMoveDirection(moveInput);

        if (direction.sqrMagnitude < 1e-6f)
            return;

        if (useTorque)
        {
            // Apply torque around an axis perpendicular to desired travel direction.
            // This is a simple mapping that creates rotation consistent with movement.
            Vector3 torqueAxis = Vector3.Cross(Vector3.up, direction.normalized);
            // Scale torque by speed; may need tuning for realistic roll depending on mass/angular drag
            rb.AddTorque(torqueAxis * speed, ForceMode.Acceleration);
        }
        else
        {
            // Apply translational acceleration in the desired direction
            rb.AddForce(direction.normalized * speed, ForceMode.Acceleration);

            // Clamp horizontal speed to maxSpeed to keep control predictable
            Vector3 vel = rb.linearVelocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            float hMag = horizontal.magnitude;
            if (hMag > maxSpeed)
            {
                Vector3 clamped = horizontal.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(clamped.x, vel.y, clamped.z);
            }
        }
    }

    /// <summary>
    /// Converts a 2D input vector to a 3D world direction.
    /// If a camera is available this returns a camera-relative direction on the XZ plane,
    /// otherwise it maps X -> world X and Y -> world Z.
    /// </summary>
    /// <param name="input">Input vector (x,horizontal / y,vertical)</param>
    /// <returns>World-space direction vector (not normalized)</returns>
    Vector3 GetMoveDirection(Vector2 input)
    {
        if (targetCamera != null)
        {
            Vector3 camForward = targetCamera.transform.forward;
            Vector3 camRight = targetCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
            return camForward * input.y + camRight * input.x;
        }
        else
        {
            // World-relative fallback: X = input.x, Z = input.y
            return new Vector3(input.x, 0f, input.y);
        }
    }

    void OnDisable()
    {
        moveInput = Vector2.zero;
    }
}

