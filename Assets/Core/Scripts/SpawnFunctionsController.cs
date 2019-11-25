﻿using UnityEngine;
using UnityHelpers;

public class SpawnFunctionsController : MonoBehaviour
{
    public string enemiesPoolName;
    private ObjectPool<Transform> enemiesPool;
    public Transform mainEnemyTarget;

    private void Start()
    {
        enemiesPool = PoolManager.GetPool(enemiesPoolName);
    }

    public void OnBirdSpawned(Transform birdTransform)
    {
        var bird = birdTransform.GetComponent<BirdController>();
        bird.Revive();
        bird.SetRandomSkin();
    }
    public void OnEnemySpawned(Transform enemyTransform)
    {
        var enemy = enemyTransform.GetComponentInChildren<EnemyController>();
        enemy.puppet.Resurrect();
        enemy.puppet.Rebuild();
        enemy.puppet.Teleport(enemyTransform.position, enemyTransform.rotation, true);
        enemy.target = mainEnemyTarget; //Temporary, will find better solution

        float diedAt = -1;
        StartCoroutine(CommonRoutines.WaitToDoAction(success =>
        {
            enemiesPool.Return(enemy.transform);
        }, 0, () =>
        {
            if (enemy.isDead && diedAt < 0)
                diedAt = Time.time;

            return enemy.isDead && Time.time - diedAt >= enemy.respawnTime;
        }));
    }
}
