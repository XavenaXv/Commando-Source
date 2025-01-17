﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class VanController : MonoBehaviour
{
    GameObject followPlayer;
    private Health health;
    private BlinkingSprite blinkingSprite;

    [Header("Bridge activation")]
    public BridgeController ownBridge;

    [Header("Enemy activation")]
    public float activationDistance = 1.8f;
    private Rigidbody2D rb;
    private Animator animator;

    [Header("Time shoot")]
    private float shotTime = 0.0f;
    public float fireDelta = 3f;
    private float nextFire = 3f;

    [Header("Bomb")]
    public GameObject bomb;
    public Transform bombSpawner;
    private Vector3 newSpawn;
    private System.Random random = new System.Random();



    void Start()
    {
        followPlayer = GameManager.GetPlayer();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        blinkingSprite = GetComponent<BlinkingSprite>();
        registerHealth();
    }

    void Update()
    {
        if (GameManager.IsGameOver())
            return;
    }

    private void FixedUpdate()
    {
        if (GameManager.IsGameOver())
            return;

        if (health.IsAlive())
        {
            float playerDistance = transform.position.x - followPlayer.transform.position.x;
            if (playerDistance < activationDistance)
            {
                animator.SetBool("isFiring", true);
                shotTime = shotTime + Time.deltaTime;
                if (shotTime > nextFire)
                {
                    nextFire = shotTime + fireDelta;

                    StartCoroutine(SpawnBombs());
                    nextFire = nextFire - shotTime;
                    shotTime = 0.0f;
                }
                else
                {
                    animator.SetBool("isFiring", false);
                }


            }
            else
            {
                animator.SetBool("isFiring", false);
            }
        }
    }

    private void registerHealth()
    {
        health = GetComponent<Health>();
        // register health delegate
        health.onDead += OnDead;
        health.onHit += OnHit;
    }

    private void OnDead(float damage)
    {
        StartCoroutine(Die());
    }

    private void OnHit(float damage)
    {
        blinkingSprite.Play();
    }

    private IEnumerator Die()
    {
        AudioManager.PlayDestroy1();
        GameManager.AddRewardAll(100, 0.05f, 5f, 100);
        animator.SetBool("isDying", true);
        if (rb)
            rb.isKinematic = true;
        GetComponent<BoxCollider2D>().enabled = false;


        yield return new WaitForSeconds(0.2f);
        ownBridge.SetBridgeDestroyed();
        yield return new WaitForSeconds(1.7f);

        Destroy(gameObject);
    }


        private IEnumerator SpawnBombs()
    {
        yield return new WaitForSeconds(0.12f);
        for (int i=0; i<4; i++)
        {
            newSpawn = new Vector3(random.Next((int)(bombSpawner.position.x - 1), (int)(bombSpawner.position.x + 1)), bombSpawner.position.y, bombSpawner.position.z);
            Instantiate(bomb, newSpawn, bombSpawner.transform.rotation);
            yield return new WaitForSeconds(0.3f);
        }
       
    }
}
