using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseUnit : MonoBehaviour
{
    public string unitName;

    // We should at least set speed to be non-negative. The BattleStateMachine's while loops will go infinite if initiative doesn't advance to the threshold.
    public float currentHP, maxHP, baseATK, currentATK, baseDEF, currentDEF, speed, stateCharge;
    public int fireTokens, waterTokens, earthTokens, skyTokens;
    public bool dualState;

    public double initiative;

    public List<Attack> attackList = new List<Attack>();
    public Sprite portrait;
}
