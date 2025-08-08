using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mainBackground; // Reference to the main background sprite renderer
    private float startPos, length;
    public GameObject cam;
    public float parallaxEffect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position.x;
        length = mainBackground.bounds.size.x;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //float distance = cam.transform.position.x * parallaxEffect;
        //transform.position = new Vector3(startPos + distance, transform.position.y, transform.position.z);

        //foreach (Transform child in transform)
        //{
        //    float childPosX = child.position.x;
        //    float camPosX = cam.transform.position.x;

        //    // If the child is too far left, move it to the right end
        //    if (camPosX - childPosX > length)
        //    {
        //        child.position += new Vector3(length * transform.childCount, 0, 0);
        //    }
        //    // If the child is too far right, move it to the left end
        //    else if (childPosX - camPosX > length)
        //    {
        //        child.position -= new Vector3(length * transform.childCount, 0, 0);
        //    }
        //}
        float distance = cam.transform.position.x * parallaxEffect;
        float movement = cam.transform.position.x * (1 - parallaxEffect);

        transform.position = new Vector3(startPos + distance, transform.position.y, transform.position.z);

        if (movement > startPos + length) {
            startPos += length;
        } else if (movement < startPos - length) {
            startPos -= length;
        }
    }
}
