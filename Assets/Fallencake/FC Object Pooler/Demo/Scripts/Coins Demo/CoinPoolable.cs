using UnityEngine;
using System.Collections;
using System;

namespace Fallencake.Tools
{
    public class CoinPoolable : FCPoolableObject
    {
        private Vector2 _force = Vector2.one;
        private Animator animator;
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        public void SetImpulse(float force)
        {
            _force = new Vector2 (UnityEngine.Random.Range(-1f, 1f) * force, UnityEngine.Random.Range(0.5f, 1.25f) * force);
            rb.AddForce(_force, ForceMode2D.Impulse);
            StartCoroutine(isGrounded());
        }

        IEnumerator isGrounded()
        {
            animator.SetBool("isGrounded", false);
            while (rb.linearVelocity != Vector2.zero)
            {
                yield return null;
            }
            animator.SetBool("isGrounded", true);
        }
    }
}