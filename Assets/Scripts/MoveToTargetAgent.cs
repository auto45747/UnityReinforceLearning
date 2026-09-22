using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class MoveToTargetAgent : Agent
{
    [Header("Scene References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform obstacle;
    [SerializeField] private Transform platform;

    [Header("Movement Dynamics")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 200f;

    [Header("Exploration Grid Settings")]
    [Tooltip("Number of subdivisions along each axis (e.g., 10 creates a 10x10 grid = 100 cells)")]
    [SerializeField] private int gridSize = 10;
    [Tooltip("Reward for discovering a new, unvisited cell")]
    [SerializeField] private float newTileReward = 0.01f;
    private bool[,] visitedTiles;
    private int totalTilesExplored;

    [Header("Spawn Margins")]
    [SerializeField] private float agentEdgeMargin = 1.5f;
    [SerializeField] private float targetEdgeMargin = 1.0f;

    public override void OnEpisodeBegin()
    {
        // 1. Reset exploration tracker
        visitedTiles = new bool[gridSize, gridSize];
        totalTilesExplored = 0;

        // 2. Compute dynamic platform boundaries
        float platformHalfX = platform.localScale.x / 2f;
        float platformHalfZ = platform.localScale.z / 2f;

        float agentRangeX = Mathf.Max(0.5f, platformHalfX - agentEdgeMargin);
        float agentRangeZ = Mathf.Max(0.5f, platformHalfZ - agentEdgeMargin);

        float targetRangeX = Mathf.Max(0.5f, platformHalfX - targetEdgeMargin);
        float targetRangeZ = Mathf.Max(0.5f, platformHalfZ - targetEdgeMargin);

        // 3. Spawn Agent safely
        Vector3 agentPos;
        do
        {
            agentPos = new Vector3(
                Random.Range(-agentRangeX, agentRangeX),
                0.5f,
                Random.Range(-agentRangeZ, agentRangeZ)
            );
        } while (IsOverlappingObstacle(agentPos, 1f));

        transform.localPosition = agentPos;
        transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Mark spawn location as visited
        MarkCurrentTileVisited();

        // 4. Spawn Target safely away from wall and agent
        Vector3 targetPos;
        do
        {
            targetPos = new Vector3(
                Random.Range(-targetRangeX, targetRangeX),
                0.5f,
                Random.Range(-targetRangeZ, targetRangeZ)
            );
        } while (IsOverlappingObstacle(targetPos, 1f) || Vector3.Distance(agentPos, targetPos) < 1.5f);

        target.localPosition = targetPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent local position (3 floats: X, Y, Z)
        sensor.AddObservation(transform.localPosition);

        // Agent horizontal heading direction (2 floats: X, Z)
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        // Total Vector Observation Space Size = 5
        // Target location is intentionally omitted; detected exclusively via Ray Perception Sensor 3D
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // actions[0] = Forward drive only [0, 1]
        // actions[1] = Turn yaw [-1, 1]
        float forwardAmount = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float turnAmount = actions.ContinuousActions[1];

        // Apply Tank-style rotation and translation
        transform.Rotate(0f, turnAmount * turnSpeed * Time.deltaTime, 0f);
        transform.localPosition += transform.forward * forwardAmount * moveSpeed * Time.deltaTime;

        // Tile exploration reward
        MarkCurrentTileVisited();

        // Anti-spin penalty: penalize pure rotational idling without forward progress
        if (forwardAmount < 0.1f && Mathf.Abs(turnAmount) > 0.2f)
        {
            AddReward(-0.0005f);
        }

        // Fatal fall penalty
        if (transform.localPosition.y < 0f)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
    }

    private void MarkCurrentTileVisited()
    {
        if (platform == null || visitedTiles == null) return;

        float halfX = platform.localScale.x / 2f;
        float halfZ = platform.localScale.z / 2f;

        float normX = Mathf.InverseLerp(-halfX, halfX, transform.localPosition.x);
        float normZ = Mathf.InverseLerp(-halfZ, halfZ, transform.localPosition.z);

        int tileX = Mathf.Clamp(Mathf.FloorToInt(normX * gridSize), 0, gridSize - 1);
        int tileZ = Mathf.Clamp(Mathf.FloorToInt(normZ * gridSize), 0, gridSize - 1);

        if (!visitedTiles[tileX, tileZ])
        {
            visitedTiles[tileX, tileZ] = true;
            totalTilesExplored++;
            AddReward(newTileReward);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Lowercase tag match or transform match
        if (other.CompareTag("target") || other.transform == target)
        {
            float baseReward = 1.0f;
            float fraction = (MaxStep > 0) ? ((float)StepCount / MaxStep) : 0f;
            float speedBonus = Mathf.Clamp01(1f - fraction) * 0.5f;

            SetReward(baseReward + speedBonus);
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("obstacle") || collision.transform == obstacle)
        {
            AddReward(-0.05f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        // W/S or Up/Down
        continuousActions[0] = Mathf.Max(0f, Input.GetAxisRaw("Vertical"));
        // A/D or Left/Right
        continuousActions[1] = Input.GetAxisRaw("Horizontal");
    }

    private bool IsOverlappingObstacle(Vector3 candidatePos, float margin)
    {
        if (obstacle == null) return false;

        Vector3 center = obstacle.localPosition;
        float halfX = (obstacle.localScale.x / 2f) + margin;
        float halfZ = (obstacle.localScale.z / 2f) + margin;

        bool insideX = Mathf.Abs(candidatePos.x - center.x) < halfX;
        bool insideZ = Mathf.Abs(candidatePos.z - center.z) < halfZ;

        return insideX && insideZ;
    }

    private void OnDrawGizmosSelected()
    {
        if (visitedTiles == null || platform == null) return;

        float halfX = platform.localScale.x / 2f;
        float halfZ = platform.localScale.z / 2f;
        float cellSizeX = platform.localScale.x / gridSize;
        float cellSizeZ = platform.localScale.z / gridSize;

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 center = transform.parent != null ? transform.parent.position : Vector3.zero;
                center.x += -halfX + (x + 0.5f) * cellSizeX;
                center.y += 0.05f;
                center.z += -halfZ + (z + 0.5f) * cellSizeZ;

                Gizmos.color = visitedTiles[x, z] 
                    ? new Color(0f, 1f, 0f, 0.35f) 
                    : new Color(1f, 0f, 0f, 0.15f);

                Gizmos.DrawCube(center, new Vector3(cellSizeX * 0.9f, 0.02f, cellSizeZ * 0.9f));
            }
        }
    }
}