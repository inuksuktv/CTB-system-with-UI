using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackHandler
{
    public string attackerName, attackTargetName, description;
    public GameObject attacker, target;
    public Attack chosenAttack;
}
