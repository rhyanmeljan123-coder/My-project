using UnityEngine;

public class DungeonCameraFollow : MonoBehaviour
{
    public Transform target;
    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = new Vector3(target.position.x, target.position.y, -10);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * 8f);
    }
}