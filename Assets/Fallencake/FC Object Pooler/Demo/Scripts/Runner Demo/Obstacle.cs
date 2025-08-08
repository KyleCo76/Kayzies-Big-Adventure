using Fallencake.Tools;
using UnityEngine;

public class Obstacle : FCPoolableObject
{

	private float _speed;
    private float _endPosX;

    public void StartMove(float speed, float endPosX)
    {
        _speed = speed;
        _endPosX = endPosX;
    }

    override protected void Update()
    {
        if (!GameControl.isGameStopped)
        {
            transform.Translate(Vector3.right * (Time.deltaTime * _speed));
        }
        
        if (_speed > 0 ? (transform.position.x > _endPosX) : (transform.position.x < _endPosX))
        {
            Destroy();
        }
    }

    void OnTriggerEnter2D(Collider2D col)
	{
		if (col.gameObject.CompareTag("Player"))
        {
            col.GetComponent<PlayerControler>().Hit();
            GameControl.instance.DinoHit();
        }	
	}
}
