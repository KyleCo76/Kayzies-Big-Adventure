using Fallencake.Tools;
using UnityEngine;
using System.Collections;
using System;

public class ChestSpawner : FCSingleTypeObjectPooler
{
    [Range(0F, 100.0f)]
    public float impulsForce = 1;
    public Collider2D levelBounds;

    private bool isMouseHeld = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            isMouseHeld = true;

            if (levelBounds != null)
            {
                if (hit.collider != null)
                {
                    if (hit.collider == levelBounds)
                    {
                        Vector2 spawnPosition = mousePos2D;
                        SpawnObject(spawnPosition);
                    }
                    else if (hit.collider.gameObject.CompareTag("Chest"))
                    {
                        StopAllCoroutines();
                        StartCoroutine(CoinSpawn(hit.collider.gameObject));
                    }
                }
            }
            else
            {
                Vector2 spawnPosition = mousePos2D;
                SpawnObject(spawnPosition);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseHeld = false;
        }

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                if (hit.collider.gameObject.CompareTag("Chest"))
                {
                    StopAllCoroutines();
                    //hit.collider.gameObject.GetComponent<ChestPoolable>().Destroy();
                    hit.collider.gameObject.GetComponent<ChestPoolable>().Unspawn();
                }
            }
        }
    }

    IEnumerator CoinSpawn(GameObject chest)
    {
        var _target = chest.GetComponent<ChestPoolable>();
        while (isMouseHeld)
        {
            _target.SpawnObject();
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// Spawns a new object and positions it
    /// </summary>
    /// <returns>The pooled game object.</returns>
    public virtual GameObject SpawnObject(Vector3 spawnPosition)
    {
        /// get the next object in the pool and make sure it's not null
        GameObject nextObject = GetPooledGameObject();

        // base checks
        if (nextObject == null) { return null; }
        if (nextObject.GetComponent<FCPoolableObject>() == null)
        {
            throw new Exception(gameObject.name + " is trying to spawn objects that don't have a PoolableObject component.");
        }

        // position the object
        nextObject.transform.position = spawnPosition;
        nextObject.transform.rotation = Quaternion.identity;

        // activate the object
        nextObject.gameObject.SetActive(true);
        nextObject.GetComponent<ChestPoolable>().SetImpulse(impulsForce);

        return nextObject;
    }
}
