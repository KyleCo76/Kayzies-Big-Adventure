using UnityEngine;
using System.Collections;
using System;

namespace Fallencake.Tools
{
    public class ChestPoolable : FCPoolableObject
    {
        private Vector2 _force = Vector2.one;
        private float coinImpulse = 10f;
        private FCMultiTypeObjectPooler multyPooler;
        private Collider2D theCollider;
        private Animator animator;
        private Rigidbody2D rb;

        [SerializeField] private Transform coinSpawnPoint;

        private void Awake()
        {
            multyPooler = gameObject.GetComponent<FCMultiTypeObjectPooler>();
            theCollider = gameObject.GetComponent<Collider2D>();
            rb = gameObject.GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        public void SetImpulse(float force)
        {
            _force = new Vector2(UnityEngine.Random.Range(-1f, 1f) * force, UnityEngine.Random.Range(0.5f, 1.25f) * force);
            rb.AddForce(_force, ForceMode2D.Impulse);
        }

        public virtual GameObject SpawnObject()
        {
            // get the next object in the pool and make sure it's not null
            GameObject nextObject = multyPooler.GetPooledGameObject();

            // base checks
            if (nextObject == null) { return null; }
            if (nextObject.GetComponent<FCPoolableObject>() == null)
            {
                throw new Exception(gameObject.name + " is trying to spawn objects that don't have a PoolableObject component.");
            }

            // position the object
            nextObject.transform.position = coinSpawnPoint.position;

            animator.SetTrigger("Open");
            
            // activate the object
            StartCoroutine(IgnoreCollisionWith(nextObject.GetComponent<Collider2D>(), 1f));
            nextObject.gameObject.SetActive(true);
            nextObject.GetComponent<CoinPoolable>().SetImpulse(coinImpulse);

            return nextObject;
        }

        public void Unspawn()
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Chest Unspawn"))
            {
                animator.SetTrigger("Unspawn");
            }
        }

        private IEnumerator IgnoreCollisionWith(Collider2D _collider, float _time)
        {
            Physics2D.IgnoreCollision(theCollider, _collider, true);
            yield return new WaitForSeconds(_time);
            Physics2D.IgnoreCollision(theCollider, _collider, false);
        }
    }
}