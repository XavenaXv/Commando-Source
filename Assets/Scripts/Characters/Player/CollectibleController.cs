﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleController : MonoBehaviour
{
    public PlayerController.CollectibleType type = PlayerController.CollectibleType.HeavyMachineGun;
    private int collectiblePoints = 1000;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameplayManager.IsPlayer(collision))
        {
            GameplayManager.GetPlayer().GetComponent<PlayerController>().getCollectible(type);
            GameplayManager.AddScore(collectiblePoints);
            if (type==PlayerController.CollectibleType.Ammo) // collectible sound
            {
                AudioManager.PlayAmmoGrab();
                AudioManager.PlayOkayVoice();
            }
            else if (type == PlayerController.CollectibleType.HeavyMachineGun)
            {
                AudioManager.PlayHeavyMachineGunVoice();
            }
            else if (type == PlayerController.CollectibleType.MedKit)
            {
                AudioManager.PlayMedKitGrab();
            }
            Destroy(gameObject);
        }
    }
}
