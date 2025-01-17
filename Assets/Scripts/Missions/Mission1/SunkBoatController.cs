﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class SunkBoatController : MonoBehaviour
{
    public Animator doorAnimator;
    public Animator explosion;

    void OnFinish()
    {
        StartCoroutine(WaitExplosion());
            }

    public void OpenDoor()
    {
        doorAnimator.SetTrigger("open");

        GetComponent<EventSpawn>().onFinish += OnFinish;
        GetComponent<EventSpawn>().Trigger();
    }

    private IEnumerator WaitExplosion()
    {
        yield return new WaitForSeconds(.2f);
        explosion.SetBool("isExploding", true);
        GameManager.AddRewardAll(100, 0.05f, 5f, 100);
        AudioManager.PlayDestroy2();
        yield return new WaitForSeconds(1.7f);
        this.gameObject.SetActive(false);
        CameraManager.AfterSunkBoat();
    }
}
