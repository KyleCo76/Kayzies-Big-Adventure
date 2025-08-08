using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControler : MonoBehaviour {

	private Animator animator;
	private Rigidbody2D rb;
	private Vector3 defaultPosition;
	private bool isInputHeld = false;
	private bool isGrounded = true;
	private bool isLowJump = false;
	private float verticalInput = 0f;
	[SerializeField] private float jumpHeight = 5f;
	[SerializeField] private float jumpDuration = 1f;
	[SerializeField] private AnimationCurve _yAnimation;

	public static PlayerControler instance = null;

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

		rb = GetComponent<Rigidbody2D> ();
		animator = GetComponent<Animator> ();
		defaultPosition = transform.position;
	}

	void Update()
	{
		GetInput();
	}

	private void GetInput()
    {
		if (!GameControl.isGameStopped)
        {
			if (Input.GetMouseButtonDown(0))
			{
				isInputHeld = true;
			}
			else if (Input.GetMouseButtonUp(0))
			{
				isInputHeld = false;
			}

			verticalInput = Input.GetAxisRaw("Vertical");
			if ((verticalInput > 0 || isInputHeld) && isGrounded)
			{
				StartCoroutine(Jump());
			}
			if (verticalInput < 0 && isGrounded)
			{
				animator.SetBool("isCrouch", true);
			}
			else
			{
				animator.SetBool("isCrouch", false);
			}
		}
	}

	private IEnumerator Jump()
    {
		isGrounded = false;
		animator.SetBool("isGrounded", false);
		animator.SetTrigger("Jump");
		float expiredSeconds = 0;
		float progress = 0;
		Vector3 startPosition = transform.position;
		float _jumpHeight = jumpHeight;
		float _jumpDuration = jumpDuration;
		while (progress < 1)
        {
			if ((verticalInput > 0 || isInputHeld) && !isLowJump)
            {
				_jumpHeight = jumpHeight;
				_jumpDuration = jumpDuration;
			}
            else
            {
				_jumpHeight -= Time.deltaTime * 5f;
				_jumpDuration -= Time.deltaTime * 0.5f;
				isLowJump = true;
			}
			expiredSeconds += Time.deltaTime;
			progress = expiredSeconds / _jumpDuration;
			transform.position = startPosition + new Vector3(0, _yAnimation.Evaluate(progress) * _jumpHeight, 0);
			yield return null;
		}
		transform.position = defaultPosition;
		animator.SetBool("isGrounded", true);
		isGrounded = true;
		isLowJump = false;
	}

	public void Hit()
    {
		animator.SetTrigger("Hit");
	}

	public void Die()
	{
		GameControl.instance.DinoHit();
	}

	public void Run()
	{
		StartCoroutine(Jump());
	}
}
