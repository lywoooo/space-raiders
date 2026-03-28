using UnityEngine;

public class ShipMotor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Translation")]
    [SerializeField] private float thrustAcceleration = 30f;
    [SerializeField] private float boostMultiplier = 1.75f;
    [SerializeField] private float maxForwardSpeed = 35f;
    [SerializeField] private float maxReverseSpeed = 12f;

    [Header("Rotation")]
    [SerializeField] private float yawAcceleration = 8f;
    [SerializeField] private float pitchAcceleration = 7f;
    [SerializeField] private float rollAcceleration = 9f;
    [SerializeField] private float maxAngularSpeed = 2.5f;

    [Header("Assist")]
    [SerializeField] private float linearDrag = 1.2f;
    [SerializeField] private float angularDrag = 2.5f;
    [SerializeField] private float lateralDamping = 3.5f;
    [SerializeField] private float verticalDamping = 3f;
    [SerializeField] private float autoBankStrength = 0.45f;
    [SerializeField] private float autoLevelStrength = 1.2f;

    private float thrustInput;
    private float yawInput;
    private float pitchInput;
    private float manualRollInput;
    private bool boosting;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.drag = linearDrag;
        rb.angularDrag = angularDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void SetInputs(float thrust, float yaw, float pitch, float manualRoll, bool boost)
    {
        thrustInput = Mathf.Clamp(thrust, -1f, 1f);
        yawInput = Mathf.Clamp(yaw, -1f, 1f);
        pitchInput = Mathf.Clamp(pitch, -1f, 1f);
        manualRollInput = Mathf.Clamp(manualRoll, -1f, 1f);
        boosting = boost;
    }

    private void FixedUpdate()
    {
        ApplyTranslation();
        ApplyRotation();
        ApplyAssist();
        ClampVelocities();
    }

    private void ApplyTranslation()
    {
        float accel = thrustAcceleration * (boosting ? boostMultiplier : 1f);

        rb.AddForce(transform.forward * thrustInput * accel, ForceMode.Acceleration);
    }

    private void ApplyRotation()
    {
        // A/D mainly controls yaw.
        // A little bit of automatic roll/bank is added from yaw.
        float autoBankRoll = -yawInput * autoBankStrength;

        // If the player is manually rolling with Q/E, let that take priority.
        float finalRollInput = Mathf.Abs(manualRollInput) > 0.01f ? manualRollInput : autoBankRoll;

        Vector3 torque =
            transform.up * yawInput * yawAcceleration +
            transform.right * pitchInput * pitchAcceleration +
            -transform.forward * finalRollInput * rollAcceleration;

        rb.AddTorque(torque, ForceMode.Acceleration);

        // Gradually level the ship if the player is not manually rolling.
        if (Mathf.Abs(manualRollInput) < 0.01f)
        {
            Vector3 shipUp = transform.up;
            float rollError = Vector3.Dot(shipUp, Vector3.right);
            rb.AddTorque(-transform.forward * rollError * autoLevelStrength, ForceMode.Acceleration);
        }
    }

    private void ApplyAssist()
    {
        // Convert world velocity into local ship space
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        // Keep forward motion strong, reduce ugly side/up drift
        localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, lateralDamping * Time.fixedDeltaTime);
        localVelocity.y = Mathf.Lerp(localVelocity.y, 0f, verticalDamping * Time.fixedDeltaTime);

        // Clamp local forward speed separately
        localVelocity.z = Mathf.Clamp(localVelocity.z, -maxReverseSpeed, maxForwardSpeed);

        rb.velocity = transform.TransformDirection(localVelocity);
    }

    private void ClampVelocities()
    {
        if (rb.angularVelocity.magnitude > maxAngularSpeed)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularSpeed;
        }
    }
}