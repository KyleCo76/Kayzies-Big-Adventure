using UnityEngine;

public class MoveLeftCycle : MonoBehaviour {

	public float moveSpeed = 1f;
	public float parallaxFactor = 1f;
	[SerializeField]
	private float leftPointX = -10f, rightPointX = 10f;
	public Transform [] environment;

	void LateUpdate ()
	{
		Move();
	}

    public void Move()
    {
		float _speed = moveSpeed;
		foreach (Transform element in environment)
		{
			element.Translate(Vector3.right * (Time.deltaTime * _speed));
			if (_speed > 0 ? (element.position.x > rightPointX) : (element.position.x < leftPointX))
			{
				element.position = new Vector2(_speed > 0 ? leftPointX : rightPointX, element.position.y);
			}
		}
	}
}
