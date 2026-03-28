using UnityEngine;
using UnityEngine.SceneManagement;

public class NPCController : MonoBehaviour
{
    [SerializeField] private ShipMotor motor;

    [Header("Target")]
    [SerializeField] private Transform playerShip;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float arrivalRadius = 5f;

    [Header("Steering")]
    [SerializeField] private float pitchSensitivity = 2f;
    [SerializeField] private float yawSensitivity = 2f;
    [SerializeField] private float rollSensitivity = 0.6f;
    [SerializeField] private float throttleAngle = 30f;

    [Header("Ranges")]
    [SerializeField] private float chaseRange = 80f;
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float killRange = 2f;

    [Header("Throttle")]
    [SerializeField] private float patrolThrust = 1f;
    [SerializeField] private float chaseThrust = 1f;
    [SerializeField] private float attackThrust = 0.35f;
    [SerializeField] private bool useBoostInChase = false;

    private float pitch;
    private float yaw;
    private float roll;
    private float thrust;
    private bool boost;

    private enum NPCState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Kill
    }

    private NPCState state = NPCState.Chase;
    private int waypointIndex = 0;

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<ShipMotor>();
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        playerShip = playerObj.transform;
    }

    private void Update()
    {
        UpdateState();
        UpdateSteering();
        checkKill();
    }

    private void FixedUpdate()
    {
        if (motor != null)
        {
            motor.SetInputs(thrust, yaw, pitch, roll, boost);
        }
    }

    private void UpdateState()
    {
        if (playerShip == null)
        {
            state = waypoints != null && waypoints.Length > 0 ? NPCState.Patrol : NPCState.Idle;
            return;
        }

        float distance = Vector3.Distance(transform.position, playerShip.position);

        if (distance <= killRange){
            state = NPCState.Kill;
        }
        else if (distance <= attackRange)
        {
            state = NPCState.Attack;
        }
        else if (distance <= chaseRange)
        {
            state = NPCState.Chase;
        }
        else if (waypoints != null && waypoints.Length > 0)
        {
            state = NPCState.Patrol;
        }
        else
        {
            state = NPCState.Idle;
        }
    }

    private void UpdateSteering()
    {
        Transform target = GetCurrentTarget();

        if (target == null || state == NPCState.Idle)
        {
            thrust = 0f;
            pitch = 0f;
            yaw = 0f;
            roll = 0f;
            boost = false;
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance < 0.001f)
        {
            thrust = 0f;
            pitch = 0f;
            yaw = 0f;
            roll = 0f;
            boost = false;
            return;
        }

        Vector3 localToTarget = transform.InverseTransformDirection(toTarget.normalized);
        float angleOff = Vector3.Angle(transform.forward, toTarget);

        pitch = Mathf.Clamp(-localToTarget.y * pitchSensitivity, -1f, 1f);
        yaw = Mathf.Clamp(localToTarget.x * yawSensitivity, -1f, 1f);

        // Bank a little into the turn
        roll = Mathf.Clamp(-localToTarget.x * rollSensitivity, -1f, 1f);

        switch (state)
        {
            case NPCState.Patrol:
                thrust = (distance > arrivalRadius && angleOff < throttleAngle) ? patrolThrust : 0f;
                boost = false;

                if (distance < arrivalRadius && waypoints.Length > 0)
                {
                    waypointIndex = (waypointIndex + 1) % waypoints.Length;
                }
                break;

            case NPCState.Chase:
                thrust = angleOff < throttleAngle ? chaseThrust : 0.25f;
                boost = useBoostInChase && angleOff < 15f;
                break;

            case NPCState.Attack:
                // Slow down a bit when close so it doesn't overshoot too hard
                thrust = angleOff < throttleAngle ? attackThrust : 0f;
                boost = false;
                break;

            default:
                thrust = 0f;
                boost = false;
                break;
        }
    }

    private Transform GetCurrentTarget()
    {
        switch (state)
        {
            case NPCState.Chase:
            case NPCState.Attack:
                return playerShip;

            case NPCState.Patrol:
                return (waypoints != null && waypoints.Length > 0) ? waypoints[waypointIndex] : null;

            default:
                return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Asteroid"))
        {
            Destroy(gameObject);
        }
    }
    private void checkKill(){
        if(state == NPCState.Kill){
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("endGame");
        }
    }
}
