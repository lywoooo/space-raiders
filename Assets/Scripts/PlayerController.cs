using UnityEngine;

public class PlayerShipInput : MonoBehaviour
{
    [SerializeField] private ShipMotor motor;

    [Header("Input")]
    [SerializeField] private float pitchSensitivity = 1.5f;
    [SerializeField] private float yawSensitivity = 0.35f;

    [Header("Keys")]
    [SerializeField] private KeyCode boostKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode rollLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rollRightKey = KeyCode.E;

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<ShipMotor>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float thrust = Input.GetAxisRaw("Vertical");      // W / S
        float keyboardYaw = Input.GetAxisRaw("Horizontal"); // A / D

        // Mouse Y handles pitch
        float pitch = -Input.GetAxis("Mouse Y") * pitchSensitivity;

        // Small mouse-x assist on yaw so keyboard turning feels less stiff
        float mouseYaw = Input.GetAxis("Mouse X") * yawSensitivity;

        float yaw = Mathf.Clamp(keyboardYaw + mouseYaw, -1f, 1f);

        float manualRoll = 0f;
        if (Input.GetKey(rollLeftKey)) manualRoll += 1f;
        if (Input.GetKey(rollRightKey)) manualRoll -= 1f;

        bool boost = Input.GetKey(boostKey);

        motor.SetInputs(thrust, yaw, pitch, manualRoll, boost);
    }
}