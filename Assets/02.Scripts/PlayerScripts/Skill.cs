using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public abstract class Skill : NetworkBehaviour
{
    //protected enum SkillType {Attack, Buff};
    protected List<Animator> _anims = new List<Animator>(); // 스킬 애니메이터 관리
    protected bool _isCoolDown; // 현재 쿨타임인지 확인용
    public Sprite _icon; // 스킬 아이콘
    public Image _CD; // 쿨타임 UI
    Text _cdText; // 쿨타임 숫자 표시 UI
    protected float useMp; // 스킬 사용 마나
    protected float cri;

    public override void OnNetworkSpawn()
    {
        if(!IsOwner){
            this.enabled = false;
            return;
        }
        
        // 스킬 애니메이션을 위해 스킬의 애니메이터 추가
        _anims.Add(this.GetComponent<Animator>());

        if(transform.childCount != 0)
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                _anims.Add(transform.GetChild(i).GetComponent<Animator>());
            }
        }

        // 쿨타임 텍스트로 표시하기 위한 UI
        _cdText = _CD.transform.GetChild(0).GetComponent<Text>();
    }

    // 스킬 버튼과 연결된 함수로 버튼에 해당하는 스킬 작동
    public void UseSkill()
    {
        if(!_isCoolDown && GameManager.Instance.player.Mp > useMp)
        {
            GameManager.Instance.player.Mp -= useMp;
            
            foreach(var anim in _anims)
            {
                anim.SetTrigger("UseSkill");
            }

            StartCoroutine(SkillProcess());
        }
    }

    // 각각의 스킬내용 작성
    public abstract IEnumerator SkillProcess();

    // 쿨타임 UI 표시
    public virtual IEnumerator CoolDown(float cd)
    {
        _isCoolDown = true;
        _cdText.gameObject.SetActive(true);
        
        float timer = 0f;
    
        while(timer < cd)
        {
            timer += 0.1f;
            _CD.fillAmount = (cd - timer) / cd;
            _cdText.text = (cd - timer).ToString("F1"); // 소수 첫째짜리까지 표시
            yield return new WaitForSeconds(0.1f);
        }

        _isCoolDown = false;
        _cdText.gameObject.SetActive(false);
    }
}
