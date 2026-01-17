using UnityEngine;

public partial class Enemy : MonoBehaviour
{
    void FixedUpdate()
    {
        if (gm == null || gm.CurrentState != GameManager.GameState.PLAYING) return;
        
        // BOUNDS CHECK SAFEGUARD (Softlock Fix)
        if (transform.position.y < -10f || Mathf.Abs(transform.position.x) > 60f || Mathf.Abs(transform.position.z) > 60f)
        {
            Die();
            return;
        }

        if (isFrozen)
        {
             rb.linearVelocity = Vector3.zero;
             if (anim != null) anim.speed = 0f;
             return;
        }
        else
        {
             if (anim != null) anim.speed = 1f;
        }

        if (Time.frameCount % 30 == 0) UpdateTargetSelection();

        if (target == null) return;

        // BEHAVIORS
        
        EnemyType type = (data != null) ? data.type : EnemyType.Normal;
        
        if (type == EnemyType.Melee) // Melee/Tiger
        {
             // Run Fast
             Vector3 dir = (target.position - transform.position);
             dir.y = 0; dir.Normalize(); // Force Horizontal

             // Velocity Based Movement
             rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
             
             transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
             return; 
        }
        else if (type == EnemyType.Shooter) // FireTiger
        {
             // Maintain distance
             float dist = Vector3.Distance(transform.position, target.position);
             float optimalDist = 12f; // Reduced from 15f to be more aggressive 
             Vector3 moveDir = Vector3.zero;

             if (dist > optimalDist)
             {
                 // Move Closer
                 moveDir = (target.position - transform.position);
             }
             else if (dist < optimalDist - 5f)
             {
                 // Back away
                 moveDir = (transform.position - target.position);
             }
             
             // Apply Velocity
             moveDir.y = 0; moveDir.Normalize();
             rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);

             transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
             
             // Shoot Logic
             if (Time.time > lastShotTime + fireRate)
             {
                 Debug.Log($"[Enemy {name}] Attempting FireBurst. Time: {Time.time}, Last: {lastShotTime}");
                 lastShotTime = Time.time;
                 FireBurst();
             }
             return;
        }
        else // Normal & Boss
        {
             // Standard Chase
             Vector3 dir = (target.position - transform.position);
             dir.y = 0; dir.Normalize(); // Force Horizontal
             
             // Velocity Based Movement
             rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
             
             transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        }
        void UpdateTargetSelection()
    {
        // Default target is Player
        if (gm != null && gm.Player != null)
        {
            Transform playerT = gm.Player.transform;
            Transform bestTarget = playerT;
            float closestDist = Vector3.Distance(transform.position, playerT.position);

            // Check Turrets
            // Optimization: If we have many turrets, this is slow. 
            // Better: GameManager maintains a list. For now, FindObjects (slow but functional for single turret).
            Turret[] turrets = FindObjectsByType<Turret>(FindObjectsSortMode.None);
            
            foreach(var t in turrets)
            {
                if (t != null)
                {
                    float d = Vector3.Distance(transform.position, t.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        bestTarget = t.transform;
                    }
                }
            }

            target = bestTarget;
        }
    }
}
}
