﻿using UnityEngine;
using UnityHelpers;

public class BowController : MonoBehaviour
{
    public ArrowController arrowPrefab;
    public ArrowController[] arrowsPool = new ArrowController[5];
    private int arrowPoolIndex;

    public AudioSource bowOutAudio, bowStringAudio, arrowShotAudio;
    private Vector3 alignedPosition;
    private float prevPullPercent;
    private bool isStretching;

    public bool showTrajectory;
    public LineRenderer prediction;
    public float trajectoryTimestep = 0.2f;
    public float trajectoryTime = 5f;

    public Transform arrowPlacement;
    public Transform arrowFireSpot;
    public float minArrowDistanceSqr = 0.01f;
    public float maxPullDistance = 1;
    public float maxLaunchSpeed = 200;
    public float stringMinStretch = 1, stringMaxStretch = 1.675f;

    public float pullPercent { get; private set; }

    public Transform topString, bottomString;

    public PoolSpawner.SpawnEvent onArrowSpawn;

    private void Update()
    {
        Debug.DrawRay(transform.position, transform.right, Color.blue);

        UpdateStretchSound();
        PredictTrajectory();
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(arrowPlacement.position, minArrowDistanceSqr * minArrowDistanceSqr);
    }

    public void PlayBowPickupSound()
    {
        bowOutAudio.Play();
    }
    public void PlayArrowShotAudio()
    {
        arrowShotAudio.Play();
    }
    private void UpdateStretchSound()
    {
        bowStringAudio.transform.position = alignedPosition;
        bowStringAudio.pitch = pullPercent * 0.2f + 1;
        bool shouldStretch = Mathf.Abs(pullPercent - prevPullPercent) > 0.001f;
        if (shouldStretch && !isStretching)
        {
            bowStringAudio.Play();
            isStretching = true;
        }
        else if (!shouldStretch && isStretching)
        {
            bowStringAudio.Stop();
            isStretching = false;
        }
        prevPullPercent = pullPercent;
    }

    public void ShowTrajectory(bool onOff)
    {
        showTrajectory = onOff;
    }
    private void PredictTrajectory()
    {
        prediction.gameObject.SetActive(showTrajectory && pullPercent > 0);

        if (showTrajectory && pullPercent > 0)
        {
            int timesteps = Mathf.RoundToInt(trajectoryTime / trajectoryTimestep);
            prediction.positionCount = timesteps;
            Vector3[] trajectory = new Vector3[timesteps];
            Vector3 currentVelocity = pullPercent * maxLaunchSpeed * arrowFireSpot.forward;
            Vector3 currentPosition = arrowFireSpot.position;
            Vector3 nextPosition;
            trajectory[0] = currentPosition;
            for (int i = 1; i < timesteps; i++)
            {
                nextPosition = PhysicsHelpers.PredictPosition(currentPosition, currentVelocity, 0.05f, trajectoryTimestep * i);
                trajectory[i] = nextPosition;
            }
            prediction.SetPositions(trajectory);
        }
    }
    public void SetPullPercent(float percent)
    {
        pullPercent = Mathf.Clamp01(percent);

        Vector3 pulledPosition = arrowPlacement.position - arrowPlacement.forward * maxPullDistance * pullPercent;

        float adjacentLength = Vector3.Distance(arrowPlacement.position, topString.position);
        float hypotenuselength = Vector3.Distance(pulledPosition, topString.position);
        float stringAngle = Mathf.Acos(adjacentLength / hypotenuselength) * Mathf.Rad2Deg;

        float stretchMultiplier = (stringMaxStretch - stringMinStretch) * pullPercent * pullPercent + stringMinStretch;

        topString.localRotation = Quaternion.Euler(0, -stringAngle, 0);
        topString.localScale = Vector3.one * stretchMultiplier;

        bottomString.localRotation = Quaternion.Euler(0, stringAngle, 0);
        bottomString.localScale = Vector3.one * stretchMultiplier;
    }
    public bool AtArrowPosition(Vector3 position)
    {
        var distanceSqr = (position - arrowPlacement.position).sqrMagnitude;
        DebugPanel.Log("Distance sqr from arrow to bow", distanceSqr);
        return distanceSqr <= minArrowDistanceSqr;
    }
    public float PullAmount(Vector3 position)
    {
        alignedPosition = position.ProjectPointToSurface(arrowPlacement.position, arrowPlacement.right);
        float distance = Vector3.Distance(arrowPlacement.position, alignedPosition);
        Vector3 direction = alignedPosition - arrowPlacement.position;
        if (Vector3.Dot(direction, arrowPlacement.forward) >= 0)
            distance = 0;

        return Mathf.Clamp(distance, 0, maxPullDistance);
    }

    public void DestroyAllArrows()
    {
        for (int i = 0; i < arrowsPool.Length; i++)
            if (arrowsPool[i] != null)
                Destroy(arrowsPool[i].gameObject);
    }
    public ArrowController FireArrow()
    {
        ArrowController shotArrow = null;
        GetArrow((arrow) =>
        {
            shotArrow = arrow;
            Vector3 fireVelocity = arrowFireSpot.forward * maxLaunchSpeed * pullPercent;
            arrow.Shoot(arrowFireSpot.position, arrowFireSpot.rotation, fireVelocity);
            onArrowSpawn?.Invoke(arrow.transform, null);
        });
        PlayArrowShotAudio();
        return shotArrow;
    }
    private void GetArrow(System.Action<ArrowController> onGot)
    {
        if (arrowsPool[arrowPoolIndex] != null)
            Destroy(arrowsPool[arrowPoolIndex].gameObject);

        arrowsPool[arrowPoolIndex] = Instantiate(arrowPrefab);
        arrowsPool[arrowPoolIndex].shotSpeedSqr = maxLaunchSpeed * maxLaunchSpeed;
        onGot(arrowsPool[arrowPoolIndex]);
        arrowPoolIndex = (arrowPoolIndex + 1) % arrowsPool.Length;
    }
}
