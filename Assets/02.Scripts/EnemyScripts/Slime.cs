using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Slime : Enemy
{

    public override void OnNetworkSpawn()
    {
        if(!IsServer) return;

        InitMonster();
        spr = GetComponent<SpriteRenderer>();
    }

    public override void InitMonster()
    {
        if(!IsServer) return;

        if (!stat.isDie)
            _initTransform = this.transform.position;

        else
        {
            anim.SetTrigger("Respawn");
            transform.position = _initTransform;
            _isAttack = false;
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            GetComponent<Collider2D>().enabled = true;
            spr.color = Color.white;
            state = States.Idle;
        }

        MaxHp = 30f;
        Hp = MaxHp;

        stat.attack = 5f;
        stat.attackRange = 2f;
        stat.attackSpeed = 1.5f;

        stat.defense = 1f;

        stat.chaseRange = 5f;
        stat.speed = 1f;

        stat.exp = 10f;
        stat.gold = 30;

        stat.isDie = false;

        StartCoroutine("MonsterState");
    }

    public override IEnumerator EnemyAttack()
    {
        if(!IsServer) yield break;

        while (_isAttack)
        {
            anim.SetTrigger("Attack");
            yield return new WaitForSeconds(1 / stat.attackSpeed);

            if (state != States.Attack)
            {
                _isAttack = false;
                _target = null;
                yield break;
            }

            if (_target != null)
                _target.GetComponent<Player>().Hit(damage: stat.attack);
        }
    }
    public override void Hit(float damage)
    {
        TakeDamageServerRpc(damage);
    }

    public override IEnumerator HitEffect()
    {
        spr.color = Color.gray;
        yield return new WaitForSeconds(0.2f);
        spr.color = Color.white;
    }

    public override void Die()
    {
        Hp = 0f;
        stat.isDie = true;

        state = States.Die;
        anim.SetFloat("RunState", 0f);
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;
        spr.color = Color.gray;

        GiveExpGold(GameManager.Instance.player);

        Invoke("InitMonster", 10f);
    }

    public override void Movement_Anim()
    {
        if (state == States.Chase || state == States.Return)
        {
            anim.SetFloat("RunState", 1f);
        }

        else
        {
            anim.SetFloat("RunState", 0f);
        }
    }
}

