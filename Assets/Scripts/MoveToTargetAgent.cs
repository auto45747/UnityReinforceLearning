using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class MoveToTargetAgent : Agent
{
    //state space = 6 (agentPos(x,y,z) + targetPos(x,y,z))
    [SerializeField] private Transform target;
    [SerializeField] private Transform obstacle;
    [SerializeField] private Transform platform;

    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float agentEdgeMargin = 1.5f;
    [SerializeField] private float targetEdgeMargin = 1.0f;
    
    public override void OnEpisodeBegin()
    {
        //dynamic platform range
        float platformHalfX = platform.localScale.x / 2f;
        float platformHalfZ = platform.localScale.z / 2f;
        //dynamic agent range calculation
        float agentRangeX = Mathf.Max(0.5f, platformHalfX - agentEdgeMargin);
        float agentRangeZ = Mathf.Max(0.5f, platformHalfZ - agentEdgeMargin);
        //dynamic target range calculation
        float targetRangeX = Mathf.Max(0.5f, platformHalfX - targetEdgeMargin);
        float targetRangeZ = Mathf.Max(0.5f, platformHalfZ - targetEdgeMargin);
        
        // 1. Spawn Agent safely without overlapping the obstacle
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

        // 2. Spawn Target safely without overlapping the obstacle or agent
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
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(target.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        transform.localPosition += new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;

        // Punish and reset if agent falls off the edge
        if (transform.localPosition.y < 0f)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("target") || other.transform == target)
        {
            // Base completion reward (always guaranteed on success)
            float baseReward = 0.5f;

            // Speed bonus: scales from 0.5 (instant) down to 0.0 (at Max Step)
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
            AddReward(-0.1f); // โทษแรงขึ้นเมื่อเริ่มชน
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("obstacle") || collision.transform == obstacle)
        {
            AddReward(-0.01f); // หักต่อเนื่องทุกเฟรมถ้ายังแช่อยู่กับกำแพง
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
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
}