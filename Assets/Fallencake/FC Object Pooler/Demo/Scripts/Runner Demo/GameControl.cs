using Fallencake.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using TMPro;

public class GameControl : MonoBehaviour
{
	public static GameControl instance = null;

	[SerializeField]
	private GameObject restartButton;

	[SerializeField]
	private GameObject startButton;

	[SerializeField]
	private TextMeshProUGUI highScoreText;

	[SerializeField]
	private TextMeshProUGUI yourScoreText;

	[SerializeField]
	private FCObjectPooler obstaclesSpawner;

	[SerializeField]
	private Transform spawnPoint;

	[SerializeField]
	private Transform endPoint;

	[SerializeField]
	private MoveLeftCycle [] environment;

	[Range(0F, 100.0f)]
	public float _speed = 1f;

	[SerializeField]
	private float spawnRate = 2f;
	private float nextSpawn;

	[SerializeField]
	private float timeToBoost = 5f;
	private float nextBoost;
	private bool toRight;
	private int highScore = 0;
	private int yourScore = 0;
	private float nextScoreIncrease = 0f;
	public static bool isGameStopped = false;

	// Use this for initialization
	void Start ()
	{
		if (instance == null)
        {
			instance = this;
		} 
		else if (instance != this)
        {
			Destroy(gameObject);
		}

		obstaclesSpawner = GetComponent<FCObjectPooler>();
		restartButton.SetActive (false);
		startButton.SetActive(true);
		isGameStopped = true;
		yourScore = 0;
		highScore = PlayerPrefs.GetInt ("highScore");
		SetEnvironmentSpeed(0);
	}
	
	// Update is called once per frame
	void Update () {
		if (!isGameStopped)
        {
			IncreaseYourScore();

			if (Time.time > nextSpawn)
				SpawnObstacle();

			if (Time.unscaledTime > nextBoost && !isGameStopped)
				BoostSpeed();
		}
		
		highScoreText.text = "High Score: " + highScore;
		yourScoreText.text = "Your Score: " + yourScore;
	}

	public void DinoHit()
	{
		if (yourScore > highScore)
        {
			PlayerPrefs.SetInt("highScore", yourScore);
		}
		//Time.timeScale = 0;
		isGameStopped = true;
		SetEnvironmentSpeed(0);
		restartButton.SetActive (true);
	}

	private GameObject SpawnObstacle()
	{
		float _spawnRate = UnityEngine.Random.Range(0.75f * spawnRate, 2f * spawnRate);
		nextSpawn = Time.time + _spawnRate;

		/// get the next obstacle in the pool and make sure it's not null
		GameObject nextObstacle = obstaclesSpawner.GetPooledGameObject();

		// base checks
		if (nextObstacle == null) { return null; }
		if (nextObstacle.GetComponent<FCPoolableObject>() == null)
		{
			throw new Exception(gameObject.name + " is trying to spawn objects that don't have a PoolableObject component.");
		}

		// position the obstacle
		nextObstacle.transform.position = new Vector3(spawnPoint.transform.position.x, nextObstacle.transform.position.y, nextObstacle.transform.position.z);

		// resize the obstacle
		float scale = UnityEngine.Random.Range(0.75f, 1.25f);
		nextObstacle.transform.localScale = new Vector2(scale, scale);

		// activate the obstacle
		nextObstacle.gameObject.SetActive(true);

		toRight = endPoint.transform.localPosition.x > 0;

		float speed = toRight ? _speed : -_speed;
		nextObstacle.GetComponent<Obstacle>().StartMove(speed, endPoint.transform.position.x);

		return nextObstacle;
	}

	void BoostSpeed()
	{
		nextBoost = Time.unscaledTime +  timeToBoost;
		_speed += 0.25f;
		SetEnvironmentSpeed(-_speed);
	}

	private void SetEnvironmentSpeed(float speed)
    {
		foreach (MoveLeftCycle layer in environment)
        {
			layer.moveSpeed = speed * layer.parallaxFactor;
		}
    }

	void IncreaseYourScore()
	{
		if (Time.unscaledTime > nextScoreIncrease) {
			yourScore += 1;
			nextScoreIncrease = Time.unscaledTime + 1;
		}
	}

	public void StartGame()
	{
		startButton.SetActive(false);
		isGameStopped = false;
		nextSpawn = Time.time + spawnRate;
		nextBoost = Time.unscaledTime + timeToBoost;
		PlayerControler.instance.Run();
		SetEnvironmentSpeed(-_speed);
	}

	public void RestartGame()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

}
