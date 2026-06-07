using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class HunterAgent : Agent
{
    [Header("Target Settings")]
    public Transform playerTransform;
    public float moveSpeed = 7f; // Slightly faster than player for challenge
    
    private Rigidbody rb;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        // Ensure the bot doesn't tip over
        rb.freezeRotation = true;
    }
    /*
    public override void OnEpisodeBegin()
    {
        // Note: MapGenerator handles initial spawning.
        // If the bot is caught or resets, we reset its velocity.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    */
    public override void OnEpisodeBegin()
    {
        // 1. Reset physical movement so we don't fly off
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2. Find a safe place to spawn the HunterBot
        // We try 100 times to find a spot that ISN'T touching an "Obstacles" wall.
        bool positionFound = false;
        int safetyCounter = 0;

        while (!positionFound && safetyCounter < 100)
        {
            safetyCounter++;
            // Pick a random spot on your 100x100 map (adjusted for offset)
            // Assuming map is centered at 0,0 and roughly 100x100 size
            Vector3 randomPos = new Vector3(Random.Range(-40f, 40f), 0.5f, Random.Range(-40f, 40f));

            // Check a 1-meter radius sphere around that point for walls
            if (!Physics.CheckSphere(randomPos, 1f, LayerMask.GetMask("Obstacles")))
            {
                transform.localPosition = randomPos;
                positionFound = true;
            }
        }

        // 3. (Optional) If you have a Target, move it too!
        // If you don't move the target, the bot just memorizes the location.
        // Transform target = transform.parent.Find("Target"); // Example way to find it
        // if (target != null) { ... repeat safe spawn logic for target ... }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Bot Position (3 floats)
        sensor.AddObservation(transform.localPosition);
        
        // 2. Player Position (3 floats)
        if (playerTransform != null)
            sensor.AddObservation(playerTransform.localPosition);
        else
            sensor.AddObservation(Vector3.zero);

        // 3. Bot Velocity (3 floats)
        sensor.AddObservation(rb.linearVelocity);
        
        // 4. Distance to Player (1 float)
        if (playerTransform != null)
            sensor.AddObservation(Vector3.Distance(transform.position, playerTransform.position));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // --- MOVEMENT ---
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        Vector3 moveForce = new Vector3(moveX, 0, moveZ) * moveSpeed;
        rb.linearVelocity = new Vector3(moveForce.x, rb.linearVelocity.y, moveForce.z);

        // --- REWARDS ---
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // 1. Proximity Reward: Encourages the bot to reduce the gap.
            // This is the 'Extrinsic' signal that guides them through the maze corridors.
            AddReward(0.001f / (distanceToPlayer + 1.0f));
        }

        // 2. Time Penalty: Prevents the bot from 'camping' or doing nothing.
        AddReward(-0.0005f);
    }

    // --- DETECTION LOGIC ---
    private void OnCollisionEnter(Collision collision)
    {
        // Success: Bot touched the Player
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Target Captured!");
            SetReward(2.0f); // High reward for completion
            EndEpisode();
        }
        
        // Penalty: Bot hit a wall (Obstacle)
        // Helps them learn to navigate corridors without rubbing against walls
        if (collision.gameObject.CompareTag("Obstacles"))
        {
            AddReward(-0.01f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }
}