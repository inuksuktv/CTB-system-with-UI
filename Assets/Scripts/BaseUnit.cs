using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseUnit : MonoBehaviour
{
    public string unitName;

    public float currentHP, maxHP, baseATK, currentATK, baseDEF, currentDEF, speed, stateCharge;
    public int fireTokens, waterTokens, earthTokens, skyTokens;
    public bool dualState;

    public float initiative, simulatedInitiative;

    public List<BaseAttack> attackList = new List<BaseAttack>();
}
