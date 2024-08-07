using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Warrior : Player
{
    void Awake()
    {
        _playerRig = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();

        SetCharater();
    }

    protected override void SetCharater()
    {
        Die = false;

        MaxHp = 100.0f;
        Hp = MaxHp;
        HpGeneration = 1f;

        MaxMp = 50.0f;
        Mp = MaxMp;
        MpGeneration = 0.2f;

        Speed = 3.0f;

        Gold = 0;

        Attack = 10.0f;
        AttackSpeed = 1.0f;
        Critical = 0.0f;
        AttackRange = 2f;

        Defense = 10.0f;

        Level = 1;
        Exp = 0;
        NextExp = 100;

        StartCoroutine("Regen");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // 플레이어 이동
        Movement();
    }

    private void LateUpdate()
    {
        // 플레이어 이동 애니메이션
        Movement_Anim(); 
    }
}
