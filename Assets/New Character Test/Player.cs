using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class MainPlayer : MonoBehaviour
{
    [SerializeField] private bool win;

    private Rigidbody2D rb;
    private Animator animator;
    public Animator bottomAnimator;
    private SpriteRenderer avatar;

    [Header("Walk and Sprint")]
    public float speed;
    public float speedMultiplier = 2f;

    [Header("Jump")]
    public LayerMask groundLayer;
    public float JumpForce;
    private bool IsGrounded;

    [Header("Shoot")]
    public float rateOfFire;    
    public GameObject bulletPrefab;
    public Transform Weapon;

    [Header("Granate")]
    public GameObject granadeSpawner;
    public GameObject granate;

    private float timeBetweenShots;
    private float lastShotTime;

    [Header("Aim Points")]
    [SerializeField] private Transform aimPoint;

    [SerializeField] private float TopAngleLimit = 40f;
    [SerializeField] private float BottomAngleLimit = -20f;

    [Header("Knife")]
    public float knifeDamage = 500f;     
    private float knifeRange = 0.35f;     
    public LayerMask enemyLayer;     
    public GameObject knifeHitVFXPrefab;

    private bool isKnifing = false;
    public float timeBetweenKnifes = 0.2f;       
    private float lastKnifeTime;

    [Header("Grenade")]
    public GameObject grenadePrefab;
    private float lastGrenadeThrowTime;
    public float grenadeCooldown = 2.0f;


    private Vector3 initScale, negatedScale;

    private Vector2 _mouseWorldPos;

    public GameObject foreground;

    private Health health;
    private bool isDead;

    private bool isHeavyMachineGunActive = false;
    private float heavyMachineGunDuration = 5f;
    private float currentHeavyMachineGunTime = 0f;

    private string causeOfDeath;

    private bool isMobile;
    private bool mobileSprint = false;
    
    private Vector3 handPivotIdle;
    private Vector3 handPivotRun;

    Cinemachine.CinemachineBrain cinemachineBrain;
    public enum CollectibleType
    {
        HeavyMachineGun,
        Ammo,
        MedKit,
    };

    private void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        animator.runtimeAnimatorController = CharacterManager.Instance.GetCharacterPrefab(CharacterManager.Instance.selectedCharacter).GetComponent<CharacterInformation>().Character.PlayerController;
        handPivotIdle = CharacterManager.Instance.GetCharacterPrefab(CharacterManager.Instance.selectedCharacter).GetComponent<CharacterInformation>().Character.HandPivotIdle;
        handPivotRun = CharacterManager.Instance.GetCharacterPrefab(CharacterManager.Instance.selectedCharacter).GetComponent<CharacterInformation>().Character.HandPivotRun;

        rb = gameObject.GetComponent<Rigidbody2D>();
        avatar = gameObject.GetComponent<SpriteRenderer>();
        cinemachineBrain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
        registerHealth();
        initScale = transform.localScale;
        negatedScale = new Vector3 (-gameObject.transform.localScale.x, gameObject.transform.localScale.y, gameObject.transform.localScale.z );

        Weapon.GetComponent<Animator>().runtimeAnimatorController = CharacterManager.Instance.GetCharacterPrefab(CharacterManager.Instance.selectedCharacter).GetComponent<CharacterInformation>().Character.Weapon;

        speed = CharacterManager.Instance.GetCharacterPrefab(CharacterManager.Instance.selectedCharacter).GetComponent<CharacterInformation>().Character.Levels[CharacterManager.Instance.GetOwnedCharacterLevel(CharacterManager.Instance.selectedCharacter)].Agility*0.2f;
        JumpForce = CharacterManager.Instance.GetCharacterPrefab(CharacterManager.Instance.selectedCharacter).GetComponent<CharacterInformation>().Character.Levels[CharacterManager.Instance.GetOwnedCharacterLevel(CharacterManager.Instance.selectedCharacter)].Agility*1.2f;
        GameManager.addAmmo(250);
        GameManager.SetBombs(15);

        isMobile = UIManager.isMobile();
    }

    void ThrowGranade()
    {
        //Debug.Log("Throw");
        if (GameManager.GetBombs() > 0)
        {
            //Debug.Log("Throw");
            rateOfFire = rateOfFire + Time.deltaTime;
            if (MobileManager.GetButtonGrenade())
            {
                GameManager.RemoveBomb();
                if (rateOfFire > lastShotTime)
                {
                    lastShotTime = rateOfFire + timeBetweenShots;

                    StartCoroutine(WaitGranade());

                    lastShotTime = lastShotTime - rateOfFire;
                    rateOfFire = 0.0f;
                }
            }
        }
    }

    private IEnumerator KnifeAttack()
    {
        isKnifing = true;

        yield return new WaitForSeconds(0.1f);          

        RaycastHit2D hit = Physics2D.Raycast(transform.position, aimPoint.right, knifeRange, enemyLayer);

        if (hit.collider != null)
        {
            float distanceToEnemy = Vector2.Distance(transform.position, hit.point);

            if (distanceToEnemy <= knifeRange)
            {
                Instantiate(knifeHitVFXPrefab, hit.point, Quaternion.identity);

                bottomAnimator.SetTrigger("IsKniving");
                Health enemyHealth = hit.collider.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.Hit(knifeDamage);
                }
            }
        }

        yield return new WaitForSeconds(timeBetweenKnifes);

        isKnifing = false;
    }

    private IEnumerator WaitGranade()
    {
        yield return new WaitForSeconds(0.1f);
        BulletManager.GetGrenadePool().Spawn(granadeSpawner.transform.position, granadeSpawner.transform.rotation);
        yield return new WaitForSeconds(0.15f);
    }



    private void registerHealth()
    {
        health = GetComponent<Health>();
        health.onDead += OnDead;
        health.onHit += OnHit;
    }

    private void OnDead(float damage)    
    {
        Died();
        GameManager.PlayerDied("Water Dead");
        AudioManager.PlayDeathAudio();
        Weapon.gameObject.SetActive(false);
        this.enabled = false;
    }

    void Died()
    {
        animator.SetBool("isDying", true);
        isDead = true;
    }

    public void Revive()
    {
        animator.SetBool("isDying", false);
        isDead = false;
        Weapon.gameObject.SetActive(true);
        this.enabled = true;
        health.Revive();
        registerHealth();

        UIManager.DisableReviveUI();
    }

    private void OnHit(float damage)    
    {
        if (!isDead) { 
        UIManager.UpdateHealthUI(health.GetHealth(), health.GetMaxHealth());
        AudioManager.PlayMeleeTakeAudio();
        }
    }
    public void getCollectible(CollectibleType type)
    {
        switch (type)
        {
            case CollectibleType.HeavyMachineGun:
                ActivateHeavyMachineGun();
                break;
            case CollectibleType.MedKit:
                health.IncreaseHealth(health.GetMaxHealth() * 0.2f);
                break;
            case CollectibleType.Ammo:
                GameManager.addAmmo(150);
                GameManager.SetBombs(15);
                UIManager.UpdateAmmoUI();
                break;
            default:
                //Debug.Log("Collectible not found");
                break;
        }
    }

    private void ActivateHeavyMachineGun()
    {
        isHeavyMachineGunActive = true;
        UIManager.TriggerRateOfFireUI(heavyMachineGunDuration);
        rateOfFire *= 4f;
        currentHeavyMachineGunTime = 0f;

    }

    private void DeactivateHeavyMachineGun()
    {
        isHeavyMachineGunActive = false;
        rateOfFire /= 4f;

    }

    private void Update()
    {
        HandleInput();
        if (isHeavyMachineGunActive)
        {
            currentHeavyMachineGunTime += Time.deltaTime;

            if (currentHeavyMachineGunTime >= heavyMachineGunDuration)
            {
                DeactivateHeavyMachineGun();
            }
        }
        if (win)
        {
            GameManager.PlayerWin();
        }
    }

    private void HandleInput()
    {
        HandleMovement();
        Aim();
        ThrowGrenade();
    }

    private bool CheckEnemiesNearby()
    {
        RaycastHit2D hit = Physics2D.Raycast(aimPoint.position, aimPoint.right, knifeRange, enemyLayer);

        return (hit.collider != null);
    }

    private bool Move()
    {
        float horizontalAxis;

        if (isMobile)
        {
            horizontalAxis = MobileManager.GetAxisHorizontal();
        }
        else
        {
            horizontalAxis = Input.GetAxis("Horizontal");
        }
        

        if (Sprint() && ((horizontalAxis != 0)))
        {
            Vector2 movement = new Vector2(horizontalAxis, 0f) * (speed*speedMultiplier);
            rb.velocity = new Vector2(movement.x, rb.velocity.y);
            Weapon.transform.localPosition = handPivotRun;
            return true;
        }
        else if (!Sprint() && ((horizontalAxis != 0)    ))
        {
            Vector2 movement = new Vector2(horizontalAxis, 0f) * speed;
            rb.velocity = new Vector2(movement.x, rb.velocity.y);
            Weapon.transform.localPosition = handPivotIdle;

            return true;
        }
        else
        {
            Weapon.transform.localPosition = handPivotIdle;
            return false;
        }
    }

    private void Aim()
    {
        Vector2 direction = new Vector2(0, 0);

        if (isMobile)
        {
            // Use mobile aim controls
            float mobileAimHorizontal = MobileManager.GetArmAxisHorizontal();
            float mobileAimVertical = MobileManager.GetArmAxisVertical();

            // Check if mobileAimHorizontal and mobileAimVertical are not zero
            if (mobileAimHorizontal != 0f || mobileAimVertical != 0f)
            {
                direction = new Vector2(mobileAimHorizontal, mobileAimVertical);

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                Weapon.rotation = Quaternion.Euler(0f, 0f, angle);
                aimPoint.rotation = Quaternion.Euler(0f, 0f, angle);

                if (direction.x < 0f)
                {
                    gameObject.transform.localScale = negatedScale;
                }
                else
                {
                    gameObject.transform.localScale = initScale;
                }

                if (direction.x < 0f)
                {
                    Weapon.transform.localScale = new Vector3(-1f, -1f, 1f);
                }
                else
                {
                    Weapon.transform.localScale = new Vector3(1f, 1f, 1f);
                }
            }
        }
        else
        {
            // Use mouse aim controls
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f;
            mousePos = Camera.main.ScreenToWorldPoint(mousePos);

            Vector2 pivotPoint = Weapon.position;

            direction = new Vector2(
                mousePos.x - pivotPoint.x,
                mousePos.y - pivotPoint.y
            );

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Weapon.rotation = Quaternion.Euler(0f, 0f, angle);
            aimPoint.rotation = Quaternion.Euler(0f, 0f, angle);

            if (direction.x < 0f)
            {
                gameObject.transform.localScale = negatedScale;
            }
            else
            {
                gameObject.transform.localScale = initScale;
            }

            if (direction.x < 0f)
            {
                Weapon.transform.localScale = new Vector3(-1f, -1f, 1f);
            }
            else
            {
                Weapon.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }
    }








    private bool Sprint()
    {
        if (isMobile)
        {
            return true;
        }
        else
        {
            float sprint = Input.GetAxis("Sprint");

            if (sprint != 0)
            {

                return true;
            }
            else
            {

                return false;
            }
        }

    }


    private void FlipSprite(float axis)
    {
        avatar.flipX = axis != 0;
    }


    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Walkable") || collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Marco Boat"))
        {
            IsGrounded = true;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Water Dead"))
        {
            string causeOfDeath = "Water Dead";

            health.Hit(9999);
            animator.SetBool("IsDying", true);

            if (foreground != null)
                gameObject.transform.parent = foreground.transform;
        }
    }


    private bool Jump()
    {
        if (isMobile)
        {
            if (MobileManager.GetButtonJump())
            {
                rb.velocity = new Vector2(rb.velocity.x, JumpForce);
                IsGrounded = false;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (IsGrounded && ((Input.GetAxis("Jump") != 0)))
            {
                rb.velocity = new Vector2(rb.velocity.x, JumpForce);
                IsGrounded = false;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private void Shoot()
    {
        float currentTime = Time.time;
        bool fireButtonPressed;

        if (isMobile)
        {
            if(MobileManager.GetArmAxisHorizontal() != 0 || MobileManager.GetArmAxisVertical() != 0)
            {
                fireButtonPressed = true;
            }
            else
            {
                fireButtonPressed = false ;
            }
        }
        else
        {
            fireButtonPressed = Input.GetButton("Fire1");
        }

        if (!isKnifing && fireButtonPressed && GameManager.getAmmo() > 0)
        {
            if (CheckEnemiesNearby())
            {
                StartCoroutine(KnifeAttack());
            }
            else
            {
                if (currentTime - lastShotTime >= 1f / rateOfFire)
                {
                    if (bulletPrefab != null)
                    {
                        GameManager.spendAmmo(1);
                        Instantiate(bulletPrefab, aimPoint.position, aimPoint.rotation);
                        lastShotTime = currentTime;
                    }
                    else
                    {
                        //Debug.LogError("Bullet prefab is not assigned in the MainPlayer script.");
                    }
                }
                
            }
        }
    }

    private void ThrowGrenade()
    {
        if (isMobile)
        {
            
            if (MobileManager.GetButtonGrenade())
            {
                if (Time.time - lastGrenadeThrowTime >= grenadeCooldown)
                {
                    if (GameManager.GetBombs() > 0)
                    {
                        bottomAnimator.SetTrigger("IsThrowing");
                        GameManager.RemoveBomb();
                        Instantiate(grenadePrefab, aimPoint.position, aimPoint.rotation);
                        lastGrenadeThrowTime = Time.time;

                    }
                }
            }
        }
        else
        {
            if (Input.GetAxis("grenade") == 1)
            {
                if (Time.time - lastGrenadeThrowTime >= grenadeCooldown)
                {
                    if (GameManager.GetBombs() > 0)
                    {
                        bottomAnimator.SetTrigger("IsThrowing");
                        GameManager.RemoveBomb();
                        Instantiate(grenadePrefab, aimPoint.position, aimPoint.rotation);
                        lastGrenadeThrowTime = Time.time;

                    }
                }
            }
        }

    }


    private void HandleMovement()
    {
        bool moved = Move();
        animator.SetBool("IsWalking", (moved && !Sprint()));
        animator.SetBool("IsRunning", (moved && Sprint()));
        animator.SetBool("Jump", Jump());
        animator.SetBool("IsGrounded", IsGrounded);
        Shoot();
    }
}
