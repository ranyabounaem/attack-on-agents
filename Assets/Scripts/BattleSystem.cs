﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { IDLE, START, PLAYERTURN, ENEMYTURN, WON, LOST, ESCAPE}

/// <summary>
/// This class is responsible for managing the battle itself
/// </summary>
public class BattleSystem : MonoBehaviour
{
    
    public static BattleSystem instance;

    GameObject player;
	GameObject enemy;

    public GameObject DialoguePanel;
    public GameObject InformationBar;
    InformationBarManager infoBarManager;

    Unit playerUnit;
	Unit enemyUnit;

    EnemyAgent agent;

	public BattleState state;

    public ParticleSystem bloodEffect;
    public ParticleSystem healEffect;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }
        DontDestroyOnLoad(this);
    }

    void Start()
    {
		state = BattleState.IDLE;
        infoBarManager = InformationBar.GetComponent<InformationBarManager>();
    }

    public void SwitchTurn()
    {
        CooldownManager.instance.SwitchTurn();
        if (state == BattleState.IDLE)
        {
            int randomTurn = Random.Range(0, 10);
            if (randomTurn > 4)
            {
                state = BattleState.PLAYERTURN;
                PlayerTurn();
            }
            else
            {
                state = BattleState.ENEMYTURN;
                agent.RequestDecision();
            }
        }
        else if (state == BattleState.PLAYERTURN)
        {
            state = BattleState.ENEMYTURN;
            agent.RequestDecision();
        }
        else if (state == BattleState.ENEMYTURN)
        {
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }

    public void SetupBattle(GameObject playerInBattle, GameObject enemyInBattle)
    {

        player = playerInBattle;
        enemy = enemyInBattle;
        playerUnit = player.GetComponent<Unit>();
        enemyUnit = enemy.GetComponent<Unit>();
        StartCoroutine(infoBarManager.UpdateText("You have entered a battle against " + enemyUnit.unitName));
        playerUnit.SetHUD();
        enemyUnit.SetHUD();
        agent = enemy.GetComponent<EnemyAgent>();
        if (!agent.trainingMode)
        {
            agent.UnfreezeAgent();
            DialoguePanel.SetActive(true);
        }
        else
        {
            enemyUnit.currentHP = enemyUnit.maxHP;
            enemyUnit.SetHP();
            playerUnit.currentHP = playerUnit.maxHP;
            playerUnit.SetHP();
        }
        //agent.BattleSystemSc = this;

        SwitchTurn();
    }

    // FOR AUTOMATION
    void Attack(Unit unitAttacking, Unit unitBeingAttacked)
    {
        if (Vector3.Distance(enemy.transform.position, player.transform.position) >= 1.5)
        {
            StartCoroutine(infoBarManager.UpdateText("Must be in range"));
            SwitchTurn();
            return;
        }

        //int missChance = Random.Range(0, 9);
        //if (missChance > 6)
        //{
        //    //Debug.Log("Miss!");
        //    if (state == BattleState.ENEMYTURN)
        //    {
        //        state = BattleState.PLAYERTURN;
        //        PlayerTurn();
        //    }
        //    else
        //        state = BattleState.ENEMYTURN;
        //    return;
        //}
        int damage = unitAttacking.damage;
        //int critChance = Random.Range(0, 9);
        //if (critChance > 6)
        //{
        //    damage *= 2;
        //    //Debug.Log("Critical!");
        //}
        bool isDead = unitBeingAttacked.TakeDamage(damage);
        ParticleSystem blood = Instantiate(bloodEffect, unitBeingAttacked.transform.position, unitBeingAttacked.transform.rotation);
        Destroy(blood.gameObject, 1f);
        //enemyHUD.SetHP(enemyUnit.currentHP);
        StartCoroutine(infoBarManager.UpdateText(unitAttacking.unitName + "'s attack is successful!"));

        if (isDead && unitBeingAttacked.gameObject.CompareTag("Player"))
        {
            state = BattleState.LOST;
            EndBattle();
        }
        else if (isDead && unitBeingAttacked.gameObject.CompareTag("Enemy"))
        {
            state = BattleState.WON;
            EndBattle();
        }
        else
        {
            SwitchTurn();   
        }
    }

    public void EnemyTurn(float[] vectorAction)
	{
        
        if(vectorAction != null)
        {
            if (vectorAction[0] == 0)
            {
                Attack(enemyUnit, playerUnit);
            }
            else if (vectorAction[0] == 1)
            {
                Heal(enemyUnit);
            }
            agent.AddReward(enemyUnit.currentHP / enemyUnit.maxHP);
            agent.AddReward(-(playerUnit.currentHP / playerUnit.maxHP));

            //else if (vectorAction[2] == 1)
            //{
            //    StartCoroutine(infoBarManager.UpdateText(enemyUnit.unitName + " moves left!"));
            //    enemy.transform.Translate(new Vector3(-1, 0, 0));
            //    state = BattleState.PLAYERTURN;
            //    PlayerTurn();
            //}
            //else if (vectorAction[3] == 1)
            //{
            //    StartCoroutine(infoBarManager.UpdateText(enemyUnit.unitName + " moves right!"));
            //    enemy.transform.Translate(new Vector3(1, 0, 0));
            //    state = BattleState.PLAYERTURN;
            //    PlayerTurn();
            //}
            //else if (vectorAction[4] == 1)
            //{
            //    StartCoroutine(infoBarManager.UpdateText(enemyUnit.unitName + " moves up!"));
            //    enemy.transform.Translate(new Vector3(0, 1, 0));
            //    state = BattleState.PLAYERTURN;
            //    PlayerTurn();
            //}
            //else if (vectorAction[5] == 1)
            //{
            //    StartCoroutine(infoBarManager.UpdateText(enemyUnit.unitName + " moves down!"));
            //    enemy.transform.Translate(new Vector3(0, -1, 0));
            //    state = BattleState.PLAYERTURN;
            //    PlayerTurn();
            //}
        }

    }

    public void EndBattle()
	{
        if(agent.trainingMode)
        {
            enemyUnit.currentHP = enemyUnit.maxHP;
            enemyUnit.SetHP();
            playerUnit.currentHP = playerUnit.maxHP;
            playerUnit.SetHP();
        }
		if(state == BattleState.WON)
		{
            Debug.Log("Agent lost");

            //Update player after winning
            StartCoroutine(infoBarManager.UpdateText("You won the battle!"));
            if(!agent.trainingMode)
            {
                playerUnit.addExperience(50 * enemyUnit.unitLevel);
                Destroy(enemy);
            }
            state = BattleState.IDLE;
        }
        else if (state == BattleState.LOST)
		{
            Debug.Log("Agent won");

            StartCoroutine(infoBarManager.UpdateText("You were defeated."));
            if(!agent.trainingMode)
            {
                enemyUnit.currentHP = enemyUnit.maxHP;
                enemyUnit.SetHP();
                playerUnit.removeExperience(10 * enemyUnit.unitLevel);
            }
            state = BattleState.IDLE;
        }
        else if (state == BattleState.ESCAPE)
        {
            Debug.Log("Player escaped");
            StartCoroutine(infoBarManager.UpdateText("You escaped the fight."));
            enemyUnit.currentHP = enemyUnit.maxHP;
            enemyUnit.SetHP();
            state = BattleState.IDLE;
        }
        
        if (!agent.trainingMode)
            DialoguePanel.SetActive(false);
        else
        {
            SwitchTurn();
        }

	}

    void Heal(Unit healingUnit)
    {
        healingUnit.Heal();
        ParticleSystem healing = Instantiate(healEffect, healingUnit.transform.position, Quaternion.Euler(new Vector3(-90, 0, 0)));
        Destroy(healing.gameObject, 1f);
        StartCoroutine(infoBarManager.UpdateText("You feel renewed strength."));
        SwitchTurn();
    }

    public void PlayerTurn()
	{
        if(agent.trainingMode)
        {
            int random = Random.Range(0, 9);
            if (random > 5)
            {
                Attack(playerUnit, enemyUnit);
            }
            else
            {
                Heal(playerUnit);
            }
        }

        //StartCoroutine(infoBarManager.UpdateText("Choose an action."));
        //dialogueText.text = "Choose an action:";
        //For automation
        //if (agent.trainingMode)
        //{
        //    if (playerUnit.currentHP/ playerUnit.maxHP > enemyUnit.currentHP/enemyUnit.maxHP)
        //        Attack(playerUnit, enemyUnit);
        //    else
        //    {
        //        int random = Random.Range(0, 9);
        //        {
        //            if(playerUnit.currentHP / playerUnit.maxHP > 0.5)
        //            {
        //                if (random > 5)
        //                    Attack(playerUnit, enemyUnit);
        //                else
        //                    Heal(playerUnit);
        //            }
        //            else if (playerUnit.currentHP / playerUnit.maxHP > 0.3)
        //            {
        //                if (random > 7)
        //                    Attack(playerUnit, enemyUnit);
        //                else
        //                    Heal(playerUnit);
        //            }
        //            else if (playerUnit.currentHP / playerUnit.maxHP > 0.1)
        //            {
        //                if (random > 9)
        //                    Attack(playerUnit, enemyUnit);
        //                else
        //                    Heal(playerUnit);
        //            }
        //            else
        //                Heal(playerUnit);
        //        }
        //    }    
        //}
    }

    //    state = BattleState.ENEMYTURN;
    //}
    public void OnSpellButton(int spellId)
    {
        playerUnit.spells[spellId].CastSpell(playerUnit, enemyUnit);
        SwitchTurn();
    }

    public void OnAttackButton()
	{
		if (state != BattleState.PLAYERTURN)
			return;
        Attack(playerUnit, enemyUnit);
    }

	public void OnHealButton()
	{
		if (state != BattleState.PLAYERTURN)
			return;
        Heal(playerUnit);
	}


    //void PlayerShield()
    //{
    //    //playerUnit.Shield();
    //    //call ability manager (shield and player
    //    playerUnit.buffs["Shield"] = 1;
    //    dialogueText.text = "You shielded yourself!";

    //    state = BattleState.ENEMYTURN;
    //}

    //public void OnShieldButton()
    //{
    //    if (state != BattleState.PLAYERTURN)
    //        return;
    //    //StartCoroutine(PlayerHeal());
    //    PlayerShield();
    //}

}
