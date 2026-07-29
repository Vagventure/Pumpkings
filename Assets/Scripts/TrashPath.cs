using UnityEngine;

public class TrashPath : MonoBehaviour
{
    [SerializeField] private TrashPathDefinition definition;
    [SerializeField] private Transform[] waypoints;

    public int PointCount => waypoints == null ? 0 : waypoints.Length;
    public float MovementSpeed => definition != null ? definition.MovementSpeed : 1f;

    public Vector3 GetPointPosition(int index)
    {
        return waypoints[index].position;
    }

    public bool IsValid()
    {
        if (PointCount == 0)
        {
            return false;
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform waypoint = waypoints[i];

            if (waypoint == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(waypoint.position, 0.15f);

            if (i > 0 && waypoints[i - 1] != null)
            {
                Gizmos.DrawLine(waypoints[i - 1].position, waypoint.position);
            }
        }
    }
}
