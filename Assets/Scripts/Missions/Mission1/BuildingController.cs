﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingController : MonoBehaviour
{
    private Health health;
    public Sprite destroyedSprite;

    private SpriteRenderer sr;
    private Collider2D cl;
    private BlinkingSprite blinkingSprite;

    private void Start()
    {
        blinkingSprite = GetComponent<BlinkingSprite>();
        sr = GetComponent<SpriteRenderer>();
        cl = GetComponent<Collider2D>();
        registerHealth();
    }

    private void registerHealth()
    {
        health = GetComponent<Health>();
        // register health delegate
        health.onDead += OnDead;
        health.onHit += OnHit;
    }

    void OnDead(float damage)
    {
        sr.sprite = destroyedSprite;
        cl.enabled = false;
        blinkingSprite.Stop();
    }

    public void OnHit(float damage)
    {
        if (health.IsAlive())
        {
            blinkingSprite.Play();
        }
        else
        {
            GameManager.AddRewardAll(100, 0.05f, 1f, 50);
        }
    }
}
