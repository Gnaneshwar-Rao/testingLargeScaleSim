using UnityEngine;

public struct PerceivedObstacle
{
    public float bearingAngle;          // Bearing angle (?)
    public float bearingAngleRate;      // Time derivative of bearing angle (??)
    public float timeToCollision;       // Time-to-collision (ttc)
    public Vector3 obstacleVelocity;    // Velocity of the obstacle
    public Vector3 position;
}
