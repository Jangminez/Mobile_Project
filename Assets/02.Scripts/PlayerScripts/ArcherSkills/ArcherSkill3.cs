using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherSkill3 : Skill
{
    private Player player;

    [System.Serializable]
    struct SkillInfo
    {
        public float damage; // 스킬 데미지
        public float interval; // 스킬 데미지 간격
        public float coolDown; // 쿨타임
        public float duration; // 지속시간
        public Collider2D collider; // 콜라이더 
        public List<Collider2D> montsterInRange; // 범위 안에 존재하는 몬스터 관리용

    }
    [SerializeField] SkillInfo _info;
    private float time;

    void Awake()
    {
        // 스킬 정보 초기화
        _info.damage = 0.7f;
        _info.interval = 0.2f;
        _info.coolDown = 40f;
        _info.duration = 10f;
        useMp = 15f;
        _info.collider = GetComponent<Collider2D>();
        _info.collider.enabled = false;
        _info.montsterInRange = new List<Collider2D>();
    }
    public override IEnumerator SkillProcess()
    {
        if (!IsOwner) yield break;

        player = GameManager.Instance.player;

        // 쿨다운 시작
        StartCoroutine(CoolDown(_info.coolDown));

        // 지속시간이 끝나면 콜라이더 비활성화
        _info.collider.enabled = true;

        while (time < _info.duration)
        {
            time += 0.1f;

            if (player._target != null)
            {
                Vector3 direction = (player._target.position - player.transform.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                this.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
            }

            yield return new WaitForSeconds(0.1f);
        }

        _info.collider.enabled = false;

        // 애니메이션 중지
        foreach (var anim in _anims)
        {
            anim.SetTrigger("End");
        }

        time = 0f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return;

        // 몬스터가 처음 스킬범위에 들어왔다면 추가 후 데미지 코루틴 시작
        if (!_info.montsterInRange.Contains(other))
        {
            _info.montsterInRange.Add(other);
            StartCoroutine(SkillDamage(other));
        }

    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsOwner) return;

        // 범위에서 벗어나면 해당 몬스터 리스트에서 제거
        _info.montsterInRange.Remove(other);
    }

    IEnumerator SkillDamage(Collider2D other)
    {
        if (!IsOwner) yield break;

        player = GameManager.Instance.player;

        var enemy = other.GetComponent<IDamgeable>();

        cri = Random.Range(1, 101);

        // 플레이어가 살아있고 몬스터가 범위 안에 있다면 데미지 부여
        while (_info.montsterInRange.Contains(other) && !GameManager.Instance.player.Die)
        {
            if (enemy != null)
            {
                if (other.tag == "Player")
                {
                    player.AttackPlayerServerRpc(damage:
                    cri <= player.Critical ?
                    player.FinalAttack * 1.5f :
                    player.FinalAttack);
                }
                else
                {
                    other.GetComponent<IDamgeable>().Hit(damage:
                    cri <= player.Critical ?
                    player.FinalAttack * _info.damage * 1.5f :
                    player.FinalAttack * _info.damage);
                }
            }

            yield return new WaitForSeconds(_info.interval);
        }

        // 플레이어가 죽으면 애니메이션 중지
        if (GameManager.Instance.player.Die)
        {
            foreach (var anim in _anims)
            {
                anim.SetTrigger("End");
            }
            time = 0f;
        }

        StopCoroutine(SkillDamage(other));
    }
}
