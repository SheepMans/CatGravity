using UnityEngine;

public class HazardMovementScript : MonoBehaviour
{
    private string hazardName;

    [SerializeField] private float moveAmplitude = 1.5f;  // how far up/down
    [SerializeField] private float moveSpeed = 1f;        // how fast it moves
    private Vector3 startPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hazardName = gameObject.name;
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (hazardName == "MovingHazard01")
        {
            float newY = startPos.y + Mathf.PingPong(Time.time * moveSpeed, moveAmplitude * 2) - moveAmplitude;
            transform.position = new Vector3(startPos.x, newY, startPos.z);

        }
    }
}
